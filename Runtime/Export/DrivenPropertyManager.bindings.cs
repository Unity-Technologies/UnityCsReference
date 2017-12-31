// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Bindings;

using Object = UnityEngine.Object;

namespace UnityEngine
{
    // NOTE: internal until further notice. Used by tests
    [NativeHeader("Editor/Src/Properties/DrivenPropertyManager.h")]
    internal class DrivenPropertyManager
    {
        [Conditional("UNITY_EDITOR")]
        public static void RegisterProperty(Object driver, Object target, string propertyPath)
        {
            RegisterPropertyPartial(driver, target, propertyPath);
        }

        [Conditional("UNITY_EDITOR")]
        public static void UnregisterProperty(Object driver, Object target, string propertyPath)
        {
            UnregisterPropertyPartial(driver, target, propertyPath);
        }

        [Conditional("UNITY_EDITOR")]
        [NativeConditional("UNITY_EDITOR")]
        [StaticAccessor("GetDrivenPropertyManager()", StaticAccessorType.Dot)]
        extern public static void UnregisterProperties([NotNull] Object driver);

        [NativeConditional("UNITY_EDITOR")]
        [StaticAccessor("GetDrivenPropertyManager()", StaticAccessorType.Dot)]
        extern private static void RegisterPropertyPartial([NotNull] Object driver, [NotNull] Object target, [NotNull] string propertyPath);

        [NativeConditional("UNITY_EDITOR")]
        [StaticAccessor("GetDrivenPropertyManager()", StaticAccessorType.Dot)]
        extern private static void UnregisterPropertyPartial([NotNull] Object driver, [NotNull] Object target, [NotNull] string propertyPath);
    }
}
