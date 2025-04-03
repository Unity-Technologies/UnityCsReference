// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [CustomPropertyDrawer(typeof(FilterParameterDeclaration))]
    class FilterParameterPropertyDrawer : PropertyDrawer
    {
        const string k_NameTooltip = "The parameter name, used for display in the UI Builder.";
        const string k_InterpolationDefaultValueTooltip = "The default interpolation value when transitioning to/from a default filter. This is typically the \"no effect\" value.";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();

            var nameProperty = property.FindPropertyRelative("m_Name");
            var nameField = new PropertyField(nameProperty);
            nameField.AddToClassList(ObjectField.alignedFieldUssClassName);
            nameField.tooltip = k_NameTooltip;
            container.Add(nameField);

            var interpolationDefaultValue = property.FindPropertyRelative("m_InterpolationDefaultValue");

            var typeProperty = interpolationDefaultValue.FindPropertyRelative("m_Type");
            var typeField = new PropertyField(typeProperty);
            typeField.AddToClassList(ObjectField.alignedFieldUssClassName);
            typeField.TrackPropertyValue(typeProperty, OnTrackTypePropertyValue);
            container.Add(typeField);

            var defaultFloatValueField = new PropertyField(interpolationDefaultValue.FindPropertyRelative("m_FloatValue"));
            defaultFloatValueField.name = "default-float-value-field";
            defaultFloatValueField.label = "Interpolation Default";
            defaultFloatValueField.tooltip = k_InterpolationDefaultValueTooltip;
            defaultFloatValueField.AddToClassList(ObjectField.alignedFieldUssClassName);
            container.Add(defaultFloatValueField);

            var defaultColorValueField = new PropertyField(interpolationDefaultValue.FindPropertyRelative("m_ColorValue"));
            defaultColorValueField.name = "default-color-value-field";
            defaultColorValueField.label = "Interpolation Default";
            defaultColorValueField.tooltip = k_InterpolationDefaultValueTooltip;
            defaultColorValueField.AddToClassList(ObjectField.alignedFieldUssClassName);
            container.Add(defaultColorValueField);

            UpdateVisibleFields(typeProperty, defaultFloatValueField, defaultColorValueField);

            return container;
        }

        void OnTrackTypePropertyValue(object obj, SerializedProperty property)
        {
            if (obj is not PropertyField target)
                return;

            var defaultFloatValueField = target.parent.Q("default-float-value-field");
            var defaultColorValueField = target.parent.Q("default-color-value-field");

            UpdateVisibleFields(property, defaultFloatValueField, defaultColorValueField);
        }

        void UpdateVisibleFields(SerializedProperty typeProperty, VisualElement defaultFloatValueField, VisualElement defaultColorValueField)
        {
            defaultFloatValueField.style.display = typeProperty.enumValueIndex == (int)FilterParameterType.Float ? DisplayStyle.Flex : DisplayStyle.None;
            defaultColorValueField.style.display = typeProperty.enumValueIndex == (int)FilterParameterType.Color ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
