// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class PopupField<T> : BasePopupField<T, T>
    {
        public virtual Func<T, string> formatSelectedValueCallback
        {
            get { return m_FormatSelectedValueCallback; }
            set
            {
                m_FormatSelectedValueCallback = value;
                m_TextElement.text = GetValueToDisplay();
            }
        }

        public virtual Func<T, string> formatListItemCallback
        {
            get { return m_FormatListItemCallback; }
            set { m_FormatListItemCallback = value; }
        }

        internal override string GetValueToDisplay()
        {
            if (m_FormatSelectedValueCallback != null)
                return m_FormatSelectedValueCallback(value);
            return value.ToString();
        }

        internal override string GetListItemToDisplay(T value)
        {
            if (m_FormatListItemCallback != null)
                return m_FormatListItemCallback(value);
            return value.ToString();
        }

        public override T value
        {
            get { return base.value; }
            set
            {
                int newIndex = m_Choices.IndexOf(value);
                if (newIndex < 0)
                {
                    throw new ArgumentException(string.Format("Value {0} is not present in the list of possible values", value));
                }
                m_Index = newIndex;

                base.value = value;
            }
        }

        public override void SetValueWithoutNotify(T newValue)
        {
            int newIndex = m_Choices.IndexOf(newValue);
            if (newIndex < 0)
            {
                throw new ArgumentException(string.Format("Value {0} is not present in the list of possible values", newValue));
            }
            m_Index = newIndex;

            base.SetValueWithoutNotify(newValue);
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

        protected PopupField(List<T> choices, Func<T, string> formatSelectedValueCallback = null, Func<T, string> formatListItemCallback = null)
        {
            this.choices = choices;
            m_FormatSelectedValueCallback = formatSelectedValueCallback;
            m_FormatListItemCallback = formatListItemCallback;
        }

        public PopupField(List<T> choices, T defaultValue, Func<T, string> formatSelectedValueCallback = null, Func<T, string> formatListItemCallback = null) :
            this(choices, formatSelectedValueCallback, formatListItemCallback)
        {
            if (defaultValue == null)
                throw new ArgumentNullException("defaultValue", "defaultValue can't be null");

            if (!m_Choices.Contains(defaultValue))
                throw new ArgumentException(string.Format("Default value {0} is not present in the list of possible values", defaultValue));

            // Note: idx will be set when setting value
            SetValueWithoutNotify(defaultValue);
        }

        public PopupField(List<T> choices, int defaultIndex, Func<T, string> formatSelectedValueCallback = null, Func<T, string> formatListItemCallback = null) :
            this(choices, formatSelectedValueCallback, formatListItemCallback)
        {
            if (defaultIndex >= m_Choices.Count || defaultIndex < 0)
                throw new ArgumentException(string.Format("Default Index {0} is beyond the scope of possible value", value));

            // Note: value will be set when setting idx
            index = defaultIndex;
        }

        internal override void AddMenuItems(GenericMenu menu)
        {
            foreach (T item in m_Choices)
            {
                bool isSelected = EqualityComparer<T>.Default.Equals(item, value);
                menu.AddItem(new GUIContent(GetListItemToDisplay(item)), isSelected,
                    () => ChangeValueFromMenu(item));
            }
        }

        private void ChangeValueFromMenu(T menuItem)
        {
            value = menuItem;
        }
    }
}
