// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
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
        }
        static Styles s_Styles = null;

        static Texture2D changeIcon = null;
        Texture2D syncIcon = null;
        Texture2D refreshIcon = null;
        GUIStyle header;
        [SerializeField] ListControl pendingList;
        [SerializeField] ListControl incomingList;

        bool m_ShowIncoming = false;
        const float k_ResizerHeight =  17f;
        const float k_MinIncomingAreaHeight = 50f;
        const float k_BottomBarHeight = 17f;
        float s_ToolbarButtonsWidth = 0f;
        float s_SettingsButtonWidth = 0f;
        float s_DeleteChangesetsButtonWidth = 0f;

        static GUIContent[] sStatusWheel;

        // Workaround to reload vcs info upon domain reload. TODO: Fix VersionControl.ListControl to get rid of this
        static bool s_DidReload = false; // defaults to false after domain reload

        void InitStyles()
        {
            if (s_Styles == null)
            {
                s_Styles = new Styles();
            }
        }

        static internal GUIContent StatusWheel
        {
            get
            {
                if (sStatusWheel == null)
                {
                    sStatusWheel = new GUIContent[12];
                    for (int i = 0; i < 12; i++)
                    {
                        GUIContent gc = new GUIContent();
                        gc.image = EditorGUIUtility.LoadIcon("WaitSpin" + i.ToString("00")) as Texture2D;
                        //EditorGUIUtility.LoadIconForSkin(
                        //gc.image = tex != null ? tex : EditorGUIUtility.LoadIcon("Builtin Skins/Icons/    WaitSpin" + i.ToString("00") + ".png") as Texture2D;
                        gc.image.hideFlags = HideFlags.HideAndDontSave;
                        gc.image.name = "Spinner";
                        sStatusWheel[i] = gc;
                    }
                }
                int frame = (int)Mathf.Repeat(Time.realtimeSinceStartup * 10, 11.99f);
                return sStatusWheel[frame];
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
                Repaint();
            }
        }

        // called to update the status
        void UpdateWindow()
        {
            if (!Provider.isActive)
            {
                pendingList.Clear();
                Provider.UpdateSettings(); // Try to resend the settings if we by chance has become online again
                Repaint();
                return;
            }

            if (Provider.onlineState == OnlineState.Online)
            {
                Task changesTask = Provider.ChangeSets();
                changesTask.SetCompletionAction(CompletionAction.OnChangeSetsPendingWindow);

                Task incomingTask = Provider.Incoming();
                incomingTask.SetCompletionAction(CompletionAction.OnIncomingPendingWindow);
            }
        }

        void OnGotLatest(Task t)
        {
            UpdateWindow();
        }

        static void OnVCTaskCompletedEvent(Task task, CompletionAction completionAction)
        {
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

        public static void UpdateAllWindows()
        {
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

        void PopulateListControl(ListControl list, Task task, Texture2D icon)
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
                    changeItem.Name = change.description;
                }
                else
                {
                    changeItem = list.Add(null, change.description, change);
                }
                changeItem.Exclusive = true;  // Single selection only
                changeItem.CanAccept = true;  // Accept drag and drop
                changeItem.Icon = icon; // changeset icon
            }

            // Refresh here will trigger the expand events to ensure the same change lists
            // are kept open.  This will in turn trigger change list contents update requests
            list.Refresh();
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

            Event e = Event.current;
            float toolBarHeight = EditorStyles.toolbar.fixedHeight;
            bool refresh = false;

            GUILayout.BeginArea(new Rect(0, 0, position.width, toolBarHeight));


            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();

            int incomingChangesetCount = incomingList.Root == null ? 0 : incomingList.Root.ChildCount;
            m_ShowIncoming = !GUILayout.Toggle(!m_ShowIncoming, "Outgoing", EditorStyles.toolbarButton);

            GUIContent cont = GUIContent.Temp("Incoming" + (incomingChangesetCount == 0 ? "" : " (" + incomingChangesetCount.ToString() + ")"));
            m_ShowIncoming = GUILayout.Toggle(m_ShowIncoming, cont, EditorStyles.toolbarButton);

            if (EditorGUI.EndChangeCheck())
                refresh = true;

            GUILayout.FlexibleSpace();

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
                Mathf.FloorToInt(position.width - s_ToolbarButtonsWidth - s_SettingsButtonWidth - s_DeleteChangesetsButtonWidth) > 0 &&
                HasEmptyPendingChangesets();
            if (showDeleteEmptyChangesetsButton && GUILayout.Button("Delete Empty Changesets", EditorStyles.toolbarButton))
            {
                DeleteEmptyPendingChangesets();
            }

            bool showSettingsButton = Mathf.FloorToInt(position.width - s_ToolbarButtonsWidth - s_SettingsButtonWidth) > 0;
            if (showSettingsButton && GUILayout.Button("Settings", EditorStyles.toolbarButton))
            {
                EditorApplication.ExecuteMenuItem("Edit/Project Settings/Editor");
                EditorWindow.FocusWindowIfItsOpen<InspectorWindow>();
                GUIUtility.ExitGUI();
            }

            Color origColor = GUI.color;
            GUI.color = new Color(1, 1, 1, 1 * .5f);
            bool refreshButtonClicked = GUILayout.Button(refreshIcon, EditorStyles.toolbarButton);
            refresh = refresh || refreshButtonClicked;
            GUI.color = origColor;

            if (e.isKey && GUIUtility.keyboardControl == 0)
            {
                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F5)
                {
                    // TODO handle refresh different
                    refresh = true;
                    e.Use();
                }
            }

            if (refresh)
            {
                if (refreshButtonClicked)
                    Provider.InvalidateCache();
                UpdateWindow();
            }

            GUILayout.EndArea();

            Rect rect = new Rect(0, toolBarHeight, position.width, position.height - toolBarHeight - k_BottomBarHeight);

            bool repaint = false;

            GUILayout.EndHorizontal();

            // Disabled Window view
            if (!Provider.isActive)
            {
                Color tmpColor = GUI.color;
                GUI.color = new Color(0.8f, 0.5f, 0.5f);
                rect.height = toolBarHeight;
                GUILayout.BeginArea(rect);

                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.FlexibleSpace();
                string msg = "DISABLED";
                if (Provider.enabled)
                {
                    if (Provider.onlineState == OnlineState.Updating)
                    {
                        GUI.color = new Color(0.8f, 0.8f, 0.5f);
                        msg = "CONNECTING...";
                    }
                    else
                        msg = "OFFLINE";
                }

                GUILayout.Label(msg, EditorStyles.miniLabel);
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

                var content = new GUIContent("Apply All Incoming Changes");
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

                    using (new EditorGUI.DisabledScope(incomingList.Size == 0))
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

            if (repaint)
                Repaint();
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
                GUIContent cont = StatusWheel;
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
                s_ToolbarButtonsWidth = EditorStyles.toolbarButton.CalcSize(new GUIContent("Incoming (xx)")).x;
                s_ToolbarButtonsWidth += EditorStyles.toolbarButton.CalcSize(new GUIContent("Outgoing")).x;
                s_ToolbarButtonsWidth += EditorStyles.toolbarButton.CalcSize(new GUIContent(refreshIcon)).x;

                s_SettingsButtonWidth = EditorStyles.toolbarButton.CalcSize(new GUIContent("Settings")).x;
                s_DeleteChangesetsButtonWidth = EditorStyles.toolbarButton.CalcSize(new GUIContent("Delete Empty Changesets")).x;
            }
        }

        void CreateStaticResources()
        {
            if (syncIcon == null)
            {
                syncIcon = EditorGUIUtility.LoadIcon("vcs_incoming");
                syncIcon.hideFlags = HideFlags.HideAndDontSave;
                syncIcon.name = "SyncIcon";
            }
            if (changeIcon == null)
            {
                changeIcon = EditorGUIUtility.LoadIcon("vcs_change");
                changeIcon.hideFlags = HideFlags.HideAndDontSave;
                changeIcon.name = "ChangeIcon";
            }
        }
    }
}
