// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine.Playables;

namespace UnityEngine.Animations
{
[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AnimationLayerMixerPlayable
{
    private static bool CreateHandleInternal (PlayableGraph graph, ref PlayableHandle handle) {
        return INTERNAL_CALL_CreateHandleInternal ( ref graph, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CreateHandleInternal (ref PlayableGraph graph, ref PlayableHandle handle);
    public bool IsLayerAdditive(uint layerIndex)
        {
            if (layerIndex >= m_Handle.GetInputCount())
                throw new ArgumentOutOfRangeException("layerIndex", String.Format("layerIndex {0} must be in the range of 0 to {1}.", layerIndex, m_Handle.GetInputCount() - 1));

            return IsLayerAdditiveInternal(ref m_Handle, layerIndex);
        }
    
    
    public void SetLayerAdditive(uint layerIndex, bool value)
        {
            if (layerIndex >= m_Handle.GetInputCount())
                throw new ArgumentOutOfRangeException("layerIndex", String.Format("layerIndex {0} must be in the range of 0 to {1}.", layerIndex, m_Handle.GetInputCount() - 1));

            SetLayerAdditiveInternal(ref m_Handle, layerIndex, value);
        }
    
    
    public void SetLayerMaskFromAvatarMask(uint layerIndex, AvatarMask mask)
        {
            if (layerIndex >= m_Handle.GetInputCount())
                throw new ArgumentOutOfRangeException("layerIndex", String.Format("layerIndex {0} must be in the range of 0 to {1}.", layerIndex, m_Handle.GetInputCount() - 1));

            if (mask == null)
                throw new System.ArgumentNullException("mask");

            SetLayerMaskFromAvatarMaskInternal(ref m_Handle, layerIndex, mask);
        }
    
    
    private static bool IsLayerAdditiveInternal (ref PlayableHandle handle, uint layerIndex) {
        return INTERNAL_CALL_IsLayerAdditiveInternal ( ref handle, layerIndex );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_IsLayerAdditiveInternal (ref PlayableHandle handle, uint layerIndex);
    private static void SetLayerAdditiveInternal (ref PlayableHandle handle, uint layerIndex, bool value) {
        INTERNAL_CALL_SetLayerAdditiveInternal ( ref handle, layerIndex, value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetLayerAdditiveInternal (ref PlayableHandle handle, uint layerIndex, bool value);
    private static void SetLayerMaskFromAvatarMaskInternal (ref PlayableHandle handle, uint layerIndex, AvatarMask mask) {
        INTERNAL_CALL_SetLayerMaskFromAvatarMaskInternal ( ref handle, layerIndex, mask );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetLayerMaskFromAvatarMaskInternal (ref PlayableHandle handle, uint layerIndex, AvatarMask mask);
}

}
