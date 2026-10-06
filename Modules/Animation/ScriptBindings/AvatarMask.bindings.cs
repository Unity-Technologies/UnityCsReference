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
    ///<summary>Avatar body part.</summary>
    [MovedFrom(true, "UnityEditor.Animations", "UnityEditor")]
    public enum AvatarMaskBodyPart
    {
        ///<summary>The Root.</summary>
        Root = 0,
        ///<summary>The Body.</summary>
        ///<remarks>Including Hips, Spine and Chest transform.</remarks>
        Body = 1,
        ///<summary>The Head.</summary>
        ///<remarks>Including Neck and Head transform.</remarks>
        Head = 2,
        ///<summary>The Left Leg.</summary>
        ///<remarks>Including Left Upper Leg, Left Lower Leg and Left Foot.</remarks>
        LeftLeg = 3,
        ///<summary>The Right Leg.</summary>
        ///<remarks>Including Right Upper Leg, Right Lower Leg and Right Foot.</remarks>
        RightLeg = 4,
        ///<summary>The Left Arm.</summary>
        ///<remarks>Including Left Shoulder, Left Upper Arm, Left Lower Arm and Left Hand.</remarks>
        LeftArm = 5,
        ///<summary>The Right Arm.</summary>
        ///<remarks>Including Right Shoulder, Right Upper Arm, Right Lower Arm and Right Hand.</remarks>
        RightArm = 6,
        ///<summary>Left Fingers.</summary>
        ///<remarks>Inluding all Left Fingers transforms.</remarks>
        LeftFingers = 7,
        ///<summary>Right Fingers.</summary>
        ///<remarks>Inluding all Right Fingers transforms.</remarks>
        RightFingers = 8,
        ///<summary>Left Foot IK.</summary>
        LeftFootIK = 9,
        ///<summary>Right Foot IK.</summary>
        RightFootIK = 10,
        ///<summary>Left Hand IK.</summary>
        LeftHandIK = 11,
        ///<summary>Right Hand IK.</summary>
        RightHandIK = 12,
        ///<summary>Total number of body parts.</summary>
        LastBodyPart = 13
    }

    ///<summary>AvatarMask is used to mask out humanoid body parts and transforms.</summary>
    ///<remarks>They can be used when importing animation or in an animator controller layer.</remarks>
    [MovedFrom(true, "UnityEditor.Animations", "UnityEditor")]
    [NativeHeader("Modules/Animation/AvatarMask.h")]
    [NativeHeader("Modules/Animation/ScriptBindings/Animation.bindings.h")]
    [global::UnityEngine.NativeClass("AvatarMask", PersistentTypeId = 319)]
    [UsedByNativeCode]
    public sealed partial class AvatarMask : Object
    {
        internal AvatarMask(global::UnityEngine.EntityId id) : base(id) {}
        ///<summary>Creates a new AvatarMask.</summary>
        public AvatarMask()
        {
            SetEntityIdFromConstructor(Internal_Create());
        }

        [FreeFunction("AnimationBindings::CreateAvatarMask")]
        extern private static EntityId Internal_Create();

        ///<summary>The number of humanoid body parts.</summary>
        ///<remarks>This member is deprecated, .</remarks>
        ///<seealso cref="AvatarMaskBodyPart.LastBodyPart" />
        [Obsolete("AvatarMask.humanoidBodyPartCount is deprecated, use AvatarMaskBodyPart.LastBodyPart instead.")]
        public int humanoidBodyPartCount
        {
            get { return (int)AvatarMaskBodyPart.LastBodyPart; }
        }

        ///<summary>Returns true if the humanoid body part at the given index is active.</summary>
        ///<param name="index">The index of the humanoid body part.</param>
        [NativeMethod("GetBodyPart")]
        extern public bool GetHumanoidBodyPartActive(AvatarMaskBodyPart index);

        ///<summary>Sets the humanoid body part at the given index to active or not.</summary>
        ///<param name="index">The index of the humanoid body part.</param>
        ///<param name="value">Active or not.</param>
        [NativeMethod("SetBodyPart")]
        extern public void SetHumanoidBodyPartActive(AvatarMaskBodyPart index, bool value);

        ///<summary>Number of transforms.</summary>
        extern public int transformCount { get; set; }

        ///<summary>Adds a transform path into the AvatarMask.</summary>
        ///<param name="transform">The transform to add into the AvatarMask.</param>
        public void AddTransformPath(Transform transform) { AddTransformPath(transform, true);  }
        ///<summary>Adds a transform path into the AvatarMask.</summary>
        ///<param name="transform">The transform to add into the AvatarMask.</param>
        ///<param name="recursive">Whether to also add all children of the specified transform.</param>
        extern public void AddTransformPath([NotNull] Transform transform, [DefaultValue("true")] bool recursive);

        ///<summary>Removes a transform path from the AvatarMask.</summary>
        ///<remarks>If there is no transform path matching **transform** nothing will be removed.</remarks>
        ///<param name="transform">The Transform that should be removed from the AvatarMask.</param>
        public void RemoveTransformPath(Transform transform) { RemoveTransformPath(transform, true); }
        ///<summary>Removes a transform path from the AvatarMask.</summary>
        ///<remarks>If there is no transform path matching **transform** nothing will be removed.</remarks>
        ///<param name="transform">The Transform that should be removed from the AvatarMask.</param>
        ///<param name="recursive">Whether to also remove all children of the specified transform.</param>
        extern public void RemoveTransformPath([NotNull] Transform transform, [DefaultValue("true")] bool recursive);

        ///<summary>Returns the path of the transform at the given index.</summary>
        ///<param name="index">The index of the transform.</param>
        extern public string GetTransformPath(int index);
        ///<summary>Sets the path of the transform at the given index.</summary>
        ///<param name="index">The index of the transform.</param>
        ///<param name="path">The path of the transform.</param>
        extern public void SetTransformPath(int index, string path);

        extern private float GetTransformWeight(int index);
        extern private void SetTransformWeight(int index, float weight);

        ///<summary>Returns true if the transform at the given index is active.</summary>
        ///<param name="index">The index of the transform.</param>
        public bool GetTransformActive(int index) { return GetTransformWeight(index) > 0.5F; }
        ///<summary>Sets the tranform at the given index to active or not.</summary>
        ///<param name="index">The index of the transform.</param>
        ///<param name="value">Active or not.</param>
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
