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
        UIToolkit_UGUI = (1 << 0),
        LowLevel = (1 << 1),
        All = ~0
    }
}
