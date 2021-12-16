using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class PopupField<T> : BasePopupField<T, T>
    {
        public virtual Func<T, string> formatSelectedValueCallback
        {
            get { return m_FormatSelectedValueCallback; }
            set
            {
                m_FormatSelectedValueCallback = value;
                textElement.text = GetValueToDisplay();
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
            return (value != null) ? value.ToString() : string.Empty;
        }

        internal override string GetListItemToDisplay(T value)
        {
            if (m_FormatListItemCallback != null)
                return m_FormatListItemCallback(value);
            return (value != null && m_Choices.Contains(value)) ? value.ToString() : string.Empty;
        }

        public override T value
        {
            get { return base.value; }
            set
            {
                m_Index = m_Choices.IndexOf(value);
                base.value = value;
            }
        }

        public override void SetValueWithoutNotify(T newValue)
        {
            m_Index = m_Choices.IndexOf(newValue);
            base.SetValueWithoutNotify(newValue);
        }

        internal const int kPopupFieldDefaultIndex = -1;
        private int m_Index = kPopupFieldDefaultIndex;
        public int index
        {
            get { return m_Index; }
            set
            {
                if (value != m_Index)
                {
                    m_Index = value;
                    if (m_Index >= 0 && m_Index < m_Choices.Count)
                        this.value = m_Choices[m_Index];
                    else
                        this.value = default(T);
                }
            }
        }

        public new static readonly string ussClassName = "unity-popup-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public PopupField()
            : this(null)
        {}

        public PopupField(string label = null)
            : base(label)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        public PopupField(List<T> choices, T defaultValue, Func<T, string> formatSelectedValueCallback = null, Func<T, string> formatListItemCallback = null)
            : this(null, choices, defaultValue, formatSelectedValueCallback, formatListItemCallback)
        {
        }

        public PopupField(string label, List<T> choices, T defaultValue, Func<T, string> formatSelectedValueCallback = null, Func<T, string> formatListItemCallback = null)
            : this(label)
        {
            if (defaultValue == null)
                throw new ArgumentNullException(nameof(defaultValue));

            this.choices = choices;
            if (!m_Choices.Contains(defaultValue))
                throw new ArgumentException(string.Format("Default value {0} is not present in the list of possible values", defaultValue));

            SetValueWithoutNotify(defaultValue);

            this.formatListItemCallback = formatListItemCallback;
            this.formatSelectedValueCallback = formatSelectedValueCallback;
        }

        public PopupField(List<T> choices, int defaultIndex, Func<T, string> formatSelectedValueCallback = null, Func<T, string> formatListItemCallback = null)
            : this(null, choices, defaultIndex, formatSelectedValueCallback, formatListItemCallback) {}

        public PopupField(string label, List<T> choices, int defaultIndex, Func<T, string> formatSelectedValueCallback = null, Func<T, string> formatListItemCallback = null)
            : this(label)
        {
            this.choices = choices;

            index = defaultIndex;

            this.formatListItemCallback = formatListItemCallback;
            this.formatSelectedValueCallback = formatSelectedValueCallback;
        }

        internal override void AddMenuItems(GenericMenu menu)
        {
            if (menu == null)
            {
                throw new ArgumentNullException(nameof(menu));
            }

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
