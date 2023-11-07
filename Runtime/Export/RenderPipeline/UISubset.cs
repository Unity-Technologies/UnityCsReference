// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [Flags]
    public enum UISubset
    {
        UGUI = (1 << 0),
        UIToolkit = (1 << 1),
        LowLevel = (1 << 2),
        All = ~0
    }
}
