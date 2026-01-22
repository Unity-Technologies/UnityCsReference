// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A GraphElement to display a <see cref="BlackboardContentModel"/>.
    /// </summary>
    [UnityRestricted]
    internal class Blackboard : BlackboardElement, IHasContextualMenuItems
    {
        /// <summary>
        /// The USS class name added to this element.
        /// </summary>
        public new static readonly string ussClassName = "ge-blackboard";

        /// <summary>
        /// The name of the header part.
        /// </summary>
        public static readonly string blackboardHeaderPartName = GraphElementHelper.headerName;

        /// <summary>
        /// The <see cref="BaseTreeView"/>.
        /// </summary>
        protected BaseTreeView m_TreeView;

        /// <summary>
        /// The ScrollView used for the whole blackboard.
        /// </summary>
        public ScrollView ScrollView => m_TreeView.Q<ScrollView>();

        List<TreeViewItemData<IGroupItemModel>> m_RootItems = new();

        struct ItemInfo
        {
            public int id;
            public List<int> childrenIds;
        }

        Dictionary<IGroupItemModel, ItemInfo> m_ItemToInfos = new Dictionary<IGroupItemModel, ItemInfo>();
        int m_Id;

        protected void UnbindTreeViewCell(VisualElement element, int index)
        {
            var rootElement = element.parent.parent;

            //Hack to get informed of collapse change.
            var toggle = rootElement.Q<Toggle>();

            if (m_ToggleRegister.TryGetValue(toggle, out var eventCallback))
            {
                toggle.UnregisterCallback(eventCallback);
            }

            var views = new List<ModelView>();

            foreach (var child in element.Children())
            {
                if (child is ModelView mv)
                    views.Add(mv);
            }

            foreach (var modelView in views)
            {
                element.Remove(modelView);
                modelView.RemoveFromRootView();
            }
        }

        Dictionary<Toggle, EventCallback<ChangeEvent<bool>>> m_ToggleRegister = new();

        protected void BindTreeViewCell(VisualElement element, int index)
        {
            var groupItem = m_TreeView.GetItemDataForIndex<IGroupItemModel>(index);

            var rootElement = element.parent.parent;

            //Hack to get informed of collapse change.
            var toggle = rootElement.Q<Toggle>();
            EventCallback<ChangeEvent<bool>> action = e => OnCollapseChange(e, groupItem);
            m_ToggleRegister[toggle] = action;
            toggle.RegisterCallback(action);

            if (element.childCount > 0 && element.ElementAt(0) is ModelView modelView && modelView.Model == groupItem)
            {
                return;
            }

            while (element.childCount > 0)
            {
                element.RemoveAt(element.childCount - 1);
            }

            var view = ModelViewFactory.CreateUI<ModelView>(RootView, groupItem as Model);

            rootElement?.EnableInClassList(ussClassName.WithUssModifier("unselectable-item"), !(groupItem as GraphElementModel).IsSelectable());

            element.Add(view);
        }

        void OnCollapseChange(ChangeEvent<bool> evt, IGroupItemModel model)
        {
            if (model is GroupModelBase group)
                BlackboardView.Dispatch(new ExpandVariableGroupCommand(group, evt.newValue));
            else if (model is VariableDeclarationModel variable)
                BlackboardView.Dispatch(new ExpandVariableDeclarationCommand(variable, evt.newValue));
        }

        bool m_WaitingForRefresh;
        bool m_WaitingForCollapseRefresh;
        IVisualElementScheduledItem m_WaitingRefreshSchedule;

        void Refresh(bool refreshCollapse, bool shouldRebuild = false)
        {
            if (refreshCollapse)
                m_WaitingForCollapseRefresh = true;
            if (m_WaitingForRefresh)
                return;

            m_WaitingForRefresh = true;
            m_WaitingRefreshSchedule = schedule.Execute(() =>
            {
                DoRefresh(shouldRebuild);
            });

            m_WaitingRefreshSchedule.ExecuteLater(0);
        }

        internal void DoRefresh(bool shouldRebuild = false)
        {
            if (!m_WaitingForRefresh)
                return;

            if (m_WaitingForCollapseRefresh)
                UpdateCollapse();
            m_IgnoreSelectionChange = true;
            if (shouldRebuild)
                m_TreeView.Rebuild();
            else
                m_TreeView.RefreshItems();
            m_IgnoreSelectionChange = false;
            if (m_WaitingRefreshSchedule != null)
            {
                m_WaitingRefreshSchedule.Pause();
                m_WaitingRefreshSchedule = null;
            }
            m_WaitingForRefresh = false;
            m_WaitingForCollapseRefresh = false;
        }

        internal void RefreshGroup(GroupModelBase group)
        {
            if (!m_ItemToInfos.TryGetValue(group, out var infos))
                return;

            bool changed = false;


            if (infos.childrenIds != null)
            {
                var deletedIds = new List<int>();
                //Check for deleted
                foreach (var childId in infos.childrenIds)
                {
                    IGroupItemModel childItem = m_TreeView.GetItemDataForId<IGroupItemModel>(childId);
                    if (!group.Items.ContainsReference(childItem))
                    {
                        changed = true;
                        deletedIds.Add(childId);
                        m_TreeView.viewController.TryRemoveItem(childId, false);
                    }
                }

                foreach (var id in deletedIds)
                {
                    infos.childrenIds.Remove(id);
                }
            }

            int cpt = 0;
            foreach (var childItem in group.Items)
            {
                if (infos.childrenIds == null || !m_ItemToInfos.TryGetValue(childItem, out ItemInfo childInfos) || !infos.childrenIds.Contains(childInfos.id))
                {
                    changed = true;

                    infos.childrenIds ??= new List<int>();
                    infos.childrenIds.Insert(cpt, m_Id);

                    m_TreeView.AddItem(GetItemData(ref m_Id, childItem), infos.id, cpt, false);
                }

                cpt++;
            }

            for (int i = 0; i < group.Items.Count; ++i)
            {
                int treeviewItemId = infos.childrenIds[i];
                int modelItemId = m_ItemToInfos[group.Items[i]].id;
                if (treeviewItemId != modelItemId)
                {
                    changed = true;
                    m_TreeView.viewController.Move(modelItemId, infos.id, i, false);
                    infos.childrenIds.Remove(modelItemId);
                    infos.childrenIds.Insert(i, modelItemId);
                }
            }

            if (changed)
            {
                m_ItemToInfos[group] = infos;
                Refresh(true);
            }
        }

        /// <summary>
        /// Creates and Configures the <see cref="BaseTreeView"/>.
        /// </summary>
        protected virtual void CreateTreeView()
        {
            if (m_TreeView != null && m_TreeView.parent != null)
                m_TreeView.RemoveFromHierarchy();

            var treeView = new TreeView(){
                reorderable = true,
                selectionType = SelectionType.Multiple,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            };
            m_TreeView = treeView;

            treeView.makeItem = () => new VisualElement();
            treeView.bindItem = BindTreeViewCell;
            treeView.unbindItem = UnbindTreeViewCell;

            RegisterCallback<ContextualMenuPopulateEvent>(BuildContextualMenu);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Blackboard"/> class.
        /// </summary>
        public Blackboard()
        {
            RegisterCallback<DragUpdatedEvent>(e =>
            {
                e.StopPropagation();
            });

            RegisterCallback<PromptItemLibraryEvent>(OnPromptItemLibrary);
            RegisterCallback<ShortcutShowItemLibraryEvent>(OnShortcutShowItemLibraryEvent);
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            base.BuildPartList();

            if( Model is BlackboardContentModel blackboardContentModel)
                PartList.AppendPart(BlackboardHeaderPart.Create(blackboardHeaderPartName, blackboardContentModel, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void BuildUI()
        {
            base.BuildUI();

            CreateTreeView();
            ConfigureTreeView();
        }

        /// <summary>
        /// Sets up the <see cref="BaseTreeView"/> with the necessary configurations.
        /// </summary>
        /// <remarks>
        /// 'ConfigureTreeView' sets up the <see cref="BaseTreeView"/> with the necessary configurations, including selection
        /// change callbacks and drag-and-drop handling.
        /// </remarks>
        protected void ConfigureTreeView()
        {
            m_TreeView.selectedIndicesChanged += OnSelectionChange;

            m_TreeView.canStartDrag += OnCanStartDrag;
            m_TreeView.dragAndDropUpdate += OnDragAndDropUpdate;

            m_TreeView.handleDrop += OnHandleDrop;
            m_TreeView.setupDragAndDrop += OnSetupDragAndDrop;

            hierarchy.Add(m_TreeView);
            m_TreeView.BringToFront();
        }

        /// <summary>
        /// Handles the drop in the <see cref="BaseTreeView"/>.
        /// </summary>
        /// <param name="args">The <see cref="HandleDragAndDropArgs"/> </param>
        /// <returns> A <see cref="DragVisualMode"/> indicating if the drop was accepted.</returns>
        protected virtual DragVisualMode OnHandleDrop(HandleDragAndDropArgs args)
        {
            var droppedOnModel = args.parentId == -1 ? BlackboardView.BlackboardRootViewModel.GraphModelState.GraphModel.GetSectionModel(GraphModel.DefaultSectionName) : m_TreeView.GetItemDataForId<IGroupItemModel>(args.parentId);

            if (droppedOnModel is GroupModel group)
            {
                var draggedModels = args.dragAndDropData.GetGenericData(SelectionDropper.k_DragAndDropKey) as IReadOnlyList<GraphElementModel>;

                if (draggedModels != null && group.CanAcceptDrop(draggedModels))
                {
                    IGroupItemModel insertAfter = null;

                    if (args.dropPosition == DragAndDropPosition.OverItem)
                        insertAfter = (group.Items.Count > 0 ? group.Items[^ 1] : null);
                    else if (args.childIndex == -1)
                        insertAfter = group.Items[^ 1];
                    else
                        insertAfter = (args.childIndex == 0 ? null : group.Items[args.childIndex - 1]);

                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    RootView.Dispatch(new ReorderGroupItemsCommand(group, insertAfter, draggedModels.Cast<IGroupItemModel>().ToList()));
#pragma warning restore RS0030
                    return DragVisualMode.Move;
                }
            }

            return DragVisualMode.Rejected;
        }

        /// <summary>
        /// Handles the drop in the <see cref="BaseTreeView"/>.
        /// </summary>
        /// <param name="args">The <see cref="HandleDragAndDropArgs"/> </param>
        /// <returns> A <see cref="DragVisualMode"/> indicating if the drop would be accepted.</returns>
        protected virtual DragVisualMode OnDragAndDropUpdate(HandleDragAndDropArgs args)
        {
            m_TreeView.ReleaseMouse();

            var draggedOnModel = args.parentId == -1 ? BlackboardView.BlackboardRootViewModel.GraphModelState.GraphModel.GetSectionModel(GraphModel.DefaultSectionName) : m_TreeView.GetItemDataForId<IGroupItemModel>(args.parentId);

            if (draggedOnModel is GroupModel group)
            {
                var draggedModels = args.dragAndDropData.GetGenericData(SelectionDropper.k_DragAndDropKey) as IReadOnlyList<GraphElementModel>;

                if (draggedModels != null && group.CanAcceptDrop(draggedModels))
                {
                    return DragVisualMode.Move;
                }
            }

            return DragVisualMode.Rejected;
        }

        /// <summary>
        /// Setups a drag and drop based on the selection in the <see cref="BaseTreeView"/>.
        /// </summary>
        /// <param name="args">the <see cref="SetupDragAndDropArgs"/>.</param>
        /// <returns>A <see cref="StartDragArgs"/> containing elements to drag.</returns>
        protected virtual StartDragArgs OnSetupDragAndDrop(SetupDragAndDropArgs args)
        {
            var arg = args.startDragArgs;

            List<GraphElementModel> graphElements = new List<GraphElementModel>();

            foreach (var id in args.selectedIds)
            {
                var model = m_TreeView.GetItemDataForId<IGroupItemModel>(id) as GraphElementModel;

                if (model != null && model.IsDroppable())
                {
                    graphElements.Add(model);
                }
            }
            arg.SetGenericData(SelectionDropper.k_DragAndDropKey, graphElements);

            return arg;
        }

        /// <summary>
        /// Checks whether the drag of a given selection is possible.
        /// </summary>
        /// <param name="args">The <see cref="CanStartDragArgs"/>.</param>
        /// <returns>True if the drag is possible. False otherwise.</returns>
        protected virtual bool OnCanStartDrag(CanStartDragArgs args)
        {
            foreach (var id in args.selectedIds)
            {
                IGroupItemModel model = m_TreeView.GetItemDataForId<IGroupItemModel>(id);
                if (model is SectionModel)
                    return false;
            }

            return true;
        }

        void OnSelectionChange(IEnumerable<int> indices)
        {
            if (m_IgnoreSelectionChange)
                return;
            var selection = new List<GraphElementModel>();

            var filteredIndices = new List<int>();
            foreach (var index in indices)
            {
                var model = m_TreeView.GetItemDataForIndex<IGroupItemModel>(index) as GraphElementModel;

                if (model != null && model.IsSelectable())
                {
                    selection.Add(model);
                    filteredIndices.Add(index);
                }
            }
            // Remove unselectable from selection
            m_TreeView.SetSelectionWithoutNotify(filteredIndices);

            BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, selection));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
            this.AddPackageStylesheet("Blackboard.uss");

            var headerPart = PartList.GetPart(blackboardHeaderPartName).Root;
            if (headerPart != null)
                hierarchy.Insert(0, headerPart);
        }

        /// <summary>
        /// Returns true if the item is in the tree view.
        /// </summary>
        /// <param name="item">The item</param>
        /// <returns>true if the item is in the tree view.</returns>
        protected bool ItemIsInTreeView(IGroupItemModel item)
        {
            return m_ItemToInfos.ContainsKey(item);
        }

        TreeViewItemData<IGroupItemModel> GetItemData(ref int id, IGroupItemModel item)
        {
            List<TreeViewItemData<IGroupItemModel>> children = null;

            if (item is GroupModelBase group && group.Items.Count > 0)
            {
                children = new List<TreeViewItemData<IGroupItemModel>>();

                foreach (var subItem in group.Items)
                {
                    children.Add(GetItemData(ref id, subItem));
                }
            }

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_ItemToInfos[item] = new ItemInfo() { id = id,  childrenIds = children?.Select(t => t.id).ToList()};
#pragma warning restore RS0030
            return new TreeViewItemData<IGroupItemModel>(id++, item, children);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);
            UpdateFromModel();
        }

        /// <summary>
        /// Updates the treeview based on the model.
        /// </summary>
        protected void UpdateFromModel()
        {
            m_Id = 0;
            m_ItemToInfos.Clear();

            if (Model is BlackboardContentModel blackboardContentModel)
            {
                var graphModel = blackboardContentModel.GraphModel;

                var defaultSection = graphModel.GetSectionModel(GraphModel.DefaultSectionName);
                m_RootItems.Clear();

                foreach (var item in defaultSection.Items)
                {
                    m_RootItems.Add(GetItemData(ref m_Id, item));
                }

                foreach (var section in graphModel.SectionModels)
                {
                    if (section == defaultSection)
                        continue;

                    m_RootItems.Add(GetItemData(ref m_Id, section));
                }
                m_TreeView.SetRootItems(m_RootItems);
                Refresh(true, true);
            }

            ScrollView.scrollOffset = BlackboardView.BlackboardRootViewModel.ViewState.ScrollOffset;
            UpdateCollapse();
        }

        static PropertyInfo s_VirtualizationControllerProperty;
        static MethodInfo s_GetIndexFromPositionMethod;

        /// <summary>
        /// Callback to add menu items to the contextual menu.
        /// </summary>
        /// <param name="evt">The <see cref="ContextualMenuPopulateEvent"/>.</param>
        /// <remarks>
        /// 'BuildContextualMenu' is a callback method used to add menu items to the contextual menu of the <see cref="Blackboard"/>. It allows customization of the right-click menu
        /// by dynamically populating it with relevant actions based on the current context.
        /// </remarks>
        protected new void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // If the menu already has items, append a separator.
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            // Get the categorized menu items.
            Dictionary<ContextualMenuCategory, List<ContextualMenuItem>> categorizedMenuItems = null;
            if (evt.target is Blackboard)
            {
                // If the user right-clicked on the blackboard itself (even if a variable is selected), use its menu items.
                categorizedMenuItems = ContextualMenuHelpers.CategorizeMenuItems(ContextualMenuItems);
            }
            else
            {
                var selection = BlackboardView.GetSelection();
                foreach (var elementModel in selection)
                {
                    if (elementModel is VariableDeclarationModelBase vdm)
                    {
                        categorizedMenuItems = ContextualMenuHelpers.CategorizeMenuItems(vdm.ContextualMenuItems);
                        break;
                    }
                }
            }

            categorizedMenuItems ??= ContextualMenuHelpers.CategorizeMenuItems(ContextualMenuItems);

            // If there are no categorized menu items, we can return early.
            if (categorizedMenuItems == null)
                return;

            var menuActionMap = new Dictionary<string, Action>();
            PopulateMenuActionMap(menuActionMap, evt);
            ViewSelection.BuildContextualMenu(categorizedMenuItems, evt, menuActionMap);
        }

        void PopulateMenuActionMap(Dictionary<string, Action> menuActionMap, ContextualMenuPopulateEvent evt)
        {
            if (menuActionMap == null)
                return;

            BlackboardView.ViewSelection.PopulateMenuActionMap(menuActionMap, evt);
            menuActionMap.Add(ContextualMenuHelpers.createVariableItem.Name, () => AppendCreateVariableMenuItem(evt));
            menuActionMap.Add(ContextualMenuHelpers.createGroupItem.Name, () => AppendCreateGroupMenuItem(evt));

            // TODO (GTF-2241): Uncomment the following when the "Unused" feature is fixed.
            // menuActionMap.Add(ContextualMenuHelpers.selectUnusedItem.Name, () => AppendSelectUnusedMenuItem(evt));
        }

        void AppendCreateVariableMenuItem(ContextualMenuPopulateEvent evt)
        {
            var blackboardContentModel = (BlackboardContentModel)Model;
            if (!blackboardContentModel.HasDefaultButton())
                return;

            evt.menu.AppendAction(L10n.Tr("Create Variable"), _ =>
            {
                CreateVariable();
            });
        }

        void AppendCreateGroupMenuItem(ContextualMenuPopulateEvent evt)
        {
            IGroupItemModel groupModel = null;

            s_VirtualizationControllerProperty ??= typeof(TreeView).GetProperty("virtualizationController", BindingFlags.NonPublic | BindingFlags.Instance);
            if (s_VirtualizationControllerProperty != null)
            {
                s_GetIndexFromPositionMethod ??= s_VirtualizationControllerProperty.PropertyType.GetMethod("GetIndexFromPosition", BindingFlags.Public | BindingFlags.Instance);
                if (s_GetIndexFromPositionMethod != null)
                {
                    int index = (int)s_GetIndexFromPositionMethod.Invoke(s_VirtualizationControllerProperty.GetValue(m_TreeView), new object[] {m_TreeView.Q<ScrollView>().contentContainer.WorldToLocal(evt.mousePosition)});
                    var model = m_TreeView.GetItemDataForIndex<IGroupItemModel>(index);
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    if (model != null && model.ParentGroup is GroupModel && !BlackboardView.GetSelection().OfType<IGroupItemModel>().HasAny(t => t.GetSection() != model.GetSection() || t.ParentGroup is not GroupModel))
#pragma warning restore RS0030
                    {
                        groupModel = model;
                    }
                }
            }

            evt.menu.AppendAction(L10n.Tr("Create Group"), _ =>
            {
                if (groupModel == null)
                    CreateGroup(null);
                else
                    BlackboardView.CreateGroupFromSelection(groupModel);
            });
        }

        void AppendSelectUnusedMenuItem(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction(L10n.Tr("Select Unused"), _ =>
            {
                BlackboardView.DispatchSelectUnusedVariables();
            });
        }

        /// <summary>
        /// Callback for <see cref="ShortcutShowItemLibraryEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutShowItemLibraryEvent(ShortcutShowItemLibraryEvent e)
        {
            using (var itemLibraryEvent = PromptItemLibraryEvent.GetPooled(e.MousePosition))
            {
                itemLibraryEvent.target = e.target;
                SendEvent(itemLibraryEvent);
            }
            e.StopPropagation();
        }

        void OnPromptItemLibrary(PromptItemLibraryEvent e)
        {
            ShowCreateVariableLibrary(e.MenuPosition, null);

            e.StopPropagation();
        }

        /// <summary>
        /// Displays the ItemLibrary in the purpose of creating a new variable.
        /// </summary>
        /// <param name="menuPosition">The position of the menu in world space.</param>
        /// <param name="parentGroup">The optional <see cref="GroupModel"/> in which the new variable will be added.</param>
        public virtual void ShowCreateVariableLibrary(Vector2 menuPosition, GroupModel parentGroup)
        {
            var graphModel = (Model as BlackboardContentModel)?.GraphModel;
            if (graphModel == null)
            {
                return;
            }

            if (parentGroup == null)
            {
                parentGroup = BlackboardView.BlackboardRootViewModel.GraphModelState.GraphModel.GetSectionModel(GraphModel.DefaultSectionName);

                foreach (var selected in BlackboardView.GetSelection())
                {
                    if (selected is GroupModel groupModel && groupModel.GetSection() == parentGroup)
                    {
                        parentGroup = groupModel;
                        break;
                    }
                }
            }

            var itemLibraryHelper = (RootView as IHasItemLibrary).GetItemLibraryHelper();

            ItemLibraryService.ShowVariableTypes(
                RootView,
                itemLibraryHelper.GetItemDatabaseProvider(),
                graphModel.VariableDeclarationType,
                RootView.GraphTool.Preferences,
                menuPosition,
                (t, variableType, scope, modifiers) =>
                {
                    if (!t.IsValid)
                    {
                        CreateGroup(parentGroup);
                    }
                    else
                    {
                        CreateVariableForType(parentGroup, t, variableType, scope, modifiers);
                    }
                });
        }

        void CreateGroup(GroupModel parentGroup)
        {
            var graphModel = (Model as BlackboardContentModel)?.GraphModel;
            if (graphModel == null)
                return;
            BlackboardView.Dispatch(new BlackboardGroupCreateCommand(parentGroup ?? graphModel.GetSectionModel(GraphModel.DefaultSectionName))
            );
        }

        void CreateVariableForType(GroupModel group, TypeHandle t, Type variableType, VariableScope scope = VariableScope.Local, ModifierFlags modifiers = ModifierFlags.None)
        {
            variableType ??= (Model as BlackboardContentModel)?.GraphModel?.VariableDeclarationType ?? typeof(VariableDeclarationModel);

            BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand
                (
                    "New Variable",
                    scope,
                    t,
                    variableType,
                    group,
                    int.MaxValue,
                    modifiers
                )
            );
        }

        /// <summary>
        /// Searches for the correct group that would be used to contain a new variable in the selection.
        /// </summary>
        /// <param name="section">The section.</param>
        /// <returns>Either a group in the section or the section itself.</returns>
        public GroupModel GetTargetGroupForNewVariable(SectionModel section)
        {
            foreach (var selected in BlackboardView.GetSelection())
            {
                if (selected is GroupModel groupModel && groupModel.GetSection() == section)
                {
                    return groupModel;
                }
            }
            foreach (var selected in BlackboardView.GetSelection())
            {
                if (selected is VariableDeclarationModel variableModel && variableModel.GetSection() == section && variableModel.ParentGroup is GroupModel groupModel)
                {
                    return groupModel;
                }
            }

            return section;
        }

        /// <summary>
        /// Create a new variable with the previous or default values for scope, type and modifiers.
        /// </summary>
        public void CreateVariable()
        {
            GroupModel selectedGroupInThisSection =  GetTargetGroupForNewVariable(BlackboardView.BlackboardRootViewModel.GraphModelState.GraphModel.GetSectionModel(GraphModel.DefaultSectionName));

            var lastVariableInfos = (Model as BlackboardContentModel)?.LastVariableInfos;
            if (lastVariableInfos == null)
                return;

            var typeHandle = lastVariableInfos.TypeHandle;

            var provider = BlackboardView.GetItemLibraryHelper().GetItemDatabaseProvider();
            if (provider is DefaultDatabaseProvider defaultProvider)
            {
                var supportedTypes = defaultProvider.SupportedTypes;

                if (supportedTypes.Count > 0 &&
                    !typeHandle.IsCustomTypeHandle() &&
                    typeHandle == (Model as BlackboardContentModel)?.DefaultVariableInfos.TypeHandle &&
                    !supportedTypes.Contains(typeHandle.Resolve()))
                {
                    typeHandle = supportedTypes[0].GenerateTypeHandle();
                }
            }

            BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand(
                "New Variable",
                lastVariableInfos.Scope,
                typeHandle,
                lastVariableInfos.VariableType ?? (Model as BlackboardContentModel)?.GraphModel?.VariableDeclarationType ?? typeof(VariableDeclarationModel),
                selectedGroupInThisSection,
                modifierFlags: lastVariableInfos.ModifierFlags
            ));
        }

        /// <summary>
        /// Makes an element visible in the scroll view. The element must be a descendant of the blackboard scrollView.
        /// </summary>
        /// <param name="element">The element to make visible.</param>
        public void ScrollToMakeVisible(VisualElement element)
        {
            schedule.Execute(_ =>
            {
                var scrolled = ScrollView.scrollOffset.y;
                var visibleHeight = ScrollView.localBound.height;

                var elementBoundsInScroll = element.parent.ChangeCoordinatesTo(ScrollView.contentContainer, element.localBound);

                if (scrolled > elementBoundsInScroll.yMin)
                {
                    ScrollView.scrollOffset = new Vector2(ScrollView.scrollOffset.x, elementBoundsInScroll.yMin);
                }
                else if (scrolled + visibleHeight < elementBoundsInScroll.yMax)
                {
                    ScrollView.scrollOffset = new Vector2(ScrollView.scrollOffset.x, elementBoundsInScroll.yMax - visibleHeight);
                }
            }).ExecuteLater(0);
        }

        List<int> m_SelectedIds = new();

        internal void UpdateSelection()
        {
            var selection = BlackboardView.GetSelection();

            m_SelectedIds.Clear();

            foreach (var graphElementModel in selection)
            {
                if (graphElementModel is not IGroupItemModel item)
                    continue;

                if (m_ItemToInfos.TryGetValue(item, out ItemInfo infos))
                {
                    m_SelectedIds.Add(infos.id);
                }
            }

            m_IgnoreSelectionChange = true;
            m_TreeView.SetSelectionByIdWithoutNotify(m_SelectedIds);
            m_IgnoreSelectionChange = false;
        }

        bool m_IgnoreSelectionChange;

        internal void UpdateCollapse()
        {
            bool changed = false;
            foreach (var kv in m_ItemToInfos)
            {
                if (kv.Key is GroupModelBase group)
                {
                    var id = kv.Value.id;
                    var expanded = BlackboardView.BlackboardRootViewModel.ViewState.GetGroupExpanded(group);

                    if (group.Items.Count > 0 && m_TreeView.IsExpanded(id) != expanded)
                    {
                        changed = true;
                        if (expanded)
                            m_TreeView.ExpandItem(id, false, false);
                        else
                            m_TreeView.CollapseItem(id, false, false);
                    }
                }
            }

            if (changed)
                Refresh(false);
        }

        internal bool IsGroupViewExpanded(GroupModelBase group)
        {
            return m_TreeView.IsExpanded(m_ItemToInfos[group].id);
        }

        /// <inheritdoc />
        public IReadOnlyList<ContextualMenuItem> ContextualMenuItems => k_ContextualMenuItems;

        static readonly List<ContextualMenuItem> k_ContextualMenuItems = new() {
            ContextualMenuHelpers.createVariableItem,
            ContextualMenuHelpers.createGroupItem,
            ContextualMenuHelpers.pasteItem,
            ContextualMenuHelpers.selectAllItem,
            ContextualMenuHelpers.selectUnusedItem,
        };
    }
}
