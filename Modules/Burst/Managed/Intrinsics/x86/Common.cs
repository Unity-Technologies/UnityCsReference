// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Burst.Intrinsics
{
    /// <summary>
    /// Static methods and properties for X86 instruction intrinsics.
    /// </summary>
    public unsafe static partial class X86
    {
        private static v128 GenericCSharpLoad(void* ptr)
        {
            return *(v128*)ptr;
        }

        private static void GenericCSharpStore(void* ptr, v128 val)
        {
            *(v128*)ptr = val;
        }

        private static sbyte Saturate_To_Int8(int val)
        {
            if (val > sbyte.MaxValue)
                return sbyte.MaxValue;
            else if (val < sbyte.MinValue)
                return sbyte.MinValue;
            return (sbyte)val;
        }

        private static byte Saturate_To_UnsignedInt8(int val)
        {
            if (val > byte.MaxValue)
                return byte.MaxValue;
            else if (val < byte.MinValue)
                return byte.MinValue;
            return (byte)val;
        }

        private static short Saturate_To_Int16(int val)
        {
            if (val > short.MaxValue)
                return short.MaxValue;
            else if (val < short.MinValue)
                return short.MinValue;
            return (short)val;
        }

        private static ushort Saturate_To_UnsignedInt16(int val)
        {
            if (val > ushort.MaxValue)
                return ushort.MaxValue;
            else if (val < ushort.MinValue)
                return ushort.MinValue;
            return (ushort)val;
        }

        private static bool IsNaN(uint v)
        {
            return (v & 0x7fffffffu) > 0x7f800000;
        }

        private static bool IsNaN(ulong v)
        {
            return (v & 0x7ffffffffffffffful) > 0x7ff0000000000000ul;
        }
    }
}
