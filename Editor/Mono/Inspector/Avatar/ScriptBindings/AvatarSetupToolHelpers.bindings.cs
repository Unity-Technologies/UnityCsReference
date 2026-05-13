// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [RequiredByNativeCode]
    [NativeHeader("Editor/Mono/Inspector/Avatar/ScriptBindings/AvatarSetupToolHelpers.bindings.h")]
    [StaticAccessor("AvatarSetup", StaticAccessorType.DoubleColon)]
    internal static class AvatarSetupToolHelpers
    {
        internal static extern HumanBone[] GetHumanDescriptionBones(IntPtr humanDescriptionPtr);

        internal static extern void SetHumanDescriptionMappings(IntPtr humanDescriptionPtr, HumanBone[] humanBoneMappingArray, SkeletonBone[] skeletonBone);
    }
}
