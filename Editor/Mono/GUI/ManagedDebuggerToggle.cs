// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
            m_DebuggerAttachedContent = L10n.IconContent("DebuggerAttached", "Debugger Attached", null);
            m_DebuggerDisabledContent = L10n.IconContent("DebuggerDisabled", "Debugger Disabled", null);
            m_DebuggerEnabledContent = L10n.IconContent("DebuggerEnabled", "Debugger Enabled", null);
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
