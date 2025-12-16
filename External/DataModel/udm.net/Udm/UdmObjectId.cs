using System;
using System.Runtime.InteropServices;

namespace Unity.DataModel
{
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal record struct UdmObjectId(ulong Id) : IComparable<UdmObjectId>
    {
        public ulong Id = Id;
        public static implicit operator UdmObjectId(ulong value) => new(value);

        public int CompareTo(UdmObjectId other)
        {
            return Id.CompareTo(other.Id);
        }
    }
}
