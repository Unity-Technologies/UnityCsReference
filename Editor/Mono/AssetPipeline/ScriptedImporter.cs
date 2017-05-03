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
    // Motivation: A source asset file can produce multiple assets (a main one and multple sub assets). Needed a way to genericaly hold these sub assets of the source Asset.
    class ImportedObject
    {
        // Is this the main part of the asset being imported?
        public bool mainAsset { get; set; }

        public Object asset { get; set; }

        // Unique identifier, within imported asset context, of asset
        public string identifier { get; set; }

        public Texture2D thumbnail { get; set; }
    }

    public class AssetImportContext
    {
        public string assetPath { get; internal set; }
        public BuildTarget selectedBuildTarget { get; internal set;  }

        List<ImportedObject> m_SubAssets = new List<ImportedObject>();
        internal List<ImportedObject> subAssets
        {
            get { return m_SubAssets; }
        }

        internal AssetImportContext()
        {
        }

        // Adds an asset object to the resulting asset list and tags it as the main asset.
        // A minimum and a maximum of one asset must be tagged as the main asset.
        // param identifier: Unique identifier, within the source asset, that remains the same accross re-import events of the given source asset file.
        public void SetMainAsset(string identifier, Object asset)
        {
            AddAsset(true, identifier, asset, null);
        }

        // Adds an asset object to the resulting asset list and tags it as the main asset.
        // A minimum and a maximum of one asset must be tagged as the main asset.
        // param identifier: Unique identifier, within the source asset, that remains the same accross re-import events of the given source asset file.
        public void SetMainAsset(string identifier, Object asset, Texture2D thumbnail)
        {
            AddAsset(true, identifier, asset, thumbnail);
        }

        // Adds a sub asset object to the resulting asset list, that is Not the main asset.
        // param identifier: Unique identifier, within the source asset, that remains the same accross re-import events of the given source asset file.
        public void AddSubAsset(string identifier, Object asset)
        {
            AddAsset(false, identifier, asset, null);
        }

        // Adds a sub asset object to the resulting asset list, that is Not the main asset.
        // param identifier: Unique identifier, within the source asset, that remains the same accross re-import events of the given source asset file.
        public void AddSubAsset(string identifier, Object asset, Texture2D thumbnail)
        {
            AddAsset(false, identifier, asset, thumbnail);
        }

        void AddAsset(bool main, string identifier, Object asset, Texture2D thumbnail)
        {
            if (asset == null)
            {
                throw new ArgumentNullException("asset", "Cannot add a null asset : " + (identifier ?? "<null>"));
            }

            var mainAsset = m_SubAssets.FirstOrDefault(x => x.mainAsset);
            if (main && mainAsset != null)
            {
                throw new Exception(String.Format(@"A Main asset has already been added and only one is allowed: ""{0}"" conflicting on ""{1}"" and ""{2}""", assetPath, mainAsset.identifier, identifier));
            }

            var desc = new ImportedObject()
            {
                mainAsset = main,
                identifier = identifier,
                asset = asset,
                thumbnail = thumbnail
            };
            if (main)
                m_SubAssets.Insert(0, desc);
            else
                m_SubAssets.Add(desc);
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

            if (!ctx.subAssets.Any((x) => x.mainAsset))
                throw new Exception("Import failed/rejected as none of the sub assets was set as the 'main asset':" + ctx.assetPath);

            var result = new ImportResult
            {
                m_Assets = ctx.subAssets.Select(x => x.asset).ToArray(),
                m_AssetNames = ctx.subAssets.Select(x => x.identifier).ToArray(),
                m_Thumbnails = ctx.subAssets.Select(x => x.thumbnail).ToArray()
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
