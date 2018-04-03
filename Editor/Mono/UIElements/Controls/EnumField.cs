// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class EnumField : BaseTextControl<Enum>
    {
        public class EnumFieldFactory : UxmlFactory<EnumField, EnumFieldUxmlTraits> {}

        public class EnumFieldUxmlTraits : BaseTextControlUxmlTraits
        {
            UxmlStringAttributeDescription m_Type;
            UxmlStringAttributeDescription m_Value;

            public EnumFieldUxmlTraits()
            {
                m_Type = new UxmlStringAttributeDescription { name = "type", use = UxmlAttributeDescription.Use.Required};
                m_Value = new UxmlStringAttributeDescription { name = "value" };
            }

            public override IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription
            {
                get
                {
                    foreach (var attr in base.uxmlAttributesDescription)
                    {
                        yield return attr;
                    }

                    yield return m_Type;
                    yield return m_Value;
                }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                EnumField enumField = (EnumField)ve;
                // Only works for the currently running assembly
                enumField.m_EnumType = Type.GetType(m_Type.GetValueFromBag(bag));
                if (enumField.m_EnumType != null)
                {
                    string v = m_Value.GetValueFromBag(bag);

                    if (!Enum.IsDefined(enumField.m_EnumType, v))
                    {
                        Debug.LogErrorFormat("Could not parse value of '{0}', because it isn't defined in the {1} enum.", v, enumField.m_EnumType.FullName);
                        enumField.value = null;
                    }
                    else
                    {
                        enumField.value = (Enum)Enum.Parse(enumField.m_EnumType, v);
                    }
                }
            }
        }

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
