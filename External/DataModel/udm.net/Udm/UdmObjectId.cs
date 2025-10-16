using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace Unity.DataModel
{
[UsedByNativeCode]
[StructLayout(LayoutKind.Sequential)]
[Serializable]
internal record struct UdmObjectId(ulong Id) : IComparable<UdmObjectId>
{
    internal ulong Id = Id;
    public static implicit operator UdmObjectId(ulong value) => new(value);

    public int CompareTo(UdmObjectId other)
    {
        return Id.CompareTo(other.Id);
    }
}
}
