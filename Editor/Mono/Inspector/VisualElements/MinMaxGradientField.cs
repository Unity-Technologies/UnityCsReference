// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class MinMaxGradientField : BaseField<ParticleSystem.MinMaxGradient>
    {
        public new static readonly string ussClassName = "unity-min-max-gradient-field";
        public static readonly string visualInputUssClass = ussClassName + "__visual-input";
        public static readonly string dropdownFieldUssClass = ussClassName + "__dropdown-field";
        public static readonly string dropdownInputUssClass = ussClassName + "__dropdown-input";
        public static readonly string gradientContainerUssClass = ussClassName + "__gradient-container";
        public static readonly string colorFieldUssClass = ussClassName + "__color-field";
        public static readonly string multipleValuesLabelUssClass = ussClassName + "__multiple-values-label";

        public readonly string[] stringModes = new[]
        {
            L10n.Tr("Color"),
            L10n.Tr("Gradient"),
            L10n.Tr("Random Between Two Colors"),
            L10n.Tr("Random Between Two Gradients"),
            L10n.Tr("Random Color")
        };

        VisualElement m_ColorMin;
        VisualElement m_ColorMax;
        VisualElement m_GradientMin;
        VisualElement m_GradientMax;
        VisualElement m_ModeDropdown;
        VisualElement m_GradientsContainer;
        VisualElement m_MixedValueTypeLabel;

        public MinMaxGradientField()
            : this(null, null) { }

        public MinMaxGradientField(MinMaxGradientPropertyDrawer.PropertyData propertyData, string label) : base(label, null)
        {
            m_ColorMin = EditorUIService.instance.CreatePropertyField(propertyData.colorMin, "");
            m_ColorMin.AddToClassList(colorFieldUssClass);
            m_ColorMax = EditorUIService.instance.CreatePropertyField(propertyData.colorMax, "");
            m_ColorMax.AddToClassList(colorFieldUssClass);
            m_GradientMin = EditorUIService.instance.CreatePropertyField(propertyData.gradientMin, "");
            m_GradientMax = EditorUIService.instance.CreatePropertyField(propertyData.gradientMax, "");
            m_ModeDropdown = EditorUIService.instance.CreateDropdownField(stringModes, () => propertyData.mode.intValue);
            m_ModeDropdown.AddToClassList(dropdownFieldUssClass);
            m_MixedValueTypeLabel = new Label("\u2014");
            m_MixedValueTypeLabel.AddToClassList(multipleValuesLabelUssClass);

            var dropdownInput = m_ModeDropdown.Q<VisualElement>(null, "unity-popup-field__input");
            dropdownInput.AddToClassList(dropdownInputUssClass);

            m_GradientsContainer = new VisualElement();
            m_GradientsContainer.AddToClassList(gradientContainerUssClass);
            m_GradientsContainer.Add(m_GradientMin);
            m_GradientsContainer.Add(m_GradientMax);

            visualInput.AddToClassList(visualInputUssClass);
            visualInput.Add(m_ColorMin);
            visualInput.Add(m_ColorMax);
            visualInput.Add(m_GradientsContainer);
            visualInput.Add(m_MixedValueTypeLabel);
            visualInput.Add(m_ModeDropdown);

            m_ModeDropdown.RegisterCallback<ChangeEvent<string>>(e =>
            {
                var index = Array.IndexOf(stringModes, e.newValue);
                var mode = (MinMaxGradientState)index;

                propertyData.mode.intValue = index;
                propertyData.mode.serializedObject.ApplyModifiedProperties();
                UpdateFieldsDisplay(propertyData.mode);
            });

            UpdateFieldsDisplay(propertyData.mode);
        }

        private void UpdateFieldsDisplay(SerializedProperty mode)
        {
            var hasMixedValues = mode.hasMultipleDifferentValues;

            m_ColorMin.style.display = DisplayStyle.None;
            m_ColorMax.style.display = DisplayStyle.None;
            m_GradientMin.style.display = DisplayStyle.None;
            m_GradientMax.style.display = DisplayStyle.None;
            m_GradientsContainer.style.display = DisplayStyle.None;
            m_MixedValueTypeLabel.style.display = hasMixedValues ? DisplayStyle.Flex : DisplayStyle.None;

            if (hasMixedValues)
            {
                return;
            }

            var modeValue = (MinMaxGradientState)mode.intValue;

            switch (modeValue)
            {
                case MinMaxGradientState.k_Color:
                    m_ColorMax.style.display = DisplayStyle.Flex;
                    break;
                case MinMaxGradientState.k_Gradient:
                case MinMaxGradientState.k_RandomColor:
                    m_GradientsContainer.style.display = DisplayStyle.Flex;
                    m_GradientMax.style.display = DisplayStyle.Flex;
                    break;
                case MinMaxGradientState.k_RandomBetweenTwoColors:
                    m_ColorMin.style.display = DisplayStyle.Flex;
                    m_ColorMax.style.display = DisplayStyle.Flex;
                    break;
                case MinMaxGradientState.k_RandomBetweenTwoGradients:
                    m_GradientsContainer.style.display = DisplayStyle.Flex;
                    m_GradientMin.style.display = DisplayStyle.Flex;
                    m_GradientMax.style.display = DisplayStyle.Flex;
                    break;
            }
        }
    }
}
