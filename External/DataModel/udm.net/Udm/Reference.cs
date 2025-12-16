using System;
using System.Runtime.InteropServices;

namespace Unity.DataModel
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ReferenceField
    {
        internal ulong Index;
    }

    internal enum ReferenceType : Int32
    {
        NonAsset = 0,
        SourceAsset = 2,
        PrimaryArtifact = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe record struct Reference
    {
        internal static Reference Default => new Reference();

        public Reference()
        {
            DocumentId = new UdmGuid();
            UdmObjectId = new UdmObjectId(0);
            Type = ReferenceType.NonAsset;
            _padding = 0;
        }

        internal Reference(UdmObjectId objectId)
        {
            DocumentId = new UdmGuid();
            UdmObjectId = objectId;
            Type = ReferenceType.NonAsset;
            _padding = 0;
        }

        internal Reference(UdmGuid documentId, UdmObjectId objectId, ReferenceType type)
        {
            DocumentId = documentId;
            UdmObjectId = objectId;
            Type = type;
            _padding = 0;
        }

        internal readonly bool IsValid()
        {
            return UdmObjectId != 0;
        }

        internal readonly bool IsExternal()
        {
            return IsValid() && DocumentId.IsValid();
        }

        internal readonly bool IsInternal()
        {
            return IsValid() && !DocumentId.IsValid();
        }

        public UdmGuid DocumentId;
        public UdmObjectId UdmObjectId;
        public ReferenceType Type;

        private int _padding;
    }

    // We can't use enum with [Flags] because of bindings doesn't work in WebGL
    internal struct SchemaFieldFlags
    {
        internal static readonly SchemaFieldFlags None = new SchemaFieldFlags { data = 0 };
        internal static readonly SchemaFieldFlags IsTypeless = new SchemaFieldFlags { data = 1 << 0 };
        internal static readonly SchemaFieldFlags SerializeOnlyDuringDomainReload = new SchemaFieldFlags { data = 1 << 1 };
        internal static readonly SchemaFieldFlags DontSerialize = new SchemaFieldFlags { data = 1 << 2 };
        internal static readonly SchemaFieldFlags IsManaged = new SchemaFieldFlags { data = 1 << 3 };
        internal static readonly SchemaFieldFlags TreatAsPadding = new SchemaFieldFlags { data = 1 << 4 };
        internal ulong data;

        internal bool HasFlag(SchemaFieldFlags flags)
        {
            return ((data & flags.data) ^ flags.data) == 0;
        }

        internal void SetFlag(SchemaFieldFlags flags)
        {
            data |= flags.data;
        }

        internal void ClearFlag(SchemaFieldFlags flags)
        {
            data &= ~(flags.data);
        }

        public static SchemaFieldFlags operator |(SchemaFieldFlags left, SchemaFieldFlags right)
        {
            return new SchemaFieldFlags { data = left.data | right.data };
        }
    }

    internal struct TypeLayout
    {
        internal byte HasExplicitLayout;
        internal byte HasSequentialLayout;

        internal short OverrideAlignment;
        internal int OverrideSize;
    }
}
