// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class EnumField : BaseTextControl<Enum>
    {
        private Type m_EnumType;

        private Enum m_Value;
        public override Enum value
        {
            get { return m_Value; }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    text = ObjectNames.NicifyVariableName(m_Value.ToString());
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        public EnumField() {}

        public EnumField(Enum defaultValue)
        {
            Init(defaultValue);
        }

        public void Init(Enum defaultValue)
        {
            m_EnumType = defaultValue.GetType();
            value = defaultValue;
        }

        public override void SetValueAndNotify(Enum newValue)
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

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse || (evt as KeyDownEvent)?.character == '\n')
                ShowMenu();
        }

        private void ShowMenu()
        {
            if (m_EnumType == null)
                return;

            var menu = new GenericMenu();

            foreach (Enum item in Enum.GetValues(m_EnumType))
            {
                bool isSelected = item.CompareTo(value) == 0;
                string label = ObjectNames.NicifyVariableName(item.ToString());
                menu.AddItem(new GUIContent(label), isSelected,
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
