// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class EnumField : BaseTextElement, INotifyValueChanged<Enum>
    {
        private Type m_EnumType;

        private Enum m_Value;
        public Enum value
        {
            get { return m_Value; }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    text = m_Value.ToString();
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        public EnumField(Enum defaultValue)
        {
            m_EnumType = defaultValue.GetType();
            value = defaultValue;
        }

        public void SetValueAndNotify(Enum newValue)
        {
            if (value != newValue)
            {
                using (ChangeEvent<Enum> evt = ChangeEvent<Enum>.GetPooled(value, newValue))
                {
                    evt.target = this;
                    value = newValue;
                    UIElementsUtility.eventDispatcher.DispatchEvent(evt, panel);
                }
            }
        }

        public void OnValueChanged(EventCallback<ChangeEvent<Enum>> callback)
        {
            RegisterCallback(callback);
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == MouseDownEvent.TypeId())
                OnMouseDown();
        }

        private void OnMouseDown()
        {
            var menu = new GenericMenu();

            foreach (Enum item in Enum.GetValues(m_EnumType))
            {
                bool isSelected = item.CompareTo(value) == 0;
                menu.AddItem(new GUIContent(item.ToString()), isSelected,
                    contentView => ChangeValueFromMenu(contentView),
                    item);
            }

            var menuPosition = new Vector2(0.0f, layout.height);
            menuPosition = this.LocalToWorld(menuPosition);
            var menuRect = new Rect(menuPosition, Vector2.zero);
            menu.DropDown(menuRect);
        }

        private void ChangeValueFromMenu(object menuItem)
        {
            SetValueAndNotify(menuItem as Enum);
        }
    }
}
