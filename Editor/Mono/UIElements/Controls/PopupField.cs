// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class PopupField<T> : BaseField<T>
    {
        private List<T> m_Choices;
        protected TextElement m_TextElement;

        public override T value
        {
            get { return base.value; }
            set
            {
                int newIndex = m_Choices.IndexOf(value);
                if (newIndex < 0)
                    throw new ArgumentException(string.Format("Value {0} is not present in the list of possible values", value));

                m_Index = newIndex;

                base.value = value;
                m_TextElement.text = m_Value.ToString();
            }
        }

        private int m_Index = -1;
        public int index
        {
            get { return m_Index; }
            set
            {
                if (value != m_Index)
                {
                    if (value >= m_Choices.Count || value < 0)
                        throw new ArgumentException(string.Format("Index {0} is beyond the scope of possible value", value));
                    m_Index = value;
                    this.value = m_Choices[m_Index];
                }
            }
        }

        public string text
        {
            get { return m_TextElement.text; }
        }

        // This property will be removed once JF's pr hits trunk.
        public List<T> choices
        {
            get { return m_Choices; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("choices can't be null");

                m_Choices = value;
            }
        }

        private PopupField(List<T> choices)
        {
            if (choices == null)
                throw new ArgumentNullException("choices can't be null");

            m_TextElement = new TextElement();
            Add(m_TextElement);

            m_Choices = choices;

            AddToClassList("popupField");

            RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
        }

        public PopupField(List<T> choices, T defaultValue) :
            this(choices)
        {
            if (defaultValue == null)
                throw new ArgumentNullException("defaultValue can't be null");

            if (!m_Choices.Contains(defaultValue))
                throw new ArgumentException(string.Format("Default value {0} is not present in the list of possible values", defaultValue));

            // note: idx will be set when setting value
            value = defaultValue;
        }

        public PopupField(List<T> choices, int defaultIndex) :
            this(choices)
        {
            if (defaultIndex >= m_Choices.Count || defaultIndex < 0)
                throw new ArgumentException(string.Format("Default Index {0} is beyond the scope of possible value", value));

            // note: value will be set when setting idx
            index = defaultIndex;
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == (int)MouseButton.LeftMouse)
                ShowMenu();
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if ((evt as KeyDownEvent)?.character == '\n')
                ShowMenu();
        }

        private void ShowMenu()
        {
            var menu = new GenericMenu();

            foreach (T item in m_Choices)
            {
                bool isSelected = EqualityComparer<T>.Default.Equals(item, value);
                menu.AddItem(new GUIContent(item.ToString()), isSelected,
                    () => ChangeValueFromMenu(item));
            }

            var menuPosition = new Vector2(0.0f, layout.height);
            menuPosition = this.LocalToWorld(menuPosition);
            var menuRect = new Rect(menuPosition, Vector2.zero);
            menu.DropDown(menuRect);
        }

        private void ChangeValueFromMenu(T menuItem)
        {
            value = menuItem;
        }
    }
}
