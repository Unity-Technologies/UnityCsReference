// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [CustomPropertyDrawer(typeof(ToggleButtonGroup.UxmlSerializedData))]
    class ToggleButtonGroupPropertyDrawer : UxmlSerializedDataPropertyDrawer
    {
        const string k_ValuePropertyName = "valueUXML";

        protected override void CreateChildPropertyGUI(VisualElement container, SerializedProperty property, SerializedProperty childProperty)
        {
            if (childProperty.name != k_ValuePropertyName)
            {
                base.CreateChildPropertyGUI(container, property, childProperty);
                return;
            }

            var field = new UxmlAttributeField(childProperty);
            container.Add(field);

            var valueProperty = childProperty.Copy();
            var isMultipleSelection = property.FindPropertyRelative(nameof(ToggleButtonGroup.isMultipleSelection));
            var allowEmptySelection = property.FindPropertyRelative(nameof(ToggleButtonGroup.allowEmptySelection));

            if (isMultipleSelection == null || allowEmptySelection == null)
                return;

            field.RegisterCallback<SerializedPropertyBindEvent>(_ => ApplySelectionRules());
            field.TrackPropertyValue(isMultipleSelection, OnSelectionRulesChanged);
            field.TrackPropertyValue(allowEmptySelection, OnSelectionRulesChanged);

            ToggleButtonGroup ApplySelectionRules()
            {
                var group = field.Q<ToggleButtonGroup>();
                if (group == null || isMultipleSelection is not { isValid: true } || allowEmptySelection is not { isValid: true })
                    return null;

                group.isMultipleSelection = isMultipleSelection.boolValue;
                group.allowEmptySelection = allowEmptySelection.boolValue;
                return group;
            }

            void OnSelectionRulesChanged(SerializedProperty _)
            {
                var group = ApplySelectionRules();
                if (group == null || valueProperty is not { isValid: true } || field.Context is null or { isReadOnly: true })
                    return;

                // Tightening the rules drops selected options through SetValueWithoutNotify, which reports no change.
                if (group.value.Equals((ToggleButtonGroupState)valueProperty.boxedValue))
                    return;

                valueProperty.boxedValue = group.value;
                valueProperty.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
