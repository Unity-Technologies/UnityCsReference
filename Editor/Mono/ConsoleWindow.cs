// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Networking.PlayerConnection;
using UnityEditor.Networking.PlayerConnection;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using UnityEditor.Profiling;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Console", useTypeNameAsIconName = true)]
    internal class ConsoleWindow : EditorWindow, IHasCustomMenu
    {
        internal delegate void EntryDoubleClickedDelegate(LogEntry entry);
        private static bool s_StripLoggingCallstack;
        private static bool m_UseMonospaceFont;
        private static Font m_MonospaceFont;
        private static int m_DefaultFontSize;
        private static List<MethodInfo> s_MethodsToHideInCallstack = null;
        private static Dictionary<MethodInfo, Regex> s_GenericMethodSignatureRegex = null;
        private static bool m_ShouldSkipClearingConsoleAfterBuild = false;

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
            public static GUIStyle ConsoleSearchNoResult;

            public static readonly GUIContent Clear = EditorGUIUtility.TrTextContent("Clear", "Clear console entries");
            public static readonly GUIContent ClearOnPlay = EditorGUIUtility.TrTextContent("Clear on Play");
            public static readonly GUIContent ClearOnBuild = EditorGUIUtility.TrTextContent("Clear on Build");
            public static readonly GUIContent ClearOnRecompile = EditorGUIUtility.TrTextContent("Clear on Recompile");
            public static readonly GUIContent Collapse = EditorGUIUtility.TrTextContent("Collapse", "Collapse identical entries");
            public static readonly GUIContent ErrorPause = EditorGUIUtility.TrTextContent("Error Pause", "Pause Play Mode on error");
            public static readonly GUIContent StopForAssert = EditorGUIUtility.TrTextContent("Stop for Assert");
            public static readonly GUIContent StopForError = EditorGUIUtility.TrTextContent("Stop for Error");
            public static readonly GUIContent UseMonospaceFont = EditorGUIUtility.TrTextContent("Use Monospace font");
            public static readonly GUIContent StripLoggingCallstack = EditorGUIUtility.TrTextContent("Strip logging callstack");

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

                ConsoleSearchNoResult = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    name = "console-search-no-result",
                    fontSize = 20,
                    wordWrap = true
                };

                // If the console window isn't open OnEnable() won't trigger so it will end up with 0 lines,
                // so we always make sure we read it up when we initialize here.
                LogStyleLineCount = EditorPrefs.GetInt("ConsoleWindowLogLineCount", 2);

                m_MonospaceFont = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font;
                m_UseMonospaceFont = HasFlag(ConsoleFlags.UseMonospaceFont);
                s_StripLoggingCallstack = HasFlag(ConsoleFlags.StripLoggingCallstack);
                m_DefaultFontSize = LogStyle.fontSize;
                SetFont();
                (s_MethodsToHideInCallstack, s_GenericMethodSignatureRegex) = InitializeHideInCallstackMethodsCache();
            }

            internal static void UpdateLogStyleFixedHeights()
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

        string m_ConsoleSearchNoResultMsg = "";

        ListViewState m_ListView;
        string m_ActiveText = "";
        StringBuilder m_CopyString;
        bool m_DevBuild;
        int m_CallstackTextStart = 0;
        private Mode m_ActiveMode = Mode.None;

        Vector2 m_TextScroll = Vector2.zero;

        int m_LastActiveEntryIndex = -1;
        [NonSerialized]
        int m_IndexHintCache;
        bool m_RestoreLatestSelection;

        //Make sure the minimum height of the panels can accomodate the cpmplete scroll bar icons
        SplitterState spl = SplitterState.FromRelative(new float[] {70, 30}, new float[] {60, 60}, null);

        static bool ms_LoadedIcons = false;
        static internal Texture2D iconInfo, iconWarn, iconError;
        static internal Texture2D iconInfoSmall, iconWarnSmall, iconErrorSmall;
        static internal Texture2D iconInfoMono, iconWarnMono, iconErrorMono;

        int ms_LVHeight = 0;

        internal class ConsoleAttachToPlayerState : GeneralConnectionState
        {
            public ConsoleAttachToPlayerState(EditorWindow parentWindow, Action<string, EditorConnectionTarget?> connectedCallback = null) : base(parentWindow, connectedCallback)
            {
                // This is needed to force initialize the instance and the state so that messages from players are received and printed to the console (if that is the serialized state)
                // on creation of the ConsoleWindow UI instead of when the uer first clicks on the dropdown, and triggers AddItemsToTree.
                PlayerConnectionLogReceiver.instance.State = PlayerConnectionLogReceiver.instance.State;
            }

            internal bool IsConnected()
            {
                return PlayerConnectionLogReceiver.instance.State != PlayerConnectionLogReceiver.ConnectionState.Disconnected;
            }

            internal void PlayerLoggingOptionSelected()
            {
                PlayerConnectionLogReceiver.instance.State = IsConnected() ? PlayerConnectionLogReceiver.ConnectionState.Disconnected : PlayerConnectionLogReceiver.ConnectionState.CleanLog;
            }

            internal bool IsLoggingFullLog()
            {
                return PlayerConnectionLogReceiver.instance.State == PlayerConnectionLogReceiver.ConnectionState.FullLog;
            }

            internal void FullLogOptionSelected()
            {
                PlayerConnectionLogReceiver.instance.State = IsLoggingFullLog() ? PlayerConnectionLogReceiver.ConnectionState.CleanLog : PlayerConnectionLogReceiver.ConnectionState.FullLog;
            }

            public override void AddItemsToTree(ConnectionTreeViewWindow view, Rect position)
            {
                view.SetLoggingOptions(this);
                base.AddItemsToTree(view, position);
            }
        }

        IConnectionState m_ConsoleAttachToPlayerState;

        [Flags]
        internal enum Mode
        {
            None = 0,
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
            ClearOnRecompile = 1 << 12,
            UseMonospaceFont = 1 << 13,
            StripLoggingCallstack = 1 << 14,
        }

        static ConsoleWindow ms_ConsoleWindow = null;
        private string m_SearchText;

        static readonly int k_HasSpaceForExtraButtonsCutoff = 420;

        public static void ShowConsoleWindow(bool immediate)
        {
            if (!ms_ConsoleWindow)
            {
                ms_ConsoleWindow = ScriptableObject.CreateInstance<ConsoleWindow>();
                if (UnityEditor.MPE.ProcessService.level == MPE.ProcessLevel.Main)
                    ms_ConsoleWindow.Show(immediate);
                else
                    ms_ConsoleWindow.ShowModalUtility();
                if (ms_ConsoleWindow)
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
            if (!ms_ConsoleWindow)
                return;
            ms_ConsoleWindow.Repaint();
        }

        public ConsoleWindow()
        {
            position = new Rect(200, 200, 800, 400);
            m_ListView = new ListViewState(0, 0);
            m_CopyString = new StringBuilder();
            m_SearchText = string.Empty;
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
                m_ActiveMode = (Mode)entry.mode;
                entry.callstackTextStartUTF8 = entry.message.Length;
                m_CallstackTextStart = entry.callstackTextStartUTF16;
                var entryRow = LogEntries.GetEntryRowIndex(entry.globalLineIndex, m_IndexHintCache);
                m_IndexHintCache = entryRow;
            }
            else
            {
                m_CallstackTextStart = 0;
                m_ActiveText = string.Empty;
                m_ListView.row = -1;
                m_CopyString.Clear();
                m_ActiveMode = Mode.None;
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
            return position.width > k_HasSpaceForExtraButtonsCutoff;
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
                var clearOnRecompile = HasFlag(ConsoleFlags.ClearOnRecompile);

                GenericMenu menu = new GenericMenu();
                menu.AddItem(Constants.ClearOnPlay, clearOnPlay, () => { SetFlag(ConsoleFlags.ClearOnPlay, !clearOnPlay); });
                menu.AddItem(Constants.ClearOnBuild, clearOnBuild, () => { SetFlag(ConsoleFlags.ClearOnBuild, !clearOnBuild); });
                menu.AddItem(Constants.ClearOnRecompile, clearOnRecompile, () => { SetFlag(ConsoleFlags.ClearOnRecompile, !clearOnRecompile); });
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
            bool showSearchNoResultMessage = currCount == 0 && !String.IsNullOrEmpty(m_SearchText);

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
                PlayerConnectionGUILayout.ConnectionTargetSelectionDropdown(m_ConsoleAttachToPlayerState, EditorStyles.toolbarDropDown, (int)(position.width - k_HasSpaceForExtraButtonsCutoff) + 80 );
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
            {
                SetActiveEntry(null);
                m_LastActiveEntryIndex = -1;
            }

            SetFlag(ConsoleFlags.LogLevelLog, setLogFlag);
            SetFlag(ConsoleFlags.LogLevelWarning, setWarningFlag);
            SetFlag(ConsoleFlags.LogLevelError, setErrorFlag);

            GUILayout.EndHorizontal();

            if (showSearchNoResultMessage)
            {
                Rect r = new Rect(0, EditorGUI.kSingleLineHeight, ms_ConsoleWindow.position.width, ms_ConsoleWindow.position.height - EditorGUI.kSingleLineHeight);
                GUI.Box(r, m_ConsoleSearchNoResultMsg, Constants.ConsoleSearchNoResult);
            }
            else
            {
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
                    float scrollPosY = m_ListView.scrollPos.y;

                    foreach (ListViewElement el in ListViewGUI.ListView(m_ListView,
                        ListViewOptions.wantsRowMultiSelection, Constants.Box))
                    {
                        // Destroy latest restore entry if needed
                        if (e.type == EventType.ScrollWheel || e.type == EventType.Used)
                            DestroyLatestRestoreEntry();

                        // Make sure that scrollPos.y is always up to date after restoring last entry
                        if (m_RestoreLatestSelection)
                        {
                            m_ListView.scrollPos.y = scrollPosY;
                        }

                        if (e.type == EventType.MouseDown && e.button == 0 && el.position.Contains(e.mousePosition))
                        {
                            selectedRow = m_ListView.row;
                            DestroyLatestRestoreEntry();
                            LogEntry entry = new LogEntry();
                            LogEntries.GetEntryInternal(el.row, entry);
                            if (entry.instanceID != 0 && e.clickCount != 2)
                                EditorGUIUtility.PingObject(entry.instanceID);
                            if (e.clickCount == 2)
                                openSelectedItem = true;
                        }
                        else if (e.type == EventType.Repaint)
                        {
                            int mode = 0;
                            string text = null;
                            LogEntries.GetLinesAndModeFromEntryInternal(el.row, Constants.LogStyleLineCount, ref mode,
                                ref text);
                            bool entryIsSelected = m_ListView.selectedItems != null &&
                                el.row < m_ListView.selectedItems.Length &&
                                m_ListView.selectedItems[el.row];

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
                            else if (text != null)
                            {
                                //the whole text contains the searchtext, we have to know where it is
                                int startIndex = text.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase);
                                if (startIndex == -1
                                ) // the searchtext is not in the visible text, we don't show the selection
                                    errorModeStyle.Draw(textRect, tempContent, id, m_ListView.row == el.row);
                                else // the searchtext is visible, we show the selection
                                {
                                    int endIndex = startIndex + m_SearchText.Length;

                                    const bool isActive = false;
                                    const bool
                                        hasKeyboardFocus =
                                        true;     // This ensure we draw the selection text over the label.
                                    const bool drawAsComposition = false;
                                    Color selectionColor = GUI.skin.settings.selectionColor;

                                    errorModeStyle.DrawWithTextSelection(textRect, tempContent, isActive,
                                        hasKeyboardFocus, startIndex, endIndex, drawAsComposition, selectionColor);
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
                        {
                            SetActiveEntry(null);
                            DestroyLatestRestoreEntry();
                        }
                    }
                    else
                    {
                        LogEntry entry = new LogEntry();
                        LogEntries.GetEntryInternal(m_ListView.row, entry);
                        SetActiveEntry(entry);
                        m_LastActiveEntryIndex = entry.globalLineIndex;


                        // see if selected entry changed. if so - clear additional info
                        LogEntries.GetEntryInternal(m_ListView.row, entry);
                        if (m_ListView.selectionChanged || !m_ActiveText.Equals(entry.message))
                        {
                            SetActiveEntry(entry);
                            m_LastActiveEntryIndex = entry.globalLineIndex;
                            activeEntryChanged?.Invoke();
                        }


                        // If copy, get the messages from selected rows
                        if (e.type == EventType.ExecuteCommand && e.commandName == EventCommandNames.Copy &&
                            m_ListView.selectedItems != null)
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

                string stackWithHyperlinks = StacktraceWithHyperlinks(m_ActiveText, m_CallstackTextStart, s_StripLoggingCallstack, m_ActiveMode);
                float height = Constants.MessageStyle.CalcHeight(GUIContent.Temp(stackWithHyperlinks), position.width);
                EditorGUILayout.SelectableLabel(stackWithHyperlinks, Constants.MessageStyle,
                    GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(height + 10));

                GUILayout.EndScrollView();

                SplitterGUILayout.EndVerticalSplit();
            }

            // Copy & Paste selected item
            if ((e.type == EventType.ValidateCommand || e.type == EventType.ExecuteCommand) && e.commandName == EventCommandNames.Copy && m_CopyString != null)
            {
                if (e.type == EventType.ExecuteCommand)
                    EditorGUIUtility.systemCopyBuffer = m_CopyString.ToString();
                e.Use();
            }

            if (!ms_ConsoleWindow)
                ms_ConsoleWindow = this;
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
                    if (!String.IsNullOrEmpty(m_SearchText))
                        RestoreLastActiveEntry();
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

        internal static string StacktraceWithHyperlinks(string stacktraceText, int callstackTextStart, bool shouldStripCallstack, Mode mode)
        {
            StringBuilder textWithHyperlinks = new StringBuilder();
            textWithHyperlinks.Append(stacktraceText.Substring(0, callstackTextStart));
            var lines = stacktraceText.Substring(callstackTextStart).Split(new string[] { "\n" }, StringSplitOptions.None);

            if (shouldStripCallstack)
                lines = StripCallstack(mode, s_GenericMethodSignatureRegex, s_MethodsToHideInCallstack, lines);

            for (int i = 0; i < lines.Length; ++i)
            {
                string textBeforeFilePath = " (at ";
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

        internal static string GetCallstackFormattedSignatureFromGenericMethod(Dictionary<MethodInfo, Regex> methodSignatureRegex, MethodInfo method, string line)
        {
            if (string.IsNullOrEmpty(line) || method == null || methodSignatureRegex == null)
                return null;

            var classType = method.DeclaringType;
            if (classType == null)
                return null;

            if (!methodSignatureRegex.TryGetValue(method, out Regex regex))
                return null;

            if (regex == null)
                return null;

            var match = regex.Match(line);
            if (!match.Success)
                return null;

            var parameterStrings = match.Groups[match.Groups.Count - 1].Value.Split(',');
            var methodParameters = method.GetParameters();
            if (parameterStrings == null || parameterStrings.Length != methodParameters.Length)
                return null;

            var dict = new Dictionary<string, string>();
            for (int i = 0; i < methodParameters.Length; i++)
            {
                var param = methodParameters[i].ParameterType.ToString();
                parameterStrings[i] = parameterStrings[i].Trim(' ');
                if (!dict.ContainsKey(param))
                {
                    dict.Add(param, parameterStrings[i]);
                    continue;
                }

                if (methodParameters[i].ParameterType.IsByRef)
                {
                    var trimmedParam = param.Trim('&');
                    if (!dict.ContainsKey(trimmedParam))
                    {
                        dict.Add(trimmedParam, parameterStrings[i].Trim('&'));
                        continue;
                    }
                }

                var nextParam = dict[param];
                if (!nextParam.Equals(parameterStrings[i], StringComparison.Ordinal))
                    return null;
            }

            return match.Value;
        }

        internal static string GetCallstackFormattedScriptingExceptionSignature(MethodInfo method)
        {
            if (method == null)
                return null;

            var classType = method.DeclaringType;
            if (classType == null)
                return null;

            var sb = new StringBuilder(255);
            var ns = classType.Namespace;
            if (!string.IsNullOrEmpty(ns))
            {
                sb.Append(ns);
                sb.Append(".");
            }

            sb.Append(classType.Name);
            sb.Append(":");
            sb.Append(method.Name);
            sb.Append("(");

            var pi = method.GetParameters();
            if (pi.Length > 0)
                sb.Append(pi[0].ParameterType.Name);

            for (int i = 1; i < pi.Length; i++)
            {
                sb.Append(", ");
                sb.Append(pi[i].ParameterType.Name);
            }

            sb.Append(")");
            return sb.ToString();
        }

        internal static string[] StripCallstack(Mode mode, Dictionary<MethodInfo, Regex> methodSignatureRegex, List<MethodInfo> methodsToHideInCallstack, string[] lines)
        {
            if (methodsToHideInCallstack == null || methodSignatureRegex == null || lines == null)
                return lines;

            var strippedLines = lines.ToList();
            var isException = HasMode((int)mode, Mode.ScriptingException);
            strippedLines.RemoveAll(line =>
            {
                foreach (var method in methodsToHideInCallstack)
                {
                    if (!line.Contains(method.Name, StringComparison.Ordinal))
                        continue;

                    if (method.IsGenericMethod)
                    {
                        var genericMethodSignature = GetCallstackFormattedSignatureFromGenericMethod(methodSignatureRegex, method, line);
                        if (genericMethodSignature == null)
                            continue;

                        return true;
                    }

                    var logMethodSignature = LogEntries.GetCallstackFormattedSignatureInternal(method);
                    if (logMethodSignature != null && line.Contains(logMethodSignature, StringComparison.Ordinal))
                        return true;

                    if (isException)
                    {
                        var exceptionMethodSignature = GetCallstackFormattedScriptingExceptionSignature(method);
                        if (exceptionMethodSignature != null && line.Contains(exceptionMethodSignature, StringComparison.Ordinal))
                            return true;
                    }
                }

                return false;
            });

            return strippedLines.ToArray();
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

            menu.AddItem(Constants.UseMonospaceFont, m_UseMonospaceFont, OnFontButtonValueChange);
            menu.AddItem(Constants.StripLoggingCallstack, s_StripLoggingCallstack, OnStripLoggingCallstackButtonValueChange);

            AddStackTraceLoggingMenu(menu);
        }

        internal ListViewState GetListViewState() { return m_ListView; }

        private static void OnFontButtonValueChange()
        {
            m_UseMonospaceFont = !m_UseMonospaceFont;
            SetFlag(ConsoleFlags.UseMonospaceFont, m_UseMonospaceFont);
            SetFont();
        }

        private static void SetFont()
        {
            var styles = new[]
            {
                Constants.LogStyle,
                Constants.LogSmallStyle,
                Constants.WarningStyle,
                Constants.WarningSmallStyle,
                Constants.ErrorStyle,
                Constants.ErrorSmallStyle,
                Constants.MessageStyle,
            };

            Font font = m_UseMonospaceFont ? m_MonospaceFont : null;

            foreach (var style in styles)
            {
                style.font = font;
                style.fontSize = m_DefaultFontSize;
            }

            // Make sure to update the fixed height so the entries do not get cropped incorrectly.
            Constants.UpdateLogStyleFixedHeights();
        }

        private static void OnStripLoggingCallstackButtonValueChange()
        {
            s_StripLoggingCallstack = !s_StripLoggingCallstack;
            SetFlag(ConsoleFlags.StripLoggingCallstack, s_StripLoggingCallstack);
        }

        internal static (List<MethodInfo>, Dictionary<MethodInfo, Regex>) InitializeHideInCallstackMethodsCache()
        {
            var methods = TypeCache.GetMethodsWithAttribute<HideInCallstackAttribute>();
            if (methods.Count == 0)
                return (null, null);

            var methodsToHideInCallstack = new List<MethodInfo>();
            var genericMethodSignatureRegexes = new Dictionary<MethodInfo, Regex>();
            foreach (var method in methods)
            {
                methodsToHideInCallstack.Add(method);
                if (method.IsGenericMethod)
                {
                    var classType = method.DeclaringType;
                    if (classType == null)
                        continue;

                    var ns = classType.Namespace;
                    var pattern = $"{(string.IsNullOrEmpty(ns) ? "" : $@"({ns})\.")}(" + classType.Name +
                                  @")\:(" + method.Name + @")([^\(]*)\s*\(([^\)]+)\)";
                    var regex = new Regex(pattern, RegexOptions.Compiled);
                    if (!genericMethodSignatureRegexes.TryAdd(method, regex))
                        continue;
                }
            }

            return (methodsToHideInCallstack, genericMethodSignatureRegexes);
        }

        internal static void ClearConsoleOnRecompile()
        {
            //After building a player, there's a recompilation happening as well
            //And in order not to lose the log entries, we need to skip clearing the console one time
            if (BuildPipeline.isBuildingPlayer)
            {
                m_ShouldSkipClearingConsoleAfterBuild = true;
            }

            // During build process, there are multiple compilation events that can trigger console clearing
            // We don't want to lose the log entries during build process so we re-enable the flag only when editor is not building
            // For clearing on build we have a separate option
            if (HasFlag(ConsoleFlags.ClearOnRecompile) && !BuildPipeline.isBuildingPlayer)
            {
                if (!m_ShouldSkipClearingConsoleAfterBuild)
                {
                    LogEntries.Clear();
                }
                else
                {
                    m_ShouldSkipClearingConsoleAfterBuild = false;
                }
            }
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
            m_RestoreLatestSelection = String.IsNullOrEmpty(filteringText) && !String.IsNullOrEmpty(m_SearchText) && m_LastActiveEntryIndex != -1;
            if (filteringText == null)
            {
                m_SearchText = "";
                LogEntries.SetFilteringText("");
            }
            else
            {
                m_ConsoleSearchNoResultMsg = $"No results for \"{filteringText}\"";
                m_SearchText = filteringText;
                LogEntries.SetFilteringText(filteringText); // Reset the active entry when we change the filtering text
            }

            if (m_RestoreLatestSelection)
            {
                RestoreLastActiveEntry();
            }
            else
            {
                // if we have an active selection before domain reload, we need to restore it. So it shouldn't set active entry to null.
                if (m_LastActiveEntryIndex == -1)
                {
                    SetActiveEntry(null);
                    DestroyLatestRestoreEntry();
                }
            }
        }

        void RestoreLastActiveEntry()
        {
            int rowIndex = LogEntries.GetEntryRowIndex(m_LastActiveEntryIndex);
            if (rowIndex != -1)
            {
                ShowConsoleRow(rowIndex);
                m_ListView.selectedItems = new bool[rowIndex + 1];
                m_ListView.selectedItems[rowIndex] = true;
                m_ListView.scrollPos.y = rowIndex * m_ListView.rowHeight;
            }
            else
            {
                SetActiveEntry(null);
            }
        }

        void DestroyLatestRestoreEntry()
        {
            m_LastActiveEntryIndex = -1;
            m_RestoreLatestSelection = false;
        }

        [UsedImplicitly] private static event EntryDoubleClickedDelegate entryWithManagedCallbackDoubleClicked;

        internal static event Action activeEntryChanged;

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

        [UsedImplicitly]
        internal static void AddMessages(NativeArray<LogEntryStruct> messages)
        {
            unsafe
            {
                LogEntries.AddMessagesImpl(messages.GetUnsafeReadOnlyPtr(), messages.Length);
            }
        }

        [UsedImplicitly]
        internal static void AddMessage(ref LogEntryStruct message)
        {
            unsafe
            {
                fixed (void* ptr = &message)
                {
                    LogEntries.AddMessagesImpl(ptr, 1);
                }
            }
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
