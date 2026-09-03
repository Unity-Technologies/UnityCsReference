// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static partial class LibraryContent
    {
        const string k_ShowInternalControlsPrefKey = "UIToolkit.UILibrary.ShowInternalControls";
        const string k_ShowPackageControlsPrefKey = "UIToolkit.UILibrary.ShowPackageControls";

        [NoAutoStaticsCleanup] // library-type cache, safe to persist
        static readonly Dictionary<LibraryTypeKey, LibraryItem> s_LibraryTypes = GenerateLibraryTypeFromSerializedDataTypes();
        [AutoStaticsCleanupOnCodeReload] // caches user Assembly refs; cleared on reload so unloaded assemblies aren't pinned
        static readonly Dictionary<Assembly, bool> s_IsPackageAssembly = new();

        internal static bool ShowInternalControls
        {
            get => EditorPrefs.GetBool(k_ShowInternalControlsPrefKey, false);
            set => EditorPrefs.SetBool(k_ShowInternalControlsPrefKey, value);
        }

        internal static bool ShowPackageControls
        {
            get => EditorPrefs.GetBool(k_ShowPackageControlsPrefKey, false);
            set => EditorPrefs.SetBool(k_ShowPackageControlsPrefKey, value);
        }

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

                foreach (var variantName in ElementConfiguratorRegistry.GetVariantNames(declaringType))
                {
                    var variantKey = new LibraryTypeKey(declaringType, key, $"{typeKey.name} ({variantName})", variantName);
                    var variantItem = new LibraryItem(variantKey.name, variantKey, libraryPath);
                    dictionary.Add(variantKey, variantItem);
                }
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
        /// Determines whether a type should be surfaced in library UI (picker, menus).
        /// </summary>
        internal static bool IsVisibleInLibrary(Type type)
        {
            if (type == null)
                return false;

            var uxmlAttr = type.GetCustomAttribute<UxmlElementAttribute>();
            if (uxmlAttr != null && uxmlAttr.visibility == LibraryVisibility.Hidden)
                return false;

            if (uxmlAttr == null || uxmlAttr.visibility == LibraryVisibility.Default)
            {
                if (type.Namespace != null && type.Namespace.StartsWith("Unity.UI.Builder"))
                    return false;

                var assemblyVisibility = GetAssemblyVisibility(type);
                if (assemblyVisibility == LibraryVisibility.Hidden)
                    return false;

                if (assemblyVisibility != LibraryVisibility.Visible)
                {
                    if (IsInternalType(type) && !ShowInternalControls)
                        return false;

                    if (IsPackageType(type) && !ShowPackageControls)
                        return false;
                }
            }

            if (type.IsAbstract)
                return false;

            return true;
        }

        static bool IsPackageType(Type type)
        {
            var assembly = type.Assembly;
            if (!s_IsPackageAssembly.TryGetValue(assembly, out var isPackage))
            {
                isPackage = PackageInfo.FindForAssembly(assembly) != null;
                s_IsPackageAssembly[assembly] = isPackage;
            }

            return isPackage;
        }

        static LibraryVisibility GetAssemblyVisibility(Type type)
        {
            var attribute = type.Assembly.GetCustomAttribute<UILibraryVisibilityAttribute>();
            return attribute?.Visibility ?? LibraryVisibility.Default;
        }

        static bool IsInternalType(Type type)
        {
            if (IsUnityBuiltInEditorAssembly(type.Assembly))
                return true;

            return type.Namespace != null && type.Namespace.StartsWith("UnityEditor.");
        }

        static bool IsUnityBuiltInEditorAssembly(System.Reflection.Assembly assembly) =>
            assembly.GetCustomAttribute<AssemblyIsEditorAssembly>() != null &&
            assembly.GetName().Name.StartsWith("UnityEditor");

        /// <summary>
        /// Returns the explicit library path declared on the type, or null when the type has no path set.
        /// </summary>
        static string ResolveLibraryPath(Type type)
        {
            var uxmlAttr = type.GetCustomAttribute<UxmlElementAttribute>();
            if (uxmlAttr is { libraryPath: not null })
                return uxmlAttr.libraryPath == "" ? string.Empty : uxmlAttr.libraryPath;

            return null;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
