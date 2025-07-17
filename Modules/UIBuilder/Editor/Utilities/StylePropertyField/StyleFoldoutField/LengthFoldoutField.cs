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
    internal class LengthFoldoutField : StyleFoldoutField<TextField>
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

            public override object CreateInstance() => new LengthFoldoutField();
        }

        StyleLength m_Top;
        StyleLength m_Right;
        StyleLength m_Bottom;
        StyleLength m_Left;

        public StyleLengthField topField;
        public StyleLengthField rightField;
        public StyleLengthField bottomField;
        public StyleLengthField leftField;

        public StyleRow topRow;
        public StyleRow rightRow;
        public StyleRow bottomRow;
        public StyleRow leftRow;

        public List<StyleLengthField> fields => new()
        {
            topField,
            rightField,
            bottomField,
            leftField
        };

        const string k_TopFieldName = "top";
        const string k_RightFieldName = "right";
        const string k_BottomFieldName = "bottom";
        const string k_LeftFieldName = "left";

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

                m_Top = value;
                Refresh();

                NotifyPropertyChanged(nameof(top));
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

                m_Right = value;
                Refresh();

                NotifyPropertyChanged(nameof(right));
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

                m_Bottom = value;
                Refresh();

                NotifyPropertyChanged(nameof(bottom));
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

                m_Left = value;
                Refresh();

                NotifyPropertyChanged(nameof(left));
            }
        }

        public LengthFoldoutField() : this(null) { }

        public LengthFoldoutField(string text)
            : base(text)
        {
            topRow = new StyleRow() { name = k_TopFieldName };
            topField = new StyleLengthField() { name = k_TopFieldName, classList = { TextField.alignedFieldUssClassName }};
            topRow.Add(topField);
            Add(topRow);

            rightRow = new StyleRow() { name = k_RightFieldName };
            rightField = new StyleLengthField() { name = k_RightFieldName, classList = { TextField.alignedFieldUssClassName }};
            rightRow.Add(rightField);
            Add(rightRow);

            bottomRow = new StyleRow() { name = k_BottomFieldName };
            bottomField = new StyleLengthField() { name = k_BottomFieldName, classList = { TextField.alignedFieldUssClassName }};
            bottomRow.Add(bottomField);
            Add(bottomRow);

            leftRow = new StyleRow() { name = k_LeftFieldName };
            leftField = new StyleLengthField() { name = k_LeftFieldName, classList = { TextField.alignedFieldUssClassName }};
            leftRow.Add(leftField);
            Add(leftRow);

            topField.RegisterValueChangedCallback((e) => top = e.newValue);
            rightField.RegisterValueChangedCallback((e) => right = e.newValue);
            bottomField.RegisterValueChangedCallback((e) => bottom = e.newValue);
            leftField.RegisterValueChangedCallback((e) => left = e.newValue);

            // Used for its dragger.
            var toggleInput = foldout.Q(className: "unity-toggle__input");
            m_DraggerField = new IntegerField(" ");
            m_DraggerField.name = "dragger-integer-field";
            m_DraggerField.AddToClassList(k_DraggerFieldUssClassName);
            m_DraggerField.RegisterValueChangedCallback(OnDraggerFieldUpdate);
            toggleInput.Add(m_DraggerField);

            headerInputField.isDelayed = true; // only updates on Enter or lost focus
            headerInputField.AddToClassList(textUssClassName);
            headerInputField.AddToClassList(TextField.alignedFieldUssClassName);
            headerInputField.RegisterValueChangedCallback(OnHeaderValueChange);

            UpdateFromChildFields();

            topField.label = "Top";
            rightField.label = "Right";
            bottomField.label = "Bottom";
            leftField.label = "Left";
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
            topField.value = m_Top;
            rightField.value = m_Right;
            bottomField.value = m_Bottom;
            leftField.value = m_Left;

            UpdateFromChildFields();
        }

        void OnDraggerFieldUpdate(ChangeEvent<int> evt)
        {
            headerInputField.value = evt.newValue.ToString();
        }
    }

    internal class PaddingFoldoutField : LengthFoldoutField
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : LengthFoldoutField.UxmlSerializedData
        {
            public override object CreateInstance() => new PaddingFoldoutField();
        }

        const string k_TopFieldName = "paddingTop";
        const string k_RightFieldName = "paddingRight";
        const string k_BottomFieldName = "paddingBottom";
        const string k_LeftFieldName = "paddingLeft";

        public PaddingFoldoutField()
        {
            topRow.Track(k_TopFieldName);
            rightRow.Track(k_RightFieldName);
            bottomRow.Track(k_BottomFieldName);
            leftRow.Track(k_LeftFieldName);

            Track(k_TopFieldName);
            Track(k_RightFieldName);
            Track(k_BottomFieldName);
            Track(k_LeftFieldName);
        }
    }

    internal class MarginFoldoutField : LengthFoldoutField
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : LengthFoldoutField.UxmlSerializedData
        {
            public override object CreateInstance() => new MarginFoldoutField();
        }

        const string k_TopFieldName = "marginTop";
        const string k_RightFieldName = "marginRight";
        const string k_BottomFieldName = "marginBottom";
        const string k_LeftFieldName = "marginLeft";

        public MarginFoldoutField()
        {
            topRow.Track(k_TopFieldName);
            rightRow.Track(k_RightFieldName);
            bottomRow.Track(k_BottomFieldName);
            leftRow.Track(k_LeftFieldName);

            Track(k_TopFieldName);
            Track(k_RightFieldName);
            Track(k_BottomFieldName);
            Track(k_LeftFieldName);
        }
    }
}
