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

        private const int k_Width = 36;
        private const int k_Height = 19;
        private const int k_MarginX = 4;
        private const int k_MarginY = 0;

        public ManagedDebuggerToggle()
        {
            m_DebuggerAttachedContent = EditorGUIUtility.TrIconContent("DebuggerAttached");
            m_DebuggerDisabledContent = EditorGUIUtility.TrIconContent("DebuggerDisabled");
            m_DebuggerEnabledContent = EditorGUIUtility.TrIconContent("DebuggerEnabled");
            m_PopupLocation = new[] { PopupLocation.AboveAlignRight };
        }

        public void OnGUI(float x, float y)
        {
            using (new EditorGUI.DisabledScope(!ManagedDebugger.isEnabled))
            {
                GUILayout.BeginVertical();
                EditorGUILayout.Space();

                var codeOptimization = CompilationPipeline.codeOptimization;
                var debuggerAttached = ManagedDebugger.isAttached;
                var debuggerContent = GetDebuggerContent(debuggerAttached, codeOptimization);
                var buttonArea = new Rect(x + k_MarginX, y + k_MarginY, k_Width, k_Height);

                if (EditorGUI.DropdownButton(buttonArea, debuggerContent, FocusType.Passive, EditorStyles.toolbarDropDown))
                {
                    PopupWindow.Show(buttonArea, new ManagedDebuggerWindow(codeOptimization), m_PopupLocation);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.Space();
                GUILayout.EndVertical();
            }
        }

        public float GetWidth()
        {
            return k_Width + (k_MarginX << 1);
        }

        public float GetHeight()
        {
            return k_Height + (k_MarginY << 1);
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
