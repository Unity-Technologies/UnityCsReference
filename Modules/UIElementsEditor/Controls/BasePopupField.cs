// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

        protected TextElement textElement
        {
            get { return m_TextElement; }
        }

        internal Func<TValueChoice, string> m_FormatSelectedValueCallback;
        internal Func<TValueChoice, string> m_FormatListItemCallback;

        // This is the value to display to the user
        internal abstract string GetValueToDisplay();

        internal abstract string GetListItemToDisplay(TValueType item);

        // This method is used when the menu is built to fill up all the choices.
        internal abstract void AddMenuItems(GenericMenu menu);

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

        public override void SetValueWithoutNotify(TValueType newValue)
        {
            base.SetValueWithoutNotify(newValue);
            ((INotifyValueChanged<string>)m_TextElement).SetValueWithoutNotify(GetValueToDisplay());
        }

        public string text
        {
            get { return m_TextElement.text; }
        }

        public new static readonly string ussClassName = "unity-base-popup-field";
        public static readonly string textUssClassName = ussClassName + "__text";
        public static readonly string arrowUssClassName = ussClassName + "__arrow";
        public new static readonly string labelUssClassName = ussClassName + "__label";
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
            var menu = new GenericMenu();
            AddMenuItems(menu);
            menu.DropDown(visualInput.worldBound);
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
    }
}
