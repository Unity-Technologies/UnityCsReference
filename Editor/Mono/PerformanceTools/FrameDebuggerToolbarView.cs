// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Networking.PlayerConnection;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine.Profiling;

namespace UnityEditor
{
    internal class FrameDebuggerToolbarView
    {
        // Non-Serialized
        [NonSerialized] private int m_PrevEventsLimit = 0;
        [NonSerialized] private int m_PrevEventsCount = 0;

        // Returns true if repaint is needed
        public bool DrawToolbar(FrameDebuggerWindow frameDebugger, IConnectionState m_AttachToPlayerState)
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            Profiler.BeginSample("DrawEnableDisableButton");
            DrawEnableDisableButton(frameDebugger, m_AttachToPlayerState, out bool needsRepaint);
            Profiler.EndSample();

            Profiler.BeginSample("DrawConnectionDropdown");
            DrawConnectionDropdown(frameDebugger, m_AttachToPlayerState, out bool isEnabled);
            Profiler.EndSample();

            GUI.enabled = isEnabled;

            Profiler.BeginSample("DrawEventLimitSlider");
            DrawEventLimitSlider(frameDebugger, out int newLimit);
            Profiler.EndSample();

            Profiler.BeginSample("DrawPrevNextButtons");
            DrawPrevNextButtons(frameDebugger, ref newLimit);
            Profiler.EndSample();

            GUILayout.EndHorizontal();

            return needsRepaint;
        }

        private void DrawEnableDisableButton(FrameDebuggerWindow frameDebugger, IConnectionState m_AttachToPlayerState, out bool needsRepaint)
        {
            needsRepaint = false;

            EditorGUI.BeginChangeCheck();

            bool wasEnabled = GUI.enabled;
            GUI.enabled = m_AttachToPlayerState.connectedToTarget != ConnectionTarget.Editor || FrameDebuggerUtility.locallySupported;
            GUIContent button = (FrameDebugger.enabled) ? FrameDebuggerStyles.TopToolbar.recordButtonDisable : FrameDebuggerStyles.TopToolbar.recordButtonEnable;
            GUILayout.Toggle(FrameDebugger.enabled, button, EditorStyles.toolbarButtonLeft, GUILayout.MinWidth(80));
            GUI.enabled = wasEnabled;

            if (EditorGUI.EndChangeCheck())
            {
                frameDebugger.ClickEnableFrameDebugger();
                needsRepaint = true;
            }
        }

        private void DrawConnectionDropdown(FrameDebuggerWindow frameDebugger, IConnectionState m_AttachToPlayerState, out bool isEnabled)
        {
            PlayerConnectionGUILayout.ConnectionTargetSelectionDropdown(m_AttachToPlayerState, EditorStyles.toolbarDropDown);
            isEnabled = FrameDebugger.enabled;
            if (isEnabled && ProfilerDriver.connectedProfiler != FrameDebuggerUtility.GetRemotePlayerGUID())
            {
                // Switch from local to remote debugger or vice versa
                FrameDebuggerUtility.SetEnabled(false, FrameDebuggerUtility.GetRemotePlayerGUID());
                FrameDebuggerUtility.SetEnabled(true, ProfilerDriver.connectedProfiler);
            }
        }

        private void DrawEventLimitSlider(FrameDebuggerWindow frameDebugger, out int newLimit)
        {
            newLimit = 0;

            EditorGUI.BeginChangeCheck();

            bool wasEnabled = GUI.enabled;
            GUI.enabled = FrameDebuggerUtility.count > 1;
            newLimit = EditorGUILayout.IntSlider(FrameDebuggerUtility.limit, 1, FrameDebuggerUtility.count, 1, EditorStyles.toolbarSlider);
            GUI.enabled = wasEnabled;

            if (EditorGUI.EndChangeCheck())
                frameDebugger.ChangeFrameEventLimit(newLimit);
        }

        private void DrawPrevNextButtons(FrameDebuggerWindow frameDebugger, ref int newLimit)
        {
            bool wasEnabled = GUI.enabled;

            GUI.enabled = newLimit > 1;
            if (GUILayout.Button(FrameDebuggerStyles.TopToolbar.prevFrame, EditorStyles.toolbarButton))
                frameDebugger.ChangeFrameEventLimit(newLimit - 1);

            GUI.enabled = newLimit < FrameDebuggerUtility.count;
            if (GUILayout.Button(FrameDebuggerStyles.TopToolbar.nextFrame, EditorStyles.toolbarButtonRight))
                frameDebugger.ChangeFrameEventLimit(newLimit + 1);

            // If we had last event selected, and something changed in the scene so that
            // number of events is different - then try to keep the last event selected.
            if (m_PrevEventsLimit == m_PrevEventsCount)
                if (FrameDebuggerUtility.count != m_PrevEventsCount && FrameDebuggerUtility.limit == m_PrevEventsLimit)
                    frameDebugger.ChangeFrameEventLimit(FrameDebuggerUtility.count);

            m_PrevEventsLimit = FrameDebuggerUtility.limit;
            m_PrevEventsCount = FrameDebuggerUtility.count;

            GUI.enabled = wasEnabled;
        }
    }
}
