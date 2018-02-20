// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/SavedGUIState.bindings.h")]
    internal struct SavedGUIState
    {
        internal GUILayoutUtility.LayoutCache layoutCache;
        internal IntPtr guiState;
        internal Vector2 screenManagerSize;
        internal Rect renderManagerRect;
        internal GUISkin skin;
        internal int instanceID;

        static private extern void Internal_SetupSavedGUIState(out IntPtr state, out Vector2 screenManagerSize);

        static private extern void Internal_ApplySavedGUIState(IntPtr state, Vector2 screenManagerSize);

        static internal extern int Internal_GetGUIDepth();

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
