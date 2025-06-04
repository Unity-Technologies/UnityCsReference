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
            var uxmlAttribute = fieldInfo.GetCustomAttribute<UxmlAttributeAttribute>();
            var label = uxmlAttribute != null ? BuilderNameUtilities.ConvertDashToHuman(uxmlAttribute.name) : property.localizedDisplayName;
            var field = new BuilderTypeField(label, desiredType);
            field.AddToClassList(BuilderTypeField.alignedFieldUssClassName);
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

    [CustomPropertyDrawer(typeof(SelectableTextElementAttribute))]
    class SelectableTextElementPropertyDrawer : PropertyDrawer
    {
        private SerializedProperty m_FocusableProperty;
        private SerializedProperty m_SelectableProperty;
        private Toggle m_SelectableField;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_SelectableProperty = property;
            var rootPath = property.propertyPath.Substring(0, property.propertyPath.LastIndexOf('.'));
            var rootProperty = property.serializedObject.FindProperty(rootPath);
            m_FocusableProperty = rootProperty.FindPropertyRelative("focusable");

            m_SelectableField = new Toggle("Selectable")
            {
                value = property.boolValue,
                bindingPath = property.propertyPath,
                classList = { Toggle.alignedFieldUssClassName }
            };

            m_SelectableField.TrackPropertyValue(m_FocusableProperty, (serializedProperty) =>
            {
                // If focusable is set to false, we need to reset the isSelectable value as well as it doesn't work
                // without focusable on. It strictly follows the logic present in the isSelectable setter.
                if (serializedProperty.boolValue == false && m_SelectableField.value)
                {
                    m_SelectableField.value = false;
                    m_SelectableProperty.boolValue = false;
                    m_SelectableProperty.serializedObject.ApplyModifiedProperties();
                }
            });

            m_SelectableField.TrackPropertyValue(m_SelectableProperty, (serializedProperty) =>
            {
                m_SelectableProperty.boolValue = serializedProperty.boolValue;
                m_FocusableProperty.boolValue = serializedProperty.boolValue;
                m_SelectableProperty.serializedObject.ApplyModifiedProperties();
            });

            return m_SelectableField;
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

            var undoMessage = $"Modified {m_ValueProperty.name}";
            if (m_ValueProperty.m_SerializedObject.targetObject.name != string.Empty)
                undoMessage += $" in {m_ValueProperty.m_SerializedObject.targetObject.name}";

            Undo.RegisterCompleteObjectUndo(m_ValueProperty.m_SerializedObject.targetObject, undoMessage);

            if (evt.newValue < 1)
            {
                SetNegativeFixedItemHeightHelpBoxEnabled(true, field);
                field.SetValueWithoutNotify(1);
                m_ValueProperty.floatValue = 1f;
                m_ValueProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                return;
            }

            m_ValueProperty.floatValue = evt.newValue;
            m_ValueProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
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
                    negativeWarningHelpBox.AddToClassList(BuilderConstants.InspectorShownWarningMessageClassName);
                }
                else
                {
                    negativeWarningHelpBox.AddToClassList(BuilderConstants.InspectorShownWarningMessageClassName);
                    negativeWarningHelpBox.RemoveFromClassList(BuilderConstants.InspectorHiddenWarningMessageClassName);
                }
                return;
            }

            if (negativeWarningHelpBox == null)
                return;
            negativeWarningHelpBox.AddToClassList(BuilderConstants.InspectorHiddenWarningMessageClassName);
            negativeWarningHelpBox.RemoveFromClassList(BuilderConstants.InspectorShownWarningMessageClassName);
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
            // simplify undo message
            var undoMessage = $"Modified {m_ValueProperty.name}";
            if (m_ValueProperty.m_SerializedObject.targetObject.name != string.Empty)
                undoMessage += $" in {m_ValueProperty.m_SerializedObject.targetObject.name}";

            Undo.RegisterCompleteObjectUndo(m_ValueProperty.m_SerializedObject.targetObject, undoMessage);

            m_ValueProperty.stringValue = evt.newValue?.ToString();

            // Because we are bypassing the binding system we must save the modified SerializedObject.
            m_ValueProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
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

    [CustomPropertyDrawer(typeof(AdvanceTextGeneratorDecoratorAttribute))]
    class AdvanceTextGeneratorDecoratorAttributePropertyDrawer : PropertyDrawer
    {
        private Toggle textNativeField;
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            textNativeField = new Toggle("Enable Advanced Text");
            textNativeField.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            textNativeField.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            textNativeField.BindProperty(property);
            textNativeField.AddToClassList(TextField.alignedFieldUssClassName);
            return textNativeField;
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            var target = (VisualElement)evt.currentTarget;
            EnableDisplay(UIToolkitProjectSettings.enableAdvancedText);
            UIToolkitProjectSettings.onEnableAdvancedTextChanged += EnableDisplay;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UIToolkitProjectSettings.onEnableAdvancedTextChanged -= EnableDisplay;
        }

        private void EnableDisplay(bool enable)
        {
            textNativeField.parent.parent.parent.style.display = enable ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
