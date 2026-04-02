// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    internal sealed class BorderRadiusFoldout : StyleFoldoutField<TextField>
    {
        public static readonly string textUssClassName = FoldoutFieldPropertyName + "__textfield";

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleFoldoutField<TextField>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleFoldoutField<TextField>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
            }

            public override object CreateInstance() => new BorderRadiusFoldout();
        }

        const string k_TopLeftPropertyName = "borderTopLeftRadius";
        const string k_TopRightPropertyName = "borderTopRightRadius";
        const string k_BottomRightPropertyName = "borderBottomRightRadius";
        const string k_BottomLeftPropertyName = "borderBottomLeftRadius";

        public StyleLengthField topLeftField { get; }
        public StyleLengthField topRightField { get; }
        public StyleLengthField bottomRightField { get; }
        public StyleLengthField bottomLeftField { get; }

        public List<StyleLengthField> fields => new()
        {
            topLeftField,
            topRightField,
            bottomRightField,
            bottomLeftField
        };

        IntegerField m_DraggerField;
        public IntegerField draggerIntegerField => m_DraggerField;

        public BorderRadiusFoldout() : this("Radius") { }

        public BorderRadiusFoldout(string text)
            : base(text)
        {
            var topLeftRow = new OverrideRow() { name = k_TopLeftPropertyName };
            topLeftField = new StyleLengthField("Top-Left") { name = k_TopLeftPropertyName }.WithClassList(TextField.alignedFieldUssClassName);
            topLeftField.SetBinding("value", new StylePropertyBinding(k_TopLeftPropertyName));
            topLeftRow.Add(topLeftField);
            Add(topLeftRow);

            var topRightRow = new OverrideRow() { name = k_TopRightPropertyName };
            topRightField = new StyleLengthField("Top-Right") { name = k_TopRightPropertyName }.WithClassList(TextField.alignedFieldUssClassName);
            topRightField.SetBinding("value", new StylePropertyBinding(k_TopRightPropertyName));
            topRightRow.Add(topRightField);
            Add(topRightRow);

            var bottomRightRight = new OverrideRow() { name = k_BottomRightPropertyName };
            bottomRightField = new StyleLengthField("Bottom-Right") { name = k_BottomRightPropertyName }.WithClassList(TextField.alignedFieldUssClassName);
            bottomRightField.SetBinding("value", new StylePropertyBinding(k_BottomRightPropertyName));
            bottomRightRight.Add(bottomRightField);
            Add(bottomRightRight);

            var bottomLeftRow = new OverrideRow() { name = k_BottomLeftPropertyName };
            bottomLeftField = new StyleLengthField("Bottom-Left") { name = k_BottomLeftPropertyName }.WithClassList(TextField.alignedFieldUssClassName);
            bottomLeftField.SetBinding("value", new StylePropertyBinding(k_BottomLeftPropertyName));
            bottomLeftRow.Add(bottomLeftField);
            Add(bottomLeftRow);

            topLeftField.tooltip = "USS property: border-top-left-radius\n\nThe radius of the top-left corner when a rounded rectangle is drawn in the element's box.";
            topRightField.tooltip = "USS property: border-top-right-radius\n\nThe radius of the top-right corner when a rounded rectangle is drawn in the element's box.";
            bottomRightField.tooltip = "USS property: border-bottom-right-radius\n\nThe radius of the bottom-right corner when a rounded rectangle is drawn in the element's box.";
            bottomLeftField.tooltip = "USS property: border-bottom-left-radius\n\nThe radius of the bottom-left corner when a rounded rectangle is drawn in the element's box.";

            topLeftField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleLength>.valueProperty ||
                    e.property == enabledSelfProperty)
                    UpdateFromChildFields();
            });
            topRightField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleLength>.valueProperty ||
                    e.property == enabledSelfProperty)
                    UpdateFromChildFields();
            });
            bottomRightField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleLength>.valueProperty ||
                    e.property == enabledSelfProperty)
                    UpdateFromChildFields();
            });
            bottomLeftField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleLength>.valueProperty ||
                    e.property == enabledSelfProperty)
                    UpdateFromChildFields();
            });

            // Used for its dragger.
            var toggleInput = this.Q(className: "unity-toggle__input");
            m_DraggerField = new IntegerField(" ");
            m_DraggerField.name = "dragger-integer-field";
            m_DraggerField.visualInput.focusable = false;
            m_DraggerField.tabIndex = -1;
            m_DraggerField.AddToClassList(DraggerFieldUssClassName);
            m_DraggerField.RegisterValueChangedCallback(OnDraggerFieldUpdate);
            toggleInput.Add(m_DraggerField);

            headerInputField.AddToClassList(textUssClassName);
            headerInputField.AddToClassList(TextField.alignedFieldUssClassName);
            headerInputField.isDelayed = true;

            headerInputField.RegisterValueChangedCallback(OnHeaderValueChange);

            UpdateFromChildFields();
        }

        protected override TextField CreateHeaderInputElement()
        {
            return new TextField();
        }

        public override void UpdateFromChildFields()
        {
            var allTheSame = true;
            var singleValue = "none";
            var cumulativeValue = string.Empty;
            var shouldBeEnabled = true;

            for (var i = 0; i < fields.Count; ++i)
            {
                shouldBeEnabled &= fields[i].enabledSelf;
                var childValue = fields[i].value.ToString().ToLower();
                if (childValue.Equals("0"))
                {
                    childValue = "0px";
                }

                if (i == 0)
                    singleValue = childValue;
                else if (singleValue != childValue)
                    allTheSame = false;

                if (i != 0)
                    cumulativeValue += FieldStringSeparator;

                cumulativeValue += childValue;
            }

            headerInputField.SetValueWithoutNotify(allTheSame ? singleValue : cumulativeValue);
            if (fields.Count > 0)
                draggerIntegerField.SetValueWithoutNotify((int)fields[0].value.value.value);
            enabledSelf = shouldBeEnabled;
        }

        private void OnHeaderValueChange(ChangeEvent<string> evt)
        {
            var newValue = evt.newValue;

            var splitBy = new[] { ' ' };
            var inputArray = newValue.Split(splitBy);

            if (inputArray.Length == 1 && fields.Count > 0)
            {
                var newCommonValue = newValue;

                for (var i = 0; i < fields.Count; ++i)
                {
                    var styleField = fields[i];
                    styleField.value = Length.ParseString(newCommonValue);

                    if (i == 0 && !newCommonValue.StartsWith(UssVariablePrefix))
                    {
                        newCommonValue = fields[i].value.ToString();
                    }
                }
            }
            else if (inputArray.Length == 2)
            {
                topLeftField.value = bottomRightField.value = Length.ParseString(inputArray[0]);
                topRightField.value = bottomLeftField.value = Length.ParseString(inputArray[1]);
            }
            else if (inputArray.Length == 3)
            {
                topLeftField.value = Length.ParseString(inputArray[0]);
                topRightField.value = bottomLeftField.value = Length.ParseString(inputArray[1]);
                bottomRightField.value = Length.ParseString(inputArray[2]);
            }
            else
            {
                for (var i = 0; i < Mathf.Min(inputArray.Length, fields.Count); ++i)
                {
                    fields[i].value = Length.ParseString(inputArray[i]);
                }
            }

            UpdateFromChildFields();
            evt.StopPropagation();
        }

        void OnDraggerFieldUpdate(ChangeEvent<int> evt)
        {
            headerInputField.value = evt.newValue.ToString();
        }

        protected override void ForwardDependentPropertiesTracking(TrackPropertyEvent evt)
        {
            base.ForwardDependentPropertiesTracking(evt);
            var target = evt.propertyName switch
            {
                k_TopLeftPropertyName => topLeftField,
                k_TopRightPropertyName => topRightField,
                k_BottomRightPropertyName => bottomRightField,
                k_BottomLeftPropertyName => bottomLeftField,
                _ => null
            };

            if (target == null)
                return;

            var subEvent = TrackPropertyEvent.GetPooled(evt.provider,  evt.propertyName);
            subEvent.target = target;
            target.SendEvent(subEvent);
            evt.StopImmediatePropagation();
        }
    }
}
