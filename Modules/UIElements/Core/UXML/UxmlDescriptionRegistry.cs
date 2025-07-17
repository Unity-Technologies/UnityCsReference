// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Holds the description data of a UXML attribute.
    /// </summary>
    /// <remarks>
    /// This is used by the code generator when a control is using <see cref="UxmlElementAttribute"/> and
    /// <see cref="UxmlAttributeAttribute"/>.
    /// </remarks>
    public readonly struct UxmlAttributeNames
    {
        /// <summary>
        /// The field name of the UXML attribute
        /// </summary>
        public readonly string fieldName;

        /// <summary>
        /// The uxml name of the UXML attribute.
        /// </summary>
        public readonly string uxmlName;

        /// <summary>
        /// The type reference of the UXML attribute.
        /// </summary>
        public readonly Type typeReference;

        /// <summary>
        /// The obsolete names of the UXML attribute.
        /// </summary>
        public readonly string[] obsoleteNames;

        /// <summary>
        /// Creates a new instance of <see cref="UxmlAttributeNames"/>.
        /// </summary>
        /// <param name="fieldName">The field name of the UXML attribute.</param>
        /// <param name="uxmlName">The type reference of the UXML attribute.</param>
        /// <param name="typeReference">The type reference of the UXML attribute.</param>
        /// <param name="obsoleteNames">The obsolete names of the UXML attribute.</param>
        public UxmlAttributeNames(string fieldName, string uxmlName, Type typeReference = null, params string[] obsoleteNames)
        {
            this.fieldName = fieldName;
            this.uxmlName = uxmlName;
            this.obsoleteNames = obsoleteNames ?? Array.Empty<string>();
            this.typeReference = typeReference;
        }
    }

    /// <summary>
    /// Attribute allowing the UXML registry to more efficiently retrieve the UXML description data.
    /// </summary>
    /// <remarks>
    /// This is used by the code generator when a control is using <see cref="UxmlElementAttribute"/> and
    /// <see cref="UxmlAttributeAttribute"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class RegisterUxmlCacheAttribute : Attribute
    {
    }

    /// <summary>
    /// Contains pre-processed information about UXML attribute descriptions to avoid relying on reflection.
    /// </summary>
    /// <remarks>
    /// This is used by the code generator when a control is using <see cref="UxmlElementAttribute"/> and
    /// <see cref="UxmlAttributeAttribute"/>.
    /// </remarks>
    public static class UxmlDescriptionCache
    {
        private static readonly Dictionary<Type, CachedDescription> s_NamesPerType = new ();

        internal struct CachedDescription
        {
            public UxmlAttributeNames[] attributeNames;
            public bool editorOnly;
        }

        /// <summary>
        /// Registers pre-processed UXML attribute descriptions.
        /// </summary>
        /// <param name="type">The type to register.</param>
        /// <param name="attributeNames">The pre-processed UXML attribute information.</param>
        /// <param name="isEditorOnly">Indicates whether the type is editor-only.</param>
        /// <remarks>
        /// This is used by the code generator when a control is using <see cref="UxmlElementAttribute"/> and
        /// <see cref="UxmlAttributeAttribute"/>.
        /// </remarks>
        public static void RegisterType(Type type, UxmlAttributeNames[] attributeNames, bool isEditorOnly = false)
        {
            s_NamesPerType[type] = new CachedDescription { attributeNames = attributeNames, editorOnly = isEditorOnly };
        }

        internal static bool TryGetCachedDescription(Type type, out CachedDescription description) => s_NamesPerType.TryGetValue(type, out description);
    }

    internal readonly struct UxmlDescription
    {
        public readonly string uxmlName;
        public readonly string cSharpName;
        public readonly string overriddenCSharpName;
        public readonly FieldInfo serializedField;
        public readonly FieldInfo serializedFieldAttributeFlags;
        public readonly Type fieldType;
        public readonly string[] obsoleteNames;

        public UxmlDescription(string uxmlName, string cSharpName, string overriddenCSharpName, FieldInfo serializedField, string[] obsoleteNames = null)
        {
            this.uxmlName = uxmlName;
            this.cSharpName = cSharpName;
            this.overriddenCSharpName = overriddenCSharpName;
            this.serializedField = serializedField;
            serializedFieldAttributeFlags = serializedField.DeclaringType.GetField(serializedField.Name + UxmlSerializedData.AttributeFlagSuffix, BindingFlags.Instance | BindingFlags.NonPublic);

            // Type are not serializable. They are serialized as string with a UxmlTypeReferenceAttribute.
            fieldType = serializedField.GetCustomAttribute<UxmlTypeReferenceAttribute>() != null ? typeof(Type) : serializedField.FieldType;
            this.obsoleteNames = obsoleteNames;
        }

        public UxmlDescription(FieldInfo serializedField, UxmlAttributeNames names, string overriddenCSharpName)
        {
            this.uxmlName = names.uxmlName;
            this.cSharpName = names.fieldName;
            this.overriddenCSharpName = overriddenCSharpName;
            this.serializedField = serializedField;
            serializedFieldAttributeFlags = serializedField.DeclaringType.GetField(serializedField.Name + UxmlSerializedData.AttributeFlagSuffix, BindingFlags.Instance | BindingFlags.NonPublic);

            // Type are not serializable. They are serialized as string with a UxmlTypeReferenceAttribute.
            fieldType = null != names.typeReference ? typeof(Type) : serializedField.FieldType;
            this.obsoleteNames = names.obsoleteNames;
        }
    }

    internal readonly struct UxmlTypeDescription
    {
        private static readonly Type s_UxmlSerializedDataType = typeof(UxmlSerializedData);

        public readonly Type type;
        public readonly List<UxmlDescription> attributeDescriptions;
        public readonly Dictionary<string, int> uxmlNameToIndex;
        public readonly Dictionary<string, int> cSharpNameToIndex;
        public readonly bool isEditorOnly;

        public UxmlTypeDescription(Type type)
        {
            if (!typeof(UxmlSerializedData).IsAssignableFrom(type))
                throw new ArgumentException();

            this.type = type;
            attributeDescriptions = new();
            uxmlNameToIndex = new();
            cSharpNameToIndex = new();

            if (UxmlDescriptionCache.TryGetCachedDescription(type, out var cachedDescription))
                isEditorOnly = cachedDescription.editorOnly;
            else
                isEditorOnly = false;

            GenerateAttributeDescription(type, cachedDescription.attributeNames);
        }

        private void GenerateAttributeDescription(Type t, UxmlAttributeNames[] attributes)
        {
            // Retrieve the base classes' attributes instead of recomputing them.
            if (t.BaseType != null && t.BaseType != s_UxmlSerializedDataType)
            {
                var parentDesc = UxmlDescriptionRegistry.GetDescription(t.BaseType);
                attributeDescriptions.AddRange(parentDesc.attributeDescriptions);
                foreach (var kvp in parentDesc.uxmlNameToIndex)
                    uxmlNameToIndex[kvp.Key] = kvp.Value;
                foreach (var kvp in parentDesc.cSharpNameToIndex)
                    cSharpNameToIndex[kvp.Key] = kvp.Value;
            }

            if (attributes != null)
            {
                foreach (var attribute in attributes)
                {
                    var fieldInfo = t.GetField(attribute.fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                    if (null == fieldInfo)
                    {
                        Debug.Log($"{t.DeclaringType.Name}: {attribute.fieldName} not found.");
                    }

                    var nameValidationError = UxmlUtility.ValidateUxmlName(attribute.uxmlName);
                    if (nameValidationError != null)
                    {
                        Debug.LogError($"Invalid UXML name '{attribute.uxmlName}' for attribute '{fieldInfo.Name}' in type '{fieldInfo.DeclaringType.DeclaringType}'. {nameValidationError}");
                        continue;
                    }

                    var attributeIsOverridden = uxmlNameToIndex.TryGetValue(attribute.uxmlName, out var index);
                    string overriddenCSharpName = null;
                    if (attributeIsOverridden)
                    {
                        overriddenCSharpName = attributeDescriptions[index].overriddenCSharpName ??
                                               attributeDescriptions[index].cSharpName;
                    }

                    var description = new UxmlDescription(fieldInfo, attribute, overriddenCSharpName);

                    if (attributeIsOverridden)
                    {
                        // Override base class attribute
                        attributeDescriptions[index] = description;
                    }
                    else
                    {
                        attributeDescriptions.Add(description);
                        index = attributeDescriptions.Count - 1;
                        uxmlNameToIndex[attribute.uxmlName] = index;
                    }

                    cSharpNameToIndex[fieldInfo.Name] = index;
                }
            }
            else
            {
                var serializedFields = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                // Some type don't define Uxml attributes and only inherit them
                if (serializedFields.Length == 0)
                    return;

                foreach (var fieldInfo in serializedFields)
                {
                    if (fieldInfo.GetCustomAttribute<UxmlIgnoreAttribute>() != null)
                    {
                        continue;
                    }

                    var cSharpName = fieldInfo.Name;
                    var uxmlNames = GetUxmlNames(fieldInfo);
                    if (!uxmlNames.valid)
                    {
                        continue;
                    }

                    var attributeName = uxmlNames.uxmlName;
                    var attributeIsOverridden = uxmlNameToIndex.TryGetValue(attributeName, out var index);
                    string overriddenCSharpName = null;
                    if (attributeIsOverridden)
                    {
                        overriddenCSharpName = attributeDescriptions[index].overriddenCSharpName ??
                                               attributeDescriptions[index].cSharpName;
                    }

                    var description = new UxmlDescription(uxmlNames.uxmlName, cSharpName, overriddenCSharpName,
                        fieldInfo, uxmlNames.obsoleteNames);

                    if (attributeIsOverridden)
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
            var result = fieldInfo.Name.ToKebabCase();
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

        public static void Clear()
        {
            s_UxmlDescriptions.Clear();
        }
    }
}
