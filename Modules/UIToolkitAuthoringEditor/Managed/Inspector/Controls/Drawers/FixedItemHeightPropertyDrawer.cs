// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [CustomPropertyDrawer(typeof(FixedItemHeightDecoratorAttribute))]
    class FixedItemHeightPropertyDrawer : PropertyDrawer
    {
        const string k_InspectorShownWarningMessageClassName = "unity-uxml-attribute__negative-warning--shown";
        const string k_InspectorHiddenWarningMessageClassName = "unity-uxml-attribute__negative-warning--hidden";
        const string k_FixedItemHeightClassName = "unity-uxml-attribute__fixed-item-height";
        static readonly string k_HeightIntFieldValueCannotBeNegativeMessage = L10n.Tr("Please enter a positive number. Non-positive numbers will default to 1.");

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var uiField = new IntegerField(property.displayName)
            {
                isDelayed = true
            };

            uiField.AddToClassList(BaseField<int>.alignedFieldUssClassName);
            uiField.AddToClassList(k_FixedItemHeightClassName);

            // Capture property in local scope to avoid drawer instance sharing issues
            ConfigureField(uiField, property);
            UpdateField(property, uiField);
            return uiField;
        }

        void ConfigureField(IntegerField field, SerializedProperty property)
        {
            // Capture property in closures to ensure each field references the correct property
            field.RegisterCallback<InputEvent>(evt => OnFixedHeightValueChangedImmediately(evt, field));
            field.labelElement.RegisterCallback<PointerMoveEvent>(evt => OnFixedHeightValueChangedImmediately(evt, field));
            field.TrackPropertyValue(property, a => UpdateField(a, field));
            field.RegisterCallback<ChangeEvent<int>>(evt => OnFixedItemHeightValueChanged(evt, property, field));
        }

        static void UpdateField(SerializedProperty property, IntegerField field)
        {
            field.SetValueWithoutNotify((int)property.floatValue);
        }

        static void OnFixedItemHeightValueChanged(ChangeEvent<int> evt, SerializedProperty valueProperty, IntegerField field)
        {
            var undoMessage = $"Modified {valueProperty.name}";
            if (valueProperty.serializedObject.targetObject.name != string.Empty)
                undoMessage += $" in {valueProperty.serializedObject.targetObject.name}";

            Undo.RegisterCompleteObjectUndo(valueProperty.serializedObject.targetObject, undoMessage);

            if (evt.newValue < 1)
            {
                SetNegativeFixedItemHeightHelpBoxEnabled(true, field);
                field.SetValueWithoutNotify(1);
                valueProperty.floatValue = 1f;
                valueProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                return;
            }

            valueProperty.floatValue = evt.newValue;
            valueProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static void OnFixedHeightValueChangedImmediately(InputEvent evt, BaseField<int> field)
        {
            var newValue = evt.newData;
            if (string.IsNullOrEmpty(newValue))
            {
                SetNegativeFixedItemHeightHelpBoxEnabled(false, field);
                return;
            }

            var valueResolved = UINumericFieldsUtils.TryConvertStringToLong(newValue, out var v);
            var resolvedValue = valueResolved ? Mathf.ClampToInt(v) : field.value;

            SetNegativeFixedItemHeightHelpBoxEnabled((resolvedValue < 1 || newValue.Equals("-")), field);
        }

        static void OnFixedHeightValueChangedImmediately(PointerMoveEvent evt, TextInputBaseField<int> field)
        {
            if (evt.target is not Label)
                return;

            var valueResolved = UINumericFieldsUtils.TryConvertStringToLong(field.text, out var v);
            var resolvedValue = valueResolved ? Mathf.ClampToInt(v) : field.value;

            SetNegativeFixedItemHeightHelpBoxEnabled((field.text.Length != 0 && (resolvedValue < 1 || field.text[0] == '-')), field);
        }

        static void SetNegativeFixedItemHeightHelpBoxEnabled(bool enabled, BaseField<int> field)
        {
            var negativeWarningHelpBox = field.parent.Q<HelpBox>();
            if (enabled)
            {
                if (negativeWarningHelpBox == null)
                {
                    negativeWarningHelpBox = new HelpBox(
                        k_HeightIntFieldValueCannotBeNegativeMessage, HelpBoxMessageType.Warning);
                    field.parent.Add(negativeWarningHelpBox);
                    negativeWarningHelpBox.AddToClassList(k_InspectorShownWarningMessageClassName);
                }
                else
                {
                    negativeWarningHelpBox.AddToClassList(k_InspectorShownWarningMessageClassName);
                    negativeWarningHelpBox.RemoveFromClassList(k_InspectorHiddenWarningMessageClassName);
                }
                return;
            }

            if (negativeWarningHelpBox == null)
                return;
            negativeWarningHelpBox.AddToClassList(k_InspectorHiddenWarningMessageClassName);
            negativeWarningHelpBox.RemoveFromClassList(k_InspectorShownWarningMessageClassName);
        }
    }
}
