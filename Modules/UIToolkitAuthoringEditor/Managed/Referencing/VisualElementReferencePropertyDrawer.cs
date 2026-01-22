// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[CustomPropertyDrawer(typeof(VisualElementReference), true)]
class VisualElementReferencePropertyDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var elementType = fieldInfo.FieldType.IsGenericType == true ? fieldInfo.FieldType.GetGenericArguments()[0] : typeof(VisualElement);
        return new VisualElementReferenceField(property.displayName)
        {
            elementType = elementType,
            bindingPath = property.propertyPath
        };
    }
}
