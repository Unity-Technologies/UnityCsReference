// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Runtime/Animation/OptimizeTransformHierarchy.h")]
    public class AnimatorUtility
    {
        [FreeFunction]
        extern public static void OptimizeTransformHierarchy(GameObject go, string[] exposedTransforms);

        [FreeFunction]
        extern public static void DeoptimizeTransformHierarchy(GameObject go);
    }
}
