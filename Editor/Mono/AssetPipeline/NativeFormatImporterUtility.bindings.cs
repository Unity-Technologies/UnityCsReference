// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Bindings;
using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    [InitializeOnLoad]
    [NativeHeader("Editor/Src/AssetPipeline/NativeFormatImporter.h")]
    internal static class NativeFormatImporterUtility
    {
        static NativeFormatImporterUtility()
        {
            foreach (var type in EditorAssemblies.GetAllTypesWithAttribute<AssetFileNameExtensionAttribute>())
            {
                var attr = type.GetCustomAttributes(typeof(AssetFileNameExtensionAttribute), false)[0]
                    as AssetFileNameExtensionAttribute;
                try
                {
                    RegisterExtensionForType(type, attr.preferredExtension, attr.otherExtensions.ToArray());
                }
                catch (ArgumentException e)
                {
                    Debug.LogException(e);
                }
                catch (NotSupportedException e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private const string k_DefaultExtension = "asset";

        static readonly Dictionary<Type, string[]> s_RegisteredExtensionsByType = new Dictionary<Type, string[]>();

        internal static void RegisterExtensionForType(
            Type type, string preferredExtension, params string[] otherExtensions
            )
        {
            if (!type.IsSubclassOf(typeof(ScriptableObject)))
            {
                throw new NotSupportedException(
                    string.Format(
                        "{0} may only be added to {1} types.",
                        typeof(AssetFileNameExtensionAttribute), typeof(ScriptableObject)
                        )
                    );
            }

            if (s_RegisteredExtensionsByType.ContainsKey(type))
                throw new ArgumentException(string.Format("Extension already registered for type {0}.", type), "type");

            var extensions = new string[otherExtensions.Length + 1];

            extensions[0] = ValidateExtension(preferredExtension, type);
            for (int i = 0, count = otherExtensions.Length; i < count; ++i)
                extensions[i + 1] = ValidateExtension(otherExtensions[i], type);

            s_RegisteredExtensionsByType[type] = extensions;
        }

        static string ValidateExtension(string extension, Type requestingType)
        {
            if (string.Equals(extension, k_DefaultExtension, StringComparison.OrdinalIgnoreCase))
                return extension;
            var registered = s_RegisteredExtensionsByType.FirstOrDefault(
                    kv => kv.Value.Count(ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase)) > 0
                    );
            if (registered.Key != null)
            {
                throw new ArgumentException(
                    string.Format(
                        "Extension \"{0}\" is already registered for type {1}. It cannot also be used for {2}.",
                        extension, registered.Key, requestingType
                        ),
                    "extension"
                    );
            }
            bool nativeExtensionRegistered;
            var registeredType = Internal_GetNativeTypeForExtension(extension, out nativeExtensionRegistered);
            if (!nativeExtensionRegistered)
            {
                throw new ArgumentException(
                    string.Format(
                        "Extension \"{0}\" must also be registered in NativeFormatImporterExtensions.h with identical capitalization.",
                        extension
                        ),
                    "extension"
                    );
            }
            if (registeredType != null && registeredType != requestingType)
            {
                throw new ArgumentException(
                    string.Format(
                        "Extension \"{0}\" is registered for type {1}. It cannot also be used for {2}.",
                        extension, registeredType, requestingType
                        ),
                    "extension"
                    );
            }
            return extension;
        }

        // TODO: swap bool and Type when bindings support `out Type`
        [FreeFunction(Name = "NativeFormatImporter::Internal_GetNativeTypeForExtension", IsThreadSafe = true)]
        static extern Type Internal_GetNativeTypeForExtension(string extension, out bool registered);

        internal static string GetExtensionForAsset(UnityObject asset)
        {
            var assetType = asset.GetType();

            if (!typeof(ScriptableObject).IsAssignableFrom(assetType))
                return Internal_GetExtensionForNativeAsset(asset);

            // prefer the most specific type that matches (matches native implementation)
            while (assetType != typeof(UnityObject))
            {
                foreach (var kv in s_RegisteredExtensionsByType)
                {
                    if (kv.Key == assetType)
                        return kv.Value[0];
                }
                assetType = assetType.BaseType;
            }
            return k_DefaultExtension;
        }

        [FreeFunction(Name = "NativeFormatImporter::Internal_GetExtensionForNativeAsset", IsThreadSafe = true)]
        static extern string Internal_GetExtensionForNativeAsset(UnityObject asset);
    }
}
