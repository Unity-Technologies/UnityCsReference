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
#pragma warning disable 414
            private UxmlTypeAttributeDescription<Enum> m_Type = EnumFieldHelpers.type;
            private UxmlStringAttributeDescription m_Value = EnumFieldHelpers.value;
            private UxmlBoolAttributeDescription m_IncludeObsoleteValues = EnumFieldHelpers.includeObsoleteValues;
#pragma warning restore 414

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                Enum resEnumValue;
                bool resIncludeObsoleteValues;
                if (EnumFieldHelpers.ExtractValue(bag, cc, out resEnumValue, out resIncludeObsoleteValues))
                {
                    EnumField enumField = (EnumField)ve;
                    enumField.Init(resEnumValue, resIncludeObsoleteValues);
                }
            }
        }

        private Type m_EnumType;
        private TextElement m_TextElement;
        private VisualElement m_ArrowElement;
        private EnumData m_EnumData;

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

            m_ArrowElement = new VisualElement();
            m_ArrowElement.AddToClassList(arrowUssClassName);
            m_ArrowElement.pickingMode = PickingMode.Ignore;
            visualInput.Add(m_ArrowElement);
            if (defaultValue != null)
            {
                Init(defaultValue);
            }
        }

        public new static readonly string ussClassName = "unity-enum-field";
        public static readonly string textUssClassName = ussClassName + "__text";
        public static readonly string arrowUssClassName = ussClassName + "__arrow";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public EnumField()
            : this((string)null) {}

        public EnumField(Enum defaultValue)
            : this(null, defaultValue) {}

        public EnumField(string label, Enum defaultValue = null)
            : base(label, null)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            Initialize(defaultValue);
        }

        public void Init(Enum defaultValue)
        {
            Init(defaultValue, false);
        }

        public void Init(Enum defaultValue, bool includeObsoleteValues)
        {
            if (defaultValue == null)
            {
                throw new ArgumentNullException(nameof(defaultValue));
            }

            m_EnumType = defaultValue.GetType();
            m_EnumData = EnumDataUtility.GetCachedEnumData(m_EnumType, !includeObsoleteValues);

            SetValueWithoutNotify(defaultValue);
        }

        public override void SetValueWithoutNotify(Enum newValue)
        {
            if (rawValue != newValue)
            {
                base.SetValueWithoutNotify(newValue);

                if (m_EnumType == null)
                    return;

                int idx = Array.IndexOf(m_EnumData.values, newValue);

                if (idx >= 0 & idx < m_EnumData.values.Length)
                {
                    m_TextElement.text = m_EnumData.displayNames[idx];
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
            var showEnumMenu = false;
            KeyDownEvent kde = (evt as KeyDownEvent);
            if (kde != null)
            {
                if ((kde.keyCode == KeyCode.Space) ||
                    (kde.keyCode == KeyCode.KeypadEnter) ||
                    (kde.keyCode == KeyCode.Return))
                {
                    showEnumMenu = true;
                }
            }
            else if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
            {
                var mde = (MouseDownEvent)evt;
                if (visualInput.ContainsPoint(visualInput.WorldToLocal(mde.mousePosition)))
                {
                    showEnumMenu = true;
                }
            }

            if (showEnumMenu)
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

            int selectedIndex = Array.IndexOf(m_EnumData.values, value);

            for (int i = 0; i < m_EnumData.values.Length; ++i)
            {
                bool isSelected = selectedIndex == i;
                menu.AddItem(new GUIContent(m_EnumData.displayNames[i]), isSelected,
                    contentView => ChangeValueFromMenu(contentView),
                    m_EnumData.values[i]);
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
