// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static class LibraryContent
    {
        static readonly Dictionary<LibraryTypeKey, LibraryItem> s_LibraryTypes = GenerateLibraryTypeFromSerializedDataTypes();

        static Dictionary<LibraryTypeKey, LibraryItem> GenerateLibraryTypeFromSerializedDataTypes()
        {
            var dictionary = new Dictionary<LibraryTypeKey, LibraryItem>();
            var serializedDataTypes = UxmlSerializedDataRegistry.SerializedDataTypes;
            foreach (var (key, type) in serializedDataTypes)
            {
                if (!IsValidSerializedDataType(type))
                    continue;

                var typeKey = new LibraryTypeKey(type.DeclaringType, key);
                var typeItem = new LibraryItem(typeKey.name, typeKey);
                dictionary.Add(typeKey, typeItem);
            }

            return dictionary;
        }

        /// <summary>
        /// Get all the LibraryTypeKey pairing for the LibraryItem.
        /// </summary>
        /// <returns>An IReadOnlyDictionary of all the LibraryTypeKeys with their respective LibraryItems.</returns>
        public static IReadOnlyDictionary<LibraryTypeKey, LibraryItem> GetAllLibraryTypes()
        {
            return s_LibraryTypes;
        }

        /// <summary>
        /// Return the LibraryItem for the specified LibraryTypeKey.
        /// </summary>
        /// <param name="key">The LibraryTypeKey to look up.</param>
        /// <returns>The LibraryItem associated to the LibraryTypeKey, or null if the key is not found.</returns>
        public static LibraryItem GetLibraryItemByLibraryKey(LibraryTypeKey key)
        {
            return s_LibraryTypes.GetValueOrDefault(key);
        }

        /// <summary>
        /// Returns the default LibraryItem for a type.
        /// </summary>
        /// <param name="type">The type to retrieve the default LibraryItem for.</param>
        /// <returns>The LibraryItem associated to the type, or null if the LibraryItem is not found.</returns>
        public static LibraryItem GetDefaultLibraryItem(Type type)
        {
            if (type == null)
                return null;

            var nestedType = type.GetNestedType(nameof(UxmlSerializedData), BindingFlags.Public | BindingFlags.NonPublic) ?? type;
            if (!IsValidSerializedDataType(nestedType))
            {
                Debug.LogWarning($"{type} type is not assignable to VisualElement.UxmlSerializedData.");
                return null;
            }

            var declaredType = type.IsNested ? type.DeclaringType : type;
            var key = new LibraryTypeKey(declaredType, declaredType.FullName);

            s_LibraryTypes.TryGetValue(key, out var item);
            return item;
        }

        /// <summary>
        /// Verifies if the type contains a UXML serialized data.
        /// </summary>
        static bool IsValidSerializedDataType(Type type)
        {
            return typeof(VisualElement.UxmlSerializedData).IsAssignableFrom(type) || type.IsAbstract || type.IsGenericType;
        }
    }
}
