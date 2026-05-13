// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [CustomPropertyDrawer(typeof(MultilineTextFieldAttribute))]
    class MultilineTextFieldPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (MultilineTextFieldAttribute)attribute;

            return new TextField
            {
                label = attr.displayName ?? property.localizedDisplayName,
                multiline = true,
                bindingPath = property.propertyPath
            }.WithClassList(TextField.alignedFieldUssClassName);
        }
    }
}
