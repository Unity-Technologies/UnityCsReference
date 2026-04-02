// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [CustomPropertyDrawer(typeof(BindingModeDrawerAttribute))]
    class BindingModePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var enumField = new EnumField
            {
                bindingPath = property.propertyPath, label = property.localizedDisplayName
            };
            enumField.AddToClassList(EnumField.alignedFieldUssClassName);
            return enumField;
        }
    }
}
