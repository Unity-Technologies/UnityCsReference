// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnityEngine
{


[System.Obsolete("this API is not for public use.")]
[UsedByNativeCode]
    public struct CacheIndex
    {
        public string name;
        public int bytesUsed;
        public int expires;
    }


[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct Cache
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool IsValidInternal (int handle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool IsReadonlyInternal (int handle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string GetPathInternal (int handle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int GetIndexInternal (int handle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  long GetSpaceFreeInternal (int handle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  long GetMaximumDiskSpaceAvailableInternal (int handle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void SetMaximumDiskSpaceAvailableInternal (int handle, long value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  long GetCachingDiskSpaceUsedInternal (int handle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int GetExpirationDelay (int handle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void SetExpirationDelay (int handle, int expiration) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool IsReadyInternal (int handle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool ClearCacheInternal (int handle) ;

}

public sealed partial class Caching
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool ClearCache () ;

    [System.Obsolete ("This function is obsolete and will always return -1. Use IsVersionCached instead.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetVersionFromCache (string url) ;

    public static bool ClearCachedVersion (string assetBundleName, Hash128 hash) {
        return INTERNAL_CALL_ClearCachedVersion ( assetBundleName, ref hash );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_ClearCachedVersion (string assetBundleName, ref Hash128 hash);
    public static bool ClearOtherCachedVersions (string assetBundleName, Hash128 hash) {
        return INTERNAL_CALL_ClearOtherCachedVersions ( assetBundleName, ref hash );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_ClearOtherCachedVersions (string assetBundleName, ref Hash128 hash);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool ClearAllCachedVersions (string assetBundleName) ;

    public static void GetCachedVersions(string assetBundleName, List<Hash128> outCachedVersions)
        {
            if (String.IsNullOrEmpty(assetBundleName))
                throw new ArgumentException("Input AssetBundle name cannot be null or empty.");
            if (outCachedVersions == null)
                throw new ArgumentNullException("Input outCachedVersions cannot be null.");

            GetCachedVersionsInternal(assetBundleName, outCachedVersions);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Hash128[] GetCachedVersions (string assetBundleName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void GetCachedVersionsInternal (string assetBundleName, object cachedVersions) ;

    [System.Obsolete ("This function is obsolete. Please use IsVersionCached with Hash128 instead.")]
public static bool IsVersionCached(string url, int version)
        {
            Hash128 tempHash = new Hash128(0, 0, 0, (uint)version);
            return IsVersionCached(url, "", tempHash);
        }
    
    
    public static bool IsVersionCached(string url, Hash128 hash)
        {
            if (String.IsNullOrEmpty(url))
                throw new ArgumentException("Input AssetBundle url cannot be null or empty.");

            return IsVersionCached(url, "", hash);
        }
    
    
    public static bool IsVersionCached(CachedAssetBundle cachedBundle)
        {
            if (String.IsNullOrEmpty(cachedBundle.name))
                throw new ArgumentException("Input AssetBundle name cannot be null or empty.");

            return IsVersionCached("", cachedBundle.name, cachedBundle.hash);
        }
    
    
    internal static bool IsVersionCached (string url, string name, Hash128 hash) {
        return INTERNAL_CALL_IsVersionCached ( url, name, ref hash );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_IsVersionCached (string url, string name, ref Hash128 hash);
    [System.Obsolete ("This function is obsolete. Please use MarkAsUsed with Hash128 instead.")]
public static bool MarkAsUsed(string url, int version)
        {
            Hash128 tempHash = new Hash128(0, 0, 0, (uint)version);
            return MarkAsUsed(url, "", tempHash);
        }
    
    
    public static bool MarkAsUsed(string url, Hash128 hash)
        {
            if (String.IsNullOrEmpty(url))
                throw new ArgumentException("Input AssetBundle url cannot be null or empty.");

            return MarkAsUsed(url, "", hash);
        }
    
    
    public static bool MarkAsUsed(CachedAssetBundle cachedBundle)
        {
            if (String.IsNullOrEmpty(cachedBundle.name))
                throw new ArgumentException("Input AssetBundle name cannot be null or empty.");

            return MarkAsUsed("", cachedBundle.name, cachedBundle.hash);
        }
    
    
    internal static bool MarkAsUsed (string url, string name, Hash128 hash) {
        return INTERNAL_CALL_MarkAsUsed ( url, name, ref hash );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_MarkAsUsed (string url, string name, ref Hash128 hash);
    [System.Obsolete ("this API is not for public use.")]
    public extern static CacheIndex[] index
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("This property is only used for the current cache, use GetSpaceFree() with cache index to get unused bytes per cache.")]
    public extern static long spaceFree
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("This property is only used for the current cache, use Cache.maximumAvailableStorageSpace to get/set the maximum available storage space per cache.")]
    public extern static long maximumAvailableDiskSpace
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("This property is only used for the current cache, use Cache.spaceOccupied to get used bytes per cache.")]
    public extern static long spaceOccupied
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("Please use Cache.spaceAvailable to get unused bytes per cache.")]
    public extern static int spaceAvailable
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("Please use use Cache.spaceOccupied to get used bytes per cache")]
    public extern static int spaceUsed
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("This property is only used for the current cache, use Cache.expirationDelay to get/set the expiration delay per cache.")]
    public extern static int expirationDelay
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool compressionEnabled
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool ready
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    
    
    
    
    
    
    
    
    
    
    
    
    public static Cache AddCache(string cachePath)
        {
            if (String.IsNullOrEmpty(cachePath))
                throw new ArgumentNullException("Cache path cannot be null or empty.");

            var isReadonly = false;
            if (cachePath.Replace('\\', '/').StartsWith(Application.streamingAssetsPath))
            {
                isReadonly = true;
            }
            else
            {
                if (!Directory.Exists(cachePath))
                    throw new ArgumentException("Cache path '" + cachePath + "' doesn't exist.");

                if ((File.GetAttributes(cachePath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    isReadonly = true;
            }

            if (GetCacheByPath(cachePath).valid)
                throw new InvalidOperationException("Cache with path '" + cachePath + "' has already been added.");

            return AddCache_Internal(cachePath, isReadonly);
        }
    
    
    private static Cache AddCache_Internal (string cachePath, bool isReadonly) {
        Cache result;
        INTERNAL_CALL_AddCache_Internal ( cachePath, isReadonly, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_AddCache_Internal (string cachePath, bool isReadonly, out Cache value);
    public static Cache GetCacheAt (int cacheIndex) {
        Cache result;
        INTERNAL_CALL_GetCacheAt ( cacheIndex, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetCacheAt (int cacheIndex, out Cache value);
    public static Cache GetCacheByPath (string cachePath) {
        Cache result;
        INTERNAL_CALL_GetCacheByPath ( cachePath, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetCacheByPath (string cachePath, out Cache value);
    public static void GetAllCachePaths(List<string> cachePaths)
        {
            cachePaths.Clear();
            for (int i = 0; i < Caching.cacheCount; ++i)
            {
                var cache = GetCacheAt(i);
                cachePaths.Add(cache.path);
            }
        }
    
    
    public static bool RemoveCache (Cache cache) {
        return INTERNAL_CALL_RemoveCache ( ref cache );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_RemoveCache (ref Cache cache);
    public static void MoveCacheBefore (Cache src, Cache dst) {
        INTERNAL_CALL_MoveCacheBefore ( ref src, ref dst );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_MoveCacheBefore (ref Cache src, ref Cache dst);
    public static void MoveCacheAfter (Cache src, Cache dst) {
        INTERNAL_CALL_MoveCacheAfter ( ref src, ref dst );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_MoveCacheAfter (ref Cache src, ref Cache dst);
    public extern static int cacheCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public static Cache defaultCache
    {
        get { Cache tmp; INTERNAL_get_defaultCache(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_defaultCache (out Cache value) ;


    public static Cache currentCacheForWriting
    {
        get { Cache tmp; INTERNAL_get_currentCacheForWriting(out tmp); return tmp;  }
        set { INTERNAL_set_currentCacheForWriting(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_currentCacheForWriting (out Cache value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_set_currentCacheForWriting (ref Cache value) ;

}


}
