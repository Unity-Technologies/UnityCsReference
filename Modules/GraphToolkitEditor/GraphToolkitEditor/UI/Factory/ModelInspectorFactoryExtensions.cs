// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Extension methods to create UI for graph element models for the <see cref="ModelInspectorView"/>.
    /// </summary>
    /// <remarks>
    /// Extension methods in this class are selected by matching the type of their second parameter to the type
    /// of the graph element model for which we need to instantiate a <see cref="ModelView"/>. You can change the UI for a
    /// model by defining new extension methods for <see cref="ElementBuilder"/> in a class having
    /// the <see cref="GraphElementsExtensionMethodsCacheAttribute"/>.
    /// </remarks>
    [GraphElementsExtensionMethodsCache(typeof(ModelInspectorView), GraphElementsExtensionMethodsCacheAttribute.k_LowestPriority)]
    [UnityRestricted]
    internal static class ModelInspectorFactoryExtensions
    {
        /// <summary>
        /// Creates a new UI for an inspector section.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The inspector section model.</param>
        /// <returns>A UI for the inspector section.</returns>
        public static ModelView CreateSection(this ElementBuilder elementBuilder, InspectorSectionModel model)
        {
            ModelView ui;
            if (model.Collapsible || !string.IsNullOrEmpty(model.Title))
            {
                ui = new CollapsibleSection();
            }
            else
            {
                ui = new InspectorSection();
            }

            ui.SetupBuildAndUpdate(model, elementBuilder.View);
            return ui;
        }

        /// <summary>
        /// Creates a new UI for a <see cref="TransitionSupportModel"/>.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The transition support model.</param>
        /// <returns>A new UI for a <see cref="TransitionSupportModel"/>.</returns>
        public static ModelView CreateTransitionSupport(this ElementBuilder elementBuilder, TransitionSupportModel model)
        {
            ModelView ui = new TransitionSupportEditor(elementBuilder.ParentView as StateTransitionsInspector);

            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        /// <summary>
        /// Creates a new UI for a <see cref="GroupConditionModel"/>.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The group condition model.</param>
        /// <returns>A new UI for a <see cref="GroupConditionModel"/>.</returns>
        public static ModelView CreateGroupConditionView(this ElementBuilder elementBuilder, GroupConditionModel model)
        {
            ModelView ui = elementBuilder.Context is RootGroupConditionViewContext ? new RootGroupConditionView((ConditionEditor)elementBuilder.ParentView) : new GroupConditionView((ConditionEditor)elementBuilder.ParentView);

            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        /// <summary>
        /// Creates a new UI for a <see cref="TransitionModel"/>.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The transition model.</param>
        /// <returns>A new UI for a <see cref="GroupConditionModel"/>.</returns>
        public static ModelView CreateTransitionEditor(this ElementBuilder elementBuilder, TransitionModel model)
        {
            ModelView ui = elementBuilder.Context is ConditionEditorContext ? new ConditionEditor((TransitionPropertiesEditor)elementBuilder.ParentView) : new TransitionPropertiesEditor((TransitionSupportEditor)elementBuilder.ParentView);


            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
