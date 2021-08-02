// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngineInternal;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/SavedGUIState.bindings.h")]
    internal struct SavedGUIState
    {
        private GUILayoutUtility.LayoutCacheState layoutCache;
        private IntPtr guiState;
        private Vector2 screenManagerSize;
        private GUISkin skin;
        private int instanceID;
        private GenericStack scrollViewStates;

        private static extern void Internal_SetupSavedGUIState(out IntPtr state, out Vector2 screenManagerSize);

        private static extern void Internal_ApplySavedGUIState(IntPtr state, Vector2 screenManagerSize);

        internal static extern int Internal_GetGUIDepth();

        internal static SavedGUIState Create()
        {
            SavedGUIState state = new SavedGUIState();
            if (Internal_GetGUIDepth() > 0)
            {
                state.skin = GUI.skin;
                state.layoutCache = GUILayoutUtility.current.State;
                state.instanceID = GUIUtility.s_OriginalID;
                if (GUI.scrollViewStates.Count != 0)
                {
                    state.scrollViewStates = GUI.scrollViewStates;
                    GUI.scrollViewStates = new GenericStack();
                }

                Internal_SetupSavedGUIState(out state.guiState, out state.screenManagerSize);
            }
            return state;
        }

        internal void ApplyAndForget()
        {
            if (layoutCache.layoutGroups != null)
            {
                GUILayoutUtility.current.CopyState(layoutCache);
                GUI.skin = skin;
                GUIUtility.s_OriginalID = instanceID;

                if (scrollViewStates != null)
                {
                    GUI.scrollViewStates = scrollViewStates;
                }

                Internal_ApplySavedGUIState(guiState, screenManagerSize);
                GUIClip.Reapply();
            }
        }
    }
}
