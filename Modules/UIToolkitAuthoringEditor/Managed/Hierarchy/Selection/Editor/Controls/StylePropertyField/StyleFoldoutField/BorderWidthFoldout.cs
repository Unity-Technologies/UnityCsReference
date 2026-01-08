// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    internal sealed class BorderWidthFoldout : StyleFoldoutField<TextField>, INotifyCompositeStylePropertyChanged<StyleFloat>
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

            public override object CreateInstance() => new BorderWidthFoldout();
        }

        const string k_TopPropertyName = "borderTopWidth";
        const string k_RightPropertyName = "borderRightWidth";
        const string k_BottomPropertyName = "borderBottomWidth";
        const string k_LeftPropertyName = "borderLeftWidth";

        public static readonly BindingId topProperty = nameof(top);
        public static readonly BindingId rightProperty = nameof(right);
        public static readonly BindingId bottomProperty = nameof(bottom);
        public static readonly BindingId leftProperty = nameof(left);

        StyleFloat m_Top;
        StyleFloat m_Right;
        StyleFloat m_Bottom;
        StyleFloat m_Left;

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

        [CreateProperty]
        public StyleFloat top
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
        public StyleFloat right
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
        public StyleFloat bottom
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
        public StyleFloat left
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

        public BorderWidthFoldout()
            : this("Width") { }

        public BorderWidthFoldout(string text)
            : base(text)
        {
            var topRow = new OverrideRow() { name = k_TopPropertyName };
            topField = new StyleLengthField("Top") { name = k_TopPropertyName, classList = { TextField.alignedFieldUssClassName }};
            topRow.Add(topField);
            Add(topRow);

            var rightRow = new OverrideRow() { name = k_RightPropertyName };
            rightField = new StyleLengthField("Right") { name = k_RightPropertyName, classList = { TextField.alignedFieldUssClassName }};
            rightRow.Add(rightField);
            Add(rightRow);

            var bottomRow = new OverrideRow() { name = k_BottomPropertyName };
            bottomField = new StyleLengthField("Bottom") { name = k_BottomPropertyName, classList = { TextField.alignedFieldUssClassName }};
            bottomRow.Add(bottomField);
            Add(bottomRow);

            var leftRow = new OverrideRow() { name = k_LeftPropertyName };
            leftField = new StyleLengthField("Left") { name = k_LeftPropertyName, classList = { TextField.alignedFieldUssClassName }};
            leftRow.Add(leftField);
            Add(leftRow);

            topField.tooltip = "USS property: border-top-width\n\nSpace reserved for the top edge of the border during the layout phase.";
            rightField.tooltip = "USS property: border-right-width\n\nSpace reserved for the right edge of the border during the layout phase.";
            bottomField.tooltip = "USS property: border-bottom-width\n\nSpace reserved for the bottom edge of the border during the layout phase.";
            leftField.tooltip = "USS property: border-left-width\n\nSpace reserved for the left edge of the border during the layout phase.";

            topField.AddValidation(new Syntax("border-top-width"));
            rightField.AddValidation(new Syntax("border-right-width"));
            bottomField.AddValidation(new Syntax("border-bottom-width"));
            leftField.AddValidation(new Syntax("border-left-width"));

            topField.RegisterValueChangedCallback((e) =>
            {
                topField.SetValueWithoutNotify(e.newValue.value.value);
                top = e.newValue.value.value;
            });
            rightField.RegisterValueChangedCallback((e) =>
            {
                rightField.SetValueWithoutNotify(e.newValue.value.value);
                right = e.newValue.value.value;
            });
            bottomField.RegisterValueChangedCallback((e) =>
            {
                bottomField.SetValueWithoutNotify(e.newValue.value.value);
                bottom = e.newValue.value.value;
            });
            leftField.RegisterValueChangedCallback((e) =>
            {
                leftField.SetValueWithoutNotify(e.newValue.value.value);
                left = e.newValue.value.value;
            });

            // Used for its dragger.
            var toggleInput = this.Q(className: "unity-toggle__input");
            m_DraggerField = new IntegerField(" ");
            m_DraggerField.name = "dragger-integer-field";
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

            for (var i = 0; i < fields.Count; ++i)
            {
                var childValue = fields[i].value.value.value.ToString().ToLower() + "px"; // We always want px.

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

        protected override void Refresh()
        {
            topField.SetValueWithoutNotify(top.value);
            rightField.SetValueWithoutNotify(right.value);
            bottomField.SetValueWithoutNotify(bottom.value);
            leftField.SetValueWithoutNotify(left.value);
            base.Refresh();
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

        public void SetValue(BindingId id, StyleFloat v, bool notify)
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

        public void NotifyStylePropertyChanged(BindingId id, StyleFloat previousValue, StyleFloat newValue)
        {
            this.NotifyStylePropertyChanged(this, id, previousValue, newValue);
            NotifyPropertyChanged(id);
        }
    }
}
