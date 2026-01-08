// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;
using Unity.UIToolkit.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    internal abstract class LengthFoldoutField : StyleFoldoutField<TextField>, INotifyCompositeStylePropertyChanged<StyleLength>
    {
        static readonly string TextUssClassName = FoldoutFieldPropertyName + "__textfield";

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleFoldoutField<TextField>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleFoldoutField<TextField>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
            }
        }

        const string k_TopFieldName = "top";
        const string k_RightFieldName = "right";
        const string k_BottomFieldName = "bottom";
        const string k_LeftFieldName = "left";

        static readonly BindingId topProperty = nameof(top);
        static readonly BindingId rightProperty = nameof(right);
        static readonly BindingId bottomProperty = nameof(bottom);
        static readonly BindingId leftProperty = nameof(left);

        StyleLength m_Top;
        StyleLength m_Right;
        StyleLength m_Bottom;
        StyleLength m_Left;

        public StyleLengthField topField { get; }
        public StyleLengthField rightField { get; }
        public StyleLengthField bottomField { get; }
        public StyleLengthField leftField { get; }

        protected abstract string topPropertyName { get; }
        protected abstract string rightPropertyName { get; }
        protected abstract string bottomPropertyName { get; }
        protected abstract string leftPropertyName { get; }

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
        public StyleLength top
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
        public StyleLength right
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
        public StyleLength bottom
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
        public StyleLength left
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

        protected LengthFoldoutField() : this(null) { }

        protected LengthFoldoutField(string text)
            : base(text)
        {
            var topRow = new OverrideRow() { name = k_TopFieldName };
            topField = new StyleLengthField("Top") { name = k_TopFieldName, classList = { TextField.alignedFieldUssClassName }};
            topRow.Add(topField);
            Add(topRow);

            var rightRow = new OverrideRow() { name = k_RightFieldName };
            rightField = new StyleLengthField("Right") { name = k_RightFieldName, classList = { TextField.alignedFieldUssClassName }};
            rightRow.Add(rightField);
            Add(rightRow);

            var bottomRow = new OverrideRow() { name = k_BottomFieldName };
            bottomField = new StyleLengthField("Bottom") { name = k_BottomFieldName, classList = { TextField.alignedFieldUssClassName }};
            bottomRow.Add(bottomField);
            Add(bottomRow);

            var leftRow = new OverrideRow() { name = k_LeftFieldName };
            leftField = new StyleLengthField("Left") { name = k_LeftFieldName, classList = { TextField.alignedFieldUssClassName }};
            leftRow.Add(leftField);
            Add(leftRow);

            topField.RegisterValueChangedCallback((e) => top = e.newValue);
            rightField.RegisterValueChangedCallback((e) => right = e.newValue);
            bottomField.RegisterValueChangedCallback((e) => bottom = e.newValue);
            leftField.RegisterValueChangedCallback((e) => left = e.newValue);

            // Used for its dragger.
            var toggleInput = this.Q(className: "unity-toggle__input");
            m_DraggerField = new IntegerField(" ");
            m_DraggerField.name = "dragger-integer-field";
            m_DraggerField.AddToClassList(DraggerFieldUssClassName);
            m_DraggerField.RegisterValueChangedCallback(OnDraggerFieldUpdate);
            toggleInput.Add(m_DraggerField);

            headerInputField.isDelayed = true; // only updates on Enter or lost focus
            headerInputField.AddToClassList(TextUssClassName);
            headerInputField.AddToClassList(TextField.alignedFieldUssClassName);
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
                top = bottom = Length.ParseString(inputArray[0]);
                left = right = Length.ParseString(inputArray[1]);
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
            topField.SetValueWithoutNotify(top);
            rightField.SetValueWithoutNotify(right);
            bottomField.SetValueWithoutNotify(bottom);
            leftField.SetValueWithoutNotify(left);
            base.Refresh();
        }

        void OnDraggerFieldUpdate(ChangeEvent<int> evt)
        {
            headerInputField.value = evt.newValue.ToString();
        }

        protected override void ForwardDependentPropertiesTracking(TrackPropertyEvent evt)
        {
            base.ForwardDependentPropertiesTracking(evt);
            var target = default(VisualElement);
            if (evt.propertyName == topPropertyName)
                target = topField;
            else if (evt.propertyName == rightPropertyName)
                target = rightField;
            else if (evt.propertyName == bottomPropertyName)
                target = bottomField;
            else if (evt.propertyName == leftPropertyName)
                target = leftField;

            if (target == null)
                return;

            var subEvent = TrackPropertyEvent.GetPooled(evt.provider,  evt.propertyName);
            subEvent.target = target;
            target.SendEvent(subEvent);
            evt.StopImmediatePropagation();
        }

        public void SetValue(BindingId id, StyleLength v, bool notify)
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

        public void NotifyStylePropertyChanged(BindingId id, StyleLength previousValue, StyleLength newValue)
        {
            this.NotifyStylePropertyChanged(this, id, previousValue, newValue);
            NotifyPropertyChanged(id);
        }
    }

    internal class PaddingFoldoutField : LengthFoldoutField
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : LengthFoldoutField.UxmlSerializedData
        {
            public override object CreateInstance() => new PaddingFoldoutField();
        }

        protected override string topPropertyName { get; } = "paddingTop";
        protected override string rightPropertyName { get; } = "paddingRight";
        protected override string bottomPropertyName { get; } = "paddingBottom";
        protected override string leftPropertyName { get; } = "paddingLeft";

        public PaddingFoldoutField() : base("Padding")
        {
            topField.tooltip = "USS property: padding-top\n\nSpace reserved for the top edge of the padding during the layout phase.";
            rightField.tooltip = "USS property: padding-right\n\nSpace reserved for the right edge of the padding during the layout phase.";
            bottomField.tooltip = "USS property: padding-bottom\n\nSpace reserved for the bottom edge of the padding during the layout phase.";
            leftField.tooltip = "USS property: padding-left\n\nSpace reserved for the left edge of the padding during the layout phase.";
        }
    }

    internal class MarginFoldoutField : LengthFoldoutField
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : LengthFoldoutField.UxmlSerializedData
        {
            public override object CreateInstance() => new MarginFoldoutField();
        }

        protected override string topPropertyName { get; } = "marginTop";
        protected override string rightPropertyName { get; } = "marginRight";
        protected override string bottomPropertyName { get; } = "marginBottom";
        protected override string leftPropertyName { get; } = "marginLeft";

        public MarginFoldoutField() : base("Margin")
        {
            topField.tooltip = "USS property: margin-top\n\nSpace reserved for the top edge of the margin during the layout phase.";
            rightField.tooltip = "USS property: margin-right\n\nSpace reserved for the right edge of the margin during the layout phase.";
            bottomField.tooltip = "USS property: margin-bottom\n\nSpace reserved for the bottom edge of the margin during the layout phase.";
            leftField.tooltip = "USS property: margin-left\n\nSpace reserved for the left edge of the margin during the layout phase.";
        }
    }
}
