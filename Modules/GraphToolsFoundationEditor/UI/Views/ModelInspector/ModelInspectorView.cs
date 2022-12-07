// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.CommandStateObserver;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A view to display the model inspector.
    /// </summary>
    class ModelInspectorView : RootView, IMultipleModelPartContainer
    {
        /// <summary>
        /// Determines if a field should be displayed in the basic settings section of the inspector.
        /// </summary>
        /// <param name="f">The field to inspect.</param>
        /// <returns>True if a field should be displayed in the basic settings section of the inspector. False otherwise.</returns>
        public static bool BasicSettingsFilter(FieldInfo f)
        {
            return SerializedFieldsInspector.CanBeInspected(f) && f.CustomAttributes.Any(a => a.AttributeType == typeof(ModelSettingAttribute));
        }

        /// <summary>
        /// Determines if a field should be displayed in the advanced settings section of the inspector.
        /// </summary>
        /// <param name="f">The field to inspect.</param>
        /// <returns>True if a field should be displayed in the advanced settings section of the inspector. False otherwise.</returns>
        public static bool AdvancedSettingsFilter(FieldInfo f)
        {
            return SerializedFieldsInspector.CanBeInspected(f) && f.CustomAttributes.All(a => a.AttributeType != typeof(ModelSettingAttribute));
        }

        static readonly List<ModelView> k_UpdateAllUIs = new List<ModelView>();

        public new static readonly string ussClassName = "model-inspector-view";
        public static readonly string titleUssClassName = ussClassName.WithUssElement("title");
        public static readonly string containerUssClassName = ussClassName.WithUssElement("container");

        public static readonly string firstChildUssClassName = "first-child";

        Label m_Title;
        ScrollView m_InspectorContainer;

        ModelInspectorGraphLoadedObserver m_LoadedGraphObserver;
        ModelViewUpdater m_UpdateObserver;

        /// <summary>
        /// The <see cref="StateObserver"/> used to detect selection changes.
        /// </summary>
        protected StateObserver m_SelectionObserver;

        public ModelInspectorViewModel ModelInspectorViewModel => (ModelInspectorViewModel)Model;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelInspectorView"/> class.
        /// </summary>
        /// <param name="window">The <see cref="GraphViewEditorWindow"/> associated with this view.</param>
        /// <param name="parentGraphView">The <see cref="GraphView"/> linked to this inspector.</param>
        public ModelInspectorView(EditorWindow window, GraphView parentGraphView)
        : base(window, parentGraphView.GraphTool)
        {
            Model = new ModelInspectorViewModel(parentGraphView);

            this.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SetInspectedGraphModelFieldCommand>(SetInspectedGraphModelFieldCommand.DefaultCommandHandler, GraphTool.UndoStateComponent, ModelInspectorViewModel.GraphModelState);

            this.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SetInspectedGraphElementModelFieldCommand>(SetInspectedGraphElementModelFieldCommand.DefaultCommandHandler, GraphTool.UndoStateComponent, ModelInspectorViewModel.GraphModelState);
            this.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, ChangeElementColorCommand>(ChangeElementColorCommand.DefaultCommandHandler, GraphTool.UndoStateComponent, ModelInspectorViewModel.GraphModelState);

            this.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, UpdateConstantValueCommand>(UpdateConstantValueCommand.DefaultCommandHandler, GraphTool.UndoStateComponent, ModelInspectorViewModel.GraphModelState);
            this.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, UpdateConstantsValueCommand>(UpdateConstantsValueCommand.DefaultCommandHandler, GraphTool.UndoStateComponent, ModelInspectorViewModel.GraphModelState);

            this.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, ExposeVariableCommand>(ExposeVariableCommand.DefaultCommandHandler, GraphTool.UndoStateComponent, ModelInspectorViewModel.GraphModelState);
            this.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, UpdateTooltipCommand>(UpdateTooltipCommand.DefaultCommandHandler, GraphTool.UndoStateComponent, ModelInspectorViewModel.GraphModelState);

            this.RegisterCommandHandler<ModelInspectorStateComponent, CollapseInspectorSectionCommand>(
                CollapseInspectorSectionCommand.DefaultCommandHandler, ModelInspectorViewModel.ModelInspectorState);

            this.AddStylesheet_Internal("ModelInspector.uss");
            AddToClassList(ussClassName);
        }

        void UpdateSelectionObserver(IState state, IStateComponent stateComponent)
        {
            if (stateComponent is SelectionStateComponent)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_SelectionObserver);
                m_SelectionObserver = null;

                BuildSelectionObserver();
            }
        }

        /// <summary>
        /// Instantiates and registers the <see cref="StateObserver"/> for the ModelInspectorView.
        /// </summary>
        protected virtual void BuildSelectionObserver()
        {
            if (m_SelectionObserver == null && GraphTool != null)
            {
                var selectionStates = GraphTool.State.AllStateComponents.OfType<SelectionStateComponent>();
                m_SelectionObserver = new InspectorSelectionObserver_Internal(GraphTool.ToolState, ModelInspectorViewModel.GraphModelState,
                    selectionStates.ToList(), ModelInspectorViewModel.ModelInspectorState);

                GraphTool.ObserverManager?.RegisterObserver(m_SelectionObserver);
            }
        }

        /// <inheritdoc />
        protected override void RegisterObservers()
        {
            if (m_LoadedGraphObserver == null)
            {
                m_LoadedGraphObserver = new ModelInspectorGraphLoadedObserver(GraphTool.ToolState, ModelInspectorViewModel.ModelInspectorState);
                GraphTool.ObserverManager.RegisterObserver(m_LoadedGraphObserver);
            }

            if (m_UpdateObserver == null)
            {
                m_UpdateObserver = new ModelViewUpdater(this, ModelInspectorViewModel.ModelInspectorState, ModelInspectorViewModel.GraphModelState);
                GraphTool.ObserverManager.RegisterObserver(m_UpdateObserver);
            }

            BuildSelectionObserver();

            GraphTool.State.OnStateComponentListModified += UpdateSelectionObserver;
        }

        /// <inheritdoc />
        protected override void UnregisterObservers()
        {
            if (GraphTool != null)
            {
                GraphTool.ObserverManager?.UnregisterObserver(m_LoadedGraphObserver);
                m_LoadedGraphObserver = null;

                GraphTool.ObserverManager?.UnregisterObserver(m_UpdateObserver);
                m_UpdateObserver = null;

                GraphTool.ObserverManager?.UnregisterObserver(m_SelectionObserver);
                m_SelectionObserver = null;

                if (GraphTool.State != null)
                {
                    GraphTool.State.OnStateComponentListModified -= UpdateSelectionObserver;
                }
            }
        }

        void RemoveAllUI()
        {
            if (m_InspectorContainer == null)
                return;

            var elements = m_InspectorContainer.Query<ModelView>().ToList();
            foreach (var element in elements)
            {
                element.RemoveFromRootView();
            }

            m_InspectorContainer.RemoveFromHierarchy();
            m_InspectorContainer = null;
        }

        void OnHorizontalScroll(ChangeEvent<float> e)
        {
            using (var updater = ModelInspectorViewModel.ModelInspectorState.UpdateScope)
            {
                updater.SetScrollOffset(new Vector2(e.newValue, m_InspectorContainer.verticalScroller.slider.value));
            }
        }

        void OnVerticalScroll(ChangeEvent<float> e)
        {
            using (var updater = ModelInspectorViewModel.ModelInspectorState.UpdateScope)
            {
                updater.SetScrollOffset(new Vector2(m_InspectorContainer.horizontalScroller.slider.value, e.newValue));
            }
        }

        static class CreateSectionCache
        {
            public static MultipleModelsView CallCreateSection(ElementBuilder eb, Model model)
            {
                Type rootViewType = eb.View?.GetType();

                if (s_SingleFunctions == null || s_SingleFunctions.Keys.All(t => t.Item1 != rootViewType))
                {
                    BuildCache(rootViewType);
                }
                Type currentType = model.GetType();
                while (currentType != typeof(object) && currentType != null)
                {
                    if (s_SingleFunctions?.TryGetValue((rootViewType,currentType), out var func) != null && func != null)
                        return func(eb, model);
                    foreach (var interfaceType in currentType.GetInterfaces())
                    {
                        if (s_SingleFunctions?.TryGetValue((rootViewType,interfaceType), out var funcInterface) != null && funcInterface != null)
                            return funcInterface(eb, model);
                    }
                    currentType = currentType.BaseType;
                }

                return null;
            }

            public static MultipleModelsView CallCreateSection(ElementBuilder eb, IEnumerable<Model> models)
            {
                if (!models.Any())
                    return null;

                Type rootViewType = eb.View?.GetType();

                if (s_EnumFunctions == null || s_EnumFunctions.Keys.All(t => t.Item1 != rootViewType))
                {
                    BuildCache(rootViewType);
                }

                if (models.Count() == 1)
                {
                    MultipleModelsView singleResult = CallCreateSection(eb, models.First());
                    if (singleResult != null)
                        return singleResult;
                }

                Type currentType = ModelHelpers.GetCommonBaseType(models);
                while (currentType != typeof(object) && currentType != null)
                {
                    if (s_EnumFunctions?.TryGetValue((rootViewType,currentType), out var func) != null && func != null)
                        return func(eb, models);
                    foreach (var interfaceType in currentType.GetInterfaces())
                    {
                        if (s_EnumFunctions?.TryGetValue((rootViewType,interfaceType), out var funcInterface) != null && funcInterface != null)
                            return funcInterface(eb, models);
                    }
                    currentType = currentType.BaseType;
                }

                return null;
            }

            static void BuildCache(Type viewType)
            {
                if( s_SingleFunctions == null)
                    s_SingleFunctions = new Dictionary<(Type,Type), Func<ElementBuilder, Model, MultipleModelsView>>();
                if( s_EnumFunctions == null)
                    s_EnumFunctions = new Dictionary<(Type,Type), Func<ElementBuilder, IEnumerable<Model>, MultipleModelsView>>();

                var matchingTypes = TypeCache.GetTypesWithAttribute<ModelInspectorCreateSectionMethodsCacheAttribute>()
                    .Where(t => t.GetCustomAttribute<ModelInspectorCreateSectionMethodsCacheAttribute>().ViewDomain.IsAssignableFrom(viewType))
                    .OrderByDescending(t => t.GetCustomAttribute<ModelInspectorCreateSectionMethodsCacheAttribute>().Priority);
                foreach (var type in matchingTypes)
                {
                    var meths = type.GetMethods(BindingFlags.Static | BindingFlags.Public);

                    foreach (var meth in meths.Where(t => t.ReturnType == typeof(MultipleModelsView) &&
                                 t.GetParameters().Length == 2 &&
                                 t.GetParameters()[0].ParameterType == typeof(ElementBuilder) &&
                                 typeof(Model).IsAssignableFrom(t.GetParameters()[1].ParameterType)))
                    {
                        s_SingleFunctions[(viewType,meth.GetParameters()[1].ParameterType)] = (eb, model) => (MultipleModelsView)meth.Invoke(null, new object[] { eb, model });
                    }
                    foreach (var meth in meths.Where(t => t.ReturnType == typeof(MultipleModelsView) &&
                                 t.GetParameters().Length == 2 &&
                                 t.GetParameters()[0].ParameterType == typeof(ElementBuilder) &&
                                 typeof(IEnumerable<Model>).IsAssignableFrom(t.GetParameters()[1].ParameterType)))
                    {
                        var modelType = meth.GetParameters()[1].ParameterType.GenericTypeArguments[0];
                        if (!s_EnumFunctions.ContainsKey((viewType,modelType)))
                        {
                            var castMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast), BindingFlags.Public | BindingFlags.Static)?.MakeGenericMethod(new[] { modelType });

                            Assert.IsNotNull(castMethod, "Cast Method for IEnumerable<" + modelType.FullName + ">  not found");

                            s_EnumFunctions[(viewType,modelType)] = (eb, models) => (MultipleModelsView)meth.Invoke(null, new[] { eb, castMethod?.Invoke(null, new object[] { models }) });
                        }
                    }
                }
            }

            static Dictionary<(Type,Type), Func<ElementBuilder, IEnumerable<Model>, MultipleModelsView>> s_EnumFunctions;
            static Dictionary<(Type,Type), Func<ElementBuilder, Model, MultipleModelsView>> s_SingleFunctions;
        }


        /// <inheritdoc />
        public override void BuildUI()
        {
            RemoveAllUI();

            if (m_Title == null)
            {
                m_Title = new Label();
                m_Title.AddToClassList(titleUssClassName);
                Add(m_Title);
            }

            // We need to recreate the m_InspectorContainer to be able to set the scroll offset to any value.
            // If we reuse the existing m_InspectorContainer, offset will be clamped according to the last layout of the scrollView.
            m_InspectorContainer = new ScrollView(ScrollViewMode.Vertical);
            m_InspectorContainer.AddToClassList(containerUssClassName);

            if (ModelInspectorViewModel == null)
            {
                m_Title.text = "";
                return;
            }

            m_InspectorContainer.scrollOffset = ModelInspectorViewModel.ModelInspectorState.GetInspectorModel()?.ScrollOffset ?? Vector2.zero;
            m_InspectorContainer.horizontalScroller.slider.RegisterCallback<ChangeEvent<float>>(OnHorizontalScroll);
            m_InspectorContainer.verticalScroller.slider.RegisterCallback<ChangeEvent<float>>(OnVerticalScroll);

            var inspectorModel = ModelInspectorViewModel.ModelInspectorState.GetInspectorModel();
            if (inspectorModel != null)
            {
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

                    // If the sectionInspector is empty, do not show it and do not create a section for it.
                    if (sectionInspector == null || (sectionInspector is ModelInspector modelInspector && modelInspector.IsEmpty()))
                        continue;

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
                        sectionUI.AddToRootView(this);
                        m_InspectorContainer.Add(sectionUI);
                        sectionUI.Add(sectionInspector);
                    }
                    else
                    {
                        // If there was no section created, let's put the sectionInspector directly in the side panel.
                        m_InspectorContainer.Add(sectionInspector);
                    }

                    sectionInspector.AddToRootView(this);
                }

                sectionUI?.AddToClassList("last-child");
            }
            else if (ModelInspectorViewModel.ModelInspectorState.InspectedModels.Count > 1)
            {
                m_Title.text = "Multiple selection";
            }

            Add(m_InspectorContainer);
        }



        public override void UpdateFromModel()
        {
            if (panel == null)
                return;

            using (var modelStateObservation = m_UpdateObserver.ObserveState(ModelInspectorViewModel.GraphModelState))
            using (var inspectorStateObservation = m_UpdateObserver.ObserveState(ModelInspectorViewModel.ModelInspectorState))
            {
                var rebuildType = inspectorStateObservation.UpdateType.Combine(modelStateObservation.UpdateType);

                if (rebuildType == UpdateType.Complete)
                {
                    BuildUI();
                }
                else
                {
                    if (ModelInspectorViewModel.ModelInspectorState.InspectedModels.Any() && modelStateObservation.UpdateType == UpdateType.Partial)
                    {
                        var changeset = ModelInspectorViewModel.GraphModelState.GetAggregatedChangeset(modelStateObservation.LastObservedVersion);

                        bool inspectedModelChanged = false;
                        foreach (var guid in changeset.ChangedModels)
                        {
                            foreach (var inspectedModel in ModelInspectorViewModel.ModelInspectorState.InspectedModels)
                            {
                                if (inspectedModel.Guid == guid)
                                {
                                    inspectedModelChanged = true;
                                }

                                if (inspectedModel is PortNodeModel portNodeModel)
                                {
                                    foreach (var portModel in portNodeModel.Ports)
                                    {
                                        if (portModel.Guid == guid)
                                        {
                                            inspectedModelChanged = true;
                                            break;
                                        }
                                    }
                                }
                                if (inspectedModelChanged)
                                    break;
                            }
                            if (inspectedModelChanged)
                                break;
                        }

                        if (inspectedModelChanged)
                        {
                            var inspectorModel = ModelInspectorViewModel.ModelInspectorState.GetInspectorModel();
                            if (inspectorModel != null)
                            {
                                m_Title.text = inspectorModel.Title;
                            }

                            foreach(var inspectedModel in ModelInspectorViewModel.ModelInspectorState.InspectedModels)
                            {
                                inspectedModel.GetAllViews(this, null, k_UpdateAllUIs);
                                foreach (var ui in k_UpdateAllUIs)
                                {
                                    ui.UpdateFromModel();
                                }
                                k_UpdateAllUIs.Clear();
                            }

                            foreach (var ui in m_PartsToUpdate)
                            {
                                ui.UpdateFromModel();
                            }
                        }
                        else if (changeset.NewModels.Any() || changeset.DeletedModels.Any())
                        {
                            foreach (var ui in m_PartsToUpdate)
                            {
                                ui.UpdateFromModel();
                            }
                        }
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
                                    guid.GetAllViews(this, null, k_UpdateAllUIs);
                                    foreach (var ui in k_UpdateAllUIs)
                                    {
                                        ui.UpdateFromModel();
                                    }

                                    k_UpdateAllUIs.Clear();
                                }

                                if (changeSet.HasAdditionalChange(ModelInspectorStateComponent.Changeset.AdditionalChangesEnum.ScrollOffset))
                                {
                                   m_InspectorContainer.scrollOffset = inspectorModel.ScrollOffset;
                                }
                            }
                        }
                    }
                }
            }
        }

        List<BaseMultipleModelViewsPart> m_PartsToUpdate = new List<BaseMultipleModelViewsPart>();
        public void Register(BaseMultipleModelViewsPart part)
        {
            m_PartsToUpdate.Add(part);
        }

        public void Unregister(BaseMultipleModelViewsPart part)
        {
            m_PartsToUpdate.Remove(part);
        }
    }
}
