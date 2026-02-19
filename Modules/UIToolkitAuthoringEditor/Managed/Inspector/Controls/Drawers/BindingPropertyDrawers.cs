// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Property drawer for DataSourceDrawerAttribute
    /// </summary>
    [CustomPropertyDrawer(typeof(DataSourceDrawerAttribute))]
    class DataSourcePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new AnyObjectField()
            {
                bindingPath = property.propertyPath,
                objectType = typeof(ScriptableObject),
                label = " ",
            };
            field.AddToClassList(AnyObjectField.alignedFieldUssClassName);
            return field;
        }
    }

    /// <summary>
    /// Property drawer for BindingPathDrawerAttribute
    /// </summary>
    [CustomPropertyDrawer(typeof(BindingPathDrawerAttribute))]
    class BindingPathPropertyDrawer : PropertyDrawer
    {
        const string k_EditorBindingPathLabel = "Editor Binding Path";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new TextField(k_EditorBindingPathLabel)
            {
                bindingPath = property.propertyPath, isDelayed = true
            };
            field.AddToClassList(AnyObjectField.alignedFieldUssClassName);
            return field;
        }
    }
}
