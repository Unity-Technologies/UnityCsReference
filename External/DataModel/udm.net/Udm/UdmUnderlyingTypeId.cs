using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Unity.DataModel
{
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal struct UdmUnderlyingTypeId: IEquatable<UdmUnderlyingTypeId>
    {
        // Add traditional constructor
        internal UdmUnderlyingTypeId(int id)
        {
            Id = id;
        }

        // Should stay in sync with the definition in udm.h
        internal static readonly UdmUnderlyingTypeId Invalid    = -1;
        internal static readonly UdmUnderlyingTypeId Int8       = -100;
        internal static readonly UdmUnderlyingTypeId UInt8      = -101;
        internal static readonly UdmUnderlyingTypeId Int16      = -102;
        internal static readonly UdmUnderlyingTypeId UInt16     = -103;
        internal static readonly UdmUnderlyingTypeId Int32      = -104;
        internal static readonly UdmUnderlyingTypeId UInt32     = -105;
        internal static readonly UdmUnderlyingTypeId Int64      = -106;
        internal static readonly UdmUnderlyingTypeId UInt64     = -107;
        internal static readonly UdmUnderlyingTypeId Float      = -108;
        internal static readonly UdmUnderlyingTypeId Double     = -109;
        internal static readonly UdmUnderlyingTypeId Hash       = -110;
        internal static readonly UdmUnderlyingTypeId Guid       = -111;
        internal static readonly UdmUnderlyingTypeId Reference  = -112;
        internal static readonly UdmUnderlyingTypeId Utf8String = -113;

        internal readonly int Id;

        public static implicit operator UdmUnderlyingTypeId(int value) => new(value);

        internal bool IsValid => Id != Invalid.Id; 

        public bool Equals(UdmUnderlyingTypeId other)
        {
            if (!IsValid || !other.IsValid)
                return false;

            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (obj is UdmUnderlyingTypeId other)
                return Equals(other);

            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(UdmUnderlyingTypeId lhs, UdmUnderlyingTypeId rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(UdmUnderlyingTypeId lhs, UdmUnderlyingTypeId rhs)
        {
            return !lhs.Equals(rhs);
        }
    }
}
