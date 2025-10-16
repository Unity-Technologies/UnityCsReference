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
    internal class BorderWidthFoldout : StyleFoldoutField<TextField>
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

        StyleFloat m_Top;
        StyleFloat m_Right;
        StyleFloat m_Bottom;
        StyleFloat m_Left;

        public StyleLengthField topField;
        public StyleLengthField rightField;
        public StyleLengthField bottomField;
        public StyleLengthField leftField;

        protected virtual string topPropertyName { get; } = "borderTopWidth";
        protected virtual string rightPropertyName { get; } = "borderRightWidth";
        protected virtual string bottomPropertyName { get; } = "borderBottomWidth";
        protected virtual string leftPropertyName { get; } = "borderLeftWidth";

        public List<StyleLengthField> fields => new()
        {
            topField,
            rightField,
            bottomField,
            leftField
        };

        const string k_TopFieldName = "borderTopWidth";
        const string k_RightFieldName = "borderRightWidth";
        const string k_BottomFieldName = "borderBottomWidth";
        const string k_LeftFieldName = "borderLeftWidth";

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

                m_Top = value;
                Refresh();

                NotifyPropertyChanged(nameof(top));
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

                m_Right = value;
                Refresh();

                NotifyPropertyChanged(nameof(right));
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

                m_Bottom = value;
                Refresh();

                NotifyPropertyChanged(nameof(bottom));
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

                m_Left = value;
                Refresh();

                NotifyPropertyChanged(nameof(left));
            }
        }

        public BorderWidthFoldout()
            : this("Border Width") { }

        public BorderWidthFoldout(string text)
            : base(text)
        {
            var topRow = new OverrideRow() { name = k_TopFieldName };
            topField = new StyleLengthField("Top") { name = k_TopFieldName };
            topRow.Add(topField);
            Add(topRow);

            var rightRow = new OverrideRow() { name = k_RightFieldName };
            rightField = new StyleLengthField("Right") { name = k_RightFieldName };
            rightRow.Add(rightField);
            Add(rightRow);

            var bottomRow = new OverrideRow() { name = k_BottomFieldName };
            bottomField = new StyleLengthField("Bottom") { name = k_BottomFieldName };
            bottomRow.Add(bottomField);
            Add(bottomRow);

            var leftRow = new OverrideRow() { name = k_LeftFieldName };
            leftField = new StyleLengthField("Left") { name = k_LeftFieldName };
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

        protected void OnHeaderValueChange(ChangeEvent<string> evt)
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
            topField.value = m_Top.value;
            rightField.value = m_Right.value;
            bottomField.value = m_Bottom.value;
            leftField.value = m_Left.value;

            UpdateFromChildFields();
        }

        void OnDraggerFieldUpdate(ChangeEvent<int> evt)
        {
            headerInputField.value = evt.newValue.ToString();
        }

        protected override void ForwardDependentPropertiesTracking(TrackStylePropertyEvent evt)
        {
            base.ForwardDependentPropertiesTracking(evt);
            if (evt.propertyName == topPropertyName)
            {
                var subEvent = TrackStylePropertyEvent.GetPooled(evt.provider,  evt.propertyName);
                subEvent.target = topField;
                topField.SendEvent(subEvent);
                evt.StopImmediatePropagation();
            }
            else if (evt.propertyName == rightPropertyName)
            {
                var subEvent = TrackStylePropertyEvent.GetPooled(evt.provider,  evt.propertyName);
                subEvent.target = rightField;
                rightField.SendEvent(subEvent);
                evt.StopImmediatePropagation();
            }
            else if (evt.propertyName == bottomPropertyName)
            {
                var subEvent = TrackStylePropertyEvent.GetPooled(evt.provider,  evt.propertyName);
                subEvent.target = bottomField;
                bottomField.SendEvent(subEvent);
                evt.StopImmediatePropagation();
            }
            else if (evt.propertyName == leftPropertyName)
            {
                var subEvent = TrackStylePropertyEvent.GetPooled(evt.provider,  evt.propertyName);
                subEvent.target = leftField;
                leftField.SendEvent(subEvent);
                evt.StopImmediatePropagation();
            }
        }
    }
}
