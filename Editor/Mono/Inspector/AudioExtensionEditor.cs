// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class AudioExtensionEditor : ScriptableObject
    {
        private bool foundAllExtensionProperties = false;

        public struct ExtensionPropertyInfo
        {
            public ExtensionPropertyInfo(string nameIn, float defaultValueIn)
            {
                propertyName = new PropertyName(nameIn);
                defaultValue = defaultValueIn;
                serializedProperty = null;
            }

            public PropertyName         propertyName;
            public float                defaultValue;
            public SerializedProperty   serializedProperty;
        }
#pragma warning disable 649
        protected ExtensionPropertyInfo[] m_ExtensionProperties;

        public virtual void InitExtensionPropertyInfo() {}
        protected virtual int GetNumSerializedExtensionProperties(Object obj) { return 0; }

        public void OnEnable()
        {
            InitExtensionPropertyInfo();
        }

        public int GetNumExtensionProperties()
        {
            return m_ExtensionProperties.Length;
        }

        public PropertyName GetExtensionPropertyName(int index)
        {
            return m_ExtensionProperties[index].propertyName;
        }

        public float GetExtensionPropertyDefaultValue(int index)
        {
            return m_ExtensionProperties[index].defaultValue;
        }

        public bool FindAudioExtensionProperties(SerializedObject serializedObject)
        {
            SerializedProperty extensionPropertyValues = null;

            if (serializedObject != null)
                extensionPropertyValues = serializedObject.FindProperty("m_ExtensionPropertyValues");

            if (extensionPropertyValues == null)
            {
                foundAllExtensionProperties = false;
                return false;
            }

            int minNumSerializedExtensionProperties = extensionPropertyValues.arraySize;
            if (extensionPropertyValues.hasMultipleDifferentValues)
                minNumSerializedExtensionProperties = GetMinNumSerializedExtensionProperties(serializedObject);

            if ((extensionPropertyValues == null) || (minNumSerializedExtensionProperties == 0))
            {
                foundAllExtensionProperties = false;
                return false;
            }

            if (!foundAllExtensionProperties && (serializedObject != null))
            {
                int numPropertiesFound = 0;
                for (int sourceIndex = 0; sourceIndex < minNumSerializedExtensionProperties; sourceIndex++)
                {
                    SerializedProperty extensionPropertyValue = extensionPropertyValues.GetArrayElementAtIndex(sourceIndex);

                    if (extensionPropertyValue == null)
                        continue;

                    SerializedProperty propertyName = extensionPropertyValue.FindPropertyRelative("propertyName");
                    for (int extensionIndex = 0; extensionIndex < m_ExtensionProperties.Length; extensionIndex++)
                    {
                        if ((m_ExtensionProperties[extensionIndex].propertyName == propertyName.stringValue) && !propertyName.hasMultipleDifferentValues)
                        {
                            m_ExtensionProperties[extensionIndex].serializedProperty = extensionPropertyValue.FindPropertyRelative("propertyValue");
                            numPropertiesFound++;
                        }
                    }
                }

                foundAllExtensionProperties = (numPropertiesFound == m_ExtensionProperties.Length) ? true : false;
            }

            return foundAllExtensionProperties;
        }

        protected static void PropertyFieldAsBool(SerializedProperty property, GUIContent title)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            title = EditorGUI.BeginProperty(rect, title, property);
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUI.Toggle(rect, title, property.floatValue > 0.0f ? true : false);
            if (EditorGUI.EndChangeCheck())
            {
                property.floatValue = newValue ? 1.0f : 0.0f;
            }
            EditorGUI.EndProperty();
        }

        private int GetMinNumSerializedExtensionProperties(SerializedObject serializedObject)
        {
            Object[] targets = serializedObject.targetObjects;
            int minNumSerializedExtensionProperties = (targets.Length > 0) ? int.MaxValue : 0;

            for (int i = 0; i < targets.Length; i++)
                minNumSerializedExtensionProperties = Math.Min(minNumSerializedExtensionProperties, GetNumSerializedExtensionProperties(targets[i]));

            return minNumSerializedExtensionProperties;
        }
    }
}
