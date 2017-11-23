// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.AssetImporters
{
    // Class Concept: Root, abstract class for all Asset importers implemented in C#.
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

        [RequiredByNativeCode]
        internal static void RegisterScriptedImporters()
        {
            var importers = EditorAssemblies.GetAllTypesWithAttribute<ScriptedImporterAttribute>();
            foreach (var importer in importers)
            {
                var importerType = importer as Type;
                var attribute = Attribute.GetCustomAttribute(importerType, typeof(ScriptedImporterAttribute)) as ScriptedImporterAttribute;
                var handledExts = GetHandledExtensionsByImporter(attribute);

                // Prevent duplicates between importers! When duplicates found: all are rejected
                foreach (var imp in importers)
                {
                    if (imp == importer)
                        continue;

                    var attribute2 = Attribute.GetCustomAttribute(imp as Type, typeof(ScriptedImporterAttribute)) as ScriptedImporterAttribute;
                    var handledExts2 = GetHandledExtensionsByImporter(attribute2);

                    // Remove intersection?
                    foreach (var x1 in handledExts2)
                    {
                        if (handledExts.ContainsKey(x1.Key))
                        {
                            // Log error message and remove from handledExts
                            Debug.LogError(String.Format("Scripted importers {0} and {1} are targeting the {2} extension, rejecting both.", importerType.FullName, (imp as Type).FullName, x1.Key));
                            handledExts.Remove(x1.Key);
                        }
                    }
                }

                MethodInfo getImportDependencyHintMethod = importerType.GetMethod("GetHashOfImportedAssetDependencyHintsForTesting", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                // Register the importer
                foreach (var ext in handledExts)
                    AssetImporter.RegisterImporter(importerType, attribute.version, attribute.importQueuePriority, ext.Key, getImportDependencyHintMethod != null);
            }
        }

        static SortedDictionary<string, bool> GetHandledExtensionsByImporter(ScriptedImporterAttribute attribute)
        {
            var handledExts = new SortedDictionary<string, bool>();
            if (attribute.fileExtensions != null)
            {
                foreach (var ext in attribute.fileExtensions)
                {
                    var cleanExt = ext;
                    if (cleanExt.StartsWith("."))
                        cleanExt = cleanExt.Substring(1);

                    handledExts.Add(cleanExt, true);
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

        public ScriptedImporterAttribute(int version, string[] exts)
        {
            Init(version, exts, 0);
        }

        public ScriptedImporterAttribute(int version, string ext)
        {
            Init(version, new[] { ext }, 0);
        }

        public ScriptedImporterAttribute(int version, string[] exts, int importQueueOffset)
        {
            Init(version, exts, importQueueOffset);
        }

        public ScriptedImporterAttribute(int version, string ext, int importQueueOffset)
        {
            Init(version, new[] { ext }, importQueueOffset);
        }

        private void Init(int version, string[] exts, int importQueueOffset)
        {
            if (exts == null || exts.Any(string.IsNullOrEmpty))
                throw new ArgumentException("Must provide valid, none null, file extension strings.");

            this.version = version;
            this.importQueuePriority = importQueueOffset;
            fileExtensions = exts;
        }
    }
}
