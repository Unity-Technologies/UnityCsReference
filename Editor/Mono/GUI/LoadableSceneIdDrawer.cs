// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Loading;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomPropertyDrawer(typeof(LoadableSceneId))]
    internal sealed class LoadableSceneIdDrawer : PropertyDrawer
    {
        static readonly string kSceneGUIDPropName = "m_SceneGUID";

        // Override CreatePropertyGUI and OnGUI to support both UI tech.
        // If only CreatePropertyGUI is overriden the property drawer wouldn't work in an IMGUI context (CBD-841).
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var guidProperty = property.FindPropertyRelative(kSceneGUIDPropName);
            Debug.Assert(guidProperty != null, $"{nameof(LoadableSceneIdDrawer)} cannot find {kSceneGUIDPropName} property.");

            var objectField = new ObjectField(property.displayName)
            {
                allowSceneObjects = false,
                objectType = typeof(SceneAsset)
            };
            objectField.AddToClassList(ObjectField.alignedFieldUssClassName);
            objectField.labelElement.AddToClassList(PropertyField.labelUssClassName);

            var guid = guidProperty.guidValue;
            if (!guid.Empty())
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                objectField.value = AssetDatabase.LoadAssetAtPath(path, typeof(SceneAsset));
            }

            objectField.RegisterValueChangedCallback(evt => OnValueChanged(evt.newValue, property, guidProperty));

            return objectField;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var guidProperty = property.FindPropertyRelative("m_SceneGUID");

            Object value = null;
            var guid = guidProperty.guidValue;
            if (!guid.Empty())
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                value = AssetDatabase.LoadAssetAtPath(path, typeof(SceneAsset));
            }

            EditorGUI.BeginChangeCheck();
            label = EditorGUI.BeginProperty(position, label, property);
            value = EditorGUI.ObjectField(position, label, value, typeof(SceneAsset), false);

            if (EditorGUI.EndChangeCheck())
                OnValueChanged(value, property, guidProperty);
        }

        private static void OnValueChanged(Object value, SerializedProperty property, SerializedProperty guidProp)
        {
            if (value == null)
            {
                guidProp.guidValue = default;
            }
            else if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out var guid, out _))
            {
                guidProp.guidValue = new GUID(guid);
            }
            else
            {
                Debug.LogWarning("Unable to assign LoadableSceneId.", value);
            }

            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
