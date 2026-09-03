// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Builds the "Overrides" submenu of a Main Stage attribute field: one entry per ancestor document,
/// nearest first, each with navigation actions targeting that document. Documents that already carry
/// an override for the attribute are flagged.
/// </summary>
internal static class AttributeOverridesMenu
{
    internal static readonly string k_OverridesMenuName = L10n.Tr("Overrides", null);
    internal static readonly string k_SelectInSceneText = L10n.Tr("Select in Scene", null);
    internal static readonly string k_SelectInProjectText = L10n.Tr("Select in Project", null);
    internal static readonly string k_OpenInContextText = L10n.Tr("Open in Context", null);
    internal static readonly string k_OverriddenLabelFormat = L10n.Tr("{0} (overridden)", null);
    internal static readonly string k_UnsavedDocumentText = L10n.Tr("(unsaved document)", null);
    internal static readonly string k_UnresolvedDocumentText = L10n.Tr("(unresolved document)", null);
    internal static readonly string k_NotInsideDocumentText = L10n.Tr("Only available for elements inside another UXML document", null);
    internal static readonly string k_UnnamedElementText = L10n.Tr("Add a name to enable overrides", null);

    // DropdownMenu treats '/' in item names as submenu nesting, so path-bearing labels use a lookalike
    // that is visually indistinguishable but never starts a submenu.
    const char k_PathSeparatorLookalike = '∕';

    public static void Append(DropdownMenu menu, UxmlAttributesEditingContext context, UxmlSerializedAttributeDescription attribute)
    {
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
            AppendDisabledExplainer(menu, k_UnnamedElementText);
            return;
        }

        using var labelsHandle = HashSetPool<string>.Get(out var usedLabels);
        foreach (var scope in scopes)
        {
            var document = scope.document;
            var label = Sanitize(StyleSheetAssetUtilities.GetDocumentDisplayName(document));

            if (string.IsNullOrEmpty(label))
            {
                // The instance names a document either not yet persisted or no longer resolvable.
                label = document ? k_UnsavedDocumentText : k_UnresolvedDocumentText;
            }

            // Two ancestor documents can share a label; the later one falls back to its full path.
            if (!usedLabels.Add(label))
            {
                var path = Sanitize(AssetDatabase.GetAssetPath(document));
                label = string.IsNullOrEmpty(path) ? $"{label} ({scope.instanceAsset.id})" : path;
                usedLabels.Add(label);
            }

            if (UxmlAssetUtilities.HasAttributeOverrideFor(scope.instanceAsset, element, attribute))
                label = string.Format(k_OverriddenLabelFormat, label);

            AppendDocumentActions(menu, $"{k_OverridesMenuName}/{label}", context, document);
        }
    }

    static void AppendDisabledExplainer(DropdownMenu menu, string text)
    {
        menu.AppendAction($"{k_OverridesMenuName}/{Sanitize(text)}", static _ => { },
            static _ => DropdownMenuAction.Status.Disabled);
    }

    static string Sanitize(string label) => label?.Replace('/', k_PathSeparatorLookalike) ?? string.Empty;

    static void AppendDocumentActions(DropdownMenu menu, string submenu, UxmlAttributesEditingContext context,
        VisualTreeAsset document)
    {
        // Live targets resolve inside the callbacks; edits between opening the menu and clicking re-clone the panel.
        menu.AppendAction($"{submenu}/{Sanitize(k_SelectInSceneText)}",
            _ =>
            {
                if (TryGetSceneSelection(context.element, document, out var selection))
                    Selection.activeObject = selection;
            },
            action => TryGetSceneSelection(context.element, document, out _)
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled);

        menu.AppendAction($"{submenu}/{Sanitize(k_SelectInProjectText)}", _ =>
            {
                if (!document)
                    return;

                Selection.activeObject = document;
                EditorGUIUtility.PingObject(document);
            },
            _ => PersistedDocumentStatus(document));

        menu.AppendAction($"{submenu}/{Sanitize(k_OpenInContextText)}", _ =>
            {
                var originElement = context.element;
                if (originElement == null || !document)
                    return;

                var stage = VisualElementEditingStage.GoToStage(
                    new VisualTreeAssetEditingContext(document, originElement.GetPanelSettings()),
                    BreadcrumbBar.SeparatorStyle.Arrow);

                // The switch can be refused, e.g. from an unsaved-changes prompt.
                if (StageUtility.GetCurrentStage() == stage)
                    UIToolkitStageUtility.RequestSelectionOnNextUpdate(originElement, document);
            },
            _ => LiveDocumentStatus(document));

        menu.AppendAction($"{submenu}/{Sanitize(StageContextMenuUtility.OpenInUIBuilder)}", _ =>
            {
                var originElement = context.element;
                if (originElement?.visualElementAsset == null || !document)
                    return;

                new LoadUIDocumentCommand
                {
                    selectedId = originElement.visualElementAsset.id,
                    selectedInstanceIds = LoadUIDocumentCommand.GetInstanceIds(originElement, document),
                    selectedSourceDocument = originElement.visualElementAsset.visualTreeAsset,
                }.OpenInBuilder(document);
            },
            _ => PersistedDocumentStatus(document));
    }

    // Selecting in the Project window and opening in the UI Builder both need a persisted asset.
    static DropdownMenuAction.Status PersistedDocumentStatus(VisualTreeAsset document)
        => EditorUtility.IsPersistent(document)
            ? DropdownMenuAction.Status.Normal
            : DropdownMenuAction.Status.Disabled;

    // Staging only needs the asset itself, which an instance naming an unresolvable document lacks.
    static DropdownMenuAction.Status LiveDocumentStatus(VisualTreeAsset document)
        => document ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

    static bool TryGetSceneSelection(VisualElement element, VisualTreeAsset document, out UISelectionObject selection)
    {
        selection = null;
        if (element?.panel == null
            || !UxmlAssetUtilities.TryGetEnclosingDocumentSceneTarget(element, document, out var sceneTarget))
            return false;

        selection = sceneTarget.GetSelectionObject();
        return selection != null;
    }
}
