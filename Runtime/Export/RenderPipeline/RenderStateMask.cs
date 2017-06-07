// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    // Must match RenderStateMask on C++ side
    [Flags]
    public enum RenderStateMask
    {
        Nothing = 0,
        Blend = 1,
        Raster = 2,
        Depth = 4,
        Stencil = 8,
        Everything = 15
    }
}
