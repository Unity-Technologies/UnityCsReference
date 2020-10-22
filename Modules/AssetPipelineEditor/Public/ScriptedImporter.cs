// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.AssetImporters
{
    // Class Concept: Root, abstract class for all Asset importers implemented in C#.
    [ExtensionOfNativeClass]
    [Preserve]
    [UsedByNativeCode]
    [MovedFrom("UnityEditor.Experimental.AssetImporters")]
    public abstract class ScriptedImporter : AssetImporter
    {
        // Called by native code to invoke the import handling code of the specialized scripted importer class.
        // Marshals the data between the two models (native / managed)
        [RequiredByNativeCode]
        void GenerateAssetData(AssetImportContext ctx)
        {
            OnImportAsset(ctx);
        }

        public abstract void OnImportAsset(AssetImportContext ctx);

        public new virtual bool SupportsRemappedAssetType(Type type)
        {
            return false;
        }

        [RequiredByNativeCode]
        private static bool SupportsRemappedAssetTypeProxy(ScriptedImporter importer, Type type)
        {
            return importer.SupportsRemappedAssetType(type);
        }

        [RequiredByNativeCode]
        internal static void RegisterScriptedImporters()
        {
            var importers = TypeCache.GetTypesWithAttribute<ScriptedImporterAttribute>();

            ScriptedImporterAttribute[] scriptedImporterAttributes = new ScriptedImporterAttribute[importers.Count];
            SortedDictionary<string, bool>[] handledExtensions = new SortedDictionary<string, bool>[importers.Count];

            Dictionary<string, List<int>> importersByExtension = new Dictionary<string, List<int>>();

            // Build a dictionary of which importers are trying to register each extension that we can use to quickly find conflicts
            // (i.e. if an entry in the extension-keyed dictionary has a list of importers with more than one entry)
            for (var importerIndex = 0; importerIndex < importers.Count; importerIndex++)
            {
                Type importer = importers[importerIndex];
                scriptedImporterAttributes[importerIndex] = Attribute.GetCustomAttribute(importer, typeof(ScriptedImporterAttribute)) as ScriptedImporterAttribute;
                handledExtensions[importerIndex] = GetHandledExtensionsByImporter(scriptedImporterAttributes[importerIndex]);

                if (handledExtensions[importerIndex].Count == 0)
                    Debug.LogError($"The ScriptedImporter {importer.FullName} does not provide any non-null file extension.");

                foreach (KeyValuePair<string, bool> handledExtension in handledExtensions[importerIndex])
                {
                    // Only consider AutoSelected ones
                    if (handledExtension.Value)
                    {
                        // Add this importer to the dictionary entry for this file extension, creating it if not already present
                        if (!importersByExtension.TryGetValue(handledExtension.Key, out List<int> importerIndicesForThisExtension))
                        {
                            importerIndicesForThisExtension = new List<int>();
                            importersByExtension.Add(handledExtension.Key, importerIndicesForThisExtension);
                        }
                        importerIndicesForThisExtension.Add(importerIndex);
                    }
                }
            }

            // Check each AutoSelected extension we found, any of them that have more than one importer associated with them are rejected (i.e. removed from all importers that provided them)
            foreach (KeyValuePair<string, List<int>> importerExtension in importersByExtension)
            {
                if (importerExtension.Value.Count > 1)
                {
                    string rejectedImporters = "";
                    foreach (int importerIndex in importerExtension.Value)
                    {
                        handledExtensions[importerIndex].Remove(importerExtension.Key);

                        if (rejectedImporters.Length == 0)
                            rejectedImporters = importers[importerIndex].FullName;
                        else
                            rejectedImporters = rejectedImporters + ", " + importers[importerIndex].FullName;
                    }
                    Debug.LogError(String.Format("Multiple scripted importers ({0}) are targeting the extension '{1}' and have all been rejected.", rejectedImporters, importerExtension.Key));
                }
            }

            for (var index = 0; index < importers.Count; index++)
            {
                var importer = importers[index];
                var attribute = scriptedImporterAttributes[index];
                var handledExts = handledExtensions[index];

                if (handledExts.Count > 0)
                {
                    var supportsImportDependencyHinting =
                        (importer.GetMethod("GetHashOfImportedAssetDependencyHintsForTesting", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) != null) ||
                        (importer.GetMethod("GatherDependenciesFromSourceFile", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) != null);

                    // Register the importer
                    foreach (var ext in handledExts)
                        AssetImporter.RegisterImporter(importer, attribute.version, attribute.importQueuePriority, ext.Key, supportsImportDependencyHinting, ext.Value, attribute.AllowCaching);
                }
            }
        }

        static SortedDictionary<string, bool> GetHandledExtensionsByImporter(ScriptedImporterAttribute attribute)
        {
            var handledExts = new SortedDictionary<string, bool>();
            if (attribute.fileExtensions != null)
            {
#pragma warning disable 618
                // AutoSelect is now obsolete, but in order to support user scripts still using it we need to read its value until we make it Obsolete error.
                var isDefault = attribute.AutoSelect;
#pragma warning restore 618
                for (var index = 0; index < attribute.fileExtensions.Length; index++)
                {
                    var cleanExt = attribute.fileExtensions[index].Trim('.');
                    if (!string.IsNullOrEmpty(cleanExt))
                        handledExts.Add(cleanExt, isDefault);
                }
            }

            if (attribute.overrideFileExtensions != null)
            {
                for (var index = 0; index < attribute.overrideFileExtensions.Length; index++)
                {
                    var cleanExt = attribute.overrideFileExtensions[index].Trim('.');
                    if (!string.IsNullOrEmpty(cleanExt))
                        handledExts.Add(cleanExt, false);
                }
            }
            return handledExts;
        }
    }

    // Class Concept: Class attribute that describes Scriptable importers and their static characteristics.
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [MovedFrom("UnityEditor.Experimental.AssetImporters")]
    public class ScriptedImporterAttribute : Attribute
    {
        public int version { get; private set; }

        // Gives control over when the asset is imported with regards to assets of other types.
        // Positive values delay the processing of source asset files while Negative values place them earlier in the import process.
        public int importQueuePriority { get; private set; }

        public string[] fileExtensions { get; private set; }

        public string[] overrideFileExtensions { get; private set; }

        [Obsolete("Use overrideFileExtensions instead to specify this importer is an override for those file extensions")]
        public bool AutoSelect = true;

        public bool AllowCaching = false;


        public ScriptedImporterAttribute(int version, string ext)
        {
            Init(version, new[] { ext }, null, 0);
        }

        public ScriptedImporterAttribute(int version, string ext, int importQueueOffset)
        {
            Init(version, new[] { ext }, null, importQueueOffset);
        }

        public ScriptedImporterAttribute(int version, string[] exts)
        {
            Init(version, exts, null, 0);
        }

        public ScriptedImporterAttribute(int version, string[] exts, string[] overrideExts)
        {
            Init(version, exts, overrideExts, 0);
        }

        public ScriptedImporterAttribute(int version, string[] exts, int importQueueOffset)
        {
            Init(version, exts, null, importQueueOffset);
        }

        public ScriptedImporterAttribute(int version, string[] exts, string[] overrideExts, int importQueueOffset)
        {
            Init(version, exts, overrideExts, importQueueOffset);
        }

        private void Init(int version, string[] exts, string[] overrideExts, int importQueueOffset)
        {
            this.version = version;
            this.importQueuePriority = importQueueOffset;
            fileExtensions = exts;
            this.overrideFileExtensions = overrideExts;
        }
    }
}

// The following block is to make sure scripts with unnecessary using directive of Experimental.AssetImporters compile
// Should be removed after API updater addresses this kind of migration.
namespace UnityEditor.Experimental.AssetImporters
{
    class PlaceholderEmptyClassForDeprecatedExperimentalAssetImporters {}
}
