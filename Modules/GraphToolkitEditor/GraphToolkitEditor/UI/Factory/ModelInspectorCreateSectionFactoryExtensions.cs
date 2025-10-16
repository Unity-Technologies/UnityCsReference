// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Extension methods to create section UI for graph element models for the <see cref="ModelInspectorView"/>.
    /// </summary>
    /// <remarks>
    /// Extension methods in this class are selected by matching the type of their second parameter to either the type
    /// of the model or an IEnumerable of models for which we need to instantiate a <see cref="MultipleModelsView"/>.
    /// You can change the UI for a model by defining new extension methods for <see cref="ElementBuilder"/> in a class having
    /// the <see cref="ModelInspectorCreateSectionMethodsCacheAttribute"/>.
    /// </remarks>
    [ModelInspectorCreateSectionMethodsCache(typeof(ModelInspectorView), ModelInspectorCreateSectionMethodsCacheAttribute.k_LowestPriority)]
    [UnityRestricted]
    internal static class ModelInspectorCreateSectionFactoryExtensions
    {
        /// <summary>
        /// Creates a new inspector for some <see cref="Model"/>s.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="models">The models for which we want to create an inspector UI.</param>
        /// <returns>An inspector UI for the node.</returns>
        public static MultipleModelsView CreateSectionInspector(this ElementBuilder elementBuilder, IReadOnlyList<Model> models)
        {
            var ui = new ModelInspector();
            ui.Setup(models, elementBuilder.View as ModelInspectorView, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Options:
                    {
                        var inspectorFields = ModelsFieldsInspector.Create(ModelInspector.fieldsPartName, models, ui, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                }
            }

            ui.BuildUITree();
            ui.DoCompleteUpdate();

            return ui;
        }

        /// <summary>
        /// Creates a new inspector for some <see cref="PortNodeModel"/>s.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="models">The models for which we want to create an inspector UI.</param>
        /// <returns>An inspector UI for the node.</returns>
        public static MultipleModelsView CreateSectionInspector(this ElementBuilder elementBuilder, IReadOnlyList<PortNodeModel> models)
        {
            var ui = new ModelInspector();

            ui.Setup(models, elementBuilder.View, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext && models is { Count: > 0 })
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Options:
                    {
                        var inspectorFields = NodeOptionsInspector.Create(ModelInspector.fieldsPartName, models, ui, ModelInspector.ussClassName, ModelInspectorView.NodeOptionsFilter);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                    case SectionType.Properties:
                        var nodeInspectorFields = NodePortsInspector.Create(ModelInspector.fieldsPartName, models, ui, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(nodeInspectorFields);
                        break;
                    case SectionType.Advanced:
                    {
                        var inspectorFields = SerializedFieldsInspector.Create(ModelInspector.fieldsPartName, models, ui, ModelInspector.ussClassName, ModelInspectorView.AdvancedSettingsFilter);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                }
            }

            ui.BuildUITree();
            ui.DoCompleteUpdate();

            return ui;
        }

        /// <summary>
        /// Creates a new inspector for an <see cref="GraphModel"/>.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The graph model for which we want to create an inspector UI.</param>
        /// <returns>An inspector UI for the graph.</returns>
        public static MultipleModelsView CreateSectionInspector(this ElementBuilder elementBuilder, GraphModel model)
        {
            var models = new[] { model };
            var ui = new ModelInspector();
            ui.Setup(models, elementBuilder.View as ModelInspectorView, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Options:
                    {
                        var inspectorFields = SerializedFieldsInspector.Create(ModelInspector.fieldsPartName, models, ui, ModelInspector.ussClassName, ModelInspectorView.NodeOptionsFilter);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                    case SectionType.Advanced:
                    {
                        var inspectorFields = SerializedFieldsInspector.Create(ModelInspector.fieldsPartName, models, ui, ModelInspector.ussClassName, ModelInspectorView.AdvancedSettingsFilter);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                }
            }

            ui.BuildUITree();
            ui.DoCompleteUpdate();

            return ui;
        }

        /// <summary>
        /// Creates a new inspector for some <see cref="VariableDeclarationModelBase"/>s.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="models">The models for which we want to create an inspector UI.</param>
        /// <returns>An inspector UI for the node.</returns>
        public static MultipleModelsView CreateSectionInspector(this ElementBuilder elementBuilder, IReadOnlyList<VariableDeclarationModelBase> models)
        {
            var ui = new ModelInspector();
            ui.Setup(models, elementBuilder.View as ModelInspectorView, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Advanced:
                    {
                        var inspectorFields = VariableFieldsInspector.Create(ModelInspector.fieldsPartName, models, ui, ModelInspector.ussClassName, _ => true, VariableFieldsInspector.DisplayFlags.AdvancedProperties);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                    case SectionType.Properties:
                    {
                        var inspectorFields = VariableFieldsInspector.Create(ModelInspector.fieldsPartName, models, ui, ModelInspector.ussClassName, SerializedFieldsInspector.CanBeInspected);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                }
            }

            ui.BuildUITree();
            ui.DoCompleteUpdate();

            return ui;
        }

        /// <summary>
        /// Creates a new inspector for some <see cref="VariableNodeModel"/>s.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="models">The models for which we want to create an inspector UI.</param>
        /// <returns>An inspector UI for the node.</returns>
        public static MultipleModelsView CreateSectionInspector(this ElementBuilder elementBuilder, IReadOnlyList<VariableNodeModel> models)
        {
            return CreateSectionCache.CallCreateSection(elementBuilder, models.SelectToList(t => t.DeclarationModel as VariableDeclarationModelBase));
        }

        /// <summary>
        /// Creates a new inspector for some <see cref="GroupModel"/>s.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="models">The models for which we want to create an inspector UI.</param>
        /// <returns>An inspector UI for the node.</returns>
        public static MultipleModelsView CreateSectionInspector(this ElementBuilder elementBuilder, IReadOnlyList<GroupModel> models)
        {
            var ui = new ModelInspector();
            ui.Setup(models, elementBuilder.View as ModelInspectorView, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Properties:
                    {
                        var inspectorFields = GraphElementFieldInspector.Create(ModelInspector.fieldsPartName, models, ui, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                }
            }

            ui.BuildUITree();
            ui.DoCompleteUpdate();

            return ui;
        }

        /// <summary>
        /// Creates a new inspector for some <see cref="StickyNoteModel"/>s.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="models">The models for which we want to create an inspector UI.</param>
        /// <returns>An inspector UI for the node.</returns>
        public static MultipleModelsView CreateSectionInspector(this ElementBuilder elementBuilder, IReadOnlyList<StickyNoteModel> models)
        {
            var ui = new ModelInspector();
            ui.Setup(models, elementBuilder.View as ModelInspectorView, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Properties:
                    {
                        var inspectorFields = SerializedFieldsInspector.Create(ModelInspector.fieldsPartName, models, ui, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                }
            }

            ui.BuildUITree();
            ui.DoCompleteUpdate();

            return ui;
        }

        /// <summary>
        /// Creates a new inspector for some <see cref="WireModel"/>s.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="models">The models for which we want to create an inspector UI.</param>
        /// <returns>An inspector UI for the node.</returns>
        public static MultipleModelsView CreateSectionInspector(this ElementBuilder elementBuilder, IReadOnlyList<WireModel> models)
        {
            var ui = new ModelInspector();
            ui.Setup(models, elementBuilder.View as ModelInspectorView, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Properties:
                    {
                        var inspectorFields = WireFieldsInspector.Create(ModelInspector.fieldsPartName, models, ui, ModelInspector.ussClassName, ModelInspectorView.NodeOptionsFilter);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                }
            }

            ui.BuildUITree();
            ui.DoCompleteUpdate();

            return ui;
        }

        /// <summary>
        /// Creates a new inspector for some <see cref="PlacematModel"/>s.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="models">The models for which we want to create an inspector UI.</param>
        /// <returns>An inspector UI for the node.</returns>
        public static MultipleModelsView CreateSectionInspector(this ElementBuilder elementBuilder, IReadOnlyList<PlacematModel> models)
        {
            var ui = new ModelInspector();
            ui.Setup(models, elementBuilder.View as ModelInspectorView, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext && models is { Count: > 0 })
            {
                var inspectorFields = PlacematFieldsInspector.Create(ModelInspector.fieldsPartName, models, ui, ModelInspector.ussClassName, ModelInspectorView.AdvancedSettingsFilter);
                ui.PartList.AppendPart(inspectorFields);
            }

            ui.BuildUITree();
            ui.DoCompleteUpdate();

            return ui;
        }

        /// <summary>
        /// Creates a new inspector for some <see cref="TransitionSupportModel"/>s.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="models">The models for which we want to create an inspector UI.</param>
        /// <returns>a new inspector for some <see cref="TransitionSupportModel"/>s.</returns>
        public static MultipleModelsView CreateStateMachineInspector(this ElementBuilder elementBuilder, IReadOnlyList<TransitionSupportModel> models)
        {
            var ui = elementBuilder.Context is InspectorSectionContext isc && models is { Count: 1 } && isc.Section.SectionType == SectionType.Properties ? new TransitionSupportInspector() : new ModelInspector();
            ui.SetupBuildAndUpdate(models, elementBuilder.View, elementBuilder.Context);
            return ui;
        }


        /// <summary>
        /// Creates a new inspector for some <see cref="StateModel"/>s.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="models">The models for which we want to create an inspector UI.</param>
        /// <returns>a new inspector for some <see cref="StateModel"/>s.</returns>
        public static MultipleModelsView CreateBaseStateInspector(this ElementBuilder elementBuilder, IReadOnlyList<StateModel> models)
        {
            if (elementBuilder.Context is InspectorSectionContext isc && isc.Section.SectionType == SectionType.StateTransitions)
            {
                var stateTransitionsInspector = new StateTransitionsInspector();
                stateTransitionsInspector.SetupBuildAndUpdate(models, elementBuilder.View, elementBuilder.Context);
                return stateTransitionsInspector;
            }

            return CreateSectionInspector(elementBuilder, models);
        }
    }
}
