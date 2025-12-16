// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomPropertyDrawer(typeof(LoadableReference))]
    internal sealed class LoadableReferenceDrawer : PropertyDrawer
    {
        struct LoadableReferenceProperties
        {
            public SerializedProperty guidProperty;
            public SerializedProperty typeProperty;
            public SerializedProperty fileIDProperty;
            public SerializedProperty objectIdHashProperty;
        }

        private static LoadableReference ExtractLoadableRef(LoadableReferenceProperties props)
        {
            GUID guid = props.guidProperty.boxedValue != null ? (GUID)props.guidProperty.boxedValue : new GUID();
            FileIdentifierType type = (FileIdentifierType)props.typeProperty.intValue;
            long fileID = props.fileIDProperty.longValue;
            return LoadableReferenceEditorUtility.CreateLoadableReference(guid, type, fileID);
        }

        // Override CreatePropertyGUI and OnGUI to support both UI tech.
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            ExtractLoadableReferenceProperties(property, out LoadableReferenceProperties props);

            LoadableReference loadableRef = ExtractLoadableRef(props);
            Object currentObject = LoadableReferenceEditorUtility.LoadableReferenceToObject(loadableRef);

            var container = new VisualElement();

            var objectField = new ObjectField(property.displayName)
            {
                allowSceneObjects = false,
                objectType = typeof(Object),
                value = currentObject
            };
            objectField.SetEnabled(true);
            objectField.AddToClassList(ObjectField.alignedFieldUssClassName);
            objectField.labelElement.AddToClassList(PropertyField.labelUssClassName);

            container.Add(objectField);

            objectField.TrackPropertyValue(props.guidProperty, newProperty =>
            {
                UpdateObjectFieldFromLoadableReferenceProperties(objectField, props);
            });

            objectField.TrackPropertyValue(props.typeProperty, newProperty =>
            {
                UpdateObjectFieldFromLoadableReferenceProperties(objectField, props);
            });

            objectField.TrackPropertyValue(props.fileIDProperty, newProperty =>
            {
                UpdateObjectFieldFromLoadableReferenceProperties(objectField, props);
            });

            objectField.RegisterCallback((ChangeEvent<Object> e) =>
            {
                OnValueChanged(e.newValue, props);
            });

            return container;
        }

        static void ExtractLoadableReferenceProperties(SerializedProperty property, out LoadableReferenceProperties props)
        {
            var guidPath = $"{property.propertyPath}.m_GUID";
            var typePath = $"{property.propertyPath}.m_Type";
            var fileIDPath = $"{property.propertyPath}.m_FileID";
            var objectIdHashPath = $"{property.propertyPath}.m_ObjectIdHash";

            props.guidProperty = property.serializedObject.FindProperty(guidPath);
            props.typeProperty = property.serializedObject.FindProperty(typePath);
            props.fileIDProperty = property.serializedObject.FindProperty(fileIDPath);
            props.objectIdHashProperty = property.serializedObject.FindProperty(objectIdHashPath);

            if (props.guidProperty == null || props.typeProperty == null || props.fileIDProperty == null)
                throw new Exception("LoadableReference properties not found");
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ExtractLoadableReferenceProperties(property, out LoadableReferenceProperties props);
            LoadableReference loadableRef = ExtractLoadableRef(props);
            Object currentObject = LoadableReferenceEditorUtility.LoadableReferenceToObject(loadableRef);

            EditorGUI.BeginChangeCheck();
            label = EditorGUI.BeginProperty(position, label, property);
            
            var newObject = EditorGUI.ObjectField(position, label, currentObject, typeof(Object), false);

            if (EditorGUI.EndChangeCheck())
                OnValueChanged(newObject, props);
        }

        private static void UpdateObjectFieldFromLoadableReferenceProperties(ObjectField objectField, LoadableReferenceProperties props)
        {
            LoadableReference loadableRef = ExtractLoadableRef(props);
            objectField.value = LoadableReferenceEditorUtility.LoadableReferenceToObject(loadableRef);
        }

        private static void OnValueChanged(Object newObject, LoadableReferenceProperties props)
        {
            LoadableReference loadableRef = LoadableReferenceEditorUtility.ObjectToLoadableReference(newObject);
            if (newObject != null && !loadableRef.isValid)
            {
                Debug.LogWarning("Cannot create valid LoadableReference from object. Object may not be persistent.");
                return;
            }

            props.guidProperty.boxedValue = loadableRef.m_GUID;
            props.typeProperty.intValue = (int)loadableRef.m_FileIdentifierType;
            props.fileIDProperty.longValue = loadableRef.m_LocalIdentifierInFile;
            props.guidProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}
