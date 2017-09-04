// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct CachedAssetBundle
    {
        private string m_Name;
        private Hash128 m_Hash;

        public CachedAssetBundle(string name, Hash128 hash)
        {
            m_Name = name;
            m_Hash = hash;
        }

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public Hash128 hash
        {
            get { return m_Hash; }
            set { m_Hash = value; }
        }
    }

    [NativeHeader("Runtime/Misc/Cache.h")]
    [StaticAccessor("CacheWrapper", StaticAccessorType.DoubleColon)]
    public partial struct Cache
    {
#pragma warning disable 649
        private int m_Handle;
        internal int handle { get { return m_Handle; } }
#pragma warning restore 649

        public static bool operator==(Cache lhs, Cache rhs) { return lhs.handle == rhs.handle; }
        public static bool operator!=(Cache lhs, Cache rhs) { return lhs.handle != rhs.handle; }
        public override int GetHashCode() { return m_Handle; }
        public override bool Equals(object other)
        {
            if (!(other is Cache))
                return false;

            Cache rhs = (Cache)other;
            return handle == rhs.handle;
        }

        public bool valid { get { return Cache_IsValid(m_Handle); } }
        extern internal static bool Cache_IsValid(int handle);

        public bool ready { get { return Cache_IsReady(m_Handle); } }
        [NativeThrows]
        extern internal static bool Cache_IsReady(int handle);

        public bool readOnly { get { return Cache_IsReadonly(m_Handle); } }
        [NativeThrows]
        extern internal static bool Cache_IsReadonly(int handle);

        public string path { get { return Cache_GetPath(m_Handle); } }
        [NativeThrows]
        extern internal static string Cache_GetPath(int handle);

        public int index { get { return Cache_GetIndex(m_Handle); } }
        extern internal static int Cache_GetIndex(int handle);

        public long spaceFree { get { return Cache_GetSpaceFree(m_Handle); } }
        [NativeThrows]
        extern internal static long Cache_GetSpaceFree(int handle);

        public long maximumAvailableStorageSpace { get { return Cache_GetMaximumDiskSpaceAvailable(m_Handle); } set { Cache_SetMaximumDiskSpaceAvailable(m_Handle, value); } }
        [NativeThrows]
        extern internal static long Cache_GetMaximumDiskSpaceAvailable(int handle);
        [NativeThrows]
        extern internal static void Cache_SetMaximumDiskSpaceAvailable(int handle, long value);

        public long spaceOccupied { get { return Cache_GetCachingDiskSpaceUsed(m_Handle); } }
        [NativeThrows]
        extern internal static long Cache_GetCachingDiskSpaceUsed(int handle);

        public int expirationDelay { get { return Cache_GetExpirationDelay(m_Handle); } set { Cache_SetExpirationDelay(m_Handle, value); } }
        [NativeThrows]
        extern internal static int Cache_GetExpirationDelay(int handle);
        [NativeThrows]
        extern internal static void Cache_SetExpirationDelay(int handle, int value);

        public bool ClearCache() { return Cache_ClearCache(m_Handle); }
        [NativeThrows]
        extern internal static bool Cache_ClearCache(int handle);

        public bool ClearCache(int expiration) { return Cache_ClearCache_Expiration(m_Handle, expiration); }
        [NativeThrows]
        extern internal static bool Cache_ClearCache_Expiration(int handle, int expiration);
    }
}
