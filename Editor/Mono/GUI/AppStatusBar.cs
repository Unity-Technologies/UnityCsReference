// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor
{
    internal class AppStatusBar : GUIView
    {
        static readonly NumberFormatInfo percentageFormat = new CultureInfo("en-US", false).NumberFormat;

        internal static class Styles
        {
            public const int spacing = 4;

            public static readonly GUILayoutOption progressBarWidth = GUILayout.Width(180);
            public static readonly GUILayoutOption progressLabelMaxWidth = GUILayout.MaxWidth(230);
            public static readonly GUIStyle background = "AppToolbar";
            public static readonly GUIStyle statusLabel = EditorStyles.FromUSS(EditorStyles.label, "status-bar-label");
            public static readonly GUIStyle statusIcon = "StatusBarIcon";

            public static readonly GUIStyle progressBar = EditorStyles.FromUSS(EditorStyles.progressBarBar, "status-bar-progress");
            public static readonly GUIStyle progressBarAlmostDone = EditorStyles.FromUSS(progressBar, "status-bar-progress-almost-done");
            public static readonly GUIStyle progressBarUnresponsive = EditorStyles.FromUSS(progressBar, "status-bar-progress-unresponsive");
            public static readonly GUIStyle progressBarIndefinite = EditorStyles.FromUSS(progressBar, "status-bar-progress-indefinite");
            public static readonly GUIStyle progressBarBack = EditorStyles.FromUSS(EditorStyles.progressBarBack, "status-bar-progress-background");
            public static readonly GUIStyle progressBarText = EditorStyles.FromUSS(statusLabel, "status-bar-progress-text");

            public static readonly GUIContent[] statusWheel;
            public static readonly GUIContent assemblyLock = EditorGUIUtility.IconContent("AssemblyLock", "|Assemblies are currently locked. Compilation will resume once they are unlocked");
            public static readonly GUIContent progressIcon = EditorGUIUtility.TrIconContent("Progress", "Show progress details");
            public static readonly GUIContent progressHideIcon = EditorGUIUtility.TrIconContent("Progress", "Hide progress details");

            static Styles()
            {
                percentageFormat.PercentDecimalDigits = 0;
                statusWheel = new GUIContent[12];
                for (int i = 0; i < 12; i++)
                    statusWheel[i] = EditorGUIUtility.IconContent("WaitSpin" + i.ToString("00"));
            }
        }

        private static AppStatusBar s_AppStatusBar;

        private string m_MiniMemoryOverview = "";
        private bool m_DrawExtraFeatures;
        private GUIContent m_ProgressStatus = new GUIContent();
        private GUIContent m_ProgressPercentageStatus = new GUIContent();
        private bool m_CurrentProgressNotResponding = false;

        private ManagedDebuggerToggle m_ManagedDebuggerToggle = null;
        private CacheServerToggle m_CacheServerToggle = null;
        const double k_CheckUnresponsiveFrequencyInSecond = 0.5;
        const float k_ShowProgressThreshold = 0.5f;
        private double m_LastUpdate;

        private bool m_ShowProgress = false;
        private bool showProgress
        {
            get
            {
                return m_ShowProgress;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            s_AppStatusBar = this;
            m_ManagedDebuggerToggle = new ManagedDebuggerToggle();
            m_CacheServerToggle = new CacheServerToggle();
            m_EventInterests.wantsLessLayoutEvents = true;
            m_DrawExtraFeatures = ModeService.HasCapability(ModeCapability.StatusBarExtraFeatures, true);

            Progress.added += RefreshProgressBar;
            Progress.removed += RefreshProgressBar;
            Progress.updated += RefreshProgressBar;
        }

        protected override void OnDisable()
        {
            Progress.added -= RefreshProgressBar;
            Progress.removed -= RefreshProgressBar;
            Progress.updated -= RefreshProgressBar;
            EditorApplication.delayCall -= DelayRepaint;
            base.OnDisable();
        }

        protected void OnInspectorUpdate()
        {
            string miniOverview = UnityEditorInternal.ProfilerDriver.miniMemoryOverview;

            if ((Unsupported.IsDeveloperMode() && m_MiniMemoryOverview != miniOverview))
            {
                m_MiniMemoryOverview = miniOverview;

                Repaint();
            }
        }

        private void ScheduleCheckProgressUnresponsive()
        {
            EditorApplication.tick -= CheckProgressUnresponsive;
            EditorApplication.tick += CheckProgressUnresponsive;
        }

        private void CheckProgressUnresponsive()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - m_LastUpdate < k_CheckUnresponsiveFrequencyInSecond)
                return;

            EditorApplication.tick -= CheckProgressUnresponsive;

            m_LastUpdate = now;
            if (Progress.running)
            {
                var unresponsiveItem = Progress.EnumerateItems().FirstOrDefault(item => !item.responding);
                if (unresponsiveItem != null)
                {
                    m_CurrentProgressNotResponding = true;
                    RefreshProgressBar(new[] { unresponsiveItem });
                }
                else
                {
                    m_CurrentProgressNotResponding = false;
                    ScheduleCheckProgressUnresponsive();
                }
            }
            else
            {
                m_CurrentProgressNotResponding = false;
            }
        }

        protected override void OldOnGUI()
        {
            ConsoleWindow.LoadIcons();

            GUI.color = EditorApplication.isPlayingOrWillChangePlaymode ? HostView.kPlayModeDarken : Color.white;

            if (Event.current.type == EventType.Layout)
                m_ShowProgress = Progress.running && Progress.GetMaxElapsedTime() > k_ShowProgressThreshold;
            else if (Event.current.type == EventType.Repaint)
                Styles.background.Draw(new Rect(0, 0, position.width, position.height), false, false, false, false);

            GUILayout.BeginHorizontal(GUILayout.MaxWidth(position.width));
            {
                GUILayout.Space(2);
                DrawStatusText();

                if(EditorPrefs.GetBool("EnableHelperBar", false)) ShortcutManagement.HelperWindow.StatusBarShortcuts();
                else GUILayout.FlexibleSpace();

                if (m_DrawExtraFeatures)
                    DrawSpecialModeLabel();
                DrawProgressBar();
                DrawDebuggerToggle();
                if (m_DrawExtraFeatures)
                {
                    DrawCacheServerToggle();
                }
                DrawRefreshStatus();
            }
            GUILayout.EndHorizontal();

            EditorGUI.ShowRepaints();
        }

        private void DrawRefreshStatus()
        {
            bool compiling = EditorApplication.isCompiling;
            bool assembliesLocked = !EditorApplication.CanReloadAssemblies();

            if (compiling)
            {
                if (assembliesLocked)
                {
                    GUILayout.Button(Styles.assemblyLock, Styles.statusIcon);
                }
                else
                {
                    int frame = (int)Mathf.Repeat(Time.realtimeSinceStartup * 10, 11.99f);
                    GUILayout.Button(Styles.statusWheel[frame], Styles.statusIcon);
                }
            }
            else
            {
                var canHide = ProgressWindow.canHideDetails;
                if (GUILayout.Button(canHide ? Styles.progressHideIcon : Styles.progressIcon, Styles.statusIcon))
                {
                    if (canHide)
                        ProgressWindow.HideDetails();
                    else
                        Progress.ShowDetails(false);
                }

                var buttonRect = GUILayoutUtility.GetLastRect();
                EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
            }
        }

        private string m_SpecialModeLabel = "";
        private void DrawSpecialModeLabel()
        {
            if (Unsupported.IsBleedingEdgeBuild())
            {
                GUILayout.Space(k_SpaceBeforeProgress);
                m_SpecialModeLabel = "THIS IS AN UNTESTED BLEEDINGEDGE UNITY BUILD";
                var backup = GUI.color;
                GUI.color = Color.yellow;
                GUILayout.Label(m_SpecialModeLabel);
                GUI.color = backup;
            }
            else if (Unsupported.IsDeveloperMode())
            {
                GUILayout.Space(k_SpaceBeforeProgress);
                m_SpecialModeLabel = m_MiniMemoryOverview;
                GUILayout.Label(m_SpecialModeLabel, Styles.statusLabel);
                EditorGUIUtility.CleanCache(m_MiniMemoryOverview);
            }
            else
                m_SpecialModeLabel = "";
        }

        float k_SpaceBeforeProgress = 15;
        float k_SpaceAfterProgress = 4;

        private void DelayRepaint()
        {
            if (!this) // The window could have been destroyed during a reset layout.
                return;
            Repaint();
        }

        private void DrawProgressBar()
        {
            if (!showProgress)
                return;

            GUILayout.Space(k_SpaceBeforeProgress);

            var buttonRect = GUILayoutUtility.GetLastRect();
            EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);

            // Define the progress styles to be used based on the current progress state.
            var globalProgress = Progress.globalProgress;
            var progressBarStyle = GetProgressBarStyle(globalProgress);
            var progressBarContent = GetProgressBarContent(m_ProgressStatus);
            var progressRect = EditorGUILayout.GetControlRect(false, position.height, Styles.progressBarBack, Styles.progressBarWidth);
            if (EditorGUI.ProgressBar(progressRect, globalProgress, progressBarContent, true,
                Styles.progressBarBack, progressBarStyle, Styles.progressBarText))
                Progress.ShowDetails(false);

            if (globalProgress == -1.0f)
            {
                EditorApplication.delayCall -= DelayRepaint;
                EditorApplication.delayCall += DelayRepaint;
            }

            EditorGUIUtility.AddCursorRect(progressRect, MouseCursor.Link);
            GUILayout.Space(k_SpaceAfterProgress);
        }

        private GUIContent GetProgressBarContent(GUIContent defaultContent)
        {
            GUIContent progressBarContent = defaultContent;
            if (m_CurrentProgressNotResponding)
                progressBarContent = GUIContent.none;

            return progressBarContent;
        }

        private GUIStyle GetProgressBarStyle(float globalProgress)
        {
            var progressBarStyle = Styles.progressBar;

            if (globalProgress == -1.0f)
                progressBarStyle = Styles.progressBarIndefinite;
            else if (m_CurrentProgressNotResponding)
                progressBarStyle = Styles.progressBarUnresponsive;
            else if (globalProgress >= 0.99f)
                progressBarStyle = Styles.progressBarAlmostDone;

            return progressBarStyle;
        }

        void DrawDebuggerToggle()
        {
            m_ManagedDebuggerToggle.OnGUI();
        }

        void DrawCacheServerToggle()
        {
            m_CacheServerToggle.OnGUI();
        }

        private void DrawStatusText()
        {
            string statusText = LogEntries.GetStatusText();
            if (String.IsNullOrEmpty(statusText))
                return;

            int mask = LogEntries.GetStatusMask();
            GUIStyle errorStyle = ConsoleWindow.GetStatusStyleForErrorMode(mask);

            Texture2D icon = ConsoleWindow.GetIconForErrorMode(mask, false);
            GUILayout.Label(icon, errorStyle);

            var iconRect = GUILayoutUtility.GetLastRect();

            GUILayout.Space(2);
            GUILayout.BeginVertical();
            GUILayout.Space(1);

            GUILayout.Label(statusText, errorStyle, GetStatusTextLayoutOption((icon != null ? icon.width : 0) + 6));

            // Handle status bar click
            if (Event.current.type == EventType.MouseDown)
            {
                var labelRect = GUILayoutUtility.GetLastRect();
                if (iconRect.Contains(Event.current.mousePosition) || labelRect.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    LogEntries.ClickStatusBar(Event.current.clickCount);
                    GUIUtility.ExitGUI();
                }
            }

            GUILayout.EndVertical();
        }

        private void RefreshProgressBar(Progress.Item[] progressItems)
        {
            if (!this)
                return;

            if (!Progress.running)
            {
                // If we enter here, it means the last remaining progresses just finished or paused.
                ClearProgressStatus();
                RepaintProgress(progressItems);
                return;
            }

            var idleCount = Progress.EnumerateItems().Count(item => item.running && item.priority == (int)Progress.Priority.Idle);
            var taskCount = Progress.GetRunningProgressCount() - idleCount;
            if (taskCount == 0)
            {
                ClearProgressStatus();
            }
            else
            {
                var currentItem = progressItems.FirstOrDefault(item => item.priority != (int)Progress.Priority.Idle);
                if (currentItem != null && !String.IsNullOrEmpty(currentItem.description))
                    m_ProgressStatus.tooltip = currentItem.name + "\r\n" + currentItem.description;
                m_ProgressPercentageStatus.text = Progress.globalProgress.ToString("P", percentageFormat);

                var remainingTimeText = "";
                var runningProgresses = Progress.EnumerateItems().Where(item => item.running);
                if (Progress.globalRemainingTime.TotalSeconds > 0 && runningProgresses.Any(item => item.timeDisplayMode == Progress.TimeDisplayMode.ShowRemainingTime && item.priority != (int)Progress.Priority.Idle) &&
                    runningProgresses.All(item => !item.indefinite))
                {
                    remainingTimeText = $" [{Progress.globalRemainingTime:g}]";
                }

                if (taskCount > 1)
                    m_ProgressStatus.text = $"Multiple tasks ({taskCount}){remainingTimeText}";
                else
                    m_ProgressStatus.text = $"{currentItem?.name}{remainingTimeText}";

                ScheduleCheckProgressUnresponsive();
            }

            RepaintProgress(progressItems);
        }

        private void RepaintProgress(Progress.Item[] progressItems)
        {
            bool hasSynchronous = false;
            foreach (var item in progressItems)
            {
                if ((item.options & Progress.Options.Synchronous) == Progress.Options.Synchronous)
                {
                    hasSynchronous = true;
                    break;
                }
            }

            if (hasSynchronous)
                RepaintImmediately();
            else
                Repaint();
        }

        private void ClearProgressStatus()
        {
            m_ProgressStatus.text = String.Empty;
            m_ProgressPercentageStatus.text = String.Empty;
        }

        private GUILayoutOption GetStatusTextLayoutOption(float consoleIconWidth)
        {
            int iconWidth = 25;
            float specialModeLabelWidth = Styles.statusLabel.CalcSize(new GUIContent(m_SpecialModeLabel)).x + k_SpaceBeforeProgress + 8;
            float helperBarWidth = (EditorPrefs.GetBool("EnableHelperBar", false) ? ShortcutManagement.HelperWindow.kHelperBarMinWidth : 0);
            float statusRightReservedSpace =
                specialModeLabelWidth +
                helperBarWidth + // helper bar
                iconWidth + // script debugger
                iconWidth + // cache server
                iconWidth; // progress
            if (!showProgress)
                return GUILayout.MaxWidth(position.width - statusRightReservedSpace - consoleIconWidth);

            return GUILayout.MaxWidth(position.width - statusRightReservedSpace - k_SpaceBeforeProgress - 8 - (float)Styles.progressBarWidth.value - k_SpaceAfterProgress - consoleIconWidth);
        }

        [RequiredByNativeCode]
        public static void StatusChanged()
        {
            if (s_AppStatusBar)
                s_AppStatusBar.Repaint();
        }

        [RequiredByNativeCode]
        internal static void StatusChangedImmediate()
        {
            if (s_AppStatusBar)
                s_AppStatusBar.RepaintImmediately();
        }
    }
}
