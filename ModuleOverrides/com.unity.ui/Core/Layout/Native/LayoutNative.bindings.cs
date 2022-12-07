// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.Layout;

[NativeHeader("ModuleOverrides/com.unity.ui/Core/Layout/Native/LayoutNative.h")]
static class LayoutNative
{
    [NativeMethod(IsThreadSafe = false)]
    internal static extern void CalculateLayout(
        IntPtr node,
        float parentWidth,
        float parentHeight,
        int parentDirection,
        IntPtr state);
}
