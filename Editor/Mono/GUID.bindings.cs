// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [Serializable]
    [RequiredByNativeCode]
    [NativeHeader("Runtime/Utilities/GUID.h")]
    public partial struct GUID : IComparable, IComparable<GUID>
    {
        private uint m_Value0, m_Value1, m_Value2, m_Value3;

        public GUID(string hexRepresentation)
        {
            m_Value0 = 0;
            m_Value1 = 0;
            m_Value2 = 0;
            m_Value3 = 0;
            TryParse(hexRepresentation, out this);
        }

        public static bool operator==(GUID x, GUID y)
        {
            return x.m_Value0 == y.m_Value0 && x.m_Value1 == y.m_Value1 && x.m_Value2 == y.m_Value2 && x.m_Value3 == y.m_Value3;
        }

        public static bool operator!=(GUID x, GUID y)
        {
            return !(x == y);
        }

        public static bool operator<(GUID x, GUID y)
        {
            if (x.m_Value0 != y.m_Value0)
                return x.m_Value0 < y.m_Value0;
            if (x.m_Value1 != y.m_Value1)
                return x.m_Value1 < y.m_Value1;
            if (x.m_Value2 != y.m_Value2)
                return x.m_Value2 < y.m_Value2;
            return x.m_Value3 < y.m_Value3;
        }

        public static bool operator>(GUID x, GUID y)
        {
            if (x < y)
                return false;
            if (x == y)
                return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is GUID))
                return false;
            GUID rhs = (GUID)obj;
            return rhs == this;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)m_Value0;
                hashCode = (hashCode * 397) ^ (int)m_Value1;
                hashCode = (hashCode * 397) ^ (int)m_Value2;
                hashCode = (hashCode * 397) ^ (int)m_Value3;
                return hashCode;
            }
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;

            GUID rhs = (GUID)obj;
            return this.CompareTo(rhs);
        }

        public int CompareTo(GUID rhs)
        {
            if (this < rhs)
                return -1;
            if (this > rhs)
                return 1;
            return 0;
        }

        public bool Empty()
        {
            return m_Value0 == 0 && m_Value1 == 0 && m_Value2 == 0 && m_Value3 == 0;
        }

        [Obsolete("Use TryParse instead")]
        public bool ParseExact(string hex)
        {
            return TryParse(hex, out this);
        }

        public static bool TryParse(string hex, out GUID result)
        {
            result = HexToGUIDInternal(hex);
            return !result.Empty();
        }

        public static GUID Generate()
        {
            return GenerateGUIDInternal();
        }

        public override string ToString()
        {
            return GUIDToHexInternal(ref this);
        }

        [NativeMethod(Name = "GUIDToString", IsFreeFunction = true)]
        extern private static string GUIDToHexInternal(ref GUID value);

        [NativeMethod(Name = "StringToGUID", IsFreeFunction = true)]
        extern private static GUID HexToGUIDInternal(string hex);

        [NativeMethod(Name = "GenerateGUID", IsFreeFunction = true)]
        extern private static GUID GenerateGUIDInternal();
    }
}
