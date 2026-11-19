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
using UnityEditor.Utils;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
static class MenuUtility
{
    const string k_UndoCreatePanelRenderer = "Create Panel Renderer";
    const string k_NewVisualTreeAssetDefaultName = "New UXML";
    const string k_UnsavedVisualTreeAssetContent = "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:uie=\"UnityEditor.UIElements\" />";

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
    /// Adds an element under the selection of <paramref name="context"/>, in the document of its session. The
    /// global context adds the way <see cref="AddElementAsLastChild(Type, string)"/> does.
    /// </summary>
    public static void AddElementAsLastChild(AuthoringContext context, Type elementType, string variantName = null)
    {
        if (context.IsGlobal)
        {
            AddElementAsLastChild(elementType, variantName);
            return;
        }

        if (!LegacyHierarchyGuard.EnsureNewHierarchy() || context.Session is not { IsDisposed: false } session)
            return;

        if (!TryResolveSessionParent(session, context.Selection.ActiveObject as VisualElementSelection, addAsSibling: false,
                SiblingPlacement.Last, out var parentVea, out var parentElement, out var index))
            return;
        ExecuteAdd(session, AddRequest.ForType(elementType, variantName), parentVea, parentElement, index, context);
    }

    /// <summary>
    /// Adds a library item (a control, or a UXML document as a template instance) to the document of the session
    /// of <paramref name="context"/>, the way a double-click in the UI Library window adds to the scene: as the last
    /// sibling of the selected element, or at the root of the document when nothing is selected. The global context
    /// adds the way <see cref="AddElementAsSibling(Type, string)"/> does.
    /// </summary>
    public static void AddFromLibrary(AuthoringContext context, LibraryItem item)
        => AddNextToContextSelection(context, item, addAsSibling: true, SiblingPlacement.Last);

    /// <summary>
    /// Adds a library item under the selected element of <paramref name="context"/>, in the document of its
    /// session. The global context adds the way <see cref="AddElementAsLastChild(Type, string)"/> does.
    /// </summary>
    public static void AddAsLastChild(AuthoringContext context, LibraryItem item)
        => AddNextToContextSelection(context, item, addAsSibling: false, SiblingPlacement.Last);

    /// <summary>
    /// Inserts a library item next to the selected element of <paramref name="context"/>, or at the root of the
    /// document of its session when nothing is selected. The global context inserts next to the editor selection,
    /// in the UI Stage or the scene.
    /// </summary>
    public static void InsertNextToSelection(AuthoringContext context, LibraryItem item, SiblingPlacement placement)
        => AddNextToContextSelection(context, item, addAsSibling: true, placement);

    static void AddNextToContextSelection(AuthoringContext context, LibraryItem item, bool addAsSibling,
        SiblingPlacement placement)
    {
        var request = item.isAsset
            ? AddRequest.ForTemplate(item.visualTreeAsset)
            : AddRequest.ForType(item.libraryType.type, item.libraryType.variantName);
        if (!request.IsTemplate && request.ElementType == null)
            return;

        if (context.IsGlobal)
        {
            AddElement(request, addAsSibling, parentNewGameObjectUnderSelection: !addAsSibling, placement);
            return;
        }

        if (!LegacyHierarchyGuard.EnsureNewHierarchy() || context.Session is not { IsDisposed: false } session)
            return;

        if (!TryResolveSessionParent(session, context.Selection.ActiveObject as VisualElementSelection, addAsSibling,
                placement, out var parentVea, out var parentElement, out var index))
            return;
        ExecuteAdd(session, request, parentVea, parentElement, index, context);
    }

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

    static void AddElement(AddRequest request, bool addAsSibling, bool parentNewGameObjectUnderSelection,
        SiblingPlacement placement = SiblingPlacement.Last)
    {
        if (!LegacyHierarchyGuard.EnsureNewHierarchy())
            return;

        if (AuthoringContext.Global.Session is { } session)
        {
            if (!TryResolveSessionParent(session, Selection.activeObject as VisualElementSelection, addAsSibling,
                    placement, out var parentVea, out var parentElement, out var index))
                return;
            ExecuteAdd(session, request, parentVea, parentElement, index, AuthoringContext.Global);
            return;
        }

        // Main Stage authoring edits the scene documents where they are, so the element is added in place.
        AddInMainStage(request, addAsSibling, parentNewGameObjectUnderSelection, placement);
    }

    static int ResolveSiblingIndex(VisualElementAsset sibling, SiblingPlacement placement)
    {
        if (placement == SiblingPlacement.Last || sibling?.parentAsset == null)
            return -1;

        return sibling.parentAsset.IndexOf(sibling) + 1;
    }

    /// <summary>
    /// Adds without leaving the Main Stage, where the scene documents are authored in place: the element goes
    /// straight into the document the selection points at.
    /// </summary>
    static void AddInMainStage(AddRequest request, bool addAsSibling, bool parentNewGameObjectUnderSelection,
        SiblingPlacement placement)
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
                if (TryResolveMainStageTarget(ves.Element, addAsSibling, placement, out var target))
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
    static bool TryResolveMainStageTarget(VisualElement element, bool addAsSibling, SiblingPlacement placement,
        out MainStageAddTarget target)
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
            target = new MainStageAddTarget(parentAsset, authored.parent ?? authored,
                ResolveSiblingIndex(authored.visualElementAsset, placement));
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

        CommandExecutionStatus status;
        if (request.IsTemplate)
        {
            if (VisualElementEditingUtility.WillCauseCircularDependency(document, target.ParentElement, request.Template))
            {
                Debug.LogWarning($"Cannot add '{request.Template.name}' here because it would create a circular reference.");
                return;
            }

            status = AddTemplatesToElementCommand.Execute(CommandSources.Menus, target.ParentAsset, target.Index, new[] { request.Template });
        }
        else
        {
            status = AddElementCommand.Execute(CommandSources.Menus, request.ElementType, document, target.ParentAsset, target.Index, request.VariantName);
        }

        if (status != CommandExecutionStatus.Success)
            return;

        // Both commands filed their selection request by asset alone; narrow it to the instance the selection
        // is in.
        UIToolkitStageUtility.ScopePendingSelectionRequestsTo(AuthoringContext.Global, target.ParentElement);

        // A new element has no name to identify it by until the user gives it one. An instance already reads as
        // the document it came from, so it is left alone.
        if (!request.IsTemplate)
            UIToolkitStageUtility.RequestRenameOfPendingSelection(AuthoringContext.Global);

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

    /// <summary>
    /// The panel component an add lands in when the selection names no document of its own: the one on the
    /// selected GameObject, the one on the hierarchy's default parent, or a new one — created along with the
    /// document it renders.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> when none could be resolved, which includes the user declining to create a new
    /// document.
    /// </returns>
    static bool TryResolveTargetPanelComponent(GameObject selectedGo, bool parentNewGameObjectUnderSelection,
        out IPanelComponent component)
    {
        var existingPanel = PanelComponentOn(selectedGo);
        if (existingPanel != null)
            return UsePanelOrAssignNewAsset(existingPanel, out component);

        // An explicit context target wins over the default parent, the way it does for plain GameObject
        // creation: the new panel hangs under the selected GameObject.
        if (parentNewGameObjectUnderSelection && selectedGo != null)
            return TryCreatePanelRendererAndAsset(selectedGo, null, out component);

        // With no target of its own, the add honors the hierarchy's default parent: an add lands in its
        // document when it renders one, and a new panel hangs under it otherwise.
        var defaultParent = SceneView.GetDefaultParentObjectIfSet();
        var defaultParentGo = defaultParent != null ? defaultParent.gameObject : null;

        var defaultParentPanel = PanelComponentOn(defaultParentGo);
        if (defaultParentPanel != null)
            return UsePanelOrAssignNewAsset(defaultParentPanel, out component);

        // No panel anywhere the add may target: create a new one rather than adopting another that happens to
        // be in the scene.
        return TryCreatePanelRendererAndAsset(defaultParentGo, null, out component);
    }

    static IPanelComponent PanelComponentOn(GameObject go)
    {
        if (go == null)
            return null;
        if (go.TryGetComponent<PanelRenderer>(out var renderer))
            return renderer;
        if (go.TryGetComponent<UIDocument>(out var document))
            return document;
        return null;
    }

    static bool UsePanelOrAssignNewAsset(IPanelComponent panel, out IPanelComponent component)
    {
        if (panel.visualTreeAsset != null)
        {
            component = panel;
            return true;
        }

        // No usable VTA on the GameObject. Reuse the existing component (so we don't leave an empty Panel
        // sitting next to the new one) rather than creating another one beside it.
        return TryCreatePanelRendererAndAsset(null, panel, out component);
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
    static bool TryResolveSessionParent(UIDocumentSession session, VisualElementSelection selection,
        bool addAsSibling, SiblingPlacement placement, out VisualElementAsset parentVea, out VisualElement parentElement,
        out int index)
    {
        parentVea = null;
        parentElement = null;
        index = -1;

        if (session.IsDocumentReadOnly)
            return false;

        var element = selection?.Element;
        if (element == null)
        {
            // No selection: the add lands at the root of the document being edited, which is what a null
            // parentVea resolves to. The live parent is still named, though — editing a sub-document in context
            // means that root is one template instance among several, and the request has nothing else to tell
            // it from the other clones of the same document.
            parentElement = session.ResolveLocalRoot();
            return true;
        }

        if (session.GetElementEditFlags(element) != VisualElementEditFlags.FullyEditable)
        {
            var suitable = GetFirstSuitableElement(element, session.EditedVisualTreeAsset);
            if (addAsSibling)
            {
                parentVea = (VisualElementAsset)suitable?.visualElementAsset?.parentAsset;
                parentElement = suitable?.parent;
                index = ResolveSiblingIndex(suitable?.visualElementAsset, placement);
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
            index = ResolveSiblingIndex(element.visualElementAsset, placement);
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

    /// <summary>
    /// Creates a new UI Document on disk, at the settings-configured default location or one the user picks.
    /// </summary>
    /// <returns><see langword="false"/> when the user declined to pick a location or the import failed.</returns>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static bool TryCreateNewVisualTreeAsset(out VisualTreeAsset asset)
    {
        asset = null;

        if (!TryResolveNewVisualTreeAssetPath(out var assetPath))
            return false;

        var directory = Path.GetDirectoryName(assetPath);
        var folder = directory != null ? directory.ConvertSeparatorsToUnity() : null;
        var contents = UIElementsTemplate.CreateUXMLTemplate(folder);
        File.WriteAllText(assetPath, contents);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

        asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
        return asset != null;
    }

    /// <summary>
    /// Creates an empty UI Document that only lives in memory, until it is written to a file. It survives domain
    /// reloads, but not the editor quitting.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static VisualTreeAsset CreateUnsavedVisualTreeAsset(string name)
    {
        new UXMLImporterImpl().ImportXmlFromString(k_UnsavedVisualTreeAssetContent, out var asset);
        asset.name = name;
        asset.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
        if (asset.inlineSheet != null)
            asset.inlineSheet.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
        return asset;
    }

    /// <summary>
    /// Destroys a document made by <see cref="CreateUnsavedVisualTreeAsset"/>, with its undo history and what the
    /// registry knows of it.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static void DestroyUnsavedVisualTreeAsset(VisualTreeAsset asset)
    {
        if (asset == null || EditorUtility.IsPersistent(asset))
            return;

        Undo.ClearUndo(asset);

        // The registry keeps what it knows of a dirty asset after its last holder lets go.
        UIAssetRegistry.LiveInstance?.MarkClean(asset);

        if (asset.inlineSheet != null)
        {
            Undo.ClearUndo(asset.inlineSheet);
            Object.DestroyImmediate(asset.inlineSheet);
        }

        Object.DestroyImmediate(asset);
    }

    static bool TryCreatePanelRendererAndAsset(GameObject parent, IPanelComponent reusableComponent, out IPanelComponent component)
    {
        component = null;

        if (!TryCreateNewVisualTreeAsset(out var newVta))
            return false;

        // Tracked before the caller adds to it, so the registry's clean baseline is the empty document.
        UIAssetRegistry.instance.TrackCreatedAsset(newVta);

        var assetPath = AssetDatabase.GetAssetPath(newVta);

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

    static void ExecuteAdd(UIDocumentSession session, AddRequest request, VisualElementAsset parentVea,
        VisualElement parentElement, int index, AuthoringContext context)
    {
        CommandExecutionStatus status;
        if (request.IsTemplate)
        {
            if (session.WillCauseCircularDependency(request.Template))
            {
                Debug.LogWarning($"Cannot add '{request.Template.name}' here because it would create a circular reference.");
                return;
            }

            var parentAsset = parentVea ?? session.EditedVisualTreeAsset.visualTree;
            status = AddTemplatesToElementCommand.Execute(CommandSources.Menus, parentAsset, index, new[] { request.Template }, context);
        }
        else
        {
            status = AddElementCommand.Execute(CommandSources.Menus, request.ElementType, session.EditedVisualTreeAsset, parentVea, index, request.VariantName, context);
        }

        if (status != CommandExecutionStatus.Success)
            return;

        // Both commands asked for what they created by asset alone. A staged document can instantiate the same
        // template twice as well, so point the request at the instance the selection is in.
        UIToolkitStageUtility.ScopePendingSelectionRequestsTo(context, parentElement);

        // A new element has no name to identify it by until the user gives it one. An instance already reads as
        // the document it came from, so it is left alone, the way a prefab instance is.
        if (!request.IsTemplate)
            UIToolkitStageUtility.RequestRenameOfPendingSelection(context);
    }

    /// <summary>
    /// Where a Main Stage add lands: the asset the new element is parented to, and the live element it becomes a
    /// child of — which is also the document to re-clone, and the instance to select the result in.
    /// </summary>
    readonly struct MainStageAddTarget
    {
        public readonly VisualElementAsset ParentAsset;
        public readonly VisualElement ParentElement;
        public readonly int Index;

        public MainStageAddTarget(VisualElementAsset parentAsset, VisualElement parentElement, int index = -1)
        {
            ParentAsset = parentAsset;
            ParentElement = parentElement;
            Index = index;
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

internal enum SiblingPlacement
{
    Last,
    After
}
