using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class PercentSlider : BaseField<float>
    {
        static readonly string s_UssPath = BuilderConstants.UtilitiesPath + "/PercentSlider/PercentSlider.uss";
        static readonly string s_UxmlPath = BuilderConstants.UtilitiesPath + "/PercentSlider/PercentSlider.uxml";

        static readonly string s_UssClassName = "unity-percent-slider";
        static readonly string s_VisualInputName = "unity-visual-input";
        static readonly string s_SliderName = "unity-slider";
        static readonly string s_FieldName = "unity-field";

        public new class UxmlFactory : UxmlFactory<PercentSlider, UxmlTraits> { }

        public new class UxmlTraits : BaseField<float>.UxmlTraits
        {
            public UxmlTraits()
            {
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
            }
        }

        SliderInt mSlider;
        IntegerField mField;

        public override void SetValueWithoutNotify(float newValue)
        {
            base.SetValueWithoutNotify(newValue);

            RefreshSubFields();
        }

        public PercentSlider() : this(null) {}

        public PercentSlider(string label) : base(label)
        {
            AddToClassList(s_UssClassName);

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPath));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);
            template.CloneTree(this);

            visualInput = this.Q(s_VisualInputName);
            mSlider = this.Q<SliderInt>(s_SliderName);
            mField = this.Q<IntegerField>(s_FieldName);

            mSlider.RegisterValueChangedCallback(OnSubFieldValueChange);
            mField.RegisterValueChangedCallback(OnSubFieldValueChange);
        }

        void RefreshSubFields()
        {
            var value100 = value * 100.0f;
            var intNewValue = (int)Math.Round(value100, 0);

            mSlider.SetValueWithoutNotify(intNewValue);
            if (mSlider.elementPanel != null)
                mSlider.OnViewDataReady(); // Hack to force update the slide handle position.

            mField.SetValueWithoutNotify(intNewValue);
        }

        void OnSubFieldValueChange(ChangeEvent<int> evt)
        {
            var newValue = Mathf.Clamp(evt.newValue, 0, 100);
            var newValueFloat = ((float)newValue) / 100;

            if (value == newValueFloat)
                RefreshSubFields(); // This enforces min/max on the sub fields.
            else
                value = newValueFloat;
        }
    }
}
