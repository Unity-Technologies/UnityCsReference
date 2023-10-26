// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class UxmlSerializedDataDescription
    {
        private readonly List<UxmlSerializedAttributeDescription> m_SerializedAttributes = new();

        private readonly Dictionary<string, int> m_UxmlNameToIndex = new();
        private readonly Dictionary<string, int> m_PropertyNameToIndex = new();
        private readonly HashSet<string> m_UxmlObjectFields = new();
        private Type m_SerializedDataType;
        private UxmlObjectAttribute m_UxmlObjectAttribute;
        private string m_UxmlName;
        private string m_UxmlFullName;

        public Type serializedDataType => m_SerializedDataType;
        public bool isUxmlObject => m_UxmlObjectAttribute != null;

        public string uxmlName
        {
            get
            {
                if (m_UxmlName == null)
                {
                    // TemplateContainer must use the class name
                    if (serializedDataType.DeclaringType == typeof(TemplateContainer))
                        m_UxmlName = nameof(TemplateContainer);
                    else
                    {
                        var elementAttribute = serializedDataType.DeclaringType.GetCustomAttribute<UxmlElementAttribute>();
                        if (elementAttribute != null && !string.IsNullOrEmpty(elementAttribute.name))
                            m_UxmlName = elementAttribute.name;
                        else
                            m_UxmlName = serializedDataType.DeclaringType.Name;
                    }
                }

                return m_UxmlName;
            }
        }

        public string uxmlFullName
        {
            get
            {
                if (m_UxmlFullName == null)
                {
                    if (string.IsNullOrEmpty(serializedDataType.DeclaringType.Namespace))
                        m_UxmlFullName = uxmlName;
                    else
                        m_UxmlFullName = $"{serializedDataType.DeclaringType.Namespace}.{uxmlName}";
                }

                return m_UxmlFullName;
            }
        }

        public UxmlSerializedData CreateSerializedData() => (UxmlSerializedData)Activator.CreateInstance(m_SerializedDataType);

        public UxmlSerializedData CreateDefaultSerializedData()
        {
            var data = CreateSerializedData();
            foreach (var attribute in serializedAttributes)
            {
                try
                {
                    attribute.SetSerializedValue(data, attribute.defaultValueClone);
                }
                catch(Exception e)
                {
                    Debug.LogException(new Exception($"Could not set value for {attribute.serializedField.Name} with {attribute.defaultValueClone}", e));
                }
            }
            return data;
        }

        public IReadOnlyList<UxmlSerializedAttributeDescription> serializedAttributes => m_SerializedAttributes;

        public UxmlSerializedAttributeDescription FindAttributeWithUxmlName(string name)
        {
            if (m_UxmlNameToIndex.TryGetValue(name, out var index))
                return m_SerializedAttributes[index];
            return null;
        }

        public UxmlSerializedAttributeDescription FindAttributeWithPropertyName(string name)
        {
            if (m_PropertyNameToIndex.TryGetValue(name, out var index))
                return m_SerializedAttributes[index];
            return null;
        }

        public static UxmlSerializedDataDescription Create(Type dataType)
        {
            var uxmlDataDesc = new UxmlSerializedDataDescription
            {
                m_SerializedDataType = dataType,
                m_UxmlObjectAttribute = dataType.DeclaringType.GetCustomAttribute<UxmlObjectAttribute>()
            };

            object defaultObject = null;
            try
            {
                defaultObject = uxmlDataDesc.CreateSerializedData().CreateInstance();
            }
            catch (Exception e)
            {
                Debug.LogException (new Exception($"Failed to create an instance of {dataType}", e));
            }

            uxmlDataDesc.GatherAttributesForType(dataType, defaultObject);
            return uxmlDataDesc;
        }

        /// <summary>
        /// Copies the attribute values from the VisualElement or UxmlObject to the UxmlSerializedData.
        /// </summary>
        /// <param name="obj">The instance of the VisualElement or UxmlObject to copy the value from.</param>
        /// <param name="uxmlSerializedData">The instance of the UxmlSerializedData to copy the value to.</param>
        public void SyncSerializedData(object obj, object uxmlSerializedData)
        {
            foreach (var attribute in m_SerializedAttributes)
            {
                attribute.SyncSerializedData(obj, uxmlSerializedData);
            }
        }

        private void GatherAttributesForType(Type t, object defaultObject)
        {
            if (t == typeof(UxmlSerializedData))
                return;

            var desc = UxmlDescriptionRegistry.GetDescription(t);
            for (var i = 0; i < desc.attributeDescriptions.Count; ++i)
            {
                var attDescription = desc.attributeDescriptions[i];

                var fieldInfo = attDescription.serializedField;
                UxmlSerializedAttributeDescription uxmlAttributeDescription;
                var elementType = defaultObject != null ? defaultObject.GetType() : fieldInfo.DeclaringType.DeclaringType;

                if (fieldInfo.GetCustomAttribute<UxmlObjectReferenceAttribute>() is { } objectReferenceAttribute)
                {
                    uxmlAttributeDescription = new UxmlSerializedUxmlObjectAttributeDescription { rootName = objectReferenceAttribute.name };
                }
                else
                {
                    if (!IsValidAttributeType(fieldInfo.FieldType))
                    {
                        Debug.LogError($"[UxmlElement] '{elementType.Name}' has a [UxmlAttribute] '{attDescription.uxmlName}' of an unknown type '{fieldInfo.FieldType.Name}'.\n" +
                                       $"To fix this error define a custom {nameof(UxmlAttributeConverter)}<{fieldInfo.FieldType.Name}>.");
                        continue;
                    }

                    uxmlAttributeDescription = new UxmlSerializedAttributeDescription();
                }

                uxmlAttributeDescription.name = attDescription.uxmlName;
                uxmlAttributeDescription.type = attDescription.fieldType;
                uxmlAttributeDescription.serializedField = attDescription.serializedField;
                uxmlAttributeDescription.elementType = elementType;
                uxmlAttributeDescription.obsoleteNames = attDescription.obsoleteNames;

                if (TryGetDefaultValue(fieldInfo, defaultObject, out var defaultValue))
                    uxmlAttributeDescription.defaultValue = defaultValue;

                // When a UxmlObjectAttribute does not contain a name then we treat it as a legacy field,
                // one that does not have an element for the field name, e.g MultiColumnListView.
                var referenceField = fieldInfo.GetCustomAttribute<UxmlObjectReferenceAttribute>();
                if (referenceField != null && !string.IsNullOrEmpty(referenceField.name))
                {
                    m_UxmlObjectFields.Add(referenceField.name);
                }

                m_SerializedAttributes.Add(uxmlAttributeDescription);
                m_UxmlNameToIndex.Add(attDescription.uxmlName, i);
                m_PropertyNameToIndex.Add(attDescription.cSharpName, i);
            }
        }

        /// <summary>
        /// Whether or not this type contain a field with a <see cref="UxmlObjectAttribute"/> attribute with a matching UXML name.
        /// </summary>
        /// <param name="name">The UXML element name to check for.</param>
        /// <returns></returns>
        public bool IsUxmlObjectField(string name) => m_UxmlObjectFields.Contains(name);

        public static bool TryGetDefaultValue(FieldInfo fieldInfo, object defaultObject, out object value)
        {
            value = null;
            if (defaultObject == null)
                return false;

            var fieldName = fieldInfo.Name;

            var prop = defaultObject.GetType().GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
                value = prop.GetValue(defaultObject);

            if (value == null)
            {
                var field = defaultObject.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                    value = field.GetValue(defaultObject);
            }

            if (value != null && fieldInfo.GetCustomAttribute<UxmlObjectReferenceAttribute>() != null)
            {
                // UxmlObject fields are not serialized directly, they use their corresponding UxmlSerializedData
                value = UxmlSerializer.SerializeObject(value);
            }

            return value != null;
        }

        private static bool IsValidAttributeType(Type t)
        {
            return UxmlAttributeConverter.TryGetConverter(t, out _) || typeof(UnityEngine.Object).IsAssignableFrom(t);
        }
    }
}
