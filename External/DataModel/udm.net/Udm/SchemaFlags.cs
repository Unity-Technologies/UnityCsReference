using System;

namespace Unity.DataModel
{
    // We can't use enum with [Flags] because of bindings doesn't work in WebGL
    internal struct SchemaFlags
    {
        internal static readonly SchemaFlags None = new SchemaFlags { data = 0 };
        internal static readonly SchemaFlags HasReferenceFields = new SchemaFlags { data = 1 << 0 };
        internal static readonly SchemaFlags IsBasic = new SchemaFlags { data = 1 << 1 };
        internal static readonly SchemaFlags IsFundamental = new SchemaFlags { data = 1 << 2 };
        internal static readonly SchemaFlags IsTriviallyCopyable = new SchemaFlags { data = 1 << 3 };
        internal static readonly SchemaFlags HasDynamicData = new SchemaFlags { data = 1 << 4 };
        internal static readonly SchemaFlags IsVector = new SchemaFlags { data = 1 << 5 };
        internal static readonly SchemaFlags IsMap = new SchemaFlags { data = 1 << 6 };
        internal static readonly SchemaFlags InlineTextSerialization = new SchemaFlags { data = 1 << 7 };
        internal static readonly SchemaFlags IsManaged = new SchemaFlags { data = 1 << 8 };
        internal static readonly SchemaFlags IsFixedBuffer = new SchemaFlags { data = 1 << 9 };

        internal ulong data;

        internal bool HasFlag(SchemaFlags flags)
        {
            return ((data & flags.data) ^ flags.data) == 0;
        }

        internal void SetFlag(SchemaFlags flags)
        {
            data |= flags.data;
        }

        internal void ClearFlag(SchemaFlags flags)
        {
            data &= ~(flags.data);
        }

        public static SchemaFlags operator |(SchemaFlags left, SchemaFlags right)
        {
            return new SchemaFlags { data = left.data | right.data };
        }
    }
}
