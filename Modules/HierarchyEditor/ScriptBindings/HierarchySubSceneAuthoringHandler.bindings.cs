// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Hierarchy.Editor
{
    /// <summary>
    /// Provides a <see cref="HierarchyNodeTypeHandler"/> for subscenes.
    /// </summary>
    [RequiredByNativeCode(Optional = true), StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/HierarchyEditor/Public/HierarchySubSceneAuthoringHandler.h")]
    [NativeHeader("Modules/HierarchyEditor/HierarchySubSceneAuthoringHandlerBindings.h")]
    public sealed class HierarchySubSceneAuthoringHandler :
        HierarchyNodeTypeHandler,
        IHierarchyEditorNodeTypeHandler
    {
        static readonly UniqueStyleString k_SubSceneNodeUssClass = new("hierarchy-item__scene-node");

        [NoAutoStaticsCleanup]
        internal static Action<GameObject> OpenSubScene;
        [NoAutoStaticsCleanup]
        internal static Action<GameObject> CloseSubScene;
        [NoAutoStaticsCleanup]
        internal static Action<GameObject> ReimportSubScene;
        [NoAutoStaticsCleanup]
        internal static Func<GameObject, bool> IsSubSceneOpen;
        [NoAutoStaticsCleanup]
        internal static Action<GameObject> OnSubSceneDoubleClick;

        internal new static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(HierarchySubSceneAuthoringHandler handler) => handler.m_Ptr;
        }

        HierarchyNodeType m_NodeType;

        HierarchySubSceneAuthoringHandler()
        {
            throw new NotSupportedException();
        }

        HierarchySubSceneAuthoringHandler(IntPtr nativePtr, Hierarchy hierarchy, HierarchyCommandList cmdList) : base(nativePtr, hierarchy, cmdList)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            AssetEvents.assetsChangedOnHDD += OnAssetsChangedOnHDD;
            EditorSceneManager.sceneDirtied += OnSceneDirty;
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        protected override void Dispose(bool disposing)
        {
            AssetEvents.assetsChangedOnHDD -= OnAssetsChangedOnHDD;
            EditorSceneManager.sceneDirtied -= OnSceneDirty;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets or creates the <see cref="HierarchyNode"/> for the subscene with the specified <see cref="EntityId"/>.
        /// </summary>
        /// <remarks>
        /// If the node doesn't exist yet, this method returns a future node that will be used for the scene.
        /// Update the <see cref="Hierarchy"/> before you query it about this node.
        /// </remarks>
        /// <param name="entityId">The <see cref="EntityId"/> of the subscene to find or create a node for.</param>
        /// <returns>The <see cref="HierarchyNode"/> for the specified subscene.</returns>
        public HierarchyNode GetOrCreateNode(EntityId entityId) => GetOrCreateNodeFromEntityId(entityId);

        /// <summary>
        /// Gets or creates the <see cref="HierarchyNode"/> for the subscene represented by the specified <see cref="GameObject"/>.
        /// </summary>
        /// <remarks>
        /// If the node doesn't exist yet, this method returns a future node that will be used for the scene.
        /// Update the <see cref="Hierarchy"/> before you query it about this node.
        /// </remarks>
        /// <param name="gameObject">The <see cref="GameObject"/> that represents the subscene.</param>
        /// <returns>The <see cref="HierarchyNode"/> for the specified subscene, or <see cref="HierarchyNode.Null"/> if the <see cref="GameObject"/> is null.</returns>
        public HierarchyNode GetOrCreateNode(GameObject gameObject) => gameObject != null ? GetOrCreateNodeFromEntityId(gameObject.GetEntityId()) : HierarchyNode.Null;

        /// <summary>
        /// Gets or creates the <see cref="HierarchyNode"/> for the specified subscene.
        /// </summary>
        /// <remarks>
        /// If the node doesn't exist yet, this method returns a future node that will be used for the scene.
        /// Update the <see cref="Hierarchy"/> before you query it about this node.
        /// </remarks>
        /// <param name="scene">The <see cref="Scene"/> associated with the subscene.</param>
        /// <returns>The <see cref="HierarchyNode"/> for the specified subscene.</returns>
        public HierarchyNode GetOrCreateNode(Scene scene) => GetOrCreateNodeFromScene(scene);

        /// <summary>
        /// Gets the <see cref="EntityId"/> of the <see cref="GameObject"/> that represents the subscene for the specified <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to get the <see cref="EntityId"/> for.</param>
        /// <returns>The <see cref="EntityId"/> of the <see cref="GameObject"/> that represents the subscene.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern EntityId GetEntityId(in HierarchyNode node);

        /// <summary>
        /// Gets the <see cref="GameObject"/> that represents the subscene for the specified <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to get the <see cref="GameObject"/> for.</param>
        /// <returns>The <see cref="GameObject"/> that represents the subscene.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern GameObject GetGameObject(in HierarchyNode node);

        /// <summary>
        /// Gets the <see cref="Scene"/> associated with the subscene for the specified <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to get the <see cref="Scene"/> for.</param>
        /// <returns>The <see cref="Scene"/> associated with the subscene.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern Scene GetScene(in HierarchyNode node);

        /// <summary>
        /// Gets the <see cref="HierarchyNodeType"/> registered for this <see cref="HierarchySubSceneAuthoringHandler"/>.
        /// </summary>
        /// <returns>The <see cref="HierarchyNodeType"/> for this handler.</returns>
        public new HierarchyNodeType GetNodeType()
        {
            if (m_NodeType == HierarchyNodeType.Null)
                m_NodeType = new HierarchyNodeType(GetStaticNodeType());

            return m_NodeType;
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
            if (!Hierarchy.Exists(node))
                return node.ToString();

            var name = Hierarchy.GetName(in node);
            var scene = GetScene(in node);
            if (scene.IsValid())
            {
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
        bool IHierarchyEditorNodeTypeHandler.CanDoubleClick(HierarchyView view, in HierarchyNode node) => OnSubSceneDoubleClick != null;

        bool IHierarchyEditorNodeTypeHandler.OnDoubleClick(HierarchyView view, in HierarchyNode node)
        {
            var go = GetGameObject(in node);
            if (go == null || OnSubSceneDoubleClick == null)
                return false;

            OnSubSceneDoubleClick(go);
            return true;
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
            // For Sub Scenes GameObjects, have menu items for cut, paste and delete.
            // Not copy or duplicate, since multiple of the same Sub Scene is not supported anyway.
            if (item == null)
                return;

            var scene = GetScene(item.Node);
            BuildSubSceneContextMenu(view, item.Node, menu);
            menu.AppendSeparator();

            if (scene.IsValid())
            {
                // Sub scenes where the scene object exists can reuse menu for regular scenes.
                HierarchySceneHandler.BuildSceneContextMenu(menu, scene, HierarchySceneHandler.GetSelectedScenes(view.ViewModel));
            }
            else
            {
                // Sub scenes where only the info exists, but not the scene object, need special handling.
                using var poolHandle = GenericMenu.Pool.Get(out var genericMenu);
                SubSceneGUI.CreateClosedSubSceneContextClick(genericMenu, new SceneHierarchyHooks.SubSceneInfo() { sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) });
                menu.AppendFromGenericMenu(genericMenu);
            }

            // Let users add extra items.
            using var poolHandle2 = GenericMenu.Pool.Get(out var genericMenu2);
            AddCustomSubSceneHeaderContextMenuItems(genericMenu2, scene);
            menu.AppendFromGenericMenu(genericMenu2);
        }

        bool IHierarchyEditorNodeTypeHandler.AcceptParent(HierarchyView view, in HierarchyNode parent)
        {
            var sceneNodeType = Hierarchy.GetNodeType<HierarchySceneHandler>();
            var parentNodeType = view.ViewModel.GetNodeType(in parent);
            return parentNodeType == sceneNodeType;
        }

        bool IHierarchyEditorNodeTypeHandler.AcceptChild(HierarchyView view, in HierarchyNode child)
        {
            var gameObjectNodeType = Hierarchy.GetNodeType<HierarchyGameObjectHandler>();
            var childNodeType = view.ViewModel.GetNodeType(in child);
            return childNodeType == gameObjectNodeType;
        }

        bool IHierarchyEditorNodeTypeHandler.CanStartDrag(HierarchyView view, ReadOnlySpan<HierarchyNode> nodes) => true;

        string IHierarchyEditorNodeTypeHandler.GetDragTitle(HierarchyView view, in HierarchyNode node)
        {
            var go = GetGameObject(in node);
            return go != null ? ObjectNames.GetDragAndDropTitle(go) : null;
        }

        void IHierarchyEditorNodeTypeHandler.OnStartDrag(in HierarchyViewDragAndDropSetupData data)
        {
            // If there's a GameObject in the selection, the drag and drop operation is
            // handled by the GameObject handler instead of the SubScene handler,
            // so we shouldn't populate entity IDs for this drag and drop.
            if (HasGameObjectInSelection(data.View.ViewModel))
                return;

            var nodeSpan = data.Nodes;
            for (var i = 0; i < nodeSpan.Length; ++i)
            {
                var node = nodeSpan[i];
                if (node == HierarchyNode.Null)
                    continue;
                var entityId = GetEntityId(in node);
                if (entityId == EntityId.None)
                    continue;
                data.EntityIds.Add(entityId);
            }
        }

        // SubScene nodes are backed by GameObjects; the actual transform reparenting + sibling-index
        // update goes through HierarchyGameObjectHandler.OnReorder. For mixed SubScene+GO drags
        // GameObjectHandler is also participating and runs its own CanReorder/OnReorder - we skip
        // here to keep that single coordinated two-phase pass and avoid double-execution.
        DragVisualMode IHierarchyEditorNodeTypeHandler.CanReorder(in HierarchyViewDragAndDropHandlingData data)
        {
            if (HasGameObjectInSelection(data.View.ViewModel))
                return DragVisualMode.None;

            if (Hierarchy.GetOrCreateNodeTypeHandler<HierarchyGameObjectHandler>() is IHierarchyEditorNodeTypeHandler handler)
                return handler.CanReorder(in data);

            // If there is no GameObject handler, we shouldn't allow reordering of SubScene nodes,
            // because the necessary transform and sibling-index updates won't happen.
            return DragVisualMode.Rejected;
        }

        void IHierarchyEditorNodeTypeHandler.OnReorder(in HierarchyViewDragAndDropHandlingData data)
        {
            if (HasGameObjectInSelection(data.View.ViewModel))
                return;

            if (Hierarchy.GetOrCreateNodeTypeHandler<HierarchyGameObjectHandler>() is IHierarchyEditorNodeTypeHandler handler)
                handler.OnReorder(in data);
        }

        DragVisualMode IHierarchyEditorNodeTypeHandler.CanAcceptDrop(in HierarchyViewDragAndDropHandlingData data) => DragVisualMode.None;

        DragVisualMode IHierarchyEditorNodeTypeHandler.OnAcceptDrop(in HierarchyViewDragAndDropHandlingData data) => DragVisualMode.None;
        #endregion

        protected override void OnBindItem(HierarchyViewItem item)
            => item.AddToClassList(k_SubSceneNodeUssClass);

        void BuildSubSceneContextMenu(HierarchyView view, in HierarchyNode node, DropdownMenu menu)
        {
            menu.AppendAction(L10n.Tr("Cut"), _ => ClipboardUtility.CutGO());
            menu.AppendAction(L10n.Tr("Paste"), _ => ClipboardUtility.PasteGO(null),
                CutBoard.CanGameObjectsBePasted() || Unsupported.CanPasteGameObjectsFromPasteboard()
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            menu.AppendAction(L10n.Tr("Paste As Child"), _ => ClipboardUtility.PasteGOAsChild(),
                ClipboardUtility.CanPasteAsChild()
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            menu.AppendAction(L10n.Tr("Delete"), _ => Unsupported.DeleteGameObjectSelection());

            menu.AppendSeparator();

            menu.AppendAction(L10n.Tr("Reimport"), _ => InvokeForSelectedSubScenes(ReimportSubScene));

            var go = GetGameObject(in node);
            var isOpen = IsSubSceneOpen?.Invoke(go) ?? false;
            if (isOpen)
                menu.AppendAction(L10n.Tr("Close"), _ => InvokeForSelectedSubScenes(CloseSubScene));
            else
                menu.AppendAction(L10n.Tr("Open"), _ => InvokeForSelectedSubScenes(OpenSubScene));
        }

        static void InvokeForSelectedSubScenes(Action<GameObject> action)
        {
            if (action == null)
                return;

            foreach (var go in Selection.gameObjects)
                action(go);
        }

        static void AddCustomSubSceneHeaderContextMenuItems(GenericMenu menu, Scene subScene)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(subScene.path);
            SceneHierarchyHooks.AddCustomSubSceneHeaderContextMenuItems(menu, new SceneHierarchyHooks.SubSceneInfo()
            {
                scene = subScene,
                sceneAsset = sceneAsset,
                sceneName = sceneAsset ? subScene.name : string.Empty
            });
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
                    if (!scene.IsValid() || !scene.isSubScene)
                        continue;

                    var sceneNode = GetOrCreateNode(scene);
                    if (sceneNode != HierarchyNode.Null)
                        CommandList.SetName(in sceneNode, scene.name);
                }
            }
        }
        void OnSceneDirty(Scene scene)
        {
            if (!scene.isSubScene)
                return;

            // Force an update even if nothing has changed.
            CommandList.SetDirty();
        }

        void OnSceneSaved(Scene scene)
        {
            if (!scene.isSubScene)
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

        bool HasGameObjectInSelection(HierarchyViewModel viewModel)
        {
            var goNodeType = Hierarchy.GetNodeType<HierarchyGameObjectHandler>();
            foreach (ref readonly var node in viewModel.EnumerateNodesWithFlags(HierarchyNodeFlags.Selected))
            {
                if (viewModel.GetNodeType(in node) == goNodeType)
                    return true;
            }
            return false;
        }

        [FreeFunction("HierarchySubSceneAuthoringHandlerBindings::GetStaticNodeType", IsThreadSafe = true)]
        static extern int GetStaticNodeType();

        [FreeFunction("HierarchySubSceneAuthoringHandlerBindings::GetOrCreateNodeFromEntityId", HasExplicitThis = true, IsThreadSafe = true)]
        extern HierarchyNode GetOrCreateNodeFromEntityId(EntityId entityId);

        [FreeFunction("HierarchySubSceneAuthoringHandlerBindings::GetOrCreateNodeFromScene", HasExplicitThis = true, IsThreadSafe = true)]
        extern HierarchyNode GetOrCreateNodeFromScene(Scene scene);

        #region Called from native
        [RequiredByNativeCode(Optional = true)]
        static IntPtr CreateSubSceneHandler(IntPtr nativePtr, IntPtr hierarchyPtr, IntPtr cmdListPtr)
        {
            if (nativePtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(nativePtr));
            if (hierarchyPtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(hierarchyPtr));
            if (cmdListPtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(cmdListPtr));

            var handler = new HierarchySubSceneAuthoringHandler(nativePtr,
                (Hierarchy)GCHandle.FromIntPtr(hierarchyPtr).Target,
                (HierarchyCommandList)GCHandle.FromIntPtr(cmdListPtr).Target);
            handler.Initialize();
            return GCHandle.ToIntPtr(GCHandle.Alloc(handler));
        }
        #endregion
    }
}
