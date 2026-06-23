// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unity.Burst.Intrinsics
{
    /// <summary>
    /// Represents a 128-bit SIMD value
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
	[DebuggerTypeProxy(typeof(V128DebugView))]
    public struct v128
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
		/// Get the 8th Byte of the vector
		/// </summary>
        [FieldOffset(8)] public byte Byte8;
		/// <summary>
		/// Get the 9th Byte of the vector
		/// </summary>
        [FieldOffset(9)] public byte Byte9;
		/// <summary>
		/// Get the 10th Byte of the vector
		/// </summary>
        [FieldOffset(10)] public byte Byte10;
		/// <summary>
		/// Get the 11th Byte of the vector
		/// </summary>
        [FieldOffset(11)] public byte Byte11;
		/// <summary>
		/// Get the 12 Byte of the vector
		/// </summary>
        [FieldOffset(12)] public byte Byte12;
		/// <summary>
		/// Get the 13th Byte of the vector
		/// </summary>
        [FieldOffset(13)] public byte Byte13;
		/// <summary>
		/// Get the 14th Byte of the vector
		/// </summary>
        [FieldOffset(14)] public byte Byte14;
		/// <summary>
		/// Get the 15th Byte of the vector
		/// </summary>
        [FieldOffset(15)] public byte Byte15;


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
		/// Get the 8th SByte of the vector
		/// </summary>
        [FieldOffset(8)] public sbyte SByte8;
		/// <summary>
		/// Get the 9th SByte of the vector
		/// </summary>
        [FieldOffset(9)] public sbyte SByte9;
		/// <summary>
		/// Get the 10th SByte of the vector
		/// </summary>
        [FieldOffset(10)] public sbyte SByte10;
		/// <summary>
		/// Get the 11th SByte of the vector
		/// </summary>
        [FieldOffset(11)] public sbyte SByte11;
		/// <summary>
		/// Get the 12th SByte of the vector
		/// </summary>
        [FieldOffset(12)] public sbyte SByte12;
		/// <summary>
		/// Get the 13th SByte of the vector
		/// </summary>
        [FieldOffset(13)] public sbyte SByte13;
		/// <summary>
		/// Get the 14th SByte of the vector
		/// </summary>
        [FieldOffset(14)] public sbyte SByte14;
		/// <summary>
		/// Get the 15th SByte of the vector
		/// </summary>
        [FieldOffset(15)] public sbyte SByte15;
		

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
		/// Get the 4th UShort of the vector
		/// </summary>
        [FieldOffset(8)] public ushort UShort4;
		/// <summary>
		/// Get the 5th UShort of the vector
		/// </summary>
        [FieldOffset(10)] public ushort UShort5;
		/// <summary>
		/// Get the 6th UShort of the vector
		/// </summary>
        [FieldOffset(12)] public ushort UShort6;
		/// <summary>
		/// Get the 7th UShort of the vector
		/// </summary>
        [FieldOffset(14)] public ushort UShort7;

		/// <summary>
		/// Get the 0th SShort of the vector
		/// </summary>
        [FieldOffset(0)] public short SShort0;
		/// <summary>
		/// Get the 1st UShort of the vector
		/// </summary>
        [FieldOffset(2)] public short SShort1;
		/// <summary>
		/// Get the 2nd UShort of the vector
		/// </summary>
        [FieldOffset(4)] public short SShort2;
		/// <summary>
		/// Get the 3rd UShort of the vector
		/// </summary>
        [FieldOffset(6)] public short SShort3;
		/// <summary>
		/// Get the 4th UShort of the vector
		/// </summary>
        [FieldOffset(8)] public short SShort4;
		/// <summary>
		/// Get the 5th UShort of the vector
		/// </summary>
        [FieldOffset(10)] public short SShort5;
		/// <summary>
		/// Get the 6th UShort of the vector
		/// </summary>
        [FieldOffset(12)] public short SShort6;
		/// <summary>
		/// Get the 7th UShort of the vector
		/// </summary>
        [FieldOffset(14)] public short SShort7;


        /// <summary>
        /// Get the 0th UInt of the vector
        /// </summary>
        [FieldOffset(0)] public uint UInt0;
		/// <summary>
		/// Get the 1st UInt of the vector
		/// </summary>
        [FieldOffset(4)] public uint UInt1;
		/// <summary>
		/// Get the 2nd UInt of the vector
		/// </summary>
        [FieldOffset(8)] public uint UInt2;
		/// <summary>
		/// Get the 3rd UInt of the vector
		/// </summary>
        [FieldOffset(12)] public uint UInt3;

		/// <summary>
		/// Get the 0th SInt of the vector
		/// </summary>
        [FieldOffset(0)] public int SInt0;
		/// <summary>
		/// Get the 1st SInt of the vector
		/// </summary>
        [FieldOffset(4)] public int SInt1;
		/// <summary>
		/// Get the 2nd SInt of the vector
		/// </summary>
        [FieldOffset(8)] public int SInt2;
		/// <summary>
		/// Get the 3rd SInt of the vector
		/// </summary>
        [FieldOffset(12)] public int SInt3;

		/// <summary>
		/// Get the 0th ULong of the vector
		/// </summary>
        [FieldOffset(0)] public ulong ULong0;
		/// <summary>
		/// Get the 1st ULong of the vector
		/// </summary>
        [FieldOffset(8)] public ulong ULong1;

		/// <summary>
		/// Get the 0th SLong of the vector
		/// </summary>
        [FieldOffset(0)] public long SLong0;
		/// <summary>
		/// Get the 1st SLong of the vector
		/// </summary>
        [FieldOffset(8)] public long SLong1;

		/// <summary>
		/// Get the 0th Float of the vector
		/// </summary>
        [FieldOffset(0)] public float Float0;
		/// <summary>
		/// Get the 1st Float of the vector
		/// </summary>
        [FieldOffset(4)] public float Float1;
		/// <summary>
		/// Get the 2nd Float of the vector
		/// </summary>
        [FieldOffset(8)] public float Float2;
		/// <summary>
		/// Get the 3rd Float of the vector
		/// </summary>
        [FieldOffset(12)] public float Float3;

		/// <summary>
		/// Get the 0th Double of the vector
		/// </summary>
        [FieldOffset(0)] public double Double0;
		/// <summary>
		/// Get the 1st Double of the vector
		/// </summary>
        [FieldOffset(8)] public double Double1;

		/// <summary>
		/// Get the low half of the vector
		/// </summary>
        [FieldOffset(0)] public v64 Lo64;
		/// <summary>
		/// Get the high half of the vector
		/// </summary>
        [FieldOffset(8)] public v64 Hi64;

        /// <summary>
        /// Splat a single byte across the v128
        /// </summary>
		/// <param name="b">Splatted byte.</param>
        public v128(byte b)
        {
            this = default(v128);
            Byte0 = Byte1 = Byte2 = Byte3 = Byte4 = Byte5 = Byte6 = Byte7 = Byte8 = Byte9 = Byte10 = Byte11 = Byte12 = Byte13 = Byte14 = Byte15 = b;
        }

        /// <summary>
        /// Initialize the v128 with 16 bytes
        /// </summary>
		/// <param name="a">byte a.</param>
		/// <param name="b">byte b.</param>
		/// <param name="c">byte c.</param>
		/// <param name="d">byte d.</param>
		/// <param name="e">byte e.</param>
		/// <param name="f">byte f.</param>
		/// <param name="g">byte g.</param>
		/// <param name="h">byte h.</param>
		/// <param name="i">byte i.</param>
		/// <param name="j">byte j.</param>
		/// <param name="k">byte k.</param>
		/// <param name="l">byte l.</param>
		/// <param name="m">byte m.</param>
		/// <param name="n">byte n.</param>
		/// <param name="o">byte o.</param>
		/// <param name="p">byte p.</param>
        public v128(
            byte a, byte b, byte c, byte d,
            byte e, byte f, byte g, byte h,
            byte i, byte j, byte k, byte l,
            byte m, byte n, byte o, byte p)
        {
            this = default(v128);
            Byte0 = a;
            Byte1 = b;
            Byte2 = c;
            Byte3 = d;
            Byte4 = e;
            Byte5 = f;
            Byte6 = g;
            Byte7 = h;
            Byte8 = i;
            Byte9 = j;
            Byte10 = k;
            Byte11 = l;
            Byte12 = m;
            Byte13 = n;
            Byte14 = o;
            Byte15 = p;
        }

        /// <summary>
        /// Splat a single sbyte across the v128
        /// </summary>
		/// <param name="b">Splatted sbyte.</param>
        public v128(sbyte b)
        {
            this = default(v128);
            SByte0 = SByte1 = SByte2 = SByte3 = SByte4 = SByte5 = SByte6 = SByte7 = SByte8 = SByte9 = SByte10 = SByte11 = SByte12 = SByte13 = SByte14 = SByte15 = b;
        }

        /// <summary>
        /// Initialize the v128 with 16 sbytes
        /// </summary>
		/// <param name="a">sbyte a.</param>
		/// <param name="b">sbyte b.</param>
		/// <param name="c">sbyte c.</param>
		/// <param name="d">sbyte d.</param>
		/// <param name="e">sbyte e.</param>
		/// <param name="f">sbyte f.</param>
		/// <param name="g">sbyte g.</param>
		/// <param name="h">sbyte h.</param>
		/// <param name="i">sbyte i.</param>
		/// <param name="j">sbyte j.</param>
		/// <param name="k">sbyte k.</param>
		/// <param name="l">sbyte l.</param>
		/// <param name="m">sbyte m.</param>
		/// <param name="n">sbyte n.</param>
		/// <param name="o">sbyte o.</param>
		/// <param name="p">sbyte p.</param>
        public v128(
            sbyte a, sbyte b, sbyte c, sbyte d,
            sbyte e, sbyte f, sbyte g, sbyte h,
            sbyte i, sbyte j, sbyte k, sbyte l,
            sbyte m, sbyte n, sbyte o, sbyte p)
        {
            this = default(v128);
            SByte0 = a;
            SByte1 = b;
            SByte2 = c;
            SByte3 = d;
            SByte4 = e;
            SByte5 = f;
            SByte6 = g;
            SByte7 = h;
            SByte8 = i;
            SByte9 = j;
            SByte10 = k;
            SByte11 = l;
            SByte12 = m;
            SByte13 = n;
            SByte14 = o;
            SByte15 = p;
        }

        /// <summary>
        /// Splat a single short across the v128
        /// </summary>
		/// <param name="v">Splatted short.</param>
        public v128(short v)
        {
            this = default(v128);
            SShort0 = SShort1 = SShort2 = SShort3 = SShort4 = SShort5 = SShort6 = SShort7 = v;
        }

        /// <summary>
        /// Initialize the v128 with 8 shorts
        /// </summary>
		/// <param name="a">short a.</param>
		/// <param name="b">short b.</param>
		/// <param name="c">short c.</param>
		/// <param name="d">short d.</param>
		/// <param name="e">short e.</param>
		/// <param name="f">short f.</param>
		/// <param name="g">short g.</param>
		/// <param name="h">short h.</param>
        public v128(short a, short b, short c, short d, short e, short f, short g, short h)
        {
            this = default(v128);
            SShort0 = a;
            SShort1 = b;
            SShort2 = c;
            SShort3 = d;
            SShort4 = e;
            SShort5 = f;
            SShort6 = g;
            SShort7 = h;
        }

        /// <summary>
        /// Splat a single ushort across the v128
        /// </summary>
		/// <param name="v">Splatted ushort.</param>
        public v128(ushort v)
        {
            this = default(v128);
            UShort0 = UShort1 = UShort2 = UShort3 = UShort4 = UShort5 = UShort6 = UShort7 = v;
        }

        /// <summary>
        /// Initialize the v128 with 8 ushorts
        /// </summary>
		/// <param name="a">ushort a.</param>
		/// <param name="b">ushort b.</param>
		/// <param name="c">ushort c.</param>
		/// <param name="d">ushort d.</param>
		/// <param name="e">ushort e.</param>
		/// <param name="f">ushort f.</param>
		/// <param name="g">ushort g.</param>
		/// <param name="h">ushort h.</param>
        public v128(ushort a, ushort b, ushort c, ushort d, ushort e, ushort f, ushort g, ushort h)
        {
            this = default(v128);
            UShort0 = a;
            UShort1 = b;
            UShort2 = c;
            UShort3 = d;
            UShort4 = e;
            UShort5 = f;
            UShort6 = g;
            UShort7 = h;
        }


        /// <summary>
        /// Splat a single int across the v128
        /// </summary>
		/// <param name="v">Splatted int.</param>
        public v128(int v)
        {
            this = default(v128);
            SInt0 = SInt1 = SInt2 = SInt3 = v;
        }

        /// <summary>
        /// Initialize the v128 with 4 ints
        /// </summary>
		/// <param name="a">int a.</param>
		/// <param name="b">int b.</param>
		/// <param name="c">int c.</param>
		/// <param name="d">int d.</param>
        public v128(int a, int b, int c, int d)
        {
            this = default(v128);
            SInt0 = a;
            SInt1 = b;
            SInt2 = c;
            SInt3 = d;
        }

        /// <summary>
        /// Splat a single uint across the v128
        /// </summary>
		/// <param name="v">Splatted uint.</param>
        public v128(uint v)
        {
            this = default(v128);
            UInt0 = UInt1 = UInt2 = UInt3 = v;
        }

        /// <summary>
        /// Initialize the v128 with 4 uints
        /// </summary>
		/// <param name="a">uint a.</param>
		/// <param name="b">uint b.</param>
		/// <param name="c">uint c.</param>
		/// <param name="d">uint d.</param>
        public v128(uint a, uint b, uint c, uint d)
        {
            this = default(v128);
            UInt0 = a;
            UInt1 = b;
            UInt2 = c;
            UInt3 = d;
        }

        /// <summary>
        /// Splat a single float across the v128
        /// </summary>
		/// <param name="f">Splatted float.</param>
        public v128(float f)
        {
            this = default(v128);
            Float0 = Float1 = Float2 = Float3 = f;
        }

        /// <summary>
        /// Initialize the v128 with 4 floats
        /// </summary>
		/// <param name="a">float a.</param>
		/// <param name="b">float b.</param>
		/// <param name="c">float c.</param>
		/// <param name="d">float d.</param>
        public v128(float a, float b, float c, float d)
        {
            this = default(v128);
            Float0 = a;
            Float1 = b;
            Float2 = c;
            Float3 = d;
        }

        /// <summary>
        /// Splat a single double across the v128
        /// </summary>
		/// <param name="f">Splatted double.</param>
        public v128(double f)
        {
            this = default(v128);
            Double0 = Double1 = f;
        }

        /// <summary>
        /// Initialize the v128 with 2 doubles
        /// </summary>
		/// <param name="a">double a.</param>
		/// <param name="b">double b.</param>
        public v128(double a, double b)
        {
            this = default(v128);
            Double0 = a;
            Double1 = b;
        }

        /// <summary>
        /// Splat a single long across the v128
        /// </summary>
		/// <param name="f">Splatted long.</param>
        public v128(long f)
        {
            this = default(v128);
            SLong0 = SLong1 = f;
        }

        /// <summary>
        /// Initialize the v128 with 2 longs
        /// </summary>
		/// <param name="a">long a.</param>
		/// <param name="b">long b.</param>
        public v128(long a, long b)
        {
            this = default(v128);
            SLong0 = a;
            SLong1 = b;
        }

        /// <summary>
        /// Splat a single ulong across the v128
        /// </summary>
		/// <param name="f">Splatted ulong.</param>
        public v128(ulong f)
        {
            this = default(v128);
            ULong0 = ULong1 = f;
        }

        /// <summary>
        /// Initialize the v128 with 2 ulongs
        /// </summary>
		/// <param name="a">ulong a.</param>
		/// <param name="b">ulong b.</param>
        public v128(ulong a, ulong b)
        {
            this = default(v128);
            ULong0 = a;
            ULong1 = b;
        }

        /// <summary>
        /// Initialize the v128 with 2 v64's
        /// </summary>
		/// <param name="lo">Low half of v64.</param>
		/// <param name="hi">High half of v64.</param>
        public v128(v64 lo, v64 hi)
        {
            this = default(v128);
            Lo64 = lo;
            Hi64 = hi;
        }
    }

}
