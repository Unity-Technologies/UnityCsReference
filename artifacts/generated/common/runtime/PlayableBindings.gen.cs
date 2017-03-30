// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Object = UnityEngine.Object;


namespace UnityEngine.Playables
{
public enum PlayState
{
    Paused = 0,
    Playing = 1
}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct PlayableHandle
{
    internal IntPtr m_Handle;
    internal Int32 m_Version;
    
    
    public T GetObject<T>() where T : IPlayable
        {
            if (!IsValid())
                return default(T);

            var playable = GetScriptInstance(ref this);
            if (playable != null) 
                return (T)playable;

            Type type = GetPlayableTypeOf(ref this);

            T instance = (T)Activator.CreateInstance(type);
            ((IPlayable)instance).playableHandle = this;

            SetScriptInstance(ref this, instance);

            return instance;
        }
    
    
    private static object GetScriptInstance (ref PlayableHandle playable) {
        return INTERNAL_CALL_GetScriptInstance ( ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static object INTERNAL_CALL_GetScriptInstance (ref PlayableHandle playable);
    private static void SetScriptInstance (ref PlayableHandle playable, object scriptInstance) {
        INTERNAL_CALL_SetScriptInstance ( ref playable, scriptInstance );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetScriptInstance (ref PlayableHandle playable, object scriptInstance);
    public bool IsValid()
        {
            return IsValidInternal(ref this);
        }
    
    
    private static bool IsValidInternal (ref PlayableHandle playable) {
        return INTERNAL_CALL_IsValidInternal ( ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_IsValidInternal (ref PlayableHandle playable);
    internal static Type GetPlayableTypeOf (ref PlayableHandle playable) {
        return INTERNAL_CALL_GetPlayableTypeOf ( ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Type INTERNAL_CALL_GetPlayableTypeOf (ref PlayableHandle playable);
    public static PlayableHandle Null
        {
            get { return new PlayableHandle() { m_Version = 10 }; }
        }
    
    
    public PlayableGraph graph
        {
            get
            {
                PlayableGraph g = new PlayableGraph();
                GetGraphInternal(ref this, ref g);
                return g;
            }
        }
    
    
    public int inputCount
        {
            get { return GetInputCountInternal(ref this); }
            set { SetInputCountInternal(ref this, value); }
        }
    
    
    public int outputCount
        {
            get { return GetOutputCountInternal(ref this); }
            set { SetOutputCountInternal(ref this, value); }
        }
    
    
    public PlayState playState
        {
            get { return GetPlayStateInternal(ref this);  }
            set { SetPlayStateInternal(ref this, value);  }
        }
    
    
    public double speed
        {
            get { return GetSpeedInternal(ref this);  }
            set { SetSpeedInternal(ref this, value);  }
        }
    
    
    public double time
        {
            get { return GetTimeInternal(ref this); }
            set { SetTimeInternal(ref this, value); }
        }
    
    
    public bool isDone
        {
            get { return InternalGetDone(ref this); }
            set { InternalSetDone(ref this, value); }
        }
    
    
    public bool propagateSetTime
        {
            get { return InternalGetPropagateSetTime(ref this); }
            set { InternalSetPropagateSetTime(ref this, value); }
        }
    
    
    internal bool canChangeInputs
        {
            get { return CanChangeInputsInternal(ref this); }
        }
    
    
    internal bool canSetWeights
        {
            get { return CanSetWeightsInternal(ref this); }
        }
    
    
    internal bool canDestroy
        {
            get { return CanDestroyInternal(ref this); }
        }
    
    
    private static bool CanChangeInputsInternal (ref PlayableHandle playable) {
        return INTERNAL_CALL_CanChangeInputsInternal ( ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CanChangeInputsInternal (ref PlayableHandle playable);
    private static bool CanSetWeightsInternal (ref PlayableHandle playable) {
        return INTERNAL_CALL_CanSetWeightsInternal ( ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CanSetWeightsInternal (ref PlayableHandle playable);
    private static bool CanDestroyInternal (ref PlayableHandle playable) {
        return INTERNAL_CALL_CanDestroyInternal ( ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CanDestroyInternal (ref PlayableHandle playable);
    private static PlayState GetPlayStateInternal (ref PlayableHandle playable) {
        return INTERNAL_CALL_GetPlayStateInternal ( ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static PlayState INTERNAL_CALL_GetPlayStateInternal (ref PlayableHandle playable);
    private static void SetPlayStateInternal (ref PlayableHandle playable, PlayState playState) {
        INTERNAL_CALL_SetPlayStateInternal ( ref playable, playState );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetPlayStateInternal (ref PlayableHandle playable, PlayState playState);
    private static double GetSpeedInternal (ref PlayableHandle playable) {
        return INTERNAL_CALL_GetSpeedInternal ( ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static double INTERNAL_CALL_GetSpeedInternal (ref PlayableHandle playable);
    private static void SetSpeedInternal (ref PlayableHandle playable, double speed) {
        INTERNAL_CALL_SetSpeedInternal ( ref playable, speed );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetSpeedInternal (ref PlayableHandle playable, double speed);
    private static double GetTimeInternal (ref PlayableHandle playable) {
        return INTERNAL_CALL_GetTimeInternal ( ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static double INTERNAL_CALL_GetTimeInternal (ref PlayableHandle playable);
    private static void SetTimeInternal (ref PlayableHandle playable, double time) {
        INTERNAL_CALL_SetTimeInternal ( ref playable, time );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetTimeInternal (ref PlayableHandle playable, double time);
    private static bool InternalGetDone (ref PlayableHandle playable) {
        return INTERNAL_CALL_InternalGetDone ( ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_InternalGetDone (ref PlayableHandle playable);
    private static void InternalSetDone (ref PlayableHandle playable, bool isDone) {
        INTERNAL_CALL_InternalSetDone ( ref playable, isDone );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalSetDone (ref PlayableHandle playable, bool isDone);
    public double duration
        {
            get { return GetDurationInternal(ref this); }
            set { SetDurationInternal(ref this, value); }
        }
    
    
    private static double GetDurationInternal (ref PlayableHandle playable) {
        return INTERNAL_CALL_GetDurationInternal ( ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static double INTERNAL_CALL_GetDurationInternal (ref PlayableHandle playable);
    private static void SetDurationInternal (ref PlayableHandle playable, double duration) {
        INTERNAL_CALL_SetDurationInternal ( ref playable, duration );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetDurationInternal (ref PlayableHandle playable, double duration);
    private static bool InternalGetPropagateSetTime (ref PlayableHandle playable) {
        return INTERNAL_CALL_InternalGetPropagateSetTime ( ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_InternalGetPropagateSetTime (ref PlayableHandle playable);
    private static void InternalSetPropagateSetTime (ref PlayableHandle playable, bool value) {
        INTERNAL_CALL_InternalSetPropagateSetTime ( ref playable, value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalSetPropagateSetTime (ref PlayableHandle playable, bool value);
    private static void GetGraphInternal (ref PlayableHandle playable, ref PlayableGraph graph) {
        INTERNAL_CALL_GetGraphInternal ( ref playable, ref graph );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetGraphInternal (ref PlayableHandle playable, ref PlayableGraph graph);
    private static int GetInputCountInternal (ref PlayableHandle playable) {
        return INTERNAL_CALL_GetInputCountInternal ( ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_GetInputCountInternal (ref PlayableHandle playable);
    private static void SetInputCountInternal (ref PlayableHandle playable, int count) {
        INTERNAL_CALL_SetInputCountInternal ( ref playable, count );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetInputCountInternal (ref PlayableHandle playable, int count);
    private static int GetOutputCountInternal (ref PlayableHandle playable) {
        return INTERNAL_CALL_GetOutputCountInternal ( ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_GetOutputCountInternal (ref PlayableHandle playable);
    private static void SetOutputCountInternal (ref PlayableHandle playable, int count) {
        INTERNAL_CALL_SetOutputCountInternal ( ref playable, count );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetOutputCountInternal (ref PlayableHandle playable, int count);
    public PlayableHandle GetInput(int inputPort)
        {
            return GetInputInternal(ref this, inputPort);
        }
    
    
    private static PlayableHandle GetInputInternal (ref PlayableHandle playable, int index) {
        PlayableHandle result;
        INTERNAL_CALL_GetInputInternal ( ref playable, index, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetInputInternal (ref PlayableHandle playable, int index, out PlayableHandle value);
    public PlayableHandle GetOutput(int outputPort)
        {
            return GetOutputInternal(ref this, outputPort);
        }
    
    
    private static PlayableHandle GetOutputInternal (ref PlayableHandle playable, int index) {
        PlayableHandle result;
        INTERNAL_CALL_GetOutputInternal ( ref playable, index, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetOutputInternal (ref PlayableHandle playable, int index, out PlayableHandle value);
    private static void SetInputWeightFromIndexInternal (ref PlayableHandle playable, int index, float weight) {
        INTERNAL_CALL_SetInputWeightFromIndexInternal ( ref playable, index, weight );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetInputWeightFromIndexInternal (ref PlayableHandle playable, int index, float weight);
    public bool SetInputWeight(int inputIndex, float weight)
        {
            if (CheckInputBounds(inputIndex))
            {
                SetInputWeightFromIndexInternal(ref this, inputIndex, weight);
                return true;
            }
            return false;
        }
    
    
    private static float GetInputWeightFromIndexInternal (ref PlayableHandle playable, int index) {
        return INTERNAL_CALL_GetInputWeightFromIndexInternal ( ref playable, index );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static float INTERNAL_CALL_GetInputWeightFromIndexInternal (ref PlayableHandle playable, int index);
    public float GetInputWeight(int inputIndex)
        {
            if (CheckInputBounds(inputIndex))
            {
                return GetInputWeightFromIndexInternal(ref this, inputIndex);
            }
            return 0.0f;
        }
    
    
    private static void SetInputWeightInternal (ref PlayableHandle playable, ref PlayableHandle input, float weight) {
        INTERNAL_CALL_SetInputWeightInternal ( ref playable, ref input, weight );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetInputWeightInternal (ref PlayableHandle playable, ref PlayableHandle input, float weight);
    public void SetInputWeight(PlayableHandle input, float weight)
        {
            SetInputWeightInternal(ref this, ref input, weight);
        }
    
    
    public void Destroy()
        {
            graph.DestroyPlayable(this);
        }
    
    
    public static bool operator==(PlayableHandle x, PlayableHandle y) { return CompareVersion(x, y); }
    public static bool operator!=(PlayableHandle x, PlayableHandle y) { return !CompareVersion(x, y); }
    
    
    public override bool Equals(object p)
        {
            if (!(p is PlayableHandle))
                return false;
            return CompareVersion(this, (PlayableHandle)p);
        }
    
    
    public override int GetHashCode() { return m_Handle.GetHashCode() ^ m_Version.GetHashCode(); }
    
    
    static internal bool CompareVersion(PlayableHandle lhs, PlayableHandle rhs)
        {
            return (lhs.m_Handle == rhs.m_Handle) && (lhs.m_Version == rhs.m_Version);
        }
    
    
    internal bool CheckInputBounds(int inputIndex)
        {
            return CheckInputBounds(inputIndex, false);
        }
    
    
    internal bool CheckInputBounds(int inputIndex, bool acceptAny)
        {
            if (inputIndex == -1 && acceptAny)
                return true;

            if (inputIndex < 0)
            {
                throw new IndexOutOfRangeException("Index must be greater than 0");
            }

            if (inputCount <= inputIndex)
            {
                throw new IndexOutOfRangeException("inputIndex " + inputIndex +  " is greater than the number of available inputs (" + inputCount + ").");
            }

            return true;
        }
    
    
}


    public interface IPlayable
    {
        PlayableHandle playableHandle {get; set; }
    }


[RequiredByNativeCode]
public partial class Playable : IPlayable
{
    
            public PlayableHandle handle;
    public static implicit operator PlayableHandle(Playable b)            { return b.handle; }
    public bool IsValid() {return handle.IsValid(); }
    
            PlayableHandle IPlayable.playableHandle { get { return handle; } set { handle = value; } }
}


}
