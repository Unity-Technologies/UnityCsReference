// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.Rendering
{
    [Flags]
    public enum DrawRendererFlags
    {
        None = 0,
        EnableDynamicBatching = (1 << 0),
        EnableInstancing = (1 << 1),
    }
}
