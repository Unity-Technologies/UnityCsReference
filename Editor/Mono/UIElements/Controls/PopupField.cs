// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class PopupField<T> : BaseTextControl<T>
    {
        private readonly List<T> m_Choices;

        private T m_Value;
        public override T value
        {
            get { return m_Value; }
            set
            {
                if (EqualityComparer<T>.Default.Equals(m_Value, value))
                    return;

                if (!m_Choices.Contains(value))
                    throw new ArgumentException(string.Format("Value {0} is not present in the list of possible values", value));

                m_Value = value;
                m_Index = m_Choices.IndexOf(m_Value);
                text = m_Value.ToString();
                Dirty(ChangeType.Repaint);
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

        private PopupField(List<T> choices)
        {
            if (choices == null)
                throw new ArgumentNullException("choices can't be null");

            m_Choices = choices;

            AddToClassList("popupField");
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

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse || (evt as KeyDownEvent)?.character == '\n')
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
            SetValueAndNotify(menuItem);
        }
    }
}
