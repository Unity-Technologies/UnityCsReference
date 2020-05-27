// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;
using System.Text;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using UnityEngine.Networking.PlayerConnection;
using UnityEditor.Networking.PlayerConnection;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Console", useTypeNameAsIconName = true)]
    internal class ConsoleWindow : EditorWindow, IHasCustomMenu
    {
        internal delegate void EntryDoubleClickedDelegate(LogEntry entry);

        //TODO: move this out of here
        internal class Constants
        {
            private static bool ms_Loaded;
            private static int ms_logStyleLineCount;
            public static GUIStyle Box;
            public static GUIStyle MiniButton;
            public static GUIStyle MiniButtonRight;
            public static GUIStyle LogStyle;
            public static GUIStyle WarningStyle;
            public static GUIStyle ErrorStyle;
            public static GUIStyle IconLogStyle;
            public static GUIStyle IconWarningStyle;
            public static GUIStyle IconErrorStyle;
            public static GUIStyle EvenBackground;
            public static GUIStyle OddBackground;
            public static GUIStyle MessageStyle;
            public static GUIStyle StatusError;
            public static GUIStyle StatusWarn;
            public static GUIStyle StatusLog;
            public static GUIStyle Toolbar;
            public static GUIStyle CountBadge;
            public static GUIStyle LogSmallStyle;
            public static GUIStyle WarningSmallStyle;
            public static GUIStyle ErrorSmallStyle;
            public static GUIStyle IconLogSmallStyle;
            public static GUIStyle IconWarningSmallStyle;
            public static GUIStyle IconErrorSmallStyle;

            public static readonly GUIContent Clear = EditorGUIUtility.TrTextContent("Clear", "Clear console entries");
            public static readonly GUIContent ClearOnPlay = EditorGUIUtility.TrTextContent("Clear on Play");
            public static readonly GUIContent ClearOnBuild = EditorGUIUtility.TrTextContent("Clear on Build");
            public static readonly GUIContent Collapse = EditorGUIUtility.TrTextContent("Collapse", "Collapse identical entries");
            public static readonly GUIContent ErrorPause = EditorGUIUtility.TrTextContent("Error Pause", "Pause Play Mode on error");
            public static readonly GUIContent StopForAssert = EditorGUIUtility.TrTextContent("Stop for Assert");
            public static readonly GUIContent StopForError = EditorGUIUtility.TrTextContent("Stop for Error");

            public static int LogStyleLineCount
            {
                get { return ms_logStyleLineCount; }
                set
                {
                    ms_logStyleLineCount = value;

                    // If Constants hasn't been initialized yet we just skip this for now
                    // and let Init() call this for us in a bit.
                    if (!ms_Loaded)
                        return;
                    UpdateLogStyleFixedHeights();
                }
            }

            public static void Init()
            {
                if (ms_Loaded)
                    return;
                ms_Loaded = true;
                Box = "CN Box";

                MiniButton = "ToolbarButton";
                MiniButtonRight = "ToolbarButtonRight";
                Toolbar = "Toolbar";
                LogStyle = "CN EntryInfo";
                LogSmallStyle = "CN EntryInfoSmall";
                WarningStyle = "CN EntryWarn";
                WarningSmallStyle = "CN EntryWarnSmall";
                ErrorStyle = "CN EntryError";
                ErrorSmallStyle = "CN EntryErrorSmall";
                IconLogStyle = "CN EntryInfoIcon";
                IconLogSmallStyle = "CN EntryInfoIconSmall";
                IconWarningStyle = "CN EntryWarnIcon";
                IconWarningSmallStyle = "CN EntryWarnIconSmall";
                IconErrorStyle = "CN EntryErrorIcon";
                IconErrorSmallStyle = "CN EntryErrorIconSmall";
                EvenBackground = "CN EntryBackEven";
                OddBackground = "CN EntryBackodd";
                MessageStyle = "CN Message";
                StatusError = "CN StatusError";
                StatusWarn = "CN StatusWarn";
                StatusLog = "CN StatusInfo";
                CountBadge = "CN CountBadge";

                // If the console window isn't open OnEnable() won't trigger so it will end up with 0 lines,
                // so we always make sure we read it up when we initialize here.
                LogStyleLineCount = EditorPrefs.GetInt("ConsoleWindowLogLineCount", 2);
            }

            private static void UpdateLogStyleFixedHeights()
            {
                // Whenever we change the line height count or the styles are set we need to update the fixed height
                // of the following GuiStyles so the entries do not get cropped incorrectly.
                ErrorStyle.fixedHeight = (LogStyleLineCount * ErrorStyle.lineHeight) + ErrorStyle.border.top;
                WarningStyle.fixedHeight = (LogStyleLineCount * WarningStyle.lineHeight) + WarningStyle.border.top;
                LogStyle.fixedHeight = (LogStyleLineCount * LogStyle.lineHeight) + LogStyle.border.top;
            }
        }

        int m_LineHeight;
        int m_BorderHeight;

        bool m_HasUpdatedGuiStyles;

        ListViewState m_ListView;
        string m_ActiveText = "";
        StringBuilder m_CopyString;
        private int m_ActiveInstanceID = 0;
        bool m_DevBuild;

        Vector2 m_TextScroll = Vector2.zero;

        SplitterState spl = SplitterState.FromRelative(new float[] {70, 30}, new float[] {32, 32}, null);

        static bool ms_LoadedIcons = false;
        static internal Texture2D iconInfo, iconWarn, iconError;
        static internal Texture2D iconInfoSmall, iconWarnSmall, iconErrorSmall;
        static internal Texture2D iconInfoMono, iconWarnMono, iconErrorMono;

        int ms_LVHeight = 0;

        class ConsoleAttachToPlayerState : GeneralConnectionState
        {
            static class Content
            {
                public static GUIContent PlayerLogging = EditorGUIUtility.TrTextContent("Player Logging");
                public static GUIContent FullLog = EditorGUIUtility.TrTextContent("Full Log (Developer Mode Only)");
            }

            public ConsoleAttachToPlayerState(EditorWindow parentWindow, Action<string> connectedCallback = null) : base(parentWindow, connectedCallback)
            {
                // This is needed to force initialize the instance and the state so that messages from players are received and printed to the console (if that is the serialized state)
                // on creation of the ConsoleWindow UI instead of when the uer first clicks on the dropdown, and triggers AddItemsToMenu.
                PlayerConnectionLogReceiver.instance.State = PlayerConnectionLogReceiver.instance.State;
            }

            bool IsConnected()
            {
                return PlayerConnectionLogReceiver.instance.State != PlayerConnectionLogReceiver.ConnectionState.Disconnected;
            }

            void PlayerLoggingOptionSelected()
            {
                PlayerConnectionLogReceiver.instance.State = IsConnected() ? PlayerConnectionLogReceiver.ConnectionState.Disconnected : PlayerConnectionLogReceiver.ConnectionState.CleanLog;
            }

            bool IsLoggingFullLog()
            {
                return PlayerConnectionLogReceiver.instance.State == PlayerConnectionLogReceiver.ConnectionState.FullLog;
            }

            void FullLogOptionSelected()
            {
                PlayerConnectionLogReceiver.instance.State = IsLoggingFullLog() ? PlayerConnectionLogReceiver.ConnectionState.CleanLog : PlayerConnectionLogReceiver.ConnectionState.FullLog;
            }

            public override void AddItemsToMenu(GenericMenu menu, Rect position)
            {
                // option to turn logging and the connection on or of
                menu.AddItem(Content.PlayerLogging, IsConnected(), PlayerLoggingOptionSelected);
                if (IsConnected())
                {
                    // All other options but the first are only available if logging is enabled
                    menu.AddItem(Content.FullLog, IsLoggingFullLog(), FullLogOptionSelected);
                    menu.AddSeparator("");
                    base.AddItemsToMenu(menu, position);
                }
            }
        }

        IConnectionState m_ConsoleAttachToPlayerState;

        [Flags]
        internal enum Mode
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
            ScriptingAssertion = 1 << 21,
            VisualScriptingError = 1 << 22
        }

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
            LogLevelError = 1 << 9,
            ShowTimestamp = 1 << 10,
            ClearOnBuild = 1 << 11,
        }

        static ConsoleWindow ms_ConsoleWindow = null;
        private string m_SearchText;

        public static void ShowConsoleWindow(bool immediate)
        {
            if (ms_ConsoleWindow == null)
            {
                ms_ConsoleWindow = ScriptableObject.CreateInstance<ConsoleWindow>();
                if (UnityEditor.MPE.ProcessService.level == MPE.ProcessLevel.Master)
                    ms_ConsoleWindow.Show(immediate);
                else
                    ms_ConsoleWindow.ShowModalUtility();
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
            iconInfoMono = EditorGUIUtility.LoadIcon("console.infoicon.inactive.sml");
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
            position = new Rect(200, 200, 800, 400);
            m_ListView = new ListViewState(0, 0);
            m_CopyString = new StringBuilder();
            m_SearchText = string.Empty;
            EditorGUI.hyperLinkClicked += EditorGUI_HyperLinkClicked;
        }

        internal void OnEnable()
        {
            if (m_ConsoleAttachToPlayerState == null)
                m_ConsoleAttachToPlayerState = new ConsoleAttachToPlayerState(this);

            // Update the filter on enable for DomainReload(keep current filter) and window opening(reset filter because m_searchText is null)
            SetFilter(LogEntries.GetFilteringText());

            wantsLessLayoutEvents = true;
            titleContent = GetLocalizedTitleContent();
            ms_ConsoleWindow = this;
            m_DevBuild = Unsupported.IsDeveloperMode();

            Constants.LogStyleLineCount = EditorPrefs.GetInt("ConsoleWindowLogLineCount", 2);
        }

        internal void OnDisable()
        {
            m_ConsoleAttachToPlayerState?.Dispose();
            m_ConsoleAttachToPlayerState = null;

            if (ms_ConsoleWindow == this)
                ms_ConsoleWindow = null;
        }

        private int RowHeight => (Constants.LogStyleLineCount > 1 ? Mathf.Max(32, (Constants.LogStyleLineCount * m_LineHeight)) : m_LineHeight) + m_BorderHeight;

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

        static internal GUIStyle GetStyleForErrorMode(int mode, bool isIcon, bool isSmall)
        {
            // Errors
            if (HasMode(mode, Mode.Fatal | Mode.Assert |
                Mode.Error | Mode.ScriptingError |
                Mode.AssetImportError | Mode.ScriptCompileError |
                Mode.GraphCompileError | Mode.ScriptingAssertion | Mode.ScriptingException))
            {
                if (isIcon)
                {
                    if (isSmall)
                    {
                        return Constants.IconErrorSmallStyle;
                    }
                    return Constants.IconErrorStyle;
                }

                if (isSmall)
                {
                    return Constants.ErrorSmallStyle;
                }
                return Constants.ErrorStyle;
            }
            // Warnings
            if (HasMode(mode, Mode.ScriptCompileWarning | Mode.ScriptingWarning | Mode.AssetImportWarning))
            {
                if (isIcon)
                {
                    if (isSmall)
                    {
                        return Constants.IconWarningSmallStyle;
                    }
                    return Constants.IconWarningStyle;
                }

                if (isSmall)
                {
                    return Constants.WarningSmallStyle;
                }
                return Constants.WarningStyle;
            }
            // Logs
            if (isIcon)
            {
                if (isSmall)
                {
                    return Constants.IconLogSmallStyle;
                }
                return Constants.IconLogStyle;
            }

            if (isSmall)
            {
                return Constants.LogSmallStyle;
            }
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

        void SetActiveEntry(LogEntry entry)
        {
            if (entry != null)
            {
                m_ActiveText = entry.message;
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
                m_CopyString.Clear();
            }
        }

        [UsedImplicitly] // Used implicitly with CallStaticMonoMethod("ConsoleWindow", "ShowConsoleRow", param);
        internal static void ShowConsoleRow(int row)
        {
            ShowConsoleWindow(false);

            if (ms_ConsoleWindow)
            {
                ms_ConsoleWindow.m_ListView.row = row;
                ms_ConsoleWindow.m_ListView.selectionChanged = true;
                ms_ConsoleWindow.Repaint();
            }
        }

        void UpdateListView()
        {
            m_HasUpdatedGuiStyles = true;
            int newRowHeight = RowHeight;

            // We reset the scroll list to auto scrolling whenever the log entry count is modified
            m_ListView.rowHeight = newRowHeight;
            m_ListView.row = -1;
            m_ListView.scrollPos.y = LogEntries.GetCount() * newRowHeight;
        }

        bool HasSpaceForExtraButtons()
        {
            return position.width > 420;
        }

        internal void OnGUI()
        {
            Event e = Event.current;
            LoadIcons();

            if (!m_HasUpdatedGuiStyles)
            {
                m_LineHeight = Mathf.RoundToInt(Constants.ErrorStyle.lineHeight);
                m_BorderHeight = Constants.ErrorStyle.border.top + Constants.ErrorStyle.border.bottom;
                UpdateListView();
            }

            GUILayout.BeginHorizontal(Constants.Toolbar);

            // Clear button and clearing options
            bool clearClicked = false;
            if (EditorGUILayout.DropDownToggle(ref clearClicked, Constants.Clear, EditorStyles.toolbarDropDownToggle))
            {
                var clearOnPlay = HasFlag(ConsoleFlags.ClearOnPlay);
                var clearOnBuild = HasFlag(ConsoleFlags.ClearOnBuild);

                GenericMenu menu = new GenericMenu();
                menu.AddItem(Constants.ClearOnPlay, clearOnPlay, () => { SetFlag(ConsoleFlags.ClearOnPlay, !clearOnPlay); });
                menu.AddItem(Constants.ClearOnBuild, clearOnBuild, () => { SetFlag(ConsoleFlags.ClearOnBuild, !clearOnBuild); });
                var rect = GUILayoutUtility.GetLastRect();
                rect.y += EditorGUIUtility.singleLineHeight;
                menu.DropDown(rect);
            }
            if (clearClicked)
            {
                LogEntries.Clear();
                GUIUtility.keyboardControl = 0;
            }

            int currCount = LogEntries.GetCount();

            if (m_ListView.totalRows != currCount)
            {
                // scroll bar was at the bottom?
                if (m_ListView.scrollPos.y >= m_ListView.rowHeight * m_ListView.totalRows - ms_LVHeight)
                {
                    m_ListView.scrollPos.y = currCount * RowHeight - ms_LVHeight;
                }
            }

            bool wasCollapsed = HasFlag(ConsoleFlags.Collapse);
            SetFlag(ConsoleFlags.Collapse, GUILayout.Toggle(wasCollapsed, Constants.Collapse, Constants.MiniButton));

            bool collapsedChanged = (wasCollapsed != HasFlag(ConsoleFlags.Collapse));
            if (collapsedChanged)
            {
                // unselect if collapsed flag changed
                m_ListView.row = -1;

                // scroll to bottom
                m_ListView.scrollPos.y = LogEntries.GetCount() * RowHeight;
            }

            if (HasSpaceForExtraButtons())
            {
                SetFlag(ConsoleFlags.ErrorPause, GUILayout.Toggle(HasFlag(ConsoleFlags.ErrorPause), Constants.ErrorPause, Constants.MiniButton));
                PlayerConnectionGUILayout.ConnectionTargetSelectionDropdown(m_ConsoleAttachToPlayerState, EditorStyles.toolbarDropDown);
            }

            GUILayout.FlexibleSpace();

            // Search bar
            if (HasSpaceForExtraButtons())
                SearchField(e);

            // Flags
            int errorCount = 0, warningCount = 0, logCount = 0;
            LogEntries.GetCountsByType(ref errorCount, ref warningCount, ref logCount);
            EditorGUI.BeginChangeCheck();
            bool setLogFlag = GUILayout.Toggle(HasFlag(ConsoleFlags.LogLevelLog), new GUIContent((logCount <= 999 ? logCount.ToString() : "999+"), logCount > 0 ? iconInfoSmall : iconInfoMono), Constants.MiniButton);
            bool setWarningFlag = GUILayout.Toggle(HasFlag(ConsoleFlags.LogLevelWarning), new GUIContent((warningCount <= 999 ? warningCount.ToString() : "999+"), warningCount > 0 ? iconWarnSmall : iconWarnMono), Constants.MiniButton);
            bool setErrorFlag = GUILayout.Toggle(HasFlag(ConsoleFlags.LogLevelError), new GUIContent((errorCount <= 999 ? errorCount.ToString() : "999+"), errorCount > 0 ? iconErrorSmall : iconErrorMono), Constants.MiniButtonRight);
            // Active entry index may no longer be valid
            if (EditorGUI.EndChangeCheck())
                SetActiveEntry(null);

            SetFlag(ConsoleFlags.LogLevelLog, setLogFlag);
            SetFlag(ConsoleFlags.LogLevelWarning, setWarningFlag);
            SetFlag(ConsoleFlags.LogLevelError, setErrorFlag);

            GUILayout.EndHorizontal();

            // Console entries
            SplitterGUILayout.BeginVerticalSplit(spl);

            GUIContent tempContent = new GUIContent();
            int id = GUIUtility.GetControlID(0);
            int rowDoubleClicked = -1;

            /////@TODO: Make Frame selected work with ListViewState
            using (new GettingLogEntriesScope(m_ListView))
            {
                int selectedRow = -1;
                bool openSelectedItem = false;
                bool collapsed = HasFlag(ConsoleFlags.Collapse);
                foreach (ListViewElement el in ListViewGUI.ListView(m_ListView, ListViewOptions.wantsRowMultiSelection, Constants.Box))
                {
                    if (e.type == EventType.MouseDown && e.button == 0 && el.position.Contains(e.mousePosition))
                    {
                        selectedRow = m_ListView.row;
                        if (e.clickCount == 2)
                            openSelectedItem = true;
                    }
                    else if (e.type == EventType.Repaint)
                    {
                        int mode = 0;
                        string text = null;
                        LogEntries.GetLinesAndModeFromEntryInternal(el.row, Constants.LogStyleLineCount, ref mode, ref text);
                        bool entryIsSelected = m_ListView.selectedItems != null && el.row < m_ListView.selectedItems.Length && m_ListView.selectedItems[el.row];

                        // offset value in x for icon and text
                        var offset = Constants.LogStyleLineCount == 1 ? 4 : 8;

                        // Draw the background
                        GUIStyle s = el.row % 2 == 0 ? Constants.OddBackground : Constants.EvenBackground;
                        s.Draw(el.position, false, false, entryIsSelected, false);

                        // Draw the icon
                        GUIStyle iconStyle = GetStyleForErrorMode(mode, true, Constants.LogStyleLineCount == 1);
                        Rect iconRect = el.position;
                        iconRect.x += offset;
                        iconRect.y += 2;

                        iconStyle.Draw(iconRect, false, false, entryIsSelected, false);

                        // Draw the text
                        tempContent.text = text;
                        GUIStyle errorModeStyle =
                            GetStyleForErrorMode(mode, false, Constants.LogStyleLineCount == 1);
                        var textRect = el.position;
                        textRect.x += offset;

                        if (string.IsNullOrEmpty(m_SearchText))
                            errorModeStyle.Draw(textRect, tempContent, id, m_ListView.row == el.row);
                        else
                        {
                            //the whole text contains the searchtext, we have to know where it is
                            int startIndex = text.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase);
                            if (startIndex == -1) // the searchtext is not in the visible text, we don't show the selection
                                errorModeStyle.Draw(textRect, tempContent, id, m_ListView.row == el.row);
                            else // the searchtext is visible, we show the selection
                            {
                                int endIndex = startIndex + m_SearchText.Length;

                                const bool isActive = false;
                                const bool hasKeyboardFocus = true; // This ensure we draw the selection text over the label.
                                const bool drawAsComposition = false;
                                Color selectionColor = GUI.skin.settings.selectionColor;

                                errorModeStyle.DrawWithTextSelection(textRect, tempContent, isActive, hasKeyboardFocus, startIndex, endIndex, drawAsComposition, selectionColor);
                            }
                        }

                        if (collapsed)
                        {
                            Rect badgeRect = el.position;
                            tempContent.text = LogEntries.GetEntryCount(el.row)
                                .ToString(CultureInfo.InvariantCulture);
                            Vector2 badgeSize = Constants.CountBadge.CalcSize(tempContent);

                            if (Constants.CountBadge.fixedHeight > 0)
                                badgeSize.y = Constants.CountBadge.fixedHeight;
                            badgeRect.xMin = badgeRect.xMax - badgeSize.x;
                            badgeRect.yMin += ((badgeRect.yMax - badgeRect.yMin) - badgeSize.y) * 0.5f;
                            badgeRect.x -= 5f;
                            GUI.Label(badgeRect, tempContent, Constants.CountBadge);
                        }
                    }
                }

                if (selectedRow != -1)
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
                    if (m_ListView.selectionChanged || !m_ActiveText.Equals(entry.message))
                    {
                        SetActiveEntry(entry);
                    }

                    // If copy, get the messages from selected rows
                    if (e.type == EventType.ExecuteCommand && e.commandName == EventCommandNames.Copy && m_ListView.selectedItems != null)
                    {
                        m_CopyString.Clear();
                        for (int rowIndex = 0; rowIndex < m_ListView.selectedItems.Length; rowIndex++)
                        {
                            if (m_ListView.selectedItems[rowIndex])
                            {
                                LogEntries.GetEntryInternal(rowIndex, entry);
                                m_CopyString.AppendLine(entry.message);
                            }
                        }
                    }
                }
                // Open entry using return key
                if ((GUIUtility.keyboardControl == m_ListView.ID) && (e.type == EventType.KeyDown) &&
                    (e.keyCode == KeyCode.Return) && (m_ListView.row != 0))
                {
                    selectedRow = m_ListView.row;
                    openSelectedItem = true;
                }

                if (e.type != EventType.Layout && ListViewGUI.ilvState.rectHeight != 1)
                    ms_LVHeight = ListViewGUI.ilvState.rectHeight;

                if (openSelectedItem)
                {
                    rowDoubleClicked = selectedRow;
                    e.Use();
                }
            }

            // Prevent dead locking in EditorMonoConsole by delaying callbacks (which can log to the console) until after LogEntries.EndGettingEntries() has been
            // called (this releases the mutex in EditorMonoConsole so logging again is allowed). Fix for case 1081060.
            if (rowDoubleClicked != -1)
                LogEntries.RowGotDoubleClicked(rowDoubleClicked);


            // Display active text (We want word wrapped text with a vertical scrollbar)
            m_TextScroll = GUILayout.BeginScrollView(m_TextScroll, Constants.Box);

            string stackWithHyperlinks = StacktraceWithHyperlinks(m_ActiveText);
            float height = Constants.MessageStyle.CalcHeight(GUIContent.Temp(stackWithHyperlinks), position.width);
            EditorGUILayout.SelectableLabel(stackWithHyperlinks, Constants.MessageStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(height + 10));

            GUILayout.EndScrollView();

            SplitterGUILayout.EndVerticalSplit();

            // Copy & Paste selected item
            if ((e.type == EventType.ValidateCommand || e.type == EventType.ExecuteCommand) && e.commandName == EventCommandNames.Copy && m_CopyString != null)
            {
                if (e.type == EventType.ExecuteCommand)
                    EditorGUIUtility.systemCopyBuffer = m_CopyString.ToString();
                e.Use();
            }
        }

        private void SearchField(Event e)
        {
            string searchBarName = "SearchFilter";
            if (e.commandName == EventCommandNames.Find)
            {
                if (e.type == EventType.ExecuteCommand)
                {
                    EditorGUI.FocusTextInControl(searchBarName);
                }

                if (e.type != EventType.Layout)
                    e.Use();
            }

            string searchText = m_SearchText;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    searchText = string.Empty;
                    GUIUtility.keyboardControl = m_ListView.ID;
                    Repaint();
                }
                else if ((e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow) &&
                         GUI.GetNameOfFocusedControl() == searchBarName)
                {
                    GUIUtility.keyboardControl = m_ListView.ID;
                }
            }

            GUI.SetNextControlName(searchBarName);
            Rect rect = GUILayoutUtility.GetRect(0, EditorGUILayout.kLabelFloatMaxW * 1.5f, EditorGUI.kSingleLineHeight,
                EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchField, GUILayout.MinWidth(60),
                GUILayout.MaxWidth(300));
            var filteringText = EditorGUI.ToolbarSearchField(rect, searchText, false);
            if (m_SearchText != filteringText)
            {
                SetFilter(filteringText);
            }
        }

        internal static string StacktraceWithHyperlinks(string stacktraceText)
        {
            StringBuilder textWithHyperlinks = new StringBuilder();
            var lines = stacktraceText.Split(new string[] {"\n"}, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; ++i)
            {
                string textBeforeFilePath = ") (at ";
                int filePathIndex = lines[i].IndexOf(textBeforeFilePath, StringComparison.Ordinal);
                if (filePathIndex > 0)
                {
                    filePathIndex += textBeforeFilePath.Length;
                    if (lines[i][filePathIndex] != '<') // sometimes no url is given, just an id between <>, we can't do an hyperlink
                    {
                        string filePathPart = lines[i].Substring(filePathIndex);
                        int lineIndex = filePathPart.LastIndexOf(":", StringComparison.Ordinal); // LastIndex because the url can contain ':' ex:"C:"
                        if (lineIndex > 0)
                        {
                            int endLineIndex = filePathPart.LastIndexOf(")", StringComparison.Ordinal); // LastIndex because files or folder in the url can contain ')'
                            if (endLineIndex > 0)
                            {
                                string lineString =
                                    filePathPart.Substring(lineIndex + 1, (endLineIndex) - (lineIndex + 1));
                                string filePath = filePathPart.Substring(0, lineIndex);

                                textWithHyperlinks.Append(lines[i].Substring(0, filePathIndex));
                                textWithHyperlinks.Append("<a href=\"" + filePath + "\"" + " line=\"" + lineString + "\">");
                                textWithHyperlinks.Append(filePath + ":" + lineString);
                                textWithHyperlinks.Append("</a>)\n");

                                continue; // continue to evade the default case
                            }
                        }
                    }
                }
                // default case if no hyperlink : we just write the line
                textWithHyperlinks.Append(lines[i] + "\n");
            }
            // Remove the last \n
            if (textWithHyperlinks.Length > 0) // textWithHyperlinks always ends with \n if it is not empty
                textWithHyperlinks.Remove(textWithHyperlinks.Length - 1, 1);

            return textWithHyperlinks.ToString();
        }

        private void EditorGUI_HyperLinkClicked(object sender, EventArgs e)
        {
            EditorGUILayout.HyperLinkClickedEventArgs args = (EditorGUILayout.HyperLinkClickedEventArgs)e;

            string filePath;
            string lineString;
            if (!args.hyperlinkInfos.TryGetValue("href", out filePath) ||
                !args.hyperlinkInfos.TryGetValue("line", out lineString))
                return;

            int line = Int32.Parse(lineString);
            var projectFilePath = filePath.Replace('\\', '/');

            if (!String.IsNullOrEmpty(projectFilePath))
                LogEntries.OpenFileOnSpecificLineAndColumn(filePath, line, -1);
        }

        [UsedImplicitly]
        public static bool GetConsoleErrorPause()
        {
            return HasFlag(ConsoleFlags.ErrorPause);
        }

        [UsedImplicitly]
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
            menu.AddItem(EditorGUIUtility.TrTextContent("Open Player Log"), false, UnityEditorInternal.InternalEditorUtility.OpenPlayerConsole);
            menu.AddItem(EditorGUIUtility.TrTextContent("Open Editor Log"), false, UnityEditorInternal.InternalEditorUtility.OpenEditorConsole);

            menu.AddItem(EditorGUIUtility.TrTextContent("Show Timestamp"), HasFlag(ConsoleFlags.ShowTimestamp), SetTimestamp);

            if (m_DevBuild)
            {
                menu.AddItem(Constants.StopForAssert, HasFlag(ConsoleFlags.StopForAssert),
                    () => { SetFlag(ConsoleFlags.StopForAssert, !HasFlag(ConsoleFlags.StopForAssert)); });
                menu.AddItem(Constants.StopForError, HasFlag(ConsoleFlags.StopForError),
                    () => { SetFlag(ConsoleFlags.StopForError, !HasFlag(ConsoleFlags.StopForError)); });
            }

            for (int i = 1; i <= 10; ++i)
            {
                var lineString = i == 1 ? "Line" : "Lines";
                menu.AddItem(new GUIContent(string.Format("Log Entry/{0} {1}", i, lineString)), i == Constants.LogStyleLineCount, SetLogLineCount, i);
            }

            AddStackTraceLoggingMenu(menu);
        }

        private void SetTimestamp()
        {
            SetFlag(ConsoleFlags.ShowTimestamp, !HasFlag(ConsoleFlags.ShowTimestamp));
        }

        private void SetLogLineCount(object obj)
        {
            int count = (int)obj;
            EditorPrefs.SetInt("ConsoleWindowLogLineCount", count);
            Constants.LogStyleLineCount = count;

            UpdateListView();
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

                    menu.AddItem(EditorGUIUtility.TrTextContent("Stack Trace Logging/" + logType + "/" + stackTraceLogType), PlayerSettings.GetStackTraceLogType(logType) == stackTraceLogType,
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
                menu.AddItem(EditorGUIUtility.TrTextContent("Stack Trace Logging/All/" + stackTraceLogType), (StackTraceLogType)stackTraceLogTypeForAll == stackTraceLogType,
                    ToggleLogStackTracesForAll, stackTraceLogType);
            }
        }

        private void SetFilter(string filteringText)
        {
            if (filteringText == null)
            {
                m_SearchText = "";
                LogEntries.SetFilteringText("");
            }
            else
            {
                m_SearchText = filteringText;
                LogEntries.SetFilteringText(filteringText); // Reset the active entry when we change the filtering text
            }
            SetActiveEntry(null);
        }

        [UsedImplicitly] private static event EntryDoubleClickedDelegate entryWithManagedCallbackDoubleClicked;

        [UsedImplicitly, RequiredByNativeCode]
        private static void SendEntryDoubleClicked(LogEntry entry)
        {
            entryWithManagedCallbackDoubleClicked?.Invoke(entry);
        }

        [UsedImplicitly] // This method is used by the Visual Scripting project. Please do not delete. Contact @husseink for more information.
        internal void AddMessageWithDoubleClickCallback(string message, string file, int mode, int instanceID)
        {
            var outputEntry = new LogEntry {message = message, file = file, mode = mode, instanceID = instanceID};
            LogEntries.AddMessageWithDoubleClickCallback(outputEntry);
        }
    }

    internal struct GettingLogEntriesScope : IDisposable
    {
        private bool m_Disposed;

        public GettingLogEntriesScope(ListViewState listView)
        {
            m_Disposed = false;
            listView.totalRows = LogEntries.StartGettingEntries();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            LogEntries.EndGettingEntries();
            m_Disposed = true;
        }
    }
}
