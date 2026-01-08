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

namespace Unity.Hierarchy.Editor
{
    /// <summary>
    /// The hierarchy node type handler for scenes.
    /// </summary>
    [RequiredByNativeCode(Optional = true), StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/HierarchyEditor/Public/HierarchySceneHandler.h")]
    [NativeHeader("Modules/HierarchyEditor/HierarchySceneHandlerBindings.h")]
    internal sealed partial class HierarchySceneHandler :
        HierarchyNodeTypeHandler,
        IHierarchyEntityIdConverter,
        IHierarchyEditorNodeTypeHandler
    {
        const string k_SceneNodeUssClass = "hierarchy-item__scene-node";
        const string k_NonMainStageSceneNodeUssClass = "hierarchy-item__scene-node--stage";
        const string k_NonMainStageSceneNodeToggleUssClass = "hierarchy-item__scene-node__toggle--stage";
        const string k_SceneNodeContainerUssClass = "hierarchy-item__scene-node-container";
        const string k_ActiveSceneNodeUssClass = "hierarchy-item__active-scene-node";
        const string k_SceneUnloadedUssClass = "unity-disabled";

        internal new static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(HierarchySceneHandler handler) => handler.m_Ptr;
        }

        HierarchyNodeType m_NodeType;
        bool m_IsMainStage;

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
        /// Gets or creates the hierarchy node corresponding to the given scene.
        /// </summary>
        /// <remarks>
        /// If the node hasn't been created yet, returns the future node that will be used for the scene.
        /// An update of the hierarchy will be necessary if you intend to query the hieararchy about this node.
        /// </remarks>
        /// <param name="scene">The scene.</param>
        /// <returns>An hierarchy node.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern HierarchyNode GetOrCreateNode(Scene scene);

        /// <summary>
        /// Gets the scene corresponding to the given hierarchy node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern Scene GetScene(in HierarchyNode node);

        /// <summary>
        /// Retrieves the hierarchy node type for this hierarchy node type handler.
        /// </summary>
        /// <returns>The type of the hierarchy node.</returns>
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
            var name = Hierarchy.GetName(in node);
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
            BuildSceneContextMenu(menu, scene);

            // Let users add extra items.
            using var poolHandle = GenericMenu.Pool.Get(out var genericMenu);
            SceneHierarchyHooks.AddCustomSceneHeaderContextMenuItems(genericMenu, scene);
            menu.AppendFromGenericMenu(genericMenu);
        }

        bool IHierarchyEditorNodeTypeHandler.AcceptParent(HierarchyView view, in HierarchyNode parent) => parent == Hierarchy.Root;

        bool IHierarchyEditorNodeTypeHandler.AcceptChild(HierarchyView view, in HierarchyNode child)
        {
            var gameObjectNodeType = Hierarchy.GetNodeType<HierarchyGameObjectHandler>();
            var subSceneNodeType = Hierarchy.GetNodeType<HierarchySubSceneHandler>();
            var childNodeType = Hierarchy.GetNodeType(in child);
            return childNodeType == gameObjectNodeType || childNodeType == subSceneNodeType;
        }

        bool IHierarchyEditorNodeTypeHandler.CanStartDrag(HierarchyView view, ReadOnlySpan<HierarchyNode> nodes) => true;

        void IHierarchyEditorNodeTypeHandler.OnStartDrag(in HierarchyViewDragAndDropSetupData data)
        {
            var nodeSpan = data.Nodes;
            for (var i = 0; i < nodeSpan.Length; ++i)
            {
                var node = nodeSpan[i];
                if (node == HierarchyNode.Null || Hierarchy.GetNodeTypeHandler(in node) != this)
                    continue;
                var scene = GetScene(in node);
                if (!string.IsNullOrEmpty(scene.path))
                    data.Paths.Add(scene.path);
            }
        }

        DragVisualMode IHierarchyEditorNodeTypeHandler.CanDrop(in HierarchyViewDragAndDropHandlingData data)
        {
            return DoHandleDrop(data, false);
        }

        DragVisualMode IHierarchyEditorNodeTypeHandler.OnDrop(in HierarchyViewDragAndDropHandlingData data)
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

        internal static void BuildSceneContextMenu(DropdownMenu menu, Scene scene)
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
                // Boxing once here instead of in each AppendAction
                object userData = (scene, isNotPlayingAndSceneIsAuthoring: !EditorApplication.isPlaying || scene.isSubScene, hasMultipleScenes);

                menu.AppendAction(L10n.Tr("Save Scene"), obj => SceneHierarchyHooks.SaveScene(obj.userData),
                                  a =>
                                  {
                                      var (_, isNotPlayingAndSceneIsAuthoring, _) = ((Scene, bool, bool))a.userData;
                                      return isNotPlayingAndSceneIsAuthoring ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                                  },
                                  userData);

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
                if (scene.isLoaded)
                {
                    // Unload
                    object userData = (scene, canUnloadScenes: !EditorApplication.isPlaying && !string.IsNullOrEmpty(scene.path) && hasMultipleScenes);

                    menu.AppendAction(L10n.Tr("Unload Scene"), obj => SceneHierarchyHooks.UnloadScene(obj.userData), a =>
                    {
                        var (_, canUnloadScenes) = ((Scene, bool))a.userData;
                        return canUnloadScenes ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                    }, userData);
                }
                else
                {
                    // Load
                    menu.AppendAction(L10n.Tr("Load Scene"), obj => SceneHierarchyHooks.LoadScene(obj.userData),
                                      a => EditorApplication.isPlaying ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal,
                                      scene);
                }

                // Remove
                menu.AppendAction(L10n.Tr("Remove Scene"), obj => SceneHierarchyHooks.RemoveScene(obj.userData),
                                  a => EditorApplication.isPlaying || SceneManager.sceneCount == 1 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal,
                                  scene);
            }

            // Discard changes
            if (scene.isLoaded)
            {
                bool canReload = scene.isDirty && SceneHierarchyHooks.CanSceneBeReloaded(scene);
                bool canDiscardChanges = !EditorApplication.isPlaying && canReload;
                var userData = (scene, canDiscardChanges);

                menu.AppendAction(L10n.Tr("Discard changes"), obj => SceneHierarchyHooks.DiscardChanges(obj.userData), a =>
                {
                    var (_, canDiscardChanges) = ((Scene, bool))a.userData;
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
            var targetNodeType = target == HierarchyNode.Null ? HierarchyNodeType.Null : Hierarchy.GetNodeType(in target);
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
                OpenDraggedScenes(data, insertedOrMovedScenes);

                var entityIds = new EntityId[insertedOrMovedScenes.Count];
                for (var i = 0; i < insertedOrMovedScenes.Count; i++)
                {
                    entityIds[i] = insertedOrMovedScenes[i].handle.ToEntityId();
                }
                Selection.entityIds = entityIds;

                if (target == HierarchyNode.Null)
                    return DragVisualMode.Move;

                Scene dstScene = GetParentScene(in target);
                if (dstScene.IsValid())
                {
                    bool dropAbove = data.InsertAtIndex == targetIndex;
                    if (targetNodeType != sceneNodeType || data.DropPosition == DragAndDropPosition.OverItem)
                        dropAbove = false;

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
            }

            return DragVisualMode.Move;
        }

        void OpenDraggedScenes(in HierarchyViewDragAndDropHandlingData data, List<Scene> openedOrMovedScenes)
        {
            void AddScene(in HierarchyViewDragAndDropHandlingData data, Scene draggedScene, string scenePath)
            {
                if (!draggedScene.IsValid() || GetOrCreateNode(draggedScene) == HierarchyNode.Null)
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

        Scene GetParentScene(in HierarchyNode node)
        {
            var nodeType = Hierarchy.GetNodeType(in node);
            if (nodeType == GetNodeType())
                return GetScene(in node);
            if (nodeType == Hierarchy.GetNodeType<HierarchyGameObjectHandler>())
            {
                var goHandler = Hierarchy.GetNodeTypeHandlerBase<HierarchyGameObjectHandler>();
                var go = goHandler.GetGameObject(in node);
                if (go != null)
                    return go.scene;
            }
            else if (nodeType == Hierarchy.GetNodeType<HierarchySubSceneHandler>())
            {
                var subSceneHandler = Hierarchy.GetNodeTypeHandlerBase<HierarchySubSceneHandler>();
                var go = subSceneHandler.GetGameObject(in node);
                if (go != null)
                    return go.scene;
            }

            return new Scene();
        }

        HierarchyNode IHierarchyEntityIdConverter.GetNode(EntityId entityId) => GetNodeFromEntityId(entityId);

        void IHierarchyEntityIdConverter.GetNodes(ReadOnlySpan<EntityId> entityIds, Span<HierarchyNode> outNodes) => GetNodesFromEntityIds(entityIds, outNodes);

        EntityId IHierarchyEntityIdConverter.GetEntityId(in HierarchyNode node) => GetEntityIdFromNode(in node);

        void IHierarchyEntityIdConverter.GetEntityIds(ReadOnlySpan<HierarchyNode> nodes, Span<EntityId> outEntityIds) => GetEntityIdsFromNodes(nodes, outEntityIds);

        [FreeFunction("HierarchySceneHandlerBindings::GetStaticNodeType", IsThreadSafe = true)]
        static extern int GetStaticNodeType();

        [FreeFunction("HierarchySceneHandlerBindings::IsValid", HasExplicitThis = true, IsThreadSafe = true)]
        internal extern bool IsValid(Scene scene);

        [FreeFunction("HierarchySceneHandlerBindings::OpenSceneUnloaded", HasExplicitThis = true, IsThreadSafe = true)]
        extern void OpenSceneUnloaded(Scene scene);

        [NativeMethod(IsThreadSafe = true)]
        extern HierarchyNode GetNodeFromEntityId(EntityId entityId);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        extern void GetNodesFromEntityIds(ReadOnlySpan<EntityId> entityIds, Span<HierarchyNode> outNodes);

        [NativeMethod(IsThreadSafe = true)]
        extern EntityId GetEntityIdFromNode(in HierarchyNode node);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        extern void GetEntityIdsFromNodes(ReadOnlySpan<HierarchyNode> nodes, Span<EntityId> outEntityIds);

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
