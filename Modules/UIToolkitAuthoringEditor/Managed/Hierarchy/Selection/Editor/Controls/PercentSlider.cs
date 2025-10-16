// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class PercentSlider : BaseField<float>, IValueField<float>
    {
        static readonly string s_UxmlPath = "UIToolkitAuthoring/Inspector/Controls/PercentSlider.uxml";
        static readonly string s_UssPath = "UIToolkitAuthoring/Inspector/Controls/PercentSlider.uss";

        static readonly string k_DraggerFieldUssClassName = "unity-style-field__dragger-field";

        static readonly string s_UssClassName = "unity-percent-slider";
        static readonly string s_VisualInputName = "unity-visual-input";
        static readonly string s_SliderName = "unity-slider";

        [Serializable]
        public new class UxmlSerializedData : BaseField<float>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<float>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new PercentSlider();
        }

        SliderInt m_Slider;

        public override void SetValueWithoutNotify(float newValue)
        {
            base.SetValueWithoutNotify(newValue);

            RefreshSubFields();
        }

        public PercentSlider() : this(null) {}

        public PercentSlider(string label) : base(label)
        {
            AddToClassList(s_UssClassName);

            styleSheets.Add(EditorGUIUtility.Load(s_UssPath) as StyleSheet);

            var template = EditorGUIUtility.Load(s_UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);

            visualInput = this.Q(s_VisualInputName);
            m_Slider = this.Q<SliderInt>(s_SliderName);
            m_Slider.AddToClassList(k_DraggerFieldUssClassName);

            m_Slider.RegisterValueChangedCallback(OnSubFieldValueChange);

            var mouseDragger = new FieldMouseDragger<float>(this);
            mouseDragger.SetDragZone(labelElement);
            labelElement.AddToClassList(labelDraggerVariantUssClassName);
        }

        void RefreshSubFields()
        {
            var value100 = value * 100.0f;
            var intNewValue = (int)Math.Round(value100, 0);

            m_Slider.SetValueWithoutNotify(intNewValue);
            if (m_Slider.panel != null)
                m_Slider.OnViewDataReady(); // Hack to force update the slide handle position.
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

        void IValueField<float>.ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, float startValue)
        {
            double sensitivity = NumericFieldDraggerUtility.CalculateIntDragSensitivity((int)startValue, m_Slider.lowValue, m_Slider.highValue);
            float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
            long v = m_Slider.value;

            v += (long)Math.Round(NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity);
            m_Slider.value = (int)v;
        }

        void IValueField<float>.StartDragging()
        {
        }

        void IValueField<float>.StopDragging()
        {
        }
    }
}
