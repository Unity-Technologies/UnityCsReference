// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Modules/Animation/ScriptBindings/AnimatorUtility.bindings.h")]
    public class AnimatorUtility
    {
        [FreeFunction("AnimatorUtilityBindings::OptimizeTransformHierarchy")]
        extern public static void OptimizeTransformHierarchy([NotNull] GameObject go, string[] exposedTransforms);

        [FreeFunction("AnimatorUtilityBindings::DeoptimizeTransformHierarchy")]
        extern public static void DeoptimizeTransformHierarchy([NotNull] GameObject go);
    }
}
