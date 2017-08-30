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

                if (path.Equals("Assets") || path.Equals("Packages"))
                    continue;

                if (AssetDatabase.IsPackagedAssetPath(path))
                    continue;

                if (InternalEditorUtility.IsUnityExtensionRegistered(path))
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

        public static void ForceReserializeAssets()
        {
            ForceReserializeAssets(GetAllAssetPaths());
        }
    }
}
