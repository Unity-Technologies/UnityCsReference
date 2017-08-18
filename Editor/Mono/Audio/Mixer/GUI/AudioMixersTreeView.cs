// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor.Audio;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // Item

    internal class AudioMixerItem : TreeViewItem
    {
        public AudioMixerController mixer { get; set; }
        public string infoText { get; set; }
        public float labelWidth { get; set; }
        bool lastSuspendedState { get; set; }
        const string kSuspendedText = " - Inactive";

        public AudioMixerItem(int id, int depth, TreeViewItem parent, string displayName, AudioMixerController mixer, string infoText)
            : base(id, depth, parent, displayName)
        {
            this.mixer = mixer;
            this.infoText = infoText;
            UpdateSuspendedString(true);
        }

        public void UpdateSuspendedString(bool force)
        {
            bool isSuspended = mixer.isSuspended;
            if (lastSuspendedState != isSuspended || force)
            {
                lastSuspendedState = isSuspended;

                if (isSuspended)
                    AddSuspendedText();
                else
                    RemoveSuspendedText();
            }
        }

        void RemoveSuspendedText()
        {
            int index = infoText.IndexOf(kSuspendedText, StringComparison.Ordinal);
            if (index >= 0)
                infoText = infoText.Remove(index, kSuspendedText.Length);
        }

        void AddSuspendedText()
        {
            int index = infoText.IndexOf(kSuspendedText, StringComparison.Ordinal);
            if (index < 0)
                infoText += kSuspendedText;
        }
    }


    // Dragging

    internal class AudioMixerTreeViewDragging : TreeViewDragging
    {
        private const string k_AudioMixerDraggingID = "AudioMixerDragging";
        Action<List<AudioMixerController>, AudioMixerController> m_MixersDroppedOnMixerCallback;

        class DragData
        {
            public DragData(List<AudioMixerItem> draggedItems)
            {
                m_DraggedItems = draggedItems;
            }

            public List<AudioMixerItem> m_DraggedItems;
        }

        public AudioMixerTreeViewDragging(TreeViewController treeView, Action<List<AudioMixerController>, AudioMixerController> mixerDroppedOnMixerCallback)
            : base(treeView)
        {
            m_MixersDroppedOnMixerCallback = mixerDroppedOnMixerCallback;
        }

        public override void StartDrag(TreeViewItem draggedNode, List<int> draggedNodes)
        {
            // We do not allow changing routing in playmode for now
            if (EditorApplication.isPlaying)
                return;

            List<AudioMixerItem> draggedItems = GetAudioMixerItemsFromIDs(draggedNodes);

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData(k_AudioMixerDraggingID, new DragData(draggedItems));
            string title = draggedNodes.Count + " AudioMixer" + (draggedNodes.Count > 1 ? "s" : ""); // title is only shown on OSX (at the cursor)
            DragAndDrop.StartDrag(title);
        }

        public override bool DragElement(TreeViewItem targetItem, Rect targetItemRect, int row)
        {
            // First ensure we are dragging AudioMixers (and not some other objects)
            var dragData = DragAndDrop.GetGenericData(k_AudioMixerDraggingID) as DragData;
            if (dragData == null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.None;
                return false;
            }

            // Handle the case where we hover inside the treeview rect but not over any items
            bool isDraggingOverEmptyArea = targetItem == null;
            if (isDraggingOverEmptyArea && m_TreeView.GetTotalRect().Contains(Event.current.mousePosition))
            {
                if (m_DropData != null)
                {
                    // Ensure no rendering of target items
                    m_DropData.dropTargetControlID = 0;
                    m_DropData.rowMarkerControlID = 0;
                }

                if (Event.current.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    if (m_MixersDroppedOnMixerCallback != null)
                        m_MixersDroppedOnMixerCallback(GetAudioMixersFromItems(dragData.m_DraggedItems), null);
                }
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                Event.current.Use();
                return false;
            }

            // Normal handling when over dragging over items
            return base.DragElement(targetItem, targetItemRect, row);
        }

        public override DragAndDropVisualMode DoDrag(TreeViewItem parentNode, TreeViewItem targetNode, bool perform, DropPosition dragPos)
        {
            var dragData = DragAndDrop.GetGenericData(k_AudioMixerDraggingID) as DragData;
            if (dragData == null)
                return DragAndDropVisualMode.None;

            var draggedItems = dragData.m_DraggedItems;
            var parentMixerItem = parentNode as AudioMixerItem;
            if (parentMixerItem != null && dragData != null)
            {
                List<AudioMixerGroupController> draggedMasterGroups = (from i in draggedItems select i.mixer.masterGroup).ToList();
                var allGroups = parentMixerItem.mixer.GetAllAudioGroupsSlow();
                bool causesFeedback = AudioMixerController.WillModificationOfTopologyCauseFeedback(allGroups, draggedMasterGroups, parentMixerItem.mixer.masterGroup, null);
                bool validDrag = ValidDrag(parentNode, draggedItems) && !causesFeedback;
                if (perform && validDrag)
                {
                    if (m_MixersDroppedOnMixerCallback != null)
                        m_MixersDroppedOnMixerCallback(GetAudioMixersFromItems(draggedItems), parentMixerItem.mixer);
                }
                return validDrag ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Rejected;
            }
            return DragAndDropVisualMode.None;
        }

        bool ValidDrag(TreeViewItem parent, List<AudioMixerItem> draggedItems)
        {
            List<int> draggedIDs = (from n in draggedItems select n.id).ToList();

            TreeViewItem currentParent = parent;
            while (currentParent != null)
            {
                if (draggedIDs.Contains(currentParent.id))
                    return false;
                currentParent = currentParent.parent;
            }
            return true;
        }

        private List<AudioMixerItem> GetAudioMixerItemsFromIDs(List<int> draggedMixers)
        {
            var found = TreeViewUtility.FindItemsInList(draggedMixers, m_TreeView.data.GetRows());
            return found.OfType<AudioMixerItem>().ToList();
        }

        private List<AudioMixerController> GetAudioMixersFromItems(List<AudioMixerItem> draggedItems)
        {
            return (from i in draggedItems select i.mixer).ToList();
        }
    }


    // Datasource

    internal class AudioMixersDataSource : TreeViewDataSource
    {
        Func<List<AudioMixerController>> m_GetAllControllersCallback;

        public AudioMixersDataSource(TreeViewController treeView, Func<List<AudioMixerController>> getAllControllersCallback)
            : base(treeView)
        {
            showRootItem = false;
            m_GetAllControllersCallback = getAllControllersCallback;
        }

        public override void FetchData()
        {
            int depth = -1;
            bool expandAll = m_TreeView.state.expandedIDs.Count == 0; // State is persisted so only do this once per new AudioMixerWindow

            m_RootItem = new TreeViewItem(1010101010, depth, null, "InvisibleRoot");
            SetExpanded(m_RootItem.id, true);

            List<AudioMixerController> m_Mixers = m_GetAllControllersCallback();

            m_NeedRefreshRows = true;
            if (m_Mixers.Count > 0)
            {
                // First create a tree view item for each controller
                var roots = m_Mixers.Select(mixer => new AudioMixerItem(mixer.GetInstanceID(), 0, m_RootItem, mixer.name, mixer, GetInfoText(mixer))).ToList();

                // Rearrange items to a tree based on output mixer group
                foreach (var item in roots)
                {
                    SetChildParentOfMixerItem(item, roots);
                }

                SetItemDepthRecursive(m_RootItem, -1);  // -1 because the root item is hidden
                SortRecursive(m_RootItem);

                if (expandAll)
                    m_TreeView.data.SetExpandedWithChildren(m_RootItem, true);
            }
        }

        static string GetInfoText(AudioMixerController controller)
        {
            string s;
            if (controller.outputAudioMixerGroup != null)
                s = string.Format("({0} of {1})",  controller.outputAudioMixerGroup.name, controller.outputAudioMixerGroup.audioMixer.name);
            else
                s = "(Audio Listener)";
            return s;
        }

        void SetChildParentOfMixerItem(AudioMixerItem item, List<AudioMixerItem> items)
        {
            if (item.mixer.outputAudioMixerGroup != null)
            {
                var parentMixer = item.mixer.outputAudioMixerGroup.audioMixer;
                var parentItem = TreeViewUtility.FindItemInList(parentMixer.GetInstanceID(), items) as AudioMixerItem;
                if (parentItem != null)
                {
                    parentItem.AddChild(item);
                }
            }
            else
            {
                m_RootItem.AddChild(item);
            }
        }

        void SetItemDepthRecursive(TreeViewItem item, int depth)
        {
            item.depth = depth;
            if (!item.hasChildren)
                return;

            foreach (var child in item.children)
                SetItemDepthRecursive(child, depth + 1);
        }

        void SortRecursive(TreeViewItem item)
        {
            if (!item.hasChildren)
                return;
            item.children.Sort(new TreeViewItemAlphaNumericSort());

            foreach (var child in item.children)
                SortRecursive(child);
        }

        public override bool IsRenamingItemAllowed(TreeViewItem item)
        {
            return true;
        }

        public int GetInsertAfterItemIDForNewItem(string newName, TreeViewItem parentItem)
        {
            // Find pos under parent
            int insertAfterID = parentItem.id;

            if (!parentItem.hasChildren)
                return insertAfterID;

            for (int idx = 0; idx < parentItem.children.Count; ++idx)
            {
                int instanceID = parentItem.children[idx].id;

                // Use same name compare as when we sort in the backend: See AssetDatabase.cpp: SortChildren
                string propertyPath = AssetDatabase.GetAssetPath(instanceID);
                if (EditorUtility.NaturalCompare(Path.GetFileNameWithoutExtension(propertyPath), newName) > 0)
                    break;

                insertAfterID = instanceID;
            }
            return insertAfterID;
        }

        override public void InsertFakeItem(int id, int parentID, string name, Texture2D icon)
        {
            TreeViewItem checkItem = FindItem(id);
            if (checkItem != null)
            {
                Debug.LogError("Cannot insert fake Item because id is not unique " + id + " Item already there: " + checkItem.displayName);
                return;
            }

            if (FindItem(parentID) != null)
            {
                // Ensure parent Item's children is visible
                SetExpanded(parentID, true);

                var visibleRows = GetRows();

                TreeViewItem parentItem;
                int parentIndex = TreeViewController.GetIndexOfID(visibleRows, parentID);
                if (parentIndex >= 0)
                    parentItem = visibleRows[parentIndex];
                else
                    parentItem = m_RootItem; // Fallback to root Item as parent

                // Create fake item for insertion
                int indentLevel = parentItem.depth + 1;
                m_FakeItem = new TreeViewItem(id, indentLevel, parentItem, name);
                m_FakeItem.icon = icon;

                // Find pos under parent
                int insertAfterID = GetInsertAfterItemIDForNewItem(name, parentItem);

                // Find pos in expanded rows and insert
                int index = TreeViewController.GetIndexOfID(visibleRows, insertAfterID);
                if (index >= 0)
                {
                    // Ensure to bypass all children of 'insertAfterID'
                    while (++index < visibleRows.Count)
                    {
                        if (visibleRows[index].depth <= indentLevel)
                            break;
                    }

                    if (index < visibleRows.Count)
                        visibleRows.Insert(index, m_FakeItem);
                    else
                        visibleRows.Add(m_FakeItem);
                }
                else
                {
                    // not visible parent: insert as first
                    if (visibleRows.Count > 0)
                        visibleRows.Insert(0, m_FakeItem);
                    else
                        visibleRows.Add(m_FakeItem);
                }

                m_NeedRefreshRows = false;

                m_TreeView.Frame(m_FakeItem.id, true, false);
                m_TreeView.Repaint();
            }
            else
            {
                Debug.LogError("No parent Item found with ID: " + parentID);
            }
        }
    }

    // Item GUI

    internal class AudioMixersTreeViewGUI : TreeViewGUI
    {
        public AudioMixersTreeViewGUI(TreeViewController treeView)
            : base(treeView)
        {
            k_IconWidth = 0;
            k_TopRowMargin = k_BottomRowMargin = 2f;
        }

        protected override void OnContentGUI(Rect rect, int row, TreeViewItem item, string label, bool selected, bool focused, bool useBoldFont, bool isPinging)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (!isPinging)
            {
                // The rect is assumed indented and sized after the content when pinging
                float indent = GetContentIndent(item);
                rect.x += indent;
                rect.width -= indent;
            }

            AudioMixerItem mixerItem = item as AudioMixerItem;
            if (mixerItem == null)
                return;

            GUIStyle lineStyle = useBoldFont ? Styles.lineBoldStyle : Styles.lineStyle;

            // Draw text
            lineStyle.padding.left = (int)(k_IconWidth + iconTotalPadding + k_SpaceBetweenIconAndText);
            lineStyle.Draw(rect, label, false, false, selected, focused);

            // Draw info text
            mixerItem.UpdateSuspendedString(false);
            const float minSpaceBetween = 8f;
            if (mixerItem.labelWidth <= 0)
                mixerItem.labelWidth = lineStyle.CalcSize(GUIContent.Temp(label)).x;   // only calc once
            Rect infoRect = rect;
            infoRect.x += mixerItem.labelWidth + minSpaceBetween;
            using (new EditorGUI.DisabledScope(true))
            {
                lineStyle.Draw(infoRect, mixerItem.infoText, false, false, false, false);
            }

            // TODO Icon overlay (RCS)
            if (iconOverlayGUI != null)
            {
                Rect iconOverlayRect = rect;
                iconOverlayRect.width = k_IconWidth + iconTotalPadding;
                iconOverlayGUI(item, iconOverlayRect);
            }
        }

        protected override Texture GetIconForItem(TreeViewItem node)
        {
            return null; // We do not want any icons
        }

        protected CreateAssetUtility GetCreateAssetUtility()
        {
            return ((TreeViewStateWithAssetUtility)m_TreeView.state).createAssetUtility;
        }

        protected override void RenameEnded()
        {
            string name = string.IsNullOrEmpty(GetRenameOverlay().name) ? GetRenameOverlay().originalName : GetRenameOverlay().name;
            int instanceID = GetRenameOverlay().userData;
            bool isCreating = GetCreateAssetUtility().IsCreatingNewAsset();
            bool userAccepted = GetRenameOverlay().userAcceptedRename;

            if (userAccepted)
            {
                if (isCreating)
                {
                    // Create a new asset
                    GetCreateAssetUtility().EndNewAssetCreation(name);
                    m_TreeView.ReloadData();
                }
                else
                {
                    // Rename an existing asset
                    ObjectNames.SetNameSmartWithInstanceID(instanceID, name);
                }
            }
        }

        override protected void ClearRenameAndNewItemState()
        {
            GetCreateAssetUtility().Clear();
            base.ClearRenameAndNewItemState();
        }

        AudioMixerItem GetSelectedItem()
        {
            return m_TreeView.FindItem(m_TreeView.GetSelection().FirstOrDefault()) as AudioMixerItem;
        }

        override protected void SyncFakeItem()
        {
            if (!m_TreeView.data.HasFakeItem() && GetCreateAssetUtility().IsCreatingNewAsset())
            {
                // Get selection from tree
                int parentInstanceID = m_TreeView.data.root.id;
                var selectedItem = GetSelectedItem();
                if (selectedItem != null)
                    parentInstanceID = selectedItem.parent.id;
                m_TreeView.data.InsertFakeItem(GetCreateAssetUtility().instanceID, parentInstanceID, GetCreateAssetUtility().originalName, GetCreateAssetUtility().icon);
            }

            if (m_TreeView.data.HasFakeItem() && !GetCreateAssetUtility().IsCreatingNewAsset())
            {
                m_TreeView.data.RemoveFakeItem();
            }
        }

        public void BeginCreateNewMixer()
        {
            //int instanceID, EndNameEditAction endAction, string pathName, Texture2D icon, string resourceFile
            ClearRenameAndNewItemState();

            // Use resouce file to store instanceID for output group so we can use that for the newly created audiomixer
            string resourceFileData = string.Empty;
            var selectedItem = GetSelectedItem();
            if (selectedItem != null && selectedItem.mixer.outputAudioMixerGroup != null)
                resourceFileData = selectedItem.mixer.outputAudioMixerGroup.GetInstanceID().ToString();

            int instanceID = 0;

            if (GetCreateAssetUtility().BeginNewAssetCreation(
                    instanceID, ScriptableObject.CreateInstance<DoCreateAudioMixer>(), "NewAudioMixer.mixer", null, resourceFileData))
            {
                SyncFakeItem();

                // Start naming the asset
                bool renameStarted = GetRenameOverlay().BeginRename(GetCreateAssetUtility().originalName, instanceID, 0f);
                if (!renameStarted)
                    Debug.LogError("Rename not started (when creating new asset)");
            }
        }
    }


    // TreeView

    internal class AudioMixersTreeView
    {
        private TreeViewController m_TreeView;
        const int kObjectSelectorID = 1212;
        List<AudioMixerController> m_DraggedMixers;

        class Styles
        {
            public GUIContent header = new GUIContent("Mixers", "All mixers in the project are shown here. By default a mixer outputs to the AudioListener but mixers can also route their output to other mixers. Each mixer shows where it outputs (in parenthesis). To reroute a mixer simply drag the mixer upon another mixer and select a group from the popup.");
            public GUIContent addText = new GUIContent("+", "Add mixer asset. The asset will be saved in the same folder as the current selected mixer or if none is selected saved in the Assets folder.");
            public Texture2D audioMixerIcon = EditorGUIUtility.FindTexture("AudioMixerController Icon");
        }
        static Styles s_Styles;

        public AudioMixersTreeView(AudioMixerWindow mixerWindow, TreeViewState treeState, Func<List<AudioMixerController>> getAllControllersCallback)
        {
            m_TreeView = new TreeViewController(mixerWindow, treeState);
            m_TreeView.deselectOnUnhandledMouseDown = false;
            m_TreeView.selectionChangedCallback += OnTreeSelectionChanged;
            m_TreeView.contextClickItemCallback += OnTreeViewContextClick;

            var treeViewGUI = new AudioMixersTreeViewGUI(m_TreeView);
            var treeViewDataSource = new AudioMixersDataSource(m_TreeView, getAllControllersCallback);
            var treeViewDragging = new AudioMixerTreeViewDragging(m_TreeView, OnMixersDroppedOnMixerCallback);
            m_TreeView.Init(mixerWindow.position, treeViewDataSource, treeViewGUI, treeViewDragging);
            m_TreeView.ReloadData();
        }

        public void ReloadTree()
        {
            m_TreeView.ReloadData();
            m_TreeView.Repaint();
        }

        public void OnMixerControllerChanged(AudioMixerController controller)
        {
            if (controller != null)
                m_TreeView.SetSelection(new int[] {controller.GetInstanceID()}, true);
        }

        public void DeleteAudioMixerCallback(object obj)
        {
            AudioMixerController controller = (AudioMixerController)obj;
            if (controller != null)
            {
                ProjectWindowUtil.DeleteAssets(new[] { controller.GetInstanceID() }.ToList(), true);
            }
        }

        public void OnTreeViewContextClick(int index)
        {
            AudioMixerItem node = (AudioMixerItem)m_TreeView.FindItem(index);
            if (node != null)
            {
                GenericMenu pm = new GenericMenu();
                pm.AddItem(new GUIContent("Delete AudioMixer"), false, DeleteAudioMixerCallback, node.mixer);
                pm.ShowAsContext();
            }
        }

        public void OnTreeSelectionChanged(int[] selection)
        {
            Selection.instanceIDs = selection;
        }

        public float GetTotalHeight()
        {
            const float minHeight = 20f;
            return AudioMixerDrawUtils.kSectionHeaderHeight + Mathf.Max(minHeight, m_TreeView.gui.GetTotalSize().y);
        }

        public void OnGUI(Rect rect)
        {
            int treeViewKeyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);

            if (s_Styles == null)
                s_Styles = new Styles();

            m_TreeView.OnEvent();

            Rect headerRect, contentRect;
            AudioMixerDrawUtils.DrawRegionBg(rect, out headerRect, out contentRect);
            AudioMixerDrawUtils.HeaderLabel(headerRect, s_Styles.header, s_Styles.audioMixerIcon);

            if (GUI.Button(new Rect(headerRect.xMax - 15f, headerRect.y + 3f, 15f, 15f), s_Styles.addText, EditorStyles.label))
            {
                AudioMixersTreeViewGUI gui = m_TreeView.gui as AudioMixersTreeViewGUI;
                gui.BeginCreateNewMixer();
            }

            m_TreeView.OnGUI(contentRect, treeViewKeyboardControlID);

            if (m_TreeView.data.rowCount == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    GUI.Label(new RectOffset(-20, 0, -2, 0).Add(contentRect), "No mixers found");
                }
            }

            AudioMixerDrawUtils.DrawScrollDropShadow(contentRect, m_TreeView.state.scrollPos.y, m_TreeView.gui.GetTotalSize().y);

            HandleCommandEvents(treeViewKeyboardControlID);
            HandleObjectSelectorResult();
        }

        void HandleCommandEvents(int treeViewKeyboardControlID)
        {
            if (GUIUtility.keyboardControl != treeViewKeyboardControlID)
                return;

            EventType eventType = Event.current.type;
            if (eventType == EventType.ExecuteCommand || eventType == EventType.ValidateCommand)
            {
                bool execute = eventType == EventType.ExecuteCommand;

                if (Event.current.commandName == "Delete" || Event.current.commandName == "SoftDelete")
                {
                    Event.current.Use();
                    if (execute)
                        ProjectWindowUtil.DeleteAssets(m_TreeView.GetSelection().ToList(), true);
                }
                else if (Event.current.commandName == "Duplicate")
                {
                    Event.current.Use();
                    if (execute)
                        ProjectWindowUtil.DuplicateAssets(m_TreeView.GetSelection());
                }
            }
        }

        const string kExpandedStateIdentifier = "AudioMixerWindowMixers";

        public void EndRenaming()
        {
            m_TreeView.EndNameEditing(true);
        }

        public void OnUndoRedoPerformed()
        {
            ReloadTree();
        }

        void OnMixersDroppedOnMixerCallback(List<AudioMixerController> draggedMixers, AudioMixerController droppedUponMixer)
        {
            // Set Unity selection when drag ended (when dragging non selected items we just render them as selected while dragging)
            int[] draggedIDs = (from i in draggedMixers select i.GetInstanceID()).ToArray();
            m_TreeView.SetSelection(draggedIDs, true);
            Selection.instanceIDs = draggedIDs;

            if (droppedUponMixer == null)
            {
                // Dragged to the root -> clear all outputgroups
                Undo.RecordObjects(draggedMixers.ToArray(), "Set output group for mixer" + (draggedMixers.Count > 1 ? "s" : ""));
                foreach (var mixer in draggedMixers)
                    mixer.outputAudioMixerGroup = null;
                ReloadTree();
            }
            else
            {
                // Show Object Selector for output group selection
                m_DraggedMixers = draggedMixers;
                Object startSelection = draggedMixers.Count == 1 ? draggedMixers[0].outputAudioMixerGroup : null;
                ObjectSelector.get.Show(startSelection, typeof(AudioMixerGroup), null, false, new List<int>() { droppedUponMixer.GetInstanceID() });
                ObjectSelector.get.objectSelectorID = kObjectSelectorID;
                ObjectSelector.get.titleContent = new GUIContent("Select Output Audio Mixer Group");
                GUIUtility.ExitGUI();
            }
        }

        void HandleObjectSelectorResult()
        {
            Event evt = Event.current;
            if (evt.type == EventType.ExecuteCommand)
            {
                string commandName = evt.commandName;
                if (commandName == "ObjectSelectorUpdated" && ObjectSelector.get.objectSelectorID == kObjectSelectorID)
                {
                    if (m_DraggedMixers == null || m_DraggedMixers.Count == 0)
                        Debug.LogError("Unexpected invalid mixer list used for dragging");

                    var selected =  ObjectSelector.GetCurrentObject();
                    AudioMixerGroup selectedGroup = selected != null ? selected as AudioMixerGroup : null;
                    Undo.RecordObjects(m_DraggedMixers.ToArray(), "Set output group for mixer" + (m_DraggedMixers.Count > 1 ? "s" : ""));
                    foreach (var mixer in m_DraggedMixers)
                    {
                        if (mixer != null)
                            mixer.outputAudioMixerGroup = selectedGroup;
                        else
                            Debug.LogError("invalid mixer: is null");
                    }

                    GUI.changed = true;
                    evt.Use();

                    ReloadTree();

                    // Ensure newly selected output group is revealed
                    int[] selectedIDs = (from i in m_DraggedMixers select i.GetInstanceID()).ToArray();
                    m_TreeView.SetSelection(selectedIDs, true);
                }

                if (commandName == "ObjectSelectorClosed")
                {
                    m_DraggedMixers = null;  // cleanup stored state
                }
            }
        }
    }
}
