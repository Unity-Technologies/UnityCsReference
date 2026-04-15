// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.Loading;
using Object = UnityEngine.Object;
using UnityEngine.Bindings;

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

            var field = new LoadableSceneIdField(preferredLabel);
            PropertyField.ConfigureFieldStyles<LoadableSceneIdField, Object>(field);

            var guid = guidProperty.guidValue;
            if (!guid.Empty())
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                field.SetValueWithoutNotify(AssetDatabase.LoadAssetAtPath(path, typeof(SceneAsset)));
            }

            field.RegisterValueChangedCallback(evt => OnLoadableSceneIdValueChanged(evt.newValue, property, guidProperty));

            return field;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var guidProperty = property.FindPropertyRelative(kSceneGUIDPropName);
            DrawLoadableSceneIdField(position, property, guidProperty, label);
        }

        /// <summary>
        /// Draws an IMGUI field for LoadableSceneId. Uses the same loadable-style object field as DrawLoadableReferenceField.
        /// </summary>
        /// <param name="position">Rect to draw the field in.</param>
        /// <param name="parentProperty">Parent serialized property (e.g. LoadableSceneId) used for BeginProperty and context menu.</param>
        /// <param name="guidProperty">SerializedProperty for the GUID field (e.g. m_SceneGUID).</param>
        /// <param name="label">Label for the field.</param>
        internal void DrawLoadableSceneIdField(Rect position, SerializedProperty parentProperty, SerializedProperty guidProperty, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, parentProperty);
            Object value = null;
            var guid = guidProperty.guidValue;
            var objectType = typeof(SceneAsset);
            if (!guid.Empty())
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                value = AssetDatabase.LoadAssetAtPath(path, objectType);
            }

            var fieldRect = EditorGUI.PrefixLabel(position, label);
            int id = GUIUtility.GetControlID(FocusType.Keyboard, fieldRect);

            EditorGUI.BeginChangeCheck();
            var newObj = EditorGUI.DoLoadableObjectField(fieldRect, fieldRect, id, value, null, objectType, parentProperty, EditorGUI.ValidateObjectFieldAssignment, false);
            if (EditorGUI.EndChangeCheck())
                OnLoadableSceneIdValueChanged(newObj, parentProperty, guidProperty);

            EditorGUI.EndProperty();
        }

        internal static void OnLoadableSceneIdValueChanged(Object value, SerializedProperty property, SerializedProperty guidProp)
        {
            if (value == null)
                guidProp.guidValue = default;
            else if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out var guid, out _))
                guidProp.guidValue = new GUID(guid);
            else
                Debug.LogWarning("Unable to assign LoadableSceneId.", value);
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
