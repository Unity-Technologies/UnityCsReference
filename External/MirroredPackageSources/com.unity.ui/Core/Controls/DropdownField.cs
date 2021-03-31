using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A control that allows the user to pick a choice from a list of options.
    /// </summary>
    public class DropdownField : BaseField<string>
    {
        /// <summary>
        /// Instantiates a <see cref="DropdownField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<DropdownField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="DropdownField"/>.
        /// </summary>
        public new class UxmlTraits : BaseField<string>.UxmlTraits
        {
            UxmlIntAttributeDescription m_Index = new UxmlIntAttributeDescription { name = "index" };
            UxmlStringAttributeDescription m_Choices = new UxmlStringAttributeDescription() { name = "choices" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (DropdownField)ve;
                f.choices = ParseChoiceList(m_Choices.GetValueFromBag(bag, cc));
                f.index = m_Index.GetValueFromBag(bag, cc);
            }
        }

        internal List<string> m_Choices;
        TextElement m_TextElement;
        VisualElement m_ArrowElement;

        /// <summary>
        /// This is the text displayed.
        /// </summary>
        protected TextElement textElement
        {
            get { return m_TextElement; }
        }

        /// <summary>
        /// This is the text displayed to the user for the current selection of the popup.
        /// </summary>
        public string text
        {
            get { return m_TextElement.text; }
        }

        internal Func<string, string> m_FormatSelectedValueCallback;
        internal Func<string, string> m_FormatListItemCallback;

        // Set this callback to provide a specific implementation of the menu.
        internal Func<IGenericMenu> createMenuCallback = null;

        // This is the value to display to the user
        internal string GetValueToDisplay()
        {
            if (m_FormatSelectedValueCallback != null)
                return m_FormatSelectedValueCallback(value);
            return value ?? string.Empty;
        }

        internal string GetListItemToDisplay(string value)
        {
            if (m_FormatListItemCallback != null)
                return m_FormatListItemCallback(value);
            return (value != null && m_Choices.Contains(value)) ? value : string.Empty;
        }

        /// <summary>
        /// Callback that provides a string representation used to display the selected value.
        /// </summary>
        internal virtual Func<string, string> formatSelectedValueCallback
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
        internal virtual Func<string, string> formatListItemCallback
        {
            get { return m_FormatListItemCallback; }
            set { m_FormatListItemCallback = value; }
        }

        private int m_Index = -1;

        /// <summary>
        /// The currently selected index in the popup menu.
        /// </summary>
        public int index
        {
            get { return m_Index; }
            set
            {
                m_Index = value;

                if (m_Choices == null || value >= m_Choices.Count || value < 0)
                    this.value = null;
                else
                    this.value = m_Choices[m_Index];
            }
        }

        // Base selectors coming from BasePopupField
        internal static readonly string ussClassNameBasePopupField = "unity-base-popup-field";
        internal static readonly string textUssClassNameBasePopupField = ussClassNameBasePopupField + "__text";
        internal static readonly string arrowUssClassNameBasePopupField = ussClassNameBasePopupField + "__arrow";
        internal static readonly string labelUssClassNameBasePopupField = ussClassNameBasePopupField + "__label";
        internal static readonly string inputUssClassNameBasePopupField = ussClassNameBasePopupField + "__input";

        // Base selectors coming from PopupField
        internal static readonly string ussClassNamePopupField = "unity-popup-field";
        internal static readonly string labelUssClassNamePopupField = ussClassNamePopupField + "__label";
        internal static readonly string inputUssClassNamePopupField = ussClassNamePopupField + "__input";

        /// <summary>
        /// Construct a DropdownField.
        /// </summary>
        public DropdownField()
            : this(null) {}

        /// <summary>
        /// Construct a DropdownField.
        /// </summary>
        public DropdownField(string label)
            : base(label, null)
        {
            // BasePopupField constructor
            AddToClassList(ussClassNameBasePopupField);
            labelElement.AddToClassList(labelUssClassNameBasePopupField);

            m_TextElement = new PopupTextElement
            {
                pickingMode = PickingMode.Ignore
            };
            m_TextElement.AddToClassList(textUssClassNameBasePopupField);
            visualInput.AddToClassList(inputUssClassNameBasePopupField);
            visualInput.Add(m_TextElement);

            m_ArrowElement = new VisualElement();
            m_ArrowElement.AddToClassList(arrowUssClassNameBasePopupField);
            m_ArrowElement.pickingMode = PickingMode.Ignore;
            visualInput.Add(m_ArrowElement);

            choices = new List<string>();

            // PopupField constructor
            AddToClassList(ussClassNamePopupField);
            labelElement.AddToClassList(labelUssClassNamePopupField);
            visualInput.AddToClassList(inputUssClassNamePopupField);
        }

        /// <summary>
        /// Construct a DropdownField.
        /// </summary>
        public DropdownField(List<string> choices, string defaultValue, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : this(null, choices, defaultValue, formatSelectedValueCallback, formatListItemCallback) {}

        /// <summary>
        /// Construct a DropdownField.
        /// </summary>
        public DropdownField(string label, List<string> choices, string defaultValue, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : this(label)
        {
            if (defaultValue == null)
                throw new ArgumentNullException(nameof(defaultValue));

            this.choices = choices;
            SetValueWithoutNotify(defaultValue);

            this.formatListItemCallback = formatListItemCallback;
            this.formatSelectedValueCallback = formatSelectedValueCallback;
        }

        /// <summary>
        /// Construct a DropdownField.
        /// </summary>
        public DropdownField(List<string> choices, int defaultIndex, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : this(null, choices, defaultIndex, formatSelectedValueCallback, formatListItemCallback) {}

        /// <summary>
        /// Construct a DropdownField.
        /// </summary>
        public DropdownField(string label, List<string> choices, int defaultIndex, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : this(label)
        {
            this.choices = choices;

            index = defaultIndex;

            this.formatListItemCallback = formatListItemCallback;
            this.formatSelectedValueCallback = formatSelectedValueCallback;
        }

        // This method is used when the menu is built to fill up all the choices.
        internal void AddMenuItems(IGenericMenu menu)
        {
            if (menu == null)
            {
                throw new ArgumentNullException(nameof(menu));
            }

            if (m_Choices == null)
                return;

            foreach (var item in m_Choices)
            {
                bool isSelected = item == value;
                menu.AddItem(GetListItemToDisplay(item), isSelected,
                    () => ChangeValueFromMenu(item));
            }
        }

        private void ChangeValueFromMenu(string menuItem)
        {
            value = menuItem;
        }

        internal virtual List<string> choices
        {
            get => m_Choices;
            set => m_Choices = value;
        }

        /// <summary>
        /// The currently selected value in the popup menu.
        /// </summary>
        public override string value
        {
            get { return base.value; }
            set
            {
                m_Index = m_Choices?.IndexOf(value) ?? -1;
                base.value = value;
            }
        }

        /// <summary>
        /// Allow changing value without triggering any change event.
        /// </summary>
        /// <param name="newValue">The new value.</param>
        public override void SetValueWithoutNotify(string newValue)
        {
            m_Index = m_Choices?.IndexOf(newValue) ?? -1;

            base.SetValueWithoutNotify(newValue);
            ((INotifyValueChanged<string>)m_TextElement).SetValueWithoutNotify(GetValueToDisplay());
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt == null)
            {
                return;
            }

            var showPopupMenu = false;
            KeyDownEvent kde = (evt as KeyDownEvent);
            if (kde != null)
            {
                if ((kde.keyCode == KeyCode.Space) ||
                    (kde.keyCode == KeyCode.KeypadEnter) ||
                    (kde.keyCode == KeyCode.Return))
                {
                    showPopupMenu = true;
                }
            }
            else if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
            {
                var mde = (MouseDownEvent)evt;
                if (visualInput.ContainsPoint(visualInput.WorldToLocal(mde.mousePosition)))
                {
                    showPopupMenu = true;
                }
            }

            if (showPopupMenu)
            {
                ShowMenu();
                evt.StopPropagation();
            }
        }

        private void ShowMenu()
        {
            IGenericMenu menu;
            if (createMenuCallback != null)
            {
                menu = createMenuCallback.Invoke();
            }
            else
            {
                menu = elementPanel?.contextType == ContextType.Player ? new GenericDropdownMenu() : DropdownUtility.CreateDropdown();
            }

            AddMenuItems(menu);
            menu.DropDown(visualInput.worldBound, this, true);
        }

        private class PopupTextElement : TextElement
        {
            protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight,
                MeasureMode heightMode)
            {
                var textToMeasure = text;
                if (string.IsNullOrEmpty(textToMeasure))
                {
                    textToMeasure = " ";
                }

                return MeasureTextSize(textToMeasure, desiredWidth, widthMode, desiredHeight, heightMode);
            }
        }

        protected override void UpdateMixedValueContent()
        {
            if (showMixedValue)
            {
                textElement.text = mixedValueString;
            }

            textElement.EnableInClassList(mixedValueLabelUssClassName, showMixedValue);
        }
    }
}
