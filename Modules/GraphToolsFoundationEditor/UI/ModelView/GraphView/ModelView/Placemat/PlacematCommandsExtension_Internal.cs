// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Extension methods to help dispatching commands for a placemat.
    /// </summary>
    static class PlacematCommandsExtension_Internal
    {
        public static void CollapsePlacemat(this Placemat self, bool value)
        {
            var collapsedModels = value ? self.GatherCollapsedElements_Internal() : null;
            self.GraphView.Dispatch(new CollapsePlacematCommand(self.PlacematModel, value, collapsedModels));
        }
    }
}
