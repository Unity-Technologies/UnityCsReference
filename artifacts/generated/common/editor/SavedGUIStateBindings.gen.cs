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

namespace UnityEditor
{



[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
internal partial struct SavedGUIState
{
    internal GUILayoutUtility.LayoutCache layoutCache;
    internal System.IntPtr guiState;
    internal Vector2 screenManagerSize;
    internal Rect renderManagerRect;
    internal GUISkin skin;
    internal int instanceID;
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetupSavedGUIState (out IntPtr state, out Vector2 screenManagerSize) ;

    private static void Internal_ApplySavedGUIState (IntPtr state, Vector2 screenManagerSize) {
        INTERNAL_CALL_Internal_ApplySavedGUIState ( state, ref screenManagerSize );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_ApplySavedGUIState (IntPtr state, ref Vector2 screenManagerSize);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int Internal_GetGUIDepth () ;

    static internal SavedGUIState Create()
        {
            SavedGUIState state = new SavedGUIState();
            if (Internal_GetGUIDepth() > 0)
            {
                state.skin = GUI.skin;
                state.layoutCache = new GUILayoutUtility.LayoutCache(GUILayoutUtility.current);
                state.instanceID = GUIUtility.s_OriginalID;
                Internal_SetupSavedGUIState(out state.guiState, out state.screenManagerSize);
            }
            return state;
        }
    
    
    internal void ApplyAndForget()
        {
            if (layoutCache != null)
            {
                GUILayoutUtility.current = layoutCache;
                GUI.skin = skin;
                GUIUtility.s_OriginalID = instanceID;
                Internal_ApplySavedGUIState(guiState, screenManagerSize);
                GUIClip.Reapply();
            }
        }
    
    
}


} 
