// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Unity.Loading;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor
{
    [CustomPropertyDrawer(typeof(LoadableObjectId))]
    internal sealed class LoadableObjectIdDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var loadableObjectIdField = new LoadableObjectIdField(preferredLabel);
            loadableObjectIdField.BindProperty(property);
            PropertyField.ConfigureFieldStyles<LoadableObjectIdField, LoadableObjectId>(loadableObjectIdField);
            return loadableObjectIdField;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LoadableObjectIdEditorUtility.DrawLoadableObjectIdField(position, property, label, typeof(UnityEngine.Object));
        }
    }
}
