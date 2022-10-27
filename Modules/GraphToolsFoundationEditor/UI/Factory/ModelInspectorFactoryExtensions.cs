// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;

namespace Unity.GraphToolsFoundation.Editor
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
    [GraphElementsExtensionMethodsCache(typeof(ModelInspectorView), GraphElementsExtensionMethodsCacheAttribute.lowestPriority_Internal)]
    static class ModelInspectorFactoryExtensions
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
    }
}
