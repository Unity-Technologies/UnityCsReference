// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

namespace UnityEngine
{


internal enum BodyDoF
{
    SpineFrontBack = 0,
    SpineLeftRight,
    SpineRollLeftRight,
    ChestFrontBack,
    ChestLeftRight,
    ChestRollLeftRight,
    UpperChestFrontBack,
    UpperChestLeftRight,
    UpperChestRollLeftRight,
    LastBodyDoF
}

internal enum HeadDoF
{
    NeckFrontBack = 0,
    NeckLeftRight,
    NeckRollLeftRight,
    HeadFrontBack,
    HeadLeftRight,
    HeadRollLeftRight,
    LeftEyeDownUp,
    LeftEyeInOut,
    RightEyeDownUp,
    RightEyeInOut,
    JawDownUp,
    JawLeftRight,
    LastHeadDoF
}

internal enum LegDoF
{
    UpperLegFrontBack = 0,
    UpperLegInOut,
    UpperLegRollInOut,
    LegCloseOpen,
    LegRollInOut,
    FootCloseOpen,
    FootInOut,
    ToesUpDown,
    LastLegDoF
}

internal enum ArmDoF
{
    ShoulderDownUp = 0,
    ShoulderFrontBack,
    ArmDownUp,
    ArmFrontBack,
    ArmRollInOut,
    ForeArmCloseOpen,
    ForeArmRollInOut,
    HandDownUp,
    HandInOut,
    LastArmDoF
}

internal enum FingerDoF
{
    ProximalDownUp = 0,
    ProximalInOut,
    IntermediateCloseOpen,
    DistalCloseOpen,
    LastFingerDoF
}

internal enum DoF
{
    BodyDoFStart = 0,
    HeadDoFStart = (int)BodyDoFStart + (int)BodyDoF.LastBodyDoF,
    LeftLegDoFStart = (int)HeadDoFStart + (int)HeadDoF.LastHeadDoF,
    RightLegDoFStart = (int)LeftLegDoFStart + (int)LegDoF.LastLegDoF,
    LeftArmDoFStart = (int)RightLegDoFStart + (int)LegDoF.LastLegDoF,
    RightArmDoFStart = (int)LeftArmDoFStart + (int)ArmDoF.LastArmDoF,
    LeftThumbDoFStart = (int)RightArmDoFStart + (int)ArmDoF.LastArmDoF,
    LeftIndexDoFStart = (int)LeftThumbDoFStart + (int)FingerDoF.LastFingerDoF,
    LeftMiddleDoFStart = (int)LeftIndexDoFStart + (int)FingerDoF.LastFingerDoF,
    LeftRingDoFStart = (int)LeftMiddleDoFStart + (int)FingerDoF.LastFingerDoF,
    LeftLittleDoFStart = (int)LeftRingDoFStart + (int)FingerDoF.LastFingerDoF,
    RightThumbDoFStart = (int)LeftLittleDoFStart + (int)FingerDoF.LastFingerDoF,
    RightIndexDoFStart = (int)RightThumbDoFStart + (int)FingerDoF.LastFingerDoF,
    RightMiddleDoFStart = (int)RightIndexDoFStart + (int)FingerDoF.LastFingerDoF,
    RightRingDoFStart = (int)RightMiddleDoFStart + (int)FingerDoF.LastFingerDoF,
    RightLittleDoFStart = (int)RightRingDoFStart + (int)FingerDoF.LastFingerDoF,
    LastDoF = (int)RightLittleDoFStart + (int)FingerDoF.LastFingerDoF
}

public enum HumanBodyBones
{
    
    Hips = 0,
    
    LeftUpperLeg = 1,
    
    RightUpperLeg = 2,
    
    LeftLowerLeg = 3,
    
    RightLowerLeg = 4,
    
    LeftFoot = 5,
    
    RightFoot = 6,
    
    Spine = 7,
    
    Chest = 8,
    
    UpperChest = 54,
    
    Neck = 9,
    
    Head = 10,
    
    LeftShoulder = 11,
    
    RightShoulder = 12,
    
    LeftUpperArm = 13,
    
    RightUpperArm = 14,
    
    LeftLowerArm = 15,
    
    RightLowerArm = 16,
    
    LeftHand = 17,
    
    RightHand = 18,
    
    LeftToes = 19,
    
    RightToes = 20,
    
    LeftEye = 21,
    
    RightEye = 22,
    
    Jaw = 23,
    LeftThumbProximal = 24,
    LeftThumbIntermediate = 25,
    LeftThumbDistal = 26,
    LeftIndexProximal = 27,
    LeftIndexIntermediate = 28,
    LeftIndexDistal = 29,
    LeftMiddleProximal = 30,
    LeftMiddleIntermediate = 31,
    LeftMiddleDistal = 32,
    LeftRingProximal = 33,
    LeftRingIntermediate = 34,
    LeftRingDistal = 35,
    LeftLittleProximal = 36,
    LeftLittleIntermediate = 37,
    LeftLittleDistal = 38,
    RightThumbProximal = 39,
    RightThumbIntermediate = 40,
    RightThumbDistal = 41,
    RightIndexProximal = 42,
    RightIndexIntermediate = 43,
    RightIndexDistal = 44,
    RightMiddleProximal = 45,
    RightMiddleIntermediate = 46,
    RightMiddleDistal = 47,
    RightRingProximal = 48,
    RightRingIntermediate = 49,
    RightRingDistal = 50,
    RightLittleProximal = 51,
    RightLittleIntermediate = 52,
    RightLittleDistal = 53,
    
    
    LastBone = 55
}

internal enum HumanParameter
{
    UpperArmTwist = 0,
    LowerArmTwist,
    UpperLegTwist,
    LowerLegTwist,
    ArmStretch,
    LegStretch,
    FeetSpacing
}

public sealed partial class Avatar : Object
{
    public extern  bool isValid
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    private Avatar()
        {
        }
    
    
    public extern  bool isHuman
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void SetMuscleMinMax (int muscleId, float min, float max) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void SetParameter (int parameterId, float value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal float GetAxisLength (int humanId) ;

    internal Quaternion GetPreRotation (int humanId) {
        Quaternion result;
        INTERNAL_CALL_GetPreRotation ( this, humanId, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetPreRotation (Avatar self, int humanId, out Quaternion value);
    internal Quaternion GetPostRotation (int humanId) {
        Quaternion result;
        INTERNAL_CALL_GetPostRotation ( this, humanId, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetPostRotation (Avatar self, int humanId, out Quaternion value);
    internal Quaternion GetZYPostQ (int humanId, Quaternion parentQ, Quaternion q) {
        Quaternion result;
        INTERNAL_CALL_GetZYPostQ ( this, humanId, ref parentQ, ref q, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetZYPostQ (Avatar self, int humanId, ref Quaternion parentQ, ref Quaternion q, out Quaternion value);
    internal Quaternion GetZYRoll (int humanId, Vector3 uvw) {
        Quaternion result;
        INTERNAL_CALL_GetZYRoll ( this, humanId, ref uvw, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetZYRoll (Avatar self, int humanId, ref Vector3 uvw, out Quaternion value);
    internal Vector3 GetLimitSign (int humanId) {
        Vector3 result;
        INTERNAL_CALL_GetLimitSign ( this, humanId, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetLimitSign (Avatar self, int humanId, out Vector3 value);
}

public sealed partial class HumanTrait
{
    public extern static int MuscleCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static string[] MuscleName
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static int BoneCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static string[] BoneName
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int MuscleFromBone (int i, int dofIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int BoneFromMuscle (int i) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool RequiredBone (int i) ;

    public extern static int RequiredBoneCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool HasCollider (Avatar avatar, int i) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float GetMuscleDefaultMin (int i) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float GetMuscleDefaultMax (int i) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetParentBone (int i) ;

}

}
