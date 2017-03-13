// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal.VersionControl;
using UnityEditor.VersionControl;
using UnityEditor;

namespace UnityEditor.VersionControl
{
    // Change window used for either submitting a window, editing a change list or creating a new change list.
    internal class WindowChange : EditorWindow
    {
        ListControl submitList = new ListControl();
        AssetList assetList = new AssetList();
        ChangeSet changeSet = new ChangeSet();
        string description = string.Empty;
        bool allowSubmit = false;
        Task taskStatus = null;
        Task taskDesc = null;
        Task taskStat = null;
        Task taskSubmit = null;
        Task taskAdd = null;

        const int kSubmitNotStartedResultCode = 256;
        const int kSubmitRunningResultCode = 0;
        int submitResultCode = kSubmitNotStartedResultCode;
        string submitErrorMessage = null;

        int m_TextAreaControlID = 0;

        const string c_defaultDescription = "";

        public void OnEnable()
        {
            position = new Rect(100, 100, 700, 395);
            minSize = new Vector2(700, 395);

            submitList.ReadOnly = true;
            taskStatus = null;
            taskDesc = null;
            taskStat = null;
            taskSubmit = null;
            submitResultCode = kSubmitNotStartedResultCode;
            submitErrorMessage = null;
        }

        public void OnDisable()
        {
            m_TextAreaControlID = 0;
        }

        // Open the change list window for a list of files only
        static public void Open(AssetList list, bool submit)
        {
            Open(null, list, submit);
        }

        // Perform the actual window open
        static public void Open(ChangeSet change, AssetList assets, bool submit)
        {
            WindowChange win = EditorWindow.GetWindow<WindowChange>(true, "Version Control Changeset");
            win.allowSubmit = submit;
            win.DoOpen(change, assets);
        }

        private string SanitizeDescription(string desc)
        {
            if (Provider.GetActivePlugin() != null && Provider.GetActivePlugin().name != "Perforce")
                return desc;

            // The format of the desc is "on 2013/02/03 by foo@bar *pending* 'The real description'"
            // Extract the part between ' and '
            int idx = desc.IndexOf('\'');
            if (idx == -1)
                return desc;
            idx++;
            int idx2 = desc.IndexOf('\'', idx);
            if (idx2 == -1)
                return desc;
            return desc.Substring(idx, idx2 - idx).Trim(' ', '\t');
        }

        // Open the change list window for one of 2 modes.  File list or exisiting change list.
        // If changeID is null then a list of files is assumed and the user can sumbit them
        // as a new change list.
        void DoOpen(ChangeSet change, AssetList assets)
        {
            taskSubmit = null;
            submitResultCode = kSubmitNotStartedResultCode;
            submitErrorMessage = null;
            changeSet = change;
            description = change == null ? c_defaultDescription : SanitizeDescription(change.description);
            assetList = null;

            // Refresh the assets or changeset states
            if (change == null)
            {
                taskStatus = Provider.Status(assets);
            }
            else
            {
                taskDesc = Provider.ChangeSetDescription(change);
                taskStat = Provider.ChangeSetStatus(change);
            }
        }

        void RefreshList()
        {
            submitList.Clear();

            foreach (Asset it in assetList)
                submitList.Add(null, it.prettyPath, it);

            if (assetList.Count == 0)
            {
                ChangeSet change = new ChangeSet(ListControl.c_emptyChangeListMessage);
                ListItem item = submitList.Add(null, change.description, change);
                item.Dummy = true;
            }

            submitList.Refresh();
            Repaint();
        }

        internal static void OnSubmitted(Task task)
        {
            var winsChange = Resources.FindObjectsOfTypeAll(typeof(WindowChange)) as WindowChange[];
            if (winsChange.Length == 0)
                return; // user closed submit window before submit finished. Just ignore the status callback.
            var win = winsChange[0];
            win.assetList = task.assetList;
            win.submitResultCode = task.resultCode;
            win.submitErrorMessage = null;

            if ((task.resultCode & (int)SubmitResult.Error) != 0)
            {
                string delim = "";
                foreach (Message msg in task.messages)
                {
                    if (msg.severity == Message.Severity.Error)
                        win.submitErrorMessage += delim + msg.message;
                }
            }

            if ((task.resultCode & ((int)SubmitResult.OK | (int)SubmitResult.Error)) != 0)
            {
                WindowPending.UpdateAllWindows();
                bool isNewChangeSet = win.changeSet == null;
                if (isNewChangeSet)
                {
                    // When change list becomes empty we open it to make "delete empty changessets" button visible
                    Task flushTask = Provider.Status(""); // Make a dummy task and wait for it since that will guarantee up to date lists
                    flushTask.Wait();
                    WindowPending.ExpandLatestChangeSet();
                }
            }

            if ((task.resultCode & (int)SubmitResult.OK) != 0)
                win.ResetAndClose();
            else
                win.RefreshList();
        }

        internal static void OnAdded(Task task)
        {
            var winsChange = Resources.FindObjectsOfTypeAll(typeof(WindowChange)) as WindowChange[];
            if (winsChange.Length == 0)
                return; // user closed submit window before submit finished. Just ignore the status callback.
            var win = winsChange[0];
            win.taskSubmit = null;
            win.submitResultCode = kSubmitNotStartedResultCode;
            win.submitErrorMessage = null;
            win.taskAdd = null;

            // Refetch status
            win.taskStatus = Provider.Status(win.assetList, false);
            win.assetList = null;
            WindowPending.UpdateAllWindows(); // reflect newly added assets in pending window
        }

        void OnGUI()
        {
            if ((submitResultCode & (int)SubmitResult.ConflictingFiles) != 0)
                OnConflictingFilesGUI();
            else if ((submitResultCode & (int)SubmitResult.UnaddedFiles) != 0)
                OnUnaddedFilesGUI();
            else if ((submitResultCode & (int)SubmitResult.Error) != 0)
                OnErrorGUI();
            else
                OnSubmitGUI();
        }

        void OnSubmitGUI()
        {
            bool isSubmitting = submitResultCode != kSubmitNotStartedResultCode;
            if (isSubmitting)
                GUI.enabled = false;

            Event evt = Event.current;
            if (evt.isKey && evt.keyCode == KeyCode.Escape)
                Close();

            GUILayout.Label("Description", EditorStyles.boldLabel);

            if (taskStatus != null && taskStatus.resultCode != 0)
            {
                const bool includeFolders = true;
                assetList = taskStatus.assetList.Filter(includeFolders,
                        Asset.States.CheckedOutLocal,
                        Asset.States.DeletedLocal,
                        Asset.States.AddedLocal/*,
                    Asset.States.Branch,
                    Asset.States.Integrate*/);
                RefreshList();
                taskStatus = null;
            }
            else if (taskDesc != null && taskDesc.resultCode != 0)
            {
                description = taskDesc.text.Length > 0 ? taskDesc.text : c_defaultDescription;
                if (description.Trim() == "<enter description here>")
                    description = string.Empty;
                taskDesc = null;
            }
            else if (taskStat != null && taskStat.resultCode != 0)
            {
                assetList = taskStat.assetList;
                RefreshList();
                taskStat = null;
            }

            Task progressTask = taskStatus != null && taskStatus.resultCode == 0 ? taskStatus :
                taskDesc != null && taskDesc.resultCode == 0 ? taskDesc :
                taskStat != null && taskStat.resultCode == 0 ? taskStat : taskSubmit;

            GUI.enabled = (taskDesc == null || taskDesc.resultCode != 0) && submitResultCode == kSubmitNotStartedResultCode;
            {
                description = EditorGUILayout.TextArea(description, GUILayout.Height(150)).Trim();
                if (m_TextAreaControlID == 0)
                    m_TextAreaControlID = EditorGUIUtility.s_LastControlID;
                if (m_TextAreaControlID != 0)
                {
                    EditorGUIUtility.keyboardControl = m_TextAreaControlID;
                    EditorGUIUtility.editingTextField = true;
                }
            }
            GUI.enabled = true;

            GUILayout.Label("Files", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // I would use GUIUtility.GetLastRect() here after the box but that seems to have wierd side effects.
            Rect r1 = new Rect(6, 206, position.width - 12, position.height - 248);
            GUILayout.BeginArea(r1);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.EndArea();
            submitList.OnGUI(new Rect(r1.x + 2, r1.y + 2, r1.width - 4, r1.height - 4), true);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();


            if (submitResultCode == kSubmitNotStartedResultCode)
            {
                if (progressTask != null)
                {
                    // It is possible to have a progressTask for getting description text.
                    GUIContent c = GUIContent.Temp("Getting info");
                    c.image = WindowPending.StatusWheel.image;
                    GUILayout.Label(c);
                    c.image = null;
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Cancel"))
                    ResetAndClose();

                GUI.enabled = progressTask == null && !string.IsNullOrEmpty(description);

                bool keyboardShortcutActivated = evt.isKey && evt.shift && evt.keyCode == KeyCode.Return;
                bool saveByShortcut = keyboardShortcutActivated && !allowSubmit;

                if (Provider.hasChangelistSupport && (GUILayout.Button("Save") || saveByShortcut))
                    Save(false);

                if (allowSubmit)
                {
                    bool origEnabled = GUI.enabled;
                    GUI.enabled = assetList != null && assetList.Count > 0 && !string.IsNullOrEmpty(description);

                    if (GUILayout.Button("Submit") || keyboardShortcutActivated)
                        Save(true);

                    GUI.enabled = origEnabled;
                }
            }
            else
            {
                // submit finished successfully or running

                bool finished = (submitResultCode & (int)SubmitResult.OK) != 0;
                GUI.enabled = finished;

                string msg = "";
                if (finished)
                {
                    msg = "Finished successfully";
                }
                else if (progressTask != null)
                {
                    GUILayout.Label(WindowPending.StatusWheel);
                    msg = progressTask.progressMessage;
                    if (msg.Length == 0)
                        msg = "Running...";
                }

                GUILayout.Label(msg);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close"))
                    ResetAndClose();
            }

            GUI.enabled = true;

            GUILayout.EndHorizontal();
            GUILayout.Space(12);

            if (progressTask != null)
                Repaint();
        }

        void OnErrorGUI()
        {
            GUILayout.Label("Submit failed", EditorStyles.boldLabel);

            string msg = "";
            if (!string.IsNullOrEmpty(submitErrorMessage))
                msg = submitErrorMessage + "\n";

            msg += "See console for details. You can get more details by increasing log level in EditorSettings.";

            GUILayout.Label(msg);

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Close"))
            {
                ResetAndClose();

                // The error message may be telling that the plugin is opening a window
                // to handle conflicts. After that has been handled and we close this window
                // we want to update our state.
                WindowPending.UpdateAllWindows();
            }
            GUILayout.EndHorizontal();
        }

        void OnConflictingFilesGUI()
        {
            string files = "";

            foreach (Asset a in assetList)
            {
                if (a.IsState(Asset.States.Conflicted))
                {
                    files += a.prettyPath + "\n";
                }
            }

            GUILayout.Label("Conflicting files", EditorStyles.boldLabel);
            GUILayout.Label("Some files need to be resolved before submitting:");
            GUI.enabled = false;
            GUILayout.TextArea(files, GUILayout.ExpandHeight(true));
            GUI.enabled = true;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Close"))
                ResetAndClose();

            GUILayout.EndHorizontal();
        }

        void OnUnaddedFilesGUI()
        {
            AssetList toAdd = new AssetList();
            string files = "";

            foreach (Asset a in assetList)
            {
                if (!a.IsState(Asset.States.OutOfSync) && !a.IsState(Asset.States.Synced) && !a.IsState(Asset.States.AddedLocal))
                {
                    files += a.prettyPath + "\n";
                    toAdd.Add(a);
                }
            }

            GUILayout.Label("Files to add", EditorStyles.boldLabel);
            GUILayout.Label("Some additional files need to be added:");
            GUI.enabled = false;
            GUILayout.TextArea(files);
            GUI.enabled = true;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Add files"))
            {
                taskAdd = Provider.Add(toAdd, false);
                taskAdd.SetCompletionAction(CompletionAction.OnAddedChangeWindow);
            }

            if (GUILayout.Button("Abort"))
                ResetAndClose();

            GUILayout.EndHorizontal();
        }

        private void ResetAndClose()
        {
            taskSubmit = null;
            submitResultCode = kSubmitNotStartedResultCode;
            submitErrorMessage = null;
            Close();
        }

        void Save(bool submit)
        {
            if (description.Trim() == c_defaultDescription)
            {
                Debug.LogError("Version control: Please enter a valid change description");
                return;
            }

            UnityEditor.AssetDatabase.SaveAssets();

            // Submit the change list. Last parameter is "save only" so invert the submit flag

            taskSubmit = Provider.Submit(changeSet, assetList, description, !submit);
            submitResultCode = kSubmitRunningResultCode;
            submitErrorMessage = null;
            taskSubmit.SetCompletionAction(CompletionAction.OnSubmittedChangeWindow); // TODO: make it CompletionAction.OnChangeSetsPendingWindow
        }
    }
}
