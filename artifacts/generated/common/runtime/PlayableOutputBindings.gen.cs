// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngine.Playables
{
[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct PlayableOutputHandle
{
    internal IntPtr m_Handle;
    internal int m_Version;
    
    
    internal bool IsValid()
        {
            return IsValidInternal(ref this);
        }
    
    
    internal static bool IsValidInternal (ref PlayableOutputHandle handle) {
        return INTERNAL_CALL_IsValidInternal ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_IsValidInternal (ref PlayableOutputHandle handle);
    public static PlayableOutputHandle Null
        {
            get { return new PlayableOutputHandle() { m_Version = Int32.MaxValue }; }
        }
    
    
    internal static Type GetPlayableOutputTypeOf (ref PlayableOutputHandle handle) {
        return INTERNAL_CALL_GetPlayableOutputTypeOf ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Type INTERNAL_CALL_GetPlayableOutputTypeOf (ref PlayableOutputHandle handle);
    internal bool IsPlayableOutputOfType<T>()
        {
            return GetPlayableOutputTypeOf(ref this) == typeof(T);
        }
    
    
    internal Object GetReferenceObject()
        {
            return GetInternalReferenceObject(ref this);
        }
    
    
    internal void SetReferenceObject(Object value)
        {
            SetInternalReferenceObject(ref this, value);
        }
    
    
    internal static Object GetInternalReferenceObject (ref PlayableOutputHandle handle) {
        return INTERNAL_CALL_GetInternalReferenceObject ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Object INTERNAL_CALL_GetInternalReferenceObject (ref PlayableOutputHandle handle);
    internal static void SetInternalReferenceObject (ref PlayableOutputHandle handle, Object target) {
        INTERNAL_CALL_SetInternalReferenceObject ( ref handle, target );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetInternalReferenceObject (ref PlayableOutputHandle handle, Object target);
    internal Object GetUserData()
        {
            return GetInternalUserData(ref this);
        }
    
    
    internal void SetUserData(Object value)
        {
            SetInternalUserData(ref this, value);
        }
    
    
    internal static Object GetInternalUserData (ref PlayableOutputHandle handle) {
        return INTERNAL_CALL_GetInternalUserData ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Object INTERNAL_CALL_GetInternalUserData (ref PlayableOutputHandle handle);
    internal static void SetInternalUserData (ref PlayableOutputHandle handle, [Writable] Object target) {
        INTERNAL_CALL_SetInternalUserData ( ref handle, target );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetInternalUserData (ref PlayableOutputHandle handle, [Writable]Object target);
    internal PlayableHandle GetSourcePlayable()
        {
            return InternalGetSourcePlayable(ref this);
        }
    
    
    internal void SetSourcePlayable(PlayableHandle value)
        {
            InternalSetSourcePlayable(ref this, ref value);
        }
    
    
    internal static PlayableHandle InternalGetSourcePlayable (ref PlayableOutputHandle handle) {
        PlayableHandle result;
        INTERNAL_CALL_InternalGetSourcePlayable ( ref handle, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalGetSourcePlayable (ref PlayableOutputHandle handle, out PlayableHandle value);
    internal static void InternalSetSourcePlayable (ref PlayableOutputHandle handle, ref PlayableHandle target) {
        INTERNAL_CALL_InternalSetSourcePlayable ( ref handle, ref target );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalSetSourcePlayable (ref PlayableOutputHandle handle, ref PlayableHandle target);
    internal int GetSourceInputPort()
        {
            return InternalGetSourceInputPort(ref this);
        }
    
    
    internal void SetSourceInputPort(int value)
        {
            InternalSetSourceInputPort(ref this, value);
        }
    
    
    internal static int InternalGetSourceInputPort (ref PlayableOutputHandle handle) {
        return INTERNAL_CALL_InternalGetSourceInputPort ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_InternalGetSourceInputPort (ref PlayableOutputHandle handle);
    internal static void InternalSetSourceInputPort (ref PlayableOutputHandle handle, int port) {
        INTERNAL_CALL_InternalSetSourceInputPort ( ref handle, port );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalSetSourceInputPort (ref PlayableOutputHandle handle, int port);
    internal float GetWeight()
        {
            return InternalGetWeight(ref this);
        }
    
    
    internal void SetWeight(float value)
        {
            InternalSetWeight(ref this, value);
        }
    
    
    internal static void InternalSetWeight (ref PlayableOutputHandle handle, float weight) {
        INTERNAL_CALL_InternalSetWeight ( ref handle, weight );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalSetWeight (ref PlayableOutputHandle handle, float weight);
    internal static float InternalGetWeight (ref PlayableOutputHandle handle) {
        return INTERNAL_CALL_InternalGetWeight ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static float INTERNAL_CALL_InternalGetWeight (ref PlayableOutputHandle handle);
    public override int GetHashCode()
        {
            return m_Handle.GetHashCode() ^ m_Version.GetHashCode();
        }
    
    
    public static bool operator==(PlayableOutputHandle lhs, PlayableOutputHandle rhs)
        {
            return CompareVersion(lhs, rhs);
        }
    
    
    public static bool operator!=(PlayableOutputHandle lhs, PlayableOutputHandle rhs)
        {
            return !CompareVersion(lhs, rhs);
        }
    
    
    public override bool Equals(object p)
        {
            return p is PlayableOutputHandle && CompareVersion(this, (PlayableOutputHandle)p);
        }
    
    
    static internal bool CompareVersion(PlayableOutputHandle lhs, PlayableOutputHandle rhs)
        {
            return (lhs.m_Handle == rhs.m_Handle) && (lhs.m_Version == rhs.m_Version);
        }
    
    
}

[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct PlayableOutput
{
}

}
