// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
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
    /// The hierarchy node type handler for sub scenes.
    /// </summary>
    [RequiredByNativeCode(GenerateProxy = true, Optional = true), StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/HierarchyEditor/Public/HierarchySubSceneHandler.h")]
    [NativeHeader("Modules/HierarchyEditor/HierarchySubSceneHandlerBindings.h")]
    internal sealed partial class HierarchySubSceneHandler : HierarchyNodeTypeHandler, IHierarchyEntityIdConverter
    {
        const string k_SubSceneNodeUssClass = "hierarchy-item__scene-node";

        internal new static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(HierarchySubSceneHandler handler) => handler.m_Ptr;
        }

        class ExcludeFromBindings
        {
            public HierarchyNodeType NodeType;
        }

        ExcludeFromBindings m_State = new();

        HierarchySubSceneHandler()
        {
            throw new NotSupportedException();
        }

        HierarchySubSceneHandler(IntPtr nativePtr, Hierarchy hierarchy, HierarchyCommandList cmdList) : base(nativePtr, hierarchy, cmdList)
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
        /// Get a node display name. Default is the node name property.
        /// </summary>
        /// <param name="view">The parent <see cref="HierarchyView"/>.</param>
        /// <param name="node">The <see cref="HierarchyNode"/>.</param>
        /// <returns>Display name</returns>
        public override string GetDisplayName(HierarchyView view, in HierarchyNode node)
        {
            var name = base.GetDisplayName(view, node);
            var scene = GetScene(node);
            if (scene.IsValid())
            {
                if (scene.isDirty)
                    name += "*";
            }
            return name;
        }

        /// <summary>
        /// Gets or creates the hierarchy node corresponding to the given entity id representing the subscene.
        /// </summary>
        /// <remarks>
        /// If the node hasn't been created yet, returns the future node that will be used for the scene.
        /// An update of the hierarchy will be necessary if you intend to query the hieararchy about this node.
        /// </remarks>
        /// <param name="entityId">The entity id.</param>
        /// <returns>An hierarchy node.</returns>
        public HierarchyNode GetOrCreateNode(EntityId entityId) => GetOrCreateNodeFromEntityId(entityId);

        /// <summary>
        /// Gets or creates the hierarchy node corresponding to the given GameObject representing the subscene.
        /// </summary>
        /// <remarks>
        /// If the node hasn't been created yet, returns the future node that will be used for the scene.
        /// An update of the hierarchy will be necessary if you intend to query the hieararchy about this node.
        /// </remarks>
        /// <param name="gameObject">The game object.</param>
        /// <returns>An hierarchy node.</returns>
        public HierarchyNode GetOrCreateNode(GameObject gameObject) => gameObject != null ? GetOrCreateNodeFromEntityId(gameObject.GetEntityId()) : HierarchyNode.Null;

        /// <summary>
        /// Gets or creates the hierarchy node corresponding to the given scene associated with the subscene.
        /// </summary>
        /// <remarks>
        /// If the node hasn't been created yet, returns the future node that will be used for the scene.
        /// An update of the hierarchy will be necessary if you intend to query the hieararchy about this node.
        /// </remarks>
        /// <param name="scene">The scene.</param>
        /// <returns>An hierarchy node.</returns>
        public HierarchyNode GetOrCreateNode(Scene scene) => GetOrCreateNodeFromScene(scene);

        /// <summary>
        /// Gets the EntityId of the GameObject representing the subscene corresponding to the given hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>An EntityId.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern EntityId GetEntityId(in HierarchyNode node);

        /// <summary>
        /// Gets the GameObject representing the subscene corresponding to the given hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>A game object.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern GameObject GetGameObject(in HierarchyNode node);

        /// <summary>
        /// Gets the scene associated with the subscene corresponding to the given hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>A scene.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern Scene GetScene(in HierarchyNode node);

        /// <summary>
        /// Retrieves the hierarchy node type for this hierarchy node type handler.
        /// </summary>
        /// <returns>The type of the hierarchy node.</returns>
        public new HierarchyNodeType GetNodeType()
        {
            if (m_State.NodeType == HierarchyNodeType.Null)
                m_State.NodeType = new HierarchyNodeType(GetStaticNodeType());

            return m_State.NodeType;
        }

        /// <summary>
        /// Determines if the nodes of this hierarchy node type handler accepts the given node as parent.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <param name="parent">The hierarchy parent node.</param>
        /// <returns><see langword="true"/> if the node can be set as a parent, <see langword="false"/> otherwise.</returns>
        public override bool AcceptParent(HierarchyView view, in HierarchyNode parent)
        {
            var sceneNodeType = Hierarchy.GetNodeType<HierarchySceneHandler>();
            var parentNodeType = Hierarchy.GetNodeType(in parent);
            return parentNodeType == sceneNodeType;
        }

        /// <summary>
        /// Determines if the nodes of this hierarchy node type handler accepts the given node as child.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <param name="child">The hierarchy child node.</param>
        /// <returns><see langword="true"/> if the node can be set as a child, <see langword="false"/> otherwise.</returns>
        public override bool AcceptChild(HierarchyView view, in HierarchyNode child)
        {
            var gameObjectNodeType = Hierarchy.GetNodeTypeHandler<HierarchyGameObjectHandler>();
            var childNodeType = Hierarchy.GetNodeTypeHandler(in child);
            return childNodeType == gameObjectNodeType;
        }

        /// <summary>
        /// Whether or not this hierarchy node type handler accepts renaming its nodes.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <param name="node">The <see cref="HierarchyNode"/>.</param>
        /// <returns><see langword="true"/> if nodes can be renamed, <see langword="false"/> otherwise.</returns>
        public override bool CanSetName(HierarchyView view, in HierarchyNode node) => false;

        /// <summary>
        /// Action to execute when starting a drag operation.
        /// Used to setup a drag operation with the specified nodes by populating the <see cref="HierarchyViewDragAndDropSetupData"/> container.
        /// The <see cref="HierarchyViewDragAndDropSetupData"/> container contains lists of <see cref="UnityEngine.Object"/> references and paths that can be populated to store information about the drag operation.
        /// </summary>
        /// <param name="data">Container holding the data needed to start a drag and drop operation. <see cref="HierarchyNodeTypeHandler"/>s can populate this container.</param>
        protected override void OnStartDrag(in HierarchyViewDragAndDropSetupData data)
        {
            var nodeSpan = data.Nodes;
            for (var i = 0; i < nodeSpan.Length; ++i)
            {
                var node = nodeSpan[i];
                if (node == HierarchyNode.Null || Hierarchy.GetNodeTypeHandler(in node) != this)
                    continue;
                var go = GetGameObject(in node);
                if (go == null)
                    continue;
                data.EntityIds.Add(go.GetEntityId());
            }
        }

        protected override void PopulateContextMenu(HierarchyView view, HierarchyViewItem item, DropdownMenu menu)
        {
            // For Sub Scenes GameObjects, have menu items for cut, paste and delete.
            // Not copy or duplicate, since multiple of the same Sub Scene is not supported anyway.
            if (item == null)
                return;

            var scene = GetScene(item.Node);
            BuildSubSceneContextMenu(view, menu);
            menu.AppendSeparator();

            if (scene.IsValid())
            {
                // Sub scenes where the scene object exists can reuse menu for regular scenes.
                HierarchySceneHandler.BuildSceneContextMenu(menu, scene);
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

        protected override void Bind(HierarchyViewItem item)
            => item.AddToClassList(k_SubSceneNodeUssClass);

        void BuildSubSceneContextMenu(HierarchyView view, DropdownMenu menu)
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

            menu.AppendSeparator();

            var gameObjectHandler = Hierarchy.GetNodeTypeHandler<HierarchyGameObjectHandler>();
            var customParentNode = gameObjectHandler?.GetCustomParentNode() ?? HierarchyNode.Null;
            menu.AppendAction(L10n.Tr("Delete GameObject"), _ => Unsupported.DeleteGameObjectSelection(),
                HierarchyViewSelectionExtension.IsChildOfSelectionOrSelected(view, in customParentNode)
                    ? DropdownMenuAction.Status.Disabled
                    : DropdownMenuAction.Status.Normal);
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

        HierarchyNode IHierarchyEntityIdConverter.GetNode(EntityId entityId) => GetNodeFromEntityId(entityId);

        void IHierarchyEntityIdConverter.GetNodes(ReadOnlySpan<EntityId> entityIds, Span<HierarchyNode> outNodes) => GetNodesFromEntityIds(entityIds, outNodes);

        EntityId IHierarchyEntityIdConverter.GetEntityId(in HierarchyNode node) => GetEntityIdFromNode(in node);

        void IHierarchyEntityIdConverter.GetEntityIds(ReadOnlySpan<HierarchyNode> nodes, Span<EntityId> outEntityIds) => GetEntityIdsFromNodes(nodes, outEntityIds);

        [FreeFunction("HierarchySubSceneHandlerBindings::GetStaticNodeType", IsThreadSafe = true)]
        static extern int GetStaticNodeType();

        [FreeFunction("HierarchySubSceneHandlerBindings::GetOrCreateNodeFromEntityId", HasExplicitThis = true, IsThreadSafe = true)]
        extern HierarchyNode GetOrCreateNodeFromEntityId(EntityId entityId);

        [FreeFunction("HierarchySubSceneHandlerBindings::GetOrCreateNodeFromScene", HasExplicitThis = true, IsThreadSafe = true)]
        extern HierarchyNode GetOrCreateNodeFromScene(Scene scene);

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
        static IntPtr CreateSubSceneHandler(IntPtr nativePtr, IntPtr hierarchyPtr, IntPtr cmdListPtr)
        {
            if (nativePtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(nativePtr));
            if (hierarchyPtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(hierarchyPtr));
            if (cmdListPtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(cmdListPtr));

            var handler = new HierarchySubSceneHandler(nativePtr,
                (Hierarchy)GCHandle.FromIntPtr(hierarchyPtr).Target,
                (HierarchyCommandList)GCHandle.FromIntPtr(cmdListPtr).Target);
            handler.Initialize();
            return GCHandle.ToIntPtr(GCHandle.Alloc(handler));
        }
        #endregion
    }
}
