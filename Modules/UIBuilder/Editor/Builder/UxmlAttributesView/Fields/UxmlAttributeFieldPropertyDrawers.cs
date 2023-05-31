// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.UI.Builder;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [CustomPropertyDrawer(typeof(UxmlTypeReferenceAttribute))]
    class UxmlTypeReferencePropertyDrawer : PropertyDrawer
    {
        public static readonly PropertyName typeCompleterPropertyKey = new PropertyName("--unity-type-completer");

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var desiredType = ((UxmlTypeReferenceAttribute)attribute).baseType ?? typeof(object);
            var fieldFactory = new BuilderUxmlTypeAttributeFieldFactory();

            var uxmlAttribute = fieldInfo.GetCustomAttribute<UxmlAttributeAttribute>();
            var label = uxmlAttribute != null ? BuilderNameUtilities.ConvertDashToHuman(uxmlAttribute.name) : property.localizedDisplayName;
            var field = fieldFactory.CreateField(desiredType, label, null, null, null, null) as BindableElement;

            // Move the completer so that it remains accessible after BindProperty has replaced userData.
            field.SetProperty(typeCompleterPropertyKey, field.userData);

            field.BindProperty(property);
            return field;
        }
    }

    [CustomPropertyDrawer(typeof(EnumFieldValueDecoratorAttribute))]
    class EnumFieldValueDecoratorPropertyDrawer : PropertyDrawer
    {
        protected static readonly string k_TypePropertyName = BuilderUxmlAttributesView.UxmlSerializedDataPathPrefix + nameof(EnumField.typeAsString);
        protected static readonly string k_ValuePropertyName = BuilderUxmlAttributesView.UxmlSerializedDataPathPrefix + nameof(EnumField.valueAsString);
        protected static readonly string k_IncludeObsoleteValuesPropertyName = BuilderUxmlAttributesView.UxmlSerializedDataPathPrefix + nameof(EnumField.includeObsoleteValues);

        EnumField m_EnumValue;
        protected SerializedProperty m_ValueProperty;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_ValueProperty = property;

            m_EnumValue = new EnumField("Value");
            ConfigureField(m_EnumValue, property);
            UpdateField(property.serializedObject);
            return m_EnumValue;
        }

        protected void ConfigureField(VisualElement element, SerializedProperty property)
        {
            var typeProperty = property.serializedObject.FindProperty(k_TypePropertyName);
            var obsoleteValuesProperty = property.serializedObject.FindProperty(k_IncludeObsoleteValuesPropertyName);
            element.TrackPropertyValue(obsoleteValuesProperty, OnTrackedValueChanged);
            element.TrackPropertyValue(typeProperty, OnTrackedValueChanged);
            element.RegisterCallback<ChangeEvent<Enum>>(EnumValueChanged);
        }

        protected void EnumValueChanged(ChangeEvent<Enum> evt)
        {
            m_ValueProperty.stringValue = evt.newValue?.ToString();

            // Because we are bypassing the binding system we must save the modified SerializedObject.
            m_ValueProperty.serializedObject.ApplyModifiedProperties();
        }

        protected void OnTrackedValueChanged(SerializedProperty property) => UpdateField(property.serializedObject);

        protected virtual void UpdateField(SerializedObject serializedObject)
        {
            var typeProperty = serializedObject.FindProperty(k_TypePropertyName);
            var obsoleteValuesProperty = serializedObject.FindProperty(k_IncludeObsoleteValuesPropertyName);
            var valueProperty = serializedObject.FindProperty(k_ValuePropertyName);

            m_EnumValue.includeObsoleteValues = obsoleteValuesProperty.boolValue;
            m_EnumValue.typeAsString = typeProperty.stringValue;
            m_EnumValue.valueAsString = valueProperty.stringValue;

            m_EnumValue.SetEnabled(m_EnumValue.type != null);
        }
    }

    [CustomPropertyDrawer(typeof(EnumFlagsFieldValueDecoratorAttribute))]
    class EnumFlagsFieldValueDecoratorPropertyDrawer : EnumFieldValueDecoratorPropertyDrawer
    {
        EnumFlagsField m_EnumFlagsValue;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_ValueProperty = property;

            m_EnumFlagsValue = new EnumFlagsField("Value");

            ConfigureField(m_EnumFlagsValue, property);
            UpdateField(property.serializedObject);
            return m_EnumFlagsValue;
        }

        protected override void UpdateField(SerializedObject serializedObject)
        {
            var typeProperty = serializedObject.FindProperty(k_TypePropertyName);
            var obsoleteValuesProperty = serializedObject.FindProperty(k_IncludeObsoleteValuesPropertyName);
            var valueProperty = serializedObject.FindProperty(k_ValuePropertyName);

            m_EnumFlagsValue.includeObsoleteValues = obsoleteValuesProperty.boolValue;
            m_EnumFlagsValue.typeAsString = typeProperty.stringValue;
            m_EnumFlagsValue.valueAsString = valueProperty.stringValue;

            m_EnumFlagsValue.SetEnabled(m_EnumFlagsValue.type != null);
        }
    }

    [CustomPropertyDrawer(typeof(TagFieldValueDecoratorAttribute))]
    class TagFieldValueDecoratorAttributePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var tagField = new TagField("Value");
            tagField.BindProperty(property);
            return tagField;
        }
    }

    [CustomPropertyDrawer(typeof(ImageFieldValueDecoratorAttribute))]
    class ImageFieldValueDecoratorAttributePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var imageField = new ImageStyleField("Icon Image");
            imageField.BindProperty(property);
            imageField.AddToClassList(ImageStyleField.alignedFieldUssClassName);
            return imageField;
        }
    }
}
