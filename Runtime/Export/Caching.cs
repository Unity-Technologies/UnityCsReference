// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    [UnityEngine.Scripting.UsedByNativeCodeAttribute]
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

    public partial struct Cache
    {
        private int m_Handle;

        internal int handle { get { return m_Handle; } }

        public bool valid { get { return IsValidInternal(m_Handle); } }
        public bool readOnly { get { return IsReadonlyInternal(m_Handle); } }
        public string path { get { return GetPathInternal(m_Handle); } }
        public int index { get { return GetIndexInternal(m_Handle); } }

        public long spaceFree { get { return GetSpaceFreeInternal(m_Handle); } }
        public long maximumAvailableStorageSpace { get { return GetMaximumDiskSpaceAvailableInternal(m_Handle); } set { SetMaximumDiskSpaceAvailableInternal(m_Handle, value); } }
        public long spaceOccupied { get { return GetCachingDiskSpaceUsedInternal(m_Handle); } }
        public int expirationDelay { get { return GetExpirationDelay(m_Handle); } set { SetExpirationDelay(m_Handle, value); } }
        public bool ready { get { return IsReadyInternal(m_Handle); } }

        public bool ClearCache() { return ClearCacheInternal(m_Handle); }

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
    }
}
