using System;
using System.Runtime.InteropServices;

namespace Unity.DataModel
{
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct SchemaFieldImpl
{
    internal UTF8StringField Name;
    internal SchemaId SchemaId;
    internal ulong Offset;
    internal uint Index;
    internal uint Padding;
    internal SchemaFieldFlags Flags;
};

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct SchemaFieldKeyImpl
{
    internal uint FieldNameHash;
    internal uint Index;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct SchemaField
{
    internal bool IsValid() => SchemaFieldPtr != null;

    internal void ThrowIfInvalid()
    {
        if (!IsValid())
            throw new InvalidOperationException("Trying to use an invalid SchemaField");
    }

    internal ConstUTF8String GetName()
    {
        ThrowIfInvalid();
        unsafe
        {
            return new ConstUTF8String
            {
                Field = (IntPtr)(&SchemaFieldPtr->Name)
            };
        }
    }

    internal SchemaFieldFlags GetFlags()
    {
        ThrowIfInvalid();
        unsafe
        {
            return SchemaFieldPtr->Flags;
        }
    }

    internal UdmTypeId GetTypeId()
    {
        ThrowIfInvalid();
        return GetSchema().GetTypeId();
    }


    internal ulong GetTypeVersion()
    {
        ThrowIfInvalid();
        return GetSchema().GetTypeVersion();
    }

    internal uint GetIndex()
    {
        ThrowIfInvalid();
        unsafe
        {
            return SchemaFieldPtr->Index;
        }
    }

    internal ulong GetOffset()
    {
        ThrowIfInvalid();
        unsafe
        {
            return SchemaFieldPtr->Offset;
        }
    }

    internal uint GetPadding()
    {
        ThrowIfInvalid();
        unsafe
        {
            return SchemaFieldPtr->Padding;
        }
    }

    internal Schema GetSchema()
    {
        ThrowIfInvalid();
        unsafe
        {
            return Schema.GetOrCreateSchemaById(SchemaFieldPtr->SchemaId);
        }
    }

    public static unsafe implicit operator SchemaField(SchemaFieldImpl* ptr)
    {
        return new SchemaField
        {
            SchemaFieldPtr = ptr
        };
    }

    internal unsafe SchemaFieldImpl* SchemaFieldPtr;
}
}
