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
        const string k_StandardElementsPath = "Standard Elements";
        const string k_ProjectElementsPath = "Project Elements";

        internal static readonly string[] k_BaseLibraryPaths = new[]
        {
            k_StandardElementsPath,
            k_ProjectElementsPath
        };

        static readonly Dictionary<string, HashSet<string>> s_CategoriesByType = new()
        {
            ["Numeric Fields"] = new()
            {
                nameof(IntegerField),
                nameof(FloatField),
                nameof(LongField),
                nameof(DoubleField)
            }
        };

        /// <summary>
        /// Unity core controls to display in "Standard Elements".
        /// Only these controls will appear (unless they have subcategories defined in s_Categories).
        /// </summary>
        static readonly HashSet<string> s_StandardElementControls = new()
        {
            nameof(VisualElement),
            nameof(ScrollView),
            nameof(Image),
            nameof(Label),
            nameof(Button),
            nameof(Toggle),
            nameof(DropdownField),
            nameof(TextField),
            nameof(Slider),
            nameof(IntegerField),
            nameof(FloatField),
            nameof(DoubleField),
            nameof(LongField),
        };

        static readonly Dictionary<LibraryTypeKey, LibraryItem> s_LibraryTypes = GenerateLibraryTypeFromSerializedDataTypes();

        static Dictionary<LibraryTypeKey, LibraryItem> GenerateLibraryTypeFromSerializedDataTypes()
        {
            var dictionary = new Dictionary<LibraryTypeKey, LibraryItem>();
            var serializedDataTypes = UxmlSerializedDataRegistry.SerializedDataTypes;
            foreach (var (key, type) in serializedDataTypes)
            {
                if (!IsValidSerializedDataType(type))
                    continue;

                var declaringType = type.DeclaringType;
                var libraryPath = ResolveLibraryPath(declaringType);
                var typeKey = new LibraryTypeKey(declaringType, key);
                var typeItem = new LibraryItem(typeKey.name, typeKey, libraryPath);
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

        /// <summary>
        /// Resolves the library path for a type based on namespace and optional attribute.
        /// </summary>
        static string ResolveLibraryPath(Type type)
        {
            if (type == null)
                return null;

            var uxmlAttr = type.GetCustomAttribute<UxmlElementAttribute>();

            // Check visibility setting
            if (uxmlAttr != null && uxmlAttr.visibility == LibraryVisibility.Hidden)
                return null;

            // Removing editor controls - for now
            if (type.Namespace != null && type.Namespace.StartsWith("UnityEditor."))
                return null;

            if (type.IsAbstract)
                return null;

            // Look for existing libraryPath value, this means they have opt-in for their control to appear in the menu.
            if (!string.IsNullOrEmpty(uxmlAttr?.libraryPath))
            {
                // Check if it's a Unity control with explicit path
                if (type.Namespace == "UnityEngine.UIElements")
                {
                    return $"{k_StandardElementsPath}/{uxmlAttr.libraryPath}";
                }

                // "Non-Core" controls
                return $"{k_ProjectElementsPath}/{uxmlAttr.libraryPath}";
            }

            if (type.Namespace == "UnityEngine.UIElements")
            {
                if (!s_StandardElementControls.Contains(type.Name))
                    return null;

                var category= GetCategoryForType(type.Name);
                return category != null ? $"{k_StandardElementsPath}/{category}" : k_StandardElementsPath;
            }

            return null;
        }

        static string GetCategoryForType(string typeName)
        {
            foreach (var (category, types) in s_CategoriesByType)
            {
                if (types.Contains(typeName))
                    return category;
            }

            return null;
        }

        /// <summary>
        /// Checks if a control is a container type that should appear at the top.
        /// </summary>
        internal static bool IsContainer(string typeName)
        {
            return typeName == nameof(VisualElement) || typeName == nameof(ScrollView);
        }
    }
}
