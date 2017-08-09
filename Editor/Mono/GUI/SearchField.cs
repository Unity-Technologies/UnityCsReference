// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.Remoting.Messaging;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    public class SearchField
    {
        int m_ControlID;
        bool m_WantsFocus;
        bool m_AutoSetFocusOnFindCommand = true;
        const float kMinWidth = 36f;
        const float kMaxWidth = 10000000f;
        const float kMinToolbarWidth = 29f;
        const float kMaxToolbarWidth = 200f;

        public delegate void SearchFieldCallback();
        public event SearchFieldCallback downOrUpArrowKeyPressed;

        public SearchField()
        {
            m_ControlID = GUIUtility.GetPermanentControlID();
        }

        public int searchFieldControlID
        {
            get { return m_ControlID; }
            set { m_ControlID = value; }
        }

        public bool autoSetFocusOnFindCommand
        {
            get { return m_AutoSetFocusOnFindCommand; }
            set { m_AutoSetFocusOnFindCommand = value; }
        }

        public void SetFocus()
        {
            m_WantsFocus = true;
        }

        public bool HasFocus()
        {
            return GUIUtility.keyboardControl == m_ControlID;
        }

        public string OnGUI(Rect rect, string text, GUIStyle style, GUIStyle cancelButtonStyle, GUIStyle emptyCancelButtonStyle)
        {
            CommandEventHandling();

            FocusAndKeyHandling();

            float cancelButtonWidth = cancelButtonStyle.fixedWidth;

            // Search field
            Rect textRect = rect;
            textRect.width -= cancelButtonWidth;
            text = EditorGUI.TextFieldInternal(m_ControlID, textRect, text, style);

            // Cancel button
            Rect buttonRect = rect;
            buttonRect.x += rect.width - cancelButtonWidth;
            buttonRect.width = cancelButtonWidth;
            if (GUI.Button(buttonRect, GUIContent.none, text != "" ? cancelButtonStyle : emptyCancelButtonStyle) && text != "")
            {
                text = "";
                GUIUtility.keyboardControl = 0;
            }
            return text;
        }

        public string OnGUI(Rect rect, string text)
        {
            return OnGUI(rect, text, EditorStyles.searchField, EditorStyles.searchFieldCancelButton, EditorStyles.searchFieldCancelButtonEmpty);
        }

        public string OnGUI(string text, params GUILayoutOption[] options)
        {
            Rect rect = GUILayoutUtility.GetRect(kMinWidth, kMaxWidth, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.searchField, options);
            return OnGUI(rect, text);
        }

        public string OnToolbarGUI(Rect rect, string text)
        {
            return OnGUI(rect, text, EditorStyles.toolbarSearchField, EditorStyles.toolbarSearchFieldCancelButton, EditorStyles.toolbarSearchFieldCancelButtonEmpty);
        }

        public string OnToolbarGUI(string text, params GUILayoutOption[] options)
        {
            Rect rect = GUILayoutUtility.GetRect(kMinToolbarWidth, kMaxToolbarWidth, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchField, options);
            return OnToolbarGUI(rect, text);
        }

        void FocusAndKeyHandling()
        {
            Event evt = Event.current;
            if (m_WantsFocus && evt.type == EventType.Repaint)
            {
                GUIUtility.keyboardControl = m_ControlID;
                EditorGUIUtility.editingTextField = true;
                m_WantsFocus = false;
            }

            if (evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.DownArrow || evt.keyCode == KeyCode.UpArrow) &&
                GUIUtility.keyboardControl == m_ControlID && GUIUtility.hotControl == 0)
            {
                if (downOrUpArrowKeyPressed != null)
                {
                    downOrUpArrowKeyPressed();
                    evt.Use();
                }
            }
        }

        void CommandEventHandling()
        {
            Event evt = Event.current;

            if (evt.type != EventType.ExecuteCommand && evt.type != EventType.ValidateCommand)
                return;

            if (m_AutoSetFocusOnFindCommand && evt.commandName == "Find")
            {
                if (evt.type == EventType.ExecuteCommand)
                    SetFocus();
                evt.Use();
            }
        }
    }
} // namespace
