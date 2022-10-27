// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GraphToolsFoundation.Editor
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
    [ModelInspectorCreateSectionMethodsCache(typeof(ModelInspectorView),ModelInspectorCreateSectionMethodsCacheAttribute.lowestPriority_Internal)]
    static class ModelInspectorCreateSectionFactoryExtensions
    {
        /// <summary>
        /// Creates a new inspector for some <see cref="PortNodeModel"/>s.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="models">The models for which we want to create an inspector UI.</param>
        /// <returns>An inspector UI for the node.</returns>
        public static MultipleModelsView CreateSectionInspector(this ElementBuilder elementBuilder, IEnumerable<PortNodeModel> models)
        {
            var ui = new ModelInspector();

            ui.Setup(models, elementBuilder.View, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext && models.Any())
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Settings:
                        {
                            var inspectorFields = SerializedFieldsInspector.Create(ModelInspector.fieldsPartName, models, ui.RootView, ModelInspector.ussClassName, ModelInspectorView.BasicSettingsFilter);
                            ui.PartList.AppendPart(inspectorFields);
                            break;
                        }
                    case SectionType.Properties:
                        var nodeInspectorFields = NodePortsInspector.Create(ModelInspector.fieldsPartName, models, ui.RootView, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(nodeInspectorFields);
                        break;
                    case SectionType.Advanced:
                        {
                            var inspectorFields = SerializedFieldsInspector.Create(ModelInspector.fieldsPartName, models, ui.RootView, ModelInspector.ussClassName, ModelInspectorView.AdvancedSettingsFilter);
                            ui.PartList.AppendPart(inspectorFields);
                            break;
                        }
                }
            }

            ui.BuildUI();
            ui.UpdateFromModel();

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
                    case SectionType.Settings:
                        {
                            var inspectorFields = SerializedFieldsInspector.Create(ModelInspector.fieldsPartName, models, ui.RootView, ModelInspector.ussClassName, ModelInspectorView.BasicSettingsFilter);
                            ui.PartList.AppendPart(inspectorFields);
                            break;
                        }
                    case SectionType.Advanced:
                        {
                            var inspectorFields = SerializedFieldsInspector.Create(ModelInspector.fieldsPartName, models, ui.RootView, ModelInspector.ussClassName, ModelInspectorView.AdvancedSettingsFilter);
                            ui.PartList.AppendPart(inspectorFields);
                            break;
                        }
                }
            }

            ui.BuildUI();
            ui.UpdateFromModel();

            return ui;
        }

        /// <summary>
        /// Creates a new inspector for some <see cref="VariableDeclarationModel"/>s.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="models">The models for which we want to create an inspector UI.</param>
        /// <returns>An inspector UI for the node.</returns>
        public static MultipleModelsView CreateSectionInspector(this ElementBuilder elementBuilder, IEnumerable<VariableDeclarationModel> models)
        {
            var ui = new ModelInspector();
            ui.Setup(models, elementBuilder.View as ModelInspectorView, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Properties:
                        {
                            var inspectorFields = SerializedFieldsInspector.Create(ModelInspector.fieldsPartName, models, ui.RootView, ModelInspector.ussClassName, ModelInspectorView.BasicSettingsFilter);
                            ui.PartList.AppendPart(inspectorFields);
                            break;
                        }
                    case SectionType.Advanced:
                        {
                            var inspectorFields = VariableFieldsInspector.Create(ModelInspector.fieldsPartName, models, ui.RootView, ModelInspector.ussClassName, ModelInspectorView.AdvancedSettingsFilter);
                            ui.PartList.AppendPart(inspectorFields);
                            break;
                        }
                }
            }

            ui.BuildUI();
            ui.UpdateFromModel();

            return ui;
        }

        /// <summary>
        /// Creates a new inspector for some <see cref="WireModel"/>s.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="models">The models for which we want to create an inspector UI.</param>
        /// <returns>An inspector UI for the node.</returns>
        public static MultipleModelsView CreateSectionInspector(this ElementBuilder elementBuilder, IEnumerable<WireModel> models)
        {
            var ui = new ModelInspector();
            ui.Setup(models, elementBuilder.View as ModelInspectorView, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Properties:
                    {
                        var inspectorFields = WireFieldsInspector.Create(ModelInspector.fieldsPartName, models, ui.RootView, ModelInspector.ussClassName, ModelInspectorView.BasicSettingsFilter);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                }
            }

            ui.BuildUI();
            ui.UpdateFromModel();

            return ui;
        }
    }
}
