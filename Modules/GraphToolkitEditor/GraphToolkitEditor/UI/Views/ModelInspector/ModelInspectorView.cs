// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.GraphToolkit.CSO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A view to display the model inspector.
    /// </summary>
    [UnityRestricted]
    internal class ModelInspectorView : RootView, IHasItemLibrary
    {
        /// <summary>
        /// Determines if a field should be displayed in the node options section of a node.
        /// </summary>
        /// <param name="f">The field to inspect.</param>
        /// <returns>True if a field should be displayed in the node options section of a node. False otherwise.</returns>
        public static bool NodeOptionsFilterForNode(FieldInfo f)
        {
            if (NodeOptionsFilter(f))
            {
                var nodeOption = f.GetCustomAttribute<NodeOptionAttribute>();
                return nodeOption != null && !nodeOption.ShowInInspectorOnly;
            }

            return false;
        }

        /// <summary>
        /// Determines if a field should be displayed in the node options section of the inspector.
        /// </summary>
        /// <param name="f">The field to inspect.</param>
        /// <returns>True if a field should be displayed in the node options section of the inspector. False otherwise.</returns>
        public static bool NodeOptionsFilter(FieldInfo f)
        {
            return SerializedFieldsInspector.CanBeInspected(f) && f.CustomAttributes.HasAny(a => a.AttributeType == typeof(NodeOptionAttribute));
        }

        /// <summary>
        /// Determines if a field should be displayed in the advanced settings section of the inspector.
        /// </summary>
        /// <param name="f">The field to inspect.</param>
        /// <returns>True if a field should be displayed in the advanced settings section of the inspector. False otherwise.</returns>
        public static bool AdvancedSettingsFilter(FieldInfo f)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return SerializedFieldsInspector.CanBeInspected(f) && f.CustomAttributes.All(a => a.AttributeType != typeof(NodeOptionAttribute));
#pragma warning restore UA2001
        }

        static readonly List<ChildView> k_UpdateAllUIs = new();

        public new static readonly string ussClassName = "model-inspector-view";
        public static readonly string titleUssClassName = ussClassName.WithUssElement(GraphElementHelper.titleName);
        public static readonly string containerUssClassName = ussClassName.WithUssElement(GraphElementHelper.containerName);

        public static readonly string firstChildUssClassName = "first-child";

        /// <summary>
        /// The <see cref="Unity.GraphToolkit.Editor.ItemLibraryHelper"/> of the <see cref="ModelInspectorView"/>.
        /// </summary>
        protected ItemLibraryHelper m_ItemLibraryHelper;

        Label m_Title;
        BaseModelPropertyField m_TitleField;

        ScrollView m_InspectorContainer;

        StateObserver m_LoadedGraphObserver;
        StateObserver m_TransitionsLoadedGraphObserver;
        ModelViewUpdater m_UpdateObserver;

        List<ChildView> m_SectionViews;

        bool m_DisplayingTransitions;
        bool m_IgnoreScrollNotifications;

        static List<ViewUpdateVisitor> s_CollapsibleUpdateVisitors;

        /// <summary>
        /// The views for the sections in the inspector.
        /// </summary>
        protected IReadOnlyList<ChildView> SectionViews => m_SectionViews;

        /// <summary>
        /// The <see cref="StateObserver"/> used to detect selection changes.
        /// </summary>
        protected StateObserver m_SelectionObserver;

        /// <summary>
        /// The <see cref="Unity.GraphToolkit.Editor.ModelInspectorViewModel"/> of the inspector.
        /// </summary>
        public ModelInspectorViewModel ModelInspectorViewModel => (ModelInspectorViewModel)Model;

        /// <summary>
        /// Creates a new instance of the <see cref="ModelInspectorView"/> class.
        /// </summary>
        /// <param name="window">The <see cref="GraphViewEditorWindow"/> associated with this view.</param>
        /// <param name="graphTool">The tool hosting this view.</param>
        /// <param name="model">The model for the view.</param>
        /// <param name="typeHandleInfos">A <see cref="TypeHandleInfos"/> to use for this view. If null a new one will be created.</param>
        public ModelInspectorView(EditorWindow window, GraphTool graphTool, ModelInspectorViewModel model, TypeHandleInfos typeHandleInfos = null)
            : base(window, graphTool, typeHandleInfos)
        {
            Model = model;

            this.AddPackageStylesheet("ModelInspector.uss");
            AddToClassList(ussClassName);

            m_SectionViews = new List<ChildView>();
        }

        /// <inheritdoc />
        protected override void RegisterCommandHandlers(CommandHandlerRegistrar registrar)
        {
            registrar.AddStateComponent(GraphTool.UndoState);
            registrar.AddStateComponent(ModelInspectorViewModel.GraphModelState);
            registrar.AddStateComponent(ModelInspectorViewModel.ModelInspectorState);
            registrar.AddStateComponent(ModelInspectorViewModel.TransitionInspectorState);

            registrar.RegisterDefaultCommandHandler<SetInspectedGraphModelFieldCommand>();

            registrar.RegisterDefaultCommandHandler<SetInspectedModelFieldCommand>();
            registrar.RegisterDefaultCommandHandler<ChangeElementColorCommand>();

            registrar.RegisterDefaultCommandHandler<UpdateConstantValueCommand>();
            registrar.RegisterDefaultCommandHandler<UpdateConstantsValueCommand>();

            registrar.RegisterDefaultCommandHandler<RenameElementsCommand>();
            registrar.RegisterDefaultCommandHandler<ChangeVariableScopeCommand>();
            registrar.RegisterDefaultCommandHandler<ChangeVariableModifiersCommand>();
            registrar.RegisterDefaultCommandHandler<ChangeVariableDisplaySettingsCommand>();
            registrar.RegisterDefaultCommandHandler<UpdateTooltipCommand>();

            registrar.RegisterDefaultCommandHandler<CollapseInspectorSectionCommand>();
            registrar.RegisterDefaultCommandHandler<ExpandExpandablePortInInspectorCommand>();

            registrar.RegisterDefaultCommandHandler<ChangeVariableTypeCommand>();

            registrar.RegisterDefaultCommandHandler<CollapseTransitionsCommand>();
            registrar.RegisterDefaultCommandHandler<AddTransitionCommand>();
            registrar.RegisterDefaultCommandHandler<RemoveTransitionCommand>();
            registrar.RegisterDefaultCommandHandler<RemoveTransitionElementsCommand>();
            registrar.RegisterDefaultCommandHandler<MoveTransitionCommand>();
            registrar.RegisterDefaultCommandHandler<AddConditionCommand>();
            registrar.RegisterDefaultCommandHandler<DeleteConditionsCommand>();
            registrar.RegisterDefaultCommandHandler<DuplicateConditionsCommand>();
            registrar.RegisterDefaultCommandHandler<MoveConditionCommand>();
            registrar.RegisterDefaultCommandHandler<SetGroupConditionOperationCommand>();

            // The DeleteElementsCommand require the SelectionState from the GraphView.
            registrar.AddStateComponent(((GraphViewEditorWindow)Window).GraphView.GraphViewModel.SelectionState);
            registrar.RegisterDefaultCommandHandler<DeleteElementsCommand>();
        }

        /// <inheritdoc />
        protected override void OnFocus(FocusInEvent e)
        {
            base.OnFocus(e);

            // When the focus is on the graph inspector, we still want the graph view to have the focused uss class.
            var graphView = (Window as GraphViewEditorWindow)?.GraphView;
            graphView?.UpdateBordersOnFocus(true);
        }

        /// <inheritdoc />
        protected override void OnLostFocus(FocusOutEvent e)
        {
            base.OnLostFocus(e);

            // If the element that gains the focus is not a graph element and is not part of the graph inspector, we remove the graph view's focused uss class.
            if (!m_OnFocusCalled && e.relatedTarget is not GraphElement && e.relatedTarget is not ModelInspectorView)
            {
                var graphView = (Window as GraphViewEditorWindow)?.GraphView;
                if (graphView != null && graphView.ClassListContains(focusedViewUssClassName))
                    graphView.UpdateBordersOnFocus(false);
            }
        }

        void UpdateSelectionObserver(IState state, IStateComponent stateComponent)
        {
            if (Window == null || !Window.hasFocus)
                return;

            if (stateComponent is SelectionStateComponent)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_SelectionObserver);
                m_SelectionObserver = null;

                BuildSelectionObserver();
            }
        }


        /// <inheritdoc />
        public override IReadOnlyList<ViewUpdateVisitor> GetChildViewUpdaters(ChildView childView)
        {
            if (childView is ICollapsibleContainer)
            {
                if (s_CollapsibleUpdateVisitors == null)
                {
                    s_CollapsibleUpdateVisitors = new List<ViewUpdateVisitor>();
                    s_CollapsibleUpdateVisitors.Add(UpdateFromModelVisitor.genericUpdateFromModelVisitor);

                    s_CollapsibleUpdateVisitors.Add(new UpdateCollapsibleVisitor());
                }

                return s_CollapsibleUpdateVisitors;
            }
            return base.GetChildViewUpdaters(childView);
        }

        /// <summary>
        /// Instantiates and registers the <see cref="StateObserver"/> for the ModelInspectorView.
        /// </summary>
        protected virtual void BuildSelectionObserver()
        {
            if (m_SelectionObserver == null && GraphTool != null)
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var selectionStates = GraphTool.State.AllStateComponents.OfType<SelectionStateComponent>();
#pragma warning restore UA2001
                m_SelectionObserver = new InspectorSelectionObserver(GraphTool.ToolState, ModelInspectorViewModel.GraphModelState,
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    selectionStates.ToList(), ModelInspectorViewModel.ModelInspectorState);
#pragma warning restore UA2001

                GraphTool?.ObserverManager?.RegisterObserver(m_SelectionObserver);
            }
        }

        /// <inheritdoc />
        protected override void RegisterModelObservers()
        {
            if (m_LoadedGraphObserver == null)
            {
                m_LoadedGraphObserver = new GraphLoadedObserver<ModelInspectorStateComponent.StateUpdater>(GraphTool.ToolState, ModelInspectorViewModel.ModelInspectorState);
                GraphTool.ObserverManager.RegisterObserver(m_LoadedGraphObserver);
            }
            if (m_TransitionsLoadedGraphObserver == null)
            {
                m_TransitionsLoadedGraphObserver = new GraphLoadedObserver<TransitionInspectorStateComponent.StateUpdater>(GraphTool.ToolState, ModelInspectorViewModel.TransitionInspectorState);
                GraphTool.ObserverManager.RegisterObserver(m_TransitionsLoadedGraphObserver);
            }
        }

        /// <inheritdoc />
        protected override void RegisterViewObservers()
        {
            if (m_UpdateObserver == null)
            {
                m_UpdateObserver = new ModelViewUpdater(this, ModelInspectorViewModel.ModelInspectorState, ModelInspectorViewModel.GraphModelState, ModelInspectorViewModel.TransitionInspectorState);
                GraphTool?.ObserverManager?.RegisterObserver(m_UpdateObserver);
            }

            BuildSelectionObserver();

            if (GraphTool != null)
                GraphTool.State.OnStateComponentListModified += UpdateSelectionObserver;
        }

        public override bool TryPauseViewObservers()
        {
            if (base.TryPauseViewObservers())
            {
                if (m_UpdateObserver != null) GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);
                if (m_SelectionObserver != null) GraphTool?.ObserverManager?.UnregisterObserver(m_SelectionObserver);
                return true;
            }
            return false;
        }

        public override bool TryResumeViewObservers()
        {
            if (base.TryResumeViewObservers())
            {
                if (m_UpdateObserver != null) GraphTool?.ObserverManager?.RegisterObserver(m_UpdateObserver);
                if (m_SelectionObserver != null) GraphTool?.ObserverManager?.RegisterObserver(m_SelectionObserver);
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        protected override void UnregisterModelObservers()
        {
            if (GraphTool != null)
            {
                GraphTool.ObserverManager?.UnregisterObserver(m_LoadedGraphObserver);
                m_LoadedGraphObserver = null;
                GraphTool.ObserverManager?.UnregisterObserver(m_TransitionsLoadedGraphObserver);
                m_TransitionsLoadedGraphObserver = null;
            }
        }

        /// <inheritdoc />
        protected override void UnregisterViewObservers()
        {
            if (GraphTool != null)
            {
                if (m_UpdateObserver != null)
                {
                    GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);
                    m_UpdateObserver = null;
                }

                if (m_SelectionObserver != null)
                {
                    GraphTool?.ObserverManager?.UnregisterObserver(m_SelectionObserver);
                    m_SelectionObserver = null;
                }

                if (GraphTool?.State != null)
                {
                    GraphTool.State.OnStateComponentListModified -= UpdateSelectionObserver;
                }
            }
        }

        void RemoveAllUI()
        {
            if (m_InspectorContainer == null)
                return;

            m_Title?.RemoveFromHierarchy();
            m_TitleField?.RemoveFromHierarchy();

            for (var i = m_SectionViews.Count - 1; i >= 0; i--)
            {
                var element = m_SectionViews[i];
                element.RemoveFromRootView();
            }

            m_SectionViews.Clear();

            m_InspectorContainer.RemoveFromHierarchy();
            m_InspectorContainer = null;
        }

        void OnHorizontalScroll(ChangeEvent<float> e)
        {
            if (m_IgnoreScrollNotifications)
                return;
            using (var updater = ModelInspectorViewModel.ModelInspectorState.UpdateScope)
            {
                updater.SetScrollOffset(new Vector2(e.newValue, m_InspectorContainer.verticalScroller.slider.value));
            }
        }

        void OnVerticalScroll(ChangeEvent<float> e)
        {
            if (m_IgnoreScrollNotifications)
                return;
            using (var updater = ModelInspectorViewModel.ModelInspectorState.UpdateScope)
            {
                updater.SetScrollOffset(new Vector2(m_InspectorContainer.horizontalScroller.slider.value, e.newValue));
            }
        }

        /// <inheritdoc />
        public override void BuildUITree()
        {
            RemoveAllUI();

            // We need to recreate the m_InspectorContainer to be able to set the scroll offset to any value.
            // If we reuse the existing m_InspectorContainer, offset will be clamped according to the last layout of the scrollView.
            m_InspectorContainer = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            m_InspectorContainer.AddToClassList(containerUssClassName);

            if (ModelInspectorViewModel == null)
            {
                m_Title.text = "";
                return;
            }

            m_InspectorContainer.scrollOffset = ModelInspectorViewModel.ModelInspectorState.ScrollOffset;
            m_InspectorContainer.horizontalScroller.slider.RegisterCallback<ChangeEvent<float>>(OnHorizontalScroll);
            m_InspectorContainer.verticalScroller.slider.RegisterCallback<ChangeEvent<float>>(OnVerticalScroll);

            var inspectorModel = ModelInspectorViewModel.ModelInspectorState.GetInspectorModel();
            if (inspectorModel != null)
            {
                if (m_Title != null)
                    m_Title.text = inspectorModel.Title;

                bool isFirst = true;
                ModelView sectionUI = null;
                foreach (var section in inspectorModel.Sections)
                {
                    var context = new InspectorSectionContext(section);

                    // Create the inspector, with all the field editors.
                    var elementBuilder = new ElementBuilder();
                    elementBuilder.View = this;
                    elementBuilder.Context = context;
                    var sectionInspector = CreateSectionCache.CallCreateSection(elementBuilder, ModelInspectorViewModel.ModelInspectorState.InspectedModels);

                    if (sectionInspector != null)
                    {
                        m_SectionViews.Add(sectionInspector);
                    }

                    var modelInspector = sectionInspector as ModelInspector;

                    if (isFirst)
                    {
                        bool useStandardTitle = false;
                        if (modelInspector?.PartList.GetPart(ModelInspector.fieldsPartName) is SerializedFieldsInspector fieldsInspector)
                        {
                            m_TitleField = fieldsInspector.GetTitleField(ModelInspectorViewModel.ModelInspectorState.InspectedModels);
                            if (m_TitleField != null)
                            {
                                Add(m_TitleField);
                                m_TitleField.UpdateDisplayedValue();
                            }
                            else
                            {
                                useStandardTitle = true;
                            }
                        }
                        else
                        {
                            useStandardTitle = true;
                        }

                        if (useStandardTitle)
                        {
                            if (m_Title == null)
                            {
                                m_Title = new Label();
                                m_Title.AddToClassList(titleUssClassName);
                            }
                            Add(m_Title);
                        }
                    }

                    // If the sectionInspector is empty, do not show it and do not create a section for it.
                    if (sectionInspector == null || (modelInspector != null && modelInspector.IsEmpty()))
                    {
                        isFirst = false;
                        continue;
                    }

                    // Create a section to wrap the sectionInspector. This could be a collapsible section, for example.
                    sectionUI = ModelViewFactory.CreateUI<ModelView>(this, section);
                    if (sectionUI != null)
                    {
                        if (isFirst)
                        {
                            // The firstChild class is useful to style the first element differently,
                            // for example to avoid a double border or a missing border between sibling elements.
                            sectionUI.AddToClassList(firstChildUssClassName);
                            isFirst = false;
                        }

                        // Add the section to the view, in the side panel, and add the sectionInspector to the section.
                        m_InspectorContainer.Add(sectionUI);
                        sectionUI.Add(sectionInspector);
                    }
                    else
                    {
                        // If there was no section created, let's put the sectionInspector directly in the side panel.
                        m_InspectorContainer.Add(sectionInspector);
                    }
                }

                sectionUI?.AddToClassList("last-child");
            }
            else if (ModelInspectorViewModel.ModelInspectorState.InspectedModels.Count > 1 && m_Title != null)
            {
                m_Title.text = "Multiple selection";
            }

            if (m_TitleField == null && m_Title == null)
            {
                m_Title = new Label();
                m_Title.AddToClassList(titleUssClassName);
                Add(m_Title);
            }

            Add(m_InspectorContainer);

            m_DisplayingTransitions = false;

            if (inspectorModel != null)
            {
                foreach (var model in ModelInspectorViewModel.ModelInspectorState.InspectedModels)
                {
                    if (model is TransitionSupportModel || model is StateModel)
                    {
                        m_DisplayingTransitions = true;
                        break;
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void Update()
        {
            if (panel == null)
                return;

            if (m_UpdateObserver == null)
                return;

            using (var modelStateObservation = m_UpdateObserver.ObserveState(ModelInspectorViewModel.GraphModelState))
            using (var inspectorStateObservation = m_UpdateObserver.ObserveState(ModelInspectorViewModel.ModelInspectorState))
            {
                var rebuildType = inspectorStateObservation.UpdateType.Combine(modelStateObservation.UpdateType);

                if (rebuildType == UpdateType.Complete)
                {
                    BuildUITree();
                }
                else
                {
                    if (ModelInspectorViewModel.ModelInspectorState.InspectedModels.HasAny() && modelStateObservation.UpdateType == UpdateType.Partial)
                    {
                        var changeset = ModelInspectorViewModel.GraphModelState.GetAggregatedChangeset(modelStateObservation.LastObservedVersion);

                        PartialUpdate(changeset);
                    }

                    if (inspectorStateObservation.UpdateType == UpdateType.Partial)
                    {
                        var inspectorModel = ModelInspectorViewModel.ModelInspectorState.GetInspectorModel();
                        if (inspectorModel != null)
                        {
                            m_Title.text = inspectorModel.Title;

                            var changeSet = ModelInspectorViewModel.ModelInspectorState.GetAggregatedChangeset(inspectorStateObservation.LastObservedVersion);
                            if (changeSet != null)
                            {
                                foreach (var guid in changeSet.ChangedModels)
                                {
                                    guid.AppendAllViews(this, null, k_UpdateAllUIs);
                                    foreach (var ui in k_UpdateAllUIs)
                                    {
                                        ui.UpdateView(UpdateFromModelVisitor.genericUpdateFromModelVisitor);
                                    }

                                    k_UpdateAllUIs.Clear();
                                }

                                if (changeSet.PortUniqueNameChanged.Count > 0)
                                {
                                    foreach (var sectionView in SectionViews)
                                    {
                                        if (sectionView is ModelInspector inspector)
                                        {
                                            foreach (var part in inspector.PartList.Parts)
                                            {
                                                if (part is NodePortsInspector portsInspector)
                                                {
                                                    portsInspector.RefreshExpandablePortsFieldCollapse(changeSet.PortUniqueNameChanged);
                                                }
                                            }
                                        }
                                    }
                                }

                                if (changeSet.HasAdditionalChange(ModelInspectorStateComponent.Changeset.AdditionalChangesEnum.ScrollOffset))
                                {
                                    m_IgnoreScrollNotifications = true;
                                    m_InspectorContainer.scrollOffset = ModelInspectorViewModel.ModelInspectorState.ScrollOffset;
                                    m_IgnoreScrollNotifications = false;
                                }
                            }
                        }
                    }
                }
            }
            UpdateCollapsible();
        }

        protected virtual void PartialUpdate(GraphModelStateComponent.Changeset changeset)
        {
            // If we display transitions, there will be changes on transitions and conditions which wouldn't be updated by the optimized partial update
            if (m_DisplayingTransitions)
            {
                bool inspectedModelChanged = false;

                foreach (var sectionView in SectionViews)
                {
                    if (sectionView is StateTransitionsInspector inspector)
                    {
                        inspector.Update(changeset);
                    }
                }
                foreach (var guid in changeset.ChangedModels)
                {
                    inspectedModelChanged = true;
                    ViewForModel.AppendAllViews(guid, this, null, k_UpdateAllUIs);

                    var visitor = new UpdateFromModelVisitor(changeset.ChangedModelsAndHints[guid]);
                    foreach (var ui in k_UpdateAllUIs)
                    {
                        ui.UpdateView(visitor);
                    }
                    k_UpdateAllUIs.Clear();
                }

                if (inspectedModelChanged)
                {
                    UpdateTitle();
                }
            }
            else
            {
                bool inspectedModelChanged = false;
                foreach (var guid in changeset.ChangedModels)
                {
                    inspectedModelChanged = IsChangedModelVisible(guid);
                    if (inspectedModelChanged)
                        break;
                }

                if (inspectedModelChanged)
                {
                    UpdateTitle();

                    foreach (var inspectedModel in ModelInspectorViewModel.ModelInspectorState.InspectedModels)
                    {
                        inspectedModel.AppendAllViews(this, null, k_UpdateAllUIs);

                        if (inspectedModel is IHasDeclarationModel hasDeclaration)
                            hasDeclaration.DeclarationModel.AppendAllViews(this, null, k_UpdateAllUIs);
                        foreach (var ui in k_UpdateAllUIs)
                        {
                            ui.UpdateView(UpdateFromModelVisitor.genericUpdateFromModelVisitor);
                        }
                        k_UpdateAllUIs.Clear();
                    }
                }
            }
        }

        void UpdateCollapsible()
        {
            using var collapsedStateObservation = m_UpdateObserver.ObserveState(ModelInspectorViewModel.TransitionInspectorState);

            if (collapsedStateObservation.UpdateType == UpdateType.Partial)
            {
                var collapsedChangeset = ModelInspectorViewModel.TransitionInspectorState.GetAggregatedChangeset(collapsedStateObservation.LastObservedVersion);
                ISet<Hash128> changedModels = collapsedChangeset.ChangedModels;
                var visitor = new UpdateCollapsibleVisitor();

                var childViews = new List<ChildView>();
                foreach (var changedModel in changedModels)
                {
                    ViewForModel.AppendAllViews(changedModel, this, t => t is ICollapsibleContainer, childViews);
                    foreach (var childView in childViews)
                    {
                        childView.UpdateView(visitor);
                    }
                    childViews.Clear();
                }
            }
            else if (collapsedStateObservation.UpdateType == UpdateType.Complete)
            {
                var visitor = new UpdateCollapsibleVisitor();

                List<TransitionSupportModel> transitionSupportModels = new List<TransitionSupportModel>();
                foreach (var model in ModelInspectorViewModel.ModelInspectorState.InspectedModels)
                {
                    if (model is TransitionSupportModel transitionSupportModel)
                    {
                        transitionSupportModels.Add(transitionSupportModel);
                    }
                    else if (model is StateModel stateModel)
                    {
                        foreach (var wire in stateModel.GetConnectedWires())
                        {
                            if (wire is TransitionSupportModel tsm)
                            {
                                transitionSupportModels.Add(tsm);
                            }
                        }
                    }
                }

                var childViews = new List<ChildView>();
                foreach (var transitionSupportModel in transitionSupportModels)
                {
                    ViewForModel.AppendAllViews(transitionSupportModel, this, t => t is ICollapsibleContainer, childViews);
                    foreach (var transitionModel in transitionSupportModel.Transitions)
                    {
                        ViewForModel.AppendAllViews(transitionModel, this, t => t is ICollapsibleContainer, childViews);
                    }
                    foreach (var childView in childViews)
                    {
                        childView.UpdateView(visitor);
                    }
                    childViews.Clear();
                }
            }
        }

        public void UpdateTitle()
        {
            var inspectorModel = ModelInspectorViewModel.ModelInspectorState.GetInspectorModel();
            if (inspectorModel != null)
            {
                if (m_TitleField != null)
                {
                    m_TitleField.UpdateDisplayedValue();
                }
                else
                    m_Title.text = inspectorModel.Title;
            }
        }

        protected virtual bool IsChangedModelVisible(Hash128 guid)
        {
            foreach (var inspectedModel in ModelInspectorViewModel.ModelInspectorState.InspectedModels)
            {
                if (inspectedModel.Guid == guid || (inspectedModel is IHasDeclarationModel hasDeclaration && hasDeclaration.DeclarationModel.Guid == guid))
                {
                    return true;
                }

                if (inspectedModel is PortNodeModel portNodeModel)
                {
                    foreach (var portModel in portNodeModel.GetPorts())
                    {
                        if (portModel.Guid == guid)
                        {
                            return true;
                        }
                    }

                    if (portNodeModel is InputOutputPortsNodeModel inoutPortsNodeModel)
                    {
                        foreach (var option in inoutPortsNodeModel.NodeOptions)
                        {
                            if (option.PortModel.Guid == guid)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Retrieves the Unity Style Sheet (USS) classes that define the icon for a given <see cref="GraphElementModel"/>.
        /// </summary>
        /// <param name="model">The <see cref="GraphElementModel"/>.</param>
        /// <returns>The USS classes that define the icon for a given model.</returns>
        /// <remarks>
        /// This method retrieves the USS classes used to define the icon for a specified <see cref="GraphElementModel"/>. The icon's USS classes depend on the type of
        /// <see cref="GraphElementModel"/>, which allows for different icon styles or visual representations based on the model's type. Override this method to customize
        /// how different types of <see cref="GraphElementModel"/> are handled.
        /// </remarks>
        public virtual IReadOnlyList<string> GetIconUssClassesForModel(GraphElementModel model)
        {
            if (model == null)
                return Array.Empty<string>();
            switch (model)
            {
                case ConstantNodeModel constantNode:
                    return GetIconUssClassesForType(constantNode.Value.GetTypeHandle());
                case VariableNodeModel variableNode:
                    return GetIconUssClassesForType(((VariableDeclarationModelBase)variableNode.DeclarationModel).DataType);
                case VariableDeclarationModelBase variable:
                    return GetIconUssClassesForType(variable.DataType);
                case NodeModel node:
                    return new[] { "ge-icon".WithUssModifier(node.IconTypeString) };
            }

            return null;
        }

        /// <summary>
        /// Returns the default uss classes for an icon given a <see cref="TypeHandle"/>.
        /// </summary>
        /// <param name="type">The TypeHandle.</param>
        /// <returns>The default uss classes for an icon given a <see cref="TypeHandle"/>.</returns>
        public IReadOnlyList<string> GetIconUssClassesForType(TypeHandle type)
        {
            return new[] { "ge-icon-variable",
                GraphElementHelper.iconDataTypeClassPrefix + TypeHandleInfos.GetUssName(type),
                GraphElementHelper.iconDataTypeClassPrefix + TypeHandleInfos.GetAdditionalUssName(type)
            };
        }

        /// <summary>
        /// Creates a new <see cref="ItemLibraryHelper"/> associated with the <see cref="ModelInspectorView"/>.
        /// </summary>
        /// <returns>A new <see cref="ItemLibraryHelper"/>.</returns>
        protected virtual ItemLibraryHelper CreateItemLibraryHelper()
        {
            return (Window as GraphViewEditorWindow)?.CreateItemLibraryHelper(ModelInspectorViewModel.GraphModelState.GraphModel);
        }

        /// <summary>
        /// Retrieves the <see cref="ItemLibraryHelper"/> associated with the <see cref="ModelInspectorView"/>.
        /// </summary>
        /// <returns>The <see cref="ItemLibraryHelper"/>.</returns>
        /// <remarks>
        /// 'GetItemLibraryHelper' retrieves the <see cref="ItemLibraryHelper"/> if it exists.
        /// If it does not exist or the associated <see cref="GraphModel"/> has changed, the method creates and returns a new instance.
        /// </remarks>
        public ItemLibraryHelper GetItemLibraryHelper()
        {
            if (m_ItemLibraryHelper == null || m_ItemLibraryHelper.GraphModel != ModelInspectorViewModel.GraphModelState.GraphModel)
                m_ItemLibraryHelper = CreateItemLibraryHelper();

            return m_ItemLibraryHelper;
        }
    }
}
