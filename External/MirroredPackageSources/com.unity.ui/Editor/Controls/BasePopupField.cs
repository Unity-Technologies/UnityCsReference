using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// This is the base class for all the popup field elements.
    /// TValue and TChoice can be different, see MaskField,
    ///   or the same, see PopupField
    /// </summary>
    /// <typeparam name="TValueType"> Used for the BaseField</typeparam>
    /// <typeparam name="TValueChoice"> Used for the choices list</typeparam>
    public abstract class BasePopupField<TValueType, TValueChoice> : BaseField<TValueType>
    {
        internal List<TValueChoice> m_Choices;
        TextElement m_TextElement;
        VisualElement m_ArrowElement;

        /// <summary>
        /// This is the text displayed.
        /// </summary>
        protected TextElement textElement
        {
            get { return m_TextElement; }
        }

        internal Func<TValueChoice, string> m_FormatSelectedValueCallback;
        internal Func<TValueChoice, string> m_FormatListItemCallback;

        // Set this callback to provide a specific implementation of the menu.
        internal Func<IGenericMenu> createMenuCallback;

        // This is the value to display to the user
        internal abstract string GetValueToDisplay();

        internal abstract string GetListItemToDisplay(TValueType item);

        // This method is used when the menu is built to fill up all the choices.
        internal abstract void AddMenuItems(IGenericMenu menu);

        internal virtual List<TValueChoice> choices
        {
            get { return m_Choices; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                m_Choices = value;
            }
        }

        /// <summary>
        /// Allow changing value without triggering any change event.
        /// </summary>
        /// <param name="newValue">The new value.</param>
        public override void SetValueWithoutNotify(TValueType newValue)
        {
            base.SetValueWithoutNotify(newValue);
            ((INotifyValueChanged<string>)m_TextElement).SetValueWithoutNotify(GetValueToDisplay());
        }

        /// <summary>
        /// This is the text displayed to the user for the current selection of the popup.
        /// </summary>
        public string text
        {
            get { return m_TextElement.text; }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-base-popup-field";
        /// <summary>
        /// USS class name of text elements in elements of this type.
        /// </summary>
        public static readonly string textUssClassName = ussClassName + "__text";
        /// <summary>
        /// USS class name of arrow indicators in elements of this type.
        /// </summary>
        public static readonly string arrowUssClassName = ussClassName + "__arrow";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";


        internal BasePopupField()
            : this(null) {}

        internal BasePopupField(string label)
            : base(label, null)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);

            m_TextElement = new PopupTextElement
            {
                pickingMode = PickingMode.Ignore
            };
            m_TextElement.AddToClassList(textUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            visualInput.Add(m_TextElement);

            m_ArrowElement = new VisualElement();
            m_ArrowElement.AddToClassList(arrowUssClassName);
            m_ArrowElement.pickingMode = PickingMode.Ignore;
            visualInput.Add(m_ArrowElement);

            choices = new List<TValueChoice>();

            RegisterCallback<PointerDownEvent>(OnPointerDownEvent);
        }

        void OnPointerDownEvent(PointerDownEvent evt)
        {
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                if (visualInput.ContainsPoint(visualInput.WorldToLocal(evt.originalMousePosition)))
                {
                    ShowMenu();
                    evt.StopPropagation();
                }
            }
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt == null)
            {
                return;
            }

            KeyDownEvent kde = (evt as KeyDownEvent);
            if (kde != null)
            {
                if ((kde.keyCode == KeyCode.Space) ||
                    (kde.keyCode == KeyCode.KeypadEnter) ||
                    (kde.keyCode == KeyCode.Return))
                {
                    ShowMenu();
                    evt.StopPropagation();
                }
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
                menu = elementPanel?.contextType == ContextType.Player ? new GenericDropdownMenu() : DropdownMenu.CreateDropdown();
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
