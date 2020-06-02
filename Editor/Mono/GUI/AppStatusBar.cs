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
            public static readonly GUIContent autoGenerateLightOn = EditorGUIUtility.TrIconContent("AutoLightbakingOn", "Auto Generate Lighting On");
            public static readonly GUIContent autoGenerateLightOff = EditorGUIUtility.TrIconContent("AutoLightbakingOff", "Auto Generate Lighting Off");

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
        private bool? m_autoLightBakingOn;
        private bool m_DrawExtraFeatures;
        private GUIContent m_ProgressStatus = new GUIContent();
        private GUIContent m_ProgressPercentageStatus = new GUIContent();
        private bool m_CurrentProgressNotResponding = false;

        private ManagedDebuggerToggle m_ManagedDebuggerToggle = null;
        private CacheServerToggle m_CacheServerToggle = null;

        private bool showBakeMode
        {
            get
            {
                var settings = Lightmapping.GetLightingSettingsOrDefaultsFallback();
                return settings.bakedGI || settings.realtimeGI;
            }
        }

        private bool showProgress
        {
            get
            {
                return Progress.running;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            s_AppStatusBar = this;
            m_autoLightBakingOn = GetBakeMode();
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
            base.OnDisable();
        }

        protected void OnInspectorUpdate()
        {
            string miniOverview = UnityEditorInternal.ProfilerDriver.miniMemoryOverview;
            bool? autoLightBakingOn = GetBakeMode();

            if ((Unsupported.IsDeveloperMode() && m_MiniMemoryOverview != miniOverview) || (m_autoLightBakingOn != autoLightBakingOn))
            {
                m_MiniMemoryOverview = miniOverview;
                m_autoLightBakingOn = autoLightBakingOn;

                Repaint();
            }
        }

        private void DelayCheckProgressUnresponsive()
        {
            EditorApplication.update -= CheckProgressUnresponsive;
            EditorApplication.CallDelayed(CheckProgressUnresponsive, 0.250);
        }

        private void CheckProgressUnresponsive()
        {
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
                    DelayCheckProgressUnresponsive();
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

            if (Event.current.type == EventType.Repaint)
                Styles.background.Draw(new Rect(0, 0, position.width, position.height), false, false, false, false);

            GUILayout.BeginHorizontal(GUILayout.MaxWidth(position.width));
            {
                GUILayout.Space(2);
                DrawStatusText();
                GUILayout.FlexibleSpace();
                if (m_DrawExtraFeatures)
                    DrawSpecialModeLabel();
                DrawProgressBar();
                if (m_DrawExtraFeatures)
                {
                    DrawDebuggerToggle();
                    DrawCacheServerToggle();
                    DrawBakeMode();
                }
                DrawRefreshStatus();
            }
            GUILayout.EndHorizontal();

            DoWindowDecorationEnd();
            EditorGUI.ShowRepaints();
        }

        private void DrawRefreshStatus()
        {
            bool compiling = EditorApplication.isCompiling;
            bool assembliesLocked = !EditorApplication.CanReloadAssemblies();

            if (compiling || showProgress)
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
                var canHide = EditorUIService.instance.ProgressWindowCanHideDetails();
                if (GUILayout.Button(canHide ? Styles.progressHideIcon : Styles.progressIcon, Styles.statusIcon))
                {
                    if (canHide)
                        EditorUIService.instance.ProgressWindowHideDetails();
                    else
                        Progress.ShowDetails();
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
                Progress.ShowDetails();

            if (globalProgress == -1.0f)
                EditorApplication.delayCall += () => Repaint();

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

        private void DrawBakeMode()
        {
            if (!showBakeMode)
                return;

            if (GUILayout.Button(GetBakeModeIcon(m_autoLightBakingOn), Styles.statusIcon))
            {
                Event.current.Use();
                LightingWindow.CreateLightingWindow();
            }

            var buttonRect = GUILayoutUtility.GetLastRect();
            EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
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
            var taskCount = Progress.GetRunningProgressCount();
            if (taskCount == 0)
            {
                m_ProgressStatus.text = String.Empty;
                m_ProgressPercentageStatus.text = String.Empty;
            }
            else
            {
                var currentItem = progressItems[0];
                if (!String.IsNullOrEmpty(currentItem.description))
                    m_ProgressStatus.tooltip = currentItem.name + "\r\n" + currentItem.description;
                m_ProgressPercentageStatus.text = Progress.globalProgress.ToString("P", percentageFormat);

                var remainingTimeText = "";
                if (progressItems.Any(item => item.timeDisplayMode == Progress.TimeDisplayMode.ShowRemainingTime) &&
                    progressItems.All(item => !item.indefinite) && Progress.globalRemainingTime.TotalSeconds > 0)
                {
                    remainingTimeText = $" [{Progress.globalRemainingTime:g}]";
                }

                if (taskCount > 1)
                    m_ProgressStatus.text = $"Multiple tasks ({taskCount}){remainingTimeText}";
                else
                    m_ProgressStatus.text = $"{currentItem.name}{remainingTimeText}";

                DelayCheckProgressUnresponsive();
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

        private GUILayoutOption GetStatusTextLayoutOption(float consoleIconWidth)
        {
            int iconWidth = 25;
            float specialModeLabelWidth = Styles.statusLabel.CalcSize(new GUIContent(m_SpecialModeLabel)).x + k_SpaceBeforeProgress + 8;
            float statusRightReservedSpace =
                specialModeLabelWidth +
                iconWidth + // script debugger
                iconWidth + // cache server
                (showBakeMode ? iconWidth : 0) + // bake
                iconWidth; // progress
            if (!showProgress)
                return GUILayout.MaxWidth(position.width - statusRightReservedSpace - consoleIconWidth);

            return GUILayout.MaxWidth(position.width - statusRightReservedSpace - k_SpaceBeforeProgress - 8 - (float)Styles.progressBarWidth.value - k_SpaceAfterProgress - consoleIconWidth);
        }

        private GUIContent GetBakeModeIcon(bool? bakeMode)
        {
            if (!bakeMode.HasValue)
                return new GUIContent();
            else if (bakeMode.Value)
                return Styles.autoGenerateLightOn;
            else
                return Styles.autoGenerateLightOff;
        }

        private bool? GetBakeMode()
        {
            if (!showBakeMode)
                return null;

            return Lightmapping.GetLightingSettingsOrDefaultsFallback().autoGenerate;
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
