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

namespace UnityEditor
{
    internal class AudioMixerSnapshotListView
    {
        private ReorderableListWithRenameAndScrollView m_ReorderableListWithRenameAndScrollView;
        private AudioMixerController m_Controller;
        List<AudioMixerSnapshotController> m_Snapshots;
        ReorderableListWithRenameAndScrollView.State m_State;

        class Styles
        {
            public GUIContent starIcon = new GUIContent(EditorGUIUtility.FindTexture("Favorite"), "Start snapshot");
            public GUIContent header = new GUIContent("Snapshots", "A snapshot is a set of values for all parameters in the mixer. When using the mixer you modify parameters in the selected snapshot. Blend between multiple snapshots at runtime.");
            public GUIContent addButton = new GUIContent("+");
            public Texture2D snapshotsIcon = EditorGUIUtility.FindTexture("AudioMixerSnapshot Icon");
        }
        static Styles s_Styles;


        public AudioMixerSnapshotListView(ReorderableListWithRenameAndScrollView.State state)
        {
            m_State = state;
        }

        public void OnMixerControllerChanged(AudioMixerController controller)
        {
            m_Controller = controller;
            RecreateListControl();
        }

        int GetSnapshotIndex(AudioMixerSnapshotController snapshot)
        {
            for (int i = 0; i < m_Snapshots.Count; i++)
            {
                if (m_Snapshots[i] == snapshot)
                    return i;
            }

            return 0;
        }

        void RecreateListControl()
        {
            if (m_Controller == null)
                return;

            m_Snapshots = new List<AudioMixerSnapshotController>(m_Controller.snapshots);

            ReorderableList reorderableList = new ReorderableList(m_Snapshots, typeof(AudioMixerSnapshotController), true, false, false, false);
            reorderableList.onReorderCallback = EndDragChild;
            reorderableList.elementHeight = 16f;
            reorderableList.headerHeight = 0f;
            reorderableList.footerHeight = 0f;
            reorderableList.showDefaultBackground = false;
            reorderableList.index = GetSnapshotIndex(m_Controller.TargetSnapshot);

            m_ReorderableListWithRenameAndScrollView = new ReorderableListWithRenameAndScrollView(reorderableList, m_State);
            m_ReorderableListWithRenameAndScrollView.onSelectionChanged += SelectionChanged;
            m_ReorderableListWithRenameAndScrollView.onNameChangedAtIndex += NameChanged;
            m_ReorderableListWithRenameAndScrollView.onDeleteItemAtIndex += Delete;
            m_ReorderableListWithRenameAndScrollView.onGetNameAtIndex += GetNameOfElement;
            m_ReorderableListWithRenameAndScrollView.onCustomDrawElement += CustomDrawElement;
        }

        void SaveToBackend()
        {
            m_Controller.snapshots = m_Snapshots.ToArray();

            m_Controller.OnSubAssetChanged();
        }

        public void LoadFromBackend()
        {
            if (m_Controller == null)
                return;

            m_Snapshots.Clear();
            m_Snapshots.AddRange(m_Controller.snapshots);
        }

        public void OnEvent()
        {
            if (m_Controller == null)
                return;
            m_ReorderableListWithRenameAndScrollView.OnEvent();
        }

        public void CustomDrawElement(Rect r, int index, bool isActive, bool isFocused)
        {
            Event evt = Event.current;
            if (evt.type == EventType.MouseUp && evt.button == 1 && r.Contains(evt.mousePosition))
            {
                SnapshotMenu.Show(r, m_Snapshots[index], this);
                evt.Use();
            }

            const float iconSize = 14f;
            const float spacing = 5f;

            bool isSelected = (index == m_ReorderableListWithRenameAndScrollView.list.index) && !m_ReorderableListWithRenameAndScrollView.IsRenamingIndex(index);

            // Text
            r.width -= iconSize + spacing;
            m_ReorderableListWithRenameAndScrollView.DrawElementText(r, index, isActive, isSelected, isFocused);

            // Startup icon
            if (m_Controller.startSnapshot == m_Snapshots[index])
            {
                r.x = r.xMax + spacing + 5f;
                r.y = r.y + (r.height - iconSize) / 2;
                r.width = r.height = iconSize;
                GUI.Label(r, s_Styles.starIcon, GUIStyle.none);
            }
        }

        public float GetTotalHeight()
        {
            if (m_Controller == null)
                return 0f;
            return m_ReorderableListWithRenameAndScrollView.list.GetHeight() + AudioMixerDrawUtils.kSectionHeaderHeight;
        }

        public void OnGUI(Rect rect)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            Rect headerRect, contentRect;
            using (new EditorGUI.DisabledScope(m_Controller == null))
            {
                AudioMixerDrawUtils.DrawRegionBg(rect, out headerRect, out contentRect);
                AudioMixerDrawUtils.HeaderLabel(headerRect, s_Styles.header, s_Styles.snapshotsIcon);
            }

            if (m_Controller != null)
            {
                // Ensure gui is in-sync with backend (TargetSnapShotIndex can be changed anytime from the backend)
                int targetIndex = GetSnapshotIndex(m_Controller.TargetSnapshot);
                if (targetIndex != m_ReorderableListWithRenameAndScrollView.list.index)
                {
                    m_ReorderableListWithRenameAndScrollView.list.index = targetIndex;
                    m_ReorderableListWithRenameAndScrollView.FrameItem(targetIndex);
                }
                m_ReorderableListWithRenameAndScrollView.OnGUI(contentRect);

                if (GUI.Button(new Rect(headerRect.xMax - 15f, headerRect.y + 3f, 15f, 15f), s_Styles.addButton, EditorStyles.label))
                    Add();
            }
        }

        public void SelectionChanged(int index)
        {
            //For some UI reason, selecting some region just below the last element of the
            //re-orderable list will cause a selection index greater than is available.
            if (index >= m_Snapshots.Count)
                index = m_Snapshots.Count - 1;

            m_Controller.TargetSnapshot = m_Snapshots[index];
            UpdateViews();
        }

        string GetNameOfElement(int index)
        {
            return m_Snapshots[index].name;
        }

        public void NameChanged(int index, string newName)
        {
            m_Snapshots[index].name = newName;
            SaveToBackend();
        }

        void DuplicateCurrentSnapshot()
        {
            Undo.RecordObject(m_Controller, "Duplicate current snapshot");
            m_Controller.CloneNewSnapshotFromTarget(true);
            LoadFromBackend();
            UpdateViews();
        }

        void Add()
        {
            Undo.RecordObject(m_Controller, "Add new snapshot");
            m_Controller.CloneNewSnapshotFromTarget(true);
            LoadFromBackend();
            Rename(m_Controller.TargetSnapshot);
            UpdateViews();
        }

        void DeleteSnapshot(AudioMixerSnapshotController snapshot)
        {
            AudioMixerSnapshotController[] snapshots = m_Controller.snapshots;
            if (snapshots.Length <= 1)
            {
                Debug.Log("You must have at least 1 snapshot in an AudioMixer.");
                return;
            }

            m_Controller.RemoveSnapshot(snapshot);
            LoadFromBackend();
            m_ReorderableListWithRenameAndScrollView.list.index = GetSnapshotIndex(m_Controller.TargetSnapshot);
            UpdateViews();
        }

        void Delete(int index)
        {
            DeleteSnapshot(m_Snapshots[index]);
        }

        public void EndDragChild(ReorderableList list)
        {
            m_Snapshots = m_ReorderableListWithRenameAndScrollView.list.list as List<AudioMixerSnapshotController>;
            SaveToBackend();
        }

        private void UpdateViews()
        {
            AudioMixerWindow mixerWindow = (AudioMixerWindow)WindowLayout.FindEditorWindowOfType(typeof(AudioMixerWindow));
            if (mixerWindow != null)
                mixerWindow.Repaint();

            InspectorWindow.RepaintAllInspectors();
        }

        void SetAsStartupSnapshot(AudioMixerSnapshotController snapshot)
        {
            Undo.RecordObject(m_Controller, "Set start snapshot");
            m_Controller.startSnapshot = snapshot;
        }

        void Rename(AudioMixerSnapshotController snapshot)
        {
            m_ReorderableListWithRenameAndScrollView.BeginRename(GetSnapshotIndex(snapshot), 0f);
        }

        internal class SnapshotMenu
        {
            class data
            {
                public AudioMixerSnapshotController snapshot;
                public AudioMixerSnapshotListView list;
            }

            static public void Show(Rect buttonRect, AudioMixerSnapshotController snapshot, AudioMixerSnapshotListView list)
            {
                var menu = new GenericMenu();
                data input = new data() { snapshot = snapshot, list = list };
                menu.AddItem(new GUIContent("Set as start Snapshot"), false, SetAsStartupSnapshot, input);
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Rename"), false, Rename, input);
                menu.AddItem(new GUIContent("Duplicate"), false, Duplicate, input);
                menu.AddItem(new GUIContent("Delete"), false, Delete, input);

                menu.DropDown(buttonRect);
            }

            static void SetAsStartupSnapshot(object userData)
            {
                data input = userData as data;
                input.list.SetAsStartupSnapshot(input.snapshot);
            }

            static void Rename(object userData)
            {
                data input = userData as data;
                input.list.Rename(input.snapshot);
            }

            static void Duplicate(object userData)
            {
                data input = userData as data;
                input.list.DuplicateCurrentSnapshot();
            }

            static void Delete(object userData)
            {
                data input = userData as data;
                input.list.DeleteSnapshot(input.snapshot);
            }
        }

        public void OnUndoRedoPerformed()
        {
            LoadFromBackend();
        }
    }
}
