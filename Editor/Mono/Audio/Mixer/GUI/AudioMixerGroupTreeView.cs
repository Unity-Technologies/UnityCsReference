// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using UnityEditorInternal;
using UnityEditor.Audio;
using UnityEditor.IMGUI.Controls;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // Item

    internal class AudioMixerTreeViewNode : TreeViewItem
    {
        public AudioMixerGroupController group { get; set; }

        public AudioMixerTreeViewNode(int instanceID, int depth, TreeViewItem parent, string displayName, AudioMixerGroupController group)
            : base(instanceID, depth, parent, displayName)
        {
            this.group = group;
        }
    }

    // Dragging
    // We want dragging to work from the mixer window to the inspector like the project browser, but also want
    // custom dragging behavior (reparent the group sub assets) so we derive from AssetOrGameObjectTreeViewDragging
    // and override DoDrag.

    internal class AudioGroupTreeViewDragging : AssetsTreeViewDragging
    {
        private AudioMixerGroupTreeView m_owner;

        public AudioGroupTreeViewDragging(TreeViewController treeView, AudioMixerGroupTreeView owner)
            : base(treeView)
        {
            m_owner = owner;
        }

        public override void StartDrag(TreeViewItem draggedItem, List<int> draggedItemIDs)
        {
            if (!EditorApplication.isPlaying)
                base.StartDrag(draggedItem, draggedItemIDs);
        }

        public override DragAndDropVisualMode DoDrag(TreeViewItem parentNode, TreeViewItem targetNode, bool perform, DropPosition dragPos)
        {
            var parentGroupNode = parentNode as AudioMixerTreeViewNode;
            var draggedGroups = new List<Object>(DragAndDrop.objectReferences).OfType<AudioMixerGroupController>().ToList();
            if (parentGroupNode != null && draggedGroups.Count > 0)
            {
                var draggedIDs = (from i in draggedGroups select i.GetInstanceID()).ToList();
                bool validDrag = ValidDrag(parentNode, draggedIDs) && !AudioMixerController.WillModificationOfTopologyCauseFeedback(m_owner.Controller.GetAllAudioGroupsSlow(), draggedGroups, parentGroupNode.group, null);
                if (perform && validDrag)
                {
                    AudioMixerGroupController parentGroup = parentGroupNode.group;

                    // If insertionIndex is -1 we are dropping upon the parentNode
                    int insertionIndex = GetInsertionIndex(parentNode, targetNode, dragPos);

                    m_owner.Controller.ReparentSelection(parentGroup, insertionIndex, draggedGroups);
                    m_owner.ReloadTree();
                    m_TreeView.SetSelection(draggedIDs.ToArray(), true);   // Ensure dropped item(s) are selected and revealed (fixes selection if click dragging a single item that is not selected when drag was started)
                }
                return validDrag ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Rejected;
            }
            return DragAndDropVisualMode.None;
        }

        bool ValidDrag(TreeViewItem parent, List<int> draggedInstanceIDs)
        {
            TreeViewItem currentParent = parent;
            while (currentParent != null)
            {
                if (draggedInstanceIDs.Contains(currentParent.id))
                    return false;
                currentParent = currentParent.parent;
            }
            return true;
        }
    }

    // Datasource

    internal class AudioGroupDataSource : TreeViewDataSource
    {
        public AudioGroupDataSource(TreeViewController treeView, AudioMixerController controller)
            : base(treeView)
        {
            m_Controller = controller;
        }

        private void AddNodesRecursively(AudioMixerGroupController group, TreeViewItem parent, int depth)
        {
            var children = new List<TreeViewItem>();
            for (int i = 0; i < group.children.Length; ++i)
            {
                int uniqueNodeID = GetUniqueNodeID(group.children[i]);
                var node = new AudioMixerTreeViewNode(uniqueNodeID, depth, parent, group.children[i].name, group.children[i]);
                node.parent = parent;
                children.Add(node);
                AddNodesRecursively(group.children[i], node, depth + 1);
            }
            parent.children = children;
        }

        static public int GetUniqueNodeID(AudioMixerGroupController group)
        {
            return group.GetInstanceID(); // alternative: group.groupID.GetHashCode();
        }

        public override void FetchData()
        {
            if (m_Controller == null)
            {
                m_RootItem = null;
                return;
            }

            if (m_Controller.masterGroup == null)
            {
                Debug.LogError("The Master group is missing !!!");
                m_RootItem = null;
                return;
            }

            int uniqueNodeID = GetUniqueNodeID(m_Controller.masterGroup);
            m_RootItem = new AudioMixerTreeViewNode(uniqueNodeID, 0, null, m_Controller.masterGroup.name, m_Controller.masterGroup);
            AddNodesRecursively(m_Controller.masterGroup, m_RootItem, 1);
            m_NeedRefreshRows = true;
        }

        public override bool IsRenamingItemAllowed(TreeViewItem node)
        {
            var audioNode = node as AudioMixerTreeViewNode;
            if (audioNode.group == m_Controller.masterGroup)
                return false;

            return true;
        }

        public AudioMixerController m_Controller;
    }

    // Node GUI

    internal class AudioGroupTreeViewGUI : TreeViewGUI
    {
        readonly float column1Width = 20f;
        readonly Texture2D k_VisibleON = EditorGUIUtility.FindTexture("VisibilityOn");
        public Action<AudioMixerTreeViewNode, bool> NodeWasToggled;
        public AudioMixerController m_Controller = null;

        public AudioGroupTreeViewGUI(TreeViewController treeView)
            : base(treeView)
        {
            k_BaseIndent = column1Width;
            k_IconWidth = 0;
            k_TopRowMargin = k_BottomRowMargin = 2f;
        }

        void OpenGroupContextMenu(AudioMixerTreeViewNode audioNode, bool visible)
        {
            GenericMenu menu = new GenericMenu();

            if (NodeWasToggled != null)
            {
                menu.AddItem(new GUIContent(visible ? "Hide group" : "Show Group"), false, () => NodeWasToggled(audioNode, !visible));
            }
            menu.AddSeparator(string.Empty);

            AudioMixerGroupController[] groups;
            if (m_Controller.CachedSelection.Contains(audioNode.group))
                groups = m_Controller.CachedSelection.ToArray();
            else
                groups = new AudioMixerGroupController[] { audioNode.group };

            AudioMixerColorCodes.AddColorItemsToGenericMenu(menu, groups);
            menu.ShowAsContext();
        }

        override public void OnRowGUI(Rect rowRect, TreeViewItem node, int row, bool selected, bool focused)
        {
            Event evt = Event.current;
            DoItemGUI(rowRect, row, node, selected, focused, false);

            if (m_Controller == null)
                return;

            var audioNode = node as AudioMixerTreeViewNode;
            if (audioNode != null)
            {
                bool oldSelected = m_Controller.CurrentViewContainsGroup(audioNode.group.groupID);
                const float kIconSize = 16f;

                float xMargin = 3f;
                Rect iconRect = new Rect(rowRect.x + xMargin, rowRect.y, kIconSize, kIconSize);
                Rect iconBgRect = new Rect(iconRect.x + 1, iconRect.y + 1, iconRect.width - 2, iconRect.height - 2);

                int colorIndex = audioNode.group.userColorIndex;
                if (colorIndex > 0)
                    EditorGUI.DrawRect(new Rect(rowRect.x, iconBgRect.y, 2, iconBgRect.height), AudioMixerColorCodes.GetColor(colorIndex));

                EditorGUI.DrawRect(iconBgRect, new Color(0.5f, 0.5f, 0.5f, 0.2f));
                if (oldSelected)
                    GUI.DrawTexture(iconRect, k_VisibleON);

                Rect toggleRect = new Rect(2, rowRect.y, rowRect.height, rowRect.height);
                if (evt.type == EventType.MouseUp && evt.button == 0 && toggleRect.Contains(evt.mousePosition))
                {
                    if (NodeWasToggled != null)
                        NodeWasToggled(audioNode, !oldSelected);
                }

                if (evt.type == EventType.ContextClick && iconRect.Contains(evt.mousePosition))
                {
                    OpenGroupContextMenu(audioNode, oldSelected);
                    evt.Use();
                }
            }
        }

        protected override Texture GetIconForItem(TreeViewItem node)
        {
            if (node != null && node.icon != null)
                return node.icon;
            return null;
        }

        protected override void SyncFakeItem()
        {
        }

        protected override void RenameEnded()
        {
            bool userAccepted = GetRenameOverlay().userAcceptedRename;
            if (userAccepted)
            {
                string name = string.IsNullOrEmpty(GetRenameOverlay().name) ? GetRenameOverlay().originalName : GetRenameOverlay().name;
                int instanceID = GetRenameOverlay().userData;
                var audioNode = m_TreeView.FindItem(instanceID) as AudioMixerTreeViewNode;
                if (audioNode != null)
                {
                    ObjectNames.SetNameSmartWithInstanceID(instanceID, name);
                    foreach (var effect in audioNode.group.effects)
                        effect.ClearCachedDisplayName();
                    m_TreeView.ReloadData();
                    if (m_Controller != null)
                        m_Controller.OnSubAssetChanged();
                }
            }
        }
    }


    // TreeView

    internal class AudioMixerGroupPopupContext
    {
        public AudioMixerGroupPopupContext(AudioMixerController controller, AudioMixerGroupController group)
        {
            this.controller = controller;
            this.groups = new AudioMixerGroupController[] { group };
        }

        public AudioMixerGroupPopupContext(AudioMixerController controller, AudioMixerGroupController[] groups)
        {
            this.controller = controller;
            this.groups = groups;
        }

        public AudioMixerController controller;
        public AudioMixerGroupController[] groups;
    }

    internal class AudioMixerGroupTreeView
    {
        private AudioMixerController m_Controller;
        private AudioGroupDataSource m_AudioGroupTreeDataSource;
        private TreeViewState m_AudioGroupTreeState;
        private TreeViewController m_AudioGroupTree;
        private AudioGroupTreeViewGUI m_TreeViewGUI;
        private AudioMixerGroupController m_ScrollToItem;

        class Styles
        {
            public GUIContent header = new GUIContent("Groups", "An Audio Mixer Group is used by e.g Audio Sources to modify the audio output before it reaches the Audio Listener. An Audio Mixer Group will route its output to another Audio Mixer Group if it is made a child of that group. The Master Group will route its output to the Audio Listener if it doesn't route its output into another Mixer.");
            public GUIContent addText = new GUIContent("+", "Add child group");
            public Texture2D audioMixerGroupIcon = EditorGUIUtility.FindTexture("AudioMixerGroup Icon");
        }
        static Styles s_Styles;

        public AudioMixerGroupTreeView(AudioMixerWindow mixerWindow, TreeViewState treeState)
        {
            m_AudioGroupTreeState = treeState;

            m_AudioGroupTree = new TreeViewController(mixerWindow, m_AudioGroupTreeState);
            m_AudioGroupTree.deselectOnUnhandledMouseDown = false;
            m_AudioGroupTree.selectionChangedCallback += OnTreeSelectionChanged;
            m_AudioGroupTree.contextClickItemCallback += OnTreeViewContextClick;
            m_AudioGroupTree.expandedStateChanged += SaveExpandedState;

            m_TreeViewGUI = new AudioGroupTreeViewGUI(m_AudioGroupTree);
            m_TreeViewGUI.NodeWasToggled += OnNodeToggled;

            m_AudioGroupTreeDataSource = new AudioGroupDataSource(m_AudioGroupTree, m_Controller);
            m_AudioGroupTree.Init(mixerWindow.position,
                m_AudioGroupTreeDataSource, m_TreeViewGUI,
                new AudioGroupTreeViewDragging(m_AudioGroupTree, this)
                );
            m_AudioGroupTree.ReloadData();
        }

        public AudioMixerController Controller { get { return m_Controller; } }

        public AudioMixerGroupController ScrollToItem { get { return m_ScrollToItem; } }

        public void UseScrollView(bool useScrollView)
        {
            m_AudioGroupTree.SetUseScrollView(useScrollView);
        }

        public void ReloadTreeData()
        {
            m_AudioGroupTree.ReloadData();
        }

        public void ReloadTree()
        {
            m_AudioGroupTree.ReloadData();
            if (m_Controller != null)
            {
                m_Controller.SanitizeGroupViews();
            }
        }

        public void AddChildGroupPopupCallback(object obj)
        {
            AudioMixerGroupPopupContext context = (AudioMixerGroupPopupContext)obj;
            if (context.groups != null && context.groups.Length > 0)
                AddAudioMixerGroup(context.groups[0]);
        }

        public void AddSiblingGroupPopupCallback(object obj)
        {
            AudioMixerGroupPopupContext context = (AudioMixerGroupPopupContext)obj;
            if (context.groups != null && context.groups.Length > 0)
            {
                var item = m_AudioGroupTree.FindItem(context.groups[0].GetInstanceID()) as AudioMixerTreeViewNode;
                if (item != null)
                {
                    var parent = item.parent as AudioMixerTreeViewNode;
                    AddAudioMixerGroup(parent.group);
                }
            }
        }

        public void AddAudioMixerGroup(AudioMixerGroupController parent)
        {
            if (parent == null || m_Controller == null)
                return;

            Undo.RecordObjects(new UnityEngine.Object[] { m_Controller, parent }, "Add Child Group");
            var newGroup = m_Controller.CreateNewGroup("New Group", true);
            m_Controller.AddChildToParent(newGroup, parent);
            m_Controller.AddGroupToCurrentView(newGroup);

            Selection.objects = new[] {newGroup};
            m_Controller.OnUnitySelectionChanged();
            m_AudioGroupTree.SetSelection(new int[] { newGroup.GetInstanceID() }, true);
            ReloadTree();
            m_AudioGroupTree.BeginNameEditing(0f);
        }

        static string PluralIfNeeded(int count)
        {
            return count > 1 ? "s" : "";
        }

        public void DeleteGroups(List<AudioMixerGroupController> groups, bool recordUndo)
        {
            foreach (AudioMixerGroupController group in groups)
            {
                if (group.HasDependentMixers())
                {
                    if (!EditorUtility.DisplayDialog("Referenced Group", "Deleted group is referenced by another AudioMixer, are you sure?", "Delete", "Cancel"))
                        return;
                    break;
                }
            }

            if (recordUndo)
                Undo.RegisterCompleteObjectUndo(m_Controller, "Delete Group" + PluralIfNeeded(groups.Count));

            m_Controller.DeleteGroups(groups.ToArray());
            ReloadTree();
        }

        public void DuplicateGroups(List<AudioMixerGroupController> groups, bool recordUndo)
        {
            if (recordUndo)
            {
                Undo.RecordObject(m_Controller, "Duplicate group" + PluralIfNeeded(groups.Count));
                Undo.RecordObject(m_Controller.masterGroup, "");
            }

            var duplicatedRoots = m_Controller.DuplicateGroups(groups.ToArray(), recordUndo);
            if (duplicatedRoots.Count > 0)
            {
                ReloadTree();
                var instanceIDs = duplicatedRoots.Select(audioMixerGroup => audioMixerGroup.GetInstanceID()).ToArray();
                m_AudioGroupTree.SetSelection(instanceIDs, false);
                m_AudioGroupTree.Frame(instanceIDs[instanceIDs.Length - 1], true, false);
            }
        }

        void DeleteGroupsPopupCallback(object obj)
        {
            var audioMixerGroupTreeView = (AudioMixerGroupTreeView)obj;
            audioMixerGroupTreeView.DeleteGroups(GetGroupSelectionWithoutMasterGroup(), true);
        }

        void DuplicateGroupPopupCallback(object obj)
        {
            var audioMixerGroupTreeView = (AudioMixerGroupTreeView)obj;
            audioMixerGroupTreeView.DuplicateGroups(GetGroupSelectionWithoutMasterGroup(), true);
        }

        void RenameGroupCallback(object obj)
        {
            var item = (TreeViewItem)obj;
            m_AudioGroupTree.SetSelection(new int[] {item.id}, false);
            m_AudioGroupTree.BeginNameEditing(0f);
        }

        List<AudioMixerGroupController> GetGroupSelectionWithoutMasterGroup()
        {
            var items = GetAudioMixerGroupsFromNodeIDs(m_AudioGroupTree.GetSelection());
            items.Remove(m_Controller.masterGroup);
            return items;
        }

        public void OnTreeViewContextClick(int index)
        {
            var node = m_AudioGroupTree.FindItem(index);
            if (node != null)
            {
                AudioMixerTreeViewNode mixerNode = node as AudioMixerTreeViewNode;
                if (mixerNode != null && mixerNode.group != null)
                {
                    GenericMenu pm = new GenericMenu();

                    if (!EditorApplication.isPlaying)
                    {
                        pm.AddItem(new GUIContent("Add child group"), false, AddChildGroupPopupCallback, new AudioMixerGroupPopupContext(m_Controller, mixerNode.group));
                        if (mixerNode.group != m_Controller.masterGroup)
                        {
                            pm.AddItem(new GUIContent("Add sibling group"), false, AddSiblingGroupPopupCallback, new AudioMixerGroupPopupContext(m_Controller, mixerNode.group));
                            pm.AddSeparator("");
                            pm.AddItem(new GUIContent("Rename"), false, RenameGroupCallback, node);

                            // Mastergroup cannot be deleted nor duplicated
                            var selection = GetGroupSelectionWithoutMasterGroup().ToArray();
                            pm.AddItem(new GUIContent((selection.Length > 1) ? "Duplicate groups (and children)" : "Duplicate group (and children)"), false, DuplicateGroupPopupCallback, this);
                            pm.AddItem(new GUIContent((selection.Length > 1) ? "Remove groups (and children)" : "Remove group (and children)"), false, DeleteGroupsPopupCallback, this);
                        }
                    }
                    else
                    {
                        pm.AddDisabledItem(new GUIContent("Modifying group topology in play mode is not allowed"));
                    }

                    pm.ShowAsContext();
                }
            }
        }

        void OnNodeToggled(AudioMixerTreeViewNode node, bool nodeWasEnabled)
        {
            var treeSelection = GetAudioMixerGroupsFromNodeIDs(m_AudioGroupTree.GetSelection());
            if (!treeSelection.Contains(node.group))
                treeSelection = new List<AudioMixerGroupController> {node.group};
            var newSelection = new List<GUID>();
            var allGroups = m_Controller.GetAllAudioGroupsSlow();
            foreach (var g in allGroups)
            {
                bool inOldSelection = m_Controller.CurrentViewContainsGroup(g.groupID);
                bool inNewSelection = treeSelection.Contains(g);
                bool add = inOldSelection && !inNewSelection;
                if (!inOldSelection && inNewSelection)
                    add = nodeWasEnabled;
                if (add)
                    newSelection.Add(g.groupID);
            }
            m_Controller.SetCurrentViewVisibility(newSelection.ToArray());
        }

        List<AudioMixerGroupController> GetAudioMixerGroupsFromNodeIDs(int[] instanceIDs)
        {
            List<AudioMixerGroupController> newSelectedGroups = new List<AudioMixerGroupController>();
            foreach (var s in instanceIDs)
            {
                var node = m_AudioGroupTree.FindItem(s);
                if (node != null)
                {
                    AudioMixerTreeViewNode mixerNode = node as AudioMixerTreeViewNode;
                    if (mixerNode != null)
                        newSelectedGroups.Add(mixerNode.group);
                }
            }
            return newSelectedGroups;
        }

        public void OnTreeSelectionChanged(int[] selection)
        {
            var groups = GetAudioMixerGroupsFromNodeIDs(selection);
            Selection.objects = groups.ToArray();
            m_Controller.OnUnitySelectionChanged();
            if (groups.Count == 1)
                m_ScrollToItem = groups[0];
            InspectorWindow.RepaintAllInspectors();
        }

        public void InitSelection(bool revealSelectionAndFrameLastSelected)
        {
            if (m_Controller == null)
                return;

            var groups = m_Controller.CachedSelection;
            m_AudioGroupTree.SetSelection((from x in groups select x.GetInstanceID()).ToArray(), revealSelectionAndFrameLastSelected);
        }

        public float GetTotalHeight()
        {
            if (m_Controller == null)
                return 0f;
            return m_AudioGroupTree.gui.GetTotalSize().y + AudioMixerDrawUtils.kSectionHeaderHeight;
        }

        public void OnGUI(Rect rect)
        {
            int treeViewKeyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);

            m_ScrollToItem = null;

            if (s_Styles == null)
                s_Styles = new Styles();

            m_AudioGroupTree.OnEvent();

            Rect headerRect, contentRect;
            using (new EditorGUI.DisabledScope(m_Controller == null))
            {
                AudioMixerDrawUtils.DrawRegionBg(rect, out headerRect, out contentRect);
                AudioMixerDrawUtils.HeaderLabel(headerRect, s_Styles.header, s_Styles.audioMixerGroupIcon);
            }

            if (m_Controller != null)
            {
                AudioMixerGroupController parent = (m_Controller.CachedSelection.Count == 1) ? m_Controller.CachedSelection[0] : m_Controller.masterGroup;
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                {
                    if (GUI.Button(new Rect(headerRect.xMax - 15f, headerRect.y + 3f, 15f, 15f), s_Styles.addText, EditorStyles.label))
                        AddAudioMixerGroup(parent);
                }

                m_AudioGroupTree.OnGUI(contentRect, treeViewKeyboardControlID);
                AudioMixerDrawUtils.DrawScrollDropShadow(contentRect, m_AudioGroupTree.state.scrollPos.y, m_AudioGroupTree.gui.GetTotalSize().y);

                HandleKeyboardEvents(treeViewKeyboardControlID);
                HandleCommandEvents(treeViewKeyboardControlID);
            }
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
                    {
                        DeleteGroups(GetGroupSelectionWithoutMasterGroup(), true);
                        GUIUtility.ExitGUI();  // Cached groups might have been deleted to so early out of event
                    }
                }
                else if (Event.current.commandName == "Duplicate")
                {
                    Event.current.Use();
                    if (execute)
                        DuplicateGroups(GetGroupSelectionWithoutMasterGroup(), true);
                }
            }
        }

        void HandleKeyboardEvents(int treeViewKeyboardControlID)
        {
            if (GUIUtility.keyboardControl != treeViewKeyboardControlID)
                return;

            Event evt = Event.current;
            if (evt.keyCode == KeyCode.Space && evt.type == EventType.KeyDown)
            {
                int[] selection = m_AudioGroupTree.GetSelection();
                if (selection.Length > 0)
                {
                    AudioMixerTreeViewNode node = m_AudioGroupTree.FindItem(selection[0]) as AudioMixerTreeViewNode;
                    bool shown = m_Controller.CurrentViewContainsGroup(node.group.groupID);
                    OnNodeToggled(node, !shown);
                    evt.Use();
                }
            }
        }

        public void OnMixerControllerChanged(AudioMixerController controller)
        {
            if (m_Controller != controller)
            {
                m_TreeViewGUI.m_Controller = controller;
                m_Controller = controller;
                m_AudioGroupTreeDataSource.m_Controller = controller;
                if (controller != null)
                {
                    ReloadTree();
                    InitSelection(false);
                    LoadExpandedState();
                    m_AudioGroupTree.data.SetExpandedWithChildren(m_AudioGroupTree.data.root, true);
                }
            }
        }

        static string GetUniqueAudioMixerName(AudioMixerController controller)
        {
            return "AudioMixer_" + controller.GetInstanceID();
        }

        void SaveExpandedState()
        {
            SessionState.SetIntArray(GetUniqueAudioMixerName(m_Controller), m_AudioGroupTreeState.expandedIDs.ToArray());
        }

        void LoadExpandedState()
        {
            int[] cachedExpandedState = SessionState.GetIntArray(GetUniqueAudioMixerName(m_Controller), null);
            if (cachedExpandedState != null)
            {
                m_AudioGroupTreeState.expandedIDs = new List<int>(cachedExpandedState);
            }
            else
            {
                // Expand whole tree. If no cached data then its the first time tree was loaded in this session
                m_AudioGroupTree.state.expandedIDs = new List<int>();
                m_AudioGroupTree.data.SetExpandedWithChildren(m_AudioGroupTree.data.root, true);
            }
        }

        public void EndRenaming()
        {
            m_AudioGroupTree.EndNameEditing(true);
        }

        public void OnUndoRedoPerformed()
        {
            ReloadTree();
        }
    }
}
