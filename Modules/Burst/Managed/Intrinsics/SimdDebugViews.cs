// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unity.Burst.Intrinsics
{
    internal unsafe class V64DebugView
    {
        v64 m_Value;

        public V64DebugView(v64 value)
        {
            m_Value = value;
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe byte[] Byte
        {
            get
            {
                return new byte[]
                {
                    m_Value.Byte0, m_Value.Byte1, m_Value.Byte2, m_Value.Byte3,
                    m_Value.Byte4, m_Value.Byte5, m_Value.Byte6, m_Value.Byte7,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe sbyte[] SByte
        {
            get
            {
                return new sbyte[]
                {
                    m_Value.SByte0, m_Value.SByte1, m_Value.SByte2, m_Value.SByte3,
                    m_Value.SByte4, m_Value.SByte5, m_Value.SByte6, m_Value.SByte7,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe ushort[] UShort
        {
            get
            {
                return new ushort[]
                {
                    m_Value.UShort0, m_Value.UShort1, m_Value.UShort2, m_Value.UShort3,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe short[] SShort
        {
            get
            {
                return new short[]
                {
                    m_Value.SShort0, m_Value.SShort1, m_Value.SShort2, m_Value.SShort3,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe uint[] UInt
        {
            get
            {
                return new uint[]
                {
                    m_Value.UInt0, m_Value.UInt1,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe int[] SInt
        {
            get
            {
                return new int[]
                {
                    m_Value.SInt0, m_Value.SInt1,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe float[] Float
        {
            get
            {
                return new float[]
                {
                    m_Value.Float0, m_Value.Float1,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe long[] SLong
        {
            get
            {
                return new long[]
                {
                    m_Value.SLong0,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe ulong[] ULong
        {
            get
            {
                return new ulong[]
                {
                    m_Value.ULong0,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe double[] Double
        {
            get
            {
                return new double[]
                {
                    m_Value.Double0,
                };
            }
        }
    }

    internal unsafe class V128DebugView
    {
        v128 m_Value;

        public V128DebugView(v128 value)
        {
            m_Value = value;
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe byte[] Byte
        {
            get
            {
                return new byte[]
                {
                    m_Value.Byte0, m_Value.Byte1, m_Value.Byte2, m_Value.Byte3,
                    m_Value.Byte4, m_Value.Byte5, m_Value.Byte6, m_Value.Byte7,
                    m_Value.Byte8, m_Value.Byte9, m_Value.Byte10, m_Value.Byte11,
                    m_Value.Byte12, m_Value.Byte13, m_Value.Byte14, m_Value.Byte15,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe sbyte[] SByte
        {
            get
            {
                return new sbyte[]
                {
                    m_Value.SByte0, m_Value.SByte1, m_Value.SByte2, m_Value.SByte3,
                    m_Value.SByte4, m_Value.SByte5, m_Value.SByte6, m_Value.SByte7,
                    m_Value.SByte8, m_Value.SByte9, m_Value.SByte10, m_Value.SByte11,
                    m_Value.SByte12, m_Value.SByte13, m_Value.SByte14, m_Value.SByte15,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe ushort[] UShort
        {
            get
            {
                return new ushort[]
                {
                    m_Value.UShort0, m_Value.UShort1, m_Value.UShort2, m_Value.UShort3,
                    m_Value.UShort4, m_Value.UShort5, m_Value.UShort6, m_Value.UShort7,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe short[] SShort
        {
            get
            {
                return new short[]
                {
                    m_Value.SShort0, m_Value.SShort1, m_Value.SShort2, m_Value.SShort3,
                    m_Value.SShort4, m_Value.SShort5, m_Value.SShort6, m_Value.SShort7,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe uint[] UInt
        {
            get
            {
                return new uint[]
                {
                    m_Value.UInt0, m_Value.UInt1, m_Value.UInt2, m_Value.UInt3,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe int[] SInt
        {
            get
            {
                return new int[]
                {
                    m_Value.SInt0, m_Value.SInt1, m_Value.SInt2, m_Value.SInt3,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe float[] Float
        {
            get
            {
                return new float[]
                {
                    m_Value.Float0, m_Value.Float1, m_Value.Float2, m_Value.Float3,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe long[] SLong
        {
            get
            {
                return new long[]
                {
                    m_Value.SLong0, m_Value.SLong1,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe ulong[] ULong
        {
            get
            {
                return new ulong[]
                {
                    m_Value.ULong0, m_Value.ULong1,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe double[] Double
        {
            get
            {
                return new double[]
                {
                    m_Value.Double0, m_Value.Double1,
                };
            }
        }
    }

    internal unsafe class V256DebugView
    {
        v256 m_Value;

        public V256DebugView(v256 value)
        {
            m_Value = value;
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe byte[] Byte
        {
            get
            {
                return new byte[]
                {
                    m_Value.Byte0, m_Value.Byte1, m_Value.Byte2, m_Value.Byte3,
                    m_Value.Byte4, m_Value.Byte5, m_Value.Byte6, m_Value.Byte7,
                    m_Value.Byte8, m_Value.Byte9, m_Value.Byte10, m_Value.Byte11,
                    m_Value.Byte12, m_Value.Byte13, m_Value.Byte14, m_Value.Byte15,
                    m_Value.Byte16, m_Value.Byte17, m_Value.Byte18, m_Value.Byte19,
                    m_Value.Byte20, m_Value.Byte21, m_Value.Byte22, m_Value.Byte23,
                    m_Value.Byte24, m_Value.Byte25, m_Value.Byte26, m_Value.Byte27,
                    m_Value.Byte28, m_Value.Byte29, m_Value.Byte30, m_Value.Byte31,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe sbyte[] SByte
        {
            get
            {
                return new sbyte[]
                {
                    m_Value.SByte0, m_Value.SByte1, m_Value.SByte2, m_Value.SByte3,
                    m_Value.SByte4, m_Value.SByte5, m_Value.SByte6, m_Value.SByte7,
                    m_Value.SByte8, m_Value.SByte9, m_Value.SByte10, m_Value.SByte11,
                    m_Value.SByte12, m_Value.SByte13, m_Value.SByte14, m_Value.SByte15,
                    m_Value.SByte16, m_Value.SByte17, m_Value.SByte18, m_Value.SByte19,
                    m_Value.SByte20, m_Value.SByte21, m_Value.SByte22, m_Value.SByte23,
                    m_Value.SByte24, m_Value.SByte25, m_Value.SByte26, m_Value.SByte27,
                    m_Value.SByte28, m_Value.SByte29, m_Value.SByte30, m_Value.SByte31,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe ushort[] UShort
        {
            get
            {
                return new ushort[]
                {
                    m_Value.UShort0, m_Value.UShort1, m_Value.UShort2, m_Value.UShort3,
                    m_Value.UShort4, m_Value.UShort5, m_Value.UShort6, m_Value.UShort7,
                    m_Value.UShort8, m_Value.UShort9, m_Value.UShort10, m_Value.UShort11,
                    m_Value.UShort12, m_Value.UShort13, m_Value.UShort14, m_Value.UShort15,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe short[] SShort
        {
            get
            {
                return new short[]
                {
                    m_Value.SShort0, m_Value.SShort1, m_Value.SShort2, m_Value.SShort3,
                    m_Value.SShort4, m_Value.SShort5, m_Value.SShort6, m_Value.SShort7,
                    m_Value.SShort8, m_Value.SShort9, m_Value.SShort10, m_Value.SShort11,
                    m_Value.SShort12, m_Value.SShort13, m_Value.SShort14, m_Value.SShort15,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe uint[] UInt
        {
            get
            {
                return new uint[]
                {
                    m_Value.UInt0, m_Value.UInt1, m_Value.UInt2, m_Value.UInt3,
                    m_Value.UInt4, m_Value.UInt5, m_Value.UInt6, m_Value.UInt7,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe int[] SInt
        {
            get
            {
                return new int[]
                {
                    m_Value.SInt0, m_Value.SInt1, m_Value.SInt2, m_Value.SInt3,
                    m_Value.SInt4, m_Value.SInt5, m_Value.SInt6, m_Value.SInt7,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe float[] Float
        {
            get
            {
                return new float[]
                {
                    m_Value.Float0, m_Value.Float1, m_Value.Float2, m_Value.Float3,
                    m_Value.Float4, m_Value.Float5, m_Value.Float6, m_Value.Float7,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe long[] SLong
        {
            get
            {
                return new long[]
                {
                    m_Value.SLong0, m_Value.SLong1, m_Value.SLong2, m_Value.SLong3,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe ulong[] ULong
        {
            get
            {
                return new ulong[]
                {
                    m_Value.ULong0, m_Value.ULong1, m_Value.ULong2, m_Value.ULong3,
                };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe double[] Double
        {
            get
            {
                return new double[]
                {
                    m_Value.Double0, m_Value.Double1, m_Value.Double2, m_Value.Double3,
                };
            }
        }
    }
}
