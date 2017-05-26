// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Collaboration
{
    internal static class AssetAccess
    {
        // Attempts to retrieve the Asset GUID within the Unity Object.
        // Failure: returns false and the given 'assetGUID' is set to empty string.
        // Success: returns true and updates 'assetGUID'.
        public static bool TryGetAssetGUIDFromObject(UnityEngine.Object objectWithGUID, out string assetGUID)
        {
            if (objectWithGUID == null)
            {
                throw new ArgumentNullException("objectWithGuid");
            }

            bool success = false;

            if (objectWithGUID.GetType() == typeof(SceneAsset))
            {
                success = TryGetAssetGUIDFromDatabase(objectWithGUID, out assetGUID);
            }
            else if (objectWithGUID.GetType() == typeof(GameObject))
            {
                success = TryGetPrefabGUID(objectWithGUID, out assetGUID);
            }
            else
            {
                assetGUID = String.Empty;
            }
            return success;
        }

        // Attempts to retrieve the Asset corresponding to the given GUID.
        // Failure: returns false and the given 'asset' is set to null.
        // Success: returns true and updates 'asset'.
        public static bool TryGetAssetFromGUID(string assetGUID, out UnityEngine.Object asset)
        {
            if (assetGUID == null)
            {
                throw new ArgumentNullException("assetGUID");
            }

            bool success = false;
            string objectPath = AssetDatabase.GUIDToAssetPath(assetGUID);

            if (objectPath == null)
            {
                asset = null;
            }
            else
            {
                asset = AssetDatabase.LoadMainAssetAtPath(objectPath);
                success = (asset != null);
            }
            return success;
        }

        // Expects the given 'gameObject' to have a 'PrefabType' which
        // is either an 'instance' or straight prefab.
        // Failure: assigns empty string to 'assetGUID', returns false.
        // Success: assigns the retrieval of the GUID to 'assetGUID' and returns true.
        private static bool TryGetPrefabGUID(UnityEngine.Object gameObject, out string assetGUID)
        {
            PrefabType prefabType = PrefabUtility.GetPrefabType(gameObject);
            UnityEngine.Object prefabObject = null;

            if (prefabType == PrefabType.PrefabInstance)
            {
                prefabObject = PrefabUtility.GetPrefabParent(gameObject);
            }
            else if (prefabType == PrefabType.Prefab)
            {
                prefabObject = gameObject;
            }

            bool success = false;

            if (prefabObject == null)
            {
                assetGUID = String.Empty;
            }
            else
            {
                success = TryGetAssetGUIDFromDatabase(prefabObject, out assetGUID);
            }
            return success;
        }

        // Interacts with 'AssetDatabase' to retrieve 'objectWithGUID' path
        // and in-turn uses this to access the GUID.
        // Failure: assigns an empty string to 'assetGUID', returns false.
        // Success: assigns the GUID result from 'AssetDatabase' to 'assetGUID' and returns true.
        private static bool TryGetAssetGUIDFromDatabase(UnityEngine.Object objectWithGUID, out string assetGUID)
        {
            if (objectWithGUID == null)
            {
                throw new ArgumentNullException("objectWithGuid");
            }

            string _assetGUID = null;
            string objectPath = AssetDatabase.GetAssetPath(objectWithGUID);

            if (!String.IsNullOrEmpty(objectPath))
            {
                _assetGUID = AssetDatabase.AssetPathToGUID(objectPath);
            }

            bool success = false;
            if (String.IsNullOrEmpty(_assetGUID))
            {
                assetGUID = String.Empty;
            }
            else
            {
                assetGUID = _assetGUID;
                success = true;
            }
            return success;
        }
    }
}
