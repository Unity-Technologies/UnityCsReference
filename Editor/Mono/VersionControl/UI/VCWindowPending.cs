// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEditorInternal.VersionControl;

namespace UnityEditor.VersionControl
{
    // Pending change list window which shows all the change lists for the version control system.  This is
    // agnostic of any version control system as long as it supports the VCInterface interface.
    //
    // This window now works in an async manner but the method of update should now be improved to hide
    // the refreshing
    [EditorWindowTitle(title = "Version Control", icon = "UnityEditor.VersionControl")]
    internal class WindowPending : EditorWindow
    {
        internal class Styles
        {
            public GUIStyle box = "CN Box";
            public GUIStyle bottomBarBg = "ProjectBrowserBottomBarBg";
            public static readonly GUIContent connectingLabel = EditorGUIUtility.TrTextContent("CONNECTING...");
            public static readonly GUIContent offlineLabel = EditorGUIUtility.TrTextContent("OFFLINE");
            public static readonly GUIContent workOfflineLabel = EditorGUIUtility.TrTextContent("WORK OFFLINE is enabled in Version Control Settings. Unity will behave as if version control is disabled.");
            public static readonly GUIContent disabledLabel = EditorGUIUtility.TrTextContent("Disabled");
            public static readonly GUIContent editorSettingsLabel = EditorGUIUtility.TrTextContent("Version Control Settings");
        }
        static Styles s_Styles = null;

        static Texture2D changeIcon = null;
        static Texture2D syncIcon = null;
        static Texture2D refreshIcon = null;
        GUIStyle header;
        [SerializeField] ListControl pendingList;
        [SerializeField] ListControl incomingList;

        bool m_ShowIncoming = false;
        bool m_ShowIncomingPrevious = true;
        const float k_MinWindowHeight = 100f;
        const float k_ResizerHeight =  17f;
        const float k_MinIncomingAreaHeight = 50f;
        const float k_BottomBarHeight = 21f;
        const float k_MinSearchFieldWidth = 50f;
        const float k_MaxSearchFieldWidth = 240f;
        float s_ToolbarButtonsWidth = 0f;
        float s_SettingsButtonWidth = 0f;
        float s_DeleteChangesetsButtonWidth = 0f;
        string m_SearchText = string.Empty;

        DateTime lastRefresh = new DateTime(0);
        private int refreshInterval = 1000; // this is in MS
        private bool scheduleRefresh = false;

        // Workaround to reload vcs info upon domain reload. TODO: Fix VersionControl.ListControl to get rid of this
        static bool s_DidReload = false; // defaults to false after domain reload

        void InitStyles()
        {
            if (s_Styles == null)
            {
                s_Styles = new Styles();
            }
        }

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();

            if (pendingList == null)
                pendingList = new ListControl();

            pendingList.ExpandEvent += OnExpand;
            pendingList.DragEvent += OnDrop;
            pendingList.MenuDefault = "CONTEXT/Pending";
            pendingList.MenuFolder = "CONTEXT/Change";
            pendingList.DragAcceptOnly = true;

            if (incomingList == null)
                incomingList = new ListControl();

            incomingList.ExpandEvent += OnExpandIncoming;
            UpdateWindow();
        }

        // Watch for selections from another window
        public void OnSelectionChange()
        {
            if (!hasFocus)
            {
                pendingList.Sync();
                Repaint();
            }
        }

        // Handle drag events from the list
        void OnDrop(ChangeSet targetItem)
        {
            AssetList list = pendingList.SelectedAssets;
            Task moveTask = Provider.ChangeSetMove(list, targetItem);
            moveTask.SetCompletionAction(CompletionAction.UpdatePendingWindow);
        }

        static public void ExpandLatestChangeSet()
        {
            var wins = Resources.FindObjectsOfTypeAll(typeof(WindowPending)) as WindowPending[];

            foreach (WindowPending win in wins)
            {
                win.pendingList.ExpandLastItem();
            }
        }

        // Handle list elements being expanded
        void OnExpand(ChangeSet change, ListItem item)
        {
            if (!Provider.isActive)
                return;

            Task task = Provider.ChangeSetStatus(change);
            task.userIdentifier = item.Identifier;
            task.SetCompletionAction(CompletionAction.OnChangeContentsPendingWindow);

            if (!item.HasChildren)
            {
                Asset asset = new Asset("Updating...");
                ListItem changeItem = pendingList.Add(item, asset.prettyPath, asset);
                changeItem.Dummy = true;
                pendingList.Refresh(false);  //true here would cause recursion
                pendingList.Filter = m_SearchText;
                Repaint();
            }
        }

        void OnExpandIncoming(ChangeSet change, ListItem item)
        {
            if (!Provider.isActive)
                return;

            Task task = Provider.IncomingChangeSetAssets(change);

            task.userIdentifier = item.Identifier;
            task.SetCompletionAction(CompletionAction.OnChangeContentsPendingWindow);
            if (!item.HasChildren)
            {
                Asset asset = new Asset("Updating...");
                ListItem changeItem = incomingList.Add(item, asset.prettyPath, asset);
                changeItem.Dummy = true;
                incomingList.Refresh(false);  //true here would cause recursion
                incomingList.Filter = m_SearchText;
                Repaint();
            }
        }

        // called to update the status
        void UpdateWindow()
        {
            if (!Provider.isActive)
            {
                pendingList.Clear();
                Repaint();
                return;
            }

            if (TimeElapsed() > refreshInterval)
            {
                if (Provider.onlineState == OnlineState.Online)
                {
                    Task changesTask = Provider.ChangeSets();
                    changesTask.SetCompletionAction(CompletionAction.OnChangeSetsPendingWindow);

                    Task incomingTask = Provider.Incoming();
                    incomingTask.SetCompletionAction(CompletionAction.OnIncomingPendingWindow);
                }

                lastRefresh = DateTime.Now;
            }
            else
                scheduleRefresh = true;
        }

        void OnGotLatest(Task t)
        {
            UpdateWindow();
        }

        [RequiredByNativeCode]
        static void OnVCTaskCompletedEvent(Task task, CompletionAction completionAction)
        {
            // inspector should re-calculate which VCS buttons it needs to show
            InspectorWindow.ClearVersionControlBarState();

            var wins = Resources.FindObjectsOfTypeAll(typeof(WindowPending)) as WindowPending[];

            foreach (WindowPending win in wins)
            {
                switch (completionAction)
                {
                    case CompletionAction.UpdatePendingWindow: // fallthrough
                    case CompletionAction.OnCheckoutCompleted:
                        win.UpdateWindow();
                        break;
                    case CompletionAction.OnChangeContentsPendingWindow:
                        win.OnChangeContents(task);
                        break;
                    case CompletionAction.OnIncomingPendingWindow:
                        win.OnIncoming(task);
                        break;
                    case CompletionAction.OnChangeSetsPendingWindow:
                        win.OnChangeSets(task);
                        break;
                    case CompletionAction.OnGotLatestPendingWindow:
                        win.OnGotLatest(task);
                        break;
                }
            }

            switch (completionAction)
            {
                case CompletionAction.OnSubmittedChangeWindow:
                    WindowChange.OnSubmitted(task);
                    break;
                case CompletionAction.OnAddedChangeWindow:
                    WindowChange.OnAdded(task);
                    break;
                case CompletionAction.OnCheckoutCompleted:
                    if (EditorUserSettings.showFailedCheckout)
                        WindowCheckoutFailure.OpenIfCheckoutFailed(task.assetList);
                    break;
            }

            task.Dispose();
        }

        public static void OnStatusUpdated()
        {
            UpdateAllWindows();
        }

        [RequiredByNativeCode]
        public static void UpdateAllWindows()
        {
            // inspector should re-calculate which VCS buttons it needs to show
            InspectorWindow.ClearVersionControlBarState();

            var wins = Resources.FindObjectsOfTypeAll(typeof(WindowPending)) as WindowPending[];

            foreach (WindowPending win in wins)
            {
                win.UpdateWindow();
            }
        }

        public static void CloseAllWindows()
        {
            var wins = Resources.FindObjectsOfTypeAll(typeof(WindowPending)) as WindowPending[];
            WindowPending win = wins.Length > 0 ? wins[0] : null;
            if (win != null)
            {
                win.Close();
            }
        }

        // Incoming Change Lists have been updated
        void OnIncoming(Task task)
        {
            CreateStaticResources();
            PopulateListControl(incomingList, task, syncIcon);
        }

        // Change lists have been updated
        void OnChangeSets(Task task)
        {
            CreateStaticResources();
            PopulateListControl(pendingList, task, changeIcon);
        }

        internal string FormatChangeSetDescription(ChangeSet changeSet)
        {
            string formattedDescription;

            string[] description = changeSet.description.Split(new char[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
            formattedDescription = description.Length > 1 ? description[0] + description[1] : description[0];
            formattedDescription = formattedDescription.Replace('\t', ' ');

            switch (VersionControlSettings.mode)
            {
                case "Perforce":
                    return changeSet.id + ": " + formattedDescription;

                default:
                    return changeSet.description;
            }
        }

        internal void PopulateListControl(ListControl list, Task task, Texture2D icon)
        {
            // We try to correct the existing list by removing/adding entries.
            // This way the entries will keep their children while the children are updated
            // and will prevent flicker

            // list.Clear ();

            // Remove from existing list entries not in the incoming one
            ChangeSets css = task.changeSets;

            ListItem it = list.Root.FirstChild;
            while (it != null)
            {
                ChangeSet cs = it.Item as ChangeSet;
                if (css.Find(elm => elm.id == cs.id) == null)
                {
                    // Found an list item that was not in the incoming list.
                    // Lets remove it.
                    ListItem rm = it;
                    it = it.Next;
                    list.Root.Remove(rm);
                }
                else
                {
                    it = it.Next;
                }
            }

            // Update the existing ones with the new content or add new ones
            foreach (ChangeSet change in css)
            {
                ListItem changeItem = list.GetChangeSetItem(change);

                if (changeItem != null)
                {
                    changeItem.Item = change;
                    changeItem.Name = FormatChangeSetDescription(change);
                }
                else
                {
                    changeItem = list.Add(null, FormatChangeSetDescription(change), change);
                }
                changeItem.Exclusive = true;  // Single selection only
                changeItem.CanAccept = true;  // Accept drag and drop
                changeItem.Icon = icon; // changeset icon
            }

            // Refresh here will trigger the expand events to ensure the same change lists
            // are kept open. This will in turn trigger change list contents update requests
            list.Refresh();
            list.Filter = m_SearchText;
            Repaint();
        }

        // Change list contents have been updated
        void OnChangeContents(Task task)
        {
            ListItem pendingItem = pendingList.FindItemWithIdentifier(task.userIdentifier);
            ListItem item = pendingItem == null ? incomingList.FindItemWithIdentifier(task.userIdentifier) : pendingItem;

            if (item == null)
                return;

            ListControl list = pendingItem == null ? incomingList : pendingList;

            item.RemoveAll();

            AssetList assetList = task.assetList;

            assetList.NaturalSort();

            // Add all files to the list
            if (assetList.Count == 0)
            {
                // Can only happen for pendingList since incoming lists are frozen by design.
                ListItem empty = list.Add(item, ListControl.c_emptyChangeListMessage, (Asset)null);
                empty.Dummy = true;
            }
            else
            {
                foreach (Asset a in assetList)
                    list.Add(item, a.prettyPath, a);
            }

            list.Refresh(false); // false means the expanded events are not called
            list.Filter = m_SearchText;
            Repaint();
        }

        private ChangeSets GetEmptyChangeSetsCandidates()
        {
            ListControl l = pendingList;
            ChangeSets set = l.EmptyChangeSets;
            ChangeSets toDelete = new ChangeSets();
            set
                .FindAll(item => item.id != ChangeSet.defaultID)
                .ForEach(delegate(ChangeSet s) { toDelete.Add(s); });
            return toDelete;
        }

        private bool HasEmptyPendingChangesets()
        {
            ChangeSets changeSets = GetEmptyChangeSetsCandidates();
            return Provider.DeleteChangeSetsIsValid(changeSets);
        }

        private void DeleteEmptyPendingChangesets()
        {
            ChangeSets changeSets = GetEmptyChangeSetsCandidates();
            Provider.DeleteChangeSets(changeSets).SetCompletionAction(CompletionAction.UpdatePendingWindow);
        }

        private void SearchField(Event e, ListControl activeList)
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
                    m_SearchText = searchText = string.Empty;
                    activeList.Filter = searchText;

                    GUI.FocusControl(null);
                    activeList.SelectedSet(activeList.Root.NextOpenVisible);
                }
                else if ((e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow || e.keyCode == KeyCode.Return) && GUI.GetNameOfFocusedControl() == searchBarName)
                {
                    GUI.FocusControl(null);
                    activeList.SelectedSet(activeList.Root.NextOpenVisible);
                }
            }

            GUI.SetNextControlName(searchBarName);
            Rect rect = GUILayoutUtility.GetRect(0, EditorGUILayout.kLabelFloatMaxW * 1.5f, EditorGUI.kSingleLineHeight,
                EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchField, GUILayout.MinWidth(k_MinSearchFieldWidth),
                GUILayout.MaxWidth(k_MaxSearchFieldWidth));

            var filteringText = EditorGUI.ToolbarSearchField(rect, searchText, false);
            if (m_SearchText != filteringText)
            {
                m_SearchText = filteringText;
                activeList.SelectedClear();
                activeList.listState.Scroll = 0;
                activeList.Filter = filteringText;
            }
        }

        // Editor window GUI paint
        void OnGUI()
        {
            InitStyles();

            if (!s_DidReload)
            {
                s_DidReload = true;
                UpdateWindow();
            }

            CreateResources();

            float toolBarHeight = EditorStyles.toolbar.fixedHeight;
            bool refresh = false;

            GUILayout.BeginArea(new Rect(0, 0, position.width, toolBarHeight));

            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();

            int incomingChangesetCount = incomingList.Root == null ? 0 : incomingList.Root.ChildCount;
            bool switchToOutgoing = GUILayout.Toggle(!m_ShowIncoming, "Outgoing", EditorStyles.toolbarButton);

            GUIContent cont = GUIContent.Temp("Incoming" + (incomingChangesetCount == 0 ? "" : " (" + incomingChangesetCount + ")"));
            bool switchToIncoming = GUILayout.Toggle(m_ShowIncoming, cont, EditorStyles.toolbarButton);

            m_ShowIncoming = m_ShowIncoming ? !switchToOutgoing : switchToIncoming;

            if (EditorGUI.EndChangeCheck())
                refresh = true;

            GUILayout.FlexibleSpace();

            Event e = Event.current;
            SearchField(e, m_ShowIncoming ? incomingList : pendingList);

            // Global context custom commands goes here
            using (new EditorGUI.DisabledScope(Provider.activeTask != null))
            {
                foreach (CustomCommand c in Provider.customCommands)
                {
                    if (c.context == CommandContext.Global && GUILayout.Button(c.label, EditorStyles.toolbarButton))
                        c.StartTask();
                }
            }

            bool showDeleteEmptyChangesetsButton =
                Mathf.FloorToInt(position.width - s_ToolbarButtonsWidth - k_MinSearchFieldWidth - s_SettingsButtonWidth - s_DeleteChangesetsButtonWidth) > 0 &&
                HasEmptyPendingChangesets();
            if (showDeleteEmptyChangesetsButton && GUILayout.Button("Delete Empty Changesets", EditorStyles.toolbarButton))
            {
                DeleteEmptyPendingChangesets();
            }

            bool showSettingsButton = Mathf.FloorToInt(position.width - s_ToolbarButtonsWidth - k_MinSearchFieldWidth - s_SettingsButtonWidth) > 0;

            if (showSettingsButton && GUILayout.Button(Styles.editorSettingsLabel, EditorStyles.toolbarButton))
            {
                SettingsService.OpenProjectSettings("Project/Version Control");
                EditorWindow.FocusWindowIfItsOpen<InspectorWindow>();
                GUIUtility.ExitGUI();
            }

            bool refreshButtonClicked = GUILayout.Button(refreshIcon, EditorStyles.toolbarButton);
            refresh = refresh || refreshButtonClicked || scheduleRefresh;

            bool repaint = false;

            if (refresh)
            {
                if (refreshButtonClicked)
                {
                    m_SearchText = string.Empty;
                    GUI.FocusControl(null);
                    Provider.InvalidateCache();
                    Provider.UpdateSettings();
                }

                repaint = true;
                scheduleRefresh = false;
                UpdateWindow();
            }

            GUILayout.EndArea();

            Rect rect = new Rect(0, toolBarHeight, position.width, position.height - toolBarHeight - k_BottomBarHeight);

            GUILayout.EndHorizontal();

            if (EditorUserSettings.WorkOffline)
            {
                GUI.color = new Color(0.8f, 0.5f, 0.5f);
                rect.height = toolBarHeight;
                GUILayout.BeginArea(rect);

                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.FlexibleSpace();

                GUILayout.Label(Styles.workOfflineLabel, EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
            // Disabled Window view
            else if (!Provider.isActive)
            {
                Color tmpColor = GUI.color;
                GUI.color = new Color(0.8f, 0.5f, 0.5f);
                rect.height = toolBarHeight;
                GUILayout.BeginArea(rect);

                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.FlexibleSpace();

                if (Provider.enabled)
                {
                    if (Provider.onlineState == OnlineState.Updating)
                    {
                        GUI.color = new Color(0.8f, 0.8f, 0.5f);
                        GUILayout.Label(Styles.connectingLabel, EditorStyles.miniLabel);
                    }
                    else
                        GUILayout.Label(Styles.offlineLabel, EditorStyles.miniLabel);
                }
                else
                    GUILayout.Label(Styles.disabledLabel, EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndArea();

                rect.y += rect.height;
                if (!string.IsNullOrEmpty(Provider.offlineReason))
                    GUI.Label(rect, Provider.offlineReason);

                GUI.color = tmpColor;
                repaint = false;
            }
            else
            {
                if (m_ShowIncoming)
                {
                    repaint |= incomingList.OnGUI(rect, hasFocus);
                }
                else
                {
                    repaint |= pendingList.OnGUI(rect, hasFocus);
                }


                rect.y += rect.height;
                rect.height = k_BottomBarHeight;

                // Draw separation line over the button
                GUI.Label(rect, GUIContent.none, s_Styles.bottomBarBg);

                var content = EditorGUIUtility.TrTextContent("Apply All Incoming Changes");
                var buttonSize = EditorStyles.miniButton.CalcSize(content);
                Rect progressRect = new Rect(rect.x, rect.y - 2, rect.width - buttonSize.x - 5f, rect.height);
                ProgressGUI(progressRect, Provider.activeTask, false);

                if (m_ShowIncoming)
                {
                    // Draw "apply incoming" button
                    var buttonRect = rect;
                    buttonRect.width = buttonSize.x;
                    buttonRect.height = buttonSize.y;
                    buttonRect.y = rect.y + 2f;
                    buttonRect.x = position.width - buttonSize.x - 5f;

                    using (new EditorGUI.DisabledScope(incomingList?.Root?.VisibleChildCount == 0))
                    {
                        if (GUI.Button(buttonRect, content, EditorStyles.miniButton))
                        {
                            Asset root = new Asset("");
                            Task t = Provider.GetLatest(new AssetList() { root });
                            t.SetCompletionAction(CompletionAction.OnGotLatestPendingWindow);
                        }
                    }
                }
            }

            if (m_ShowIncoming != m_ShowIncomingPrevious)
            {
                (m_ShowIncoming ? incomingList : pendingList).Filter = m_SearchText;
                m_ShowIncomingPrevious = m_ShowIncoming;
            }

            if (repaint)
                Repaint();
        }

        [Shortcut("VersionControl/RefreshWindow", typeof(WindowPending), KeyCode.F5, displayName = "Version Control/Refresh Window")]
        static void RefreshWindow(ShortcutArguments args)
        {
            if (GUIUtility.keyboardControl != 0)
                return;
            (args.context as WindowPending).UpdateWindow();
        }

        /* Keep for future description area
        void ResizeHandling (float width, float height)
        {
            // We add a horizontal size controller to adjust incoming vs outgoing area
            float headersHeight = 26f + k_ResizerHeight + EditorStyles.toolbarButton.fixedHeight;
            Rect dragRect = new Rect (0f, m_IncomingAreaHeight + headersHeight, width, k_ResizerHeight);

            if (Event.current.type == EventType.Repaint)
                EditorGUIUtility.AddCursorRect (dragRect, MouseCursor.ResizeVertical);

            // Drag internal splitter
            float deltaY = EditorGUI.MouseDeltaReader(dragRect, true).y;
            if (deltaY != 0f)
            {
                m_IncomingAreaHeight += deltaY;

            }
            float maxHeight = height - k_MinIncomingAreaHeight - headersHeight;
            m_IncomingAreaHeight = Mathf.Clamp (m_IncomingAreaHeight, k_MinIncomingAreaHeight, maxHeight);
        }
        */

        internal static bool ProgressGUI(Rect rect, Task activeTask, bool descriptionTextFirst)
        {
            if (activeTask != null && (activeTask.progressPct != -1 || activeTask.secondsSpent != -1 || activeTask.progressMessage.Length != 0))
            {
                string msg = activeTask.progressMessage;
                Rect sr = rect;
                GUIContent cont = UnityEditorInternal.InternalEditorUtility.animatedProgressImage;
                sr.width = sr.height;
                sr.x += 4;
                sr.y += 4;
                GUI.Label(sr, cont);

                rect.x += sr.width + 4;

                msg = msg.Length == 0 ? activeTask.description : msg;

                if (activeTask.progressPct == -1)
                {
                    rect.width -= sr.width + 4;
                    rect.y += 4;
                    GUI.Label(rect, msg, EditorStyles.miniLabel);
                }
                else
                {
                    rect.width = 120;
                    EditorGUI.ProgressBar(rect, activeTask.progressPct, msg);
                }
                return true;
            }
            return false;
        }

        void CreateResources()
        {
            if (refreshIcon == null)
            {
                refreshIcon = EditorGUIUtility.LoadIcon("Refresh");
                refreshIcon.hideFlags = HideFlags.HideAndDontSave;
                refreshIcon.name = "RefreshIcon";
            }

            if (header == null)
                header = "OL Title";

            CreateStaticResources();

            if (s_ToolbarButtonsWidth == 0f)
            {
                s_ToolbarButtonsWidth = EditorStyles.toolbarButton.CalcSize(EditorGUIUtility.TrTextContent("Incoming (xx)")).x;
                s_ToolbarButtonsWidth += EditorStyles.toolbarButton.CalcSize(EditorGUIUtility.TrTextContent("Outgoing")).x;
                s_ToolbarButtonsWidth += EditorStyles.toolbarButton.CalcSize(new GUIContent(refreshIcon)).x;
                s_SettingsButtonWidth = EditorStyles.toolbarButton.CalcSize(Styles.editorSettingsLabel).x;
                s_DeleteChangesetsButtonWidth = EditorStyles.toolbarButton.CalcSize(EditorGUIUtility.TrTextContent("Delete Empty Changesets")).x;
                minSize = new Vector2(s_ToolbarButtonsWidth + k_MinSearchFieldWidth, k_MinWindowHeight);
            }
        }

        void CreateStaticResources()
        {
            if (syncIcon == null)
            {
                syncIcon = EditorGUIUtility.LoadIcon("VersionControl/Incoming Icon");
                syncIcon.hideFlags = HideFlags.HideAndDontSave;
                syncIcon.name = "SyncIcon";
            }
            if (changeIcon == null)
            {
                changeIcon = EditorGUIUtility.LoadIcon("VersionControl/Outgoing Icon");
                changeIcon.hideFlags = HideFlags.HideAndDontSave;
                changeIcon.name = "ChangeIcon";
            }
        }

        double TimeElapsed()
        {
            var elapsed = DateTime.Now - lastRefresh;
            return elapsed.TotalMilliseconds;
        }
    }
}
