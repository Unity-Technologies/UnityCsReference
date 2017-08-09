// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;

namespace UnityEditorInternal
{
    internal class ReorderableListWithRenameAndScrollView
    {
        [Serializable]
        public class State
        {
            public Vector2 m_ScrollPos = new Vector2(0, 0);
            public RenameOverlay m_RenameOverlay = new RenameOverlay();
        }

        ReorderableList m_ReorderableList;
        State m_State;
        int m_LastSelectedIndex = -1;
        bool m_HadKeyFocusAtMouseDown = false;
        int m_FrameIndex = -1;

        public GUIStyle listElementStyle = null; // if null then a default style is used
        public GUIStyle renameOverlayStyle = null;  // if null then the default rename style is used
        public Func<int, string> onGetNameAtIndex; // rect, index, selected
        public Action<int, string> onNameChangedAtIndex; // index, new name
        public Action<int> onSelectionChanged;
        public Action<int> onDeleteItemAtIndex;
        public ReorderableList.ElementCallbackDelegate onCustomDrawElement;
        public ReorderableList list { get { return m_ReorderableList; } }

        public class Styles
        {
            public GUIStyle reorderableListLabel = new GUIStyle("PR Label");
            public GUIStyle reorderableListLabelRightAligned;

            public Styles()
            {
                Texture2D transparent = reorderableListLabel.hover.background;
                reorderableListLabel.normal.background = transparent;
                reorderableListLabel.active.background = transparent;
                reorderableListLabel.focused.background = transparent;
                reorderableListLabel.onNormal.background = transparent;
                reorderableListLabel.onHover.background = transparent;
                reorderableListLabel.onActive.background = transparent;
                reorderableListLabel.onFocused.background = transparent;
                reorderableListLabel.padding.left = reorderableListLabel.padding.right = 0;
                reorderableListLabel.alignment = TextAnchor.MiddleLeft;

                reorderableListLabelRightAligned = new GUIStyle(reorderableListLabel);
                reorderableListLabelRightAligned.alignment = TextAnchor.MiddleRight;
                reorderableListLabelRightAligned.clipping = TextClipping.Overflow;
            }
        }
        static Styles s_Styles;

        public GUIStyle elementStyle
        {
            get { return listElementStyle ?? s_Styles.reorderableListLabel; }
        }

        public GUIStyle elementStyleRightAligned
        {
            get { return s_Styles.reorderableListLabelRightAligned; }
        }

        public ReorderableListWithRenameAndScrollView(ReorderableList list, State state)
        {
            m_State = state;

            m_ReorderableList = list;

            // Add common handling for the following delegates
            m_ReorderableList.drawElementCallback += DrawElement;
            m_ReorderableList.onSelectCallback += SelectCallback;
            m_ReorderableList.onMouseUpCallback += MouseUpCallback;
            m_ReorderableList.onReorderCallback += ReorderCallback;
        }

        RenameOverlay GetRenameOverlay()
        {
            return m_State.m_RenameOverlay;
        }

        public void OnEvent()
        {
            GetRenameOverlay().OnEvent();
        }

        void EnsureRowIsVisible(int index, float scrollGUIHeight)
        {
            if (index < 0)
                return;

            float topPixelOfRow = m_ReorderableList.elementHeight * index + 2;
            float scrollBottom = topPixelOfRow - scrollGUIHeight + m_ReorderableList.elementHeight + 3;
            m_State.m_ScrollPos.y = Mathf.Clamp(m_State.m_ScrollPos.y, scrollBottom, topPixelOfRow);
        }

        public void OnGUI(Rect rect)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            //Event evt = Event.current;

            if (onGetNameAtIndex == null)
                Debug.LogError("Ensure to set: 'onGetNameAtIndex'");

            Event evt = Event.current;

            if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition))
                m_HadKeyFocusAtMouseDown = m_ReorderableList.HasKeyboardControl();

            if (m_FrameIndex != -1)
            {
                EnsureRowIsVisible(m_FrameIndex, rect.height);
                m_FrameIndex = -1;
            }

            GUILayout.BeginArea(rect);
            m_State.m_ScrollPos = GUILayout.BeginScrollView(m_State.m_ScrollPos);

            m_ReorderableList.DoLayoutList();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
            AudioMixerDrawUtils.DrawScrollDropShadow(rect, m_State.m_ScrollPos.y, m_ReorderableList.GetHeight());

            KeyboardHandling();
            CommandHandling();
        }

        public bool IsRenamingIndex(int index)
        {
            return GetRenameOverlay().IsRenaming() && GetRenameOverlay().userData == index && !GetRenameOverlay().isWaitingForDelay;
        }

        public void DrawElement(Rect r, int index, bool isActive, bool isFocused)
        {
            if (IsRenamingIndex(index))
            {
                // We do not want to set rect when they can be invalid, this can happen for layout or used events
                if (r.width >= 0f && r.height >= 0f)
                {
                    r.x -= 2; // Fit rename field text perfect with label
                    GetRenameOverlay().editFieldRect = r;
                }
                DoRenameOverlay();
            }
            else
            {
                if (onCustomDrawElement != null)
                    onCustomDrawElement(r, index, isActive, isFocused);
                else
                    DrawElementText(r, index, isActive, index == m_ReorderableList.index, isFocused);
            }
        }

        public void DrawElementText(Rect r, int index, bool isActive, bool isSelected, bool isFocused)
        {
            if (Event.current.type == EventType.Repaint && onGetNameAtIndex != null)
            {
                elementStyle.Draw(r, onGetNameAtIndex(index), false, false, isSelected, true);
            }
        }

        virtual public void DoRenameOverlay()
        {
            if (GetRenameOverlay().IsRenaming())
                if (!GetRenameOverlay().OnGUI())
                    RenameEnded();
        }

        public void BeginRename(int index, float delay)
        {
            GetRenameOverlay().BeginRename(onGetNameAtIndex(index), index, delay);
            m_ReorderableList.index = index;
            m_LastSelectedIndex = index;
            FrameItem(index);
        }

        void RenameEnded()
        {
            if (GetRenameOverlay().userAcceptedRename)
            {
                if (onNameChangedAtIndex != null)
                {
                    string name = string.IsNullOrEmpty(GetRenameOverlay().name) ? GetRenameOverlay().originalName : GetRenameOverlay().name;
                    int index = GetRenameOverlay().userData;
                    onNameChangedAtIndex(index, name);
                }
            }

            // We give keyboard focus back to our reorderable list because the rename utility stole it (now we give it back)
            if (GetRenameOverlay().HasKeyboardFocus())
                m_ReorderableList.GrabKeyboardFocus();

            GetRenameOverlay().Clear();
        }

        public void EndRename(bool acceptChanges)
        {
            if (GetRenameOverlay().IsRenaming())
            {
                GetRenameOverlay().EndRename(acceptChanges);
                RenameEnded();
            }
        }

        public void ReorderCallback(ReorderableList list)
        {
            m_LastSelectedIndex = list.index;
        }

        public void MouseUpCallback(ReorderableList list)
        {
            if (m_HadKeyFocusAtMouseDown && list.index == m_LastSelectedIndex)
                BeginRename(list.index, 0.5f);

            m_LastSelectedIndex = list.index;
        }

        // Fired on mousedown on elemement
        public void SelectCallback(ReorderableList list)
        {
            FrameItem(list.index);
            if (onSelectionChanged != null)
                onSelectionChanged(list.index);
        }

        void RemoveSelected()
        {
            if (m_ReorderableList.index < 0 || m_ReorderableList.index >= m_ReorderableList.count)
            {
                Debug.Log("Invalid index to remove " + m_ReorderableList.index);
                return;
            }

            if (onDeleteItemAtIndex != null)
                onDeleteItemAtIndex(m_ReorderableList.index);
        }

        public void FrameItem(int index)
        {
            m_FrameIndex = index;
        }

        bool CanBeginRename()
        {
            return !GetRenameOverlay().IsRenaming() && m_ReorderableList.index >= 0;
        }

        void CommandHandling()
        {
            Event evt = Event.current;
            if (Event.current.type == EventType.ExecuteCommand)
            {
                switch (evt.commandName)
                {
                    case "OnLostFocus":
                        EndRename(true);
                        evt.Use();
                        break;
                }
            }
        }

        void KeyboardHandling()
        {
            Event evt = Event.current;
            if (evt.type != EventType.KeyDown)
                return;

            /*if (GUI.GetNameOfFocusedControl () == kRenameField)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Escape:
                        EndRename ();
                        break;
                }
            }*/
            if (m_ReorderableList.HasKeyboardControl())
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Home:
                        evt.Use();
                        m_ReorderableList.index = 0;
                        //EndRename(true);
                        FrameItem(m_ReorderableList.index);
                        break;

                    case KeyCode.End:
                        evt.Use();
                        m_ReorderableList.index = m_ReorderableList.count - 1;
                        //EndRename(true);
                        FrameItem(m_ReorderableList.index);
                        break;

                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        if (CanBeginRename() && Application.platform == RuntimePlatform.OSXEditor)
                        {
                            BeginRename(m_ReorderableList.index, 0f);
                            evt.Use();
                        }
                        break;

                    case KeyCode.F2:
                        if (CanBeginRename() && Application.platform != RuntimePlatform.OSXEditor)
                        {
                            BeginRename(m_ReorderableList.index, 0f);
                            evt.Use();
                        }
                        break;

                    case KeyCode.Delete:
                        RemoveSelected();
                        evt.Use();
                        break;
                }
            }
        }
    }
}
