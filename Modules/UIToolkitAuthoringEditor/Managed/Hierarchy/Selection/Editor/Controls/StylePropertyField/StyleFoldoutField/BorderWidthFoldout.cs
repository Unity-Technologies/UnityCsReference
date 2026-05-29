// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [UxmlElement]
    internal sealed partial class BorderWidthFoldout : StyleFoldoutField<TextField>
    {
        public static readonly string textUssClassName = FoldoutFieldPropertyName + "__textfield";

        const string k_TopPropertyName = "borderTopWidth";
        const string k_RightPropertyName = "borderRightWidth";
        const string k_BottomPropertyName = "borderBottomWidth";
        const string k_LeftPropertyName = "borderLeftWidth";

        public StyleLengthField topField { get; }
        public StyleLengthField rightField { get; }
        public StyleLengthField bottomField { get; }
        public StyleLengthField leftField { get; }

        public List<StyleLengthField> fields => new()
        {
            topField,
            rightField,
            bottomField,
            leftField
        };

        IntegerField m_DraggerField;

        public IntegerField draggerIntegerField => m_DraggerField;

        public BorderWidthFoldout()
            : this("Width") { }

        public BorderWidthFoldout(string text)
            : base(text)
        {
            var topRow = new OverrideRow() { name = k_TopPropertyName };
            topField = new StyleLengthField("Top") { name = k_TopPropertyName }.WithClassList(TextField.alignedFieldUssClassName);
            topField.SetBinding("value", new StylePropertyBinding(k_TopPropertyName));
            topRow.Add(topField);
            Add(topRow);

            var rightRow = new OverrideRow() { name = k_RightPropertyName };
            rightField = new StyleLengthField("Right") { name = k_RightPropertyName }.WithClassList(TextField.alignedFieldUssClassName);
            rightField.SetBinding("value", new StylePropertyBinding(k_RightPropertyName));
            rightRow.Add(rightField);
            Add(rightRow);

            var bottomRow = new OverrideRow() { name = k_BottomPropertyName };
            bottomField = new StyleLengthField("Bottom") { name = k_BottomPropertyName }.WithClassList(TextField.alignedFieldUssClassName);
            bottomField.SetBinding("value", new StylePropertyBinding(k_BottomPropertyName));
            bottomRow.Add(bottomField);
            Add(bottomRow);

            var leftRow = new OverrideRow() { name = k_LeftPropertyName };
            leftField = new StyleLengthField("Left") { name = k_LeftPropertyName }.WithClassList(TextField.alignedFieldUssClassName);
            leftField.SetBinding("value", new StylePropertyBinding(k_LeftPropertyName));
            leftRow.Add(leftField);
            Add(leftRow);

            topField.tooltip = "<b>USS property: border-top-width</b>\nSpace reserved for the top edge of the border during the layout phase.";
            rightField.tooltip = "<b>USS property: border-right-width</b>\nSpace reserved for the right edge of the border during the layout phase.";
            bottomField.tooltip = "<b>USS property: border-bottom-width</b>\nSpace reserved for the bottom edge of the border during the layout phase.";
            leftField.tooltip = "<b>USS property: border-left-width</b>\nSpace reserved for the left edge of the border during the layout phase.";

            topField.AddValidation(new Syntax("border-top-width"));
            rightField.AddValidation(new Syntax("border-right-width"));
            bottomField.AddValidation(new Syntax("border-bottom-width"));
            leftField.AddValidation(new Syntax("border-left-width"));

            topField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleLength>.valueProperty ||
                    e.property == enabledSelfProperty)
                {
                    UpdateFromChildFields();
                }
            });
            rightField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleLength>.valueProperty ||
                    e.property == enabledSelfProperty)
                {
                    UpdateFromChildFields();
                }
            });
            bottomField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleLength>.valueProperty ||
                    e.property == enabledSelfProperty)
                {
                    UpdateFromChildFields();
                }
            });
            leftField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleLength>.valueProperty ||
                    e.property == enabledSelfProperty)
                {
                    UpdateFromChildFields();
                }
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
            var shouldBeEnabled = false;

            for (var i = 0; i < fields.Count; ++i)
            {
                shouldBeEnabled |= fields[i].enabledSelf;
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
                // Set the first value to top and bottom, and the second value to left and right
                topField.value = bottomField.value = Length.ParseString(inputArray[0]);
                leftField.value = rightField.value = Length.ParseString(inputArray[1]);
            }
            else if (inputArray.Length == 3)
            {
                // Set the first value to top, the second value to right and left, and the third value to bottom
                topField.value = Length.ParseString(inputArray[0]);
                rightField.value = leftField.value = Length.ParseString(inputArray[1]);
                bottomField.value = Length.ParseString(inputArray[2]);
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
    }
}
