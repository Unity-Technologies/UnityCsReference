// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Modules/Animation/OptimizeTransformHierarchy.h")]
    public class AnimatorUtility
    {
        [FreeFunction]
        extern public static void OptimizeTransformHierarchy([NotNull("NullExceptionObject")] GameObject go, string[] exposedTransforms);

        [FreeFunction]
        extern public static void DeoptimizeTransformHierarchy([NotNull("NullExceptionObject")] GameObject go);
    }
}
