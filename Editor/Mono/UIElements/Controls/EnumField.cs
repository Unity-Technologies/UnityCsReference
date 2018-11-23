// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class EnumField : BaseField<Enum>
    {
        public new class UxmlFactory : UxmlFactory<EnumField, UxmlTraits> {}

        public new class UxmlTraits : BaseField<Enum>.UxmlTraits
        {
            UxmlStringAttributeDescription m_Type = new UxmlStringAttributeDescription { name = "type", use = UxmlAttributeDescription.Use.Required };
            UxmlStringAttributeDescription m_Value = new UxmlStringAttributeDescription { name = "value" };

            internal static void ParseEnumValues(EnumField enumField, string typeValue, string v)
            {
                // Only works for the currently running assembly
                enumField.m_EnumType = Type.GetType(typeValue);
                if (enumField.m_EnumType != null)
                {
                    if (!Enum.IsDefined(enumField.m_EnumType, v))
                    {
                        Debug.LogErrorFormat("Could not parse value of '{0}', because it isn't defined in the {1} enum.", v, enumField.m_EnumType.FullName);
                        enumField.SetValueWithoutNotify(null);
                    }
                    else
                    {
                        enumField.SetValueWithoutNotify((Enum)Enum.Parse(enumField.m_EnumType, v));
                    }
                }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                EnumField enumField = (EnumField)ve;

                ParseEnumValues(enumField, m_Type.GetValueFromBag(bag, cc), m_Value.GetValueFromBag(bag, cc));
            }
        }

        private Type m_EnumType;
        private TextElement m_TextElement;

        public string text
        {
            get { return m_TextElement.text; }
        }

        private void Initialize(Enum defaultValue)
        {
            m_TextElement = new TextElement();
            m_TextElement.AddToClassList(textUssClassName);
            m_TextElement.pickingMode = PickingMode.Ignore;
            visualInput.Add(m_TextElement);
            if (defaultValue != null)
            {
                Init(defaultValue);
            }
        }

        public new static readonly string ussClassName = "unity-enum-field";
        public static readonly string textUssClassName = ussClassName + "__text";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public EnumField()
            : this((string)null) {}

        public EnumField(Enum defaultValue)
            : this(null, defaultValue) {}

        public EnumField(string label, Enum defaultValue = null)
            : base(label, null)
        {
            AddToClassList(ussClassName);
            visualInput.AddToClassList(inputUssClassName);
            Initialize(defaultValue);
        }

        public void Init(Enum defaultValue)
        {
            if (defaultValue == null)
            {
                throw new ArgumentNullException(nameof(defaultValue));
            }

            m_EnumType = defaultValue.GetType();
            SetValueWithoutNotify(defaultValue);
        }

        public override void SetValueWithoutNotify(Enum newValue)
        {
            if (rawValue != newValue)
            {
                base.SetValueWithoutNotify(newValue);
                m_TextElement.text = ObjectNames.NicifyVariableName(rawValue.ToString());
            }
        }

        protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt == null)
            {
                return;
            }

            if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse || (evt as KeyDownEvent)?.character == '\n')
            {
                ShowMenu();
                evt.StopPropagation();
            }
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

            var menuPosition = new Vector2(visualInput.layout.xMin, visualInput.layout.height);
            menuPosition = this.LocalToWorld(menuPosition);
            var menuRect = new Rect(menuPosition, Vector2.zero);
            menu.DropDown(menuRect);
        }

        private void ChangeValueFromMenu(object menuItem)
        {
            value = menuItem as Enum;
        }
    }
}
