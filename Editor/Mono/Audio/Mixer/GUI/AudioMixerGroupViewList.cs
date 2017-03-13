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
    internal class AudioMixerGroupViewList
    {
        ReorderableListWithRenameAndScrollView m_ReorderableListWithRenameAndScrollView;
        AudioMixerController m_Controller;
        List<MixerGroupView> m_Views;
        readonly ReorderableListWithRenameAndScrollView.State m_State;

        class Styles
        {
            public GUIContent header = new GUIContent("Views", "A view is the saved visiblity state of the current Mixer Groups. Use views to setup often used combinations of Mixer Groups.");
            public GUIContent addButton = new GUIContent("+");
            public Texture2D viewsIcon = EditorGUIUtility.FindTexture("AudioMixerView Icon");
        }
        static Styles s_Styles;

        public AudioMixerGroupViewList(ReorderableListWithRenameAndScrollView.State state)
        {
            m_State = state;
        }

        public void OnMixerControllerChanged(AudioMixerController controller)
        {
            m_Controller = controller;
            RecreateListControl();
        }

        public void OnUndoRedoPerformed()
        {
            RecreateListControl();
        }

        public void OnEvent()
        {
            if (m_Controller == null)
                return;
            m_ReorderableListWithRenameAndScrollView.OnEvent();
        }

        public void RecreateListControl()
        {
            if (m_Controller == null)
                return;

            m_Views = new List<MixerGroupView>(m_Controller.views);

            // Ensure default view
            if (m_Views.Count == 0)
            {
                var view = new MixerGroupView();
                view.guids = m_Controller.GetAllAudioGroupsSlow().Select(gr => gr.groupID).ToArray();
                view.name = "View";
                m_Views.Add(view);
                SaveToBackend();
            }

            var reorderableList = new ReorderableList(m_Views, typeof(MixerGroupView), true, false, false, false);
            reorderableList.onReorderCallback += EndDragChild;
            reorderableList.elementHeight = 16;
            reorderableList.headerHeight = 0;
            reorderableList.footerHeight = 0;
            reorderableList.showDefaultBackground = false;
            reorderableList.index = m_Controller.currentViewIndex;

            if (m_Controller.currentViewIndex >= reorderableList.count)
                Debug.LogError("State mismatch, currentViewIndex: " + m_Controller.currentViewIndex + ", num items: " + reorderableList.count);

            // Now extend reorderable list with scrollview and renaming functionality
            m_ReorderableListWithRenameAndScrollView = new ReorderableListWithRenameAndScrollView(reorderableList, m_State);
            m_ReorderableListWithRenameAndScrollView.onSelectionChanged += SelectionChanged;
            m_ReorderableListWithRenameAndScrollView.onNameChangedAtIndex += NameChanged;
            m_ReorderableListWithRenameAndScrollView.onDeleteItemAtIndex += Delete;
            m_ReorderableListWithRenameAndScrollView.onGetNameAtIndex += GetNameOfElement;
            m_ReorderableListWithRenameAndScrollView.onCustomDrawElement += CustomDrawElement;
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
                AudioMixerDrawUtils.HeaderLabel(headerRect, s_Styles.header, s_Styles.viewsIcon);
            }

            if (m_Controller != null)
            {
                // Ensure in-sync
                if (m_ReorderableListWithRenameAndScrollView.list.index != m_Controller.currentViewIndex)
                {
                    m_ReorderableListWithRenameAndScrollView.list.index = m_Controller.currentViewIndex;
                    m_ReorderableListWithRenameAndScrollView.FrameItem(m_Controller.currentViewIndex);
                }

                m_ReorderableListWithRenameAndScrollView.OnGUI(contentRect);

                // Call after list to prevent id mismatch
                if (GUI.Button(new Rect(headerRect.xMax - 15f, headerRect.y + 3f, 15f, 15f), s_Styles.addButton, EditorStyles.label))
                    Add();
            }
        }

        public void CustomDrawElement(Rect r, int index, bool isActive, bool isFocused)
        {
            Event evt = Event.current;
            if (evt.type == EventType.MouseUp && evt.button == 1 && r.Contains(evt.mousePosition))
            {
                ViewsContexttMenu.Show(r, index, this);
                evt.Use();
            }

            bool isSelected = (index == m_ReorderableListWithRenameAndScrollView.list.index) && !m_ReorderableListWithRenameAndScrollView.IsRenamingIndex(index);
            m_ReorderableListWithRenameAndScrollView.DrawElementText(r, index, isActive, isSelected, isFocused);
        }

        void SaveToBackend()
        {
            m_Controller.views = m_Views.ToArray();
        }

        void LoadFromBackend()
        {
            m_Views.Clear();
            m_Views.AddRange(m_Controller.views);
        }

        string GetNameOfElement(int index)
        {
            return m_Views[index].name;
        }

        void Add()
        {
            m_Controller.CloneViewFromCurrent();
            LoadFromBackend();

            int newSelectedIndex = m_Views.Count - 1;
            m_Controller.currentViewIndex = newSelectedIndex;
            m_ReorderableListWithRenameAndScrollView.BeginRename(newSelectedIndex, 0f);
        }

        void Delete(int index)
        {
            if (m_Views.Count <= 1)
            {
                Debug.Log("Deleting all views is not allowed");
                return;
            }

            m_Controller.DeleteView(index);
            LoadFromBackend();
        }

        public void NameChanged(int index, string newName)
        {
            LoadFromBackend();
            MixerGroupView view = m_Views[index];
            view.name = newName;
            m_Views[index] = view;
            SaveToBackend();
        }

        public void SelectionChanged(int selectedIndex)
        {
            LoadFromBackend();
            m_Controller.SetView(selectedIndex);
        }

        public void EndDragChild(ReorderableList list)
        {
            m_Views = m_ReorderableListWithRenameAndScrollView.list.list as List<MixerGroupView>;
            SaveToBackend();
        }

        void Rename(int index)
        {
            m_ReorderableListWithRenameAndScrollView.BeginRename(index, 0f);
        }

        void DuplicateCurrentView()
        {
            m_Controller.CloneViewFromCurrent();
            LoadFromBackend();
        }

        internal class ViewsContexttMenu
        {
            class data
            {
                public int viewIndex;
                public AudioMixerGroupViewList list;
            }

            static public void Show(Rect buttonRect, int viewIndex, AudioMixerGroupViewList list)
            {
                var menu = new GenericMenu();
                data input = new data() { viewIndex = viewIndex, list = list };
                menu.AddItem(new GUIContent("Rename"), false, Rename, input);
                menu.AddItem(new GUIContent("Duplicate"), false, Duplicate, input);
                menu.AddItem(new GUIContent("Delete"), false, Delete, input);

                menu.DropDown(buttonRect);
            }

            static void Rename(object userData)
            {
                data input = userData as data;
                input.list.Rename(input.viewIndex);
            }

            static void Duplicate(object userData)
            {
                data input = userData as data;
                input.list.m_Controller.currentViewIndex = input.viewIndex;
                input.list.DuplicateCurrentView();
            }

            static void Delete(object userData)
            {
                data input = userData as data;
                input.list.Delete(input.viewIndex);
            }
        }
    }
}
