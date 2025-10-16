using System;
using System.Runtime.InteropServices;
using udm_const_vector_ptr = System.IntPtr;

namespace Unity.DataModel
{
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ConstVector
{
    internal ConstVector(ConstAccessor accessor)
    {
        Field = default;
        ElementSchema = default;

        Schema schema = accessor.GetSchema();
        if (schema.IsValid() && schema.GetFlags().HasFlag(SchemaFlags.IsVector))
        {
            Field = accessor.Data;
            ElementSchema = schema.GetVectorElementSchema();
        }
    }

    internal bool IsValid()
    {
        unsafe
        {
            return Field != IntPtr.Zero && ElementSchema.IsValid();
        }
    }

    internal Schema GetElementSchema()
    {
        return ElementSchema;
    }

    internal ulong GetLength()
    {
        ThrowIfInvalid();
        return GetLengthInternalUnsafe();
    }

    private ulong GetLengthInternalUnsafe()
    {
        return ((VectorField*)Field)->Size;
    }

    internal byte* GetDataPtr()
    {
        ThrowIfInvalid();
        return GetDataPtrInternalUnsafe();
    }

    private byte* GetDataPtrInternalUnsafe()
    {
        return (byte*)Field + ((VectorField*)Field)->Location;
    }

    internal ConstAccessor ElementAt(ulong index)
    {
        ThrowIfInvalid();
        ulong length = GetLengthInternalUnsafe();

        if (index >= length)
            throw new IndexOutOfRangeException($"Index {index} is out of range. Vector size {length}");

        
        fixed (ConstVector* vectorAccessorPtr = &this)
        {
            var ptr = GetDataPtrInternalUnsafe() + ElementSchema.GetSize() * index;

            return new ConstAccessor
            {
                Schema = ElementSchema,
                Data = (IntPtr)ptr
            };
        }
    }

    internal void ThrowIfInvalid()
    {
        if (!IsValid())
            throw new InvalidOperationException("Trying to use an invalid Vector");
    }

    internal Schema ElementSchema;
    // Pointers to blittable types are not considered blittable by the bindings generator
    // internal VectorField* Field;
    internal IntPtr Field;
}
}
