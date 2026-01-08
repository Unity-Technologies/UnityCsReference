// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    internal sealed class BorderColorFoldout : StyleFoldoutField<ColorField>, INotifyCompositeStylePropertyChanged<StyleColor>
    {
        static readonly string k_FieldClassName = FoldoutFieldPropertyName + "__color-field";
        static readonly string k_MixedValueLineClassName = FoldoutFieldPropertyName + "__mixed-value-line";
        internal static readonly string internalColorFieldName = "unity-internal-color-field";

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleFoldoutField<ColorField>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleFoldoutField<ColorField>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
            }

            public override object CreateInstance() => new BorderColorFoldout();
        }

        const string k_TopPropertyName = "borderTopColor";
        const string k_RightPropertyName = "borderRightColor";
        const string k_BottomPropertyName = "borderBottomColor";
        const string k_LeftPropertyName = "borderLeftColor";

        public static readonly BindingId topProperty = nameof(top);
        public static readonly BindingId rightProperty = nameof(right);
        public static readonly BindingId bottomProperty = nameof(bottom);
        public static readonly BindingId leftProperty = nameof(left);

        private readonly VisualElement m_MixedValueLine;

        public bool isMixed
        {
            get
            {
                if (fields.Count == 0)
                    return true;

                var allSame = fields.TrueForAll(f => f.value == fields[0].value);
                return !allSame;
            }
        }

        StyleColor m_Top;
        StyleColor m_Right;
        StyleColor m_Bottom;
        StyleColor m_Left;

        public StyleColorField topField { get; }
        public StyleColorField rightField { get; }
        public StyleColorField bottomField { get; }
        public StyleColorField leftField { get; }

        public List<StyleColorField> fields => new()
        {
            topField,
            rightField,
            bottomField,
            leftField
        };

        [CreateProperty]
        public StyleColor top
        {
            get => m_Top;
            set
            {
                if (m_Top.Equals(value))
                    return;

                var previousValue = m_Top;
                m_Top = value;
                Refresh();
                NotifyStylePropertyChanged(topProperty, previousValue, m_Top);
            }
        }

        [CreateProperty]
        public StyleColor right
        {
            get => m_Right;
            set
            {
                if (m_Right.Equals(value))
                    return;

                var previousValue = m_Right;
                m_Right = value;
                Refresh();
                NotifyStylePropertyChanged(rightProperty, previousValue, m_Right);
            }
        }

        [CreateProperty]
        public StyleColor bottom
        {
            get => m_Bottom;
            set
            {
                if (m_Bottom.Equals(value))
                    return;

                var previousValue = m_Bottom;
                m_Bottom = value;
                Refresh();
                NotifyStylePropertyChanged(bottomProperty, previousValue, m_Bottom);
            }
        }

        [CreateProperty]
        public StyleColor left
        {
            get => m_Left;
            set
            {
                if (m_Left.Equals(value))
                    return;

                var previousValue = m_Left;
                m_Left = value;
                Refresh();
                NotifyStylePropertyChanged(leftProperty, previousValue, m_Left);
            }
        }

        public BorderColorFoldout()
            : this("Color") { }

        public BorderColorFoldout(string text)
            : base(text)
        {
            var topRow = new OverrideRow() { name = k_TopPropertyName };
            topField = new StyleColorField("Top") { name = k_TopPropertyName, classList = { TextField.alignedFieldUssClassName } };
            topRow.Add(topField);
            Add(topRow);

            var rightRow = new OverrideRow() { name = k_RightPropertyName };
            rightField = new StyleColorField("Right") { name = k_RightPropertyName, classList = { TextField.alignedFieldUssClassName } };
            rightRow.Add(rightField);
            Add(rightRow);

            var bottomRow = new OverrideRow() { name = k_BottomPropertyName };
            bottomField = new StyleColorField("Bottom") { name = k_BottomPropertyName, classList = { TextField.alignedFieldUssClassName } };
            bottomRow.Add(bottomField);
            Add(bottomRow);

            var leftRow = new OverrideRow() { name = k_LeftPropertyName };
            leftField = new StyleColorField("Left") { name = k_LeftPropertyName, classList = { TextField.alignedFieldUssClassName } };
            leftRow.Add(leftField);
            Add(leftRow);

            topField.tooltip = "USS property: border-top-color\n\nColor of the element's top border.";
            rightField.tooltip = "USS property: border-right-color\n\nColor of the element's right border.";
            bottomField.tooltip = "USS property: border-bottom-color\n\nColor of the element's bottom border.";
            leftField.tooltip = "USS property: border-left-color\n\nColor of the element's left border.";

            topField.AddValidation(new Syntax("border-top-width"));
            rightField.AddValidation(new Syntax("border-right-width"));
            bottomField.AddValidation(new Syntax("border-bottom-width"));
            leftField.AddValidation(new Syntax("border-left-width"));

            topField.RegisterValueChangedCallback((e) => top = e.newValue);
            rightField.RegisterValueChangedCallback((e) => right = e.newValue);
            bottomField.RegisterValueChangedCallback((e) => bottom = e.newValue);
            leftField.RegisterValueChangedCallback((e) => left = e.newValue);

            headerInputField.AddToClassList(TextField.alignedFieldUssClassName);
            headerInputField.name = k_FieldClassName;
            headerInputField.AddToClassList(k_FieldClassName);
            headerInputField.RegisterValueChangedCallback(OnHeaderValueChange);

            m_MixedValueLine = new VisualElement();
            m_MixedValueLine.name = k_MixedValueLineClassName;
            m_MixedValueLine.AddToClassList(k_MixedValueLineClassName);
            headerInputField.Q(internalColorFieldName).hierarchy.Add(m_MixedValueLine);

            UpdateFromChildFields();
        }

        protected override void ForwardDependentPropertiesTracking(TrackPropertyEvent evt)
        {
            base.ForwardDependentPropertiesTracking(evt);
            var target = evt.propertyName switch
            {
                k_TopPropertyName => topField,
                k_RightPropertyName => rightField,
                k_BottomPropertyName => bottomField,
                k_LeftPropertyName => leftField,
                _ => default(VisualElement)
            };

            if (target == null)
                return;

            var subEvent = TrackPropertyEvent.GetPooled(evt.provider,  evt.propertyName);
            subEvent.target = target;
            target.SendEvent(subEvent);
            evt.StopImmediatePropagation();
        }

        protected override ColorField CreateHeaderInputElement()
        {
            return new ColorField();
        }

        public override void UpdateFromChildFields()
        {
            for (int i = 0; i < fields.Count; ++i)
            {
                var value = GetCommonValueFromChildFields();
                headerInputField.SetValueWithoutNotify(value);

                m_MixedValueLine.style.display = isMixed ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public Color GetCommonValueFromChildFields()
        {
            return !isMixed ? topField.value.value : Color.white;
        }

        void OnHeaderValueChange(ChangeEvent<Color> evt)
        {
            var newValue = evt.newValue;

            foreach (var f in fields)
            {
                f.value = newValue;
            }
            UpdateFromChildFields();
            evt.StopPropagation();
        }

        protected override void Refresh()
        {
            topField.SetValueWithoutNotify(top);
            rightField.SetValueWithoutNotify(right);
            bottomField.SetValueWithoutNotify(bottom);
            leftField.SetValueWithoutNotify(left);
            base.Refresh();
        }

        public void SetValue(BindingId id, StyleColor v, bool notify)
        {
            if (id == topProperty)
            {
                if (notify)
                    top = v;
                else
                    m_Top = v;
            }
            else if (id == rightProperty)
            {
                if (notify)
                    right = v;
                else
                    m_Right = v;
            }
            else if (id == bottomProperty)
            {
                if (notify)
                    bottom = v;
                else
                    m_Bottom = v;
            }
            else if (id == leftProperty)
            {
                if (notify)
                    left = v;
                else
                    m_Left = v;
            }

            if (!notify)
                Refresh();
        }

        public void NotifyStylePropertyChanged(BindingId id, StyleColor previousValue, StyleColor newValue)
        {
            this.NotifyStylePropertyChanged(this, id, previousValue, newValue);
            NotifyPropertyChanged(id);
        }
    }
}
