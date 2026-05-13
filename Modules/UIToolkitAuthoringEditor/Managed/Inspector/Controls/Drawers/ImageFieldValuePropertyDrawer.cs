// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [CustomPropertyDrawer(typeof(ImageFieldValueDecoratorAttribute))]
    class ImageFieldValuePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var imageAttribute = (ImageFieldValueDecoratorAttribute)attribute;

            var imageField = new ImageField(imageAttribute.displayName).WithClassList(ImageField.alignedFieldUssClassName);
            imageField.bindingPath = property.propertyPath;
            return imageField;
        }
    }
}
