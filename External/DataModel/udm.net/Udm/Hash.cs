using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Unity.DataModel;

[StructLayout(LayoutKind.Sequential)]
internal unsafe record struct Hash(ulong Uint64Data1, ulong Uint64Data2)
{
    internal static Hash Default => new(0, 0);

    private const int HashAsStringSize = 32;

    internal readonly bool IsValid()
    {
        return this != Default;
    }

    private static ulong ByteSwap(ulong value)
    {
        return BinaryPrimitives.ReverseEndianness(value);
    }

    internal string ToHex()
    {
        return string.Format("{0:x16}{1:x16}", ByteSwap(Uint64Data1), ByteSwap(Uint64Data2));
    }

    internal static Hash FromHex(string hex)
    {
        if (hex.Length != HashAsStringSize)
            throw new InvalidOperationException("The hex parameter needs to be exactly 32 chars");
        var uint64Data1 = ulong.Parse(hex.AsSpan(0, 16), System.Globalization.NumberStyles.HexNumber);
        var uint64Data2 = ulong.Parse(hex.AsSpan(16, 16), System.Globalization.NumberStyles.HexNumber);
        return new Hash(ByteSwap(uint64Data1), ByteSwap(uint64Data2));
    }

    internal ulong Uint64Data1 = Uint64Data1;
    internal ulong Uint64Data2 = Uint64Data2;
}
