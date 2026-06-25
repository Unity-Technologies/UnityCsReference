// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unity.Burst.Intrinsics
{
    /// <summary>
    /// Represents a 256-bit SIMD value
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
	[DebuggerTypeProxy(typeof(V256DebugView))]
    public struct v256
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
		/// Get the 16th Byte of the vector
		/// </summary>
        [FieldOffset(16)] public byte Byte16;
		/// <summary>
		/// Get the 17th Byte of the vector
		/// </summary>
        [FieldOffset(17)] public byte Byte17;
		/// <summary>
		/// Get the 18th Byte of the vector
		/// </summary>
        [FieldOffset(18)] public byte Byte18;
		/// <summary>
		/// Get the 19th Byte of the vector
		/// </summary>
        [FieldOffset(19)] public byte Byte19;
		/// <summary>
		/// Get the 20th Byte of the vector
		/// </summary>
        [FieldOffset(20)] public byte Byte20;
		/// <summary>
		/// Get the 21st Byte of the vector
		/// </summary>
        [FieldOffset(21)] public byte Byte21;
		/// <summary>
		/// Get the 22nd Byte of the vector
		/// </summary>
        [FieldOffset(22)] public byte Byte22;
		/// <summary>
		/// Get the 23rd Byte of the vector
		/// </summary>
        [FieldOffset(23)] public byte Byte23;
		/// <summary>
		/// Get the 24th Byte of the vector
		/// </summary>
        [FieldOffset(24)] public byte Byte24;
		/// <summary>
		/// Get the 25th Byte of the vector
		/// </summary>
        [FieldOffset(25)] public byte Byte25;
		/// <summary>
		/// Get the 26th Byte of the vector
		/// </summary>
        [FieldOffset(26)] public byte Byte26;
		/// <summary>
		/// Get the 27th Byte of the vector
		/// </summary>
        [FieldOffset(27)] public byte Byte27;
		/// <summary>
		/// Get the 28th Byte of the vector
		/// </summary>
        [FieldOffset(28)] public byte Byte28;
		/// <summary>
		/// Get the 29th Byte of the vector
		/// </summary>
        [FieldOffset(29)] public byte Byte29;
		/// <summary>
		/// Get the 30th Byte of the vector
		/// </summary>
        [FieldOffset(30)] public byte Byte30;
		/// <summary>
		/// Get the 31st Byte of the vector
		/// </summary>
        [FieldOffset(31)] public byte Byte31;
		
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
		/// Get the 16th SByte of the vector
		/// </summary>
        [FieldOffset(16)] public sbyte SByte16;
		/// <summary>
		/// Get the 17th SByte of the vector
		/// </summary>
        [FieldOffset(17)] public sbyte SByte17;
		/// <summary>
		/// Get the 18th SByte of the vector
		/// </summary>
        [FieldOffset(18)] public sbyte SByte18;
		/// <summary>
		/// Get the 19th SByte of the vector
		/// </summary>
        [FieldOffset(19)] public sbyte SByte19;
		/// <summary>
		/// Get the 20th SByte of the vector
		/// </summary>
        [FieldOffset(20)] public sbyte SByte20;
		/// <summary>
		/// Get the 21st SByte of the vector
		/// </summary>
        [FieldOffset(21)] public sbyte SByte21;
		/// <summary>
		/// Get the 22nd SByte of the vector
		/// </summary>
        [FieldOffset(22)] public sbyte SByte22;
		/// <summary>
		/// Get the 23rd SByte of the vector
		/// </summary>
        [FieldOffset(23)] public sbyte SByte23;
		/// <summary>
		/// Get the 24th SByte of the vector
		/// </summary>
        [FieldOffset(24)] public sbyte SByte24;
		/// <summary>
		/// Get the 25th SByte of the vector
		/// </summary>
        [FieldOffset(25)] public sbyte SByte25;
		/// <summary>
		/// Get the 26th SByte of the vector
		/// </summary>
        [FieldOffset(26)] public sbyte SByte26;
		/// <summary>
		/// Get the 27th SByte of the vector
		/// </summary>
        [FieldOffset(27)] public sbyte SByte27;
		/// <summary>
		/// Get the 28th SByte of the vector
		/// </summary>
        [FieldOffset(28)] public sbyte SByte28;
		/// <summary>
		/// Get the 29th SByte of the vector
		/// </summary>
        [FieldOffset(29)] public sbyte SByte29;
		/// <summary>
		/// Get the 30th SByte of the vector
		/// </summary>
        [FieldOffset(30)] public sbyte SByte30;
		/// <summary>
		/// Get the 31st SByte of the vector
		/// </summary>
        [FieldOffset(31)] public sbyte SByte31;

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
		/// Get the 8th UShort of the vector
		/// </summary>
        [FieldOffset(16)] public ushort UShort8;
		/// <summary>
		/// Get the 9th UShort of the vector
		/// </summary>
        [FieldOffset(18)] public ushort UShort9;
		/// <summary>
		/// Get the 10th UShort of the vector
		/// </summary>
        [FieldOffset(20)] public ushort UShort10;
		/// <summary>
		/// Get the 11th UShort of the vector
		/// </summary>
        [FieldOffset(22)] public ushort UShort11;
		/// <summary>
		/// Get the 12th UShort of the vector
		/// </summary>
        [FieldOffset(24)] public ushort UShort12;
		/// <summary>
		/// Get the 13th UShort of the vector
		/// </summary>
        [FieldOffset(26)] public ushort UShort13;
		/// <summary>
		/// Get the 14th UShort of the vector
		/// </summary>
        [FieldOffset(28)] public ushort UShort14;
		/// <summary>
		/// Get the 15th UShort of the vector
		/// </summary>
        [FieldOffset(30)] public ushort UShort15;

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
		/// Get the 4th SShort of the vector
		/// </summary>
        [FieldOffset(8)] public short SShort4;
		/// <summary>
		/// Get the 5th SShort of the vector
		/// </summary>
        [FieldOffset(10)] public short SShort5;
		/// <summary>
		/// Get the 6th SShort of the vector
		/// </summary>
        [FieldOffset(12)] public short SShort6;
		/// <summary>
		/// Get the 7th SShort of the vector
		/// </summary>
        [FieldOffset(14)] public short SShort7;
		/// <summary>
		/// Get the 8th SShort of the vector
		/// </summary>
        [FieldOffset(16)] public short SShort8;
		/// <summary>
		/// Get the 9th SShort of the vector
		/// </summary>
        [FieldOffset(18)] public short SShort9;
		/// <summary>
		/// Get the 10th SShort of the vector
		/// </summary>
        [FieldOffset(20)] public short SShort10;
		/// <summary>
		/// Get the 11th SShort of the vector
		/// </summary>
        [FieldOffset(22)] public short SShort11;
		/// <summary>
		/// Get the 12th SShort of the vector
		/// </summary>
        [FieldOffset(24)] public short SShort12;
		/// <summary>
		/// Get the 13th SShort of the vector
		/// </summary>
        [FieldOffset(26)] public short SShort13;
		/// <summary>
		/// Get the 14th SShort of the vector
		/// </summary>
        [FieldOffset(28)] public short SShort14;
		/// <summary>
		/// Get the 15th SShort of the vector
		/// </summary>
        [FieldOffset(30)] public short SShort15;


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
		/// Get the 4th UInt of the vector
		/// </summary>
        [FieldOffset(16)] public uint UInt4;
		/// <summary>
		/// Get the 5th UInt of the vector
		/// </summary>
        [FieldOffset(20)] public uint UInt5;
		/// <summary>
		/// Get the 6th UInt of the vector
		/// </summary>
        [FieldOffset(24)] public uint UInt6;
		/// <summary>
		/// Get the 7th UInt of the vector
		/// </summary>
        [FieldOffset(28)] public uint UInt7;

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
		/// Get the 4th SInt of the vector
		/// </summary>
        [FieldOffset(16)] public int SInt4;
		/// <summary>
		/// Get the 5th SInt of the vector
		/// </summary>
        [FieldOffset(20)] public int SInt5;
		/// <summary>
		/// Get the 6th SInt of the vector
		/// </summary>
        [FieldOffset(24)] public int SInt6;
		/// <summary>
		/// Get the 7th SInt of the vector
		/// </summary>
        [FieldOffset(28)] public int SInt7;

		/// <summary>
		/// Get the 0th ULong of the vector
		/// </summary>
        [FieldOffset(0)] public ulong ULong0;
		/// <summary>
		/// Get the 1st ULong of the vector
		/// </summary>
        [FieldOffset(8)] public ulong ULong1;
		/// <summary>
		/// Get the 2nd ULong of the vector
		/// </summary>
        [FieldOffset(16)] public ulong ULong2;
		/// <summary>
		/// Get the 3rd ULong of the vector
		/// </summary>
        [FieldOffset(24)] public ulong ULong3;

		/// <summary>
		/// Get the 0th SLong of the vector
		/// </summary>
        [FieldOffset(0)] public long SLong0;
		/// <summary>
		/// Get the 1st SLong of the vector
		/// </summary>
        [FieldOffset(8)] public long SLong1;
		/// <summary>
		/// Get the 2nd SLong of the vector
		/// </summary>
        [FieldOffset(16)] public long SLong2;
		/// <summary>
		/// Get the 3rd SLong of the vector
		/// </summary>
        [FieldOffset(24)] public long SLong3;

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
		/// Get the 4th Float of the vector
		/// </summary>
        [FieldOffset(16)] public float Float4;
		/// <summary>
		/// Get the 5th Float of the vector
		/// </summary>
        [FieldOffset(20)] public float Float5;
		/// <summary>
		/// Get the 6th Float of the vector
		/// </summary>
        [FieldOffset(24)] public float Float6;
		/// <summary>
		/// Get the 7th Float of the vector
		/// </summary>
        [FieldOffset(28)] public float Float7;

		/// <summary>
		/// Get the 0th Double of the vector
		/// </summary>
        [FieldOffset(0)] public double Double0;
		/// <summary>
		/// Get the 1st Double of the vector
		/// </summary>
        [FieldOffset(8)] public double Double1;
		/// <summary>
		/// Get the 2nd Double of the vector
		/// </summary>
        [FieldOffset(16)] public double Double2;
		/// <summary>
		/// Get the 3rd Double of the vector
		/// </summary>
        [FieldOffset(24)] public double Double3;

		/// <summary>
		/// Get the low half of the vector
		/// </summary>
        [FieldOffset(0)] public v128 Lo128;
		/// <summary>
		/// Get the high half of the vector
		/// </summary>
        [FieldOffset(16)] public v128 Hi128;
		
		/// <summary>
		/// Splat a single byte across the v256
		/// </summary>
		/// <param name="b">Splatted byte.</param>
        public v256(byte b)
        {
            this = default(v256);
            Byte0 = Byte1 = Byte2 = Byte3 = Byte4 = Byte5 = Byte6 = Byte7 =
            Byte8 = Byte9 = Byte10 = Byte11 = Byte12 = Byte13 = Byte14 = Byte15 =
            Byte16 = Byte17 = Byte18 = Byte19 = Byte20 = Byte21 = Byte22 = Byte23 =
            Byte24 = Byte25 = Byte26 = Byte27 = Byte28 = Byte29 = Byte30 = Byte31 =
                b;
        }
		
		/// <summary>
        /// Initialize the v256 with 32 bytes
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
		/// <param name="q">byte q.</param>
		/// <param name="r">byte r.</param>
		/// <param name="s">byte s.</param>
		/// <param name="t">byte t.</param>
		/// <param name="u">byte u.</param>
		/// <param name="v">byte v.</param>
		/// <param name="w">byte w.</param>
		/// <param name="x">byte x.</param>
		/// <param name="y">byte y.</param>
		/// <param name="z">byte z.</param>
		/// <param name="A">byte A.</param>
		/// <param name="B">byte B.</param>
		/// <param name="C">byte C.</param>
		/// <param name="D">byte D.</param>
		/// <param name="E">byte E.</param>
		/// <param name="F">byte F.</param>
        public v256(
            byte a, byte b, byte c, byte d,
            byte e, byte f, byte g, byte h,
            byte i, byte j, byte k, byte l,
            byte m, byte n, byte o, byte p,
            byte q, byte r, byte s, byte t,
            byte u, byte v, byte w, byte x,
            byte y, byte z, byte A, byte B,
            byte C, byte D, byte E, byte F)
        {
            this = default(v256);
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
            Byte16 = q;
            Byte17 = r;
            Byte18 = s;
            Byte19 = t;
            Byte20 = u;
            Byte21 = v;
            Byte22 = w;
            Byte23 = x;
            Byte24 = y;
            Byte25 = z;
            Byte26 = A;
            Byte27 = B;
            Byte28 = C;
            Byte29 = D;
            Byte30 = E;
            Byte31 = F;
        }
		
		/// <summary>
        /// Splat a single sbyte across the v256
        /// </summary>
		/// <param name="b">Splatted sbyte.</param>
        public v256(sbyte b)
        {
            this = default(v256);
            SByte0 = SByte1 = SByte2 = SByte3 = SByte4 = SByte5 = SByte6 = SByte7 =
            SByte8 = SByte9 = SByte10 = SByte11 = SByte12 = SByte13 = SByte14 = SByte15 =
            SByte16 = SByte17 = SByte18 = SByte19 = SByte20 = SByte21 = SByte22 = SByte23 =
            SByte24 = SByte25 = SByte26 = SByte27 = SByte28 = SByte29 = SByte30 = SByte31 =
                b;
        }
		
		/// <summary>
        /// Initialize the v256 with 32 sbytes
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
		/// <param name="q">sbyte q.</param>
		/// <param name="r">sbyte r.</param>
		/// <param name="s">sbyte s.</param>
		/// <param name="t">sbyte t.</param>
		/// <param name="u">sbyte u.</param>
		/// <param name="v">sbyte v.</param>
		/// <param name="w">sbyte w.</param>
		/// <param name="x">sbyte x.</param>
		/// <param name="y">sbyte y.</param>
		/// <param name="z">sbyte z.</param>
		/// <param name="A">sbyte A.</param>
		/// <param name="B">sbyte B.</param>
		/// <param name="C">sbyte C.</param>
		/// <param name="D">sbyte D.</param>
		/// <param name="E">sbyte E.</param>
		/// <param name="F">sbyte F.</param>
        public v256(
            sbyte a, sbyte b, sbyte c, sbyte d,
            sbyte e, sbyte f, sbyte g, sbyte h,
            sbyte i, sbyte j, sbyte k, sbyte l,
            sbyte m, sbyte n, sbyte o, sbyte p,
            sbyte q, sbyte r, sbyte s, sbyte t,
            sbyte u, sbyte v, sbyte w, sbyte x,
            sbyte y, sbyte z, sbyte A, sbyte B,
            sbyte C, sbyte D, sbyte E, sbyte F)
        {
            this = default(v256);
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
            SByte16 = q;
            SByte17 = r;
            SByte18 = s;
            SByte19 = t;
            SByte20 = u;
            SByte21 = v;
            SByte22 = w;
            SByte23 = x;
            SByte24 = y;
            SByte25 = z;
            SByte26 = A;
            SByte27 = B;
            SByte28 = C;
            SByte29 = D;
            SByte30 = E;
            SByte31 = F;
        }

		/// <summary>
        /// Splat a single short across the v256
        /// </summary>
		/// <param name="v">Splatted short.</param>
        public v256(short v)
        {
            this = default(v256);
            SShort0 = SShort1 = SShort2 = SShort3 = SShort4 = SShort5 = SShort6 = SShort7 =
            SShort8 = SShort9 = SShort10 = SShort11 = SShort12 = SShort13 = SShort14 = SShort15 =
                v;
        }

		/// <summary>
        /// Initialize the v256 with 16 shorts
        /// </summary>
		/// <param name="a">short a.</param>
		/// <param name="b">short b.</param>
		/// <param name="c">short c.</param>
		/// <param name="d">short d.</param>
		/// <param name="e">short e.</param>
		/// <param name="f">short f.</param>
		/// <param name="g">short g.</param>
		/// <param name="h">short h.</param>
		/// <param name="i">short i.</param>
		/// <param name="j">short j.</param>
		/// <param name="k">short k.</param>
		/// <param name="l">short l.</param>
		/// <param name="m">short m.</param>
		/// <param name="n">short n.</param>
		/// <param name="o">short o.</param>
		/// <param name="p">short p.</param>
        public v256(
                short a, short b, short c, short d, short e, short f, short g, short h,
                short i, short j, short k, short l, short m, short n, short o, short p)
        {
            this = default(v256);
            SShort0 = a;
            SShort1 = b;
            SShort2 = c;
            SShort3 = d;
            SShort4 = e;
            SShort5 = f;
            SShort6 = g;
            SShort7 = h;
            SShort8 = i;
            SShort9 = j;
            SShort10 = k;
            SShort11 = l;
            SShort12 = m;
            SShort13 = n;
            SShort14 = o;
            SShort15 = p;
        }

		/// <summary>
        /// Splat a single ushort across the v256
        /// </summary>
		/// <param name="v">Splatted ushort.</param>
        public v256(ushort v)
        {
            this = default(v256);
            UShort0 = UShort1 = UShort2 = UShort3 = UShort4 = UShort5 = UShort6 = UShort7 =
            UShort8 = UShort9 = UShort10 = UShort11 = UShort12 = UShort13 = UShort14 = UShort15 =
                v;
        }

		/// <summary>
        /// Initialize the v256 with 16 ushorts
        /// </summary>
		/// <param name="a">ushort a.</param>
		/// <param name="b">ushort b.</param>
		/// <param name="c">ushort c.</param>
		/// <param name="d">ushort d.</param>
		/// <param name="e">ushort e.</param>
		/// <param name="f">ushort f.</param>
		/// <param name="g">ushort g.</param>
		/// <param name="h">ushort h.</param>
		/// <param name="i">ushort i.</param>
		/// <param name="j">ushort j.</param>
		/// <param name="k">ushort k.</param>
		/// <param name="l">ushort l.</param>
		/// <param name="m">ushort m.</param>
		/// <param name="n">ushort n.</param>
		/// <param name="o">ushort o.</param>
		/// <param name="p">ushort p.</param>
        public v256(
                ushort a, ushort b, ushort c, ushort d, ushort e, ushort f, ushort g, ushort h,
                ushort i, ushort j, ushort k, ushort l, ushort m, ushort n, ushort o, ushort p)
        {
            this = default(v256);
            UShort0 = a;
            UShort1 = b;
            UShort2 = c;
            UShort3 = d;
            UShort4 = e;
            UShort5 = f;
            UShort6 = g;
            UShort7 = h;
            UShort8 = i;
            UShort9 = j;
            UShort10 = k;
            UShort11 = l;
            UShort12 = m;
            UShort13 = n;
            UShort14 = o;
            UShort15 = p;
        }


        /// <summary>
        /// Splat a single int across the v256
        /// </summary>
        /// <param name="v">Splatted int.</param>
        public v256(int v)
        {
            this = default(v256);
            SInt0 = SInt1 = SInt2 = SInt3 = SInt4 = SInt5 = SInt6 = SInt7 = v;
        }

		/// <summary>
        /// Initialize the v256 with 8 ints
        /// </summary>
		/// <param name="a">int a.</param>
		/// <param name="b">int b.</param>
		/// <param name="c">int c.</param>
		/// <param name="d">int d.</param>
		/// <param name="e">int e.</param>
		/// <param name="f">int f.</param>
		/// <param name="g">int g.</param>
		/// <param name="h">int h.</param>
        public v256(int a, int b, int c, int d, int e, int f, int g, int h)
        {
            this = default(v256);
            SInt0 = a;
            SInt1 = b;
            SInt2 = c;
            SInt3 = d;
            SInt4 = e;
            SInt5 = f;
            SInt6 = g;
            SInt7 = h;
        }

		/// <summary>
        /// Splat a single uint across the v256
        /// </summary>
		/// <param name="v">Splatted uint.</param>
        public v256(uint v)
        {
            this = default(v256);
            UInt0 = UInt1 = UInt2 = UInt3 = UInt4 = UInt5 = UInt6 = UInt7 = v;
        }

        /// <summary>
        /// Initialize the v256 with 8 uints
        /// </summary>
		/// <param name="a">uint a.</param>
		/// <param name="b">uint b.</param>
		/// <param name="c">uint c.</param>
		/// <param name="d">uint d.</param>
		/// <param name="e">uint e.</param>
		/// <param name="f">uint f.</param>
		/// <param name="g">uint g.</param>
		/// <param name="h">uint h.</param>
        public v256(uint a, uint b, uint c, uint d, uint e, uint f, uint g, uint h)
        {
            this = default(v256);
            UInt0 = a;
            UInt1 = b;
            UInt2 = c;
            UInt3 = d;
            UInt4 = e;
            UInt5 = f;
            UInt6 = g;
            UInt7 = h;
        }

        /// <summary>
        /// Splat a single float across the v256
        /// </summary>
		/// <param name="f">Splatted float.</param>
        public v256(float f)
        {
            this = default(v256);
            Float0 = Float1 = Float2 = Float3 = Float4 = Float5 = Float6 = Float7 = f;
        }

        /// <summary>
        /// Initialize the v256 with 8 floats
        /// </summary>
		/// <param name="a">float a.</param>
		/// <param name="b">float b.</param>
		/// <param name="c">float c.</param>
		/// <param name="d">float d.</param>
		/// <param name="e">float e.</param>
		/// <param name="f">float f.</param>
		/// <param name="g">float g.</param>
		/// <param name="h">float h.</param>
        public v256(float a, float b, float c, float d, float e, float f, float g, float h)
        {
            this = default(v256);
            Float0 = a;
            Float1 = b;
            Float2 = c;
            Float3 = d;
            Float4 = e;
            Float5 = f;
            Float6 = g;
            Float7 = h;
        }

        /// <summary>
        /// Splat a single double across the v256
        /// </summary>
		/// <param name="f">Splatted double.</param>
        public v256(double f)
        {
            this = default(v256);
            Double0 = Double1 = Double2 = Double3 = f;
        }

        /// <summary>
        /// Initialize the v256 with 4 doubles
        /// </summary>
		/// <param name="a">double a.</param>
		/// <param name="b">double b.</param>
		/// <param name="c">double c.</param>
		/// <param name="d">double d.</param>
        public v256(double a, double b, double c, double d)
        {
            this = default(v256);
            Double0 = a;
            Double1 = b;
            Double2 = c;
            Double3 = d;
        }

		/// <summary>
        /// Splat a single long across the v256
        /// </summary>
		/// <param name="f">Splatted long.</param>
        public v256(long f)
        {
            this = default(v256);
            SLong0 = SLong1 = SLong2 = SLong3 = f;
        }

		/// <summary>
        /// Initialize the v256 with 4 longs
        /// </summary>
		/// <param name="a">long a.</param>
		/// <param name="b">long b.</param>
		/// <param name="c">long c.</param>
		/// <param name="d">long d.</param>
        public v256(long a, long b, long c, long d)
        {
            this = default(v256);
            SLong0 = a;
            SLong1 = b;
            SLong2 = c;
            SLong3 = d;
        }

        /// <summary>
        /// Splat a single ulong across the v256
        /// </summary>
		/// <param name="f">Splatted ulong.</param>
        public v256(ulong f)
        {
            this = default(v256);
            ULong0 = ULong1 = ULong2 = ULong3 = f;
        }

        /// <summary>
        /// Initialize the v256 with 4 ulongs
        /// </summary>
		/// <param name="a">ulong a.</param>
		/// <param name="b">ulong b.</param>
		/// <param name="c">ulong c.</param>
		/// <param name="d">ulong d.</param>
        public v256(ulong a, ulong b, ulong c, ulong d)
        {
            this = default(v256);
            ULong0 = a;
            ULong1 = b;
            ULong2 = c;
            ULong3 = d;
        }

        /// <summary>
        /// Initialize the v256 with 2 v128's
        /// </summary>
		/// <param name="lo">Low half of v128.</param>
		/// <param name="hi">High half of v128.</param>
        public v256(v128 lo, v128 hi)
        {
            this = default(v256);
            Lo128 = lo;
            Hi128 = hi;
        }
    }

}
