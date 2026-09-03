// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: IMGUIControls not yet converted
using UnityEngine;
using UnityEditor.Compilation;
using UnityEditor.Scripting;

namespace UnityEditor
{
    internal class ManagedDebuggerToggle
    {
        private readonly GUIContent m_DebuggerAttachedContent;
        private readonly GUIContent m_DebuggerDisabledContent;
        private readonly GUIContent m_DebuggerEnabledContent;
        private readonly PopupLocation[] m_PopupLocation;

        public ManagedDebuggerToggle()
        {
            m_DebuggerAttachedContent = EditorGUIUtility.TrIconContent("DebuggerAttached", "Debugger Attached");
            m_DebuggerDisabledContent = EditorGUIUtility.TrIconContent("DebuggerDisabled", "Debugger Disabled");
            m_DebuggerEnabledContent = EditorGUIUtility.TrIconContent("DebuggerEnabled", "Debugger Enabled");
            m_PopupLocation = new[] { PopupLocation.AboveAlignRight };
        }

        public void OnGUI()
        {
            bool debuggerEnabled = ManagedDebugger.isEnabled;
            bool debuggerAttached = ManagedDebugger.isAttached;

            using (new EditorGUI.DisabledScope(!debuggerEnabled))
            {
                var codeOptimization = CompilationPipeline.codeOptimization;
                var content = GetDebuggerContent(debuggerAttached, codeOptimization);

                var style = AppStatusBar.Styles.statusIcon;
                var rect = GUILayoutUtility.GetRect(content, style);
                if (GUI.Button(rect, content, style))
                {
                    PopupWindow.Show(rect, new ManagedDebuggerWindow(codeOptimization), m_PopupLocation);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private GUIContent GetDebuggerContent(bool debuggerAttached, CodeOptimization optimization)
        {
            if (CodeOptimization.Debug == optimization)
            {
                return debuggerAttached ? m_DebuggerAttachedContent : m_DebuggerEnabledContent;
            }

            return m_DebuggerDisabledContent;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
