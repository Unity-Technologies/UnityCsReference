// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [CustomPropertyDrawer(typeof(UxmlTypeReferenceAttribute))]
    class UxmlTypeReferencePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var desiredType = ((UxmlTypeReferenceAttribute)attribute).baseType ?? typeof(object); var uxmlAttribute = fieldInfo.GetCustomAttribute<UxmlAttributeAttribute>();
            var label = uxmlAttribute != null ? StyleSheetUtility.ConvertDashToHuman(uxmlAttribute.name) : property.localizedDisplayName;
            var field = new UxmlTypeReferenceField(label, desiredType) { bindingPath = property.propertyPath };

            field.AddToClassList(UxmlTypeReferenceField.alignedFieldUssClassName);
            return field;
        }
    }
}
