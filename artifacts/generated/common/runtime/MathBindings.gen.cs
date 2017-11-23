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
using System.Threading;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine
{
public enum WeightedMode
{
    None = 0,
    In = 1 << 0,
    Out = 1 << 1,
    Both = In | Out
}

[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct Keyframe
{
    float m_Time;
    float m_Value;
    float m_InTangent;
    float m_OutTangent;
    
    int m_TangentMode;
    
    
    int m_WeightedMode;
    
    
    float m_InWeight;
    float m_OutWeight;
    
    
    public Keyframe(float time, float value)
        {
            m_Time = time;
            m_Value = value;
            m_InTangent = 0;
            m_OutTangent = 0;
            m_WeightedMode = 0;
            m_InWeight = 0f;
            m_OutWeight = 0f;
            m_TangentMode = 0;
        }
    
    
    public Keyframe(float time, float value, float inTangent, float outTangent)
        {
            m_Time = time;
            m_Value = value;
            m_InTangent = inTangent;
            m_OutTangent = outTangent;
            m_WeightedMode = 0;
            m_InWeight = 0f;
            m_OutWeight = 0f;
            m_TangentMode = 0;
        }
    
    
    public Keyframe(float time, float value, float inTangent, float outTangent, float inWeight, float outWeight)
        {
            m_Time = time;
            m_Value = value;
            m_InTangent = inTangent;
            m_OutTangent = outTangent;
            m_WeightedMode = (int)WeightedMode.Both;
            m_InWeight = inWeight;
            m_OutWeight = outWeight;
            m_TangentMode = 0;
        }
    
    
    public float time { get { return m_Time; } set { m_Time = value; }  }
    
    
    public float value { get { return m_Value; } set { m_Value = value; }  }
    
    
    public float inTangent { get { return m_InTangent; } set { m_InTangent = value; }  }
    
    
    public float outTangent { get { return m_OutTangent; } set { m_OutTangent = value; }  }
    
    
    public float inWeight { get { return m_InWeight; } set { m_InWeight = value; }  }
    
    
    public float outWeight { get { return m_OutWeight; } set { m_OutWeight = value; }  }
    
    
    public WeightedMode weightedMode { get { return (WeightedMode)m_WeightedMode; } set { m_WeightedMode = (int)value; } }
    
    
    [System.Obsolete ("Use AnimationUtility.SetLeftTangentMode, AnimationUtility.SetRightTangentMode, AnimationUtility.GetLeftTangentMode or AnimationUtility.GetRightTangentMode instead.")]
    public int tangentMode { get { return tangentModeInternal; } set { tangentModeInternal = value; } }
    
    
    internal int tangentModeInternal
        {
            get
            {
                return m_TangentMode;
            }
            set
            {
                m_TangentMode = value;
            }
        }
    
    
}

public enum WrapMode
{
    
    Once = 1,
    
    Loop = 2,
    
    PingPong = 4,
    
    Default = 0,
    
    ClampForever = 8,
    
    Clamp = 1,
}

#pragma warning disable 414


[StructLayout(LayoutKind.Sequential)]
[RequiredByNativeCode]
public sealed partial class AnimationCurve
{
    
            internal IntPtr m_Ptr;
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Cleanup () ;

    
            ~AnimationCurve()
        {
            Cleanup();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public float Evaluate (float time) ;

    public Keyframe[]  keys { get { return GetKeys(); } set { SetKeys(value); } }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int AddKey (float time, float value) ;

    public int AddKey(Keyframe key) { return AddKey_Internal(key); }
    
    
    private int AddKey_Internal (Keyframe key) {
        return INTERNAL_CALL_AddKey_Internal ( this, ref key );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_AddKey_Internal (AnimationCurve self, ref Keyframe key);
    public int MoveKey (int index, Keyframe key) {
        return INTERNAL_CALL_MoveKey ( this, index, ref key );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_MoveKey (AnimationCurve self, int index, ref Keyframe key);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void RemoveKey (int index) ;

    public Keyframe this[int index]
            {
            get { return GetKey_Internal(index); }
        }
    
    
    public extern  int length
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetKeys (Keyframe[] keys) ;

    private Keyframe GetKey_Internal (int index) {
        Keyframe result;
        INTERNAL_CALL_GetKey_Internal ( this, index, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetKey_Internal (AnimationCurve self, int index, out Keyframe value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private Keyframe[] GetKeys () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SmoothTangents (int index, float weight) ;

    public static AnimationCurve Constant(float timeStart, float timeEnd, float value)
        {
            return Linear(timeStart, value, timeEnd, value);
        }
    
    
    public static AnimationCurve Linear(float timeStart, float valueStart, float timeEnd, float valueEnd)
        {
            float tangent = (valueEnd - valueStart) / (timeEnd - timeStart);
            Keyframe[] keys = { new Keyframe(timeStart, valueStart, 0.0F, tangent), new Keyframe(timeEnd, valueEnd, tangent, 0.0F) };
            return new AnimationCurve(keys);
        }
    
    
    public static AnimationCurve EaseInOut(float timeStart, float valueStart, float timeEnd, float valueEnd)
        {
            Keyframe[] keys = { new Keyframe(timeStart, valueStart, 0.0F, 0.0F), new Keyframe(timeEnd, valueEnd, 0.0F, 0.0F) };
            return new AnimationCurve(keys);
        }
    
    
    public extern  WrapMode preWrapMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  WrapMode postWrapMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public AnimationCurve(params Keyframe[] keys) { Init(keys); }
    
    
    
    
    [RequiredByNativeCode]
    public AnimationCurve()  { Init(null); }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Init (Keyframe[] keys) ;

}

#pragma warning restore 414

}
