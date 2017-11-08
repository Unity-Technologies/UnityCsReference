// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

namespace UnityEngine
{
    [NativeHeader("Runtime/Animation/HumanTrait.h")]
    public class HumanTrait
    {
        // Number of muscles
        extern public static int MuscleCount
        {
            get;
        }

        extern internal static int GetBoneIndexFromMono(int humanId);
        extern internal static int GetBoneIndexToMono(int boneIndex);

        // Muscle's name
        extern public static string[] MuscleName
        {
            get;
        }

        // Number of bones
        extern public static int BoneCount
        {
            get;
        }

        // Bone's name
        extern public static string[] BoneName
        {
            [NativeMethod("MonoBoneName")]
            get;
        }

        // Return muscle index linked to bone i, dofIndex allow you to choose between X, Y and Z muscle's axis
        public static int MuscleFromBone(int i, int dofIndex)
        {
            return Internal_MuscleFromBone(GetBoneIndexFromMono(i), dofIndex);
        }

        [NativeMethod("MuscleFromBone")]
        extern static int Internal_MuscleFromBone(int i, int dofIndex);

        // Return bone index linked to muscle i
        static public int BoneFromMuscle(int i)
        {
            return GetBoneIndexToMono(Internal_BoneFromMuscle(i));
        }

        [NativeMethod("BoneFromMuscle")]
        extern static int Internal_BoneFromMuscle(int i);

        // Return true if bone i is a required bone.
        public static bool RequiredBone(int i)
        {
            return Internal_RequiredBone(GetBoneIndexFromMono(i));
        }

        [NativeMethod("RequiredBone")]
        extern static bool Internal_RequiredBone(int i);

        // Number of required bones.
        extern public static int RequiredBoneCount
        {
            [NativeMethod("RequiredBoneCount")]
            get;
        }

        internal static bool HasCollider(Avatar avatar, int i)
        {
            return Internal_HasCollider(avatar, GetBoneIndexFromMono(i));
        }

        [NativeMethod("HasCollider")]
        extern static bool Internal_HasCollider(Avatar avatar, int i);

        // Return default minimum values for muscle.
        extern public static float GetMuscleDefaultMin(int i);

        // Return default maximum values for muscle.
        extern public static float GetMuscleDefaultMax(int i);

        // Return parent human bone id
        static public int GetParentBone(int i)
        {
            int parentIndex = Internal_GetParent(GetBoneIndexFromMono(i));
            return parentIndex != -1 ? GetBoneIndexToMono(parentIndex) : -1;
        }

        [NativeMethod("GetParent")]
        extern static int Internal_GetParent(int i);
    }
}
