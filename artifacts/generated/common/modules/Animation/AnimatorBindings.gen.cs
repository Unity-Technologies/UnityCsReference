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
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Playables;

namespace UnityEngine
{


public enum AvatarTarget
{
    
    Root = 0,
    
    Body = 1,
    
    LeftFoot = 2,
    
    RightFoot = 3,
    
    LeftHand = 4,
    
    RightHand = 5,
}

public enum AvatarIKGoal
{
    
    LeftFoot = 0,
    
    RightFoot = 1,
    
    LeftHand = 2,
    
    RightHand = 3
}

public enum AvatarIKHint
{
    
    LeftKnee = 0,
    
    RightKnee = 1,
    
    LeftElbow = 2,
    
    RightElbow = 3
}

public enum AnimatorControllerParameterType
{
    Float = 1,
    Int = 3,
    Bool = 4,
    Trigger = 9,
}

internal enum TransitionType
{
    Normal = 1 << 0,
    Entry  = 1 << 1,
    Exit   = 1 << 2
}

public enum AnimatorRecorderMode
{
    Offline,
    Playback,
    Record
}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AnimatorClipInfo
{
    public AnimationClip clip  { get {return m_ClipInstanceID != 0 ? ClipInstanceToScriptingObject(m_ClipInstanceID) : null; } }
    
    
    public float weight        { get { return m_Weight; }}
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  AnimationClip ClipInstanceToScriptingObject (int instanceID) ;

    private int m_ClipInstanceID;
    private float m_Weight;
}

public enum AnimatorCullingMode
{
    
    AlwaysAnimate = 0,
    
    CullUpdateTransforms = 1,
    
    CullCompletely = 2,
    [System.Obsolete ("Enum member AnimatorCullingMode.BasedOnRenderers has been deprecated. Use AnimatorCullingMode.CullUpdateTransforms instead (UnityUpgradable) -> CullUpdateTransforms", true)]
    BasedOnRenderers = 1,
}

public enum AnimatorUpdateMode
{
    Normal = 0,
    AnimatePhysics = 1,
    UnscaledTime = 2
}

[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AnimatorStateInfo
{
    public bool IsName(string name)    { int hash = Animator.StringToHash(name); return hash == m_FullPath || hash == m_Name || hash == m_Path; }
    
    
    public int fullPathHash             { get { return m_FullPath; } }
    
    
    [System.Obsolete ("Use AnimatorStateInfo.fullPathHash instead.")]
    public int nameHash                 { get { return m_Path; } }
    
    
    public int shortNameHash            { get { return m_Name; } }
    
    
    public float normalizedTime         { get { return m_NormalizedTime; } }
    
    
    public float length                 { get { return m_Length; } }
    
    
    public float speed                  { get { return m_Speed; } }
    
    
    public float speedMultiplier        { get { return m_SpeedMultiplier; } }
    
    
    public int tagHash                  { get { return m_Tag; } }
    
    
    public bool IsTag(string tag)      { return Animator.StringToHash(tag) == m_Tag; }
    
    
    public bool loop                    { get { return m_Loop != 0; } }
    
    
    private int    m_Name;
    private int    m_Path;
    private int    m_FullPath;
    private float  m_NormalizedTime;
    private float  m_Length;
    private float  m_Speed;
    private float  m_SpeedMultiplier;
    private int    m_Tag;
    private int    m_Loop;
}

public enum DurationUnit
{
    Fixed,
    Normalized
}

[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AnimatorTransitionInfo
{
    public bool IsName(string name) { return Animator.StringToHash(name) == m_Name  || Animator.StringToHash(name) == m_FullPath; }
    
    
    public bool IsUserName(string name) { return Animator.StringToHash(name) == m_UserName; }
    
    
    
    public int fullPathHash               { get { return m_FullPath; } }
    
    
    public int nameHash                   { get { return m_Name; } }
    
    
    public int userNameHash               { get { return m_UserName; } }
    
    
    public DurationUnit durationUnit      { get { return m_HasFixedDuration ? DurationUnit.Fixed : DurationUnit.Normalized; } }
    
    
    public float duration                 { get { return m_Duration; } }
    
    
    public float normalizedTime           { get { return m_NormalizedTime; } }
    
    
    public bool anyState                  { get { return m_AnyState; } }
    
    
    internal bool entry                   { get { return (m_TransitionType & (int)TransitionType.Entry) != 0; }}
    
    
    internal bool exit                    { get { return (m_TransitionType & (int)TransitionType.Exit) != 0; }}
    
    
    private int   m_FullPath;
    private int   m_UserName;
    private int   m_Name;
    private bool  m_HasFixedDuration;
    private float m_Duration;
    private float m_NormalizedTime;
    private bool  m_AnyState;
    private int   m_TransitionType;
    
    
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct MatchTargetWeightMask
{
    public MatchTargetWeightMask(Vector3 positionXYZWeight, float rotationWeight)
        {
            m_PositionXYZWeight = positionXYZWeight;
            m_RotationWeight = rotationWeight;
        }
    
    
    public Vector3 positionXYZWeight
        {
            get { return m_PositionXYZWeight; }
            set { m_PositionXYZWeight = value; }
        }
    
    
    public float rotationWeight
        {
            get { return m_RotationWeight; }
            set { m_RotationWeight = value; }
        }
    
    
    private Vector3 m_PositionXYZWeight;
    private float m_RotationWeight;
}

[UsedByNativeCode]
public sealed partial class Animator : Behaviour
{
    public extern  bool isOptimizable
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool isHuman
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool hasRootMotion
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern  bool isRootPositionOrRotationControlledByCurves
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  float humanScale
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool isInitialized
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public float GetFloat(string name)             { return GetFloatString(name); }
    public float GetFloat(int id)                  { return GetFloatID(id); }
    public void SetFloat(string name, float value) { SetFloatString(name, value); }
    public void SetFloat(string name, float value, float dampTime, float deltaTime) { SetFloatStringDamp(name, value, dampTime, deltaTime); }
    
    
    public void SetFloat(int id, float value)       { SetFloatID(id, value); }
    public void SetFloat(int id, float value, float dampTime, float deltaTime) { SetFloatIDDamp(id, value, dampTime, deltaTime); }
    
    
    public bool GetBool(string name)                { return GetBoolString(name); }
    public bool GetBool(int id)                     { return GetBoolID(id); }
    public void SetBool(string name, bool value)    { SetBoolString(name, value); }
    public void SetBool(int id, bool value)         { SetBoolID(id, value); }
    
    
    public int GetInteger(string name)              { return GetIntegerString(name); }
    public int GetInteger(int id)                   { return GetIntegerID(id); }
    public void SetInteger(string name, int value)  { SetIntegerString(name, value); }
    
    
    public void SetInteger(int id, int value)       { SetIntegerID(id, value); }
    
    
    public void SetTrigger(string name)       { SetTriggerString(name); }
    
    
    public void SetTrigger(int id)       { SetTriggerID(id); }
    
    
    public void ResetTrigger(string name)       { ResetTriggerString(name); }
    
    
    public void ResetTrigger(int id)       { ResetTriggerID(id); }
    
    
    public bool IsParameterControlledByCurve(string name)     { return IsParameterControlledByCurveString(name); }
    public bool IsParameterControlledByCurve(int id)          { return IsParameterControlledByCurveID(id); }
    
    
    public  Vector3 deltaPosition
    {
        get { Vector3 tmp; INTERNAL_get_deltaPosition(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_deltaPosition (out Vector3 value) ;


    public  Quaternion deltaRotation
    {
        get { Quaternion tmp; INTERNAL_get_deltaRotation(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_deltaRotation (out Quaternion value) ;


    public  Vector3 velocity
    {
        get { Vector3 tmp; INTERNAL_get_velocity(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_velocity (out Vector3 value) ;


    public  Vector3 angularVelocity
    {
        get { Vector3 tmp; INTERNAL_get_angularVelocity(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_angularVelocity (out Vector3 value) ;


    public  Vector3 rootPosition
    {
        get { Vector3 tmp; INTERNAL_get_rootPosition(out tmp); return tmp;  }
        set { INTERNAL_set_rootPosition(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_rootPosition (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_rootPosition (ref Vector3 value) ;

    public  Quaternion rootRotation
    {
        get { Quaternion tmp; INTERNAL_get_rootRotation(out tmp); return tmp;  }
        set { INTERNAL_set_rootRotation(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_rootRotation (out Quaternion value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_rootRotation (ref Quaternion value) ;

    public extern  bool applyRootMotion
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  bool linearVelocityBlending
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("Use Animator.updateMode instead")]
    public bool animatePhysics
        {
            get { return updateMode == AnimatorUpdateMode.AnimatePhysics; }
            set { updateMode =  (value ? AnimatorUpdateMode.AnimatePhysics : AnimatorUpdateMode.Normal); }
        }
    
    
    public extern  AnimatorUpdateMode updateMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  bool hasTransformHierarchy
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern  bool allowConstantClipSamplingOptimization
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  float gravityWeight
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public Vector3 bodyPosition
        {
            get { CheckIfInIKPass(); return GetBodyPositionInternal(); }
            set { CheckIfInIKPass(); SetBodyPositionInternal(value); }
        }
    internal Vector3 GetBodyPositionInternal () {
        Vector3 result;
        INTERNAL_CALL_GetBodyPositionInternal ( this, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetBodyPositionInternal (Animator self, out Vector3 value);
    internal void SetBodyPositionInternal (Vector3 bodyPosition) {
        INTERNAL_CALL_SetBodyPositionInternal ( this, ref bodyPosition );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetBodyPositionInternal (Animator self, ref Vector3 bodyPosition);
    public Quaternion bodyRotation
        {
            get { CheckIfInIKPass(); return GetBodyRotationInternal(); }
            set { CheckIfInIKPass(); SetBodyRotationInternal(value); }
        }
    internal Quaternion GetBodyRotationInternal () {
        Quaternion result;
        INTERNAL_CALL_GetBodyRotationInternal ( this, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetBodyRotationInternal (Animator self, out Quaternion value);
    internal void SetBodyRotationInternal (Quaternion bodyRotation) {
        INTERNAL_CALL_SetBodyRotationInternal ( this, ref bodyRotation );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetBodyRotationInternal (Animator self, ref Quaternion bodyRotation);
    public Vector3 GetIKPosition(AvatarIKGoal goal) {  CheckIfInIKPass(); return GetIKPositionInternal(goal); }
    internal Vector3 GetIKPositionInternal (AvatarIKGoal goal) {
        Vector3 result;
        INTERNAL_CALL_GetIKPositionInternal ( this, goal, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetIKPositionInternal (Animator self, AvatarIKGoal goal, out Vector3 value);
    public void SetIKPosition(AvatarIKGoal goal, Vector3 goalPosition) { CheckIfInIKPass(); SetIKPositionInternal(goal, goalPosition); }
    internal void SetIKPositionInternal (AvatarIKGoal goal, Vector3 goalPosition) {
        INTERNAL_CALL_SetIKPositionInternal ( this, goal, ref goalPosition );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetIKPositionInternal (Animator self, AvatarIKGoal goal, ref Vector3 goalPosition);
    public Quaternion GetIKRotation(AvatarIKGoal goal) { CheckIfInIKPass(); return GetIKRotationInternal(goal); }
    internal Quaternion GetIKRotationInternal (AvatarIKGoal goal) {
        Quaternion result;
        INTERNAL_CALL_GetIKRotationInternal ( this, goal, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetIKRotationInternal (Animator self, AvatarIKGoal goal, out Quaternion value);
    public void SetIKRotation(AvatarIKGoal goal, Quaternion goalRotation) { CheckIfInIKPass();  SetIKRotationInternal(goal, goalRotation); }
    internal void SetIKRotationInternal (AvatarIKGoal goal, Quaternion goalRotation) {
        INTERNAL_CALL_SetIKRotationInternal ( this, goal, ref goalRotation );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetIKRotationInternal (Animator self, AvatarIKGoal goal, ref Quaternion goalRotation);
    public float GetIKPositionWeight(AvatarIKGoal goal) { CheckIfInIKPass(); return GetIKPositionWeightInternal(goal); }
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal float GetIKPositionWeightInternal (AvatarIKGoal goal) ;

    public void SetIKPositionWeight(AvatarIKGoal goal, float value) { CheckIfInIKPass(); SetIKPositionWeightInternal(goal, value); }
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void SetIKPositionWeightInternal (AvatarIKGoal goal, float value) ;

    public float GetIKRotationWeight(AvatarIKGoal goal) { CheckIfInIKPass(); return GetIKRotationWeightInternal(goal); }
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal float GetIKRotationWeightInternal (AvatarIKGoal goal) ;

    public void SetIKRotationWeight(AvatarIKGoal goal, float value) { CheckIfInIKPass(); SetIKRotationWeightInternal(goal, value); }
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void SetIKRotationWeightInternal (AvatarIKGoal goal, float value) ;

    public Vector3 GetIKHintPosition(AvatarIKHint hint) {  CheckIfInIKPass(); return GetIKHintPositionInternal(hint); }
    internal Vector3 GetIKHintPositionInternal (AvatarIKHint hint) {
        Vector3 result;
        INTERNAL_CALL_GetIKHintPositionInternal ( this, hint, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetIKHintPositionInternal (Animator self, AvatarIKHint hint, out Vector3 value);
    public void SetIKHintPosition(AvatarIKHint hint, Vector3 hintPosition) { CheckIfInIKPass(); SetIKHintPositionInternal(hint, hintPosition); }
    internal void SetIKHintPositionInternal (AvatarIKHint hint, Vector3 hintPosition) {
        INTERNAL_CALL_SetIKHintPositionInternal ( this, hint, ref hintPosition );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetIKHintPositionInternal (Animator self, AvatarIKHint hint, ref Vector3 hintPosition);
    public float GetIKHintPositionWeight(AvatarIKHint hint) { CheckIfInIKPass(); return GetHintWeightPositionInternal(hint); }
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal float GetHintWeightPositionInternal (AvatarIKHint hint) ;

    public void SetIKHintPositionWeight(AvatarIKHint hint, float value) { CheckIfInIKPass(); SetIKHintPositionWeightInternal(hint, value); }
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void SetIKHintPositionWeightInternal (AvatarIKHint hint, float value) ;

    public void SetLookAtPosition(Vector3 lookAtPosition) { CheckIfInIKPass(); SetLookAtPositionInternal(lookAtPosition); }
    internal void SetLookAtPositionInternal (Vector3 lookAtPosition) {
        INTERNAL_CALL_SetLookAtPositionInternal ( this, ref lookAtPosition );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetLookAtPositionInternal (Animator self, ref Vector3 lookAtPosition);
    [uei.ExcludeFromDocs]
public void SetLookAtWeight (float weight, float bodyWeight , float headWeight , float eyesWeight ) {
    float clampWeight = 0.50f;
    SetLookAtWeight ( weight, bodyWeight, headWeight, eyesWeight, clampWeight );
}

[uei.ExcludeFromDocs]
public void SetLookAtWeight (float weight, float bodyWeight , float headWeight ) {
    float clampWeight = 0.50f;
    float eyesWeight = 0.00f;
    SetLookAtWeight ( weight, bodyWeight, headWeight, eyesWeight, clampWeight );
}

[uei.ExcludeFromDocs]
public void SetLookAtWeight (float weight, float bodyWeight ) {
    float clampWeight = 0.50f;
    float eyesWeight = 0.00f;
    float headWeight = 1.00f;
    SetLookAtWeight ( weight, bodyWeight, headWeight, eyesWeight, clampWeight );
}

[uei.ExcludeFromDocs]
public void SetLookAtWeight (float weight) {
    float clampWeight = 0.50f;
    float eyesWeight = 0.00f;
    float headWeight = 1.00f;
    float bodyWeight = 0.00f;
    SetLookAtWeight ( weight, bodyWeight, headWeight, eyesWeight, clampWeight );
}

public void SetLookAtWeight(float weight, [uei.DefaultValue("0.00f")]  float bodyWeight , [uei.DefaultValue("1.00f")]  float headWeight , [uei.DefaultValue("0.00f")]  float eyesWeight , [uei.DefaultValue("0.50f")]  float clampWeight )
        {
            CheckIfInIKPass();
            SetLookAtWeightInternal(weight, bodyWeight, headWeight, eyesWeight, clampWeight);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void SetLookAtWeightInternal (float weight, [uei.DefaultValue("0.00f")]  float bodyWeight , [uei.DefaultValue("1.00f")]  float headWeight , [uei.DefaultValue("0.00f")]  float eyesWeight , [uei.DefaultValue("0.50f")]  float clampWeight ) ;

    [uei.ExcludeFromDocs]
    internal void SetLookAtWeightInternal (float weight, float bodyWeight , float headWeight , float eyesWeight ) {
        float clampWeight = 0.50f;
        SetLookAtWeightInternal ( weight, bodyWeight, headWeight, eyesWeight, clampWeight );
    }

    [uei.ExcludeFromDocs]
    internal void SetLookAtWeightInternal (float weight, float bodyWeight , float headWeight ) {
        float clampWeight = 0.50f;
        float eyesWeight = 0.00f;
        SetLookAtWeightInternal ( weight, bodyWeight, headWeight, eyesWeight, clampWeight );
    }

    [uei.ExcludeFromDocs]
    internal void SetLookAtWeightInternal (float weight, float bodyWeight ) {
        float clampWeight = 0.50f;
        float eyesWeight = 0.00f;
        float headWeight = 1.00f;
        SetLookAtWeightInternal ( weight, bodyWeight, headWeight, eyesWeight, clampWeight );
    }

    [uei.ExcludeFromDocs]
    internal void SetLookAtWeightInternal (float weight) {
        float clampWeight = 0.50f;
        float eyesWeight = 0.00f;
        float headWeight = 1.00f;
        float bodyWeight = 0.00f;
        SetLookAtWeightInternal ( weight, bodyWeight, headWeight, eyesWeight, clampWeight );
    }

    public void SetBoneLocalRotation(HumanBodyBones humanBoneId, Quaternion rotation) { CheckIfInIKPass(); SetBoneLocalRotationInternal((int)humanBoneId, rotation); }
    internal void SetBoneLocalRotationInternal (int humanBoneId, Quaternion rotation) {
        INTERNAL_CALL_SetBoneLocalRotationInternal ( this, humanBoneId, ref rotation );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetBoneLocalRotationInternal (Animator self, int humanBoneId, ref Quaternion rotation);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal ScriptableObject GetBehaviour (Type type) ;

    
    public T GetBehaviour<T>() where T : StateMachineBehaviour { return GetBehaviour(typeof(T)) as T; }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal ScriptableObject[] InternalGetBehaviours (Type type) ;

    internal static T[] ConvertStateMachineBehaviour<T>(ScriptableObject[] rawObjects) where T : StateMachineBehaviour
        {
            if (rawObjects == null) return null;
            T[] typedObjects = new T[rawObjects.Length];
            for (int i = 0; i < typedObjects.Length; i++)
                typedObjects[i] = (T)rawObjects[i];
            return typedObjects;
        }
    
    
    
    public T[] GetBehaviours<T>() where T : StateMachineBehaviour
        {
            return ConvertStateMachineBehaviour<T>(InternalGetBehaviours(typeof(T)));
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal StateMachineBehaviour[] InternalGetBehavioursByKey (int fullPathHash, int layerIndex, Type type) ;

    
    public StateMachineBehaviour[] GetBehaviours(int fullPathHash, int layerIndex)
        {
            return InternalGetBehavioursByKey(fullPathHash, layerIndex, typeof(StateMachineBehaviour));
        }
    
    
    public extern  bool stabilizeFeet
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  int layerCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public string GetLayerName (int layerIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetLayerIndex (string layerName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public float GetLayerWeight (int layerIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetLayerWeight (int layerIndex, float weight) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public AnimatorStateInfo GetCurrentAnimatorStateInfo (int layerIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public AnimatorStateInfo GetNextAnimatorStateInfo (int layerIndex) ;

    public AnimatorTransitionInfo GetAnimatorTransitionInfo(int layerIndex)
        {
            AnimatorTransitionInfo  info;
            GetAnimatorTransitionInfo(layerIndex, out info);
            return info;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void GetAnimatorTransitionInfo (int layerIndex, out AnimatorTransitionInfo info) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetCurrentAnimatorClipInfoCount (int layerIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public AnimatorClipInfo[] GetCurrentAnimatorClipInfo (int layerIndex) ;

    public void GetCurrentAnimatorClipInfo(int layerIndex, List<AnimatorClipInfo> clips)
        {
            if (clips == null) throw new ArgumentNullException("clips");

            GetAnimatorClipInfoInternal(layerIndex, true, clips);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void GetAnimatorClipInfoInternal (int layerIndex, bool isCurrent, object clips) ;

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetNextAnimatorClipInfoCount (int layerIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public AnimatorClipInfo[] GetNextAnimatorClipInfo (int layerIndex) ;

    public void GetNextAnimatorClipInfo(int layerIndex, List<AnimatorClipInfo> clips)
        {
            if (clips == null) throw new ArgumentNullException("clips");

            GetAnimatorClipInfoInternal(layerIndex, false, clips);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool IsInTransition (int layerIndex) ;

    public extern  AnimatorControllerParameter[] parameters
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int parameterCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public AnimatorControllerParameter GetParameter(int index)
        {
            AnimatorControllerParameter[] param = parameters;
            if (index < 0 && index >= parameters.Length)
                throw new IndexOutOfRangeException("Index must be between 0 and " + parameters.Length);
            return param[index];
        }
    
    
    
    public extern  float feetPivotActive
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  float pivotWeight
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public  Vector3 pivotPosition
    {
        get { Vector3 tmp; INTERNAL_get_pivotPosition(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_pivotPosition (out Vector3 value) ;


    public void MatchTarget (Vector3 matchPosition,  Quaternion matchRotation, AvatarTarget targetBodyPart,  MatchTargetWeightMask weightMask, float startNormalizedTime, [uei.DefaultValue("1")]  float targetNormalizedTime ) {
        INTERNAL_CALL_MatchTarget ( this, ref matchPosition, ref matchRotation, targetBodyPart, ref weightMask, startNormalizedTime, targetNormalizedTime );
    }

    [uei.ExcludeFromDocs]
    public void MatchTarget (Vector3 matchPosition, Quaternion matchRotation, AvatarTarget targetBodyPart, MatchTargetWeightMask weightMask, float startNormalizedTime) {
        float targetNormalizedTime = 1;
        INTERNAL_CALL_MatchTarget ( this, ref matchPosition, ref matchRotation, targetBodyPart, ref weightMask, startNormalizedTime, targetNormalizedTime );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_MatchTarget (Animator self, ref Vector3 matchPosition, ref Quaternion matchRotation, AvatarTarget targetBodyPart, ref MatchTargetWeightMask weightMask, float startNormalizedTime, float targetNormalizedTime);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InterruptMatchTarget ( [uei.DefaultValue("true")] bool completeMatch ) ;

    [uei.ExcludeFromDocs]
    public void InterruptMatchTarget () {
        bool completeMatch = true;
        InterruptMatchTarget ( completeMatch );
    }

    public extern  bool isMatchingTarget
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  float speed
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("ForceStateNormalizedTime is deprecated. Please use Play or CrossFade instead.")]
public void ForceStateNormalizedTime(float normalizedTime) { Play(0, 0, normalizedTime); }
    
    
    [uei.ExcludeFromDocs]
public void CrossFadeInFixedTime (string stateName, float fixedTransitionDuration, int layer , float fixedTimeOffset ) {
    float normalizedTransitionTime = 0.0f;
    CrossFadeInFixedTime ( stateName, fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime );
}

[uei.ExcludeFromDocs]
public void CrossFadeInFixedTime (string stateName, float fixedTransitionDuration, int layer ) {
    float normalizedTransitionTime = 0.0f;
    float fixedTimeOffset = 0.0f;
    CrossFadeInFixedTime ( stateName, fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime );
}

[uei.ExcludeFromDocs]
public void CrossFadeInFixedTime (string stateName, float fixedTransitionDuration) {
    float normalizedTransitionTime = 0.0f;
    float fixedTimeOffset = 0.0f;
    int layer = -1;
    CrossFadeInFixedTime ( stateName, fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime );
}

public void CrossFadeInFixedTime(string stateName, float fixedTransitionDuration, [uei.DefaultValue("-1")]  int layer , [uei.DefaultValue("0.0f")]  float fixedTimeOffset , [uei.DefaultValue("0.0f")]  float normalizedTransitionTime )
        {
            CrossFadeInFixedTime(StringToHash(stateName), fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void CrossFadeInFixedTime (int stateHashName, float fixedTransitionDuration, [uei.DefaultValue("-1")]  int layer , [uei.DefaultValue("0.0f")]  float fixedTimeOffset , [uei.DefaultValue("0.0f")]  float normalizedTransitionTime ) ;

    [uei.ExcludeFromDocs]
    public void CrossFadeInFixedTime (int stateHashName, float fixedTransitionDuration, int layer , float fixedTimeOffset ) {
        float normalizedTransitionTime = 0.0f;
        CrossFadeInFixedTime ( stateHashName, fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime );
    }

    [uei.ExcludeFromDocs]
    public void CrossFadeInFixedTime (int stateHashName, float fixedTransitionDuration, int layer ) {
        float normalizedTransitionTime = 0.0f;
        float fixedTimeOffset = 0.0f;
        CrossFadeInFixedTime ( stateHashName, fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime );
    }

    [uei.ExcludeFromDocs]
    public void CrossFadeInFixedTime (int stateHashName, float fixedTransitionDuration) {
        float normalizedTransitionTime = 0.0f;
        float fixedTimeOffset = 0.0f;
        int layer = -1;
        CrossFadeInFixedTime ( stateHashName, fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime );
    }

    [uei.ExcludeFromDocs]
public void CrossFade (string stateName, float normalizedTransitionDuration, int layer , float normalizedTimeOffset ) {
    float normalizedTransitionTime = 0.0f;
    CrossFade ( stateName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime );
}

[uei.ExcludeFromDocs]
public void CrossFade (string stateName, float normalizedTransitionDuration, int layer ) {
    float normalizedTransitionTime = 0.0f;
    float normalizedTimeOffset = float.NegativeInfinity;
    CrossFade ( stateName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime );
}

[uei.ExcludeFromDocs]
public void CrossFade (string stateName, float normalizedTransitionDuration) {
    float normalizedTransitionTime = 0.0f;
    float normalizedTimeOffset = float.NegativeInfinity;
    int layer = -1;
    CrossFade ( stateName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime );
}

public void CrossFade(string stateName, float normalizedTransitionDuration, [uei.DefaultValue("-1")]  int layer , [uei.DefaultValue("float.NegativeInfinity")]  float normalizedTimeOffset , [uei.DefaultValue("0.0f")]  float normalizedTransitionTime )
        {
            CrossFade(StringToHash(stateName), normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void CrossFade (int stateHashName, float normalizedTransitionDuration, [uei.DefaultValue("-1")]  int layer , [uei.DefaultValue("float.NegativeInfinity")]  float normalizedTimeOffset , [uei.DefaultValue("0.0f")]  float normalizedTransitionTime ) ;

    [uei.ExcludeFromDocs]
    public void CrossFade (int stateHashName, float normalizedTransitionDuration, int layer , float normalizedTimeOffset ) {
        float normalizedTransitionTime = 0.0f;
        CrossFade ( stateHashName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime );
    }

    [uei.ExcludeFromDocs]
    public void CrossFade (int stateHashName, float normalizedTransitionDuration, int layer ) {
        float normalizedTransitionTime = 0.0f;
        float normalizedTimeOffset = float.NegativeInfinity;
        CrossFade ( stateHashName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime );
    }

    [uei.ExcludeFromDocs]
    public void CrossFade (int stateHashName, float normalizedTransitionDuration) {
        float normalizedTransitionTime = 0.0f;
        float normalizedTimeOffset = float.NegativeInfinity;
        int layer = -1;
        CrossFade ( stateHashName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime );
    }

    [uei.ExcludeFromDocs]
public void PlayInFixedTime (string stateName, int layer ) {
    float fixedTime = float.NegativeInfinity;
    PlayInFixedTime ( stateName, layer, fixedTime );
}

[uei.ExcludeFromDocs]
public void PlayInFixedTime (string stateName) {
    float fixedTime = float.NegativeInfinity;
    int layer = -1;
    PlayInFixedTime ( stateName, layer, fixedTime );
}

public void PlayInFixedTime(string stateName, [uei.DefaultValue("-1")]  int layer , [uei.DefaultValue("float.NegativeInfinity")]  float fixedTime )
        {
            PlayInFixedTime(StringToHash(stateName), layer, fixedTime);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void PlayInFixedTime (int stateNameHash, [uei.DefaultValue("-1")]  int layer , [uei.DefaultValue("float.NegativeInfinity")]  float fixedTime ) ;

    [uei.ExcludeFromDocs]
    public void PlayInFixedTime (int stateNameHash, int layer ) {
        float fixedTime = float.NegativeInfinity;
        PlayInFixedTime ( stateNameHash, layer, fixedTime );
    }

    [uei.ExcludeFromDocs]
    public void PlayInFixedTime (int stateNameHash) {
        float fixedTime = float.NegativeInfinity;
        int layer = -1;
        PlayInFixedTime ( stateNameHash, layer, fixedTime );
    }

    [uei.ExcludeFromDocs]
public void Play (string stateName, int layer ) {
    float normalizedTime = float.NegativeInfinity;
    Play ( stateName, layer, normalizedTime );
}

[uei.ExcludeFromDocs]
public void Play (string stateName) {
    float normalizedTime = float.NegativeInfinity;
    int layer = -1;
    Play ( stateName, layer, normalizedTime );
}

public void Play(string stateName, [uei.DefaultValue("-1")]  int layer , [uei.DefaultValue("float.NegativeInfinity")]  float normalizedTime )
        {
            Play(StringToHash(stateName), layer, normalizedTime);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Play (int stateNameHash, [uei.DefaultValue("-1")]  int layer , [uei.DefaultValue("float.NegativeInfinity")]  float normalizedTime ) ;

    [uei.ExcludeFromDocs]
    public void Play (int stateNameHash, int layer ) {
        float normalizedTime = float.NegativeInfinity;
        Play ( stateNameHash, layer, normalizedTime );
    }

    [uei.ExcludeFromDocs]
    public void Play (int stateNameHash) {
        float normalizedTime = float.NegativeInfinity;
        int layer = -1;
        Play ( stateNameHash, layer, normalizedTime );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetTarget (AvatarTarget targetIndex, float targetNormalizedTime) ;

    public  Vector3 targetPosition
    {
        get { Vector3 tmp; INTERNAL_get_targetPosition(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_targetPosition (out Vector3 value) ;


    public  Quaternion targetRotation
    {
        get { Quaternion tmp; INTERNAL_get_targetRotation(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_targetRotation (out Quaternion value) ;


    [System.Obsolete ("use mask and layers to control subset of transfroms in a skeleton", true)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool IsControlled (Transform transform) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal bool IsBoneTransform (Transform transform) ;

    internal extern  Transform avatarRoot
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public Transform GetBoneTransform(HumanBodyBones humanBoneId) {return GetBoneTransformInternal((int)humanBoneId); }
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal Transform GetBoneTransformInternal (int humanBoneId) ;

    public extern AnimatorCullingMode cullingMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void StartPlayback () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void StopPlayback () ;

    public extern  float playbackTime
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void StartRecording (int frameCount) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void StopRecording () ;

    public extern  float recorderStartTime
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  float recorderStopTime
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  AnimatorRecorderMode recorderMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  RuntimeAnimatorController runtimeAnimatorController
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  bool hasBoundPlayables
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void ClearInternalControllerPlayable () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool HasState (int layerIndex, int stateID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int StringToHash (string name) ;

    public extern  Avatar avatar
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal string GetStats () ;

    public PlayableGraph playableGraph
        {
            get
            {
                PlayableGraph graph = new PlayableGraph();
                InternalGetCurrentGraph(ref graph);
                return graph;
            }
        }
    
    
    private void InternalGetCurrentGraph (ref PlayableGraph graph) {
        INTERNAL_CALL_InternalGetCurrentGraph ( this, ref graph );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalGetCurrentGraph (Animator self, ref PlayableGraph graph);
    private void CheckIfInIKPass()
        {
            if (logWarnings && !CheckIfInIKPassInternal())
                Debug.LogWarning("Setting and getting Body Position/Rotation, IK Goals, Lookat and BoneLocalRotation should only be done in OnAnimatorIK or OnStateIK");
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private bool CheckIfInIKPassInternal () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetFloatString (string name, float value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetFloatID (int id, float value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private float GetFloatString (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private float GetFloatID (int id) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetBoolString (string name, bool value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetBoolID (int id, bool value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private bool GetBoolString (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private bool GetBoolID (int id) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetIntegerString (string name, int value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetIntegerID (int id, int value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private int GetIntegerString (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private int GetIntegerID (int id) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetTriggerString (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetTriggerID (int id) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void ResetTriggerString (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void ResetTriggerID (int id) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private bool IsParameterControlledByCurveString (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private bool IsParameterControlledByCurveID (int id) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetFloatStringDamp (string name, float value, float dampTime, float deltaTime) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetFloatIDDamp (int id, float value, float dampTime, float deltaTime) ;

    public extern  bool layersAffectMassCenter
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  float leftFeetBottomHeight
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  float rightFeetBottomHeight
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern  bool supportsOnAnimatorMove
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void OnUpdateModeChanged () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void OnCullingModeChanged () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void WriteDefaultPose () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Update (float deltaTime) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Rebind () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void ApplyBuiltinRootMotion () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void EvaluateController () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal string GetCurrentStateName (int layerIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal string GetNextStateName (int layerIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal string ResolveHash (int hash) ;

    public extern bool logWarnings
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool fireEvents
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("GetVector is deprecated.")]
public Vector3 GetVector(string name)                     { return Vector3.zero; }
    [System.Obsolete ("GetVector is deprecated.")]
public Vector3 GetVector(int id)                          { return Vector3.zero; }
    [System.Obsolete ("SetVector is deprecated.")]
public void SetVector(string name, Vector3 value)         {}
    [System.Obsolete ("SetVector is deprecated.")]
public void SetVector(int id, Vector3 value)              {}
    
    
    [System.Obsolete ("GetQuaternion is deprecated.")]
public Quaternion GetQuaternion(string name)              { return Quaternion.identity; }
    [System.Obsolete ("GetQuaternion is deprecated.")]
public Quaternion GetQuaternion(int id)                   { return Quaternion.identity; }
    [System.Obsolete ("SetQuaternion is deprecated.")]
public void SetQuaternion(string name, Quaternion value)  {}
    [System.Obsolete ("SetQuaternion is deprecated.")]
public void SetQuaternion(int id, Quaternion value)       {}
    
    
}

}
