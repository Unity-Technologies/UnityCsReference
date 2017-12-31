// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine;
using UnityEngine.Bindings;

using Object = UnityEngine.Object;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/DrivenPropertyManagerInternal.bindings.h")]
    [StaticAccessor("DrivenPropertyManagerInternal", StaticAccessorType.DoubleColon)]
    internal class DrivenPropertyManagerInternal
    {
        extern public static bool IsDriven(Object target, string propertyPath);
        extern public static bool IsDriving(Object driver, Object target, string propertyPath);
    }
}
