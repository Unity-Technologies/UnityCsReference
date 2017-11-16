// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
#pragma warning disable 414
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Utilities/Hash128.h")]
    public partial struct Hash128
    {
        public Hash128(uint u32_0, uint u32_1, uint u32_2, uint u32_3)
        {
            m_u32_0 = u32_0;
            m_u32_1 = u32_1;
            m_u32_2 = u32_2;
            m_u32_3 = u32_3;
        }

        uint m_u32_0;
        uint m_u32_1;
        uint m_u32_2;
        uint m_u32_3;


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


        public override string ToString()
        {
            return Internal_Hash128ToString(this);
        }

        [FreeFunction("StringToHash128")]
        public static extern Hash128 Parse(string hashString);

        [FreeFunction("Hash128ToString")]
        internal static extern string Internal_Hash128ToString(Hash128 hash128);

        public override bool Equals(object obj)
        {
            return obj is Hash128 && this == (Hash128)obj;
        }

        public override int GetHashCode()
        {
            return m_u32_0.GetHashCode() ^ m_u32_1.GetHashCode() ^ m_u32_2.GetHashCode() ^ m_u32_3.GetHashCode();
        }

        public static bool operator==(Hash128 hash1, Hash128 hash2)
        {
            return (hash1.m_u32_0 == hash2.m_u32_0 && hash1.m_u32_1 == hash2.m_u32_1 && hash1.m_u32_2 == hash2.m_u32_2 && hash1.m_u32_3 == hash2.m_u32_3);
        }

        public static bool operator!=(Hash128 hash1, Hash128 hash2)
        {
            return !(hash1 == hash2);
        }
    }
}
