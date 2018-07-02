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
        internal override string GetValueToDisplay()
        {
            return m_Value.ToString();
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

        protected PopupField(List<T> choices)
        {
            this.choices = choices;
        }

        public PopupField(List<T> choices, T defaultValue) :
            this(choices)
        {
            if (defaultValue == null)
                throw new ArgumentNullException("defaultValue", "defaultValue can't be null");

            if (!m_Choices.Contains(defaultValue))
                throw new ArgumentException(string.Format("Default value {0} is not present in the list of possible values", defaultValue));

            // Note: idx will be set when setting value
            value = defaultValue;
        }

        public PopupField(List<T> choices, int defaultIndex) :
            this(choices)
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
                menu.AddItem(new GUIContent(item.ToString()), isSelected,
                    () => ChangeValueFromMenu(item));
            }
        }

        private void ChangeValueFromMenu(T menuItem)
        {
            value = menuItem;
        }
    }
}
