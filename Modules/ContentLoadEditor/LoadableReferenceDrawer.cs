// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor
{
    [CustomPropertyDrawer(typeof(LoadableReference))]
    internal sealed class LoadableReferenceDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var loadableReferenceField = new LoadableReferenceField(preferredLabel);
            loadableReferenceField.BindProperty(property);
            PropertyField.ConfigureFieldStyles<LoadableReferenceField, LoadableReference>(loadableReferenceField);
            return loadableReferenceField;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LoadableReferenceEditorUtility.DrawLoadableReferenceField(position, property, label, typeof(UnityEngine.Object));
        }
    }
}
