using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Unity.DataModel
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe record struct UdmGuid(uint UInt32Data1, uint UInt32Data2, uint UInt32Data3, uint UInt32Data4)
    {
        internal static UdmGuid Default => new(0, 0, 0, 0);

        private const int GuidAsStringSize = 32;

        internal readonly bool IsValid()
        {
            return this != Default;
        }

        static readonly int[] LiteralToHex;

        static UdmGuid()
        {
            int size = 'f' - '0' + 1;
            LiteralToHex = new int[size];

            for (int i = 0; i < size; i++)
            {
                char c = (char)('0' + i);

                if (c >= '0' && c <= '9')
                    LiteralToHex[i] = c - '0';
                else if (c >= 'A' && c <= 'F')
                    LiteralToHex[i] = c - 'A' + 10;
                else if (c >= 'a' && c <= 'f')
                    LiteralToHex[i] = c - 'a' + 10;
                else
                    LiteralToHex[i] = -1;
            }
        }

        static readonly char[] HexToLiteral = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        internal string ToHex()
        {
            return string.Create(32, this, (chars, g) =>
            {
                // adapted from GUIDToString (Runtime/Utilities/UnityGUID.cpp)
                uint* p = &g.UInt32Data1;
                {
                    for (var i = 0; i < 4; ++i)
                    {
                        for (var j = 8; j-- > 0;)
                        {
                            var cur = p[i];
                            cur >>= j * 4;
                            cur &= 0xF;
                            chars[i * 8 + j] = HexToLiteral[cur];
                        }
                    }
                }
            });
        }

        internal static UdmGuid FromHex(string hex)
        {
            if (hex.Length != GuidAsStringSize)
                throw new InvalidOperationException("The hex parameter needs to be exactly 32 chars");

            UdmGuid guid = default;

            var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref guid, 1));
            for (var i = 0; i < 16; ++i)
            {
                var b0 = LiteralToHex[hex[i * 2] - '0'];
                var b1 = LiteralToHex[hex[i * 2 + 1] - '0'];
                bytes[i] = (byte)(b0 | b1 << 4);
            }

            return guid;
        }

        public readonly uint UInt32Data1 = UInt32Data1;
        public readonly uint UInt32Data2 = UInt32Data2;
        public readonly uint UInt32Data3 = UInt32Data3;
        public readonly uint UInt32Data4 = UInt32Data4;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly record struct ConstUdmGuid
    {
        internal readonly UdmGuid _udmGuid;
    }
}
