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
            return GetSourcePlayableInternal(ref this);
        }
    
    
    internal void SetSourcePlayable(PlayableHandle value)
        {
            SetSourcePlayableInternal(ref this, ref value);
        }
    
    
    internal static PlayableHandle GetSourcePlayableInternal (ref PlayableOutputHandle handle) {
        PlayableHandle result;
        INTERNAL_CALL_GetSourcePlayableInternal ( ref handle, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetSourcePlayableInternal (ref PlayableOutputHandle handle, out PlayableHandle value);
    internal static void SetSourcePlayableInternal (ref PlayableOutputHandle handle, ref PlayableHandle target) {
        INTERNAL_CALL_SetSourcePlayableInternal ( ref handle, ref target );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetSourcePlayableInternal (ref PlayableOutputHandle handle, ref PlayableHandle target);
    internal int GetSourceInputPort()
        {
            return GetSourceInputPortInternal(ref this);
        }
    
    
    internal void SetSourceInputPort(int value)
        {
            SetSourceInputPortInternal(ref this, value);
        }
    
    
    internal static int GetSourceInputPortInternal (ref PlayableOutputHandle handle) {
        return INTERNAL_CALL_GetSourceInputPortInternal ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_GetSourceInputPortInternal (ref PlayableOutputHandle handle);
    internal static void SetSourceInputPortInternal (ref PlayableOutputHandle handle, int port) {
        INTERNAL_CALL_SetSourceInputPortInternal ( ref handle, port );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetSourceInputPortInternal (ref PlayableOutputHandle handle, int port);
    internal float GetWeight()
        {
            return GetWeightInternal(ref this);
        }
    
    
    internal void SetWeight(float value)
        {
            SetWeightInternal(ref this, value);
        }
    
    
    internal static void SetWeightInternal (ref PlayableOutputHandle handle, float weight) {
        INTERNAL_CALL_SetWeightInternal ( ref handle, weight );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetWeightInternal (ref PlayableOutputHandle handle, float weight);
    internal static float GetWeightInternal (ref PlayableOutputHandle handle) {
        return INTERNAL_CALL_GetWeightInternal ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static float INTERNAL_CALL_GetWeightInternal (ref PlayableOutputHandle handle);
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
