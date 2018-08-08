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

    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabaseUtility.h")]
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

        public static void ForceReserializeAssets()
        {
            ForceReserializeAssets(GetAllAssetPaths());
        }

        [FreeFunction("AssetDatabase::RemoveObjectFromAsset")]
        extern public static void RemoveObjectFromAsset([NotNull] UnityEngine.Object objectToRemove);
    }
}
