// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEngine.UIElements.HierarchyV2;

namespace Unity.Hierarchy.Editor
{
    /// <summary>
    /// Provides a hierarchy node type handler for <see cref="Scene"/> instances in a <see cref="HierarchyView"/>.
    /// </summary>
    [RequiredByNativeCode(Optional = true), StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/HierarchyEditor/Public/HierarchySceneHandler.h")]
    [NativeHeader("Modules/HierarchyEditor/HierarchySceneHandlerBindings.h")]
    public sealed class HierarchySceneHandler :
        HierarchyNodeTypeHandler,
        IHierarchyEditorNodeTypeHandler
    {
        static readonly UniqueStyleString k_SceneNodeUssClass = new("hierarchy-item__scene-node");
        static readonly UniqueStyleString k_NonMainStageSceneNodeUssClass = new("hierarchy-item__scene-node--stage");
        static readonly UniqueStyleString k_NonMainStageSceneNodeToggleUssClass = new("hierarchy-item__scene-node__toggle--stage");
        static readonly UniqueStyleString k_SceneNodeContainerUssClass = new("hierarchy-item__scene-node-container");
        static readonly UniqueStyleString k_ActiveSceneNodeUssClass = new("hierarchy-item__active-scene-node");
        static readonly UniqueStyleString k_SceneUnloadedUssClass = new("unity-disabled");

        internal new static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(HierarchySceneHandler handler) => handler.m_Ptr;
        }

        HierarchyNodeType m_NodeType;
        bool m_IsMainStage;
        HierarchyView m_BoundView;
        StickyRowController m_StickyRowController;

        HierarchySceneHandler()
        {
            throw new NotSupportedException();
        }

        HierarchySceneHandler(IntPtr nativePtr, Hierarchy hierarchy, HierarchyCommandList cmdList) : base(nativePtr, hierarchy, cmdList)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            m_IsMainStage = StageUtility.GetCurrentStage() is MainStage;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            EditorSceneManager.sceneDirtied += OnSceneDirty;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            AssetEvents.assetsChangedOnHDD += OnAssetsChangedOnHDD;
        }

        protected override void Dispose(bool disposing)
        {
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            EditorSceneManager.sceneDirtied -= OnSceneDirty;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            AssetEvents.assetsChangedOnHDD -= OnAssetsChangedOnHDD;

            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets or creates the <see cref="HierarchyNode"/> that corresponds to the specified <see cref="Scene"/>.
        /// </summary>
        /// <remarks>
        /// If the node doesn't exist, this method returns a future node that will be used for the <see cref="Scene"/>.
        /// To query the <see cref="Hierarchy"/> about this node, update the hierarchy first.
        /// </remarks>
        /// <param name="scene">The <see cref="Scene"/> to get the <see cref="HierarchyNode"/> for.</param>
        /// <returns>The <see cref="HierarchyNode"/> that corresponds to the specified <see cref="Scene"/>.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern HierarchyNode GetOrCreateNode(Scene scene);

        /// <summary>
        /// Gets the <see cref="Scene"/> that corresponds to the specified <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to get the <see cref="Scene"/> for.</param>
        /// <returns>The <see cref="Scene"/> that corresponds to the specified <see cref="HierarchyNode"/>.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern Scene GetScene(in HierarchyNode node);

        /// <summary>
        /// Gets the <see cref="HierarchyNodeType"/> for this <see cref="HierarchySceneHandler"/>.
        /// </summary>
        /// <returns>The <see cref="HierarchyNodeType"/> for <see cref="Scene"/> nodes.</returns>
        public new HierarchyNodeType GetNodeType()
        {
            if (m_NodeType == HierarchyNodeType.Null)
                m_NodeType = new HierarchyNodeType(GetStaticNodeType());

            return m_NodeType;
        }

        protected override void OnBindItem(HierarchyViewItem item)
        {
            // We unfortunately can't conditionally apply inline styles since the control's BindItem happens after. This
            // means, any selected/checked state happens after. We should find a way to expose that method or element
            // which will allow all this logic to take place when the control BindItem happens.
            item.RowContainer?.AddToClassList(k_SceneNodeContainerUssClass);
            item.AddToClassList(k_SceneNodeUssClass);

            var isMainStage = m_IsMainStage;
            item.EnableInClassList(k_NonMainStageSceneNodeUssClass, !isMainStage);
            item.Toggle.EnableInClassList(k_NonMainStageSceneNodeToggleUssClass, !isMainStage);

            var scene = GetScene(item.Node);
            var isActiveScene = EditorSceneManager.GetActiveScene() == scene;
            item.EnableInClassList(k_ActiveSceneNodeUssClass, isActiveScene);
            item.EnableInClassList(k_SceneUnloadedUssClass, !scene.isLoaded);
        }

        protected override void OnUnbindItem(HierarchyViewItem item)
        {
            // We have no choice but to remove this class from the row container, since there is no guarantee
            // that the HierarchyViewItem will be reused in the same row container.
            item.RowContainer?.RemoveFromClassList(k_SceneNodeContainerUssClass);
        }

        protected override void OnBindView(HierarchyView view)
        {
            m_BoundView = view;
            m_StickyRowController = new StickyRowController();
            view.ListView.stickyRowController = m_StickyRowController;
            view.ListView.BeforeRefreshingItems += UpdateStickyIndices;
        }

        protected override void OnUnbindView(HierarchyView view)
        {
            if (m_BoundView != null)
            {
                m_BoundView.ListView.BeforeRefreshingItems -= UpdateStickyIndices;
                m_BoundView.ListView.stickyRowController = null;
            }

            m_BoundView = null;
            m_StickyRowController = null;
        }

        void UpdateStickyIndices()
        {
            if (m_StickyRowController == null || m_BoundView?.ViewModel is not { IsCreated: true } viewModel)
                return;

            m_StickyRowController.ClearWithoutNotify();

            var sceneCount = SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                var node = GetOrCreateNode(scene);
                if (node == HierarchyNode.Null)
                    continue;

                var viewModelIndex = viewModel.IndexOf(in node);
                if (viewModelIndex >= 0)
                    m_StickyRowController.SetStickyWithoutNotify(viewModelIndex, true);
            }
        }

        #region IHierarchyEditorNodeTypeHandler
        bool IHierarchyEditorNodeTypeHandler.CanCut(HierarchyView view) => false;
        bool IHierarchyEditorNodeTypeHandler.OnCut(HierarchyView view) => false;
        bool IHierarchyEditorNodeTypeHandler.CanCopy(HierarchyView view) => false;
        bool IHierarchyEditorNodeTypeHandler.OnCopy(HierarchyView view) => false;
        bool IHierarchyEditorNodeTypeHandler.CanPaste(HierarchyView view) => false;
        bool IHierarchyEditorNodeTypeHandler.OnPaste(HierarchyView view) => false;
        bool IHierarchyEditorNodeTypeHandler.CanPasteAsChild(HierarchyView view) => false;
        bool IHierarchyEditorNodeTypeHandler.OnPasteAsChild(HierarchyView view, bool keepWorldPos) => false;
        bool IHierarchyEditorNodeTypeHandler.CanSetName(HierarchyView view, in HierarchyNode node) => false;
        bool IHierarchyEditorNodeTypeHandler.OnSetName(HierarchyView view, in HierarchyNode node, string name) => false;

        string IHierarchyEditorNodeTypeHandler.GetDisplayName(HierarchyView view, in HierarchyNode node)
        {
            var name = Hierarchy.Exists(node) ? Hierarchy.GetName(in node) : node.ToString();
            var scene = GetScene(node);
            if (scene.IsValid())
            {
                if (!scene.isLoaded)
                    name += " (not loaded)";
                if (scene.isDirty)
                    name += "*";
            }
            return name;
        }

        bool IHierarchyEditorNodeTypeHandler.CanDuplicate(HierarchyView view) => false;
        bool IHierarchyEditorNodeTypeHandler.OnDuplicate(HierarchyView view) => false;
        bool IHierarchyEditorNodeTypeHandler.CanDelete(HierarchyView view) => false;
        bool IHierarchyEditorNodeTypeHandler.OnDelete(HierarchyView view) => false;
        bool IHierarchyEditorNodeTypeHandler.CanFindReferences(HierarchyView view) => false;
        bool IHierarchyEditorNodeTypeHandler.OnFindReferences(HierarchyView view) => false;

        bool IHierarchyEditorNodeTypeHandler.CanDoubleClick(HierarchyView view, in HierarchyNode node)
        {
            var scene = GetScene(in node);
            return !PrefabStageUtility.IsPrefabStageScene(scene) && SceneManager.CanSetAsActiveScene(scene);
        }

        bool IHierarchyEditorNodeTypeHandler.OnDoubleClick(HierarchyView view, in HierarchyNode node)
        {
            var scene = GetScene(in node);
            return SceneManager.SetActiveScene(scene);
        }

        void IHierarchyEditorNodeTypeHandler.GetTooltip(HierarchyViewItem item, bool isFiltering, StringBuilder tooltip)
        {
            if (!Hierarchy.Exists(in item.Node))
                return;

            // By default only show tooltip when filtering
            if (!isFiltering)
                return;

            tooltip.Append(Hierarchy.GetPath(in item.Node));
        }

        void IHierarchyEditorNodeTypeHandler.PopulateContextMenu(HierarchyView view, HierarchyViewItem item, DropdownMenu menu)
        {
            if (StageUtility.GetCurrentStage() is not MainStage)
                return;

            var scene = item == null ? default : GetScene(item.Node);
            BuildSceneContextMenu(menu, scene, GetSelectedScenes(view.ViewModel));

            // Let users add extra items.
            using var poolHandle = GenericMenu.Pool.Get(out var genericMenu);
            SceneHierarchyHooks.AddCustomSceneHeaderContextMenuItems(genericMenu, scene);
            menu.AppendFromGenericMenu(genericMenu);
        }

        internal static Scene[] GetSelectedScenes(HierarchyViewModel viewModel)
        {
            var selectionCount = viewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
            using var _ = ListPool<Scene>.Get(out var selectedScenes);
            if (selectionCount > 0)
            {
                foreach (var node in viewModel.EnumerateNodesWithFlags(HierarchyNodeFlags.Selected))
                {
                    var handler = viewModel.GetNodeTypeHandler(in node);
                    Scene scene = handler switch
                    {
                        HierarchySceneHandler sceneHandler => sceneHandler.GetScene(in node),
                        HierarchySubSceneAuthoringHandler subSceneHandler => subSceneHandler.GetScene(in node),
                        _ => default
                    };

                    if (scene.IsValid())
                        selectedScenes.Add(scene);
                }
            }

            return selectedScenes.ToArray();
        }

        static int GetLoadedScenesCount(Scene[] scenes)
        {
            int loadedScenes = 0;
            foreach (var scene in scenes)
            {
                if (scene.isLoaded)
                    loadedScenes++;
            }
            return loadedScenes;
        }

        bool IHierarchyEditorNodeTypeHandler.AcceptParent(HierarchyView view, in HierarchyNode parent) => parent == Hierarchy.Root;

        bool IHierarchyEditorNodeTypeHandler.AcceptChild(HierarchyView view, in HierarchyNode child)
        {
            var gameObjectNodeType = Hierarchy.GetNodeType<HierarchyGameObjectHandler>();
            var subSceneNodeType = Hierarchy.GetNodeType<HierarchySubSceneAuthoringHandler>();
            var childNodeType = view.ViewModel.GetNodeType(in child);
            return childNodeType == gameObjectNodeType || childNodeType == subSceneNodeType;
        }

        bool IHierarchyEditorNodeTypeHandler.CanStartDrag(HierarchyView view, ReadOnlySpan<HierarchyNode> nodes) => true;

        string IHierarchyEditorNodeTypeHandler.GetDragTitle(HierarchyView view, in HierarchyNode node)
        {
            var scene = GetScene(in node);
            return !string.IsNullOrEmpty(scene.path) ? scene.path : null;
        }

        void IHierarchyEditorNodeTypeHandler.OnStartDrag(in HierarchyViewDragAndDropSetupData data)
        {
            var nodeSpan = data.Nodes;
            for (var i = 0; i < nodeSpan.Length; ++i)
            {
                var node = nodeSpan[i];
                if (node == HierarchyNode.Null)
                    continue;
                var scene = GetScene(in node);
                if (!string.IsNullOrEmpty(scene.path))
                    data.Paths.Add(scene.path);
                else if (scene.IsValid())
                    data.EntityIds.Add(scene.handle.ToEntityId());
            }
        }

        DragVisualMode IHierarchyEditorNodeTypeHandler.CanReorder(in HierarchyViewDragAndDropHandlingData data)
        {
            if (StageNavigationManager.instance.currentStage is not MainStage)
                return DragVisualMode.Rejected;

            return DragVisualMode.Move;
        }

        void IHierarchyEditorNodeTypeHandler.OnReorder(in HierarchyViewDragAndDropHandlingData data)
        {
            // SetParentOfSelection has already moved the scene nodes in the hierarchy.
            // Sync the SceneManager scene order to match the new hierarchy order.
            var viewModel = data.View.ViewModel;
            var sceneNodeType = GetNodeType();
            var root = data.View.Source.Root;

            // Collect scenes in their new hierarchy order, tracking which are selected (moved).
            // Use Source.GetChildren (not viewModel.GetChild): ViewModel read buffer is stale until
            // Update(); Source's children array is synchronously updated by SetParentOfSelection.
            using var _ = ListPool<(Scene scene, bool isSelected)>.Get(out var scenesWithSelection);
            foreach (var child in data.View.Source.EnumerateChildren(in root))
            {
                if (data.View.Source.GetNodeType(in child) != sceneNodeType)
                    continue;
                var scene = GetScene(in child);
                if (!scene.IsValid())
                    continue;
                scenesWithSelection.Add((scene, viewModel.HasFlags(in child, HierarchyNodeFlags.Selected)));
            }

            // Gather moved scenes
            using var __ = ListPool<Scene>.Get(out var movedScenes);
            foreach (var (scene, isSelected) in scenesWithSelection)
            {
                if (isSelected)
                    movedScenes.Add(scene);
            }

            if (movedScenes.Count == 0)
                return;

            // Find the reference scene: the non-selected scene immediately before the moved group.
            Scene dstScene = default;
            bool dropAbove = false;
            Scene lastNonSelected = default;
            for (var i = 0; i < scenesWithSelection.Count; i++)
            {
                var (scene, isSelected) = scenesWithSelection[i];
                if (!isSelected)
                {
                    lastNonSelected = scene;
                }
                else
                {
                    if (lastNonSelected.IsValid())
                    {
                        dstScene = lastNonSelected;
                        dropAbove = false;
                    }
                    else
                    {
                        // No non-selected scene before the moved group; find the first after
                        for (var j = i; j < scenesWithSelection.Count; j++)
                        {
                            var (nextScene, nextIsSelected) = scenesWithSelection[j];
                            if (!nextIsSelected)
                            {
                                dstScene = nextScene;
                                dropAbove = true;
                                break;
                            }
                        }
                    }
                    break;
                }
            }

            if (!dstScene.IsValid())
                return;

            if (dropAbove)
            {
                for (var i = 0; i < movedScenes.Count; i++)
                    EditorSceneManager.MoveSceneBefore(movedScenes[i], dstScene);
            }
            else
            {
                for (var i = movedScenes.Count - 1; i >= 0; i--)
                    EditorSceneManager.MoveSceneAfter(movedScenes[i], dstScene);
            }

            // Sort index sync is handled by HierarchySceneHandler::UpdateEnd, which runs every
            // hierarchy update and iterates scenes in their current SceneManager order.
        }

        DragVisualMode IHierarchyEditorNodeTypeHandler.CanAcceptDrop(in HierarchyViewDragAndDropHandlingData data)
        {
            return DoHandleDrop(data, false);
        }

        DragVisualMode IHierarchyEditorNodeTypeHandler.OnAcceptDrop(in HierarchyViewDragAndDropHandlingData data)
        {
            return DoHandleDrop(data, true);
        }
        #endregion

        void OnSceneDirty(Scene scene)
        {
            if (scene.isSubScene)
                return;

            // Force an update even if nothing has changed.
            CommandList.SetDirty();
        }

        void OnSceneSaved(Scene scene)
        {
            if (scene.isSubScene)
                return;

            // Force an update even if nothing has changed.
            CommandList.SetDirty();

            var sceneNode = GetOrCreateNode(scene);
            if (sceneNode != HierarchyNode.Null)
            {
                // Name might have changed
                CommandList.SetName(in sceneNode, scene.name);
            }
        }

        void OnActiveSceneChanged(Scene previousActiveScene, Scene newActiveScene)
        {
            if (previousActiveScene.isSubScene || newActiveScene.isSubScene)
                return;

            if ((previousActiveScene.IsValid() && IsValid(previousActiveScene)) || (newActiveScene.IsValid() && IsValid(newActiveScene)))
                CommandList.SetDirty();
        }

        void OnAssetsChangedOnHDD(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var asset in importedAssets)
            {
                bool isScene = asset.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
                if (isScene)
                {
                    var scene = EditorSceneManager.GetSceneByPath(asset);

                    // When saving a new scene or as a new scene, the new asset path has not been set on the scene yet.
                    // We have to rely on OnSceneSaved.
                    if (!scene.IsValid() || scene.isSubScene)
                        continue;

                    var sceneNode = GetOrCreateNode(scene);
                    if (sceneNode != HierarchyNode.Null)
                        CommandList.SetName(in sceneNode, scene.name);
                }
            }
        }

        void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            // Any other mode will already be handled by the native sceneLoaded event.
            // We are handling the additive without loading mode here because there is
            // no native event.
            if (mode != OpenSceneMode.AdditiveWithoutLoading)
                return;
            OpenSceneUnloaded(scene);
        }

        static void RemoveAllPrefabInstancesUnusedOverridesFromSceneForMenuItem(DropdownMenuAction obj)
        {
            var userData = obj.userData;
            PrefabUtility.RemoveAllPrefabInstancesUnusedOverridesFromSceneForMenuItem(userData);
        }

        internal static void BuildSceneContextMenu(DropdownMenu menu, Scene scene, Scene[] allSelectedScenes)
        {
            var hasMultipleScenes = EditorSceneManager.sceneCount > 1;

            // Set active
            if (scene.isLoaded)
            {
                menu.AppendAction(L10n.Tr("Set Active Scene"),
                                  a => EditorSceneManager.SetActiveScene((Scene)a.userData),
                                  a => hasMultipleScenes && SceneManager.CanSetAsActiveScene(scene)
                                      ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled,
                                  scene);
                menu.AppendSeparator();
            }

            // Save
            if (scene.isLoaded)
            {
                var actionCondition = !EditorApplication.isPlaying || scene.isSubScene;
                // Boxing once here instead of in each AppendAction
                object userDataMulti = (allSelectedScenes, isNotPlayingAndSceneIsAuthoring: actionCondition, hasMultipleScenes);
                menu.AppendAction(L10n.Tr("Save Scene"), obj => SceneHierarchyHooks.SaveScenes(obj.userData),
                                  a =>
                                  {
                                      var (_, isNotPlayingAndSceneIsAuthoring, _) = ((Scene[], bool, bool))a.userData;
                                      return isNotPlayingAndSceneIsAuthoring ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                                  },
                                  userDataMulti);

                object userData = (scene, isNotPlayingAndSceneIsAuthoring: actionCondition, hasMultipleScenes);
                menu.AppendAction(L10n.Tr("Save Scene As"),
                                  obj => SceneHierarchyHooks.SaveSceneAs(obj.userData),
                                  a =>
                                  {
                                      var (scene, isNotPlayingAndSceneIsAuthoring, _) = ((Scene, bool, bool))a.userData;
                                      return isNotPlayingAndSceneIsAuthoring && !scene.isSubScene ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                                  }, userData);

                menu.AppendAction(L10n.Tr("Save All"),
                                  obj => EditorSceneManager.SaveOpenScenes(),
                                  a =>
                                  {
                                      var (_, isNotPlayingAndSceneIsAuthoring, hasMultipleScenes) = ((Scene, bool, bool))a.userData;
                                      return isNotPlayingAndSceneIsAuthoring && hasMultipleScenes ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                                  }, userData);

                menu.AppendSeparator();
            }

            if (!scene.isSubScene)
            {
                // Do not allow unloading or removing scenes if all loaded scenes are selected
                var isUnloadOrRemoveValid = SceneManager.loadedSceneCount > GetLoadedScenesCount(allSelectedScenes);

                if (scene.isLoaded)
                {
                    // Unload
                    object userData = (allSelectedScenes, canUnloadScenes: isUnloadOrRemoveValid && !EditorApplication.isPlaying && !string.IsNullOrEmpty(scene.path) && hasMultipleScenes);

                    menu.AppendAction(L10n.Tr("Unload Scene"), obj => SceneHierarchyHooks.UnloadScenes(obj.userData), a =>
                    {
                        var (_, canUnloadScenes) = ((Scene[], bool))a.userData;
                        return canUnloadScenes ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                    }, userData);
                }
                else
                {
                    // Load
                    menu.AppendAction(L10n.Tr("Load Scene"), obj => SceneHierarchyHooks.LoadScenes(obj.userData),
                                      a => EditorApplication.isPlaying ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal,
                                      allSelectedScenes);
                }

                // Remove
                bool allScenesSelected = allSelectedScenes.Length == EditorSceneManager.sceneCount;
                menu.AppendAction(L10n.Tr("Remove Scene"), obj => SceneHierarchyHooks.RemoveScenes(obj.userData),
                                  a => !isUnloadOrRemoveValid || allScenesSelected || EditorApplication.isPlaying || SceneManager.sceneCount == 1 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal,
                                  allSelectedScenes);
            }

            // Discard changes
            if (scene.isLoaded)
            {
                var userData = (allSelectedScenes, SceneHierarchyHooks.CanSceneChangesBeDiscarded(scene));
                menu.AppendAction(L10n.Tr("Discard changes"), obj => SceneHierarchyHooks.DiscardChanges(obj.userData), a =>
                {
                    var (_, canDiscardChanges) = ((Scene[], bool))a.userData;
                    return canDiscardChanges ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                }, userData);
            }

            // Ping Scene Asset
            menu.AppendSeparator();
            if (scene.IsValid())
            {
                menu.AppendAction(L10n.Tr("Select Scene Asset"), obj => SceneHierarchy.SelectSceneAsset(obj.userData),
                    a => string.IsNullOrEmpty(((Scene)a.userData).path)
                        ? DropdownMenuAction.Status.Disabled
                        : DropdownMenuAction.Status.Normal,
                    scene);
            }

            if (!scene.isSubScene)
            {
                menu.AppendAction(L10n.Tr("Add New Scene"), obj => SceneHierarchy.AddNewScene(obj.userData),
                                  _ => EditorApplication.isPlaying ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal,
                                  scene);
            }

            menu.AppendSeparator();
            if (scene.IsValid())
            {
                menu.AppendAction(
                    L10n.Tr("Prefab/Remove Unused Overrides..."),
                    RemoveAllPrefabInstancesUnusedOverridesFromSceneForMenuItem,
                    a => ((Scene)a.userData).isLoaded ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled,
                    scene);
            }

            if (scene.isLoaded)
            {
                menu.AppendSeparator();
                if (scene.isSubScene)
                    MenuUtilsForHierarchyWindow.AddCreateGameObjectItemsToSubSceneMenu(menu, scene);
                else
                    MenuUtilsForHierarchyWindow.AddCreateGameObjectItemsToSceneMenu(menu, scene);
            }
        }

        DragVisualMode DoHandleDrop(in HierarchyViewDragAndDropHandlingData data, bool perform)
        {
            var target = data.Target;
            var targetNodeType = target == HierarchyNode.Null ? HierarchyNodeType.Null : data.View.ViewModel.GetNodeType(in target);
            var sceneNodeType = GetNodeType();
            var targetIndex = target == HierarchyNode.Null ? -1 : data.View.ViewModel.IndexOf(in target);

            if (!DragAndDropHelpers.IsDraggingScene(data))
                return DragVisualMode.None;

            // Disallow dragging scenes into the hierarchy when it is in Prefab Mode (we do not support multi-scenes for prefabs yet)
            if (StageNavigationManager.instance.currentStage is not MainStage)
                return DragVisualMode.Rejected;

            if (perform)
            {
                using var _ = ListPool<Scene>.Get(out var insertedOrMovedScenes);

                // OpenDraggedScenes resolves scenes via paths/asset IDs and cannot handle unsaved scenes.
                if (data.Source is CollectionView)
                {
                    foreach (ref readonly var node in data.View.ViewModel.EnumerateNodesWithFlags(HierarchyNodeFlags.Selected))
                    {
                        if (data.View.ViewModel.GetNodeType(in node) != sceneNodeType)
                            continue;

                        var s = GetScene(in node);
                        if (s.IsValid())
                            insertedOrMovedScenes.Add(s);
                    }
                }
                else
                {
                    OpenDraggedScenes(data, insertedOrMovedScenes);
                }

                var entityIds = new EntityId[insertedOrMovedScenes.Count];
                for (var i = 0; i < insertedOrMovedScenes.Count; i++)
                {
                    entityIds[i] = insertedOrMovedScenes[i].handle.ToEntityId();
                }
                Selection.entityIds = entityIds;

                if (target == HierarchyNode.Null)
                    return DragVisualMode.Move;

                var dstScene = GetParentScene(data.View.ViewModel, in target);
                var dropAbove = data.InsertAtIndex == targetIndex;

                if (data.DropPosition == DragAndDropPosition.OutsideItems && data.InsertAtIndex > targetIndex)
                {
                    for (var i = data.InsertAtIndex - 1; i >= 0; i--)
                    {
                        var nodeAtIndex = data.View.ViewModel[i];
                        if (data.View.ViewModel.GetNodeType(nodeAtIndex) == sceneNodeType)
                        {
                            dstScene = GetScene(nodeAtIndex);
                            dropAbove = false;
                            break;
                        }
                    }
                }
                else if (targetNodeType != sceneNodeType || data.DropPosition == DragAndDropPosition.OverItem)
                {
                    dropAbove = false;
                }

                if (!dstScene.IsValid())
                    return DragVisualMode.Move;

                if (dropAbove)
                {
                    for (int i = 0; i < insertedOrMovedScenes.Count; i++)
                        EditorSceneManager.MoveSceneBefore(insertedOrMovedScenes[i], dstScene);
                }
                else
                {
                    for (int i = insertedOrMovedScenes.Count - 1; i >= 0; i--)
                        EditorSceneManager.MoveSceneAfter(insertedOrMovedScenes[i], dstScene);
                }
            }

            return DragVisualMode.Move;
        }

        void OpenDraggedScenes(in HierarchyViewDragAndDropHandlingData data, List<Scene> openedOrMovedScenes)
        {
            using var _ = HashSetPool<string>.Get(out var processedPaths);

            void AddScene(in HierarchyViewDragAndDropHandlingData data, Scene draggedScene, string scenePath)
            {
                // Prevent paths that have been already processed/opened to not go through it again
                if (!processedPaths.Add(scenePath))
                    return;

                var sceneNode = GetOrCreateNode(draggedScene);
                if (data.Source is not CollectionView && draggedScene.IsValid() && sceneNode != HierarchyNode.Null)
                {
                    // Need to further defer the call specially when dragging a new scene together with other existing scenes
                    var view = data.View;
                    view.schedule.Execute(() => view.Ping(sceneNode));
                    return;
                }

                if (!draggedScene.IsValid() || sceneNode == HierarchyNode.Null)
                {
                    var unloaded = data.EventModifiers.HasFlag(UnityEngine.EventModifiers.Alt);
                    if (unloaded)
                        draggedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.AdditiveWithoutLoading);
                    else
                        draggedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }

                if (draggedScene.IsValid())
                    openedOrMovedScenes.Add(draggedScene);
            }

            if (data.EntityIds != null)
            {
                foreach (var sceneAsset in data.EntityIds)
                {
                    string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                    Scene draggedScene = SceneManager.GetSceneByPath(scenePath);
                    AddScene(data, draggedScene, scenePath);
                }
            }
            if (data.Paths?.Length > 0)
            {
                foreach (var scenePath in data.Paths)
                {
                    Scene draggedScene = SceneManager.GetSceneByPath(scenePath);
                    AddScene(data, draggedScene, scenePath);
                }
            }
        }

        Scene GetParentScene(HierarchyViewModel viewModel, in HierarchyNode node)
        {
            var nodeType = viewModel.GetNodeType(in node);
            if (nodeType == GetNodeType())
                return GetScene(in node);
            if (nodeType == Hierarchy.GetNodeType<HierarchyGameObjectHandler>())
            {
                var goHandler = Hierarchy.GetNodeTypeHandlerBase<HierarchyGameObjectHandler>();
                var go = goHandler.GetGameObject(in node);
                if (go != null)
                    return go.scene;
            }
            else if (nodeType == Hierarchy.GetNodeType<HierarchySubSceneAuthoringHandler>())
            {
                var subSceneHandler = Hierarchy.GetNodeTypeHandlerBase<HierarchySubSceneAuthoringHandler>();
                var go = subSceneHandler.GetGameObject(in node);
                if (go != null)
                    return go.scene;
            }

            return new Scene();
        }

        [FreeFunction("HierarchySceneHandlerBindings::GetStaticNodeType", IsThreadSafe = true)]
        static extern int GetStaticNodeType();

        [FreeFunction("HierarchySceneHandlerBindings::IsValid", HasExplicitThis = true, IsThreadSafe = true)]
        internal extern bool IsValid(Scene scene);

        [FreeFunction("HierarchySceneHandlerBindings::OpenSceneUnloaded", HasExplicitThis = true, IsThreadSafe = true)]
        extern void OpenSceneUnloaded(Scene scene);

        #region Called from native
        [RequiredByNativeCode(Optional = true)]
        static IntPtr CreateSceneHandler(IntPtr nativePtr, IntPtr hierarchyPtr, IntPtr cmdListPtr)
        {
            if (nativePtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(nativePtr));
            if (hierarchyPtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(hierarchyPtr));
            if (cmdListPtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(cmdListPtr));

            var handler = new HierarchySceneHandler(nativePtr,
                (Hierarchy)GCHandle.FromIntPtr(hierarchyPtr).Target,
                (HierarchyCommandList)GCHandle.FromIntPtr(cmdListPtr).Target);
            handler.Initialize();
            return GCHandle.ToIntPtr(GCHandle.Alloc(handler));
        }
        #endregion
    }
}
