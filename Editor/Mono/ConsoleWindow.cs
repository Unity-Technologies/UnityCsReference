// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Console", useTypeNameAsIconName = true)]
    internal class ConsoleWindow : EditorWindow, IHasCustomMenu
    {
        //TODO: move this out of here
        internal class Constants
        {
            public static bool ms_Loaded;
            public static GUIStyle Box;
            public static GUIStyle Button;
            public static GUIStyle MiniButton;
            public static GUIStyle MiniButtonLeft;
            public static GUIStyle MiniButtonMiddle;
            public static GUIStyle MiniButtonRight;
            public static GUIStyle LogStyle;
            public static GUIStyle WarningStyle;
            public static GUIStyle ErrorStyle;
            public static GUIStyle EvenBackground;
            public static GUIStyle OddBackground;
            public static GUIStyle MessageStyle;
            public static GUIStyle StatusError;
            public static GUIStyle StatusWarn;
            public static GUIStyle StatusLog;
            public static GUIStyle Toolbar;
            public static GUIStyle CountBadge;
            public static readonly string ClearLabel = L10n.Tr("Clear");
            public static readonly string ClearOnPlayLabel = L10n.Tr("Clear on Play");
            public static readonly string ErrorPauseLabel = L10n.Tr("Error Pause");
            public static readonly string CollapseLabel = L10n.Tr("Collapse");
            public static readonly string StopForAssertLabel = L10n.Tr("Stop for Assert");
            public static readonly string StopForErrorLabel = L10n.Tr("Stop for Error");

            public static void Init()
            {
                if (ms_Loaded)
                    return;
                ms_Loaded = true;
                Box = "CN Box";
                Button = "Button";

                MiniButton = "ToolbarButton";
                MiniButtonLeft = "ToolbarButton";
                MiniButtonMiddle = "ToolbarButton";
                MiniButtonRight = "ToolbarButton";
                Toolbar = "Toolbar";

                LogStyle = "CN EntryInfo";
                WarningStyle = "CN EntryWarn";
                ErrorStyle = "CN EntryError";
                EvenBackground = "CN EntryBackEven";
                OddBackground = "CN EntryBackodd";
                MessageStyle = "CN Message";
                StatusError = "CN StatusError";
                StatusWarn = "CN StatusWarn";
                StatusLog = "CN StatusInfo";
                CountBadge = "CN CountBadge";
            }
        }

        const int m_RowHeight = 32;

        ListViewState m_ListView;
        string m_ActiveText = "";
        private int m_ActiveInstanceID = 0;
        bool m_DevBuild;

        Vector2 m_TextScroll = Vector2.zero;

        SplitterState spl = new SplitterState(new float[] {70, 30}, new int[] {32, 32}, null);

        static bool ms_LoadedIcons = false;
        static internal Texture2D iconInfo, iconWarn, iconError;
        static internal Texture2D iconInfoSmall, iconWarnSmall, iconErrorSmall;
        static internal Texture2D iconInfoMono, iconWarnMono, iconErrorMono;

        int ms_LVHeight = 0;

        AttachProfilerUI m_AttachProfilerUI = new AttachProfilerUI();

        enum Mode
        {
            Error = 1 << 0,
            Assert = 1 << 1,
            Log = 1 << 2,
            Fatal = 1 << 4,
            DontPreprocessCondition = 1 << 5,
            AssetImportError = 1 << 6,
            AssetImportWarning = 1 << 7,
            ScriptingError = 1 << 8,
            ScriptingWarning = 1 << 9,
            ScriptingLog = 1 << 10,
            ScriptCompileError = 1 << 11,
            ScriptCompileWarning = 1 << 12,
            StickyError = 1 << 13,
            MayIgnoreLineNumber = 1 << 14,
            ReportBug = 1 << 15,
            DisplayPreviousErrorInStatusBar = 1 << 16,
            ScriptingException = 1 << 17,
            DontExtractStacktrace = 1 << 18,
            ShouldClearOnPlay = 1 << 19,
            GraphCompileError = 1 << 20,
            ScriptingAssertion = 1 << 21
        };

        enum ConsoleFlags
        {
            Collapse = 1 << 0,
            ClearOnPlay = 1 << 1,
            ErrorPause = 1 << 2,
            Verbose = 1 << 3,
            StopForAssert = 1 << 4,
            StopForError = 1 << 5,
            Autoscroll = 1 << 6,
            LogLevelLog = 1 << 7,
            LogLevelWarning = 1 << 8,
            LogLevelError = 1 << 9
        };

        static ConsoleWindow ms_ConsoleWindow = null;

        static void ShowConsoleWindowImmediate()
        {
            ShowConsoleWindow(true);
        }

        public static void ShowConsoleWindow(bool immediate)
        {
            if (ms_ConsoleWindow == null)
            {
                ms_ConsoleWindow = ScriptableObject.CreateInstance<ConsoleWindow>();
                ms_ConsoleWindow.Show(immediate);
                ms_ConsoleWindow.Focus();
            }
            else
            {
                ms_ConsoleWindow.Show(immediate);
                ms_ConsoleWindow.Focus();
            }
        }

        static internal void LoadIcons()
        {
            if (ms_LoadedIcons)
                return;

            ms_LoadedIcons = true;
            iconInfo = EditorGUIUtility.LoadIcon("console.infoicon");
            iconWarn = EditorGUIUtility.LoadIcon("console.warnicon");
            iconError = EditorGUIUtility.LoadIcon("console.erroricon");
            iconInfoSmall = EditorGUIUtility.LoadIcon("console.infoicon.sml");
            iconWarnSmall = EditorGUIUtility.LoadIcon("console.warnicon.sml");
            iconErrorSmall = EditorGUIUtility.LoadIcon("console.erroricon.sml");

            // TODO: Once we get the proper monochrome images put them here.
            /*iconInfoMono = EditorGUIUtility.LoadIcon("console.infoicon.mono");
            iconWarnMono = EditorGUIUtility.LoadIcon("console.warnicon.mono");
            iconErrorMono = EditorGUIUtility.LoadIcon("console.erroricon.mono");*/
            iconInfoMono = EditorGUIUtility.LoadIcon("console.infoicon.sml");
            iconWarnMono = EditorGUIUtility.LoadIcon("console.warnicon.inactive.sml");
            iconErrorMono = EditorGUIUtility.LoadIcon("console.erroricon.inactive.sml");
            Constants.Init();
        }

        [RequiredByNativeCode]
        public static void LogChanged()
        {
            if (ms_ConsoleWindow == null)
                return;

            ms_ConsoleWindow.DoLogChanged();
        }

        public void DoLogChanged()
        {
            ms_ConsoleWindow.Repaint();
        }

        public ConsoleWindow()
        {
            position = new Rect(200, 200, 800, 400); //TODO:
            m_ListView = new ListViewState(0, m_RowHeight);
        }

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();
            ms_ConsoleWindow = this;
            m_DevBuild = Unsupported.IsDeveloperBuild();
        }

        void OnDisable()
        {
            if (ms_ConsoleWindow == this)
                ms_ConsoleWindow = null;
        }

        private static bool HasMode(int mode, Mode modeToCheck) { return (mode & (int)modeToCheck) != 0; }
        private static bool HasFlag(ConsoleFlags flags) { return (LogEntries.consoleFlags & (int)flags) != 0; }
        private static void SetFlag(ConsoleFlags flags, bool val) { LogEntries.SetConsoleFlag((int)flags, val); }

        static internal Texture2D GetIconForErrorMode(int mode, bool large)
        {
            // Errors
            if (HasMode(mode, Mode.Fatal | Mode.Assert |
                    Mode.Error | Mode.ScriptingError |
                    Mode.AssetImportError | Mode.ScriptCompileError |
                    Mode.GraphCompileError | Mode.ScriptingAssertion))
                return large ? iconError : iconErrorSmall;
            // Warnings
            if (HasMode(mode, Mode.ScriptCompileWarning | Mode.ScriptingWarning | Mode.AssetImportWarning))
                return large ? iconWarn : iconWarnSmall;
            // Logs
            if (HasMode(mode, Mode.Log | Mode.ScriptingLog))
                return large ? iconInfo : iconInfoSmall;

            // Nothing
            return null;
        }

        static internal GUIStyle GetStyleForErrorMode(int mode)
        {
            // Errors
            if (HasMode(mode, Mode.Fatal | Mode.Assert |
                    Mode.Error | Mode.ScriptingError |
                    Mode.AssetImportError | Mode.ScriptCompileError |
                    Mode.GraphCompileError | Mode.ScriptingAssertion))
                return Constants.ErrorStyle;
            // Warnings
            if (HasMode(mode, Mode.ScriptCompileWarning | Mode.ScriptingWarning | Mode.AssetImportWarning))
                return Constants.WarningStyle;
            // Logs
            return Constants.LogStyle;
        }

        static internal GUIStyle GetStatusStyleForErrorMode(int mode)
        {
            // Errors
            if (HasMode(mode, Mode.Fatal | Mode.Assert |
                    Mode.Error | Mode.ScriptingError |
                    Mode.AssetImportError | Mode.ScriptCompileError |
                    Mode.GraphCompileError | Mode.ScriptingAssertion))
                return Constants.StatusError;
            // Warnings
            if (HasMode(mode, Mode.ScriptCompileWarning | Mode.ScriptingWarning | Mode.AssetImportWarning))
                return Constants.StatusWarn;
            // Logs
            return Constants.StatusLog;
        }

        static string ContextString(LogEntry entry)
        {
            StringBuilder context = new StringBuilder();

            if (HasMode(entry.mode, Mode.Error))
                context.Append("Error ");
            else if (HasMode(entry.mode, Mode.Log))
                context.Append("Log ");
            else
                context.Append("Assert ");

            context.Append("in file: ");
            context.Append(entry.file);
            context.Append(" at line: ");
            context.Append(entry.line);

            if (entry.errorNum != 0)
            {
                context.Append(" and errorNum: ");
                context.Append(entry.errorNum);
            }

            return context.ToString();
        }

        static string GetFirstLine(string s)
        {
            int i = s.IndexOf("\n");
            return (i != -1) ? s.Substring(0, i) : s;
        }

        static string GetFirstTwoLines(string s)
        {
            int i = s.IndexOf("\n");
            if (i != -1)
            {
                i = s.IndexOf("\n", i + 1);
                if (i != -1)
                    return s.Substring(0, i);
            }

            return s;
        }

        void SetActiveEntry(LogEntry entry)
        {
            if (entry != null)
            {
                m_ActiveText = entry.condition;
                // ping object referred by the log entry
                if (m_ActiveInstanceID != entry.instanceID)
                {
                    m_ActiveInstanceID = entry.instanceID;
                    if (entry.instanceID != 0)
                        EditorGUIUtility.PingObject(entry.instanceID);
                }
            }
            else
            {
                m_ActiveText = string.Empty;
                m_ActiveInstanceID = 0;
                m_ListView.row = -1;
            }
        }

        static void ShowConsoleRow(int row)
        {
            ShowConsoleWindow(false);

            if (ms_ConsoleWindow)
            {
                ms_ConsoleWindow.m_ListView.row = row;
                ms_ConsoleWindow.m_ListView.selectionChanged = true;
                ms_ConsoleWindow.Repaint();
            }
        }

        void OnGUI()
        {
            Event e = Event.current;
            LoadIcons();

            GUILayout.BeginHorizontal(Constants.Toolbar);

            if (GUILayout.Button(Constants.ClearLabel, Constants.MiniButton))
            {
                LogEntries.Clear();
                GUIUtility.keyboardControl = 0;
            }

            int currCount = LogEntries.GetCount();

            if (m_ListView.totalRows != currCount && m_ListView.totalRows > 0)
            {
                // scroll bar was at the bottom?
                if (m_ListView.scrollPos.y >= m_ListView.rowHeight * m_ListView.totalRows - ms_LVHeight)
                {
                    m_ListView.scrollPos.y = currCount * m_RowHeight - ms_LVHeight;
                }
            }

            EditorGUILayout.Space();

            bool wasCollapsed = HasFlag(ConsoleFlags.Collapse);
            SetFlag(ConsoleFlags.Collapse, GUILayout.Toggle(wasCollapsed, Constants.CollapseLabel, Constants.MiniButtonLeft));

            bool collapsedChanged = (wasCollapsed != HasFlag(ConsoleFlags.Collapse));
            if (collapsedChanged)
            {
                // unselect if collapsed flag changed
                m_ListView.row = -1;

                // scroll to bottom
                m_ListView.scrollPos.y = LogEntries.GetCount() * m_RowHeight;
            }

            SetFlag(ConsoleFlags.ClearOnPlay, GUILayout.Toggle(HasFlag(ConsoleFlags.ClearOnPlay), Constants.ClearOnPlayLabel, Constants.MiniButtonMiddle));
            SetFlag(ConsoleFlags.ErrorPause, GUILayout.Toggle(HasFlag(ConsoleFlags.ErrorPause), Constants.ErrorPauseLabel, Constants.MiniButtonRight));

            m_AttachProfilerUI.OnGUILayout(this);

            EditorGUILayout.Space();

            if (m_DevBuild)
            {
                GUILayout.FlexibleSpace();
                SetFlag(ConsoleFlags.StopForAssert, GUILayout.Toggle(HasFlag(ConsoleFlags.StopForAssert), Constants.StopForAssertLabel, Constants.MiniButtonLeft));
                SetFlag(ConsoleFlags.StopForError, GUILayout.Toggle(HasFlag(ConsoleFlags.StopForError), Constants.StopForErrorLabel, Constants.MiniButtonRight));
            }

            GUILayout.FlexibleSpace();

            int errorCount = 0, warningCount = 0, logCount = 0;
            LogEntries.GetCountsByType(ref errorCount, ref warningCount, ref logCount);
            EditorGUI.BeginChangeCheck();
            bool setLogFlag = GUILayout.Toggle(HasFlag(ConsoleFlags.LogLevelLog), new GUIContent((logCount <= 999 ? logCount.ToString() : "999+"), logCount > 0 ? iconInfoSmall : iconInfoMono), Constants.MiniButtonRight);
            bool setWarningFlag = GUILayout.Toggle(HasFlag(ConsoleFlags.LogLevelWarning), new GUIContent((warningCount <= 999 ? warningCount.ToString() : "999+"), warningCount > 0 ? iconWarnSmall : iconWarnMono), Constants.MiniButtonMiddle);
            bool setErrorFlag = GUILayout.Toggle(HasFlag(ConsoleFlags.LogLevelError), new GUIContent((errorCount <= 999 ? errorCount.ToString() : "999+"), errorCount > 0 ? iconErrorSmall : iconErrorMono), Constants.MiniButtonLeft);
            // Active entry index may no longer be valid
            if (EditorGUI.EndChangeCheck())
                SetActiveEntry(null);

            SetFlag(ConsoleFlags.LogLevelLog, setLogFlag);
            SetFlag(ConsoleFlags.LogLevelWarning, setWarningFlag);
            SetFlag(ConsoleFlags.LogLevelError, setErrorFlag);

            GUILayout.EndHorizontal();

            m_ListView.totalRows = LogEntries.StartGettingEntries();

            SplitterGUILayout.BeginVerticalSplit(spl);
            EditorGUIUtility.SetIconSize(new Vector2(m_RowHeight, m_RowHeight));
            GUIContent tempContent = new GUIContent();
            int id = GUIUtility.GetControlID(0);

            /////@TODO: Make Frame selected work with ListViewState
            try
            {
                bool selected = false;
                bool collapsed = HasFlag(ConsoleFlags.Collapse);
                foreach (ListViewElement el in ListViewGUI.ListView(m_ListView, Constants.Box))
                {
                    if (e.type == EventType.mouseDown && e.button == 0 && el.position.Contains(e.mousePosition))
                    {
                        if (e.clickCount == 2)
                        {
                            LogEntries.RowGotDoubleClicked(m_ListView.row);
                        }

                        selected = true;
                    }

                    // Paint list view element
                    if (e.type == EventType.repaint)
                    {
                        int mode = 0;
                        string text = null;
                        LogEntries.GetFirstTwoLinesEntryTextAndModeInternal(el.row, ref mode, ref text);

                        GUIStyle s = el.row % 2 == 0 ? Constants.OddBackground : Constants.EvenBackground;
                        s.Draw(el.position, false, false, m_ListView.row == el.row, false);
                        tempContent.text = text;
                        GUIStyle errorModeStyle = GetStyleForErrorMode(mode);
                        errorModeStyle.Draw(el.position, tempContent, id, m_ListView.row == el.row);

                        if (collapsed)
                        {
                            Rect badgeRect = el.position;
                            tempContent.text = LogEntries.GetEntryCount(el.row).ToString(CultureInfo.InvariantCulture);
                            Vector2 badgeSize = Constants.CountBadge.CalcSize(tempContent);
                            badgeRect.xMin = badgeRect.xMax - badgeSize.x;
                            badgeRect.yMin += ((badgeRect.yMax - badgeRect.yMin) - badgeSize.y) * 0.5f;
                            badgeRect.x -= 5f;
                            GUI.Label(badgeRect, tempContent, Constants.CountBadge);
                        }
                    }
                }

                if (selected)
                {
                    if (m_ListView.scrollPos.y >= m_ListView.rowHeight * m_ListView.totalRows - ms_LVHeight)
                        m_ListView.scrollPos.y = m_ListView.rowHeight * m_ListView.totalRows - ms_LVHeight - 1;
                }


                // Make sure the selected entry is up to date
                if (m_ListView.totalRows == 0 || m_ListView.row >= m_ListView.totalRows || m_ListView.row < 0)
                {
                    if (m_ActiveText.Length != 0)
                        SetActiveEntry(null);
                }
                else
                {
                    LogEntry entry = new LogEntry();
                    LogEntries.GetEntryInternal(m_ListView.row, entry);
                    SetActiveEntry(entry);

                    // see if selected entry changed. if so - clear additional info
                    LogEntries.GetEntryInternal(m_ListView.row, entry);
                    if (m_ListView.selectionChanged || !m_ActiveText.Equals(entry.condition))
                    {
                        SetActiveEntry(entry);
                    }
                }

                // Open entry using return key
                if ((GUIUtility.keyboardControl == m_ListView.ID) && (e.type == EventType.KeyDown) && (e.keyCode == KeyCode.Return) && (m_ListView.row != 0))
                {
                    LogEntries.RowGotDoubleClicked(m_ListView.row);
                    Event.current.Use();
                }

                if (e.type != EventType.Layout && ListViewGUI.ilvState.rectHeight != 1)
                    ms_LVHeight = ListViewGUI.ilvState.rectHeight;
            }
            finally
            {
                LogEntries.EndGettingEntries();
                EditorGUIUtility.SetIconSize(Vector2.zero);
            }

            // Display active text (We want wordwrapped text with a vertical scrollbar)
            m_TextScroll = GUILayout.BeginScrollView(m_TextScroll, Constants.Box);
            float height = Constants.MessageStyle.CalcHeight(GUIContent.Temp(m_ActiveText), position.width);
            EditorGUILayout.SelectableLabel(m_ActiveText, Constants.MessageStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(height));
            GUILayout.EndScrollView();

            SplitterGUILayout.EndVerticalSplit();

            // Copy & Paste selected item
            if ((e.type == EventType.ValidateCommand || e.type == EventType.ExecuteCommand) && e.commandName == "Copy" && m_ActiveText != string.Empty)
            {
                if (e.type == EventType.ExecuteCommand)
                    EditorGUIUtility.systemCopyBuffer = m_ActiveText;
                e.Use();
            }
        }

        public static bool GetConsoleErrorPause()
        {
            return HasFlag(ConsoleFlags.ErrorPause);
        }

        public static void SetConsoleErrorPause(bool enabled)
        {
            SetFlag(ConsoleFlags.ErrorPause, enabled);
        }

        public struct StackTraceLogTypeData
        {
            public LogType logType;
            public StackTraceLogType stackTraceLogType;
        }

        public void ToggleLogStackTraces(object userData)
        {
            StackTraceLogTypeData data = (StackTraceLogTypeData)userData;
            PlayerSettings.SetStackTraceLogType(data.logType, data.stackTraceLogType);
        }

        public void ToggleLogStackTracesForAll(object userData)
        {
            foreach (LogType logType in Enum.GetValues(typeof(LogType)))
                PlayerSettings.SetStackTraceLogType(logType, (StackTraceLogType)userData);
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
                menu.AddItem(new GUIContent("Open Player Log"), false, UnityEditorInternal.InternalEditorUtility.OpenPlayerConsole);
            menu.AddItem(new GUIContent("Open Editor Log"), false, UnityEditorInternal.InternalEditorUtility.OpenEditorConsole);

            AddStackTraceLoggingMenu(menu);
        }

        private void AddStackTraceLoggingMenu(GenericMenu menu)
        {
            // TODO: Maybe remove this, because it basically duplicates UI in PlayerSettings
            foreach (LogType logType in Enum.GetValues(typeof(LogType)))
            {
                foreach (StackTraceLogType stackTraceLogType in Enum.GetValues(typeof(StackTraceLogType)))
                {
                    StackTraceLogTypeData data;
                    data.logType = logType;
                    data.stackTraceLogType = stackTraceLogType;

                    menu.AddItem(new GUIContent("Stack Trace Logging/" + logType + "/" + stackTraceLogType), PlayerSettings.GetStackTraceLogType(logType) == stackTraceLogType,
                        ToggleLogStackTraces, data);
                }
            }

            int stackTraceLogTypeForAll = (int)PlayerSettings.GetStackTraceLogType(LogType.Log);
            foreach (LogType logType in Enum.GetValues(typeof(LogType)))
            {
                if (PlayerSettings.GetStackTraceLogType(logType) != (StackTraceLogType)stackTraceLogTypeForAll)
                {
                    stackTraceLogTypeForAll = -1;
                    break;
                }
            }

            foreach (StackTraceLogType stackTraceLogType in Enum.GetValues(typeof(StackTraceLogType)))
            {
                menu.AddItem(new GUIContent("Stack Trace Logging/All/" + stackTraceLogType), (StackTraceLogType)stackTraceLogTypeForAll == stackTraceLogType,
                    ToggleLogStackTracesForAll, stackTraceLogType);
            }
        }
    }
}
