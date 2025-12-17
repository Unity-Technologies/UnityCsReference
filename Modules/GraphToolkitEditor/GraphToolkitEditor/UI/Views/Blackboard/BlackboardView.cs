// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.CSO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The blackboard view.
    /// </summary>
    [UnityRestricted]
    internal class BlackboardView : RootView, IDragSource, IHasItemLibrary
    {
        static List<ChildView> s_UIList = new();
        static List<ChildView> s_SelectionUIList = new();

        public new static readonly string ussClassName = "blackboard-view";

        BlackboardGraphLoadedObserver m_GraphLoadedObserver;
        ModelViewUpdater m_UpdateObserver;
        DeclarationHighlighter m_DeclarationHighlighter;
        GraphVariablesObserver m_GraphVariablesObserver;

        /// <summary>
        /// The <see cref="Unity.GraphToolkit.Editor.ItemLibraryHelper"/> of the <see cref="BlackboardView"/>.
        /// </summary>
        protected ItemLibraryHelper m_ItemLibraryHelper;

        /// <summary>
        /// The <see cref="Unity.GraphToolkit.Editor.ViewSelection"/> of the <see cref="BlackboardView"/>.
        /// </summary>
        public ViewSelection ViewSelection { get; }

        /// <summary>
        /// The <see cref="Unity.GraphToolkit.Editor.Blackboard"/> of the <see cref="BlackboardView"/>.
        /// </summary>
        public Blackboard Blackboard { get; private set; }

        /// <summary>
        /// The model backing the <see cref="BlackboardView"/>.
        /// </summary>
        public BlackboardRootViewModel BlackboardRootViewModel => (BlackboardRootViewModel)Model;

        /// <summary>
        /// Creates a new instance of the <see cref="BlackboardView"/> class.
        /// </summary>
        /// <param name="window">The <see cref="EditorWindow"/> containing this view.</param>
        /// <param name="graphTool">The tool hosting this view.</param>
        /// <param name="typeHandleInfos">A <see cref="TypeHandleInfos"/> to use for this view. If null a new one will be created.</param>
        /// <param name="blackboardRootViewModel">The model for the view.</param>
        /// <param name="viewSelection">The selection helper.</param>
        public BlackboardView(EditorWindow window, GraphTool graphTool, TypeHandleInfos typeHandleInfos,
                                 BlackboardRootViewModel blackboardRootViewModel, ViewSelection viewSelection)
            : base(window, graphTool, typeHandleInfos)
        {
            Model = blackboardRootViewModel;

            this.AddPackageStylesheet("BlackboardView.uss");
            AddToClassList(ussClassName);

            RegisterCallback<KeyDownEvent>(OnRenameKeyDown);

            ViewSelection = viewSelection;
            ViewSelection.AttachToView(this);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            ViewSelection.DetachFromView();
        }

        /// <inheritdoc />
        protected override void RegisterCommandHandlers(CommandHandlerRegistrar registrar)
        {
            BlackboardCommandsRegistrarHelper.RegisterCommands(registrar, this);
        }

        /// <inheritdoc />
        protected override void RegisterModelObservers()
        {
            if (m_GraphLoadedObserver == null)
            {
                m_GraphLoadedObserver = new BlackboardGraphLoadedObserver(GraphTool.ToolState,
                    BlackboardRootViewModel.BlackboardContentState, BlackboardRootViewModel.ViewState, BlackboardRootViewModel.SelectionState);
                GraphTool.ObserverManager.RegisterObserver(m_GraphLoadedObserver);
            }
        }

        /// <inheritdoc />
        protected override void RegisterViewObservers()
        {
            if (m_UpdateObserver == null)
            {
                m_UpdateObserver = new ModelViewUpdater(this, BlackboardRootViewModel.BlackboardContentState,
                    BlackboardRootViewModel.GraphModelState, BlackboardRootViewModel.SelectionState, BlackboardRootViewModel.ViewState,
                    GraphTool.HighlighterState);
                GraphTool.ObserverManager.RegisterObserver(m_UpdateObserver);
            }

            if (m_DeclarationHighlighter == null)
            {
                m_DeclarationHighlighter = new DeclarationHighlighter(GraphTool.ToolState, BlackboardRootViewModel.SelectionState, GraphTool.HighlighterState);
                GraphTool.ObserverManager.RegisterObserver(m_DeclarationHighlighter);
            }

            if (m_GraphVariablesObserver == null)
            {
                m_GraphVariablesObserver = new GraphVariablesObserver(BlackboardRootViewModel.GraphModelState, BlackboardRootViewModel.ViewState, BlackboardRootViewModel.SelectionState);
                GraphTool.ObserverManager.RegisterObserver(m_GraphVariablesObserver);
            }
        }

        /// <inheritdoc />
        public override bool TryPauseViewObservers()
        {
            if (base.TryPauseViewObservers())
            {
                if (m_UpdateObserver != null) GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);
                if (m_DeclarationHighlighter != null) GraphTool?.ObserverManager?.UnregisterObserver(m_DeclarationHighlighter);
                if (m_GraphVariablesObserver != null) GraphTool?.ObserverManager?.UnregisterObserver(m_GraphVariablesObserver);
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public override bool TryResumeViewObservers()
        {
            if (base.TryResumeViewObservers())
            {
                if (m_UpdateObserver != null) GraphTool?.ObserverManager?.RegisterObserver(m_UpdateObserver);
                if (m_DeclarationHighlighter != null) GraphTool?.ObserverManager?.RegisterObserver(m_DeclarationHighlighter);
                if (m_GraphVariablesObserver != null) GraphTool?.ObserverManager?.RegisterObserver(m_GraphVariablesObserver);
            }
            return false;
        }

        /// <inheritdoc />
        protected override void UnregisterModelObservers()
        {
            if (m_GraphLoadedObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_GraphLoadedObserver);
                m_GraphLoadedObserver = null;
            }
        }

        /// <inheritdoc />
        protected override void UnregisterViewObservers()
        {
            if (m_UpdateObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);
                m_UpdateObserver = null;
            }

            if (m_DeclarationHighlighter != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_DeclarationHighlighter);
                m_DeclarationHighlighter = null;
            }

            if (m_GraphVariablesObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_GraphVariablesObserver);
                m_GraphVariablesObserver = null;
            }
        }

        public ItemLibraryHelper GetItemLibraryHelper()
        {
            if (m_ItemLibraryHelper == null || m_ItemLibraryHelper.GraphModel != BlackboardRootViewModel.GraphModelState.GraphModel)
                m_ItemLibraryHelper = CreateItemLibraryHelper();

            return m_ItemLibraryHelper;
        }
        /// <summary>
        /// Creates a new <see cref="ItemLibraryHelper"/> associated with the <see cref="BlackboardView"/>.
        /// </summary>
        /// <returns>A new <see cref="ItemLibraryHelper"/>.</returns>
        /// <remarks>
        /// This helper is responsible for managing the item library, including retrieving the appropriate <see cref="Unity.GraphToolkit.ItemLibrary.Editor.IItemLibraryAdapter"/>,
        /// <see cref="IItemDatabaseProvider"/>, and <see cref="ILibraryFilterProvider"/> for items displayed in the blackboard. By default, this method calls <see cref="GraphViewEditorWindow.CreateItemLibraryHelper"/>.
        /// </remarks>
        protected virtual ItemLibraryHelper CreateItemLibraryHelper()
        {
            return (Window as GraphViewEditorWindow)?.CreateItemLibraryHelper(BlackboardRootViewModel.GraphModelState.GraphModel);
        }

        /// <inheritdoc />
        public IReadOnlyList<GraphElementModel> GetSelection()
        {
            return ViewSelection.GetSelection();
        }

        /// <summary>
        /// Rebuilds the whole blackboard UI.
        /// </summary>
        public override void BuildUITree()
        {
            if (Blackboard != null)
            {
                Blackboard.RemoveFromHierarchy();
                Blackboard.RemoveFromRootView();
                Blackboard = null;
            }

            if (BlackboardRootViewModel.BlackboardContentState.BlackboardModel.IsValid())
            {
                Blackboard = ModelViewFactory.CreateUI<Blackboard>(this, BlackboardRootViewModel.BlackboardContentState.BlackboardModel);
            }

            if (Blackboard != null)
            {
                Blackboard.ScrollView.scrollOffset = BlackboardRootViewModel.ViewState.ScrollOffset;
                Blackboard.ScrollView.horizontalScroller.slider.RegisterCallback<ChangeEvent<float>>(OnHorizontalScroll);
                Blackboard.ScrollView.verticalScroller.slider.RegisterCallback<ChangeEvent<float>>(OnVerticalScroll);
                Add(Blackboard);
            }
        }

        void OnHorizontalScroll(ChangeEvent<float> e)
        {
            using (var updater = BlackboardRootViewModel.ViewState.UpdateScope)
            {
                updater.SetScrollOffset(new Vector2(e.newValue, Blackboard.ScrollView.verticalScroller.slider.value));
            }
        }

        void OnVerticalScroll(ChangeEvent<float> e)
        {
            using (var updater = BlackboardRootViewModel.ViewState.UpdateScope)
            {
                updater.SetScrollOffset(new Vector2(Blackboard.ScrollView.horizontalScroller.slider.value, e.newValue));
            }
        }

        /// <inheritdoc />
        public override void Update()
        {
            if (panel == null)
                return;

            if (m_UpdateObserver == null)
                return;

            var updateSelection = false;
            var updateCollapse = false;
            var shouldRebuildTreeView = false;

            using (var selectionObservation = m_UpdateObserver.ObserveState(BlackboardRootViewModel.SelectionState))
            using (var contentStateObservation = m_UpdateObserver.ObserveState(BlackboardRootViewModel.BlackboardContentState))
            using (var graphModelObservation = m_UpdateObserver.ObserveState(BlackboardRootViewModel.GraphModelState))
            using (var viewModelObservation = m_UpdateObserver.ObserveState(BlackboardRootViewModel.ViewState))
            using (var highlighterObservation = m_UpdateObserver.ObserveState(BlackboardRootViewModel.HighlighterState))
            {
                if (graphModelObservation.UpdateType == UpdateType.Complete ||
                    viewModelObservation.UpdateType == UpdateType.Complete)
                {
                    // Another GraphModel loaded, or big changes in the GraphModel.
                    BuildUITree();
                }
                else
                {
                    var graphModel = BlackboardRootViewModel.GraphModelState.GraphModel;
                    if (graphModel == null)
                        return;

                    var deletedModels = new List<Hash128>();

                    GraphElementModel renamedModel = null;

                    if (contentStateObservation.UpdateType != UpdateType.None && Blackboard != null)
                    {
                        s_UIList.Add(Blackboard);
                    }

                    // Update variable/groups model UI.
                    if (graphModelObservation.UpdateType == UpdateType.Partial)
                    {
                        var gvChangeSet = BlackboardRootViewModel.GraphModelState.GetAggregatedChangeset(graphModelObservation.LastObservedVersion);

                        if (gvChangeSet != null)
                        {
                            deletedModels = gvChangeSet.DeletedModels.ToList();

                            // Adding/removing a variable/group should mark the parent group as changed.
                            // Updating the parent will add/remove the variable UI.
                            foreach (var (guid, hint) in gvChangeSet.ChangedModelsAndHints)
                            {
                                if (hint == ChangeHintList.Grouping)
                                {
                                    // When there is a structural change (added, removed, reordered items), Rebuild should be called.
                                    shouldRebuildTreeView = true;
                                }
                                guid.AppendAllViews(this, null, s_UIList);
                            }

                            if (shouldRebuildTreeView || gvChangeSet.ChangedModels.Contains(graphModel.GetSectionModel(GraphModel.DefaultSectionName).Guid))
                            {
                                Blackboard?.UpdateUIFromModel(UpdateFromModelVisitor.genericUpdateFromModelVisitor);
                            }

                            if (gvChangeSet.RenamedModel != null)
                            {
                                renamedModel = gvChangeSet.RenamedModel;
                            }
                        }
                    }

                    // Update collapsed state.
                    if (viewModelObservation.UpdateType == UpdateType.Partial)
                    {
                        var changeset = BlackboardRootViewModel.ViewState.GetAggregatedChangeset(viewModelObservation.LastObservedVersion);
                        foreach (var guid in changeset.ChangedModels)
                        {
                            guid.AppendAllViews(this, null, s_UIList);
                        }

                        if (changeset.HasAdditionalChange(BlackboardViewStateComponent.Changeset.AdditionalChangesEnum.ScrollOffset) &&
                            Blackboard != null)
                        {
                            s_UIList.Add(Blackboard);
                        }

                        updateCollapse = true;
                    }

                    // Update selection.
                    if (selectionObservation.UpdateType != UpdateType.None)
                    {
                        var selChangeSet = BlackboardRootViewModel.SelectionState.GetAggregatedChangeset(selectionObservation.LastObservedVersion);
                        if (selChangeSet != null)
                        {
                            var selectionChangedModels = selChangeSet.ChangedModels.Select(graphModel.GetModel).Where(m => m != null);
                            foreach (var changedModel in selectionChangedModels)
                            {
                                if (changedModel is IGroupItemModel)
                                {
                                    changedModel.AppendAllViews(this, null, s_SelectionUIList);
                                }
                            }
                        }

                        updateSelection = true;
                    }

                    // Update highlighting.
                    if (highlighterObservation.UpdateType == UpdateType.Complete)
                    {
                        for (var i = 0; i < graphModel.VariableDeclarations.Count; i++)
                        {
                            var declaration = graphModel.VariableDeclarations[i];
                            declaration.AppendAllViews(this, null, s_UIList);
                        }
                    }
                    else if (highlighterObservation.UpdateType == UpdateType.Partial)
                    {
                        var changedModels = GraphTool.HighlighterState.GetAggregatedChangeset(highlighterObservation.LastObservedVersion);
                        foreach (var guid in changedModels.ChangedModels)
                        {
                            guid.AppendAllViews(this, null, s_UIList);
                        }
                    }

                    foreach (var ui in s_UIList.Distinct())
                    {
                        // Check ui.View != null because ui.UpdateFromModel can remove other ui from the view.
                        if (ui is ModelView modelView && ui.RootView != null && !deletedModels.Contains(modelView.Model.Guid))
                        {
                            ui.UpdateView(UpdateFromModelVisitor.genericUpdateFromModelVisitor);
                        }
                    }

                    foreach (var ui in s_SelectionUIList.Distinct())
                    {
                        if (ui is ModelView && ui.RootView != null)
                        {
                            UpdateSelectionVisitor.Visitor.Update(ui);
                        }
                    }

                    s_UIList.Clear();
                    s_SelectionUIList.Clear();

                    if (renamedModel != null)
                    {
                        Blackboard.DoRefresh();

                        renamedModel.AppendAllViews(this, null, s_UIList);
                        foreach (var ui in s_UIList)
                        {
                            if (ui is ModelView modelView)
                            {
                                modelView.ActivateRename();
                            }
                        }

                        s_UIList.Clear();
                    }
                }
            }
            if (updateCollapse)
                Blackboard.UpdateCollapse();
            if (updateSelection)
                Blackboard.UpdateSelection();
        }

        internal IGroupItemModel CreateGroupFromSelection(IGroupItemModel model)
        {
            var selectedItems = GetSelection().OfType<IGroupItemModel>().Where(t => t.GetSection() == model.GetSection() && t.ParentGroup is GroupModel).ToList();
            selectedItems.Add(model);

            selectedItems.Sort(GroupItemOrderComparer.Default);

            int index = model.ParentGroup.Items.IndexOf(selectedItems.First(t => !selectedItems.Contains(t.ParentGroup)));

            while (selectedItems.Contains(model.ParentGroup)) // make sure whe don't move to a group within the selection
            {
                model = model.ParentGroup;
            }

            if (model.ParentGroup is GroupModel parentGroup)
            {
                //Look up for the next non selected item after me
                IGroupItemModel prevItem = null;
                for (int i = index - 1; i >= 0; --i)
                {
                    prevItem = model.ParentGroup.Items[i];
                    if (!selectedItems.Contains(prevItem))
                        break;
                }

                Dispatch(new BlackboardGroupCreateCommand(parentGroup, prevItem, null, selectedItems));

                return model;
            }

            return null;
        }

        /// <summary>
        /// Handles the <see cref="KeyDownEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnRenameKeyDown(KeyDownEvent e)
        {
            if (ModelView.IsRenameKey(e))
            {
                GraphElementModel lastSelectedItem = GetLastRenamableModel(GetSelection());
                if (lastSelectedItem == null || !lastSelectedItem.IsRenamable())
                    return;

                var uiList = new List<ChildView>();
                lastSelectedItem.AppendAllViews(this, null, uiList);
                if (uiList.Any(ui => (ui as ModelView)?.Rename() ?? false))
                {
                    e.StopPropagation();
                }
            }
        }

        static GraphElementModel GetLastRenamableModel(IReadOnlyList<GraphElementModel> models)
        {
            for (int i = models.Count - 1; i >= 0; --i)
            {
                if (models[i].IsRenamable())
                    return models[i];
            }

            return null;
        }
    }
}
