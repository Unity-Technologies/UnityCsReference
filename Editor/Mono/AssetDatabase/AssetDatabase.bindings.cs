// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    public enum ForceReserializeAssetsOptions
    {
        ReserializeAssets = 1 << 0,
        ReserializeMetadata = 1 << 1,
        ReserializeAssetsAndMetadata = ReserializeAssets | ReserializeMetadata
    }

    internal enum ImportPackageOptions
    {
        Default = 0,
        NoGUI = 1 << 0,
    }

    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabase.h")]
    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabaseUtility.h")]
    [NativeHeader("Editor/Src/PackageUtility.h")]
    public partial class AssetDatabase
    {
        [FreeFunction("AssetDatabase::ReSerializeAssetsForced")]
        extern private static void ReSerializeAssetsForced(GUID[] guids, ForceReserializeAssetsOptions options);

        public static void ForceReserializeAssets(IEnumerable<string> assetPaths, ForceReserializeAssetsOptions options = ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata)
        {
            if (EditorApplication.isPlaying)
                throw new Exception("AssetDatabase.ForceReserializeAssets cannot be used when in play mode");

            HashSet<GUID> guidList = new HashSet<GUID>();

            foreach (string path in assetPaths)
            {
                if (path == "")
                    continue;

                if (InternalEditorUtility.IsUnityExtensionRegistered(path))
                    continue;

                bool rootFolder, readOnly;
                bool validPath = GetAssetFolderInfo(path, out rootFolder, out readOnly);
                if (validPath && (rootFolder || readOnly))
                    continue;

                GUID guid = new GUID(AssetPathToGUID(path));

                if (!guid.Empty())
                {
                    guidList.Add(guid);
                }
                else
                {
                    if (File.Exists(path))
                    {
                        Debug.LogWarningFormat("Cannot reserialize file \"{0}\": the file is not in the AssetDatabase. Skipping.", path);
                    }
                    else
                    {
                        Debug.LogWarningFormat("Cannot reserialize file \"{0}\": the file does not exist. Skipping.", path);
                    }
                }
            }

            GUID[] guids = new GUID[guidList.Count];
            guidList.CopyTo(guids);
            ReSerializeAssetsForced(guids, options);
        }

        [FreeFunction("AssetDatabase::GetGUIDAndLocalIdentifierInFile")]
        extern private static bool GetGUIDAndLocalIdentifierInFile(int instanceID, out GUID outGuid, out long outLocalId);

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Please use the overload of this function that uses a long data type for the localId parameter, because this version can return a localID that has overflowed. This can happen when called on objects that are part of a Prefab.",  true)]
        public static bool TryGetGUIDAndLocalFileIdentifier(UnityEngine.Object obj, out string guid, out int localId)
        {
            return TryGetGUIDAndLocalFileIdentifier(obj.GetInstanceID(), out guid, out localId);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Please use the overload of this function that uses a long data type for the localId parameter, because this version can return a localID that has overflowed. This can happen when called on objects that are part of a Prefab.",  true)]
        public static bool TryGetGUIDAndLocalFileIdentifier(int instanceID, out string guid, out int localId)
        {
            throw new NotSupportedException("Use the overload of this function that uses a long data type for the localId parameter, because this version can return a localID that has overflowed. This can happen when called on objects that are part of a Prefab.");
        }

        public static bool TryGetGUIDAndLocalFileIdentifier(UnityEngine.Object obj, out string guid, out long localId)
        {
            return TryGetGUIDAndLocalFileIdentifier(obj.GetInstanceID(), out guid, out localId);
        }

        public static bool TryGetGUIDAndLocalFileIdentifier(int instanceID, out string guid, out long localId)
        {
            GUID uguid;
            bool res = GetGUIDAndLocalIdentifierInFile(instanceID, out uguid, out localId);
            guid = uguid.ToString();
            return res;
        }

        public static bool TryGetGUIDAndLocalFileIdentifier<T>(LazyLoadReference<T> assetRef, out string guid, out long localId) where T : UnityEngine.Object
        {
            GUID uguid;
            bool res = GetGUIDAndLocalIdentifierInFile(assetRef.instanceID, out uguid, out localId);
            guid = uguid.ToString();
            return res;
        }

        public static void ForceReserializeAssets()
        {
            ForceReserializeAssets(GetAllAssetPaths());
        }

        [FreeFunction("AssetDatabase::RemoveObjectFromAsset")]
        extern public static void RemoveObjectFromAsset([NotNull] UnityEngine.Object objectToRemove);

        [FreeFunction("AssetDatabase::GUIDFromExistingAssetPath")]
        extern internal static GUID GUIDFromExistingAssetPath(string path);

        [FreeFunction("::ImportPackage")]
        extern private static bool ImportPackage(string packagePath, ImportPackageOptions options);
        //TODO: This API should be Obsoleted when there is time available to update all the uses of it in Package Manager packages
        public static void ImportPackage(string packagePath, bool interactive)
        {
            ImportPackage(packagePath, interactive ? ImportPackageOptions.Default : ImportPackageOptions.NoGUI);
        }

        internal static bool ImportPackageImmediately(string packagePath)
        {
            return ImportPackage(packagePath, ImportPackageOptions.NoGUI);
        }
    }
}

namespace UnityEditor.Experimental
{
    public partial class AssetDatabaseExperimental
    {
        [FreeFunction("AssetDatabase::ClearImporterOverride")]
        extern public static void ClearImporterOverride(string path);

        public static void SetImporterOverride<T>(string path)
            where T : Experimental.AssetImporters.ScriptedImporter
        {
            SetImporterOverrideInternal(path, typeof(T));
        }

        [FreeFunction("AssetDatabase::SetImporterOverride")]
        extern internal static void SetImporterOverrideInternal(string path, System.Type importer);

        [FreeFunction("AssetDatabase::GetImporterOverride")]
        extern public static System.Type GetImporterOverride(string path);
    }
}
