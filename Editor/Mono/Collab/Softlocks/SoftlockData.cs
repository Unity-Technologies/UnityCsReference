// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.Utils;
using UnityEditor.Web;

namespace UnityEditor.Collaboration
{
    // Access and query the current set of softlock data.
    internal static class SoftLockData
    {
        internal delegate void OnSoftlockUpdate(string[] assetGUIDs);
        internal static OnSoftlockUpdate SoftlockSubscriber = null;

        // Invoked from C++
        public static void SetSoftlockChanges(string[] assetGUIDs)
        {
            if (null != SoftlockSubscriber)
            {
                SoftlockSubscriber(assetGUIDs);
            }
        }

        // Returns whether the given object has soft lock support
        // (i.e. is tracked in the back-end for simultaneous changes).
        // Note: Scene is supported, but isn't a Unity Object.
        public static bool AllowsSoftLocks(UnityEngine.Object unityObject)
        {
            if (unityObject == null)
            {
                throw new ArgumentNullException("unityObject");
            }

            bool supportsSoftLocks = false;
            if (unityObject.GetType().Equals(typeof(SceneAsset)))
            {
                supportsSoftLocks = true;
            }
            else
            {
                supportsSoftLocks = IsPrefab(unityObject);
            }
            return supportsSoftLocks;
        }

        public static bool IsPrefab(UnityEngine.Object unityObject)
        {
            PrefabType prefabType = PrefabUtility.GetPrefabType(unityObject);
            bool isPrefab = (prefabType == PrefabType.PrefabInstance || prefabType == PrefabType.Prefab);
            return isPrefab;
        }

        public static bool IsPrefab(string assetGUID)
        {
            bool isPrefab = false;
            UnityEngine.Object unityObject;
            if (AssetAccess.TryGetAssetFromGUID(assetGUID, out unityObject))
            {
                isPrefab = IsPrefab(unityObject);
            }
            return isPrefab;
        }

        private static bool TryHasSoftLocks(Scene scene, out bool hasSoftLocks)
        {
            string assetGUID = AssetDatabase.AssetPathToGUID(scene.path);
            bool success = TryHasSoftLocks(assetGUID, out hasSoftLocks);
            return success;
        }

        // Soft locks are present when collab is enabled and other users are
        // editing the given object.
        // Failure: assigns false to 'hasSoftLocks', returns false.
        // Success: assigns true or false to 'hasSoftLocks', returns true.
        public static bool TryHasSoftLocks(UnityEngine.Object objectWithGUID, out bool hasSoftLocks)
        {
            string assetGuid = null;
            AssetAccess.TryGetAssetGUIDFromObject(objectWithGUID, out assetGuid);
            bool success = TryHasSoftLocks(assetGuid, out hasSoftLocks);
            return success;
        }

        public static bool TryHasSoftLocks(string assetGuid, out bool hasSoftLocks)
        {
            hasSoftLocks = false;
            bool success = false;
            int count = 0;

            if (TryGetSoftlockCount(assetGuid, out count))
            {
                success = true;
                hasSoftLocks = (count > 0);
            }
            return success;
        }

        // Provides the number of additional users editing the given scene.
        // Failure: assigns 0 to count, return false.
        // Success: assigns a value in [0, n] to count, returns true.
        public static bool TryGetSoftlockCount(Scene scene, out int count)
        {
            bool success = false;
            count = 0;

            if (!scene.IsValid())
            {
                return false;
            }
            string assetGUID = AssetDatabase.AssetPathToGUID(scene.path);
            success = TryGetSoftlockCount(assetGUID, out count);
            return success;
        }

        // Provides the number of additional users editing the given object.
        // Failure: assigns 0 to count, return false.
        // Success: assigns a value in [0, n] to count, returns true.
        public static bool TryGetSoftlockCount(UnityEngine.Object objectWithGUID, out int count)
        {
            string assetGUID = null;
            AssetAccess.TryGetAssetGUIDFromObject(objectWithGUID, out assetGUID);
            bool success = TryGetSoftlockCount(assetGUID, out count);
            return success;
        }

        // Provides the number of additional users editing the given 'assetGUID'.
        // Failure: assigns 0 to count, return false.
        // Success: assigns a value in [0, n] to count, returns true.
        public static bool TryGetSoftlockCount(string assetGuid, out int count)
        {
            bool success = false;
            count = 0;
            List<SoftLock> softLocks = null;

            if (TryGetLocksOnAssetGUID(assetGuid, out softLocks))
            {
                count = softLocks.Count;
                success = true;
            }
            return success;
        }

        private static bool TryGetLocksOnObject(UnityEngine.Object objectWithGUID, out List<SoftLock> softLocks)
        {
            bool success = false;
            string assetGUID = null;

            if (AssetAccess.TryGetAssetGUIDFromObject(objectWithGUID, out assetGUID))
            {
                success = TryGetLocksOnAssetGUID(assetGUID, out softLocks);
            }
            else
            {
                softLocks = new List<SoftLock>();
            }
            return success;
        }

        // Provides a list of 'SoftLock' items, representing
        // the additional users editing the given assetGUID.
        // Failure: assigns empty list to 'softLocks', return false.
        // Success: assigns the retrieved list to 'softLocks', return true. (May be empty).
        public static bool TryGetLocksOnAssetGUID(string assetGuid, out List<SoftLock> softLocks)
        {
            if (assetGuid == null)
            {
                throw new ArgumentNullException("assetGuid");
            }

            if (!Collab.instance.IsCollabEnabledForCurrentProject() || assetGuid.Length == 0)
            {
                softLocks = new List<SoftLock>();
                return false;
            }

            SoftLock[] _softlocks = Collab.instance.GetSoftLocks(assetGuid);
            softLocks = new List<SoftLock>();

            for (int index = 0; index < _softlocks.Length; index++)
            {
                softLocks.Add(_softlocks[index]);
            }
            return true;
        }
    }
}

