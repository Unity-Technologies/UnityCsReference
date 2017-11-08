// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Internal;

namespace UnityEngine
{
    [MovedFrom("UnityEditor.Animations", true)]
    public enum AvatarMaskBodyPart
    {
        Root = 0,
        Body = 1,
        Head = 2,
        LeftLeg = 3,
        RightLeg = 4,
        LeftArm = 5,
        RightArm = 6,
        LeftFingers = 7,
        RightFingers = 8,
        LeftFootIK = 9,
        RightFootIK = 10,
        LeftHandIK = 11,
        RightHandIK = 12,
        LastBodyPart = 13
    }

    [MovedFrom("UnityEditor.Animations", true)]
    [NativeHeader("Runtime/Animation/AvatarMask.h")]
    [NativeHeader("Runtime/Animation/ScriptBindings/Animation.bindings.h")]
    [UsedByNativeCode]
    public sealed partial class AvatarMask : Object
    {
        public AvatarMask()
        {
            Internal_Create(this);
        }

        [FreeFunction("AnimationBindings::CreateAvatarMask")]
        extern private static void Internal_Create([Writable] AvatarMask self);

        [Obsolete("AvatarMask.humanoidBodyPartCount is deprecated, use AvatarMaskBodyPart.LastBodyPart instead.")]
        public int humanoidBodyPartCount
        {
            get { return (int)AvatarMaskBodyPart.LastBodyPart; }
        }

        [NativeMethod("GetBodyPart")]
        extern public bool GetHumanoidBodyPartActive(AvatarMaskBodyPart index);

        [NativeMethod("SetBodyPart")]
        extern public void SetHumanoidBodyPartActive(AvatarMaskBodyPart index, bool value);

        extern public int transformCount { get; set; }

        public void AddTransformPath(Transform transform) { AddTransformPath(transform, true);  }
        extern public void AddTransformPath([NotNull] Transform transform, [DefaultValue("true")] bool recursive);

        public void RemoveTransformPath(Transform transform) { RemoveTransformPath(transform, true); }
        extern public void RemoveTransformPath([NotNull] Transform transform, [DefaultValue("true")] bool recursive);

        extern public string GetTransformPath(int index);
        extern public void SetTransformPath(int index, string path);

        extern private float GetTransformWeight(int index);
        extern private void SetTransformWeight(int index, float weight);

        public bool GetTransformActive(int index) { return GetTransformWeight(index) > 0.5F; }
        public void SetTransformActive(int index, bool value) { SetTransformWeight(index, value ? 1.0F : 0.0F); }

        extern internal bool hasFeetIK { get; }

        internal void Copy(AvatarMask other)
        {
            for (AvatarMaskBodyPart i = 0; i < AvatarMaskBodyPart.LastBodyPart; i++)
                SetHumanoidBodyPartActive(i, other.GetHumanoidBodyPartActive(i));

            transformCount = other.transformCount;

            for (int i = 0; i < other.transformCount; i++)
            {
                SetTransformPath(i, other.GetTransformPath(i));
                SetTransformActive(i, other.GetTransformActive(i));
            }
        }
    }
}
