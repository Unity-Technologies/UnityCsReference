// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [Obsolete("This struct is not for public use.")]
    [UsedByNativeCode]
    public struct CacheIndex
    {
        public string name;
        public int bytesUsed;
        public int expires;
    }

    [NativeHeader("Runtime/Misc/CachingManager.h")]
    [StaticAccessor("GetCachingManager()", StaticAccessorType.Dot)]
    public sealed partial class Caching
    {
        // Is compression enabled?
        extern public static bool compressionEnabled { get; set; }

        extern public static bool ready
        {
            [NativeName("GetIsReady")]
            get;
        }

        // Delete all AssetBundle content that has been cached by the current application.
        extern public static bool ClearCache();

        public static bool ClearCache(int expiration)
        {
            return ClearCache_Int(expiration);
        }

        [NativeName("ClearCache")]
        extern internal static bool ClearCache_Int(int expiration);

        // Clean the given version of the AssetBundle.
        public static bool ClearCachedVersion(string assetBundleName, Hash128 hash)
        {
            if (string.IsNullOrEmpty(assetBundleName))
                throw new ArgumentException("Input AssetBundle name cannot be null or empty.");

            return ClearCachedVersionInternal(assetBundleName, hash);
        }

        [NativeName("ClearCachedVersion")]
        extern internal static bool ClearCachedVersionInternal(string assetBundleName, Hash128 hash);

        // Clean all the versions other than the given AssetBundle.
        public static bool ClearOtherCachedVersions(string assetBundleName, Hash128 hash)
        {
            if (string.IsNullOrEmpty(assetBundleName))
                throw new ArgumentException("Input AssetBundle name cannot be null or empty.");

            return ClearCachedVersions(assetBundleName, hash, true);
        }

        // Clean all the versions of the given AssetBundle.
        public static bool ClearAllCachedVersions(string assetBundleName)
        {
            if (string.IsNullOrEmpty(assetBundleName))
                throw new ArgumentException("Input AssetBundle name cannot be null or empty.");

            return ClearCachedVersions(assetBundleName, new Hash128(), false);
        }

        extern internal static bool ClearCachedVersions(string assetBundleName, Hash128 hash, bool keepInputVersion);

        public static void GetCachedVersions(string assetBundleName, List<Hash128> outCachedVersions)
        {
            if (string.IsNullOrEmpty(assetBundleName))
                throw new ArgumentException("Input AssetBundle name cannot be null or empty.");
            if (outCachedVersions == null)
                throw new ArgumentNullException("Input outCachedVersions cannot be null.");

            GetCachedVersionsInternal(assetBundleName, outCachedVersions);
        }

        // Checks if an AssetBundle is cached.
        [Obsolete("Please use IsVersionCached with Hash128 instead.")]
        public static bool IsVersionCached(string url, int version)
        {
            return IsVersionCached(url, new Hash128(0, 0, 0, (uint)version));
        }

        public static bool IsVersionCached(string url, Hash128 hash)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("Input AssetBundle url cannot be null or empty.");

            return IsVersionCached(url, "", hash);
        }

        // Checks if an AssetBundle is cached.
        public static bool IsVersionCached(CachedAssetBundle cachedBundle)
        {
            if (string.IsNullOrEmpty(cachedBundle.name))
                throw new ArgumentException("Input AssetBundle name cannot be null or empty.");

            return IsVersionCached("", cachedBundle.name, cachedBundle.hash);
        }

        [NativeName("IsCached")]
        extern internal static bool IsVersionCached(string url, string assetBundleName, Hash128 hash);

        // Bumps the timestamp of a cached file to be the current time.
        [Obsolete("Please use MarkAsUsed with Hash128 instead.")]
        public static bool MarkAsUsed(string url, int version)
        {
            return MarkAsUsed(url, new Hash128(0, 0, 0, (uint)version));
        }

        // Bumps the timestamp of a cached file to be the current time.
        public static bool MarkAsUsed(string url, Hash128 hash)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("Input AssetBundle url cannot be null or empty.");

            return MarkAsUsed(url, "", hash);
        }

        // Bumps the timestamp of a cached file to be the current time.
        public static bool MarkAsUsed(CachedAssetBundle cachedBundle)
        {
            if (string.IsNullOrEmpty(cachedBundle.name))
                throw new ArgumentException("Input AssetBundle name cannot be null or empty.");

            return MarkAsUsed("", cachedBundle.name, cachedBundle.hash);
        }

        extern internal static bool MarkAsUsed(string url, string assetBundleName, Hash128 hash);

        [Obsolete("Please use SetNoBackupFlag with Hash128 instead.")]
        public static void SetNoBackupFlag(string url, int version)
        {
        }

        public static void SetNoBackupFlag(string url, Hash128 hash)
        {
        }

        public static void SetNoBackupFlag(CachedAssetBundle cachedBundle)
        {
        }

        [Obsolete("Please use ResetNoBackupFlag with Hash128 instead.")]
        public static void ResetNoBackupFlag(string url, int version)
        {
        }

        public static void ResetNoBackupFlag(string url, Hash128 hash)
        {
        }

        public static void ResetNoBackupFlag(CachedAssetBundle cachedBundle)
        {
        }

        [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
        extern internal static void SetNoBackupFlag(string url, string assetBundleName, Hash128 hash, bool enabled);

        [Obsolete("This function is obsolete and will always return -1. Use IsVersionCached instead.")]
        public static int GetVersionFromCache(string url) { return -1; }

        [Obsolete("Please use use Cache.spaceOccupied to get used bytes per cache.")]
        public static int spaceUsed { get { return (int)Caching.spaceOccupied; } }

        [Obsolete("This property is only used for the current cache, use Cache.spaceOccupied to get used bytes per cache.")]
        extern public static long spaceOccupied
        {
            [StaticAccessor("GetCachingManager().GetCurrentCache()", StaticAccessorType.Dot)]
            [NativeName("GetCachingDiskSpaceUsed")]
            get;
        }

        [Obsolete("Please use use Cache.spaceOccupied to get used bytes per cache.")]
        public static int spaceAvailable { get { return (int)Caching.spaceFree; } }

        [Obsolete("This property is only used for the current cache, use Cache.spaceFree to get unused bytes per cache.")]
        extern public static long spaceFree
        {
            [StaticAccessor("GetCachingManager().GetCurrentCache()", StaticAccessorType.Dot)]
            [NativeName("GetCachingDiskSpaceFree")]
            get;
        }

        [Obsolete("This property is only used for the current cache, use Cache.maximumAvailableStorageSpace to access the maximum available storage space per cache.")]
        [StaticAccessor("GetCachingManager().GetCurrentCache()", StaticAccessorType.Dot)]
        extern public static long maximumAvailableDiskSpace
        {
            [NativeName("GetMaximumDiskSpaceAvailable")]
            get;
            [NativeName("SetMaximumDiskSpaceAvailable")]
            set;
        }

        [Obsolete("This property is only used for the current cache, use Cache.expirationDelay to access the expiration delay per cache.")]
        [StaticAccessor("GetCachingManager().GetCurrentCache()", StaticAccessorType.Dot)]
        extern public static int expirationDelay { get; set; }

        public static Cache AddCache(string cachePath)
        {
            if (string.IsNullOrEmpty(cachePath))
                throw new ArgumentNullException("Cache path cannot be null or empty.");

            var isReadonly = false;
            if (cachePath.Replace('\\', '/').StartsWith(Application.streamingAssetsPath))
            {
                // Set to readonly if the input cache path is under StreamingAssetsFolder.
                isReadonly = true;
            }
            else
            {
                if (!Directory.Exists(cachePath))
                    throw new ArgumentException("Cache path '" + cachePath + "' doesn't exist.");

                // Set to readonly if the input cache path is readonly.
                if ((File.GetAttributes(cachePath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    isReadonly = true;
            }

            // Don't add again.
            if (GetCacheByPath(cachePath).valid)
                throw new InvalidOperationException("Cache with path '" + cachePath + "' has already been added.");

            return AddCache(cachePath, isReadonly);
        }

        [NativeName("AddCachePath")]
        extern internal static Cache AddCache(string cachePath, bool isReadonly);

        [StaticAccessor("CachingManagerWrapper", StaticAccessorType.DoubleColon)]
        [NativeName("Caching_GetCacheHandleAt")]
        [NativeThrows]
        extern public static Cache GetCacheAt(int cacheIndex);

        [StaticAccessor("CachingManagerWrapper", StaticAccessorType.DoubleColon)]
        [NativeName("Caching_GetCacheHandleByPath")]
        [NativeThrows]
        extern public static Cache GetCacheByPath(string cachePath);

        public static void GetAllCachePaths(List<string> cachePaths)
        {
            cachePaths.Clear();
            for (int i = 0; i < Caching.cacheCount; ++i)
            {
                var cache = GetCacheAt(i);
                cachePaths.Add(cache.path);
            }
        }

        [StaticAccessor("CachingManagerWrapper", StaticAccessorType.DoubleColon)]
        [NativeName("Caching_RemoveCacheByHandle")]
        [NativeThrows]
        extern public static bool RemoveCache(Cache cache);

        [StaticAccessor("CachingManagerWrapper", StaticAccessorType.DoubleColon)]
        [NativeName("Caching_MoveCacheBeforeByHandle")]
        [NativeThrows]
        extern public static void MoveCacheBefore(Cache src, Cache dst);

        [StaticAccessor("CachingManagerWrapper", StaticAccessorType.DoubleColon)]
        [NativeName("Caching_MoveCacheAfterByHandle")]
        [NativeThrows]
        extern public static void MoveCacheAfter(Cache src, Cache dst);

        extern public static int cacheCount { get; }

        [StaticAccessor("CachingManagerWrapper", StaticAccessorType.DoubleColon)]
        extern public static Cache defaultCache
        {
            [NativeName("Caching_GetDefaultCacheHandle")]
            get;
        }

        [StaticAccessor("CachingManagerWrapper", StaticAccessorType.DoubleColon)]
        extern public static Cache currentCacheForWriting
        {
            [NativeName("Caching_GetCurrentCacheHandle")]
            get;
            [NativeName("Caching_SetCurrentCacheByHandle")]
            [NativeThrows]
            set;
        }
    }
}
