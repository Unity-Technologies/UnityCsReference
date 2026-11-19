// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.UIElements;
using UnityEditor.Utils;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Unity.UIToolkit.Editor
{
    internal static partial class LibraryContent
    {
        const string k_ShowInternalControlsPrefKey = "UIToolkit.UILibrary.ShowInternalControls";
        const string k_ShowPackageControlsPrefKey = "UIToolkit.UILibrary.ShowPackageControls";

        [AutoStaticsCleanupOnCodeReload] // caches user Type refs; cleared on reload so unloaded assemblies aren't pinned
        static Dictionary<LibraryTypeKey, LibraryItem> s_LibraryTypes;
        [AutoStaticsCleanupOnCodeReload]
        static bool s_VariantsGenerated;
        [AutoStaticsCleanupOnCodeReload] // caches user Assembly refs; cleared on reload so unloaded assemblies aren't pinned
        static readonly Dictionary<Assembly, bool> s_IsPackageAssembly = new();
        [AutoStaticsCleanupOnCodeReload] // the script assemblies change with each compilation
        static HashSet<string> s_EditorScriptAssemblyNames;

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
            }

            return dictionary;
        }

        // Variant discovery is what forces ElementConfiguratorRegistry's TypeCache scans, so it is kept
        // out of the base table and only runs for callers that surface variants (the UI Library).
        static void AddVariantLibraryTypes(Dictionary<LibraryTypeKey, LibraryItem> dictionary)
        {
            var serializedDataTypes = UxmlSerializedDataRegistry.SerializedDataTypes;
            foreach (var (key, type) in serializedDataTypes)
            {
                if (!IsValidSerializedDataType(type))
                    continue;

                var declaringType = type.DeclaringType;
                var variantNames = ElementConfiguratorRegistry.GetVariantNames(declaringType);
                if (variantNames.Count == 0)
                    continue;

                var libraryPath = ResolveLibraryPath(declaringType);
                var typeKey = new LibraryTypeKey(declaringType, key);
                foreach (var variantName in variantNames)
                {
                    var variantKey = new LibraryTypeKey(declaringType, key, $"{typeKey.name} ({variantName})", variantName);
                    dictionary.Add(variantKey, new LibraryItem(variantKey.name, variantKey, libraryPath));
                }
            }
        }

        static Dictionary<LibraryTypeKey, LibraryItem> GetLibraryTypes(bool includeVariants)
        {
            s_LibraryTypes ??= GenerateLibraryTypeFromSerializedDataTypes();

            if (includeVariants && !s_VariantsGenerated)
            {
                s_VariantsGenerated = true;
                AddVariantLibraryTypes(s_LibraryTypes);
            }

            return s_LibraryTypes;
        }

        // For testing purposes
        internal static void ResetCache()
        {
            s_LibraryTypes = null;
            s_VariantsGenerated = false;
        }

        /// <summary>
        /// Get all the LibraryTypeKey pairing for the LibraryItem.
        /// </summary>
        /// <param name="includeVariants">Whether the table should also contain the configurator-declared
        /// variants. Pass <see langword="false"/> when only the base types are surfaced.</param>
        /// <returns>An IReadOnlyDictionary of all the LibraryTypeKeys with their respective LibraryItems.</returns>
        public static IReadOnlyDictionary<LibraryTypeKey, LibraryItem> GetAllLibraryTypes(bool includeVariants = true)
        {
            return GetLibraryTypes(includeVariants);
        }

        /// <summary>
        /// Return the LibraryItem for the specified LibraryTypeKey.
        /// </summary>
        /// <param name="key">The LibraryTypeKey to look up.</param>
        /// <returns>The LibraryItem associated to the LibraryTypeKey, or null if the key is not found.</returns>
        public static LibraryItem GetLibraryItemByLibraryKey(LibraryTypeKey key)
        {
            return GetLibraryTypes(includeVariants: key.variantName != null).GetValueOrDefault(key);
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

            GetLibraryTypes(includeVariants: false).TryGetValue(key, out var item);
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

        /// <summary>
        /// Determines whether a type only works in editor UI, which a document has to opt in to with its editor
        /// extension mode. The curated categories say it for the standard controls; any other type is editor-only
        /// when it is compiled in an editor assembly.
        /// </summary>
        internal static bool IsEditorOnly(Type type)
        {
            if (type == null)
                return false;

            var category = LibraryOrdering.GetCategory(type);
            if (category != null)
                return category.IsEditorOnly;

            return (type.Namespace != null && type.Namespace.StartsWith("UnityEditor"))
                || IsEditorAssembly(type.Assembly);
        }

        static bool IsEditorAssembly(Assembly assembly)
        {
            if (assembly.GetCustomAttribute<AssemblyIsEditorAssembly>() != null)
                return true;

            // Script assemblies do not carry the attribute; the compilation pipeline knows which are editor-only.
            if (s_EditorScriptAssemblyNames == null)
            {
                s_EditorScriptAssemblyNames = new HashSet<string>();
                var scriptAssemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies(
                    UnityEditor.Compilation.AssembliesType.Editor);
                foreach (var scriptAssembly in scriptAssemblies)
                {
                    if ((scriptAssembly.flags & UnityEditor.Compilation.AssemblyFlags.EditorAssembly) != 0)
                        s_EditorScriptAssemblyNames.Add(scriptAssembly.name);
                }
            }

            return s_EditorScriptAssemblyNames.Contains(assembly.GetName().Name);
        }

        /// <summary>
        /// Lazily enumerates the project's UXML documents as <see cref="LibraryItem"/>s, sorted by path. The
        /// documents of embedded and local packages are included on request; other packages are read-only and
        /// never are.
        /// </summary>
        internal static IEnumerable<LibraryItem> EnumerateProjectTemplates(bool includePackages = false)
        {
            var searchFilter = new SearchFilter
            {
                classNames = new[] { nameof(VisualTreeAsset) },
                searchArea = includePackages ? SearchFilter.SearchArea.AllAssets : SearchFilter.SearchArea.InAssetsOnly
            };

            var guids = AssetDatabase.FindAssets(searchFilter);
            var paths = new string[guids.Length];
            for (var i = 0; i < guids.Length; i++)
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);

            Array.Sort(paths, StringComparer.Ordinal);

            foreach (var assetPath in paths)
            {
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                if (includePackages && !assetPath.StartsWith("Assets/", StringComparison.Ordinal) && !IsWritablePackageAsset(assetPath))
                    continue;

                var name = Path.GetFileName(assetPath);
                var folder = Path.GetDirectoryName(assetPath);
                folder = string.IsNullOrEmpty(folder) ? string.Empty : folder.ConvertSeparatorsToUnity();

                yield return new LibraryItem(name, assetPath, folder);
            }
        }

        static bool IsWritablePackageAsset(string assetPath)
        {
            var packageInfo = PackageInfo.FindForAssetPath(assetPath);
            return packageInfo != null && (packageInfo.source == PackageSource.Embedded || packageInfo.source == PackageSource.Local);
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
