// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    [GraphElementsExtensionMethodsCache(typeof(MiniMapView), GraphElementsExtensionMethodsCacheAttribute.lowestPriority_Internal)]
    static class MiniMapViewFactoryExtensions
    {
        /// <summary>
        /// Creates a MiniMap from for the given model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The <see cref="GraphModel"/> this <see cref="ModelView"/> will display.</param>
        /// <returns>A setup <see cref="ModelView"/>.</returns>
        public static ModelView CreateMiniMap(this ElementBuilder elementBuilder, GraphModel model)
        {
            ModelView ui = new MiniMap();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
