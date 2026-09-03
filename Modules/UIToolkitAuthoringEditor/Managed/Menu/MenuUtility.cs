// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

static class MenuUtility
{
    const string k_UndoCreatePanelRenderer = "Create Panel Renderer";
    const string k_NewVisualTreeAssetDefaultName = "New UXML";
    const string k_StageEntryOptOutKey = "UIToolkit.AutoEnterEditingStage";
    const string k_StageEntryDialogTitle = "Open visual element editing stage";
    const string k_StageEntryDialogMessage =
        "To add this element, Unity will open the Visual Element editing stage to edit the underlying UI Document. " +
        "You can return to the previous stage by clicking the breadcrumb or pressing the back button.";

    /// <summary>
    /// When adding from a top menu, we will prioritize to find an existing <see cref="IPanelComponent"/> component in the
    /// scene or create a new one a the last root sibling and add the element to its targeted <see cref="VisualTreeAsset"/>.
    /// </summary>
    public static void AddElementAsSibling(Type elementType, string variantName = null)
        => AddElement(AddRequest.ForType(elementType, variantName), addAsSibling: true, parentNewGameObjectUnderSelection: false);

    /// <summary>
    /// While adding from a context menu, we will prioritize trying to find (and possible add) a <see cref="IPanelComponent"/>
    /// component on the selected game object and add the element to its targeted <see cref="VisualTreeAsset"/>.
    /// </summary>
    public static void AddElementAsLastChild(Type elementType, string variantName = null)
        => AddElement(AddRequest.ForType(elementType, variantName), addAsSibling: false, parentNewGameObjectUnderSelection: true);

    /// <summary>
    /// Adds a UXML document as a template instance beside the current selection.
    /// </summary>
    public static void AddTemplateAsSibling(VisualTreeAsset template)
        => AddElement(AddRequest.ForTemplate(template), addAsSibling: true, parentNewGameObjectUnderSelection: false);

    /// <summary>
    /// Adds a UXML document as a template instance under the current selection.
    /// </summary>
    public static void AddTemplateAsLastChild(VisualTreeAsset template)
        => AddElement(AddRequest.ForTemplate(template), addAsSibling: false, parentNewGameObjectUnderSelection: true);

    static void AddElement(AddRequest request, bool addAsSibling, bool parentNewGameObjectUnderSelection)
    {
        if (StageUtility.GetCurrentStage() is VisualElementEditingStage activeStage)
        {
            if (!TryResolveStageParent(activeStage, Selection.activeObject as VisualElementSelection, addAsSibling,
                    out var parentVea, out var parentElement))
                return;
            ExecuteAdd(activeStage, request, parentVea, parentElement);
            return;
        }

        // Main Stage authoring edits the scene documents where they are, so the element is added in place.
        // There is no stage to enter, and therefore nothing to ask the user about either.
        if (UIToolkitStageUtility.IsAuthoringEnabledInMainStage)
        {
            AddInMainStage(request, addAsSibling, parentNewGameObjectUnderSelection);
            return;
        }

        if (!ConfirmStageEntry())
            return;

        UIToolkitAuthoringSettings.EnableInSceneUIAuthoring = true;

        switch (Selection.activeObject)
        {
            case VisualTreeAssetSelection { PanelComponent: not null } vtaSelection
                when vtaSelection.PanelComponent.visualTreeAsset:
            {
                var sceneContext = new VisualTreeAssetEditingContext(vtaSelection.PanelComponent.visualTreeAsset, vtaSelection.PanelComponent.panelSettings);
                EnterStageAndAdd(sceneContext, request, parentVea: null);
                return;
            }
            case VisualElementSelection { Element: not null } ves
                when TryBuildContextFromElement(ves.Element, out var elementContext):
            {
                var parentElement = GetFirstSuitableElement(ves.Element, elementContext.EditedVisualTreeAsset);
                var vea = parentElement?.visualElementAsset;
                if (addAsSibling)
                    vea = (VisualElementAsset)vea?.parentAsset;
                else if (parentElement != null && !VisualElementUtility.CanReceiveChildren(parentElement))
                    return;
                EnterStageAndAdd(elementContext, request, vea);
                return;
            }
        }

        AddInAppropriatePanelRendererComponent(Selection.activeGameObject, request, parentNewGameObjectUnderSelection);
    }

    /// <summary>
    /// Adds without leaving the Main Stage, where the scene documents are authored in place: the element goes
    /// straight into the document the selection points at.
    /// </summary>
    static void AddInMainStage(AddRequest request, bool addAsSibling, bool parentNewGameObjectUnderSelection)
    {
        switch (Selection.activeObject)
        {
            // The `Foo.uxml` row stands for the document itself, which takes the element at its root.
            case VisualTreeAssetSelection vtaSelection:
            {
                if (!(Component)vtaSelection.PanelComponent || !vtaSelection.PanelComponent.visualTreeAsset)
                    return;

                AddToPanelComponent(vtaSelection.PanelComponent, request);
                return;
            }
            case VisualElementSelection ves:
            {
                if (ves.Element == null)
                    return;

                // A selection naming an element we cannot author into — one that cannot take children, or one
                // belonging to no scene document at all — is not a reason to go and add somewhere else.
                if (TryResolveMainStageTarget(ves.Element, addAsSibling, out var target))
                    ExecuteMainStageAdd(in target, request);
                return;
            }
        }

        if (TryResolveTargetPanelComponent(Selection.activeGameObject, parentNewGameObjectUnderSelection, out var component))
            AddToPanelComponent(component, request);
    }

    // Dropping a UI Library element on empty Main Stage Hierarchy space, where the root holds GameObjects and
    // no document, creates a GameObject with a PanelRenderer, a new document and the element.
    internal static bool CreatePanelWithElement(Type elementType, string variantName)
    {
        if (!TryCreatePanelRendererAndAsset(null, reusableComponent: null, out var component))
            return false;

        AddToPanelComponent(component, AddRequest.ForType(elementType, variantName));
        return true;
    }

    static void AddToPanelComponent(IPanelComponent component, AddRequest request)
    {
        var document = component.visualTreeAsset;
        if (document == null)
            return;

        // A document that was only just created has no live tree yet; its panel clones it, new element
        // included, on its next update.
        var target = new MainStageAddTarget(document.visualTree, component.GetRootVisualElement());
        ExecuteMainStageAdd(in target, request);
    }

    /// <summary>
    /// Resolves where an add aimed at <paramref name="element"/> has to land in the Main Stage.
    /// </summary>
    /// <remarks>
    /// The selection can point at an element that is not authored — a control's internals, the content container
    /// of a <see cref="ScrollView"/> — so the add is aimed at the closest ancestor that is. The document that
    /// ends up changed is the one owning the resolved asset, which for content cloned from a template is the
    /// template's own document rather than the scene document instantiating it.
    /// </remarks>
    static bool TryResolveMainStageTarget(VisualElement element, bool addAsSibling, out MainStageAddTarget target)
    {
        target = default;

        var authored = element;
        while (authored != null
               && authored is not IPanelComponentRootElement
               && !UIToolkitStageUtility.GetMainStageEditFlags(authored).IsFullyEditable())
        {
            authored = authored.hierarchy.parent;
        }

        if (authored == null)
            return false;

        // A document root has no asset of its own; the document it hosts takes the children instead.
        if (authored is IPanelComponentRootElement rootElement)
        {
            var document = rootElement.panelComponent?.visualTreeAsset;
            if (document == null)
                return false;

            target = new MainStageAddTarget(document.visualTree, authored);
            return true;
        }

        if (addAsSibling)
        {
            if (authored.visualElementAsset.parentAsset is not VisualElementAsset parentAsset)
                return false;

            // A sibling is a child of the selection's own parent, which is the element the result hangs from.
            target = new MainStageAddTarget(parentAsset, authored.parent ?? authored);
            return true;
        }

        if (!VisualElementUtility.CanReceiveChildren(authored))
            return false;

        target = new MainStageAddTarget(authored.visualElementAsset, authored);
        return true;
    }

    static void ExecuteMainStageAdd(in MainStageAddTarget target, AddRequest request)
    {
        var document = target.ParentAsset.visualTreeAsset;

        if (request.IsTemplate)
        {
            if (VisualElementEditingUtility.WillCauseCircularDependency(document, target.ParentElement, request.Template))
            {
                Debug.LogWarning($"Cannot add '{request.Template.name}' here because it would create a circular reference.");
                return;
            }

            AddTemplatesToElementCommand.Execute(CommandSources.Menus, target.ParentAsset, -1, new[] { request.Template });
        }
        else
        {
            AddElementCommand.Execute(CommandSources.Menus, request.ElementType, document, target.ParentAsset, -1, request.VariantName);
        }

        // Both commands filed their selection request by asset alone; narrow it to the instance the selection
        // is in.
        UIToolkitStageUtility.ScopePendingSelectionRequestsTo(target.ParentElement);

        // The commands only write to the authoring assets, and a scene panel does not show an in-memory change
        // to the document it renders until it clones it again.
        UIAssetRegistrySceneTracking.ReloadDocumentOf(target.ParentElement);
    }

    static bool TryResolveNewVisualTreeAssetPath(out string assetPath)
    {
        if (UIToolkitAuthoringSettings.NewVisualTreeAssetLocation == NewVisualTreeAssetLocation.DefaultLocation)
        {
            var folder = "Assets/UI Toolkit";
            Directory.CreateDirectory(folder);
            assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{k_NewVisualTreeAssetDefaultName}.uxml");
            return !string.IsNullOrEmpty(assetPath);
        }

        assetPath = EditorUtility.SaveFilePanelInProject(
            "Create UI Document",
            k_NewVisualTreeAssetDefaultName,
            "uxml",
            "Choose a location for the new UI Document.");
        return !string.IsNullOrEmpty(assetPath);
    }

    static bool ConfirmStageEntry()
    {
        var postFix = UIToolkitAuthoringSettings.EnableInSceneUIAuthoring ? "" : "\n\nthis is an experimental feature that is currently disabled, continuing will automatically enable the feature.";

        return EditorDialog.DisplayDecisionDialogWithOptOut(
            k_StageEntryDialogTitle,
            k_StageEntryDialogMessage + postFix,
            yesButtonText: "Continue",
            noButtonText: "Cancel",
            DialogOptOutDecisionType.ForThisMachine,
            k_StageEntryOptOutKey,
            DialogIconType.Info);
    }

    static void AddInAppropriatePanelRendererComponent(GameObject selectedGo, AddRequest request, bool parentNewGameObjectUnderSelection)
    {
        if (!TryResolveTargetPanelComponent(selectedGo, parentNewGameObjectUnderSelection, out var component))
            return;

        var context = new VisualTreeAssetEditingContext(component.visualTreeAsset, component.panelSettings);
        EnterStageAndAdd(context, request, parentVea: null);
    }

    /// <summary>
    /// The panel component an add lands in when the selection names no document of its own: the one on the
    /// selected GameObject, or a new one — created along with the document it renders.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> when none could be resolved, which includes the user declining to create a new
    /// document.
    /// </returns>
    static bool TryResolveTargetPanelComponent(GameObject selectedGo, bool parentNewGameObjectUnderSelection,
        out IPanelComponent component)
    {
        var existingPanel = (IPanelComponent)selectedGo?.GetComponent<PanelRenderer>()
                            ?? selectedGo?.GetComponent<UIDocument>();

        if ((Component)existingPanel)
        {
            if (existingPanel.visualTreeAsset != null)
            {
                component = existingPanel;
                return true;
            }

            // No usable VTA on the GameObject. Reuse the existing component (so we don't leave an empty Panel
            // sitting next to the new one) rather than creating another one beside it.
            return TryCreatePanelRendererAndAsset(null, existingPanel, out component);
        }

        // No panel on the selection: create a new one rather than adopting another that happens to be in the
        // scene. It hangs under the selected GameObject when the add asked to parent there.
        return TryCreatePanelRendererAndAsset(parentNewGameObjectUnderSelection ? selectedGo : null, null, out component);
    }

    /// <summary>
    /// Resolves where an add aimed at <paramref name="selection"/> has to land in the UI Stage. Both outputs are
    /// left <see langword="null"/> when there is nothing selected to resolve them from, which means the root of
    /// the document being edited.
    /// </summary>
    /// <param name="parentElement">
    /// The live element the new content becomes a child of. It tells apart two instances of one document, which
    /// the asset alone cannot, so the result is selected in the instance the selection is in.
    /// </param>
    /// <returns><see langword="false"/> when the selection resolves to an element that cannot take children.</returns>
    static bool TryResolveStageParent(VisualElementEditingStage stage, VisualElementSelection selection,
        bool addAsSibling, out VisualElementAsset parentVea, out VisualElement parentElement)
    {
        parentVea = null;
        parentElement = null;

        var element = selection?.Element;
        if (element == null)
        {
            // No selection: the add lands at the root of the document being edited, which is what a null
            // parentVea resolves to. The live parent is still named, though — editing a sub-document in context
            // means that root is one template instance among several, and the request has nothing else to tell
            // it from the other clones of the same document.
            parentElement = stage.ResolveLocalRoot();
            return true;
        }

        if (stage.Context.GetElementEditFlags(element) != VisualElementEditFlags.FullyEditable)
        {
            var suitable = GetFirstSuitableElement(element, stage.EditedVisualTreeAsset);
            if (addAsSibling)
            {
                parentVea = (VisualElementAsset)suitable?.visualElementAsset?.parentAsset;
                parentElement = suitable?.parent;
                return true;
            }

            if (suitable != null && !VisualElementUtility.CanReceiveChildren(suitable))
                return false;

            parentVea = suitable?.visualElementAsset;
            parentElement = suitable;
            return true;
        }

        if (addAsSibling)
        {
            parentVea = (VisualElementAsset)element.visualElementAsset?.parentAsset;
            parentElement = element.parent;
            return true;
        }

        if (!VisualElementUtility.CanReceiveChildren(element))
            return false;

        parentVea = element.visualElementAsset;
        parentElement = element;
        return true;
    }

    static VisualElement GetFirstSuitableElement(VisualElement candidate, VisualTreeAsset targetVta)
    {
        var current = candidate;
        while (current != null)
        {
            if (current.visualElementAsset != null && current.visualElementAsset.visualTreeAsset == targetVta)
                return current;
            current = current.hierarchy.parent;
        }

        return null;
    }

    static bool TryBuildContextFromElement(VisualElement element, out VisualTreeAssetEditingContext context)
    {
        var rootElement = element.GetFirstOfType<IPanelComponentRootElement>();
        if (rootElement?.panelComponent?.visualTreeAsset != null)
        {
            var panelComponent = rootElement.panelComponent;
            using var pathHandle = ListPool<TemplateAsset>.Get(out var path);
            element.GenerateSubDocumentPath(path);

            context = path.Count > 0
                ? new VisualTreeAssetEditingContext(panelComponent.visualTreeAsset, path.ToArray(), SubDocumentOptions.InContext, panelComponent.panelSettings)
                : new VisualTreeAssetEditingContext(panelComponent.visualTreeAsset, panelComponent.panelSettings);
            return true;
        }

        // The selection may have outlived its stage; the element is detached but still
        // remembers the VisualTreeAsset it was cloned from. Walk up to find the closest one.
        var current = element;
        while (current != null)
        {
            if (current.visualTreeAssetSource != null)
            {
                context = new VisualTreeAssetEditingContext(current.visualTreeAssetSource);
                return true;
            }
            current = current.hierarchy.parent;
        }

        context = default;
        return false;
    }

    static bool TryCreatePanelRendererAndAsset(GameObject parent, IPanelComponent reusableComponent, out IPanelComponent component)
    {
        component = null;

        if (!TryResolveNewVisualTreeAssetPath(out var assetPath))
            return false;

        var folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        var contents = UIElementsTemplate.CreateUXMLTemplate(folder);
        File.WriteAllText(assetPath, contents);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

        var newVta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
        if (newVta == null)
            return false;

        var defaultPanelSettings = PlayModeMenuItems.GetPanelSettingsFromProjectOrCreate();

        if ((Component)reusableComponent)
        {
            // The right-clicked GameObject already has an empty Panel*; assign the new VTA there
            // instead of creating a sibling component.
            Undo.RecordObject((Object)reusableComponent, k_UndoCreatePanelRenderer);
            reusableComponent.visualTreeAsset = newVta;
            if (reusableComponent.panelSettings == null)
                reusableComponent.panelSettings = defaultPanelSettings;
            component = reusableComponent;
            return true;
        }

        var name = Path.GetFileNameWithoutExtension(assetPath);
        var go = new GameObject(name);
        var panelRenderer = go.AddComponent<PanelRenderer>();
        panelRenderer.visualTreeAsset = newVta;
        panelRenderer.panelSettings = defaultPanelSettings;

        StageUtility.PlaceGameObjectInCurrentStage(go);
        Undo.RegisterCreatedObjectUndo(go, k_UndoCreatePanelRenderer);

        if (parent != null)
        {
            Undo.SetTransformParent(go.transform, parent.transform, k_UndoCreatePanelRenderer);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.layer = parent.layer;
        }

        Selection.activeGameObject = go;
        component = panelRenderer;
        return true;
    }

    static void EnterStageAndAdd(VisualTreeAssetEditingContext context, AddRequest request, VisualElementAsset parentVea)
    {
        var stage = VisualElementEditingStage.GoToStage(context, BreadcrumbBar.SeparatorStyle.Arrow);

        // The stage was only just entered, so it has cloned nothing yet and there is no live parent to name; the
        // add lands at the root of the document it opened on, which exists only once.
        ExecuteAdd(stage, request, parentVea, parentElement: null);
    }

    static void ExecuteAdd(VisualElementEditingStage stage, AddRequest request, VisualElementAsset parentVea,
        VisualElement parentElement)
    {
        if (request.IsTemplate)
        {
            if (stage.Context.WillCauseCircularDependency(request.Template))
            {
                Debug.LogWarning($"Cannot add '{request.Template.name}' here because it would create a circular reference.");
                return;
            }

            var parentAsset = parentVea ?? stage.EditedVisualTreeAsset.visualTree;
            AddTemplatesToElementCommand.Execute(CommandSources.Menus, parentAsset, -1, new[] { request.Template });
        }
        else
        {
            AddElementCommand.Execute(CommandSources.Menus, request.ElementType, stage.EditedVisualTreeAsset, parentVea, -1, request.VariantName);
        }

        // Both commands asked for what they created by asset alone. A staged document can instantiate the same
        // template twice as well, so point the request at the instance the selection is in.
        UIToolkitStageUtility.ScopePendingSelectionRequestsTo(parentElement);
    }

    /// <summary>
    /// Where a Main Stage add lands: the asset the new element is parented to, and the live element it becomes a
    /// child of — which is also the document to re-clone, and the instance to select the result in.
    /// </summary>
    readonly struct MainStageAddTarget
    {
        public readonly VisualElementAsset ParentAsset;
        public readonly VisualElement ParentElement;

        public MainStageAddTarget(VisualElementAsset parentAsset, VisualElement parentElement)
        {
            ParentAsset = parentAsset;
            ParentElement = parentElement;
        }
    }

    readonly struct AddRequest
    {
        public readonly Type ElementType;
        public readonly string VariantName;
        public readonly VisualTreeAsset Template;

        AddRequest(Type elementType, string variantName, VisualTreeAsset template)
        {
            ElementType = elementType;
            VariantName = variantName;
            Template = template;
        }

        public bool IsTemplate => Template != null;

        public static AddRequest ForType(Type elementType, string variantName) => new(elementType, variantName, null);
        public static AddRequest ForTemplate(VisualTreeAsset template) => new(null, null, template);
    }
}
