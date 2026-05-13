// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [CustomPropertyDrawer(typeof(LayerDecoratorAttribute))]
    class LayerPropertyDrawer : PropertyDrawer
    {
        static readonly string s_LocalizedLabel = L10n.Tr("Value");

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new LayerField(s_LocalizedLabel).WithClassList(LayerField.alignedFieldUssClassName);

            field.bindingPath = property.propertyPath;
            return field;
        }
    }
}
