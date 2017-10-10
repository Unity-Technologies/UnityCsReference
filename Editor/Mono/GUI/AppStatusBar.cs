// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Scripting;

namespace UnityEditor
{
    internal class AppStatusBar : GUIView
    {
        static AppStatusBar s_AppStatusBar;
        static GUIContent[] s_StatusWheel;
        string m_LastMiniMemoryOverview = "";

        protected override void OnEnable()
        {
            base.OnEnable();
            // Disable clipping on the root element to fix case 931831
            visualTree.clippingOptions = VisualElement.ClippingOptions.NoClipping;
            s_AppStatusBar = this;
            s_StatusWheel = new GUIContent[12];
            for (int i = 0; i < 12; i++)
                s_StatusWheel[i] = EditorGUIUtility.IconContent("WaitSpin" + i.ToString("00"));
        }

        [RequiredByNativeCode]
        public static void StatusChanged()
        {
            if (s_AppStatusBar)
                s_AppStatusBar.Repaint();
        }

        void OnInspectorUpdate()
        {
            string miniOverview = UnityEditorInternal.ProfilerDriver.miniMemoryOverview;
            if (Unsupported.IsDeveloperBuild() && m_LastMiniMemoryOverview != miniOverview)
            {
                m_LastMiniMemoryOverview = miniOverview;
                Repaint();
            }
        }

        static GUIStyle background;

        protected override void OldOnGUI()
        {
            ConsoleWindow.LoadIcons();
            if (background == null)
                background = "AppToolbar";

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                GUI.color = HostView.kPlayModeDarken;

            if (Event.current.type == EventType.Repaint)
                background.Draw(new Rect(0, 0, position.width, position.height), false, false, false, false);

            bool compiling = EditorApplication.isCompiling;

            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.Space(2);

            string statusText = LogEntries.GetStatusText();
            if (statusText != null)
            {
                // Render
                int mask = LogEntries.GetStatusMask();
                GUIStyle errorStyle = ConsoleWindow.GetStatusStyleForErrorMode(mask);

                GUILayout.Label(ConsoleWindow.GetIconForErrorMode(mask, false), errorStyle);

                GUILayout.Space(2);
                GUILayout.BeginVertical();
                GUILayout.Space(2);

                if (compiling) //leave space for indicator
                    GUILayout.Label(statusText, errorStyle, GUILayout.MaxWidth(GUIView.current.position.width - 52));
                else
                    GUILayout.Label(statusText, errorStyle);
                GUILayout.FlexibleSpace();

                GUILayout.EndVertical();

                // Handle status bar click
                if (Event.current.type == EventType.MouseDown)
                {
                    Event.current.Use();
                    LogEntries.ClickStatusBar(Event.current.clickCount);
                    GUIUtility.ExitGUI();
                }
            }

            GUILayout.EndHorizontal();

            if (Event.current.type == EventType.Repaint)
            {
                const float statusWheelWidth = 24;
                const float progressBarStatusWheelSpacing = 3;

                float progressBarHorizontalPosition = position.width - statusWheelWidth;
                if (AsyncProgressBar.isShowing)
                {
                    const int progressBarWidth = 185;
                    progressBarHorizontalPosition -= progressBarWidth + progressBarStatusWheelSpacing;
                    EditorGUI.ProgressBar(new Rect(progressBarHorizontalPosition, 0, progressBarWidth, 19), AsyncProgressBar.progress, AsyncProgressBar.progressInfo);
                }

                if (compiling)
                {
                    int frame = (int)Mathf.Repeat(Time.realtimeSinceStartup * 10, 11.99f);
                    GUI.Label(new Rect(position.width - statusWheelWidth, 0, s_StatusWheel[frame].image.width, s_StatusWheel[frame].image.height), s_StatusWheel[frame], GUIStyle.none);
                }

                if (Unsupported.IsBleedingEdgeBuild())
                {
                    var backup = GUI.color;
                    GUI.color = Color.yellow;
                    GUI.Label(new Rect(progressBarHorizontalPosition - 310, 0, 310, 19), "THIS IS AN UNTESTED BLEEDINGEDGE UNITY BUILD");
                    GUI.color = backup;
                }
                else if (Unsupported.IsDeveloperBuild())
                {
                    GUI.Label(new Rect(progressBarHorizontalPosition - 200, 0, 200, 19), m_LastMiniMemoryOverview, EditorStyles.progressBarText);
                    EditorGUIUtility.CleanCache(m_LastMiniMemoryOverview);
                }
            }

            DoWindowDecorationEnd();

            EditorGUI.ShowRepaints();
        }
    }
} //namespace
