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

namespace Unity.UI.Builder
{
    internal class BorderColorFoldout : StyleFoldoutField<ColorField>
    {
        static readonly string k_FieldClassName = BuilderConstants.FoldoutFieldPropertyName + "__color-field";
        static readonly string k_MixedValueLineClassName = BuilderConstants.FoldoutFieldPropertyName + "__mixed-value-line";
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

        VisualElement m_MixedValueLine;

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

        public StyleColorField topField;
        public StyleColorField rightField;
        public StyleColorField bottomField;
        public StyleColorField leftField;

        public List<StyleColorField> fields => new()
        {
            topField,
            rightField,
            bottomField,
            leftField
        };

        const string k_TopFieldName = "borderTopColor";
        const string k_RightFieldName = "borderRightColor";
        const string k_BottomFieldName = "borderBottomColor";
        const string k_LeftFieldName = "borderLeftColor";

        [CreateProperty]
        public StyleColor top
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
        public StyleColor right
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
        public StyleColor bottom
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
        public StyleColor left
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

        public BorderColorFoldout()
            : this(null) { }

        public BorderColorFoldout(string text)
            : base(text)
        {
            this.text = text ?? "Border Color";

            var topRow = new StyleRow() { name = k_TopFieldName };
            topRow.Track(k_TopFieldName);
            topField = new StyleColorField() { name = k_TopFieldName, classList = { TextField.alignedFieldUssClassName } };
            topRow.Add(topField);
            Add(topRow);

            var rightRow = new StyleRow() { name = k_RightFieldName };
            rightRow.Track(k_RightFieldName);
            rightField = new StyleColorField() { name = k_RightFieldName, classList = { TextField.alignedFieldUssClassName } };
            rightRow.Add(rightField);
            Add(rightRow);

            var bottomRow = new StyleRow() { name = k_BottomFieldName };
            bottomRow.Track(k_BottomFieldName);
            bottomField = new StyleColorField() { name = k_BottomFieldName, classList = { TextField.alignedFieldUssClassName } };
            bottomRow.Add(bottomField);
            Add(bottomRow);

            var leftRow = new StyleRow() { name = k_LeftFieldName };
            leftRow.Track(k_LeftFieldName);
            leftField = new StyleColorField() { name = k_LeftFieldName, classList = { TextField.alignedFieldUssClassName } };
            leftRow.Add(leftField);
            Add(leftRow);

            topField.AddValidation(new Syntax("border-top-width"));
            rightField.AddValidation(new Syntax("border-right-width"));
            bottomField.AddValidation(new Syntax("border-bottom-width"));
            leftField.AddValidation(new Syntax("border-left-width"));

            Track(k_TopFieldName);
            Track(k_RightFieldName);
            Track(k_BottomFieldName);
            Track(k_LeftFieldName);

            topField.RegisterValueChangedCallback((e) => top = e.newValue);
            rightField.RegisterValueChangedCallback((e) => right = e.newValue);
            bottomField.RegisterValueChangedCallback((e) => bottom = e.newValue);
            leftField.RegisterValueChangedCallback((e) => left = e.newValue);

            headerInputField.AddToClassList(TextField.alignedFieldUssClassName);
            headerInputField.name = k_FieldClassName;
            headerInputField.AddToClassList(k_FieldClassName);
            headerInputField.RegisterValueChangedCallback(OnHeaderValueChange);

            topField.label = "Top";
            rightField.label = "Right";
            bottomField.label = "Bottom";
            leftField.label = "Left";

            m_MixedValueLine = new VisualElement();
            m_MixedValueLine.name = k_MixedValueLineClassName;
            m_MixedValueLine.AddToClassList(k_MixedValueLineClassName);
            headerInputField.Q(internalColorFieldName).hierarchy.Add(m_MixedValueLine);

            UpdateFromChildFields();
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
            topField.value = m_Top.value;
            rightField.value = m_Right.value;
            bottomField.value = m_Bottom.value;
            leftField.value = m_Left.value;

            UpdateFromChildFields();
        }
    }
}
