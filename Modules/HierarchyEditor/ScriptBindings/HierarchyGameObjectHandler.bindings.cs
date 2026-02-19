// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
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
    /// The hierarchy node type handler for GameObjects.
    /// </summary>
    [RequiredByNativeCode(Optional = true), StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/HierarchyEditor/Public/HierarchyGameObjectHandler.h")]
    [NativeHeader("Modules/HierarchyEditor/HierarchyGameObjectHandlerBindings.h")]
    public sealed class HierarchyGameObjectHandler :
        HierarchyNodeTypeHandler,
        IHierarchyEntityIdConverter,
        IHierarchyEditorNodeTypeHandler,
        IHierarchySearchPropositionProvider,
        IHierarchyExtendCreateMenu
    {
        const string k_GameObjectUssClass = "hierarchy-item__gameobject-node";
        const string k_GameObjectDisabledUssClass = "unity-disabled";
        const string k_GameObjectDefaultParentUssClass = "hierarchy-item__gameobject-default-parent";

        static HashSet<string> k_SpecialTypes = new HashSet<string>(new [] { "prefab" });

        internal new static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(HierarchyGameObjectHandler handler) => handler.m_Ptr;
        }

        HierarchyNodeType m_NodeType;
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
        /// Gets or creates the hierarchy node corresponding to the given game object entity id.
        /// </summary>
        /// <remarks>
        /// If the node hasn't been created yet, returns the future node that will be used for the game object.
        /// An update of the hierarchy will be necessary if you intend to query the hieararchy about this node.
        /// </remarks>
        /// <param name="entityId">The game object entity id.</param>
        /// <returns>An hierarchy node.</returns>
        public HierarchyNode GetOrCreateNode(EntityId entityId) => GetOrCreateNodeFromEntityId(entityId);

        /// <summary>
        /// Gets or creates the hierarchy node corresponding to the given game object.
        /// </summary>
        /// <remarks>
        /// If the node hasn't been created yet, returns the future node that will be used for the game object.
        /// An update of the hierarchy will be necessary if you intend to query the hieararchy about this node.
        /// </remarks>
        /// <param name="gameObject">The game object.</param>
        /// <returns>An hierarchy node.</returns>
        public HierarchyNode GetOrCreateNode(GameObject gameObject) => gameObject != null ? GetOrCreateNodeFromEntityId(gameObject.GetEntityId()) : HierarchyNode.Null;

        /// <summary>
        /// Gets the game object entity id corresponding to the given hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The entity id.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern EntityId GetEntityId(in HierarchyNode node);

        /// <summary>
        /// Gets the game object corresponding to the given hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>A game object.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern GameObject GetGameObject(in HierarchyNode node);

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

            if (PrefabUtility.IsPartOfPrefabInstance(gameObject) || PrefabUtility.IsAddedGameObjectOverride(gameObject))
                HierarchyViewPrefabStyleUtility.SetNodePrefabStyle(gameObject, item);
            else
                HierarchyViewPrefabStyleUtility.CleanUpNodePrefabStyle(item);
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
            return Hierarchy.GetName(in node);
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
            var subSceneNodeType = Hierarchy.GetNodeType<HierarchySubSceneHandler>();
            var parentNodeType = Hierarchy.GetNodeType(in parent);
            return parentNodeType == gameObjectNodeType || parentNodeType == sceneNodeType || parentNodeType == subSceneNodeType;
        }

        bool IHierarchyEditorNodeTypeHandler.AcceptChild(HierarchyView view, in HierarchyNode child)
        {
            var sceneNodeType = Hierarchy.GetNodeType<HierarchySceneHandler>();
            var subSceneNodeType = Hierarchy.GetNodeType<HierarchySubSceneHandler>();
            var childNodeType = Hierarchy.GetNodeType(in child);
            return childNodeType != sceneNodeType && childNodeType != subSceneNodeType;
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
                var go = GetGameObject(in node);
                if (go == null)
                    continue;
                data.EntityIds.Add(go.GetEntityId());
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
            // Sets includeCreateEmptyChild to false since "Create Empty Child" is redundant when right-clicking a GameObject.
            MenuUtilsForHierarchyWindow.AddCreateGameObjectItemsToMenu(menu,
                                           gameObject != null ? selectedGameObjects : Array.Empty<GameObject>(),
                                           true,
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
            return customParentNode != HierarchyNode.Null && !view.IsSelectedOrAnyAncestorSelected(Hierarchy.GetParent(in customParentNode));
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

            var parent = data.Parent;
            var target = data.Target;
            var view = data.View;
            var parentIndex = parent == HierarchyNode.Null ? -1 : view.ViewModel.IndexOf(in parent);
            var targetIndex = target == HierarchyNode.Null ? -1 : view.ViewModel.IndexOf(in target);
            option = DragAndDropHelpers.GetDefaultDropFlags(view, in parent, dropPosition, searchActive, data.InsertAtIndex, parentIndex, targetIndex);

            var currentTarget = target;
            var sceneNodeType = Hierarchy.GetNodeType<HierarchySceneHandler>();
            var targetNodeType = Hierarchy.GetNodeType(in target);

            // When dropping above a scene, we need to update the target and options for the drop handler to work properly.
            if (target != HierarchyNode.Null && option.HasFlag(HierarchyDropFlags.DropAbove) && targetNodeType == sceneNodeType)
            {
                --targetIndex;
                currentTarget = targetIndex >= 0 ? view.ViewModel[targetIndex] : HierarchyNode.Null;
                targetNodeType = currentTarget == HierarchyNode.Null ? HierarchyNodeType.Null : Hierarchy.GetNodeType(in currentTarget);

                option &= ~HierarchyDropFlags.DropAbove;

                if (targetNodeType == sceneNodeType)
                    option |= HierarchyDropFlags.DropUpon;
                else
                    option |= HierarchyDropFlags.DropBetween;
            }

            // When dropping as first child, we need to update the target and options for the drop handler to work properly.
            if (target != HierarchyNode.Null && option.HasFlag(HierarchyDropFlags.DropAfterParent) && !option.HasFlag(HierarchyDropFlags.DropAbove))
            {
                option |= HierarchyDropFlags.DropAbove;
                ++targetIndex;
                currentTarget = targetIndex >= 0 ? view.ViewModel[targetIndex] : HierarchyNode.Null;
                targetNodeType = currentTarget == HierarchyNode.Null ? HierarchyNodeType.Null : Hierarchy.GetNodeType(in currentTarget);
            }

            var entityId = GetDropTargetEntityId(in currentTarget, in targetNodeType, option);
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

            visualMode = DragAndDrop.DropOnHierarchyWindow(entityId, option, null, perform);

            if (perform && visualMode != DragAndDropVisualMode.Rejected && visualMode != DragAndDropVisualMode.None)
            {
                // This is specifically to handle the case where we drag and drop existing game objects around. In that case,
                // we can't rely on the selectionChanged event to trigger the framing, because selection doesn't change.
                // We also can't rely on the generic framing done after the drop, because it queues an action to be done after all Hierarchy
                // update during the next HierarchyWindow.Update, which happens BEFORE SceneTracker.FlushDirty so the Hierarchy is not dirty
                // and we only process the extra actions, which frames the nodes at the old position, not the new one.
                // Note: this code is not clean, and definitely not the best solution. This is because the HierarchyGameObjectHandler is depending
                // on the SceneTracker to get notified that something happened. If we had GlobalCallbacks for everything, commands would have already been
                // queued on the Hierarchy and this would not be an issue.
                void OnDropNewGameObjectsSelectionChange()
                {
                    // Selection changed happened because new game objects were added,
                    // so FlushDirty triggered before the next Editor update. In that case, no need
                    // to do extra framing since changing the selection will frame the nodes.
                    EditorApplication.update -= OnDropExistingGameObjects;
                    Selection.selectionChanged -= OnDropNewGameObjectsSelectionChange;
                }
                void OnDropExistingGameObjects()
                {
                    // Editor update happened and there was no selection change, execute the framing.
                    Selection.selectionChanged -= OnDropNewGameObjectsSelectionChange;
                    EditorApplication.update -= OnDropExistingGameObjects;

                    if (view?.ViewModel == null)
                        return;

                    foreach (ref readonly var node in view.ViewModel.EnumerateNodesWithFlags(HierarchyNodeFlags.Selected))
                    {
                        if (node == HierarchyNode.Null)
                            continue;

                        view.Frame(in node);
                        break;
                    }
                }

                Selection.selectionChanged += OnDropNewGameObjectsSelectionChange;
                EditorApplication.update += OnDropExistingGameObjects;
            }

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
            if (dropTargetNodeType == Hierarchy.GetNodeType<HierarchySceneHandler>())
            {
                var sceneHandler = Hierarchy.GetNodeTypeHandlerBase<HierarchySceneHandler>();
                var scene = sceneHandler.GetScene(in dropTarget);
                return scene.IsValid() ? scene.handle.ToEntityId() : EntityId.None;
            }

            if (dropTargetNodeType == Hierarchy.GetNodeType<HierarchySubSceneHandler>())
            {
                var subSceneHandler = Hierarchy.GetNodeTypeHandlerBase<HierarchySubSceneHandler>();
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

        HierarchyNode IHierarchyEntityIdConverter.GetNode(EntityId entityId) => GetNodeFromEntityId(entityId);

        void IHierarchyEntityIdConverter.GetNodes(ReadOnlySpan<EntityId> entityIds, Span<HierarchyNode> outNodes) => GetNodesFromEntityIds(entityIds, outNodes);

        EntityId IHierarchyEntityIdConverter.GetEntityId(in HierarchyNode node) => GetEntityIdFromNode(in node);

        void IHierarchyEntityIdConverter.GetEntityIds(ReadOnlySpan<HierarchyNode> nodes, Span<EntityId> outEntityIds) => GetEntityIdsFromNodes(nodes, outEntityIds);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern GameObject GetGameObjectFromEntityId(EntityId entityId);

        [FreeFunction("HierarchyGameObjectHandlerBindings::GetStaticNodeType", IsThreadSafe = true)]
        static extern int GetStaticNodeType();

        [FreeFunction("HierarchyGameObjectHandlerBindings::GetOrCreateNodeFromEntityId", HasExplicitThis = true, IsThreadSafe = true)]
        extern HierarchyNode GetOrCreateNodeFromEntityId(EntityId entityId);

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
