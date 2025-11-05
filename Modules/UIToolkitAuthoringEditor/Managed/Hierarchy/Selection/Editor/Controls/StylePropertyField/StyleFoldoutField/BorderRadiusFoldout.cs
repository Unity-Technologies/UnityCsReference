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
    internal class BorderRadiusFoldout : StyleFoldoutField<TextField>
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

        StyleLength m_TopLeft;
        StyleLength m_TopRight;
        StyleLength m_BottomLeft;
        StyleLength m_BottomRight;

        public StyleLengthField topLeftField;
        public StyleLengthField topRightField;
        public StyleLengthField bottomRightField;
        public StyleLengthField bottomLeftField;

        protected virtual string topLeftPropertyName { get; } = "borderTopLeftRadius";
        protected virtual string topRightPropertyName { get; } = "borderTopRightRadius";
        protected virtual string bottomRightPropertyName { get; } = "borderBottomRightRadius";
        protected virtual string bottomLeftPropertyName { get; } = "borderBottomLeftRadius";

        public List<StyleLengthField> fields => new()
        {
            topLeftField,
            topRightField,
            bottomRightField,
            bottomLeftField
        };

        const string k_TopLeftFieldName = "borderTopLeftRadius";
        const string k_TopRightFieldName = "borderTopRightRadius";
        const string k_BottomRightFieldName = "borderBottomRightRadius";
        const string k_BottomLeftFieldName = "borderBottomLeftRadius";

        IntegerField m_DraggerField;
        public IntegerField draggerIntegerField => m_DraggerField;

        [CreateProperty]
        public StyleLength topLeft
        {
            get => m_TopLeft;
            set
            {
                if (m_TopLeft.Equals(value))
                    return;

                m_TopLeft = value;
                Refresh();

                NotifyPropertyChanged(nameof(topLeft));
            }
        }

        [CreateProperty]
        public StyleLength topRight
        {
            get => m_TopRight;
            set
            {
                if (m_TopRight.Equals(value))
                    return;

                m_TopRight = value;
                Refresh();

                NotifyPropertyChanged(nameof(topRight));
            }
        }

        [CreateProperty]
        public StyleLength bottomRight
        {
            get => m_BottomRight;
            set
            {
                if (m_BottomRight.Equals(value))
                    return;

                m_BottomRight = value;
                Refresh();

                NotifyPropertyChanged(nameof(bottomRight));
            }
        }

        [CreateProperty]
        public StyleLength bottomLeft
        {
            get => m_BottomLeft;
            set
            {
                if (m_BottomLeft.Equals(value))
                    return;

                m_BottomLeft = value;
                Refresh();

                NotifyPropertyChanged(nameof(bottomLeft));
            }
        }

        public BorderRadiusFoldout() : this("Radius") { }

        public BorderRadiusFoldout(string text)
            : base(text)
        {
            var topLeftRow = new OverrideRow() { name = k_TopLeftFieldName };
            topLeftField = new StyleLengthField("Top-Left") { name = k_TopLeftFieldName, classList = { TextField.alignedFieldUssClassName }};
            topLeftRow.Add(topLeftField);
            Add(topLeftRow);

            var topRightRow = new OverrideRow() { name = k_TopRightFieldName };
            topRightField = new StyleLengthField("Top-Right") { name = k_TopRightFieldName, classList = { TextField.alignedFieldUssClassName }};
            topRightRow.Add(topRightField);
            Add(topRightRow);

            var bottomRightRight = new OverrideRow() { name = k_BottomRightFieldName };
            bottomRightField = new StyleLengthField("Bottom-Right") { name = k_BottomRightFieldName, classList = { TextField.alignedFieldUssClassName }};
            bottomRightRight.Add(bottomRightField);
            Add(bottomRightRight);

            var bottomLeftRow = new OverrideRow() { name = k_BottomLeftFieldName };
            bottomLeftField = new StyleLengthField("Bottom-Left") { name = k_BottomLeftFieldName, classList = { TextField.alignedFieldUssClassName }};
            bottomLeftRow.Add(bottomLeftField);
            Add(bottomLeftRow);

            topLeftField.tooltip = "USS property: border-top-left-radius\n\nThe radius of the top-left corner when a rounded rectangle is drawn in the element's box.";
            topRightField.tooltip = "USS property: border-top-right-radius\n\nThe radius of the top-right corner when a rounded rectangle is drawn in the element's box.";
            bottomRightField.tooltip = "USS property: border-bottom-right-radius\n\nThe radius of the bottom-right corner when a rounded rectangle is drawn in the element's box.";
            bottomLeftField.tooltip = "USS property: border-bottom-left-radius\n\nThe radius of the bottom-left corner when a rounded rectangle is drawn in the element's box.";

            topLeftField.RegisterValueChangedCallback((e) => topLeft = e.newValue);
            topRightField.RegisterValueChangedCallback((e) => topRight = e.newValue);
            bottomRightField.RegisterValueChangedCallback((e) => bottomRight = e.newValue);
            bottomLeftField.RegisterValueChangedCallback((e) => bottomLeft = e.newValue);

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
                topLeft = bottomRight = Length.ParseString(inputArray[0]);
                topRight = bottomLeft = Length.ParseString(inputArray[1]);
            }
            else if (inputArray.Length == 3)
            {
                topLeft = Length.ParseString(inputArray[0]);
                topRight = bottomLeft = Length.ParseString(inputArray[1]);
                bottomRight = Length.ParseString(inputArray[2]);
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
            topLeftField.value = m_TopLeft;
            topRightField.value = m_TopRight;
            bottomLeftField.value = m_BottomLeft;
            bottomRightField.value = m_BottomRight;

            UpdateFromChildFields();
        }

        void OnDraggerFieldUpdate(ChangeEvent<int> evt)
        {
            headerInputField.value = evt.newValue.ToString();
        }

        protected override void ForwardDependentPropertiesTracking(TrackStylePropertyEvent evt)
        {
            base.ForwardDependentPropertiesTracking(evt);
            if (evt.propertyName == topLeftPropertyName)
            {
                var subEvent = TrackStylePropertyEvent.GetPooled(evt.provider,  evt.propertyName);
                subEvent.target = topLeftField;
                topLeftField.SendEvent(subEvent);
                evt.StopImmediatePropagation();
            }
            else if (evt.propertyName == topRightPropertyName)
            {
                var subEvent = TrackStylePropertyEvent.GetPooled(evt.provider,  evt.propertyName);
                subEvent.target = topRightField;
                topRightField.SendEvent(subEvent);
                evt.StopImmediatePropagation();
            }
            else if (evt.propertyName == bottomRightPropertyName)
            {
                var subEvent = TrackStylePropertyEvent.GetPooled(evt.provider,  evt.propertyName);
                subEvent.target = bottomRightField;
                bottomRightField.SendEvent(subEvent);
                evt.StopImmediatePropagation();
            }
            else if (evt.propertyName == bottomLeftPropertyName)
            {
                var subEvent = TrackStylePropertyEvent.GetPooled(evt.provider,  evt.propertyName);
                subEvent.target = bottomLeftField;
                bottomLeftField.SendEvent(subEvent);
                evt.StopImmediatePropagation();
            }
        }
    }
}
