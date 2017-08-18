// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor
{
    internal class PopupList : PopupWindowContent
    {
        public delegate void OnSelectCallback(ListElement element);
        static EditorGUI.RecycledTextEditor s_RecycledEditor = new EditorGUI.RecycledTextEditor();
        static string s_TextFieldName = "ProjectBrowserPopupsTextField";
        static int s_TextFieldHash = s_TextFieldName.GetHashCode();
        public enum Gravity
        {
            Top,
            Bottom
        }

        public class ListElement
        {
            public GUIContent m_Content;
            private float m_FilterScore;
            private bool m_Selected;
            private bool m_WasSelected;
            private bool m_PartiallySelected;
            private bool m_Enabled;

            public ListElement(string text, bool selected, float score)
            {
                m_Content = new GUIContent(text);
                if (!string.IsNullOrEmpty(m_Content.text))
                {
                    char[] a = m_Content.text.ToCharArray();
                    a[0] = char.ToUpper(a[0]);
                    m_Content.text = new string(a);
                }
                m_Selected = selected;
                filterScore = score;
                m_PartiallySelected = false;
                m_Enabled = true;
            }

            public ListElement(string text, bool selected)
            {
                m_Content = new GUIContent(text);
                m_Selected = selected;
                filterScore = 0;
                m_PartiallySelected = false;
                m_Enabled = true;
            }

            public ListElement(string text) : this(text, false)
            {
            }

            public float filterScore
            {
                get
                {
                    return m_WasSelected ? float.MaxValue : m_FilterScore;
                }
                set
                {
                    m_FilterScore = value;
                    ResetScore();
                }
            }

            public bool selected
            {
                get
                {
                    return m_Selected;
                }
                set
                {
                    m_Selected = value;
                    if (m_Selected)
                        m_WasSelected = true;
                }
            }

            public bool enabled
            {
                get
                {
                    return m_Enabled;
                }
                set
                {
                    m_Enabled = value;
                }
            }

            public bool partiallySelected
            {
                get
                {
                    return m_PartiallySelected;
                }
                set
                {
                    m_PartiallySelected = value;
                    if (m_PartiallySelected)
                        m_WasSelected = true;
                }
            }

            public string text
            {
                get
                {
                    return m_Content.text;
                }
                set
                {
                    m_Content.text = value;
                }
            }

            public void ResetScore()
            {
                m_WasSelected = m_Selected || m_PartiallySelected;
            }
        }

        public class InputData
        {
            public List<ListElement> m_ListElements;
            public bool m_CloseOnSelection;
            public bool m_AllowCustom;
            public bool m_EnableAutoCompletion = true;
            public bool m_SortAlphabetically;
            public OnSelectCallback m_OnSelectCallback;
            public int m_MaxCount;

            public InputData()
            {
                m_ListElements = new List<ListElement>();
            }

            public void DeselectAll()
            {
                foreach (ListElement element in m_ListElements)
                {
                    element.selected = false;
                    element.partiallySelected = false;
                }
            }

            public void ResetScores()
            {
                foreach (var element in m_ListElements)
                    element.ResetScore();
            }

            public virtual IEnumerable<ListElement> BuildQuery(string prefix)
            {
                if (prefix == "")
                    return m_ListElements;
                else
                    return m_ListElements.Where(
                        element => element.m_Content.text.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)
                        );
            }

            public IEnumerable<ListElement> GetFilteredList(string prefix)
            {
                IEnumerable<ListElement> res = BuildQuery(prefix);
                if (m_MaxCount > 0)
                    res = res.OrderByDescending(element => element.filterScore).Take(m_MaxCount);
                if (m_SortAlphabetically)
                    return res.OrderBy(element => element.text.ToLower());
                else
                    return res;
            }

            public int GetFilteredCount(string prefix)
            {
                IEnumerable<ListElement> res = BuildQuery(prefix);
                if (m_MaxCount > 0)
                    res = res.Take(m_MaxCount);
                return res.Count();
            }

            public ListElement NewOrMatchingElement(string label)
            {
                foreach (var element in m_ListElements)
                {
                    if (element.text.Equals(label, StringComparison.OrdinalIgnoreCase))
                        return element;
                }

                var res = new ListElement(label, false, -1);
                m_ListElements.Add(res);
                return res;
            }
        }

        private class Styles
        {
            public GUIStyle menuItem = "MenuItem";
            public GUIStyle menuItemMixed = "MenuItemMixed";
            public GUIStyle background = "grey_border";
            public GUIStyle customTextField;
            public GUIStyle customTextFieldCancelButton;
            public GUIStyle customTextFieldCancelButtonEmpty;
            public Styles()
            {
                customTextField = new GUIStyle(EditorStyles.toolbarSearchField);
                customTextFieldCancelButton = new GUIStyle(EditorStyles.toolbarSearchFieldCancelButton);
                customTextFieldCancelButtonEmpty = new GUIStyle(EditorStyles.toolbarSearchFieldCancelButtonEmpty);
            }
        }

        // Static
        static Styles s_Styles;

        // State
        private InputData m_Data;

        // Layout
        const float k_LineHeight = 16;
        const float k_TextFieldHeight = 16;
        const float k_Margin = 10;
        Gravity m_Gravity;

        string m_EnteredTextCompletion = "";
        string m_EnteredText = "";
        int m_SelectedCompletionIndex = 0;

        public PopupList(InputData inputData) : this(inputData, null) {}

        public PopupList(InputData inputData, string initialSelectionLabel)
        {
            m_Data = inputData;
            m_Data.ResetScores();
            SelectNoCompletion();
            m_Gravity = Gravity.Top;
            if (initialSelectionLabel != null)
            {
                m_EnteredTextCompletion = initialSelectionLabel;
                UpdateCompletion();
            }
        }

        public override void OnClose()
        {
            if (m_Data != null)
                m_Data.ResetScores();
        }

        public virtual float GetWindowHeight()
        {
            int count = (m_Data.m_MaxCount == 0) ? m_Data.GetFilteredCount(m_EnteredText) : m_Data.m_MaxCount;
            return count * k_LineHeight + 2 * k_Margin + (m_Data.m_AllowCustom ? k_TextFieldHeight : 0);
        }

        public virtual float GetWindowWidth()
        {
            return 150f;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(GetWindowWidth(), GetWindowHeight());
        }

        public override void OnGUI(Rect windowRect)
        {
            Event evt = Event.current;
            // We do not use the layout event
            if (evt.type == EventType.Layout)
                return;

            if (s_Styles == null)
                s_Styles = new Styles();

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }

            if (m_Gravity == Gravity.Bottom)
            {
                DrawList(editorWindow, windowRect);
                DrawCustomTextField(editorWindow, windowRect);
            }
            else
            {
                DrawCustomTextField(editorWindow, windowRect);
                DrawList(editorWindow, windowRect);
            }

            // Background with 1 pixel border (rendered above content)
            if (evt.type == EventType.Repaint)
                s_Styles.background.Draw(new Rect(windowRect.x, windowRect.y, windowRect.width, windowRect.height), false, false, false, false);
        }

        private void DrawCustomTextField(EditorWindow editorWindow, Rect windowRect)
        {
            if (!m_Data.m_AllowCustom)
                return;

            Event evt = Event.current;
            bool enableAutoCompletion = m_Data.m_EnableAutoCompletion;
            bool closeWindow = false;
            bool useEventBeforeTextField = false;
            bool clearText = false;

            string textBeforeEdit = CurrentDisplayedText();

            // Handle "special" keyboard input
            if (evt.type == EventType.KeyDown)
            {
                switch (evt.keyCode)
                {
                    case KeyCode.Comma:
                    case KeyCode.Space:
                    case KeyCode.Tab:
                    case KeyCode.Return:
                        if (textBeforeEdit != "")
                        {
                            // Toggle state
                            if (m_Data.m_OnSelectCallback != null)
                                m_Data.m_OnSelectCallback(m_Data.NewOrMatchingElement(textBeforeEdit));

                            if (evt.keyCode == KeyCode.Tab || evt.keyCode == KeyCode.Comma)
                                clearText = true;  // to ease multiple entries (it is unlikely that the same filter is used more than once)

                            // Auto close
                            if (m_Data.m_CloseOnSelection || evt.keyCode == KeyCode.Return)
                                closeWindow = true;
                        }
                        useEventBeforeTextField = true;
                        break;
                    case KeyCode.Delete:
                    case KeyCode.Backspace:
                        enableAutoCompletion = false;
                        // Don't use the event yet, so the textfield below can get it and delete the selection
                        break;

                    case KeyCode.DownArrow:
                        ChangeSelectedCompletion(1);
                        useEventBeforeTextField = true;
                        break;

                    case KeyCode.UpArrow:
                        ChangeSelectedCompletion(-1);
                        useEventBeforeTextField = true;
                        break;
                    case KeyCode.None:
                        if (evt.character == ' ' || evt.character == ',')
                            useEventBeforeTextField = true;
                        break;
                }
            }

            string textFieldText;
            // Draw textfield
            {
                bool dummy = false;
                Rect pos = new Rect(windowRect.x + k_Margin / 2, windowRect.y + (m_Gravity == Gravity.Top ? (k_Margin / 2) : (windowRect.height - k_TextFieldHeight - k_Margin / 2)), windowRect.width - k_Margin - 14, k_TextFieldHeight);

                GUI.SetNextControlName(s_TextFieldName);
                EditorGUI.FocusTextInControl(s_TextFieldName);
                int id = EditorGUIUtility.GetControlID(s_TextFieldHash, FocusType.Keyboard, pos);

                if (useEventBeforeTextField)
                    evt.Use();  // We have to delay this until after we get the control id, otherwise the id we get is just -1

                if (GUIUtility.keyboardControl == 0)
                    GUIUtility.keyboardControl = id;

                textFieldText =  EditorGUI.DoTextField(s_RecycledEditor, id, pos, textBeforeEdit, s_Styles.customTextField, null, out dummy, false, false, false);
                Rect buttonRect = pos;
                buttonRect.x += pos.width;
                buttonRect.width = 14;
                // Draw "clear textfield" button (X)
                if ((GUI.Button(buttonRect, GUIContent.none, textFieldText != "" ? s_Styles.customTextFieldCancelButton : s_Styles.customTextFieldCancelButtonEmpty) && textFieldText != "")
                    || clearText)
                {
                    textFieldText = EditorGUI.s_OriginalText = s_RecycledEditor.text = "";
                    s_RecycledEditor.cursorIndex = 0;
                    s_RecycledEditor.selectIndex = 0;
                    enableAutoCompletion = false;
                }
            }

            // Handle autocompletion
            if (textBeforeEdit != textFieldText)
            {
                m_EnteredText = (0 <= s_RecycledEditor.cursorIndex && s_RecycledEditor.cursorIndex < textFieldText.Length) ? textFieldText.Substring(0, s_RecycledEditor.cursorIndex) : textFieldText;

                if (enableAutoCompletion)
                    UpdateCompletion();
                else
                    SelectNoCompletion();
            }

            if (closeWindow)
                editorWindow.Close();
        }

        private string CurrentDisplayedText()
        {
            return m_EnteredTextCompletion != "" ? m_EnteredTextCompletion : m_EnteredText;
        }

        private void UpdateCompletion()
        {
            if (!m_Data.m_EnableAutoCompletion)
                return;
            IEnumerable<string> query = m_Data.GetFilteredList(m_EnteredText).Select(element => element.text);

            if (m_EnteredTextCompletion != "" && m_EnteredTextCompletion.StartsWith(m_EnteredText, System.StringComparison.OrdinalIgnoreCase))
            {
                m_SelectedCompletionIndex = query.TakeWhile(element => element != m_EnteredTextCompletion).Count();
                // m_EnteredTextCompletion is already correct
            }
            else
            {
                // Clamp m_SelectedCompletionIndex to 0..query.Count () - 1
                if (m_SelectedCompletionIndex < 0)
                    m_SelectedCompletionIndex = 0;
                else if (m_SelectedCompletionIndex >= query.Count())
                    m_SelectedCompletionIndex = query.Count() - 1;

                m_EnteredTextCompletion = query.Skip(m_SelectedCompletionIndex).DefaultIfEmpty("").FirstOrDefault();
            }
            AdjustRecycledEditorSelectionToCompletion();
        }

        private void ChangeSelectedCompletion(int change)
        {
            int count = m_Data.GetFilteredCount(m_EnteredText);
            if (m_SelectedCompletionIndex == -1 && change < 0)  // specal case for initial selection
                m_SelectedCompletionIndex = count;

            int index = count > 0 ? (m_SelectedCompletionIndex + change + count) % count : 0;
            SelectCompletionWithIndex(index);
        }

        private void SelectCompletionWithIndex(int index)
        {
            m_SelectedCompletionIndex = index;
            m_EnteredTextCompletion = "";
            UpdateCompletion();
        }

        private void SelectNoCompletion()
        {
            m_SelectedCompletionIndex = -1;
            m_EnteredTextCompletion = "";
            AdjustRecycledEditorSelectionToCompletion();
        }

        private void AdjustRecycledEditorSelectionToCompletion()
        {
            if (m_EnteredTextCompletion != "")
            {
                s_RecycledEditor.text = m_EnteredTextCompletion;
                EditorGUI.s_OriginalText = m_EnteredTextCompletion;
                s_RecycledEditor.cursorIndex = m_EnteredText.Length;
                s_RecycledEditor.selectIndex = m_EnteredTextCompletion.Length; //the selection goes from s_RecycledEditor.cursorIndex (already set by DoTextField) to s_RecycledEditor.selectIndex
            }
        }

        private void DrawList(EditorWindow editorWindow, Rect windowRect)
        {
            Event evt = Event.current;

            int i = -1;
            foreach (var element in m_Data.GetFilteredList(m_EnteredText))
            {
                i++;
                Rect rect = new Rect(windowRect.x, windowRect.y + k_Margin + i * k_LineHeight + (m_Gravity == Gravity.Top && m_Data.m_AllowCustom ? k_TextFieldHeight : 0), windowRect.width, k_LineHeight);

                switch (evt.type)
                {
                    case EventType.Repaint:
                    {
                        GUIStyle style = element.partiallySelected ? s_Styles.menuItemMixed : s_Styles.menuItem;
                        bool selected = element.selected || element.partiallySelected;
                        bool focused = false;
                        bool isHover = i == m_SelectedCompletionIndex;
                        bool isActive = selected;

                        using (new EditorGUI.DisabledScope(!element.enabled))
                        {
                            GUIContent content = element.m_Content;
                            style.Draw(rect, content, isHover, isActive, selected, focused);
                        }
                    }
                    break;
                    case EventType.MouseDown:
                    {
                        if (Event.current.button == 0 && rect.Contains(Event.current.mousePosition) && element.enabled)
                        {
                            // Toggle state
                            if (m_Data.m_OnSelectCallback != null)
                                m_Data.m_OnSelectCallback(element);

                            evt.Use();

                            // Auto close
                            if (m_Data.m_CloseOnSelection)
                                editorWindow.Close();
                        }
                    }
                    break;
                    case EventType.MouseMove:
                    {
                        if (rect.Contains(Event.current.mousePosition))
                        {
                            SelectCompletionWithIndex(i);
                            evt.Use();
                        }
                    }
                    break;
                }
            }
        }
    }
}
