// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEditor.AssetImporters;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/AssetPipeline/AssetImporter.h")]
    [NativeHeader("Editor/Src/AssetPipeline/AssetImporter.bindings.h")]
    [ExcludeFromObjectFactory]
    [Preserve]
    [UsedByNativeCode]
    public partial class AssetImporter : Object
    {
        [NativeType(CodegenOptions.Custom, "MonoSourceAssetIdentifier")]
        public struct SourceAssetIdentifier
        {
            public SourceAssetIdentifier(Object asset)
            {
                if (asset == null)
                {
                    throw new ArgumentNullException("asset");
                }

                this.type = asset.GetType();
                this.name = asset.name;
            }

            public SourceAssetIdentifier(Type type, string name)
            {
                if (type == null)
                {
                    throw new ArgumentNullException("type");
                }

                if (name == null)
                {
                    throw new ArgumentNullException("name");
                }

                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException("The name is empty", "name");
                }

                this.type = type;
                this.name = name;
            }

            public Type type;
            public string name;
        }

        [NativeName("AssetPathName")]
        public extern  string assetPath
        {
            get;
        }

        public extern  bool importSettingsMissing
        {
            get;
        }

        public extern  ulong assetTimeStamp
        {
            get;
        }

        public extern  string userData
        {
            get;
            set;
        }

        public extern  string assetBundleName
        {
            get;
            set;
        }

        public extern  string assetBundleVariant
        {
            get;
            set;
        }

        public static ImportLog GetImportLog(string path)
        {
            return GetImportLog(AssetDatabase.GUIDFromAssetPath(path));
        }

        [FreeFunction("AssetImporter::GetImportLog")]
        internal static extern ImportLog GetImportLog(GUID guid);

        [FreeFunction("AssetImporter::GetImportLogEntriesCount")]
        internal static extern bool GetImportLogEntriesCount(GUID guid, out int nbErrors, out int nbWarnings);

        [NativeName("SetAssetBundleName")]
        extern public void SetAssetBundleNameAndVariant(string assetBundleName, string assetBundleVariant);

        [NativeMethod("SetThumbnailFromTexture2D")]
        extern internal void SetThumbnailFromTexture2D(Texture2D image, int instanceID);

        [FreeFunction("FindAssetImporterAtAssetPath")]
        extern public static  AssetImporter GetAtPath(string path);

        public void SaveAndReimport()
        {
            AssetDatabase.ImportAsset(assetPath);
        }

        [FreeFunction("AssetImporterBindings::LocalFileIDToClassID")]
        extern internal static  int LocalFileIDToClassID(long fileId);

        [FreeFunction("AssetImporterBindings::MakeLocalFileIDWithHash")]
        extern internal static long MakeLocalFileIDWithHash(int persistentTypeId, string name, long offset);
        extern internal long MakeInternalID(int persistentTypeId, string name);

        extern public void AddRemap(SourceAssetIdentifier identifier, Object externalObject);

        extern public bool RemoveRemap(SourceAssetIdentifier identifier);

        extern internal void RenameSubAssets(int peristentTypeId, string[] oldNames, string[] newNames);

        [FreeFunction("AssetImporterBindings::GetIdentifiers")]
        extern private static SourceAssetIdentifier[] GetIdentifiers(AssetImporter self);
        [FreeFunction("AssetImporterBindings::GetExternalObjects")]
        extern private static Object[] GetExternalObjects(AssetImporter self);

        public Dictionary<SourceAssetIdentifier, Object> GetExternalObjectMap()
        {
            // bogdanc: this is not optimal - we should have only one call to get both the identifiers and the external objects.
            // However, the new bindings do not support well output array parameters.
            // FIXME: change this to a single call when the bindings are fixed
            SourceAssetIdentifier[] identifiers = GetIdentifiers(this);
            Object[] externalObjects = GetExternalObjects(this);

            Dictionary<SourceAssetIdentifier, Object> map = new Dictionary<SourceAssetIdentifier, Object>();

            for (int i = 0; i < identifiers.Length; ++i)
            {
                map.Add(identifiers[i], externalObjects[i]);
            }

            return map;
        }

        [FreeFunction("AssetImporterBindings::RegisterImporter", ThrowsException = true)]
        extern internal static  void RegisterImporter(Type importer, int importerVersion, int queuePos, string fileExt, bool supportsImportDependencyHinting, bool autoSelect, bool allowCaching);

        [FreeFunction("AssetImporterBindings::SupportsRemappedAssetType", HasExplicitThis = true, IsThreadSafe = true)]
        public extern bool SupportsRemappedAssetType(Type type);

        internal extern double GetImportStartTime();

        internal AssetPostprocessor.PostprocessorInfo[] GetDynamicPostprocessors()
        {
            return AssetPostprocessingInternal.GetSortedDynamicPostprocessorsForAsset(assetPath).ToArray();
        }

        internal static AssetPostprocessor.PostprocessorInfo[] GetStaticPostprocessors(Type importerType)
        {
            return AssetPostprocessingInternal.GetSortedStaticPostprocessorTypes(importerType).ToArray();
        }
    }
}
