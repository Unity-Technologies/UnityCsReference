// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements;

[NativeHeader("Modules/UIElements/Core/Native/Unmanaged/StyleDataAllocator.bindings.h")]
static partial class StyleDataAllocator
{
    internal static extern IntPtr Allocate(StyleDataType type);
    internal static extern void Free(IntPtr ptr, StyleDataType type);

    // For tests
    internal static extern int SizeOf(StyleDataType type);
}
