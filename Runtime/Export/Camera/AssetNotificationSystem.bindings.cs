// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace UnityEngine.Internal
{
    [NativeHeader("Runtime/Camera/AssetNotificationSystemScripting.h")]
    internal static class AssetNotificationSystem
    {
        // Match AssetNotificationSystem.h Type
        internal enum AssetType {
            Mesh = 0,
            Material = 1,
            NumTypes
        }

        /// <summary>
        /// Register a subscriber index for receiving asset change/deletion notifications.
        /// </summary>
        public static extern int AddSubscriber();

        /// <summary>
        /// Remove a subscriber index for receiving asset change/deletion notifications.
        /// </summary>
        public static extern void RemoveSubscriber(int subscriberId);

        /// <summary>
        /// Clear all change notifications for the given subscriber ID.
        /// </summary>
        public static extern void ClearChanges(int subscriberId);

        /// <summary>
        /// Count the number of outstanding change/deletion notifications for this asset type.
        ///
        /// Asset changes/deletions will only be reported if the specific asset has been subscribed
        /// for notifications using SubscribeAsset.
        /// </summary>
        public static extern void CountChanges(int subscriberId, AssetType type, out int changedCount, out int deletedCount);

        /// <summary>
        /// Receive pending change notifications for the given subscriber ID and the asset type.
        /// The caller should call CountChanges first to get the length necessary of each array.
        /// If an array that is too small is passed in, the function will return false.
        ///
        /// After receiving changes, the caller should call ClearChanges to reset the notifications.
        ///
        /// Asset changes/deletions will only be reported if the specific asset has been subscribed
        /// for notifications using SubscribeAsset.
        /// </summary>
        public static bool GetChanges(int subscriberId, AssetType type,
            NativeArray<int> changedAssetInstanceIds, NativeArray<int> deletedAssetInstanceIds)
        {
            unsafe {
                return GetChangesForScripting(subscriberId, type,
                    (int*) NativeArrayUnsafeUtility.GetUnsafePtr(changedAssetInstanceIds), changedAssetInstanceIds.Length,
                    (int*) NativeArrayUnsafeUtility.GetUnsafePtr(deletedAssetInstanceIds), deletedAssetInstanceIds.Length);
            }
        }

        private static extern unsafe bool GetChangesForScripting(int subscriberID, AssetType type,
            int* changed, int changedLength, int* deleted, int deletedLength);

    }
}
