// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [NativeHeader("Modules/Animation/Animator.h")]
    [NativeHeader("Editor/Src/Animation/AvatarUtility.h")]
    internal class AvatarUtility
    {
        extern static internal void SetHumanPose([NotNull] Animator animator, float[] dof);
    }
}
