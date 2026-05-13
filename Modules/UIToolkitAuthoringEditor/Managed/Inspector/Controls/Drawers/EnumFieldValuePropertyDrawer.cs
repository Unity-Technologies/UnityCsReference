// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    class BaseEnumStringValueField<TField> : BaseField<string> where TField : BaseField<Enum>, new()
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string UssClass = "unity-enum-string-value-field";

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public static readonly string InputUssClass = UssClass + "__input";

        readonly Func<TField, string> m_GetFieldValue;
        readonly Action<TField, string> m_SetFieldValue;

        bool m_SettingValueWithoutNotify;

        public TField EnumField => (visualInput as TField);

        public BaseEnumStringValueField(string label, Func<TField, string> getFieldValue,
            Action<TField, string> setFieldValue) : base(label, new TField())
        {
            AddToClassList(UssClass);
            visualInput.AddToClassList(InputUssClass);

            // Load stylesheet
            var styleSheet =
                EditorGUIUtility.Load("UIToolkitAuthoring/Inspector/Controls/Fields/EnumStringValueField.uss") as StyleSheet;
            if (styleSheet != null)
                styleSheets.Add(styleSheet);

            m_GetFieldValue = getFieldValue;
            m_SetFieldValue = setFieldValue;

            (visualInput as TField).RegisterCallback<ChangeEvent<Enum>>(OnValueChanged);
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            if (m_SettingValueWithoutNotify)
            {
                base.SetValueWithoutNotify(newValue);
            }
            else
            {
                try
                {
                    m_SettingValueWithoutNotify = true;

                    var inputField = visualInput as TField;

                    m_SetFieldValue(inputField, newValue);
                    base.SetValueWithoutNotify(m_GetFieldValue(inputField));
                }
                finally
                {
                    m_SettingValueWithoutNotify = false;
                }
            }
        }

        void OnValueChanged(ChangeEvent<Enum> evt)
        {
            if (m_SettingValueWithoutNotify)
                return;

            var inputField = visualInput as TField;
            value = m_GetFieldValue(inputField);
            evt.StopImmediatePropagation();
        }
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class EnumStringValueField : BaseEnumStringValueField<EnumField>
    {
        static readonly Func<EnumField, string> s_GetFieldValue = (field) => field.valueAsString;
        static readonly Action<EnumField, string> s_SetFieldValue = (field, value) => field.valueAsString = value;

        public EnumStringValueField(string label) : base(label, s_GetFieldValue, s_SetFieldValue)
        {
        }
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class EnumFlagsStringValueField : BaseEnumStringValueField<EnumFlagsField>
    {
        static readonly Func<EnumFlagsField, string> s_GetFieldValue = (field) => field.valueAsString;
        static readonly Action<EnumFlagsField, string> s_SetFieldValue = (field, value) => field.valueAsString = value;

        public EnumFlagsStringValueField(string label) : base(label, s_GetFieldValue, s_SetFieldValue)
        {
        }
    }

    abstract class BaseEnumFieldPropertyDrawer<TFieldWrapper, TEnumField> : PropertyDrawer
        where TFieldWrapper : BaseEnumStringValueField<TEnumField>
        where TEnumField : BaseField<Enum>, new()
    {
        protected static readonly string k_TypePropertyName = nameof(EnumField.type);
        protected static readonly string k_IncludeObsoleteValuesPropertyName = nameof(EnumField.includeObsoleteValues);
        protected static readonly string k_ValueAsStringPropertyName = nameof(EnumField.valueAsString);

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = (TFieldWrapper)Activator.CreateInstance(typeof(TFieldWrapper), "Value");
            field.AddToClassList(TextField.alignedFieldUssClassName);

            ConfigureField(field, property);
            UpdateField(property, field);

            return field;
        }

        protected void ConfigureField(BindableElement element, SerializedProperty property)
        {
            var parentProperty = property.Copy();
            parentProperty.Parent();

            var typeProperty = parentProperty.FindPropertyRelative(k_TypePropertyName);
            var obsoleteValuesProperty = parentProperty.FindPropertyRelative(k_IncludeObsoleteValuesPropertyName);
            element.TrackPropertyValue(obsoleteValuesProperty, a => UpdateField(a, element));
            element.TrackPropertyValue(typeProperty, a => UpdateField(a, element));
            element.bindingPath = property.propertyPath;
        }

        protected void UpdateField(SerializedProperty property, VisualElement element)
        {
            var parentProperty = property.Copy();
            parentProperty.Parent();

            var typeProperty = parentProperty.FindPropertyRelative(k_TypePropertyName);
            var obsoleteValuesProperty = parentProperty.FindPropertyRelative(k_IncludeObsoleteValuesPropertyName);
            var valueProperty = parentProperty.FindPropertyRelative(k_ValueAsStringPropertyName);

            var wrapper = element as TFieldWrapper;
            var enumField = wrapper.EnumField;
            var enumType = UxmlUtility.ParseType(typeProperty.stringValue);

            UpdateEnumField(enumField, enumType, obsoleteValuesProperty.boolValue, valueProperty.stringValue);
            enumField.SetEnabled(enumType != null);
        }

        protected abstract void UpdateEnumField(TEnumField enumField, Type enumType, bool includeObsoleteValues, string valueAsString);
    }

    [CustomPropertyDrawer(typeof(EnumFieldValueDecoratorAttribute))]
    class EnumFieldValuePropertyDrawer : BaseEnumFieldPropertyDrawer<EnumStringValueField, EnumField>
    {
        protected override void UpdateEnumField(EnumField enumField, Type enumType,
            bool includeObsoleteValues, string valueAsString)
        {
            enumField.includeObsoleteValues = includeObsoleteValues;
            enumField.type = enumType;
            enumField.valueAsString = valueAsString;
        }
    }

    [CustomPropertyDrawer(typeof(EnumFlagsFieldValueDecoratorAttribute))]
    class EnumFlagsFieldValuePropertyDrawer : BaseEnumFieldPropertyDrawer<EnumFlagsStringValueField, EnumFlagsField>
    {
        protected override void UpdateEnumField(EnumFlagsField enumFlagsValue, Type enumType,
            bool includeObsoleteValues, string valueAsString)
        {
            enumFlagsValue.includeObsoleteValues = includeObsoleteValues;
            enumFlagsValue.type = enumType;
            enumFlagsValue.valueAsString = valueAsString;
        }
    }
}
