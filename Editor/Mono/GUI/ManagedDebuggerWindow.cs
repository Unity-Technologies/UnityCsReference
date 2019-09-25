// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Compilation;
using UnityEditor.Scripting;

namespace UnityEditor
{
    [InitializeOnLoad]
    internal class ManagedDebuggerWindow : PopupWindowContent
    {
        private readonly GUIContent m_CodeOptimizationTitleContent;
        private readonly GUIContent m_CodeOptimizationTextContent;
        private readonly GUIContent m_CodeOptimizationButtonContent;

        private readonly GUIStyle m_WindowStyle;

        private readonly CodeOptimization m_CodeOptimization;

        private float m_TextRectHeight;

        private const int k_FieldCount = 2;
        private const int k_FrameWidth = 11;
        private const int k_WindowWidth = 290;
        private const int k_WindowHeight = (int)EditorGUI.kSingleLineHeight * k_FieldCount + k_FrameWidth * 2;

        static ManagedDebuggerWindow()
        {
            SubscribeToDebuggerAttached();
        }

        public ManagedDebuggerWindow(CodeOptimization codeOptimization)
        {
            m_CodeOptimization = codeOptimization;

            if (CodeOptimization.Debug == m_CodeOptimization)
            {
                m_CodeOptimizationTitleContent = EditorGUIUtility.TrTextContent("Mode: Debug");
                m_CodeOptimizationTextContent = EditorGUIUtility.TrTextContentWithIcon("Release mode disables C# debugging but improves C# performance.\nSwitching to release mode will recompile and reload all scripts.", EditorGUIUtility.GetHelpIcon(MessageType.Info));
                m_CodeOptimizationButtonContent = EditorGUIUtility.TrTextContent("Switch to release mode");
            }
            else
            {
                m_CodeOptimizationTitleContent = EditorGUIUtility.TrTextContent("Mode: Release");
                m_CodeOptimizationTextContent = EditorGUIUtility.TrTextContentWithIcon("Debug mode enables C# debugging but reduces C# performance.\nSwitching to debug mode will recompile and reload all scripts.", EditorGUIUtility.GetHelpIcon(MessageType.Info));
                m_CodeOptimizationButtonContent = EditorGUIUtility.TrTextContent("Switch to debug mode");
            }

            m_TextRectHeight = EditorStyles.helpBox.CalcHeight(m_CodeOptimizationTextContent, k_WindowWidth);
            m_WindowStyle = new GUIStyle { padding = new RectOffset(10, 10, 10, 10) };
        }

        public override void OnGUI(Rect rect)
        {
            var exit = false;

            GUILayout.BeginArea(rect, m_WindowStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label(m_CodeOptimizationTitleContent, EditorStyles.boldLabel);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(GUIContent.none, m_CodeOptimizationTextContent, EditorStyles.helpBox);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(m_CodeOptimizationButtonContent))
            {
                ToggleDebugState(m_CodeOptimization);
                exit = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();

            exit |= Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;

            if (exit)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(k_WindowWidth, k_WindowHeight + m_TextRectHeight);
        }

        private static void OnDebuggerAttached(bool debuggerAttached)
        {
            if (debuggerAttached && (CodeOptimization.Release == CompilationPipeline.codeOptimization))
            {
                if (EditorUtility.DisplayDialog(
                    "C# Debugger Attached",
                    "Do you want to switch to debug mode to enable C# debugging?\nDebug mode reduces C# performance.\nSwitching to debug mode will recompile and reload all scripts.",
                    "Switch to debug mode",
                    "Cancel"))
                {
                    ToggleDebugState(CompilationPipeline.codeOptimization);
                }
                else
                {
                    ManagedDebugger.Disconnect();
                }
            }

            AppStatusBar.StatusChanged();
        }

        private static void SubscribeToDebuggerAttached()
        {
            ManagedDebugger.debuggerAttached += OnDebuggerAttached;
        }

        private static void ToggleDebugState(CodeOptimization codeOptimization)
        {
            if (CodeOptimization.Debug == codeOptimization)
            {
                CompilationPipeline.codeOptimization = CodeOptimization.Release;
            }
            else
            {
                CompilationPipeline.codeOptimization = CodeOptimization.Debug;
            }
        }
    }
}
