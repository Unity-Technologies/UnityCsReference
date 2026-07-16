// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unity.Burst.Intrinsics
{
    /// <summary>
    /// Represents a 64-bit SIMD value (Arm only)
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    [DebuggerTypeProxy(typeof(V64DebugView))]
    public struct v64
    {
        /// <summary>
        /// Get the 0th Byte of the vector
        /// </summary>
        [FieldOffset(0)] public byte Byte0;
        /// <summary>
        /// Get the 1st Byte of the vector
        /// </summary>
        [FieldOffset(1)] public byte Byte1;
        /// <summary>
        /// Get the 2nd Byte of the vector
        /// </summary>
        [FieldOffset(2)] public byte Byte2;
        /// <summary>
        /// Get the 3rd Byte of the vector
        /// </summary>
        [FieldOffset(3)] public byte Byte3;
        /// <summary>
        /// Get the 4th Byte of the vector
        /// </summary>
        [FieldOffset(4)] public byte Byte4;
        /// <summary>
        /// Get the 5th Byte of the vector
        /// </summary>
        [FieldOffset(5)] public byte Byte5;
        /// <summary>
        /// Get the 6th Byte of the vector
        /// </summary>
        [FieldOffset(6)] public byte Byte6;
        /// <summary>
        /// Get the 7th Byte of the vector
        /// </summary>
        [FieldOffset(7)] public byte Byte7;

        /// <summary>
        /// Get the 0th SByte of the vector
        /// </summary>
        [FieldOffset(0)] public sbyte SByte0;
        /// <summary>
        /// Get the 1st SByte of the vector
        /// </summary>
        [FieldOffset(1)] public sbyte SByte1;
        /// <summary>
        /// Get the 2nd SByte of the vector
        /// </summary>
        [FieldOffset(2)] public sbyte SByte2;
        /// <summary>
        /// Get the 3rd SByte of the vector
        /// </summary>
        [FieldOffset(3)] public sbyte SByte3;
        /// <summary>
        /// Get the 4th SByte of the vector
        /// </summary>
        [FieldOffset(4)] public sbyte SByte4;
        /// <summary>
        /// Get the 5th SByte of the vector
        /// </summary>
        [FieldOffset(5)] public sbyte SByte5;
        /// <summary>
        /// Get the 6th SByte of the vector
        /// </summary>
        [FieldOffset(6)] public sbyte SByte6;
        /// <summary>
        /// Get the 7th SByte of the vector
        /// </summary>
        [FieldOffset(7)] public sbyte SByte7;

        /// <summary>
        /// Get the 0th UShort of the vector
        /// </summary>
        [FieldOffset(0)] public ushort UShort0;
        /// <summary>
        /// Get the 1st UShort of the vector
        /// </summary>
        [FieldOffset(2)] public ushort UShort1;
        /// <summary>
        /// Get the 2nd UShort of the vector
        /// </summary>
        [FieldOffset(4)] public ushort UShort2;
        /// <summary>
        /// Get the 3rd UShort of the vector
        /// </summary>
        [FieldOffset(6)] public ushort UShort3;

        /// <summary>
        /// Get the 0th SShort of the vector
        /// </summary>
        [FieldOffset(0)] public short SShort0;
        /// <summary>
        /// Get the 1st SShort of the vector
        /// </summary>
        [FieldOffset(2)] public short SShort1;
        /// <summary>
        /// Get the 2nd SShort of the vector
        /// </summary>
        [FieldOffset(4)] public short SShort2;
        /// <summary>
        /// Get the 3rd SShort of the vector
        /// </summary>
        [FieldOffset(6)] public short SShort3;

        /// <summary>
        /// Get the 0th f16 of the vector
        /// </summary>
        [FieldOffset(0)] public f16 Half0;
        /// <summary>
        /// Get the 1st f16 of the vector
        /// </summary>
        [FieldOffset(2)] public f16 Half1;
        /// <summary>
        /// Get the 2nd f16 of the vector
        /// </summary>
        [FieldOffset(4)] public f16 Half2;
        /// <summary>
        /// Get the 3rd f16 of the vector
        /// </summary>
        [FieldOffset(6)] public f16 Half3;

        /// <summary>
        /// Get the 0th UInt of the vector
        /// </summary>
        [FieldOffset(0)] public uint UInt0;
        /// <summary>
        /// Get the 1st UInt of the vector
        /// </summary>
        [FieldOffset(4)] public uint UInt1;

        /// <summary>
        /// Get the 0th SInt of the vector
        /// </summary>
        [FieldOffset(0)] public int SInt0;
        /// <summary>
        /// Get the 1st SInt of the vector
        /// </summary>
        [FieldOffset(4)] public int SInt1;

        /// <summary>
        /// Get the 0th ULong of the vector
        /// </summary>
        [FieldOffset(0)] public ulong ULong0;

        /// <summary>
        /// Get the 0th SLong of the vector
        /// </summary>
        [FieldOffset(0)] public long SLong0;

        /// <summary>
        /// Get the 0th Float of the vector
        /// </summary>
        [FieldOffset(0)] public float Float0;
        /// <summary>
        /// Get the 1st Float of the vector
        /// </summary>
        [FieldOffset(4)] public float Float1;

        /// <summary>
        /// Get the 0th Double of the vector
        /// </summary>
        [FieldOffset(0)] public double Double0;


        /// <summary>
        /// Splat a single byte across the v64
        /// </summary>
		/// <param name="b">Splatted byte</param>
        public v64(byte b)
        {
            this = default(v64);
            Byte0 = Byte1 = Byte2 = Byte3 = Byte4 = Byte5 = Byte6 = Byte7 = b;
        }

        /// <summary>
        /// Initialize the v64 with 8 bytes
        /// </summary>
		/// <param name="a">byte a</param>
		/// <param name="b">byte b</param>
		/// <param name="c">byte c</param>
		/// <param name="d">byte d</param>
		/// <param name="e">byte e</param>
		/// <param name="f">byte f</param>
		/// <param name="g">byte g</param>
		/// <param name="h">byte h</param>
        public v64(
            byte a, byte b, byte c, byte d,
            byte e, byte f, byte g, byte h)
        {
            this = default(v64);
            Byte0 = a;
            Byte1 = b;
            Byte2 = c;
            Byte3 = d;
            Byte4 = e;
            Byte5 = f;
            Byte6 = g;
            Byte7 = h;
        }

        /// <summary>
        /// Splat a single sbyte across the v64
        /// </summary>
		/// <param name="b">Splatted sbyte</param>
        public v64(sbyte b)
        {
            this = default(v64);
            SByte0 = SByte1 = SByte2 = SByte3 = SByte4 = SByte5 = SByte6 = SByte7 = b;
        }

        /// <summary>
        /// Initialize the v64 with 8 sbytes
        /// </summary>
		/// <param name="a">sbyte a</param>
		/// <param name="b">sbyte b</param>
		/// <param name="c">sbyte c</param>
		/// <param name="d">sbyte d</param>
		/// <param name="e">sbyte e</param>
		/// <param name="f">sbyte f</param>
		/// <param name="g">sbyte g</param>
		/// <param name="h">sbyte h</param>
        public v64(
            sbyte a, sbyte b, sbyte c, sbyte d,
            sbyte e, sbyte f, sbyte g, sbyte h)
        {
            this = default(v64);
            SByte0 = a;
            SByte1 = b;
            SByte2 = c;
            SByte3 = d;
            SByte4 = e;
            SByte5 = f;
            SByte6 = g;
            SByte7 = h;
        }

        /// <summary>
        /// Splat a single short across the v64
        /// </summary>
		/// <param name="v">Splatted short</param>
        public v64(short v)
        {
            this = default(v64);
            SShort0 = SShort1 = SShort2 = SShort3 = v;
        }

        /// <summary>
        /// Initialize the v64 with 4 shorts
        /// </summary>
		/// <param name="a">short a</param>
		/// <param name="b">short b</param>
		/// <param name="c">short c</param>
		/// <param name="d">short d</param>
        public v64(short a, short b, short c, short d)
        {
            this = default(v64);
            SShort0 = a;
            SShort1 = b;
            SShort2 = c;
            SShort3 = d;
        }

        /// <summary>
        /// Splat a single ushort across the v64
        /// </summary>
		/// <param name="v">Splatted ushort</param>
        public v64(ushort v)
        {
            this = default(v64);
            UShort0 = UShort1 = UShort2 = UShort3 = v;
        }

        /// <summary>
        /// Initialize the v64 with 4 ushorts
        /// </summary>
		/// <param name="a">ushort a</param>
		/// <param name="b">ushort b</param>
		/// <param name="c">ushort c</param>
		/// <param name="d">ushort d</param>
        public v64(ushort a, ushort b, ushort c, ushort d)
        {
            this = default(v64);
            UShort0 = a;
            UShort1 = b;
            UShort2 = c;
            UShort3 = d;
        }

        /// <summary>
        /// Splat a single f16 across the v64
        /// </summary>
        /// <param name="v">Splatted f16</param>
        public v64(f16 v)
        {
            this = default(v64);
            Half0 = Half1 = Half2 = Half3 = v;
        }

        /// <summary>
        /// Initialize the v64 with 4 half's
        /// </summary>
        /// <param name="a">f16 a</param>
        /// <param name="b">f16 b</param>
        /// <param name="c">f16 c</param>
        /// <param name="d">f16 d</param>
        public v64(f16 a, f16 b, f16 c, f16 d)
        {
            this = default(v64);
            Half0 = a;
            Half1 = b;
            Half2 = c;
            Half3 = d;
        }

        /// <summary>
        /// Splat a single int across the v64
        /// </summary>
		/// <param name="v">Splatted int</param>
        public v64(int v)
        {
            this = default(v64);
            SInt0 = SInt1 = v;
        }

        /// <summary>
        /// Initialize the v64 with 2 ints
        /// </summary>
		/// <param name="a">int a</param>
		/// <param name="b">int b</param>
        public v64(int a, int b)
        {
            this = default(v64);
            SInt0 = a;
            SInt1 = b;
        }

        /// <summary>
        /// Splat a single uint across the v64
        /// </summary>
		/// <param name="v">Splatted uint</param>
        public v64(uint v)
        {
            this = default(v64);
            UInt0 = UInt1 = v;
        }

        /// <summary>
        /// Initialize the v64 with 2 uints
        /// </summary>
		/// <param name="a">uint a</param>
		/// <param name="b">uint b</param>
        public v64(uint a, uint b)
        {
            this = default(v64);
            UInt0 = a;
            UInt1 = b;
        }

        /// <summary>
        /// Splat a single float across the v64
        /// </summary>
		/// <param name="f">Splatted float</param>
        public v64(float f)
        {
            this = default(v64);
            Float0 = Float1 = f;
        }

        /// <summary>
        /// Initialize the v64 with 2 floats
        /// </summary>
		/// <param name="a">float a</param>
		/// <param name="b">float b</param>
        public v64(float a, float b)
        {
            this = default(v64);
            Float0 = a;
            Float1 = b;
        }

        /// <summary>
        /// Initialize the v64 with a double
        /// </summary>
        /// <param name="a">Splatted double</param>
        public v64(double a)
        {
            this = default(v64);
            Double0 = a;
        }

        /// <summary>
        /// Initialize the v64 with a long
        /// </summary>
		/// <param name="a">long a</param>
        public v64(long a)
        {
            this = default(v64);
            SLong0 = a;
        }

        /// <summary>
        /// Initialize the v64 with a ulong
        /// </summary>
		/// <param name="a">ulong a</param>
        public v64(ulong a)
        {
            this = default(v64);
            ULong0 = a;
        }
    }

    /// <summary>
    /// Represents a 128-bit SIMD value (Arm only)
    /// (a combination of 2 64-bit values, equivalent to Arm Neon *x2 types)
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct v64x2
    {
        /// <summary>
        /// Get the first 64 bits of the vector
        /// </summary>
        [FieldOffset(0)] public v64 v64_0;
        /// <summary>
        /// Get the second 64 bits of the vector
        /// </summary>
        [FieldOffset(8)] public v64 v64_1;

        /// <summary>
        /// Initialize the v64x2 with 2 v64's
        /// </summary>
		/// <param name="v0">First v64.</param>
		/// <param name="v1">Second v64.</param>
        public v64x2(v64 v0, v64 v1)
        {
            this = default(v64x2);
            v64_0 = v0;
            v64_1 = v1;
        }
    }

    /// <summary>
    /// Represents a 192-bit SIMD value (Arm only)
    /// (a combination of 3 64-bit values, equivalent to Arm Neon *x3 types)
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct v64x3
    {
        /// <summary>
        /// Get the first 64 bits of the vector
        /// </summary>
        [FieldOffset(0)] public v64 v64_0;
        /// <summary>
        /// Get the second 64 bits of the vector
        /// </summary>
        [FieldOffset(8)] public v64 v64_1;
        /// <summary>
        /// Get the third 64 bits of the vector
        /// </summary>
        [FieldOffset(16)] public v64 v64_2;

        /// <summary>
        /// Initialize the v64x3 with 3 v64's
        /// </summary>
		/// <param name="v0">First v64.</param>
		/// <param name="v1">Second v64.</param>
		/// <param name="v2">Third v64.</param>
        public v64x3(v64 v0, v64 v1, v64 v2)
        {
            this = default(v64x3);
            v64_0 = v0;
            v64_1 = v1;
            v64_2 = v2;
        }
    }

    /// <summary>
    /// Represents a 256-bit SIMD value (Arm only)
    /// (a combination of 4 64-bit values, equivalent to Arm Neon *x4 types)
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct v64x4
    {
        /// <summary>
        /// Get the first 64 bits of the vector
        /// </summary>
        [FieldOffset(0)] public v64 v64_0;
        /// <summary>
        /// Get the second 64 bits of the vector
        /// </summary>
        [FieldOffset(8)] public v64 v64_1;
        /// <summary>
        /// Get the third 64 bits of the vector
        /// </summary>
        [FieldOffset(16)] public v64 v64_2;
        /// <summary>
        /// Get the fourth 64 bits of the vector
        /// </summary>
        [FieldOffset(24)] public v64 v64_3;

        /// <summary>
        /// Initialize the v64x4 with 4 v64's
        /// </summary>
		/// <param name="v0">First v64.</param>
		/// <param name="v1">Second v64.</param>
		/// <param name="v2">Third v64.</param>
		/// <param name="v3">Fourth v64.</param>
        public v64x4(v64 v0, v64 v1, v64 v2, v64 v3)
        {
            this = default(v64x4);
            v64_0 = v0;
            v64_1 = v1;
            v64_2 = v2;
            v64_3 = v3;
        }
    }
}
