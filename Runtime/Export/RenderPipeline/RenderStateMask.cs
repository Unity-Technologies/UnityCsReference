// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    // Must match RenderStateMask on C++ side
    [Flags]
    public enum RenderStateMask
    {
        Nothing = 0,
        Blend = 1 << 0,
        Raster = 1 << 1,
        Depth = 1 << 2,
        Stencil = 1 << 3,
        Everything = Blend | Raster | Depth | Stencil
    }
}
