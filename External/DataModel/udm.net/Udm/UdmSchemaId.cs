using System;
using System.Runtime.InteropServices;

namespace Unity.DataModel;

#nullable enable

[StructLayout(LayoutKind.Sequential)]
internal struct SchemaId : IEquatable<SchemaId>
{
    internal Hash hash;
    internal static SchemaId Default => new(0, 0);
    internal SchemaId(ulong Uint64Data1, ulong Uint64Data2) => (hash.Uint64Data1, hash.Uint64Data2) = (Uint64Data1, Uint64Data2);
    internal SchemaId(Hash hashValue) => hash = hashValue;

    public override bool Equals(object? obj) => (obj is SchemaId other) ? hash.Equals(other.hash) : false;
    public bool Equals(SchemaId other) => hash.Equals(other.hash);
    public override int GetHashCode() => hash.GetHashCode();
    public static bool operator ==(SchemaId left, SchemaId right) => left.hash == right.hash;
    public static bool operator !=(SchemaId left, SchemaId right) => left.hash != right.hash;

    internal readonly bool IsValid => hash.IsValid();

    internal string ToHex() => hash.ToHex();

    internal static SchemaId FromHex(string hex) => new SchemaId(Hash.FromHex(hex));

    public override string ToString() => hash.ToHex();
}

#nullable restore
