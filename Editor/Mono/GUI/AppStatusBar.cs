// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEngine.Scripting;

namespace UnityEditor
{
    internal class AppStatusBar : GUIView
    {
        static AppStatusBar s_AppStatusBar;
        static GUIContent[] s_StatusWheel;
        static GUIContent s_AssemblyLock;

        string m_MiniMemoryOverview = "";
        string m_BakeModeString = "";

        ManagedDebuggerToggle m_ManagedDebuggerToggle = null;

        private bool showBakeMode
        {
            get { return Lightmapping.bakedGI || Lightmapping.realtimeGI; }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            s_AppStatusBar = this;
            s_StatusWheel = new GUIContent[12];
            for (int i = 0; i < 12; i++)
                s_StatusWheel[i] = EditorGUIUtility.IconContent("WaitSpin" + i.ToString("00"));
            s_AssemblyLock = EditorGUIUtility.IconContent("AssemblyLock", "|Assemblies are currently locked. Compilation will resume once they are unlocked");
            m_ManagedDebuggerToggle = new ManagedDebuggerToggle();
        }

        [RequiredByNativeCode]
        public static void StatusChanged()
        {
            if (s_AppStatusBar)
            {
                s_AppStatusBar.Repaint();
            }
        }

        [RequiredByNativeCode]
        internal static void StatusChangedImmediate()
        {
            if (s_AppStatusBar)
            {
                s_AppStatusBar.RepaintImmediately();
            }
        }

        void OnInspectorUpdate()
        {
            string miniOverview = UnityEditorInternal.ProfilerDriver.miniMemoryOverview;
            string bakeModeString = GetBakeModeString();

            if ((Unsupported.IsDeveloperMode() && m_MiniMemoryOverview != miniOverview) || (m_BakeModeString != bakeModeString))
            {
                m_MiniMemoryOverview = miniOverview;
                m_BakeModeString = bakeModeString;

                Repaint();
            }
        }

        static GUIStyle background;

        protected override void OldOnGUI()
        {
            const int spacing = 4;
            const int statusWheelWidth = 24;
            const int progressBarWidth = 185;
            float lightingBakeModeBarWidth = EditorStyles.progressBarText.CalcSize(EditorGUIUtility.TempContent(m_BakeModeString)).x;
            const int barHeight = 19;

            ConsoleWindow.LoadIcons();

            if (background == null)
                background = "AppToolbar";

            GUI.color = EditorApplication.isPlayingOrWillChangePlaymode ? HostView.kPlayModeDarken : Color.white;

            if (Event.current.type == EventType.Repaint)
                background.Draw(new Rect(0, 0, position.width, position.height), false, false, false, false);

            bool compiling = EditorApplication.isCompiling;
            bool assembliesLocked = !EditorApplication.CanReloadAssemblies();

            GUILayout.BeginHorizontal();
            GUILayout.Space(2);

            float statusBarItemsWidth = statusWheelWidth + (AsyncProgressBar.isShowing ? (progressBarWidth + spacing) : 0) + (showBakeMode ? (lightingBakeModeBarWidth + spacing) : 0);

            if (Event.current.type == EventType.MouseDown)
            {
                Rect rect = new Rect(position.width - statusBarItemsWidth - m_ManagedDebuggerToggle.GetWidth(), 0, lightingBakeModeBarWidth, barHeight);

                if (rect.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    LightingWindow.CreateLightingWindow();
                }
            }

            string statusText = LogEntries.GetStatusText();
            if (statusText != null)
            {
                // Render
                int mask = LogEntries.GetStatusMask();
                GUIStyle errorStyle = ConsoleWindow.GetStatusStyleForErrorMode(mask);

                Texture2D icon = ConsoleWindow.GetIconForErrorMode(mask, false);
                GUILayout.Label(icon, errorStyle);

                GUILayout.Space(2);
                GUILayout.BeginVertical();
                GUILayout.Space(1);

                float statusTextWidth = GUIView.current.position.width - statusBarItemsWidth - m_ManagedDebuggerToggle.GetWidth() - (icon != null ? icon.width : 0);
                GUILayout.Label(statusText, errorStyle, GUILayout.MaxWidth(statusTextWidth));

                GUILayout.FlexibleSpace();

                GUILayout.EndVertical();

                // Handle status bar click
                if (Event.current.type == EventType.MouseDown)
                {
                    Rect rect = new Rect(0, 0, statusTextWidth, barHeight);
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        Event.current.Use();
                        LogEntries.ClickStatusBar(Event.current.clickCount);
                        GUIUtility.ExitGUI();
                    }
                }
            }

            float managedDebuggerTogglePosX = position.width - statusWheelWidth - m_ManagedDebuggerToggle.GetWidth();
            m_ManagedDebuggerToggle.OnGUI(managedDebuggerTogglePosX, 0);

            GUILayout.EndHorizontal();

            if (Event.current.type == EventType.Repaint)
            {
                float progressBarHorizontalPosition = position.width - statusWheelWidth - m_ManagedDebuggerToggle.GetWidth();

                if (AsyncProgressBar.isShowing)
                {
                    progressBarHorizontalPosition -= progressBarWidth + spacing;
                    EditorGUI.ProgressBar(new Rect(progressBarHorizontalPosition, 0, progressBarWidth, barHeight), AsyncProgressBar.progress, AsyncProgressBar.progressInfo);
                }

                if (showBakeMode)
                {
                    GUI.Label(new Rect(progressBarHorizontalPosition - lightingBakeModeBarWidth - spacing, 0, lightingBakeModeBarWidth, barHeight), m_BakeModeString, EditorStyles.progressBarText);
                    progressBarHorizontalPosition -= lightingBakeModeBarWidth + spacing;
                }

                if (compiling)
                {
                    if (assembliesLocked)
                    {
                        GUI.Label(new Rect(position.width - statusWheelWidth, 0, s_AssemblyLock.image.width, s_AssemblyLock.image.height), s_AssemblyLock, GUIStyle.none);
                    }
                    else
                    {
                        int frame = (int)Mathf.Repeat(Time.realtimeSinceStartup * 10, 11.99f);
                        GUI.Label(new Rect(position.width - statusWheelWidth, 0, s_StatusWheel[frame].image.width, s_StatusWheel[frame].image.height), s_StatusWheel[frame], GUIStyle.none);
                    }
                }

                if (Unsupported.IsBleedingEdgeBuild())
                {
                    var backup = GUI.color;
                    GUI.color = Color.yellow;
                    GUI.Label(new Rect(progressBarHorizontalPosition - 310 - spacing, 0, 310, barHeight), "THIS IS AN UNTESTED BLEEDINGEDGE UNITY BUILD");
                    GUI.color = backup;
                }
                else if (Unsupported.IsDeveloperMode())
                {
                    GUI.Label(new Rect(progressBarHorizontalPosition - 300 - spacing, 0, 300, barHeight), m_MiniMemoryOverview, EditorStyles.progressBarText);
                    EditorGUIUtility.CleanCache(m_MiniMemoryOverview);
                }
            }

            DoWindowDecorationEnd();

            EditorGUI.ShowRepaints();
        }

        private string GetBakeModeString()
        {
            if (!showBakeMode)
                return "";

            return "Auto Generate Lighting " + (Lightmapping.giWorkflowMode == Lightmapping.GIWorkflowMode.Iterative ? "On" : "Off");
        }
    }
} //namespace
