// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.Rendering
{
    [Flags]
    public enum SortFlags
    {
        None = 0,

        SortingLayer = (1 << 0), // by global sorting layer
        RenderQueue = (1 << 1), // by material render queue
        BackToFront = (1 << 2), // distance back to front, sorting group order, same distance sort priority, material index on renderer
        QuantizedFrontToBack = (1 << 3), // front to back by quantized distance
        OptimizeStateChanges = (1 << 4), // combination of: static batching, lightmaps, material sort key, geometry ID
        CanvasOrder = (1 << 5), // same distance sort priority (used in Canvas)

        CommonOpaque = SortingLayer | RenderQueue | QuantizedFrontToBack | OptimizeStateChanges | CanvasOrder,
        CommonTransparent = SortingLayer | RenderQueue | BackToFront | OptimizeStateChanges,
    }
}
