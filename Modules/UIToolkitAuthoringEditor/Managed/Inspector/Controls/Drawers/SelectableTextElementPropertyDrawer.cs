// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [CustomPropertyDrawer(typeof(SelectableTextElementAttribute))]
    class SelectableTextElementPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Capture properties in local scope to avoid drawer instance sharing issues
            var selectableProperty = property;
            var rootPath = property.propertyPath.Substring(0, property.propertyPath.LastIndexOf('.'));
            var rootProperty = property.serializedObject.FindProperty(rootPath);
            var focusableProperty = rootProperty.FindPropertyRelative("focusableUXML");

            var selectableField = new Toggle("Selectable")
            {
                value = property.boolValue,
                bindingPath = property.propertyPath
            }.WithClassList(Toggle.alignedFieldUssClassName);

            // Capture selectableField in closure to ensure correct reference
            selectableField.TrackPropertyValue(focusableProperty, (serializedProperty) =>
            {
                var editingField = SerializedObjectBindingBase.editingField;

                // Only override the selectable property if the focusable property was edited by a field
                // in the same panel.
                if (editingField == null || editingField.panel != selectableField.panel)
                    return;

                // If focusable is set to false, we need to reset the isSelectable value as well as it doesn't work
                // without focusable on. It strictly follows the logic present in the isSelectable setter.
                if (serializedProperty.boolValue == false && selectableField.value)
                {
                    selectableField.value = false;
                    selectableProperty.boolValue = false;
                    selectableProperty.serializedObject.ApplyModifiedProperties();
                }
            });

            selectableField.TrackPropertyValue(selectableProperty, (serializedProperty) =>
            {
                var editingField = SerializedObjectBindingBase.editingField;

                // Only override the selectable property if the selectable property was edited by this field.
                if (editingField == null || editingField != selectableField)
                    return;

                selectableProperty.boolValue = serializedProperty.boolValue;
                // Only force focusable to true when selectable becomes true (to satisfy dependency).
                // Don't force it to false when selectable becomes false (element can be focusable without being selectable).
                if (serializedProperty.boolValue)
                    focusableProperty.boolValue = true;
                selectableProperty.serializedObject.ApplyModifiedProperties();
            });

            return selectableField;
        }
    }
}
