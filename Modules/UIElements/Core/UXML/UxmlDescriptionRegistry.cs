// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace UnityEngine.UIElements
{
    internal readonly struct UxmlDescription
    {
        public readonly string uxmlName;
        public readonly string cSharpName;
        public readonly FieldInfo serializedField;
        public readonly FieldInfo serializedFieldAttributeFlags;
        public readonly Type fieldType;
        public readonly string[] obsoleteNames;

        public UxmlDescription(string uxmlName, string cSharpName, FieldInfo serializedField, string[] obsoleteNames = null)
        {
            this.uxmlName = uxmlName;
            this.cSharpName = cSharpName;
            this.serializedField = serializedField;
            serializedFieldAttributeFlags = serializedField.DeclaringType.GetField(serializedField.Name + UxmlSerializedData.AttributeFlagSuffix, BindingFlags.Instance | BindingFlags.NonPublic);

            // Type are not serializable. They are serialized as string with a UxmlTypeReferenceAttribute.
            fieldType = serializedField.GetCustomAttribute<UxmlTypeReferenceAttribute>() != null ? typeof(Type) : serializedField.FieldType;
            this.obsoleteNames = obsoleteNames;
        }
    }

    internal readonly struct UxmlTypeDescription
    {
        public readonly Type type;
        public readonly List<UxmlDescription> attributeDescriptions;
        public readonly Dictionary<string, int> uxmlNameToIndex;
        public readonly Dictionary<string, int> cSharpNameToIndex;

        public UxmlTypeDescription(Type type)
        {
            if (!typeof(UxmlSerializedData).IsAssignableFrom(type))
                throw new ArgumentException();

            this.type = type;
            attributeDescriptions = new();
            uxmlNameToIndex = new();
            cSharpNameToIndex = new();
            GenerateAttributeDescription(type);
        }

        private void GenerateAttributeDescription(Type t)
        {
            if (t == typeof(UxmlSerializedData))
                return;

            GenerateAttributeDescription(t.BaseType);
            var serializedFields = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            // Some type don't define Uxml attributes and only inherit them
            if (serializedFields.Length == 0)
                return;

            foreach (var fieldInfo in serializedFields)
            {
                if (!TryCreateSerializedAttributeDescription(fieldInfo, out var description))
                {
                    continue;
                }

                var attributeName = description.uxmlName;
                if (uxmlNameToIndex.TryGetValue(attributeName, out var index))
                {
                    // Override base class attribute
                    attributeDescriptions[index] = description;
                }
                else
                {
                    attributeDescriptions.Add(description);
                    index = attributeDescriptions.Count - 1;
                    uxmlNameToIndex[attributeName] = index;
                }

                cSharpNameToIndex[fieldInfo.Name] = index;
            }
        }

        private static bool TryCreateSerializedAttributeDescription(FieldInfo fieldInfo, out UxmlDescription description)
        {
            if (fieldInfo.GetCustomAttribute<UxmlIgnoreAttribute>() != null)
            {
                description = default;
                return false;
            }

            var cSharpName = fieldInfo.Name;
            var uxmlNames = GetUxmlNames(fieldInfo);
            if (!uxmlNames.valid)
            {
                description = default;
                return false;
            }

            description = new UxmlDescription(uxmlNames.uxmlName, cSharpName, fieldInfo, uxmlNames.obsoleteNames);
            return true;
        }

        internal static (bool valid, string uxmlName, string[] obsoleteNames) GetUxmlNames(FieldInfo fieldInfo)
        {
            using var pooledListHandle = ListPool<string>.Get(out var obsoleteNamesList);
            using var pooledHashSetHandle = HashSetPool<string>.Get(out var obsoleteNamesSet);

            string[] GetArray(List<string> list)
            {
                if (list.Count == 0)
                    return Array.Empty<string>();
                return list.ToArray();
            }

            var formerlySerializedAttributes = fieldInfo.GetCustomAttributes<FormerlySerializedAsAttribute>();
            foreach (var formerlySerializedAs in formerlySerializedAttributes)
            {
                if (obsoleteNamesSet.Add(formerlySerializedAs.oldName))
                    obsoleteNamesList.Add(formerlySerializedAs.oldName);
            }

            var uxmlAttribute = fieldInfo.GetCustomAttribute<UxmlAttributeAttribute>();
            if (null != uxmlAttribute)
            {
                if (null != uxmlAttribute.obsoleteNames)
                {
                    foreach (var obsoleteName in uxmlAttribute?.obsoleteNames)
                    {
                        if (obsoleteNamesSet.Add(obsoleteName))
                            obsoleteNamesList.Add(obsoleteName);
                    }
                }
                if (!string.IsNullOrWhiteSpace(uxmlAttribute.name))
                {
                    var nameValidationError = UxmlUtility.ValidateUxmlName(uxmlAttribute.name);
                    if (nameValidationError != null)
                    {
                        Debug.LogError($"Invalid UXML name '{uxmlAttribute.name}' for attribute '{fieldInfo.Name}' in type '{fieldInfo.DeclaringType.DeclaringType}'. {nameValidationError}");
                        return (false, null, null);
                    }
                    return (true, uxmlAttribute.name, GetArray(obsoleteNamesList));
                }
            }

            var uxmlObjectAttribute = fieldInfo.GetCustomAttribute<UxmlObjectReferenceAttribute>();
            if (null != uxmlObjectAttribute)
            {
                if (!string.IsNullOrWhiteSpace(uxmlObjectAttribute.name))
                {
                    var validName = UxmlUtility.ValidateUxmlName(uxmlObjectAttribute.name);
                    if (validName != null)
                    {
                        Debug.LogError($"Invalid UXML Object name '{uxmlObjectAttribute.name}' for attribute '{fieldInfo.Name}' in type '{fieldInfo.DeclaringType.DeclaringType}'. {validName}");
                        return (false, null, null);
                    }

                    return (true, uxmlObjectAttribute.name, GetArray(obsoleteNamesList));
                }
            }

            // Use the name of the field to determine the attribute name
            var sb = GenericPool<StringBuilder>.Get();

            var fieldName = fieldInfo.Name;
            for (var i = 0; i < fieldName.Length; i++)
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
            return (true, result, GetArray(obsoleteNamesList));
        }
    }

    internal static class UxmlDescriptionRegistry
    {
        private static readonly Dictionary<Type, UxmlTypeDescription> s_UxmlDescriptions = new();

        public static UxmlTypeDescription GetDescription(Type type)
        {
            if (!s_UxmlDescriptions.TryGetValue(type, out var description))
                s_UxmlDescriptions.Add(type, description = new UxmlTypeDescription(type));

            return description;
        }
    }
}
