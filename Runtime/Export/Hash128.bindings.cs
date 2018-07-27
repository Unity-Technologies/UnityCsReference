// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
#pragma warning disable 414
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Utilities/Hash128.h")]
    public unsafe partial struct Hash128 : IComparable, IComparable<Hash128>, IEquatable<Hash128>
    {
        public Hash128(uint u32_0, uint u32_1, uint u32_2, uint u32_3)
        {
            m_u32_0 = u32_0;
            m_u32_1 = u32_1;
            m_u32_2 = u32_2;
            m_u32_3 = u32_3;
        }

        public Hash128(ulong u64_0, ulong u64_1)
        {
            var ptr0 = (uint*)&u64_0;
            var ptr1 = (uint*)&u64_1;

            m_u32_0 = *ptr0;
            m_u32_1 = *(ptr0 + 1);
            m_u32_2 = *ptr1;
            m_u32_3 = *(ptr1 + 1);
        }

        uint m_u32_0;
        uint m_u32_1;
        uint m_u32_2;
        uint m_u32_3;

        internal ulong u64_0
        {
            get
            {
                fixed(uint* ptr0 = &m_u32_0)
                return *(ulong*)ptr0;
            }
        }

        internal ulong u64_1
        {
            get
            {
                fixed(uint* ptr1 = &m_u32_1)
                return *(ulong*)ptr1;
            }
        }

        public bool isValid
        {
            get
            {
                return m_u32_0 != 0
                    || m_u32_1 != 0
                    || m_u32_2 != 0
                    || m_u32_3 != 0;
            }
        }

        public int CompareTo(Hash128 rhs)
        {
            if (this < rhs)
                return -1;
            if (this > rhs)
                return 1;
            return 0;
        }

        public override string ToString()
        {
            return Internal_Hash128ToString(this);
        }

        [FreeFunction("StringToHash128")]
        public static extern Hash128 Parse(string hashString);

        [FreeFunction("Hash128ToString")]
        internal static extern string Internal_Hash128ToString(Hash128 hash128);

        [FreeFunction("ComputeHash128FromString")]
        public static extern Hash128 Compute(string hashString);

        public override bool Equals(object obj)
        {
            return obj is Hash128 && this == (Hash128)obj;
        }

        public bool Equals(Hash128 obj)
        {
            return this == obj;
        }

        public override int GetHashCode()
        {
            return m_u32_0.GetHashCode() ^ m_u32_1.GetHashCode() ^ m_u32_2.GetHashCode() ^ m_u32_3.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            if (obj == null || !(obj is Hash128))
                return 1;

            Hash128 rhs = (Hash128)obj;
            return this.CompareTo(rhs);
        }

        public static bool operator==(Hash128 hash1, Hash128 hash2)
        {
            return (hash1.m_u32_0 == hash2.m_u32_0 && hash1.m_u32_1 == hash2.m_u32_1 && hash1.m_u32_2 == hash2.m_u32_2 && hash1.m_u32_3 == hash2.m_u32_3);
        }

        public static bool operator!=(Hash128 hash1, Hash128 hash2)
        {
            return !(hash1 == hash2);
        }

        public static bool operator<(Hash128 x, Hash128 y)
        {
            if (x.m_u32_0 != y.m_u32_0)
                return x.m_u32_0 < y.m_u32_0;
            if (x.m_u32_1 != y.m_u32_1)
                return x.m_u32_1 < y.m_u32_1;
            if (x.m_u32_2 != y.m_u32_2)
                return x.m_u32_2 < y.m_u32_2;
            return x.m_u32_3 < y.m_u32_3;
        }

        public static bool operator>(Hash128 x, Hash128 y)
        {
            if (x < y)
                return false;
            if (x == y)
                return false;
            return true;
        }
    }
}
