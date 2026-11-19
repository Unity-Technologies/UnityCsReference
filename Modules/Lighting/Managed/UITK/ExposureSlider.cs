// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Lighting
{
    internal class ExposureSlider : VisualElement
    {
        const string k_ExposureIconName = "Exposure";
        const string k_LightingSearchUSSPath = "StyleSheets/LightingSearch.uss";
        const string k_ExposureSliderClassName = "lighting-search-exposure-slider";
        const string k_ExposureSliderSliderClassName = "lighting-search-exposure-slider__slider";
        const string k_ExposureSliderIconClassName = "lighting-search-exposure-slider__icon";
        const string k_ExposureSliderTextFieldClassName = "lighting-search-exposure-slider__textfield";

        readonly Slider m_Slider;
        readonly FloatField m_TextField;
        readonly float m_MinValue;
        readonly float m_MaxValue;

        public float value
        {
            get => m_Slider.value;
            set
            {
                if (!Mathf.Approximately(m_Slider.value, value))
                {
                    m_Slider.value = value;
                    m_TextField.SetValueWithoutNotify(value);
                }
            }
        }

        public ExposureSlider(float minValue, float maxValue)
        {
            m_MinValue = minValue;
            m_MaxValue = maxValue;

            AddToClassList(k_ExposureSliderClassName);

            StyleSheet uss = EditorResources.Load<Object>(k_LightingSearchUSSPath) as StyleSheet;
            styleSheets.Add(uss);

            Texture2D exposureIcon = EditorGUIUtility.FindTexture(k_ExposureIconName);
            VisualElement icon = new VisualElement();
            icon.style.backgroundImage = exposureIcon;
            icon.AddToClassList(k_ExposureSliderIconClassName);

            m_Slider = new Slider(null, minValue, maxValue);
            m_Slider.AddToClassList(k_ExposureSliderSliderClassName);

            m_TextField = new FloatField();
            m_TextField.AddToClassList(k_ExposureSliderTextFieldClassName);

            Add(icon);
            Add(m_Slider);
            Add(m_TextField);

            m_Slider.RegisterValueChangedCallback(OnSliderValueChanged);
            m_TextField.RegisterValueChangedCallback(OnTextFieldValueChanged);
        }

        void OnSliderValueChanged(ChangeEvent<float> evt)
        {
            m_TextField.SetValueWithoutNotify(evt.newValue);
        }

        void OnTextFieldValueChanged(ChangeEvent<float> evt)
        {
            float clampedValue = Mathf.Clamp(evt.newValue, m_MinValue, m_MaxValue);
            m_Slider.SetValueWithoutNotify(clampedValue);

            if (!Mathf.Approximately(clampedValue, evt.newValue))
            {
                m_TextField.SetValueWithoutNotify(clampedValue);
            }
        }

        public void RegisterValueChangedCallback(EventCallback<ChangeEvent<float>> callback)
        {
            m_Slider.RegisterValueChangedCallback(callback);
            m_TextField.RegisterValueChangedCallback(callback);
        }

        internal void SetValueWithoutNotify(float newValue)
        {
            m_Slider.SetValueWithoutNotify(newValue);
            m_TextField.SetValueWithoutNotify(newValue);
        }
    }
}
