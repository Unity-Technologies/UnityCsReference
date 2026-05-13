// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [CustomPropertyDrawer(typeof(TagFieldValueDecoratorAttribute))]
    class TagFieldValuePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var tagField = new TagField("Value");
            tagField.AddToClassList(TextField.alignedFieldUssClassName);
            tagField.bindingPath = property.propertyPath;
            return tagField;
        }
    }
}
