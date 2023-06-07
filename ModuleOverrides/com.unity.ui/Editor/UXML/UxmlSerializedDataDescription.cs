// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class UxmlSerializedDataDescription
    {
        private List<UxmlSerializedAttributeDescription> m_SerializedAttributes = new();

        private Dictionary<string, int> m_UxmlNameToIndex = new();
        private Dictionary<string, int> m_PropertyNameToIndex = new();
        private HashSet<string> m_UxmlObjectFields = new();
        private Type m_SerializedDataType;
        private UxmlObjectAttribute m_UxmlObjectAttribute;

        public Type serializedDataType => m_SerializedDataType;
        public bool isUxmlObject => m_UxmlObjectAttribute != null;

        public string uxmlName
        {
            get
            {
                // TemplateContainer must use the class name
                if (serializedDataType.DeclaringType == typeof(TemplateContainer))
                    return nameof(TemplateContainer);

                var elementAttribute = serializedDataType.DeclaringType.GetCustomAttribute<UxmlElementAttribute>();
                if (elementAttribute != null && !string.IsNullOrEmpty(elementAttribute.name))
                    return elementAttribute.name;
                return serializedDataType.DeclaringType.Name;
            }
        }

        public string uxmlFullName
        {
            get
            {
                if (string.IsNullOrEmpty(serializedDataType.DeclaringType.Namespace))
                    return uxmlName;
                return $"{serializedDataType.DeclaringType.Namespace}.{uxmlName}";
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

            GatherAttributesForType(t.BaseType, defaultObject);

            var serializedFields = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            // Some type don't define Uxml attributes and only inherit them
            if (serializedFields.Length == 0)
                return;

            foreach (var fieldInfo in serializedFields)
            {
                var description = CreateSerializedAttributeDescription(fieldInfo, defaultObject);
                if (description == null)
                    continue;

                // When a UxmlObjectAttribute does not contain a name then we treat it as a legacy field,
                // one that does not have an element for the field name, e.g MultiColumnListView.
                var referenceField = fieldInfo.GetCustomAttribute<UxmlObjectReferenceAttribute>();
                if (referenceField != null && !string.IsNullOrEmpty(referenceField.name))
                {
                    m_UxmlObjectFields.Add(referenceField.name);
                }

                var attributeName = description.name;
                if (m_UxmlNameToIndex.TryGetValue(attributeName, out var index))
                {
                    // Override base class attribute
                    m_SerializedAttributes[index] = description;
                }
                else
                {
                    m_SerializedAttributes.Add(description);
                    index = m_SerializedAttributes.Count - 1;
                    m_UxmlNameToIndex[attributeName] = index;
                }

                m_PropertyNameToIndex[fieldInfo.Name] = index;
            }
        }

        /// <summary>
        /// Whether or not this type contain a field with a <see cref="UxmlObjectAttribute"/> attribute with a matching UXML name.
        /// </summary>
        /// <param name="name">The UXML element name to check for.</param>
        /// <returns></returns>
        public bool IsUxmlObjectField(string name) => m_UxmlObjectFields.Contains(name);

        private static UxmlSerializedAttributeDescription CreateSerializedAttributeDescription(FieldInfo fieldInfo, object defaultObject)
        {
            UxmlSerializedAttributeDescription uxmlAttributeDescription;
            if (fieldInfo.GetCustomAttribute<UxmlIgnoreAttribute>() != null)
                return null;

            if (fieldInfo.GetCustomAttribute<UxmlObjectReferenceAttribute>() is { } objectReferenceAttribute)
            {
                uxmlAttributeDescription = new UxmlSerializedUxmlObjectAttributeDescription { rootName = objectReferenceAttribute.name };
            }
            else
            {
                if (!IsValidAttributeType(fieldInfo.FieldType))
                {
                    var elementType = defaultObject != null ? defaultObject.GetType() : fieldInfo.DeclaringType.DeclaringType;
                    if (!InternalEditorUtility.IsUnityAssembly(elementType))
                    {
                        Debug.LogError($"[UxmlElement] '{elementType.Name}' has a [UxmlAttribute] '{GetUxmlName(fieldInfo)}' of an unknown type '{fieldInfo.FieldType.Name}'.\n" +
                                       $"To fix this error define a custom {nameof(UxmlAttributeConverter)}<{fieldInfo.FieldType.Name}>.");
                    }
                    return null;
                }

                uxmlAttributeDescription = new UxmlSerializedAttributeDescription();
            }

            uxmlAttributeDescription.name = GetUxmlName(fieldInfo);
            uxmlAttributeDescription.type = fieldInfo.FieldType;
            uxmlAttributeDescription.serializedField = fieldInfo;

            // Type are not serializable. They are serialized as string with a UxmlTypeReferenceAttribute.
            if (fieldInfo.GetCustomAttribute<UxmlTypeReferenceAttribute>() != null)
                uxmlAttributeDescription.type = typeof(Type);

            if (TryGetDefaultValue(fieldInfo, defaultObject, out var defaultValue))
                uxmlAttributeDescription.defaultValue = defaultValue;

            // Look for obsolete names
            var uxmlAttribute = fieldInfo.GetCustomAttribute<UxmlAttributeAttribute>();
            if (uxmlAttribute?.obsoleteNames?.Length > 0)
                uxmlAttributeDescription.obsoleteNames = uxmlAttribute.obsoleteNames;

            var formerlySerializedAttributes = fieldInfo.GetCustomAttributes<FormerlySerializedAsAttribute>();
            if (formerlySerializedAttributes.Any())
            {
                var obsoleteNames = formerlySerializedAttributes.Select(a => a.oldName);
                if (uxmlAttributeDescription.obsoleteNames == null)
                    uxmlAttributeDescription.obsoleteNames = obsoleteNames;
                else
                    uxmlAttributeDescription.obsoleteNames = uxmlAttributeDescription.obsoleteNames.Concat(obsoleteNames);
            }

            return uxmlAttributeDescription;
        }

        internal static string GetUxmlName(FieldInfo fieldInfo)
        {
            var uxmlAttribute = fieldInfo.GetCustomAttribute<UxmlAttributeAttribute>();
            if (!string.IsNullOrEmpty(uxmlAttribute?.name))
                return uxmlAttribute.name;

            // Use the name of the field to determine the attribute name
            var sb = GenericPool<StringBuilder>.Get();

            var fieldName = fieldInfo.Name;
            for (int i = 0; i < fieldName.Length; i++)
            {
                var c = fieldName[i];
                if (char.IsUpper(c))
                {
                    c = char.ToLower(c);
                    if (i > 0)
                        sb.Append("-");
                }

                sb.Append(c);
            }

            var result = sb.ToString();
            GenericPool<StringBuilder>.Release(sb.Clear());
            return result;
        }

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
