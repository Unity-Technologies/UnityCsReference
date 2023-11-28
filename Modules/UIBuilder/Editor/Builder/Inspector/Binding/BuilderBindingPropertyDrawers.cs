// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [CustomPropertyDrawer(typeof(BindingModeDrawerAttribute))]
    class BindingModePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var enumField = new EnumField
            {
                bindingPath = property.propertyPath,
                label = property.displayName
            };
            enumField.AddToClassList(EnumField.alignedFieldUssClassName);
            return enumField;
        }
    }

    [CustomPropertyDrawer(typeof(DataSourceDrawerAttribute))]
    class BuilderDataSourcePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new BuilderObjectField()
            {
                bindingPath = property.propertyPath,
                objectType = typeof(ScriptableObject),
                label = " ",
            };
            field.AddToClassList(BuilderObjectField.alignedFieldUssClassName);
            return field;
        }
    }

    [CustomPropertyDrawer(typeof(BindingPathDrawerAttribute))]
    class BuilderBindingPathPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new TextField("Editor Binding Path");
            field.bindingPath = property.propertyPath;
            field.AddToClassList(TextField.alignedFieldUssClassName);
            return field;
        }
    }

    [CustomPropertyDrawer(typeof(DataSourceTypeDrawerAttribute))]
    class BuilderDataSourceTypePropertyDrawer : UxmlTypeReferencePropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = base.CreatePropertyGUI(property);
            var textField = field.Q<TextField>();
            textField.AddToClassList(TextField.alignedFieldUssClassName);
            textField.label = " ";
            textField.isDelayed = true;
            return field;
        }
    }

    [CustomPropertyDrawer(typeof(ConverterDrawerAttribute))]
    class BuilderConverterPropertyDrawer : PropertyDrawer
    {
        protected static readonly string k_BindingMode = nameof(DataBinding.bindingMode);
        protected static readonly string k_DataSource = nameof(DataBinding.dataSource);
        protected static readonly string k_DataSourceType = nameof(DataBinding.dataSourceTypeString);
        protected static readonly string k_DataSourcePathString = nameof(DataBinding.dataSourcePathString);
        protected static readonly string k_Property = nameof(DataBinding.property);

        private BindingConvertersField m_ConvertersField;

        private bool m_IsToSource;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_ConvertersField = new BindingConvertersField()
            {
                bindingPath = property.propertyPath,
                label = property.propertyPath.Contains("uiToSourceConverters") ? "To data source" : "To target property (UI)",
            };

            m_ConvertersField.AddToClassList(BindingConvertersField.alignedFieldUssClassName);
            var converterAttribute = (ConverterDrawerAttribute)attribute;
            m_IsToSource = converterAttribute.isConverterToSource;

            var rootPath = BuilderBindingUxmlAttributesView.GetSerializedDataBindingRoot(property.propertyPath);
            var rootProperty = property.serializedObject.FindProperty(rootPath);
            ConfigureField(m_ConvertersField, rootProperty);
            UpdateField(rootProperty);

            return m_ConvertersField;
        }

        protected void ConfigureField(VisualElement element, SerializedProperty rootProperty)
        {
            var bindingModeProperty = rootProperty.FindPropertyRelative(k_BindingMode);
            var dataSourceProperty = rootProperty.FindPropertyRelative(k_DataSource);
            var dataSourcePathStringProperty = rootProperty.FindPropertyRelative(k_DataSourcePathString);
            var dataSourceTypeProperty = rootProperty.FindPropertyRelative(k_DataSourceType);
            element.TrackPropertyValue(bindingModeProperty, OnTrackedBindingMode);
            element.TrackPropertyValue(dataSourceProperty, OnTrackedDataSource);
            element.TrackPropertyValue(dataSourceTypeProperty, OnTrackedDataSource);
            element.TrackPropertyValue(dataSourcePathStringProperty, OnTrackedDataSource);
        }

        private void OnTrackedBindingMode(SerializedProperty property)
        {
            var rootPath = BuilderBindingUxmlAttributesView.GetSerializedDataBindingRoot(property.propertyPath);
            var rootProperty = property.serializedObject.FindProperty(rootPath);
            UpdateField(rootProperty);
        }

        private void OnTrackedDataSource(SerializedProperty property)
        {
            var rootPath = BuilderBindingUxmlAttributesView.GetSerializedDataBindingRoot(property.propertyPath);
            var rootProperty = property.serializedObject.FindProperty(rootPath);
            UpdateConvertersContext(rootProperty);
        }

        private void UpdateConvertersContext(SerializedProperty rootProperty)
        {
            var currentElement = BuilderBindingWindow.activeWindow.view.m_AttributesView.currentElement;
            var dataSource = BuilderBindingWindow.activeWindow.view.m_AttributesView.dataSource;
            var dataSourceType = BuilderBindingWindow.activeWindow.view.m_AttributesView.dataSourceType;
            var propertyString = rootProperty.FindPropertyRelative(k_Property).stringValue;
            var propertyPathString = rootProperty.FindPropertyRelative(k_DataSourcePathString).stringValue;

            m_ConvertersField.SetDataSourceContext(currentElement, propertyPathString, propertyString, dataSource, dataSourceType, m_IsToSource);
        }

        private void UpdateField(SerializedProperty rootProperty)
        {
            var bindingModeProperty = rootProperty.FindPropertyRelative(k_BindingMode);
            var bindingMode = (BindingMode)bindingModeProperty.enumValueIndex;

            if (m_IsToSource)
            {
                m_ConvertersField.SetEnabled(bindingMode is BindingMode.TwoWay or BindingMode.ToSource);
                m_ConvertersField.tooltip = !m_ConvertersField.enabledSelf ? BuilderConstants.BindingWindowLocalConverterNotApplicableMessage : "";
            }
            else
            {
                m_ConvertersField.SetEnabled(bindingMode != BindingMode.ToSource);
                m_ConvertersField.tooltip = !m_ConvertersField.enabledSelf ? BuilderConstants.BindingWindowLocalConverterNotApplicableMessage : "";
            }
        }
    }
}
