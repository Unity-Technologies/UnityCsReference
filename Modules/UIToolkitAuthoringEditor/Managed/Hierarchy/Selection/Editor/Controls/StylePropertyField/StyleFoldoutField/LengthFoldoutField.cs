// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [UxmlElement]
    internal abstract partial class LengthFoldoutField : StyleFoldoutField<TextField>
    {
        static readonly string TextUssClassName = FoldoutFieldPropertyName + "__textfield";

        public StyleLengthField topField { get; protected set; }
        public StyleLengthField rightField { get; protected set; }
        public StyleLengthField bottomField { get; protected set; }
        public StyleLengthField leftField { get; protected set; }

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

        protected LengthFoldoutField() : this(null) { }

        protected LengthFoldoutField(string text)
            : base(text)
        {
            // Derived classes will load UXML and query for fields
        }

        protected void SetupBaseCallbacks()
        {
            topField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleLength>.valueProperty ||
                    e.property == enabledSelfProperty)
                    UpdateFromChildFields();
            });
            rightField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleLength>.valueProperty ||
                    e.property == enabledSelfProperty)
                    UpdateFromChildFields();
            });
            bottomField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleLength>.valueProperty ||
                    e.property == enabledSelfProperty)
                    UpdateFromChildFields();
            });
            leftField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleLength>.valueProperty ||
                    e.property == enabledSelfProperty)
                    UpdateFromChildFields();
            });

            // Used for its dragger.
            m_DraggerField = new IntegerField(" ");
            m_DraggerField.name = "dragger-integer-field";
            m_DraggerField.visualInput.focusable = false;
            m_DraggerField.tabIndex = -1;
            m_DraggerField.AddToClassList(DraggerFieldUssClassName);
            m_DraggerField.RegisterValueChangedCallback(OnDraggerFieldUpdate);
            m_Toggle.labelElement.Add(m_DraggerField);

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
    }

    [UxmlElement]
    internal partial class PaddingFoldoutField : LengthFoldoutField
    {
        private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/Controls/PaddingFoldoutField.uxml";

        protected override string topPropertyName => "paddingTop";
        protected override string rightPropertyName => "paddingRight";
        protected override string bottomPropertyName => "paddingBottom";
        protected override string leftPropertyName => "paddingLeft";

        public PaddingFoldoutField() : base("Padding")
        {
            var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
            vta.CloneTree(contentContainer);

            topField = this.Q<StyleLengthField>("top");
            rightField = this.Q<StyleLengthField>("right");
            bottomField = this.Q<StyleLengthField>("bottom");
            leftField = this.Q<StyleLengthField>("left");

            SetupBaseCallbacks();
        }
    }

    [UxmlElement]
    internal partial class MarginFoldoutField : LengthFoldoutField
    {
        private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/Controls/MarginFoldoutField.uxml";

        protected override string topPropertyName => "marginTop";
        protected override string rightPropertyName => "marginRight";
        protected override string bottomPropertyName => "marginBottom";
        protected override string leftPropertyName => "marginLeft";

        public MarginFoldoutField() : base("Margin")
        {
            var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
            vta.CloneTree(contentContainer);

            topField = this.Q<StyleLengthField>("top");
            rightField = this.Q<StyleLengthField>("right");
            bottomField = this.Q<StyleLengthField>("bottom");
            leftField = this.Q<StyleLengthField>("left");

            SetupBaseCallbacks();
        }
    }
}
