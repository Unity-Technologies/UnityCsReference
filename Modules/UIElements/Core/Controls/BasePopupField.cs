// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This is the base class for all the popup field elements.
    /// TValue and TChoice can be different, see MaskField,
    ///   or the same, see PopupField
    /// </summary>
    /// <typeparam name="TValueType"> Used for the BaseField</typeparam>
    /// <typeparam name="TValueChoice"> Used for the choices list</typeparam>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    public abstract class BasePopupField<TValueType, TValueChoice> : BaseField<TValueType>
    {
        internal static readonly BindingId choicesProperty = nameof(choices);
        internal static readonly BindingId textProperty = nameof(text);

        internal List<TValueChoice> m_Choices;
        TextElement m_TextElement;
        VisualElement m_ArrowElement;
        IVisualElementScheduledItem m_ScheduledShowMenuItem;

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
        internal IGenericMenu m_GenericMenu;
        internal bool m_AutoCloseMenu = true;

        // This is the value to display to the user
        internal abstract string GetValueToDisplay();

        internal abstract string GetListItemToDisplay(TValueType item);

        // This method is used when the menu is built to fill up all the choices.
        internal abstract void AddMenuItems(IGenericMenu menu);

        /// <summary>
        /// The list of choices to display in the popup menu.
        /// </summary>
        [CreateProperty]
        public virtual List<TValueChoice> choices
        {
            get { return m_Choices; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                m_Choices = value;

                // Make sure to update the text displayed
                SetValueWithoutNotify(rawValue);
                NotifyPropertyChanged(choicesProperty);
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
        [CreateProperty(ReadOnly = true)]
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
            RegisterCallback<PointerUpEvent>(OnPointerUpEvent);
            RegisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
            RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == (int)MouseButton.LeftMouse)
                    e.StopPropagation();
            });
            RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
        }

        void OnPointerDownEvent(PointerDownEvent evt)
        {
            ProcessPointerDown(evt);
        }

        void OnPointerUpEvent(PointerUpEvent evt)
        {
            if (evt.button == (int)MouseButton.LeftMouse && ContainsPointer(evt.pointerId))
            {
                // if the menu is not showing due to inputs on the same frame, force show it (UUM-91306)
                if (m_ScheduledShowMenuItem != null && m_ScheduledShowMenuItem.isActive)
                {
                    m_ScheduledShowMenuItem.Pause();
                    ShowMenu();
                }
                // Prevent propagation to other elements (UUM-85620)
                evt.StopPropagation();
            }
        }

        void OnPointerMoveEvent(PointerMoveEvent evt)
        {
            // Support cases where PointerMove corresponds to a MouseDown or MouseUp event with multiple buttons.
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                if ((evt.pressedButtons & (1 << (int)MouseButton.LeftMouse)) != 0)
                {
                    ProcessPointerDown(evt);
                }
            }
        }

        bool ContainsPointer(int pointerId)
        {
            var elementUnderPointer = elementPanel.GetTopElementUnderPointer(pointerId);
            return this == elementUnderPointer || visualInput == elementUnderPointer;
        }

        void ProcessPointerDown<T>(PointerEventBase<T> evt) where T : PointerEventBase<T>, new()
        {
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                if (ContainsPointer(evt.pointerId))
                {
                    m_ScheduledShowMenuItem = schedule.Execute(ShowMenu);
                    evt.StopPropagation();
                }
            }
        }

        void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            ShowMenu();
            evt.StopPropagation();
        }

        // Used in tests
        internal void ShowMenu()
        {
            m_GenericMenu = createMenuCallback != null ? createMenuCallback.Invoke() : elementPanel.CreateMenu();
            AddMenuItems(m_GenericMenu);
            m_GenericMenu.DropDown(visualInput.worldBound, this, true);
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
                ((INotifyValueChanged<string>)m_TextElement).SetValueWithoutNotify(mixedValueString);
            }

            textElement.EnableInClassList(mixedValueLabelUssClassName, showMixedValue);
        }
    }
}
