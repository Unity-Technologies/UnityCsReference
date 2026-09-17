// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: IMGUIFramework not yet converted
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngineInternal;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/SavedGUIState.bindings.h")]
    internal partial struct SavedGUIState
    {
        private GUILayoutUtility.LayoutCacheState layoutCache;
        private IntPtr guiState;
        private Vector2 screenManagerSize;
        private GUISkin skin;
        private EntityId entityId;
        private GenericStack scrollViewStates;
        private int unbalancedGroupsCount;

        private static extern void Internal_SetupSavedGUIState(out IntPtr state, out Vector2 screenManagerSize);

        private static extern void Internal_ApplySavedGUIState(IntPtr state, Vector2 screenManagerSize);

        internal static extern int Internal_GetGUIDepth();

        // Save/restore only the managed IMGUI layout state (the native GUIState is handled separately).
        private void CaptureManaged()
        {
            skin = GUI.skin;
            // The state's group stack aliases the live cache, which a re-entrant Layout pass clears in place; the backup must own a copy (UUM-148153).
            var live = GUILayoutUtility.current.State;
            var groups = live.layoutGroups.ToArray();
            var copiedGroups = new GenericStack();
            for (int i = groups.Length - 1; i >= 0; i--)
                copiedGroups.Push(groups[i]);
            layoutCache = new GUILayoutUtility.LayoutCacheState(live.id, live.topLevel, copiedGroups, live.windows);
            unbalancedGroupsCount = GUILayoutUtility.unbalancedgroupscount;
            entityId = GUIUtility.s_OriginalID;
            if (GUI.scrollViewStates.Count != 0)
            {
                scrollViewStates = GUI.scrollViewStates;
                GUI.scrollViewStates = new GenericStack();
            }
        }

        private void ApplyManaged()
        {
            GUILayoutUtility.current.CopyState(layoutCache);
            GUILayoutUtility.unbalancedgroupscount = unbalancedGroupsCount;
            if (skin != null) 
                GUI.DoSetSkin(skin); //setting GUI.skin directly doesn't work at a depth of 0
            GUIUtility.s_OriginalID = entityId;
            if (scrollViewStates != null)
                GUI.scrollViewStates = scrollViewStates;
        }

        internal static SavedGUIState Create()
        {
            SavedGUIState state = new SavedGUIState();
            if (Internal_GetGUIDepth() > 0)
            {
                state.CaptureManaged();
                Internal_SetupSavedGUIState(out state.guiState, out state.screenManagerSize);
            }
            return state;
        }

        internal void ApplyAndForget()
        {
            if (layoutCache.layoutGroups != null)
            {
                ApplyManaged();
                Internal_ApplySavedGUIState(guiState, screenManagerSize);
                GUIClip.Reapply();
            }
        }

        // UUM-145914: managed-only backup used by the native re-entrancy path (GUIView::OnInputEvent).
        [AutoStaticsCleanupOnCodeReload] // cleared on reload so no captured layout state survives a domain reload
        static readonly Stack<(SavedGUIState guiState, SavedEditorGUIState editorState)> s_ReentrantLayoutStates = new();

        internal static void PushReentrantLayoutState()
        {
            SavedGUIState state = new SavedGUIState();
            state.CaptureManaged();
            s_ReentrantLayoutStates.Push((state, SavedEditorGUIState.Capture()));
        }

        internal static void PopReentrantLayoutState()
        {
            if (s_ReentrantLayoutStates.Count > 0)
            {
                var (guiState, editorState) = s_ReentrantLayoutStates.Pop();
                editorState.Apply();
                guiState.ApplyManaged();
            }
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
