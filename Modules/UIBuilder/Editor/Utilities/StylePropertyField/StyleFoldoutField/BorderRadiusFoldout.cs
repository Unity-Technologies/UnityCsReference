// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BorderRadiusFoldout : StyleFoldoutField<TextField>
    {
        public static readonly string textUssClassName = BuilderConstants.FoldoutFieldPropertyName + "__textfield";

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

        public BorderRadiusFoldout() : this(null) { }

        public BorderRadiusFoldout(string text)
            : base(text)
        {
            this.text = text ?? "Border Radius";

            var topLeftRow = new StyleRow() { name = k_TopLeftFieldName };
            topLeftRow.Track(k_TopLeftFieldName);
            topLeftField = new StyleLengthField() { name = k_TopLeftFieldName, classList = { TextField.alignedFieldUssClassName }};
            topLeftRow.Add(topLeftField);
            Add(topLeftRow);

            var topRightRow = new StyleRow() { name = k_TopRightFieldName };
            topRightRow.Track(k_TopRightFieldName);
            topRightField = new StyleLengthField() { name = k_TopRightFieldName, classList = { TextField.alignedFieldUssClassName }};
            topRightRow.Add(topRightField);
            Add(topRightRow);

            var bottomRightRight = new StyleRow() { name = k_BottomRightFieldName };
            bottomRightRight.Track(k_BottomRightFieldName);
            bottomRightField = new StyleLengthField() { name = k_BottomRightFieldName, classList = { TextField.alignedFieldUssClassName }};
            bottomRightRight.Add(bottomRightField);
            Add(bottomRightRight);

            var bottomLeftRow = new StyleRow() { name = k_BottomLeftFieldName };
            bottomLeftRow.Track(k_BottomLeftFieldName);
            bottomLeftField = new StyleLengthField() { name = k_BottomLeftFieldName, classList = { TextField.alignedFieldUssClassName }};
            bottomLeftRow.Add(bottomLeftField);
            Add(bottomLeftRow);

            Track(k_TopLeftFieldName);
            Track(k_TopRightFieldName);
            Track(k_BottomRightFieldName);
            Track(k_BottomLeftFieldName);

            topLeftField.RegisterValueChangedCallback((e) => topLeft = e.newValue);
            topRightField.RegisterValueChangedCallback((e) => topRight = e.newValue);
            bottomRightField.RegisterValueChangedCallback((e) => bottomRight = e.newValue);
            bottomLeftField.RegisterValueChangedCallback((e) => bottomLeft = e.newValue);

            // Used for its dragger.
            var toggleInput = foldout.Q(className: "unity-toggle__input");
            m_DraggerField = new IntegerField(" ");
            m_DraggerField.name = "dragger-integer-field";
            m_DraggerField.AddToClassList(k_DraggerFieldUssClassName);
            m_DraggerField.RegisterValueChangedCallback(OnDraggerFieldUpdate);
            toggleInput.Add(m_DraggerField);

            headerInputField.AddToClassList(textUssClassName);
            headerInputField.AddToClassList(TextField.alignedFieldUssClassName);
            headerInputField.isDelayed = true;

            headerInputField.RegisterValueChangedCallback(OnHeaderValueChange);

            UpdateFromChildFields();

            topLeftField.label = "Top-Left";
            topRightField.label = "Top-Right";
            bottomRightField.label = "Bottom-Right";
            bottomLeftField.label = "Bottom-Left";
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
                    cumulativeValue += k_FieldStringSeparator;

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

                    if (i == 0 && !newCommonValue.StartsWith(BuilderConstants.UssVariablePrefix))
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
    }
}
