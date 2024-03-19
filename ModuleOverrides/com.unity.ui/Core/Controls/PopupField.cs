// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Generic popup selection field.
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    public class PopupField<T> : BasePopupField<T, T>
    {
        /// <summary>
        /// Callback that provides a string representation used to display the selected value.
        /// </summary>
        public virtual Func<T, string> formatSelectedValueCallback
        {
            get { return m_FormatSelectedValueCallback; }
            set
            {
                m_FormatSelectedValueCallback = value;
                textElement.text = GetValueToDisplay();
            }
        }

        /// <summary>
        /// Callback that provides a string representation used to populate the popup menu.
        /// </summary>
        public virtual Func<T, string> formatListItemCallback
        {
            get { return m_FormatListItemCallback; }
            set { m_FormatListItemCallback = value; }
        }

        internal override string GetValueToDisplay()
        {
            if (m_FormatSelectedValueCallback != null)
                return m_FormatSelectedValueCallback(value);

            if (value != null)
                return UIElementsUtility.ParseMenuName(value.ToString());

            return string.Empty;
        }

        internal override string GetListItemToDisplay(T value)
        {
            if (m_FormatListItemCallback != null)
                return m_FormatListItemCallback(value);
            return (value != null && m_Choices.Contains(value)) ? value.ToString() : string.Empty;
        }

        /// <summary>
        /// The currently selected value in the popup menu.
        /// </summary>
        public override T value
        {
            get { return base.value; }
            set
            {
                m_Index = m_Choices?.IndexOf(value) ?? -1;
                base.value = value;
            }
        }

        public override void SetValueWithoutNotify(T newValue)
        {
            m_Index = m_Choices?.IndexOf(newValue) ?? -1;
            base.SetValueWithoutNotify(newValue);
        }

        internal const int kPopupFieldDefaultIndex = -1;
        private int m_Index = kPopupFieldDefaultIndex;
        /// <summary>
        /// The currently selected index in the popup menu.
        /// Setting the index will update the ::ref::value field and send a property change notification.
        /// </summary>
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

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-popup-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Construct a PopupField.
        /// </summary>
        public PopupField()
            : this(null)
        {}

        /// <summary>
        /// Construct a PopupField.
        /// </summary>
        public PopupField(string label = null)
            : base(label)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        /// <summary>
        /// Construct a PopupField.
        /// </summary>
        public PopupField(List<T> choices, T defaultValue, Func<T, string> formatSelectedValueCallback = null, Func<T, string> formatListItemCallback = null)
            : this(null, choices, defaultValue, formatSelectedValueCallback, formatListItemCallback)
        {
        }

        /// <summary>
        /// Construct a PopupField.
        /// </summary>
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

        /// <summary>
        /// Construct a PopupField.
        /// </summary>
        public PopupField(List<T> choices, int defaultIndex, Func<T, string> formatSelectedValueCallback = null, Func<T, string> formatListItemCallback = null)
            : this(null, choices, defaultIndex, formatSelectedValueCallback, formatListItemCallback) {}

        /// <summary>
        /// Construct a PopupField.
        /// </summary>
        public PopupField(string label, List<T> choices, int defaultIndex, Func<T, string> formatSelectedValueCallback = null, Func<T, string> formatListItemCallback = null)
            : this(label)
        {
            this.choices = choices;

            index = defaultIndex;

            this.formatListItemCallback = formatListItemCallback;
            this.formatSelectedValueCallback = formatSelectedValueCallback;
        }

        internal override void AddMenuItems(IGenericMenu menu)
        {
            if (menu == null)
            {
                throw new ArgumentNullException(nameof(menu));
            }

            if (m_Choices == null)
                return;

            foreach (T item in m_Choices)
            {
                bool isSelected = EqualityComparer<T>.Default.Equals(item, value);
                menu.AddItem(GetListItemToDisplay(item), isSelected,
                    () => ChangeValueFromMenu(item));
            }
        }

        private void ChangeValueFromMenu(T menuItem)
        {
			showMixedValue = false;
            value = menuItem;
        }
    }
}
