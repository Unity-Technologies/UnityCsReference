// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Entities;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // The default object field filters by the field type, which is the UnityObjectRef<T> wrapper rather than T; the object reference is its Id child
    [CustomPropertyDrawer(typeof(UnityObjectRef<>))]
    internal sealed class UnityObjectRefDrawer : PropertyDrawer
    {
        // Data components have no target object, so the owning GameObject decides
        internal static bool AllowSceneObjects(SerializedObject serializedObject)
        {
            Object owner;
            if (serializedObject.isTargetDataComponent)
                owner = EditorUtility.EntityIdToObject(DataComponentEntityId.GetOwner(serializedObject.targetEntityId));
            else
                owner = serializedObject.targetObject;

            return owner != null && !EditorUtility.IsPersistent(owner);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ScriptAttributeUtility.GetFieldInfoFromProperty(property, out var fieldType);
            var idProperty = property.FindPropertyRelative("Id");

            label = EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            var value = EditorGUI.ObjectField(position, label, idProperty.objectReferenceValue, fieldType.GetGenericArguments()[0], AllowSceneObjects(property.serializedObject));

            if (EditorGUI.EndChangeCheck())
                idProperty.objectReferenceValue = value;

            EditorGUI.EndProperty();
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            ScriptAttributeUtility.GetFieldInfoFromProperty(property, out var fieldType);
            var idProperty = property.FindPropertyRelative("Id");

            var objectField = new ObjectField(preferredLabel);
            objectField.objectType = fieldType.GetGenericArguments()[0];
            objectField.allowSceneObjects = AllowSceneObjects(property.serializedObject);
            objectField.value = idProperty.objectReferenceValue;
            objectField.bindingPath = idProperty.propertyPath;

            PropertyField.ConfigureFieldStyles<ObjectField, Object>(objectField);

            return objectField;
        }
    }
}
