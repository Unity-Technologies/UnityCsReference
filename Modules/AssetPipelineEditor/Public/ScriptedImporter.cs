// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental.AssetImporters
{
    // Class Concept: Root, abstract class for all Asset importers implemented in C#.
    [ExtensionOfNativeClass]
    [Preserve]
    [UsedByNativeCode]
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
            for (var index = 0; index < importers.Count; index++)
            {
                var importer = importers[index];
                var attribute = Attribute.GetCustomAttribute(importer, typeof(ScriptedImporterAttribute)) as ScriptedImporterAttribute;
                var handledExts = GetHandledExtensionsByImporter(attribute);

                if (handledExts.Count == 0)
                    Debug.LogError($"The ScriptedImporter {importer.FullName} does not provide any none null file extension.");

                // Prevent duplicates between importers! When duplicates found: all are rejected
                for (var j = index + 1; j < importers.Count; j++)
                {
                    var imp = importers[j];
                    if (imp == importer)
                        continue;

                    var attribute2 = Attribute.GetCustomAttribute(imp, typeof(ScriptedImporterAttribute)) as ScriptedImporterAttribute;
                    var handledExts2 = GetHandledExtensionsByImporter(attribute2);

                    // Remove intersection?
                    foreach (var x1 in handledExts2)
                    {
                        // reject the scripted importers that handle the same extension *AND* are both AutoSelected
                        if (handledExts.ContainsKey(x1.Key) && handledExts[x1.Key] && x1.Value)
                        {
                            // Log error message and remove from handledExts
                            Debug.LogError(String.Format("Scripted importers {0} and {1} are targeting the {2} extension, rejecting both.", importer.FullName, (imp as Type).FullName, x1.Key));
                            handledExts.Remove(x1.Key);
                        }
                    }
                }

                var supportsImportDependencyHinting = importer.GetMethod("GetHashOfImportedAssetDependencyHintsForTesting", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) != null
                    || importer.GetMethod("GatherDependenciesFromSourceFile", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) != null;

                // Register the importer
                foreach (var ext in handledExts)
                    AssetImporter.RegisterImporter(importer, attribute.version, attribute.importQueuePriority, ext.Key, supportsImportDependencyHinting, ext.Value, attribute.AllowCaching);
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
