// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditor.SceneManagement;
using UnityEditor.Search;
using UnityEditor.Search.Providers;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Hierarchy.Editor
{
    /// <summary>
    /// Provides a hierarchy node type handler for <see cref="GameObject"/> instances in a <see cref="HierarchyView"/>.
    /// </summary>
    [RequiredByNativeCode(Optional = true), StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/HierarchyEditor/Public/HierarchyGameObjectHandler.h")]
    [NativeHeader("Modules/HierarchyEditor/HierarchyGameObjectHandlerBindings.h")]
    public sealed class HierarchyGameObjectHandler :
        HierarchyNodeTypeHandler,
        IHierarchyEditorNodeTypeHandler,
        IHierarchySearchPropositionProvider,
        IHierarchyExtendCreateMenu
    {
        static readonly UniqueStyleString k_GameObjectUssClass = new("hierarchy-item__gameobject-node");
        static readonly UniqueStyleString k_GameObjectDisabledUssClass = new("unity-disabled");
        static readonly UniqueStyleString k_GameObjectDefaultParentUssClass = new("hierarchy-item__gameobject-default-parent");

        [NoAutoStaticsCleanup] // constant lookup set for search filter special-case names
        static readonly HashSet<string> k_SpecialTypes = new HashSet<string>(new [] { "prefab" });

        internal new static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(HierarchyGameObjectHandler handler) => handler.m_Ptr;
        }

        HierarchyNodeType m_NodeType;
        HierarchyNodeType m_SubSceneNodeType;
        ParsedQuery<GameObject> m_ParsedQuery;
        SearchMonitorView m_SearchMonitorView;
        Transform m_CustomParentForNewGameObjects;
        SceneQueryEngine m_QueryEngine;

        SceneQueryEngine QueryEngine
        {
            get
            {
                if (m_QueryEngine == null)
                {
                    m_QueryEngine = new SceneQueryEngine(Array.Empty<GameObject>());
                    m_QueryEngine.SetupQueryEnginePropositions();
                }
                return m_QueryEngine;
            }
        }

        internal HierarchySearchQueryDescriptor CurrentFilter { get; set; }

        HierarchyGameObjectHandler()
        {
            throw new NotSupportedException();
        }

        HierarchyGameObjectHandler(IntPtr nativePtr, Hierarchy hierarchy, HierarchyCommandList cmdList) : base(nativePtr, hierarchy, cmdList)
        {
        }

        protected override void Initialize()
        {
            var currentStage = StageUtility.GetCurrentStage();
            m_CustomParentForNewGameObjects = currentStage is PrefabStage prefabStage ? prefabStage.prefabContentsRoot.transform : null;
        }

        /// <summary>
        /// Gets or creates the <see cref="HierarchyNode"/> that corresponds to the specified <see cref="GameObject"/> <see cref="EntityId"/>.
        /// </summary>
        /// <remarks>
        /// If the node doesn't exist, this method returns a node that represents the <see cref="GameObject"/> but that doesn't yet exist in the hierarchy.
        /// To query the <see cref="Hierarchy"/> about this node, update the hierarchy first.
        /// </remarks>
        /// <param name="entityId">The <see cref="EntityId"/> of the <see cref="GameObject"/> to get the <see cref="HierarchyNode"/> for.</param>
        /// <returns>The <see cref="HierarchyNode"/> that corresponds to the specified <see cref="EntityId"/>.</returns>
        public HierarchyNode GetOrCreateNode(EntityId entityId) => GetOrCreateNodeFromEntityId(entityId);

        /// <summary>
        /// Gets or creates the <see cref="HierarchyNode"/> that corresponds to the specified <see cref="GameObject"/>.
        /// </summary>
        /// <remarks>
        /// If the node doesn't exist, this method returns a node that represents the <see cref="GameObject"/> but that doesn't yet exist in the Hierarchy.
        /// To query the <see cref="Hierarchy"/> about this node, update the hierarchy first.
        /// </remarks>
        /// <param name="gameObject">The <see cref="GameObject"/> to get the <see cref="HierarchyNode"/> for.</param>
        /// <returns>The <see cref="HierarchyNode"/> that corresponds to the specified <see cref="GameObject"/>.</returns>
        public HierarchyNode GetOrCreateNode(GameObject gameObject) => gameObject != null ? GetOrCreateNodeFromEntityId(gameObject.GetEntityId()) : HierarchyNode.Null;

        /// <summary>
        /// Gets the <see cref="EntityId"/> of the <see cref="GameObject"/> that corresponds to the specified <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to get the <see cref="GameObject"/> <see cref="EntityId"/> for.</param>
        /// <returns>The <see cref="EntityId"/> of the <see cref="GameObject"/> that corresponds to the specified <see cref="HierarchyNode"/>.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern EntityId GetEntityId(in HierarchyNode node);

        /// <summary>
        /// Gets the <see cref="GameObject"/> that corresponds to the specified <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to get the <see cref="GameObject"/> for.</param>
        /// <returns>The <see cref="GameObject"/> that corresponds to the specified <see cref="HierarchyNode"/>.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern GameObject GetGameObject(in HierarchyNode node);

        /// <summary>
        /// Gets the <see cref="HierarchyNodeType"/> for this <see cref="HierarchyGameObjectHandler"/>.
        /// </summary>
        /// <returns>The <see cref="HierarchyNodeType"/> for <see cref="GameObject"/> nodes.</returns>
        public new HierarchyNodeType GetNodeType()
        {
            if (m_NodeType == HierarchyNodeType.Null)
                m_NodeType = new HierarchyNodeType(GetStaticNodeType());

            return m_NodeType;
        }

        HierarchyNodeType GetSubSceneNodeType()
        {
            if (m_SubSceneNodeType == HierarchyNodeType.Null)
                m_SubSceneNodeType = Hierarchy.GetNodeType<HierarchySubSceneAuthoringHandler>();

            return m_SubSceneNodeType;
        }

        protected override void OnBindItem(HierarchyViewItem item)
        {
            item.AddToClassList(k_GameObjectUssClass);

            var gameObject = GetGameObject(in item.Node);
            if (gameObject == null)
                return;

            var disabled = (gameObject.hideFlags & HideFlags.NotEditable) != 0 || !gameObject.activeInHierarchy;
            item.EnableInClassList(k_GameObjectDisabledUssClass, disabled);

            var scene = gameObject.scene;
            var isActiveScene = EditorSceneManager.GetActiveScene().guid == scene.guid;
            var isDefaultParent = isActiveScene && gameObject.GetEntityId() == scene.defaultParent;
            item.EnableInClassList(k_GameObjectDefaultParentUssClass, isDefaultParent);

            GameObjectIconUtility.SetNodeIconForObject(item, gameObject);

            if (PrefabUtility.ShowPrefabModeButton(gameObject))
                HierarchyViewPrefabStyleUtility.SetNavigationButton(gameObject, item);
            else
                HierarchyViewPrefabStyleUtility.ClearNavigationButton(item);
        }

        protected override void OnUnbindItem(HierarchyViewItem item)
        {
            HierarchyViewPrefabStyleUtility.ClearNavigationButton(item);
        }

        void IHierarchyExtendCreateMenu.PopulateCreateMenu(DropdownMenu menu)
        {
            PopulateCreateMenu(menu, null, null);
        }

        #region IHierarchyEditorNodeTypeHandler
        bool IHierarchyEditorNodeTypeHandler.CanCut(HierarchyView view)
        {
            return AllowCutCopyAndDuplicate(view);
        }

        bool IHierarchyEditorNodeTypeHandler.OnCut(HierarchyView view)
        {
            ClipboardUtility.CutGO();
            return true;
        }

        bool IHierarchyEditorNodeTypeHandler.CanCopy(HierarchyView view)
        {
            return AllowCutCopyAndDuplicate(view);
        }

        bool IHierarchyEditorNodeTypeHandler.OnCopy(HierarchyView view)
        {
            ClipboardUtility.CopyGO();
            return true;
        }

        bool IHierarchyEditorNodeTypeHandler.CanPaste(HierarchyView view)
        {
            return CutBoard.CanGameObjectsBePasted() || Unsupported.CanPasteGameObjectsFromPasteboard();
        }

        bool IHierarchyEditorNodeTypeHandler.OnPaste(HierarchyView view)
        {
            ClipboardUtility.PasteGO(m_CustomParentForNewGameObjects);
            return true;
        }

        bool IHierarchyEditorNodeTypeHandler.CanPasteAsChild(HierarchyView view)
        {
            return ClipboardUtility.CanPasteAsChild();
        }

        bool IHierarchyEditorNodeTypeHandler.OnPasteAsChild(HierarchyView view, bool keepWorldPos)
        {
            ClipboardUtility.PasteGOAsChild(keepWorldPos);
            return true;
        }

        bool IHierarchyEditorNodeTypeHandler.CanSetName(HierarchyView view, in HierarchyNode node)
        {
            if (node == HierarchyNode.Null) return false;

            var gameObject = GetGameObject(in node);
            if ((gameObject.hideFlags & HideFlags.NotEditable) != 0)
                return false;

            return true;
        }

        bool IHierarchyEditorNodeTypeHandler.OnSetName(HierarchyView view, in HierarchyNode node, string name)
        {
            var go = GetGameObject(in node);
            if (go == null)
                return false;

            Undo.RecordObject(go, "Rename");
            go.name = name;
            return true;
        }

        string IHierarchyEditorNodeTypeHandler.GetDisplayName(HierarchyView view, in HierarchyNode node)
        {
            return Hierarchy.Exists(in node) ? Hierarchy.GetName(in node) : node.ToString();
        }

        bool IHierarchyEditorNodeTypeHandler.CanDuplicate(HierarchyView view)
        {
            return AllowCutCopyAndDuplicate(view);
        }

        bool IHierarchyEditorNodeTypeHandler.OnDuplicate(HierarchyView view)
        {
            ClipboardUtility.DuplicateGO(m_CustomParentForNewGameObjects);
            return true;
        }

        bool IHierarchyEditorNodeTypeHandler.CanDelete(HierarchyView view)
        {
            if (m_CustomParentForNewGameObjects == null)
                return true;

            // In prefab stage, CustomParentForNewGameObjects and it's ancestors cannot be deleted.
            var node = GetOrCreateNode(m_CustomParentForNewGameObjects.gameObject);
            return !view.IsSelectedOrAnyAncestorSelected(in node);
        }

        bool IHierarchyEditorNodeTypeHandler.OnDelete(HierarchyView view)
        {
            Unsupported.DeleteGameObjectSelection();
            return true;
        }

        bool IHierarchyEditorNodeTypeHandler.CanFindReferences(HierarchyView view) => true;

        bool IHierarchyEditorNodeTypeHandler.OnFindReferences(HierarchyView view)
        {
            if (!Selection.activeGameObject)
                return false;

            SearchableEditorWindow.SearchForReferencesToInstanceID(Selection.activeGameObject.GetEntityId());
            return true;
        }

        bool IHierarchyEditorNodeTypeHandler.CanDoubleClick(HierarchyView view, in HierarchyNode node) => true;

        bool IHierarchyEditorNodeTypeHandler.OnDoubleClick(HierarchyView view, in HierarchyNode node)
        {
            return GetGameObject(in node) != null && SceneView.lastActiveSceneView?.FrameSelected() == true;
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
            HierarchyWindowContextMenuUtility.PopulateCommonContextMenuItems(view, item?.Node ?? HierarchyNode.Null, this, menu);
            BuildGameObjectContextMenu(view, item?.Node ?? HierarchyNode.Null, GetGameObject(item?.Node ?? HierarchyNode.Null), menu);
        }

        bool IHierarchyEditorNodeTypeHandler.AcceptParent(HierarchyView view, in HierarchyNode parent)
        {
            var gameObjectNodeType = GetNodeType();
            var sceneNodeType = Hierarchy.GetNodeType<HierarchySceneHandler>();
            var parentNodeType = view.ViewModel.GetNodeType(in parent);
            // SubScene drops are excluded: they need scene loading checks and are handled
            // by the external drop path (DropOnHierarchyWindow) via OnAcceptDrop.
            return parentNodeType == gameObjectNodeType || parentNodeType == sceneNodeType;
        }

        bool IHierarchyEditorNodeTypeHandler.AcceptChild(HierarchyView view, in HierarchyNode child)
        {
            var sceneNodeType = Hierarchy.GetNodeType<HierarchySceneHandler>();
            var subSceneNodeType = Hierarchy.GetNodeType<HierarchySubSceneAuthoringHandler>();
            var childNodeType = view.ViewModel.GetNodeType(in child);
            return childNodeType != sceneNodeType && childNodeType != subSceneNodeType;
        }

        bool IHierarchyEditorNodeTypeHandler.CanStartDrag(HierarchyView view, ReadOnlySpan<HierarchyNode> nodes) => CanStartDrag(nodes);

        string IHierarchyEditorNodeTypeHandler.GetDragTitle(HierarchyView view, in HierarchyNode node)
        {
            var go = GetGameObject(in node);
            return go != null ? ObjectNames.GetDragAndDropTitle(go) : null;
        }

        void IHierarchyEditorNodeTypeHandler.OnStartDrag(in HierarchyViewDragAndDropSetupData data)
        {
            // DropOnHierarchyWindow processes DragAndDrop.objectReferences in OnStartDrag order,
            // not in the Hierarchy's selection order. For multi-item drops this can
            // place dragged GameObjects/SubScenes in the wrong relative order under their new parent.
            // To work around this, we push the dragged nodes' GameObjects/SubScenes into DragAndDrop.objectReferences
            // in the correct order here, so DropOnHierarchyWindow gets them right.
            var gameObjectNodeType = GetNodeType();
            var subSceneNodeType = Hierarchy.GetNodeType<HierarchySubSceneAuthoringHandler>();
            var subSceneHandler = Hierarchy.GetOrCreateNodeTypeHandler<HierarchySubSceneAuthoringHandler>();
            foreach (ref readonly var node in data.View.ViewModel.EnumerateNodesWithFlags(HierarchyNodeFlags.Selected))
            {
                if (node == HierarchyNode.Null)
                    continue;

                EntityId nodeEntityId = EntityId.None;
                var nodeType = data.View.ViewModel.GetNodeType(in node);
                if (nodeType == gameObjectNodeType)
                    nodeEntityId = GetEntityId(in node);
                else if (nodeType == subSceneNodeType)
                    nodeEntityId = subSceneHandler.GetEntityId(in node);

                if (nodeEntityId == EntityId.None)
                    continue;

                data.EntityIds.Add(nodeEntityId);
            }
        }

        DragVisualMode IHierarchyEditorNodeTypeHandler.CanReorder(in HierarchyViewDragAndDropHandlingData data)
        {
            // Only handle in stages where GameObjects exist.
            if (StageNavigationManager.instance.currentStage is not (MainStage or PrefabStage))
                return DragVisualMode.None;

            var parent = data.Parent;
            var view = data.View;
            var viewModel = data.View.ViewModel;

            // In PrefabStage, reject drops that would place objects as siblings of the prefab root.
            if (StageNavigationManager.instance.currentStage is PrefabStage prefabStage)
            {
                var prefabRootNode = GetOrCreateNode(prefabStage.prefabContentsRoot);
                if (data.DropPosition == DragAndDropPosition.OutsideItems && parent != prefabRootNode)
                    parent = prefabRootNode;

                var prefabRootDepth = viewModel.GetDepth(in prefabRootNode);
                var parentDepth = viewModel.GetDepth(in parent);
                if (parent != prefabRootNode && parentDepth <= prefabRootDepth)
                    return DragVisualMode.Rejected;
            }

            if (parent == HierarchyNode.Null)
                return DragVisualMode.None;

            ResolveSiblingTarget(data, skipSelected: true, out var target, out var flags);

            if (view.Filtering)
                flags |= HierarchyDropFlags.SearchActive;

            var targetNodeType = viewModel.GetNodeType(in target);
            var entityId = GetDropTargetEntityId(in target, in targetNodeType, flags);
            if (entityId == EntityId.None)
                return DragVisualMode.None;

            var visualMode = DragAndDrop.DropOnHierarchyWindow(entityId, flags, null, perform: false);
            return DragAndDropHelpers.ConvertDragAndDropVisualModeToDragVisualMode(visualMode);
        }

        void IHierarchyEditorNodeTypeHandler.OnReorder(in HierarchyViewDragAndDropHandlingData data)
        {
            var parent = data.Parent;
            if (parent == HierarchyNode.Null)
                return;

            var view = data.View;
            var viewModel = view.ViewModel;

            ResolveSiblingTarget(data, skipSelected: true, out var target, out var flags);

            if (view.Filtering)
                flags |= HierarchyDropFlags.SearchActive;

            var targetNodeType = viewModel.GetNodeType(in target);
            var entityId = GetDropTargetEntityId(in target, in targetNodeType, flags);
            if (entityId == EntityId.None)
                return;

            DragAndDrop.DropOnHierarchyWindow(entityId, flags, null, perform: true);
        }

        DragVisualMode IHierarchyEditorNodeTypeHandler.CanAcceptDrop(in HierarchyViewDragAndDropHandlingData data)
        {
            return DoHandleDrop(in data, false);
        }

        DragVisualMode IHierarchyEditorNodeTypeHandler.OnAcceptDrop(in HierarchyViewDragAndDropHandlingData data)
        {
            return DoHandleDrop(in data, true);
        }
        #endregion

        void BuildGameObjectContextMenu(HierarchyView view, in HierarchyNode node, GameObject gameObject, DropdownMenu menu)
        {
            // Set as Default Parent
            if (!view.ViewModel.HasFlags(HierarchyNodeFlags.Selected) && gameObject == null || gameObject && (gameObject.name == PrefabUtility.kDummyPrefabStageRootObjectName || PrefabStageUtility.IsGameObjectThePrefabRootInAnyPrefabStage(gameObject)))
            {
                menu.AppendAction(L10n.Tr("Set as Default Parent"), _ => SetAsDefaultParent(view), DropdownMenuAction.Status.Disabled);
            }
            else if (gameObject != null && (gameObject.GetEntityId() != gameObject.scene.defaultParent || EditorSceneManager.GetActiveScene().guid != gameObject.scene.guid))
            {
                menu.AppendAction(L10n.Tr("Set as Default Parent"), _ => SetAsDefaultParent(view));
            }
            else
            {
                menu.AppendAction(L10n.Tr("Clear Default Parent"), _ => ClearDefaultParent(view));
            }

            BuildPrefabContextMenu(view, menu, gameObject);

            using var _ = ListPool<GameObject>.Get(out var selectedGameObjects);
            GetSelectedGameObjects(selectedGameObjects);

            // All Create GameObject menu items
            {
                menu.AppendSeparator();
                PopulateCreateMenu(menu, gameObject, selectedGameObjects.ToArray());
            }

            using var poolHandle = GenericMenu.Pool.Get(out var genericMenu);
            SceneHierarchyHooks.AddCustomGameObjectContextMenuItems(genericMenu, gameObject);
            menu.AppendFromGenericMenu(genericMenu);

            if (selectedGameObjects.Count > 0)
            {
                menu.AppendSeparator();
                menu.AppendAction(L10n.Tr("Properties..."), _ => PropertyEditor.OpenPropertyEditorOnSelection());
            }
        }

        void GetSelectedGameObjects(List<GameObject> selectedGameObjects)
        {
            foreach (var t in Selection.transforms)
            {
                if (t.gameObject != null)
                    selectedGameObjects.Add(t.gameObject);
            }
        }

        void PopulateCreateMenu(DropdownMenu menu, GameObject gameObject, GameObject[] selectedGameObjects)
        {
            var customParent = m_CustomParentForNewGameObjects;
            var targetSceneForCreation = customParent != null ? customParent.gameObject.scene.handle : SceneHandle.None;

            // When right-clicking on a GameObject, set the context to the current selection so created GameObjects are added as children.
            // When right-clicking on blank space, pass no context so created GameObjects become root objects.
            // Sets includeCreateEmptyChild to false when right-clicking on blank space.
            MenuUtilsForHierarchyWindow.AddCreateGameObjectItemsToMenu(menu,
                                           gameObject != null ? selectedGameObjects : Array.Empty<GameObject>(),
                                           gameObject != null,
                                           false,
                                           false,
                                           targetSceneForCreation,
                                           gameObject == null ? MenuUtils.ContextMenuOrigin.None : MenuUtils.ContextMenuOrigin.GameObject);
        }

        bool AllowCutCopyAndDuplicate(HierarchyView view)
        {
            var customParent = m_CustomParentForNewGameObjects;
            if (customParent == null)
                return true;

            var customParentNode = GetOrCreateNode(customParent.gameObject);

            // In Prefab stage, CustomParentForNewGameObjects's ancestors cannot be cut, copied nor duplicated.
            return customParentNode != HierarchyNode.Null && !view.IsSelectedOrAnyAncestorSelected(view.ViewModel.GetParent(in customParentNode));
        }

        bool IsSelectPrefabRootAvailable(HierarchyView view)
        {
            foreach (ref readonly var selectedNode in view.ViewModel.EnumerateNodesWithFlags(HierarchyNodeFlags.Selected))
            {
                var go = GetGameObject(in selectedNode);
                if (go == null)
                    continue;

                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                if (root != null)
                    return true;
            }
            return false;
        }

        internal void SelectPrefabRoot(HierarchyView view)
        {
            List<HierarchyNode> rootNodes = new();

            foreach (ref readonly var selectedNode in view.ViewModel.EnumerateNodesWithFlags(HierarchyNodeFlags.Selected))
            {
                var go = GetGameObject(in selectedNode);
                if (go != null && PrefabUtility.IsPartOfAnyPrefab(go))
                {
                    var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                    if (root != null)
                    {
                        var node = GetOrCreateNode(root);
                        if (node != HierarchyNode.Null)
                            rootNodes.Add(node);
                    }
                }
            }

            if (rootNodes.Count > 0)
            {
                view.SetSelection(rootNodes.ToArray());
            }
            else
            {
                view.DeselectAll();
            }
        }

        void SetAsDefaultParent(HierarchyView view)
        {
            SceneHierarchy.SetDefaultParentObject(false);
            CommandList.SetDirty();
        }

        void ClearDefaultParent(HierarchyView view)
        {
            SceneHierarchy.ClearDefaultParentObject();
            CommandList.SetDirty();
        }

        void BuildPrefabContextMenu(HierarchyView view, DropdownMenu menu, GameObject gameObject)
        {
            if (gameObject == null)
                return;

            string assetPath = null;
            GameObject prefabAsset = null;

            if (view.ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected) == 1)
            {
                assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
                prefabAsset = (GameObject)AssetDatabase.LoadMainAssetAtPath(assetPath);
            }

            var isAssetPathValid = !string.IsNullOrEmpty(assetPath);
            if (gameObject != null || isAssetPathValid)
                menu.AppendSeparator();

            if (isAssetPathValid)
            {
                if (PrefabUtility.IsPartOfModelPrefab(prefabAsset))
                {
                    menu.AppendAction(L10n.Tr("Prefab/Open Model"), _ =>
                    {
                        AssetDatabase.OpenAsset(prefabAsset);
                    });
                }
                else
                {
                    menu.AppendAction(L10n.Tr("Prefab/Open Asset in Context"), _ =>
                    {
                        PrefabStageUtility.OpenPrefab(assetPath, gameObject, PrefabStage.Mode.InContext, StageNavigationManager.Analytics.ChangeType.EnterViaInstanceHierarchyContextMenu);
                    });
                    menu.AppendAction(L10n.Tr("Prefab/Open Asset in Isolation"), _ =>
                    {
                        PrefabStageUtility.OpenPrefab(assetPath, gameObject, PrefabStage.Mode.InIsolation, StageNavigationManager.Analytics.ChangeType.EnterViaInstanceHierarchyContextMenu);
                    });
                }

                menu.AppendSeparator("Prefab/");
                menu.AppendAction(L10n.Tr("Prefab/Select Asset"), _ =>
                {
                    Selection.activeObject = prefabAsset;
                    EditorGUIUtility.PingObject(prefabAsset.GetEntityId());
                });
            }

            if (IsSelectPrefabRootAvailable(view))
            {
                menu.AppendAction(L10n.Tr("Prefab/Select Root"), _ => SelectPrefabRoot(view));
            }

            GameObject sourceRoot = PrefabUtility.GetSourceRootWhereGameObjectIsAddedAsOverride(gameObject);
            if (sourceRoot != null)
            {
                var s = PrefabUtility.GetOriginalSourceRootWhereGameObjectIsAdded(gameObject);
                menu.AppendAction(L10n.Tr("Prefab/Go to Added GameObject in '" + sourceRoot.name + "'"), _ =>
                {
                    PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(sourceRoot), PrefabUtility.GetNearestPrefabInstanceRoot(gameObject), PrefabStage.Mode.InIsolation);
                });
            }

            var selectedGOs = GetSelectedGameObjects(view);

            using var poolHandleForOverride = GenericMenu.Pool.Get(out var genericMenuForOverride);
            PrefabUtility.HandleAddedGameObjectOverridesMenuItems(genericMenuForOverride, selectedGOs);
            menu.AppendFromGenericMenu(genericMenuForOverride);

            if (gameObject != null)
            {
                menu.AppendSeparator("Prefab/");
                List<GameObject> listOfInstanceRoots;
                List<GameObject> listOfPlainGameObjects;
                PrefabReplaceUtility.FindGameObjectsToReplace(gameObject, out listOfPlainGameObjects, out listOfInstanceRoots);

                var multiselection = listOfInstanceRoots.Count > 1 || listOfPlainGameObjects.Count > 1;

                using var poolHandle = GenericMenu.Pool.Get(out var genericMenu);
                PrefabReplaceUtility.AddReplaceMenuItemsToMenuBasedOnCurrentSelection(genericMenu, "Prefab/", gameObject, listOfInstanceRoots, listOfPlainGameObjects, null);
                menu.AppendFromGenericMenu(genericMenu);
            }

            if (PrefabUtility.AnyOutermostPrefabRoots(selectedGOs))
            {
                menu.AppendSeparator("Prefab/");
                menu.AppendAction(L10n.Tr("Prefab/Unpack"), _ => UnpackPrefab(view, selectedGOs));
                menu.AppendAction(L10n.Tr("Prefab/Unpack Completely"), _ => UnpackPrefabCompletely(view, selectedGOs));
                menu.AppendSeparator("Prefab/");
                menu.AppendAction(L10n.Tr("Prefab/Remove Unused Overrides..."), _ => PrefabUtility.RemoveSelectedPrefabInstanceUnusedOverrides(selectedGOs));
            }
        }

        GameObject[] GetSelectedGameObjects(HierarchyView view)
        {
            var selectionCount = view.ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);

            using var selectedNodes = new RentSpanUnmanaged<HierarchyNode>(selectionCount);
            view.ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, selectedNodes);

            using var pooledObject = ListPool<GameObject>.Get(out var gameObjects);
            for (var i = 0; i < selectionCount; i++)
            {
                var go = GetGameObject(in selectedNodes.Span[i]);
                if (go != null)
                    gameObjects.Add(go);
            }

            return gameObjects.ToArray();
        }

        void UnpackPrefab(HierarchyView view, GameObject[] selectedGOs)
        {
            PrefabUtility.UnpackPrefab(selectedGOs);
            CommandList.SetDirty();
        }

        void UnpackPrefabCompletely(HierarchyView view, GameObject[] selectedGOs)
        {
            PrefabUtility.UnpackPrefabCompletely(selectedGOs);
            CommandList.SetDirty();
        }

        internal HierarchyNode GetCustomParentNode()
        {
            if (m_CustomParentForNewGameObjects == null)
                return HierarchyNode.Null;

            return GetOrCreateNode(m_CustomParentForNewGameObjects.gameObject);
        }

        protected override void SearchBegin(HierarchySearchQueryDescriptor query)
        {
            // We know all the filter have been processed natively.
            var nonNativeFilters = new List<HierarchySearchFilter>(query.Filters.Length);
            foreach (var f in query.Filters)
            {
                if (f.Name != "t" || k_SpecialTypes.Contains(f.Value))
                    nonNativeFilters.Add(f);
            }
            CurrentFilter = new HierarchySearchQueryDescriptor(nonNativeFilters.ToArray());
            var queryStr = CurrentFilter.BuildFilterQuery();
            m_ParsedQuery = QueryEngine.engine.ParseQuery(queryStr);
            // TODO Search: GetView needs to be per Window id.
            m_SearchMonitorView = SearchMonitor.GetView();
        }

        protected override bool SearchMatch(in HierarchyNode node)
        {
            if (CurrentFilter != null && CurrentFilter.IsEmpty)
            {
                // Filter is empty, accept anything.
                return true;
            }

            if (CurrentFilter.Invalid || !m_ParsedQuery.valid)
                return false;

            var go = GetGameObject(in node);
            return IsGameObjectSearchMatch(go);
        }

        protected override void SearchEnd()
        {
            m_SearchMonitorView.Dispose();
        }

        internal bool IsGameObjectSearchMatch(GameObject go)
        {
            return m_ParsedQuery.Test(go);
        }

        #region IHierarchySearchPropositionProvider
        IEnumerable<SearchProposition> IHierarchySearchPropositionProvider.FetchPropositions(HierarchyViewModel viewModel, SearchContext context, SearchPropositionOptions options)
        {
            using (new EditorPerformanceTracker($"{nameof(HierarchyGameObjectHandler)}.FetchPropositions"))
            {
                return SceneProvider.FetchQueryBuilderPropositions(QueryEngine, null);
            }
        }
        #endregion

        DragVisualMode DoHandleDrop(in HierarchyViewDragAndDropHandlingData data, bool perform)
        {
            // If we are dragging scenes, let the scene handler do it.
            if (DragAndDropHelpers.IsDraggingScene(data) || DragAndDropHelpers.IsDraggingEntity(data))
                return DragVisualMode.None;

            if (StageNavigationManager.instance.currentStage is PrefabStage)
            {
                var result = PrefabModeDraggingHandler(data, perform);
                if (result != DragVisualMode.None)
                    return result;
            }

            // Only handle GameObject drops in stages where GameObjects exist (MainStage, PrefabStage)
            // In other stages (e.g., VisualElementEditingStage), let the appropriate handler handle it
            if (StageNavigationManager.instance.currentStage is not (MainStage or PrefabStage))
                return DragVisualMode.None;

            var dropPosition = data.DropPosition;
            var searchActive = data.View.Filtering;
            var draggingUpon = dropPosition == DragAndDropPosition.OverItem;
            if (searchActive && !draggingUpon)
                return DragVisualMode.None;

            var option = searchActive ? HierarchyDropFlags.SearchActive : HierarchyDropFlags.None;

            DragAndDropVisualMode visualMode;
            if (dropPosition == DragAndDropPosition.OutsideItems)
            {
                if (m_CustomParentForNewGameObjects != null)
                {
                    // Use specific parent for DragAndDropForwarding
                    visualMode = DragAndDrop.DropOnHierarchyWindow(EntityId.None, option, m_CustomParentForNewGameObjects, perform);
                }
                else
                {
                    // Simulate drag upon the last loaded scene in the hierarchy (adds as last root sibling of the last scene)
                    Scene lastScene = GetLastScene();
                    if (!lastScene.IsValid())
                        return DragVisualMode.Rejected;

                    option |= HierarchyDropFlags.DropUpon;
                    visualMode = DragAndDrop.DropOnHierarchyWindow(lastScene.handle.ToEntityId(), option, null, perform);
                }

                return DragAndDropHelpers.ConvertDragAndDropVisualModeToDragVisualMode(visualMode);
            }

            var view = data.View;

            // Root-level drops aren't supported. This happens when scenes are collapsed, or when a
            // horizontal drag pushes the parent out to the root.
            if (data.Parent == HierarchyNode.Null || data.Parent == view.Source.Root)
                return DragVisualMode.Rejected;

            var isInternalReorder = data.Source == view.ListView;
            ResolveSiblingTarget(data, skipSelected: isInternalReorder, out var target, out var positionFlags);
            option = (searchActive ? HierarchyDropFlags.SearchActive : HierarchyDropFlags.None) | positionFlags;

            var targetNodeType = view.ViewModel.GetNodeType(in target);
            var entityId = GetDropTargetEntityId(in target, in targetNodeType, option);
            if (entityId == EntityId.None)
                return DragVisualMode.None;

            if (SubSceneManager.HasAnySubSceneRegistered && !IsValidSubSceneDropTarget(entityId, dropPosition, data.EntityIds))
                return DragVisualMode.Rejected;

            var go = EditorUtility.EntityIdToObject(entityId) as GameObject;
            if (go != null)
            {
                if (PrefabReplaceUtility.GetDragVisualModeAndShowMenuWithReplaceMenuItemsWhenNeeded(go, draggingUpon, perform, true, true, out visualMode))
                {
                    return DragAndDropHelpers.ConvertDragAndDropVisualModeToDragVisualMode(visualMode);
                }
            }

            if (perform && data.Parent != HierarchyNode.Null)
                SetPendingExternalDrop(data.Parent, data.ChildIndex);

            visualMode = DragAndDrop.DropOnHierarchyWindow(entityId, option, null, perform);

            return DragAndDropHelpers.ConvertDragAndDropVisualModeToDragVisualMode(visualMode);
        }

        static Scene GetLastScene()
        {
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
                if (SceneManager.GetSceneAt(i).isLoaded && !SceneManager.GetSceneAt(i).isSubScene)
                    return SceneManager.GetSceneAt(i);
            return new Scene();
        }

        EntityId GetDropTargetEntityId(in HierarchyNode dropTarget, in HierarchyNodeType dropTargetNodeType, HierarchyDropFlags dropOption)
        {
            if (!Hierarchy.Exists(dropTarget))
                return EntityId.None;

            if (dropTargetNodeType == Hierarchy.GetNodeType<HierarchySceneHandler>())
            {
                var sceneHandler = Hierarchy.GetNodeTypeHandlerBase<HierarchySceneHandler>();
                var scene = sceneHandler.GetScene(in dropTarget);
                return scene.IsValid() ? scene.handle.ToEntityId() : EntityId.None;
            }

            if (dropTargetNodeType == Hierarchy.GetNodeType<HierarchySubSceneAuthoringHandler>())
            {
                var subSceneHandler = Hierarchy.GetNodeTypeHandlerBase<HierarchySubSceneAuthoringHandler>();
                if (dropOption.HasFlag(HierarchyDropFlags.DropUpon))
                {
                    var scene = subSceneHandler.GetScene(in dropTarget);
                    if (scene.IsValid())
                        return scene.handle.ToEntityId();
                }
                var go = subSceneHandler.GetGameObject(in dropTarget);
                var goEntityId = go?.GetEntityId() ?? EntityId.None;
                return goEntityId;
            }

            if (dropTargetNodeType == Hierarchy.GetNodeType<HierarchyGameObjectHandler>())
                return GetEntityId(in dropTarget);

            return EntityId.None;
        }

        static bool IsValidSubSceneDropTarget(EntityId dropTargetGameObjectOrSceneEntityId, DragAndDropPosition dropPosition, IEnumerable<EntityId> draggedEntityIds)
        {
            if (draggedEntityIds == null)
                return false;

            Transform parentForDrop = GetTransformParentForDrop(dropTargetGameObjectOrSceneEntityId, dropPosition);
            if (parentForDrop == null)
            {
                // Drop is on a root scene which is always allowed
                return true;
            }

            // Check if we are trying to drop on a subscene node
            if (SubSceneManager.TryGetSubSceneDescription(parentForDrop.gameObject, out var description))
            {
                // Retrieve the underlying scene for the subscene node
                var scene = EditorSceneManager.FindSceneBySceneGUID(description.SceneGuid);
                if (scene == default || !scene.isLoaded)
                {
                    // Scene is not found or not loaded: drop is not allowed
                    return false;
                }
            }

            // Valid drop target for current dragged objects
            return true;
        }

        static Transform GetTransformParentForDrop(EntityId gameObjectOrSceneEntityId, DragAndDropPosition dropPosition)
        {
            var obj = EditorUtility.EntityIdToObject(gameObjectOrSceneEntityId);
            if (obj != null)
            {
                // Find transform parent from GameObject
                var go = obj as GameObject;
                if (go == null)
                    throw new InvalidOperationException("Unexpected UnityEngine.Object type in Hierarchy " + obj.GetType());

                switch (dropPosition)
                {
                    case DragAndDropPosition.OverItem:
                        return go.transform;

                    case DragAndDropPosition.BetweenItems:
                        if (go.transform.parent == null)
                        {
                            var subSceneInfo = SubSceneGUI.GetSubSceneInfo(go.scene);
                            if (subSceneInfo.isValid)
                                return subSceneInfo.transform;
                        }
                        return go.transform.parent;
                    case DragAndDropPosition.OutsideItems:
                        return null;
                    default:
                        throw new InvalidOperationException("Unhandled enum " + dropPosition);
                }
            }
            else
            {
                // Find transform parent from Scene
                var scene = EditorSceneManager.GetSceneByHandle(SceneHandle.From(gameObjectOrSceneEntityId));
                var subSceneInfo = SubSceneGUI.GetSubSceneInfo(scene);
                if (subSceneInfo.isValid)
                    return subSceneInfo.transform;
                return null; // root scene has no transform parent
            }
        }

        DragVisualMode PrefabModeDraggingHandler(in HierarchyViewDragAndDropHandlingData data, bool perform)
        {
            var prefabStage = StageNavigationManager.instance.currentStage as PrefabStage;
            if (prefabStage == null)
                throw new InvalidOperationException("PrefabModeDraggingHandler should only be called in Prefab Mode");

            var parent = data.Parent;
            var prefabRootNode = GetOrCreateNode(prefabStage.prefabContentsRoot);

            // When dropping outside items, treat it as dropping onto the prefab root
            if (data.DropPosition == DragAndDropPosition.OutsideItems && parent != prefabRootNode)
            {
                parent = prefabRootNode;
            }

            var viewModel = data.View.ViewModel;
            var prefabRootDepth = viewModel.GetDepth(in prefabRootNode);
            var parentDepth = viewModel.GetDepth(in parent);

            // Disallow dropping as sibling to the prefab instance root (In Prefab Mode we only want to show one root).
            if (parent != prefabRootNode && parentDepth <= prefabRootDepth)
                return DragVisualMode.Rejected;

            // Check for cyclic nesting (only on perform since it is an expensive operation)
            if (perform)
            {
                var prefabAssetThatIsAddedTo = AssetDatabase.LoadMainAssetAtPath(prefabStage.assetPath);

                if (prefabAssetThatIsAddedTo is BrokenPrefabAsset)
                    return DragVisualMode.None;

                foreach (var dragged in data.EntityIds)
                {
                    var obj = EditorUtility.EntityIdToObject(dragged);
                    if (obj is GameObject && EditorUtility.IsPersistent(obj))
                    {
                        var prefabAssetThatWillBeAdded = obj;
                        if (PrefabUtility.CheckIfAddingPrefabWouldResultInCyclicNesting(prefabAssetThatIsAddedTo, prefabAssetThatWillBeAdded))
                        {
                            PrefabUtility.ShowCyclicNestingWarningDialog();
                            return DragVisualMode.Rejected;
                        }
                    }
                }
            }

            return DragVisualMode.None;
        }

        // Resolves the framing sibling (or parent fallback) for an insertion point described by
        // data. For BetweenItems, walks data.Parent's children — skipping selected nodes when
        // skipSelected is set, and skipping non-droppable types (anything other than GameObject or
        // SubScene) — to pick the sibling around data.ChildIndex.
        void ResolveSiblingTarget(in HierarchyViewDragAndDropHandlingData data, bool skipSelected, out HierarchyNode target, out HierarchyDropFlags flags)
        {
            if (data.DropPosition == DragAndDropPosition.OverItem)
            {
                target = data.Target;
                flags = HierarchyDropFlags.DropUpon;
                return;
            }

            var parent = data.Parent;
            var childIndex = data.ChildIndex;
            var viewModel = data.View.ViewModel;

            var goNodeType = GetNodeType();
            var subSceneNodeType = GetSubSceneNodeType();

            var siblingAbove = HierarchyNode.Null;
            var siblingBelow = HierarchyNode.Null;

            var currentIndex = 0;
            foreach (var child in data.View.Source.EnumerateChildren(in parent))
            {
                var childType = viewModel.GetNodeType(in child);
                var isValidTarget = childType == goNodeType || childType == subSceneNodeType;

                if (isValidTarget && !(skipSelected && viewModel.HasFlags(in child, HierarchyNodeFlags.Selected)))
                {
                    if (currentIndex < childIndex)
                        siblingAbove = child;
                    else if (siblingBelow == HierarchyNode.Null)
                    {
                        // Once we've crossed childIndex and found a valid below-sibling, siblingAbove
                        // can no longer change — no need to walk the rest of the children.
                        siblingBelow = child;
                        break;
                    }
                }
                currentIndex++;
            }

            if (siblingAbove != HierarchyNode.Null)
            {
                target = siblingAbove;
                flags = HierarchyDropFlags.DropBetween;
            }
            else if (siblingBelow != HierarchyNode.Null)
            {
                target = siblingBelow;
                flags = HierarchyDropFlags.DropAbove;
            }
            else
            {
                target = parent;
                flags = HierarchyDropFlags.DropUpon;
            }
        }

        [NativeMethod(IsThreadSafe = true)]
        internal static extern GameObject GetGameObjectFromEntityId(EntityId entityId);

        [FreeFunction("HierarchyGameObjectHandlerBindings::GetStaticNodeType", IsThreadSafe = true)]
        static extern int GetStaticNodeType();

        [FreeFunction("HierarchyGameObjectHandlerBindings::GetOrCreateNodeFromEntityId", HasExplicitThis = true, IsThreadSafe = true)]
        extern HierarchyNode GetOrCreateNodeFromEntityId(EntityId entityId);

        [FreeFunction("HierarchyGameObjectHandlerBindings::SetPendingExternalDrop", HasExplicitThis = true)]
        extern void SetPendingExternalDrop(HierarchyNode parentNode, int dropIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        extern bool CanStartDrag(ReadOnlySpan<HierarchyNode> nodes);

        #region Called from native
        [RequiredByNativeCode(Optional = true)]
        static IntPtr CreateGameObjectHandler(IntPtr nativePtr, IntPtr hierarchyPtr, IntPtr cmdListPtr)
        {
            if (nativePtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(nativePtr));
            if (hierarchyPtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(hierarchyPtr));
            if (cmdListPtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(cmdListPtr));

            var handler = new HierarchyGameObjectHandler(nativePtr,
                (Hierarchy)GCHandle.FromIntPtr(hierarchyPtr).Target,
                (HierarchyCommandList)GCHandle.FromIntPtr(cmdListPtr).Target);
            handler.Initialize();
            return GCHandle.ToIntPtr(GCHandle.Alloc(handler));
        }
        #endregion
    }
}
