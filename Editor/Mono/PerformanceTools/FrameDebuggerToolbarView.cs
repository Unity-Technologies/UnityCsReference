// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Networking.PlayerConnection;

using UnityEditor;
using UnityEditorInternal;
using UnityEditorInternal.FrameDebuggerInternal;
using UnityEditor.Networking.PlayerConnection;


namespace UnityEditorInternal.FrameDebuggerInternal
{
    internal class FrameDebuggerToolbarView
    {
        // Non-Serialized
        [NonSerialized] private int m_PrevEventsLimit = 0;
        [NonSerialized] private int m_PrevEventsCount = 0;

        // Returns true if repaint is needed
        public bool DrawToolbar(FrameDebuggerWindow frameDebugger, IConnectionState m_AttachToPlayerState)
        {
            Profiler.BeginSample("DrawToolbar");
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            DrawEnableDisableButton(frameDebugger, m_AttachToPlayerState, out bool needsRepaint);
            DrawConnectionDropdown(frameDebugger, m_AttachToPlayerState, out bool isEnabled);

            GUI.enabled = isEnabled;

            DrawEventLimitSlider(frameDebugger, out int newLimit);
            DrawPrevNextButtons(frameDebugger, ref newLimit);

            GUILayout.EndHorizontal();
            Profiler.EndSample();

            return needsRepaint;
        }

        private void DrawEnableDisableButton(FrameDebuggerWindow frameDebuggerWindow, IConnectionState m_AttachToPlayerState, out bool needsRepaint)
        {
            needsRepaint = false;

            EditorGUI.BeginChangeCheck();

            bool wasEnabled = GUI.enabled;
            GUI.enabled = m_AttachToPlayerState.connectedToTarget != ConnectionTarget.Editor || FrameDebuggerUtility.locallySupported;
            GUIContent button = (FrameDebugger.enabled) ? FrameDebuggerStyles.TopToolbar.s_RecordButtonDisable : FrameDebuggerStyles.TopToolbar.s_RecordButtonEnable;
            GUILayout.Toggle(FrameDebugger.enabled, button, EditorStyles.toolbarButtonLeft, GUILayout.MinWidth(80));
            GUI.enabled = wasEnabled;

            if (EditorGUI.EndChangeCheck())
            {
                frameDebuggerWindow.ToggleFrameDebuggerEnabled();
                needsRepaint = true;
            }
        }

        private void DrawConnectionDropdown(FrameDebuggerWindow frameDebuggerWindow, IConnectionState m_AttachToPlayerState, out bool isEnabled)
        {
            PlayerConnectionGUILayout.ConnectionTargetSelectionDropdown(m_AttachToPlayerState, EditorStyles.toolbarDropDown);
            isEnabled = FrameDebugger.enabled;
            if (isEnabled && ProfilerDriver.connectedProfiler != FrameDebuggerUtility.GetRemotePlayerGUID())
            {
                // Switch from local to remote debugger or vice versa
                frameDebuggerWindow.OnConnectedProfilerChange();
            }
        }

        private void DrawEventLimitSlider(FrameDebuggerWindow frameDebuggerWindow, out int newLimit)
        {
            newLimit = 0;

            EditorGUI.BeginChangeCheck();

            bool wasEnabled = GUI.enabled;
            GUI.enabled = FrameDebuggerUtility.count > 1;

            // We need to use Slider instead of IntSlider due to a bug where the invisible label makes
            // the mouse cursor different when hovering over the leftmost 10-20% of the slider (UUM-17184)
            // We add 0.5 to make it switch between frames like when we use a IntSlider.
            newLimit = (int) (0.5f + EditorGUILayout.Slider(FrameDebuggerUtility.limit, 1, FrameDebuggerUtility.count));

            GUI.enabled = wasEnabled;

            if (EditorGUI.EndChangeCheck())
                frameDebuggerWindow.ChangeFrameEventLimit(newLimit);
        }

        private void DrawPrevNextButtons(FrameDebuggerWindow frameDebuggerWindow, ref int newLimit)
        {
            bool wasEnabled = GUI.enabled;

            GUI.enabled = newLimit > 1;
            if (GUILayout.Button(FrameDebuggerStyles.TopToolbar.s_PrevFrame, EditorStyles.toolbarButton))
                frameDebuggerWindow.ChangeFrameEventLimit(newLimit - 1);

            GUI.enabled = newLimit < FrameDebuggerUtility.count;
            if (GUILayout.Button(FrameDebuggerStyles.TopToolbar.s_NextFrame, EditorStyles.toolbarButtonRight))
                frameDebuggerWindow.ChangeFrameEventLimit(newLimit + 1);

            // If we had last event selected, and something changed in the scene so that
            // number of events is different - then try to keep the last event selected.
            if (m_PrevEventsLimit == m_PrevEventsCount)
                if (FrameDebuggerUtility.count != m_PrevEventsCount && FrameDebuggerUtility.limit == m_PrevEventsLimit)
                    frameDebuggerWindow.ChangeFrameEventLimit(FrameDebuggerUtility.count);

            // The number of events has changed...
            if (FrameDebuggerUtility.count != m_PrevEventsCount)
            {
                frameDebuggerWindow.ReselectItemOnCountChange();
            }

            m_PrevEventsLimit = FrameDebuggerUtility.limit;
            m_PrevEventsCount = FrameDebuggerUtility.count;

            GUI.enabled = wasEnabled;
        }
    }
}
