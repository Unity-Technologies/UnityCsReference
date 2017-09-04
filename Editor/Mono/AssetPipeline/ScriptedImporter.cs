// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.AssetImporters
{
    // Class concept: helper class that holds a generated asset object, during the import phase of scripted importer.
    //
    // Motivation: A source asset file can produce multiple objects. Needed a way to generically hold these objects and keep track of which one is the main object.
    class ImportedObject
    {
        // Is this the main part of the asset being imported?
        public bool mainAssetObject { get; set; }

        public Object obj { get; set; }

        public string localIdentifier { get; set; }

        public Texture2D thumbnail { get; set; }
    }

    public class AssetImportContext
    {
        public string assetPath { get; internal set; }
        public BuildTarget selectedBuildTarget { get; internal set;  }

        List<ImportedObject> m_ImportedObjects = new List<ImportedObject>();
        internal List<ImportedObject> importedObjects
        {
            get { return m_ImportedObjects; }
        }

        internal AssetImportContext()
        {
        }

        public void SetMainObject(Object obj)
        {
            if (obj == null)
                return;

            var mainObject = m_ImportedObjects.FirstOrDefault(x => x.mainAssetObject);
            if (mainObject != null)
            {
                if (mainObject.obj == obj)
                    return;

                Debug.LogWarning(string.Format(@"An object was already set as the main object: ""{0}"" conflicting on ""{1}""", assetPath, mainObject.localIdentifier));
                mainObject.mainAssetObject = false;
            }

            mainObject = m_ImportedObjects.FirstOrDefault(x => x.obj == obj);
            if (mainObject == null)
            {
                throw new Exception("Before an object can be set as main, it must first be added using AddObjectToAsset.");
            }

            mainObject.mainAssetObject = true;
            m_ImportedObjects.Remove(mainObject);
            m_ImportedObjects.Insert(0, mainObject);
        }

        public void AddObjectToAsset(string identifier, Object obj)
        {
            AddObjectToAsset(identifier, obj, null);
        }

        public void AddObjectToAsset(string identifier, Object obj, Texture2D thumbnail)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj", "Cannot add a null object : " + (identifier ?? "<null>"));
            }

            var desc = new ImportedObject()
            {
                mainAssetObject = false,
                localIdentifier = identifier,
                obj = obj,
                thumbnail = thumbnail
            };
            m_ImportedObjects.Add(desc);
        }
    }

    // Class Concept: Root, abstract class for all Asset importers implemented in C#.
    public abstract class ScriptedImporter : AssetImporter
    {
        // Struct Concept: simple struct to carry import request arguments from native code.
        // Must mirror native counterpart exactly!
        [StructLayout(LayoutKind.Sequential)]
        struct ImportRequest
        {
            public readonly string m_AssetSourcePath;
            public readonly BuildTarget m_SelectedBuildTarget;
        }

        // Struct Concept: simple struct to carry resulting objects of import back to native code.
        // Must mirror native counterpart exactly!
        [StructLayout(LayoutKind.Sequential)]
        struct ImportResult
        {
            public Object[] m_Assets;
            public string[] m_AssetNames;
            public Texture2D[] m_Thumbnails;
        }

        // Called by native code to invoke the import handling code of the specialized scripted importer class.
        // Marshals the data between the two models (native / managed)
        [RequiredByNativeCode]
        ImportResult GenerateAssetData(ImportRequest request)
        {
            var ctx = new AssetImportContext()
            {
                assetPath = request.m_AssetSourcePath,
                selectedBuildTarget = request.m_SelectedBuildTarget
            };

            OnImportAsset(ctx);

            var result = new ImportResult
            {
                m_Assets = ctx.importedObjects.Select(x => x.obj).ToArray(),
                m_AssetNames = ctx.importedObjects.Select(x => x.localIdentifier).ToArray(),
                m_Thumbnails = ctx.importedObjects.Select(x => x.thumbnail).ToArray()
            };

            return result;
        }

        public abstract void OnImportAsset(AssetImportContext ctx);

        [RequiredByNativeCode]
        internal static void RegisterScriptedImporters()
        {
            var importers = AttributeHelper.FindEditorClassesWithAttribute(typeof(ScriptedImporterAttribute));
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

                // Register the importer
                foreach (var ext in handledExts)
                    AssetImporter.RegisterImporter(importerType, attribute.version, attribute.importQueuePriority, ext.Key);
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
