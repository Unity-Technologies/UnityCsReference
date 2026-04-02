// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    internal sealed class BorderColorFoldout : StyleFoldoutField<ColorField>
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

        public BorderColorFoldout()
            : this("Color") { }

        public BorderColorFoldout(string text)
            : base(text)
        {
            var topRow = new OverrideRow() { name = k_TopPropertyName };
            topField = new StyleColorField("Top") { name = k_TopPropertyName }.WithClassList(TextField.alignedFieldUssClassName);
            topField.SetBinding("value", new StylePropertyBinding(k_TopPropertyName));
            topRow.Add(topField);
            Add(topRow);

            var rightRow = new OverrideRow() { name = k_RightPropertyName };
            rightField = new StyleColorField("Right") { name = k_RightPropertyName }.WithClassList(TextField.alignedFieldUssClassName);
            rightField.SetBinding("value", new StylePropertyBinding(k_RightPropertyName));
            rightRow.Add(rightField);
            Add(rightRow);

            var bottomRow = new OverrideRow() { name = k_BottomPropertyName };
            bottomField = new StyleColorField("Bottom") { name = k_BottomPropertyName }.WithClassList(TextField.alignedFieldUssClassName);
            bottomField.SetBinding("value", new StylePropertyBinding(k_BottomPropertyName));
            bottomRow.Add(bottomField);
            Add(bottomRow);

            var leftRow = new OverrideRow() { name = k_LeftPropertyName };
            leftField = new StyleColorField("Left") { name = k_LeftPropertyName }.WithClassList(TextField.alignedFieldUssClassName);
            leftField.SetBinding("value", new StylePropertyBinding(k_LeftPropertyName));
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

            topField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleColor>.valueProperty ||
                    e.property == enabledSelfProperty)
                    UpdateFromChildFields();
            });
            rightField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleColor>.valueProperty ||
                    e.property == enabledSelfProperty)
                    UpdateFromChildFields();
            });
            bottomField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleColor>.valueProperty ||
                    e.property == enabledSelfProperty)
                    UpdateFromChildFields();
            });
            leftField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleColor>.valueProperty ||
                    e.property == enabledSelfProperty)
                    UpdateFromChildFields();
            });

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
            var shouldBeEnabled = true;
            for (int i = 0; i < fields.Count; ++i)
            {
                shouldBeEnabled &= fields[i].enabledSelf;
                var value = GetCommonValueFromChildFields();
                headerInputField.SetValueWithoutNotify(value);

                m_MixedValueLine.style.display = isMixed ? DisplayStyle.Flex : DisplayStyle.None;
            }

            enabledSelf = shouldBeEnabled;
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

    }
}
