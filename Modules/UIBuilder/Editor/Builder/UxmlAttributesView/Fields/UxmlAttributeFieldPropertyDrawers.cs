// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using Unity.UI.Builder;
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
            field.AddToClassList(TextField.alignedFieldUssClassName);

            // Move the completer so that it remains accessible after BindProperty has replaced userData.
            field.SetProperty(typeCompleterPropertyKey, field.userData);

            field.BindProperty(property);
            return field;
        }
    }

    [CustomPropertyDrawer(typeof(LayerDecoratorAttribute))]
    class LayerDecoratorPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new LayerField("Value");
            field.AddToClassList(LayerField.alignedFieldUssClassName);
            field.BindProperty(property);
            return field;
        }
    }

    [CustomPropertyDrawer(typeof(MultilineDecoratorAttribute))]
    class MultilineDecoratorPropertyDrawer : PropertyDrawer
    {
        private SerializedProperty m_Property;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_Property = property;

            var field = new Toggle(property.displayName);

            field.AddToClassList(Toggle.alignedFieldUssClassName);
            field.bindingPath = property.propertyPath;
            field.RegisterCallback<ChangeEvent<bool>>(evt => SetMultilineOfValueField(evt.newValue, evt.target as VisualElement));

            return field;
        }

        void SetMultilineOfValueField(bool multiline, VisualElement visualElement)
        {
            var inspector = visualElement.GetFirstAncestorOfType<BuilderInspector>();
            var valueFieldInInspector = inspector.Query<TextField>().Where(x => x.label is "Value").First();
            if (valueFieldInInspector == null)
            {
                var propertyField = inspector.Query<PropertyField>().Where(x => x.label is "Value").First();
                propertyField?.RegisterCallback<SerializedPropertyBindEvent>(_ =>
                {
                    EditorApplication.delayCall += () => SetMultilineOfValueField(multiline, visualElement);
                });
                return;
            }

            valueFieldInInspector.multiline = multiline;
            valueFieldInInspector.EnableInClassList(BuilderConstants.InspectorMultiLineTextFieldClassName, multiline);
        }
    }

    [CustomPropertyDrawer(typeof(MultilineTextFieldAttribute))]
    class MultilineTextFieldAttributePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new TextField
            {
                label = property.localizedDisplayName,
                multiline = true,
                bindingPath = property.propertyPath,
                classList = { TextField.alignedFieldUssClassName }
            };
        }
    }

    [CustomPropertyDrawer(typeof(FixedItemHeightDecoratorAttribute))]
    class FixedItemHeightDecoratorPropertyDrawer : PropertyDrawer
    {
        protected SerializedProperty m_ValueProperty;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_ValueProperty = property;

            var uiField = new IntegerField(property.displayName)
            {
                isDelayed = true
            };

            uiField.AddToClassList(BaseField<int>.alignedFieldUssClassName);
            ConfigureField(uiField, property);
            UpdateField(property, uiField);
            return uiField;
        }

        private void ConfigureField(IntegerField field, SerializedProperty property)
        {
            field.RegisterCallback<InputEvent>(OnFixedHeightValueChangedImmediately);
            field.labelElement.RegisterCallback<PointerMoveEvent>(OnFixedHeightValueChangedImmediately);
            field.TrackPropertyValue(property, a => UpdateField(a, field));
            field.RegisterCallback<ChangeEvent<int>>(OnFixedItemHeightValueChanged);
        }

        private void UpdateField(SerializedProperty property, IntegerField field)
        {
            field.value = (int)property.floatValue;
        }

        void OnFixedItemHeightValueChanged(ChangeEvent<int> evt)
        {
            var field = evt.currentTarget as IntegerField;

            if (evt.newValue < 1)
            {
                SetNegativeFixedItemHeightHelpBoxEnabled(true, field);
                field.SetValueWithoutNotify(1);
                m_ValueProperty.floatValue = 1f;
                m_ValueProperty.serializedObject.ApplyModifiedProperties();
                return;
            }

            m_ValueProperty.floatValue = evt.newValue;
            m_ValueProperty.serializedObject.ApplyModifiedProperties();
        }

        void OnFixedHeightValueChangedImmediately(InputEvent evt)
        {
            var field = evt.currentTarget as BaseField<int>;
            if (field == null)
                return;

            var newValue = evt.newData;
            var valueResolved = UINumericFieldsUtils.TryConvertStringToLong(newValue, out var v);
            var resolvedValue = valueResolved ? Mathf.ClampToInt(v) : field.value;

            SetNegativeFixedItemHeightHelpBoxEnabled((newValue.Length != 0 && (resolvedValue < 1 || newValue.Equals("-"))), field);
        }

        void OnFixedHeightValueChangedImmediately(PointerMoveEvent evt)
        {
            if (evt.target is not Label labelElement)
                return;

            var field = labelElement.parent as TextInputBaseField<int>;
            if (field == null)
                return;
            var valueResolved = UINumericFieldsUtils.TryConvertStringToLong(field.text, out var v);
            var resolvedValue = valueResolved ? Mathf.ClampToInt(v) : field.value;

            SetNegativeFixedItemHeightHelpBoxEnabled((resolvedValue < 1 || field.text.ToCharArray()[0].Equals('-')), field);
        }

        void SetNegativeFixedItemHeightHelpBoxEnabled(bool enabled, BaseField<int> field)
        {
            var negativeWarningHelpBox = field.parent.Q<UnityEngine.UIElements.HelpBox>();
            if (enabled)
            {
                if (negativeWarningHelpBox == null)
                {
                    negativeWarningHelpBox = new UnityEngine.UIElements.HelpBox(
                        L10n.Tr(BuilderConstants.HeightIntFieldValueCannotBeNegativeMessage), HelpBoxMessageType.Warning);
                    field.parent.Add(negativeWarningHelpBox);
                    negativeWarningHelpBox.EnableInClassList(BuilderConstants.InspectorShownNegativeWarningMessageClassName, true);
                }
                else
                {
                    negativeWarningHelpBox.EnableInClassList(BuilderConstants.InspectorShownNegativeWarningMessageClassName, true);
                    negativeWarningHelpBox.EnableInClassList(BuilderConstants.InspectorHiddenNegativeWarningMessageClassName, false);
                }
                return;
            }

            if (negativeWarningHelpBox == null)
                return;
            negativeWarningHelpBox.EnableInClassList(BuilderConstants.InspectorHiddenNegativeWarningMessageClassName, true);
            negativeWarningHelpBox.EnableInClassList(BuilderConstants.InspectorShownNegativeWarningMessageClassName, false);
        }
    }

    [CustomPropertyDrawer(typeof(EnumFieldValueDecoratorAttribute))]
    class EnumFieldValueDecoratorPropertyDrawer : PropertyDrawer
    {
        protected static readonly string k_TypePropertyName = nameof(EnumField.typeAsString);
        protected static readonly string k_ValuePropertyName = nameof(EnumField.valueAsString);
        protected static readonly string k_IncludeObsoleteValuesPropertyName = nameof(EnumField.includeObsoleteValues);

        protected SerializedProperty m_ValueProperty;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_ValueProperty = property;

            var enumValue = new EnumField("Value");
            enumValue.AddToClassList(TextField.alignedFieldUssClassName);
            ConfigureField(enumValue, property);
            UpdateField(property, enumValue);

            return enumValue;
        }

        protected void ConfigureField(VisualElement element, SerializedProperty property)
        {
            var rootPath = BuilderUxmlAttributesView.GetSerializedDataRoot(property.propertyPath);
            var rootProperty = property.serializedObject.FindProperty(rootPath);

            var typeProperty = rootProperty.FindPropertyRelative(k_TypePropertyName);
            var obsoleteValuesProperty = rootProperty.FindPropertyRelative(k_IncludeObsoleteValuesPropertyName);
            element.TrackPropertyValue(obsoleteValuesProperty, a => UpdateField(a, element));
            element.TrackPropertyValue(typeProperty, a => UpdateField(a, element));
            element.RegisterCallback<ChangeEvent<Enum>>(EnumValueChanged);
        }

        protected void EnumValueChanged(ChangeEvent<Enum> evt)
        {
            m_ValueProperty.stringValue = evt.newValue?.ToString();

            // Because we are bypassing the binding system we must save the modified SerializedObject.
            m_ValueProperty.serializedObject.ApplyModifiedProperties();
        }

        protected virtual void UpdateField(SerializedProperty property, VisualElement element)
        {
            var rootPath = BuilderUxmlAttributesView.GetSerializedDataRoot(property.propertyPath);
            var rootProperty = property.serializedObject.FindProperty(rootPath);

            var typeProperty = rootProperty.FindPropertyRelative(k_TypePropertyName);
            var obsoleteValuesProperty = rootProperty.FindPropertyRelative(k_IncludeObsoleteValuesPropertyName);
            var valueProperty = rootProperty.FindPropertyRelative(k_ValuePropertyName);

            var enumField = element as EnumField;
            enumField.includeObsoleteValues = obsoleteValuesProperty.boolValue;
            enumField.typeAsString = typeProperty.stringValue;
            enumField.valueAsString = valueProperty.stringValue;

            enumField.SetEnabled(enumField.type != null);
        }
    }

    [CustomPropertyDrawer(typeof(EnumFlagsFieldValueDecoratorAttribute))]
    class EnumFlagsFieldValueDecoratorPropertyDrawer : EnumFieldValueDecoratorPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_ValueProperty = property;

            var enumFlagsValue = new EnumFlagsField("Value");
            enumFlagsValue.AddToClassList(TextField.alignedFieldUssClassName);

            ConfigureField(enumFlagsValue, property);
            UpdateField(property, enumFlagsValue);
            return enumFlagsValue;
        }

        protected override void UpdateField(SerializedProperty property, VisualElement element)
        {
            var rootPath = BuilderUxmlAttributesView.GetSerializedDataRoot(property.propertyPath);
            var rootProperty = property.serializedObject.FindProperty(rootPath);

            var typeProperty = rootProperty.FindPropertyRelative(k_TypePropertyName);
            var obsoleteValuesProperty = rootProperty.FindPropertyRelative(k_IncludeObsoleteValuesPropertyName);
            var valueProperty = rootProperty.FindPropertyRelative(k_ValuePropertyName);

            var enumFlagsValue = element as EnumFlagsField;
            enumFlagsValue.includeObsoleteValues = obsoleteValuesProperty.boolValue;
            enumFlagsValue.typeAsString = typeProperty.stringValue;
            enumFlagsValue.valueAsString = valueProperty.stringValue;

            enumFlagsValue.SetEnabled(enumFlagsValue.type != null);
        }
    }

    [CustomPropertyDrawer(typeof(TagFieldValueDecoratorAttribute))]
    class TagFieldValueDecoratorAttributePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var tagField = new TagField("Value");
            tagField.AddToClassList(TextField.alignedFieldUssClassName);
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
