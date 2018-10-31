// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    /// <summary>
    /// This is the base class for all the popup field elements.
    /// TValue and TChoice can be different, see MaskField,
    ///   or the same, see PopupField
    /// </summary>
    /// <typeparam name="TValue"> Used for the BaseField</typeparam>
    /// <typeparam name="TChoice"> Used for the choices list</typeparam>
    public abstract class BasePopupField<TValue, TChoice> : BaseField<TValue>
    {
        internal List<TChoice> m_Choices;
        protected TextElement m_TextElement;

        internal Func<TChoice, string> m_FormatSelectedValueCallback;
        internal Func<TChoice, string> m_FormatListItemCallback;

        // This is the value to display to the user
        internal abstract string GetValueToDisplay();

        internal abstract string GetListItemToDisplay(TValue item);

        // This method is used when the menu is built to fill up all the choices.
        internal abstract void AddMenuItems(GenericMenu menu);

        internal virtual List<TChoice> choices
        {
            get { return m_Choices; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("choices", "choices can't be null");

                m_Choices = value;
            }
        }

        public override void SetValueWithoutNotify(TValue newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_TextElement.text = GetValueToDisplay();
        }

        public string text
        {
            get { return m_TextElement.text; }
        }

        protected BasePopupField()
        {
            m_TextElement = new TextElement();
            m_TextElement.pickingMode = PickingMode.Ignore;
            Add(m_TextElement);
            AddToClassList("popupField");

            choices = new List<TChoice>();
        }

        protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse) ||
                ((evt.GetEventTypeId() == KeyDownEvent.TypeId()) && ((evt as KeyDownEvent)?.character == '\n') || ((evt as KeyDownEvent)?.character == ' ')))
            {
                ShowMenu();
                evt.StopPropagation();
            }
        }

        private void ShowMenu()
        {
            var menu = new GenericMenu();
            AddMenuItems(menu);
            menu.DropDown(worldBound);
        }
    }
}
