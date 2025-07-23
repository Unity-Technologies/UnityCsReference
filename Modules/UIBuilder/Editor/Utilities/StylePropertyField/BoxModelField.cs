// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal class BoxModelEditableLabel : StyleLengthField
    {
        new static readonly string labelUssClassName = "unity-box-model__style-field__label";
        static readonly string fieldUssClassName = "unity-box-model__textfield";
        static readonly string draggerFieldUssClassName = "unity-style-field__dragger-field";

        Label m_EditableLabel;
        bool m_ShowUnit;
        IntegerField m_DraggerField;

        public IntegerField draggerIntegerField => m_DraggerField;
        public Label editableLabel => m_EditableLabel;
        public bool isUsingLabelDragger { get; private set; }

        [CreateProperty]
        public bool showUnit
        {
            get => m_ShowUnit;
            set
            {
                m_ShowUnit = value;
                UpdateLabel();

                NotifyPropertyChanged(nameof(showUnit));
            }
        }

        public BoxModelEditableLabel()
        {
            AddToClassList(fieldUssClassName);

            m_EditableLabel = new Label(value.value.ToString());
            m_EditableLabel.AddToClassList(labelUssClassName);
            Insert(0, m_EditableLabel);

            // Used for its dragger.
            m_DraggerField = new IntegerField(" ");
            m_DraggerField.name = "dragger-integer-field";
            m_DraggerField.AddToClassList(draggerFieldUssClassName);
            m_DraggerField.RegisterValueChangedCallback(OnDraggerFieldUpdate);
            Insert(1, m_DraggerField);

            // remove focus on enter
            RegisterCallback<KeyUpEvent>(OnKeyUp);
            draggerIntegerField.labelElement.RegisterCallback<PointerUpEvent>(OnDraggerPointerUp, TrickleDown.TrickleDown);
        }

        void OnKeyUp(KeyUpEvent e)
        {
            if (e.keyCode is KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Escape)
            {
                Blur();
            }
        }

        void OnDraggerPointerUp(PointerUpEvent e)
        {
            if (!isUsingLabelDragger)
            {
                valueField.Focus();
                return;
            }

            isUsingLabelDragger = false;
            Blur();
        }

        void OnDraggerFieldUpdate(ChangeEvent<int> evt)
        {
            isUsingLabelDragger = true;
            value = evt.newValue;
        }

        public override void SetValueWithoutNotify(StyleLength newValue)
        {
            base.SetValueWithoutNotify(newValue);
            UpdateLabel();
        }

        public void UpdateLabel()
        {
            m_EditableLabel.text = showUnit ? value.value.ToString() : value.value.value.ToString();
        }
    }

    class BoxModelField<TValueType, TControl> : BindableElement
        where TControl : BaseField<TValueType>, new()
    {
        static readonly string k_MixedUnitLabel = "mixed";
        static readonly string k_BoxModelClassName = "unity-box-model";
        static readonly string k_CenterContentClassName = k_BoxModelClassName + "__center-content";
        static readonly string k_TextfieldClassName = k_BoxModelClassName + "__textfield";
        static readonly string k_ColorfieldClassName = k_BoxModelClassName + "__colorfield";
        static readonly string k_TitleClassName = k_BoxModelClassName + "__title";
        static readonly string k_CenterClassName = k_BoxModelClassName + "__center";

        private Label m_Title;
        private VisualElement m_Center;
        private VisualElement m_CenterContent;

        private BaseField<TValueType> m_LeftField;
        private BaseField<TValueType> m_RightField;
        private BaseField<TValueType> m_TopField;
        private BaseField<TValueType> m_BottomField;
        private List<BaseField<TValueType>> m_Fields;

        public BaseField<TValueType> topField => m_TopField;
        public BaseField<TValueType> rightField => m_RightField;
        public BaseField<TValueType> bottomField => m_BottomField;
        public BaseField<TValueType> leftField => m_LeftField;
        public List<BaseField<TValueType>> fields => m_Fields;

        internal Label title => m_Title;

        private bool m_NeedsUnit;

        public bool needsUnit
        {
            get => m_NeedsUnit;
            set
            {
                if (m_NeedsUnit == value)
                    return;

                m_NeedsUnit = value;
                if (!m_NeedsUnit) return;

                m_Title = new Label(GetUnitText());
                m_Title.AddToClassList(k_TitleClassName);
                m_Title.AddToClassList(boxType.ToString().ToLowerInvariant());
                Add(m_Title);
            }
        }

        public BoxModelField(BoxType boxType, bool needsUnit, VisualElement content,
            VisualElement topFieldContainer, VisualElement bottomFieldContainer,
            VisualElement leftFieldContainer, VisualElement rightFieldContainer)
        {
            this.boxType = boxType;

            m_CenterContent = content;
            m_CenterContent.AddToClassList(k_CenterContentClassName);

            m_TopField = new TControl();
            m_BottomField = new TControl();
            m_RightField = new TControl();
            m_LeftField = new TControl();

            m_Fields = new List<BaseField<TValueType>>() { m_TopField, m_BottomField, m_RightField, m_LeftField };

            foreach (var field in m_Fields)
            {
                if (typeof(TValueType) == typeof(StyleLength) || typeof(TValueType) == typeof(StyleFloat))
                    field.AddToClassList(k_TextfieldClassName);
                else if (typeof(TValueType) == typeof(StyleColor))
                    field.AddToClassList(k_ColorfieldClassName);
            }

            leftFieldContainer.Add(m_LeftField);
            rightFieldContainer.Add(m_RightField);
            topFieldContainer.Add(m_TopField);
            bottomFieldContainer.Add(m_BottomField);

            m_Center = new VisualElement();
            m_Center.AddToClassList(k_CenterClassName);
            m_Center.AddToClassList(BuilderConstants.InspectorCompositeStyleRowElementClassName);
            m_Center.Add(m_CenterContent);
            Add(m_Center);

            this.needsUnit = needsUnit;
        }

        public BoxType boxType { get; private set; }

        internal void UpdateUnitFromFields()
        {
            if (!needsUnit)
                return;

            var unit = GetUnitText();

            m_Title.text = unit;

            foreach (var field in m_Fields)
            {
                if (field is BoxModelEditableLabel editableLabel)
                    editableLabel.showUnit = unit == k_MixedUnitLabel;
            }
        }

        string GetUnitFromFields()
        {
            if (!needsUnit || typeof(TControl) != typeof(BoxModelEditableLabel))
                return string.Empty;

            var unit = (topField as BoxModelEditableLabel)?.value.value.unit ?? LengthUnit.Pixel;
            var singleUnit = true;

            foreach (var field in m_Fields)
            {
                if (field is BoxModelEditableLabel lengthField && lengthField.value.value.unit != unit)
                {
                    singleUnit = false;
                    break;
                }
            }

            if (!singleUnit)
                return k_MixedUnitLabel;

            var unitString = unit.ToDisplayString();
            return unitString == "-" ? string.Empty : unitString;
        }

        string GetUnitText()
        {
            string unit;
            if (boxType == BoxType.BorderColor)
            {
                var boxModels = this.Query<BoxModelField<StyleLength, BoxModelEditableLabel>>().Build();
                var outerBoxModel = boxModels.First();
                var innerBoxModel = boxModels.Last();
                var outerBoxModelUnit = outerBoxModel.GetUnitFromFields();
                var innerBoxModelUnit = innerBoxModel.GetUnitFromFields();
                unit = outerBoxModelUnit == innerBoxModelUnit ? outerBoxModelUnit : k_MixedUnitLabel;

                outerBoxModel.fields.ForEach(f => ((BoxModelEditableLabel)f).showUnit = unit == k_MixedUnitLabel);
                innerBoxModel.fields.ForEach(f => ((BoxModelEditableLabel)f).showUnit = unit == k_MixedUnitLabel);
            }
            else
            {
                unit = GetUnitFromFields();
            }

            return unit;
        }
    }
}
