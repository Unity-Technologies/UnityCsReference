// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor
{
    [CustomPropertyDrawer(typeof(Loadable<>))]
    internal sealed class LoadableDrawer : PropertyDrawer
    {
        // Override CreatePropertyGUI and OnGUI to support both UI tech.
        // If only CreatePropertyGUI is overriden the property drawer wouldn't work in an IMGUI context (CBD-841).
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var loadableRefProp = property.FindPropertyRelative("m_LoadableRef");
            var loadableType = FindLoadableType(property);
            var loadableReferenceField = new LoadableReferenceField(preferredLabel, loadableType);
            loadableReferenceField.BindProperty(loadableRefProp);

            PropertyField.ConfigureFieldStyles<LoadableReferenceField, LoadableReference>(loadableReferenceField);

            return loadableReferenceField;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var loadableRefProp = property.FindPropertyRelative("m_LoadableRef");
            var loadableType = FindLoadableType(property);
            LoadableReferenceEditorUtility.DrawLoadableReferenceField(position, loadableRefProp, label, loadableType);
        }

        private static Type FindLoadableType(SerializedProperty property)
        {
            // Try to find the generic type of the loadable field by walking up the property path
            FieldInfo field = null;
            var t = property.serializedObject.targetObject.GetType();
            var path = property.propertyPath.Split(".");

            // Find matching field
            var i = 0;
            while (i < path.Length)
            {
                field = t.GetField(path[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                t = field.FieldType;

                if (path.Length - i >= 2)
                {
                    if (path[i + 1] == "Array" && path[i + 2].StartsWith("data["))
                    {
                        t = t.IsArray ? t.GetElementType() : t.GetGenericArguments()[0];
                        i += 3;
                        continue;
                    }
                }

                ++i;
            }

            t = field.FieldType;

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Loadable<>))
                return t.GetGenericArguments()[0];

            if (t.IsArray && t.GetElementType().GetGenericTypeDefinition() == typeof(Loadable<>))
                return t.GetElementType().GetGenericArguments()[0];

            // Assume this is a list
            var genericType = t.GetGenericArguments()[0];
            if (genericType.IsGenericType && genericType.GetGenericTypeDefinition() == typeof(Loadable<>))
                return genericType.GetGenericArguments()[0];

            return typeof(UnityEngine.Object);
        }
    }
}
