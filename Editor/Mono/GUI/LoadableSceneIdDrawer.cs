// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.Loading;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomPropertyDrawer(typeof(LoadableSceneId))]
    internal sealed class LoadableSceneIdDrawer : PropertyDrawer
    {
        // Override CreatePropertyGUI and OnGUI to support both UI tech.
        // If only CreatePropertyGUI is overriden the property drawer wouldn't work in an IMGUI context (CBD-841).
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new LoadableSceneIdField(preferredLabel);
            field.BindProperty(property);
            PropertyField.ConfigureFieldStyles<LoadableSceneIdField, LoadableSceneId>(field);
            BindingsStyleHelpers.RegisterRightClickMenu(field, property);
            return field;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawLoadableSceneIdField(position, property, label);
        }

        /// <summary>
        /// Draws an IMGUI field for LoadableSceneId. Uses the same loadable-style object field as DrawLoadableReferenceField.
        /// </summary>
        /// <param name="position">Rect to draw the field in.</param>
        /// <param name="property">Serialized property of type LoadableSceneId.</param>
        /// <param name="label">Label for the field.</param>
        internal void DrawLoadableSceneIdField(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var value = LoadableSceneIdEditorUtility.LoadableSceneIdToScene(property.loadableSceneIdValue);
            var objectType = typeof(SceneAsset);

            var fieldRect = EditorGUI.PrefixLabel(position, label);
            int id = GUIUtility.GetControlID(FocusType.Keyboard, fieldRect);

            EditorGUI.BeginChangeCheck();
            var newObj = EditorGUI.DoLoadableObjectField(fieldRect, fieldRect, id, value, null, objectType, property, EditorGUI.ValidateObjectFieldAssignment, false);
            if (EditorGUI.EndChangeCheck())
                OnLoadableSceneIdValueChanged(newObj, property);

            EditorGUI.EndProperty();
        }

        internal static void OnLoadableSceneIdValueChanged(Object value, SerializedProperty property)
        {
            if (value == null)
                property.loadableSceneIdValue = default;
            else if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out var guidStr, out _))
                property.loadableSceneIdValue = LoadableSceneIdEditorUtility.CreateLoadableSceneId(new GUID(guidStr));
            else
                Debug.LogWarning("Unable to assign LoadableSceneId.", value);
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
