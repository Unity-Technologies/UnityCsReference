// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomPropertyDrawer(typeof(Loadable<>))]
    internal sealed class LoadableDrawer : PropertyDrawer
    {
        static readonly string kAssetGUIDPropName = "m_LoadableRef.m_GUID";
        static readonly string kIdentifierTypePropName = "m_LoadableRef.m_Type";
        static readonly string kLfidPropName = "m_LoadableRef.m_FileID";

        // Override CreatePropertyGUI and OnGUI to support both UI tech.
        // If only CreatePropertyGUI is overriden the property drawer wouldn't work in an IMGUI context (CBD-841).
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var loadableRef = ReadReference(property, out var guidProperty, out var fileIdentifierProperty, out var localIdentifierInFileProperty);
            var loadableType = FindLoadableType(property);

            var objectField = new ObjectField(property.displayName)
            {
                allowSceneObjects = false,
                objectType = loadableType
            };
            objectField.AddToClassList(ObjectField.alignedFieldUssClassName);
            objectField.labelElement.AddToClassList(PropertyField.labelUssClassName);

            if (!loadableRef.m_GUID.Empty())
                objectField.value = LoadableReferenceEditorUtility.LoadableReferenceToObject(loadableRef);

            objectField.RegisterValueChangedCallback(evt => OnValueChanged(evt.newValue, property, guidProperty, fileIdentifierProperty, localIdentifierInFileProperty));
            return objectField;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var loadableRef = ReadReference(property, out var guidProperty, out var fileIdentifierProperty, out var localIdentifierInFileProperty);
            var loadableType = FindLoadableType(property);

            Object value = null;
            if (!loadableRef.m_GUID.Empty())
                value = LoadableReferenceEditorUtility.LoadableReferenceToObject(loadableRef);

            EditorGUI.BeginChangeCheck();
            label = EditorGUI.BeginProperty(position, label, property);
            value = EditorGUI.ObjectField(position, label, value, loadableType, false);

            if (EditorGUI.EndChangeCheck())
                OnValueChanged(value, property, guidProperty, fileIdentifierProperty, localIdentifierInFileProperty);
        }

        private static LoadableReference ReadReference(SerializedProperty property, out SerializedProperty guidProp, out SerializedProperty fileIdentifierProp, out SerializedProperty lfidProp)
        {
            guidProp = property.FindPropertyRelative(kAssetGUIDPropName);
            fileIdentifierProp = property.FindPropertyRelative(kIdentifierTypePropName);
            lfidProp = property.FindPropertyRelative(kLfidPropName);

            if (guidProp == null || fileIdentifierProp == null || lfidProp == null)
            {
                Debug.LogError($"{nameof(LoadableDrawer)} cannot find required properties. GUID: {guidProp != null}, Type: {fileIdentifierProp != null}, FileID: {lfidProp != null}");
                return LoadableReferenceEditorUtility.CreateLoadableReference(new GUID(), FileIdentifierType.NonAsset, 0);
            }
            return LoadableReferenceEditorUtility.CreateLoadableReference(guidProp.guidValue, (FileIdentifierType)fileIdentifierProp.intValue, lfidProp.longValue);
        }

        private static void OnValueChanged(Object value, SerializedProperty property, SerializedProperty guidProp, SerializedProperty fileIdentifierProp, SerializedProperty lfidProp)
        {
            if (value == null)
            {
                guidProp.guidValue = default;
                fileIdentifierProp.intValue = default;
                lfidProp.longValue = default;
            }
            else
            {
                LoadableReference lRef = LoadableReferenceEditorUtility.ObjectToLoadableReference(value);
                if (lRef.isValid)
                {
                    guidProp.guidValue = lRef.m_GUID;
                    fileIdentifierProp.intValue = (int)lRef.m_FileIdentifierType;
                    lfidProp.longValue = lRef.m_LocalIdentifierInFile;
                }
                else
                {
                    Debug.LogWarning("Unable to assign Loadable. Only objects from Assets can be assigned.", value);
                    return;
                }
            }

            property.serializedObject.ApplyModifiedProperties();
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

            return typeof(Object);
        }
    }
}
