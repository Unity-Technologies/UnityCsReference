// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Builds the "Overrides" submenu of a Main Stage attribute field: one entry per ancestor document,
/// nearest first, each opening that document in a stage with the field's element selected and the field
/// itself focused. Entries note the nearest document and any that already carries an override.
/// </summary>
internal static class AttributeOverridesMenu
{
    internal static readonly string k_OverridesMenuName = L10n.Tr("Overrides", null);
    internal static readonly string k_UnsavedDocumentText = L10n.Tr("(unsaved document)", null);
    internal static readonly string k_UnresolvedDocumentText = L10n.Tr("(unresolved document)", null);
    internal static readonly string k_NotInsideDocumentText = L10n.Tr("No .uxml above this element", null);
    internal static readonly string k_UnnamedElementText = L10n.Tr("Give this element a name", null);
    internal static readonly string k_NearestLabelFormat = L10n.Tr("{0} (nearest)", null);
    internal static readonly string k_OverriddenLabelFormat = L10n.Tr("{0} (overridden)", null);
    internal static readonly string k_NearestOverriddenLabelFormat = L10n.Tr("{0} (nearest, overridden)", null);

    static readonly BindingId k_NameAttributePath = nameof(VisualElement.name);

    // DropdownMenu treats '/' in item names as submenu nesting, so path-bearing labels use a lookalike
    // that is visually indistinguishable but never starts a submenu.
    const char k_PathSeparatorLookalike = '∕';

    public static void Append(DropdownMenu menu, UxmlAttributeFieldDecorator decorator)
    {
        var context = decorator.context;
        var attribute = decorator.boundAttributeDescription;
        var attributePath = decorator.GetFullBindingPath();

        // Every other unlocked inspector rebuilds on the same selection and holds a field for the same
        // element and path, so the focus request has to name this one.
        var inspectorPanel = decorator.panel;
        var element = context.element;

        using var scopesHandle = ListPool<AncestorOverrideScope>.Get(out var scopes);
        UxmlAssetUtilities.GetAncestorOverrideScopes(element, scopes);

        if (scopes.Count == 0)
        {
            AppendDisabledExplainer(menu, k_NotInsideDocumentText);
            return;
        }

        // Overrides are authored by name, but a driven unnamed element still needs the navigation.
        if (string.IsNullOrEmpty(element.name) && !UxmlAssetUtilities.TryGetDrivingAttributeOverride(element, attribute, out _))
        {
            AppendNameElementAction(menu, context);
            return;
        }

        using var labelsHandle = HashSetPool<string>.Get(out var usedLabels);
        for (var i = 0; i < scopes.Count; ++i)
        {
            var scope = scopes[i];
            var document = scope.document;
            var label = Sanitize(StyleSheetAssetUtilities.GetDocumentDisplayName(document));

            if (string.IsNullOrEmpty(label))
            {
                // The instance names a document either not yet persisted or no longer resolvable.
                label = Sanitize(document ? k_UnsavedDocumentText : k_UnresolvedDocumentText);
            }

            // Two ancestor documents can share a label; the later one falls back to its full path.
            if (!usedLabels.Add(label))
            {
                var path = Sanitize(AssetDatabase.GetAssetPath(document));
                label = string.IsNullOrEmpty(path) ? $"{label} ({scope.instanceAsset.id})" : path;
                usedLabels.Add(label);
            }

            var isNearest = i == 0;
            var isOverridden = UxmlAssetUtilities.HasAttributeOverrideFor(scope.instanceAsset, element, attribute);
            var format = (isNearest, isOverridden) switch
            {
                (true, true) => k_NearestOverriddenLabelFormat,
                (true, false) => k_NearestLabelFormat,
                (false, true) => k_OverriddenLabelFormat,
                _ => null,
            };

            // A translation can carry a '/' of its own.
            if (format != null)
                label = Sanitize(string.Format(format, label));

            AppendDocumentAction(menu, label, context, attributePath, inspectorPanel, document);
        }
    }

    static void AppendDisabledExplainer(DropdownMenu menu, string text)
    {
        menu.AppendAction($"{k_OverridesMenuName}/{Sanitize(text)}", static _ => { },
            static _ => DropdownMenuAction.Status.Disabled);
    }

    static void AppendNameElementAction(DropdownMenu menu, UxmlAttributesEditingContext context)
    {
        menu.AppendAction($"{k_OverridesMenuName}/{Sanitize(k_UnnamedElementText)}",
            _ => NameField(context)?.RevealAndFocus(),
            _ => NameField(context) != null
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled);
    }

    // Null in an inspector with no Name field of its own, such as a drawer without the element header.
    static UxmlAttributeFieldDecorator NameField(UxmlAttributesEditingContext context)
        => context.editingController.FindAttributeField(k_NameAttributePath);

    static string Sanitize(string label) => label?.Replace('/', k_PathSeparatorLookalike) ?? string.Empty;

    static void AppendDocumentAction(DropdownMenu menu, string label, UxmlAttributesEditingContext context,
        BindingId attributePath, IPanel inspectorPanel, VisualTreeAsset document)
    {
        // Live targets resolve inside the callback; edits between opening the menu and clicking re-clone the panel.
        // Staging only needs the asset itself, which an instance naming an unresolvable document lacks.
        menu.AppendAction($"{k_OverridesMenuName}/{label}",
            _ => OpenInContext(context, attributePath, inspectorPanel, document),
            _ => document ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
    }

    static void OpenInContext(UxmlAttributesEditingContext context, BindingId attributePath, IPanel inspectorPanel,
        VisualTreeAsset document)
    {
        // The asset carries the selection into the stage, so without it there is nothing to land on.
        var originElement = context.element;
        if (originElement?.visualElementAsset == null || !document)
            return;

        var stage = UIStageNavigation.Navigate(
            new VisualTreeAssetEditingContext(document, originElement.GetPanelSettings()),
            BreadcrumbBar.SeparatorStyle.Arrow);

        // The switch can be refused, e.g. from an unsaved-changes prompt.
        if (StageUtility.GetCurrentStage() != stage)
            return;

        UIToolkitStageUtility.RequestSelectionOnNextUpdate(originElement, document);
        UIToolkitStageUtility.RequestFocusOfPendingSelection(attributePath, inspectorPanel);
    }
}
