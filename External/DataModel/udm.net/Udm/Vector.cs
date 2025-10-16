using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using udm_vector_ptr = System.IntPtr;

namespace Unity.DataModel
{
[StructLayout(LayoutKind.Sequential)]
internal struct VectorField
{
    internal ulong Size;
    internal long Location;

    internal unsafe byte* GetDataPtr()
    {
        var handle = GCHandle.Alloc(this, GCHandleType.Pinned);
        unsafe
        {
            byte* ptr = (byte*)handle.AddrOfPinnedObject().ToPointer();
            return ptr;
        }
    }
}

// TODO: Support Vector<T>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct Vector
{
    internal Vector(Accessor accessor)
    {
        Field = default;
        ElementSchema = default;
        DocumentModel = default;

        Schema schema = accessor.GetSchema();
        if (schema.IsValid() && schema.GetFlags().HasFlag(SchemaFlags.IsVector))
        {
            Field = accessor.Data;
            ElementSchema = schema.GetVectorElementSchema();
            DocumentModel = accessor.DocumentModel;
        }
    }

    internal bool IsValid()
{
        return Field != IntPtr.Zero && ElementSchema.IsValid();
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

    private unsafe byte* GetDataPtrInternalUnsafe()
    {
        return ((VectorField*)Field)->GetDataPtr();
    }

    internal void Clear()
    {
        ThrowIfInvalid();

        fixed (Vector* vectorAccessorPtr = &this)
        {
            UdmInterop.Instance.udm_vector_clear(vectorAccessorPtr);
        }
    }

    internal void Reserve(ulong capacity)
    {
        ThrowIfInvalid();

        fixed (Vector* vectorAccessorPtr = &this)
        {
            UdmInterop.Instance.udm_vector_reserve(vectorAccessorPtr, capacity);
        }
    }

    internal Accessor Insert(ConstAccessor accessor, ulong index)
    {
        ThrowIfInvalid();
        accessor.ThrowIfInvalid();

        ulong length = GetLengthInternalUnsafe();

        if (index > length)
            throw new IndexOutOfRangeException($"Index {index} is out of range. Vector size {length}");

        fixed (Vector* vectorAccessorPtr = &this)
        {
            var ptr = UdmInterop.Instance.udm_vector_insert_uninitialized(vectorAccessorPtr, index);
            var elementAccessor = new Accessor
            {
                Schema = ElementSchema,
                Data = ptr,
                DocumentModel = DocumentModel
            };

            UdmInterop.Instance.udm_accessor_initialize(&elementAccessor, &accessor);
            return elementAccessor;
        }
    }

    internal Accessor Insert(ulong index)
    {
        ThrowIfInvalid();

        ulong length = GetLengthInternalUnsafe();

        if (index > length)
            throw new IndexOutOfRangeException($"Index {index} is out of range. Vector size {length}");

        fixed (Vector* vectorAccessorPtr = &this)
        {
            var ptr = UdmInterop.Instance.udm_vector_insert_uninitialized(vectorAccessorPtr, index);
            var elementAccessor = new Accessor
            {
                Schema = ElementSchema,
                Data = ptr,
                DocumentModel = DocumentModel
            };
            var schemaAccessor = ElementSchema.GetAccessor();

            UdmInterop.Instance.udm_accessor_initialize(&elementAccessor, &schemaAccessor);
            return elementAccessor;
        }
    }

    internal void RemoveAt(ulong index)
    {
        ThrowIfInvalid();

        fixed (Vector* vectorAccessorPtr = &this)
        {
            UdmInterop.Instance.udm_vector_erase(vectorAccessorPtr, index);
        }
    }

    internal void Resize(ulong length)
    {
        ThrowIfInvalid();

        ulong oldLength = GetLengthInternalUnsafe();
        var accessor = ElementSchema.GetAccessor();

        fixed (Vector* vectorAccessorPtr = &this)
        {
            UdmInterop.Instance.udm_vector_resize_uninitialized(vectorAccessorPtr, length);

            var dataPtr = GetDataPtrInternalUnsafe();
            for (ulong index = oldLength; index < length; ++index)
            {
                var ptr = dataPtr + ElementSchema.GetSize() * index;
                var elementAccessor = new Accessor
                {
                    Schema = ElementSchema,
                    Data = (IntPtr)ptr,
                    DocumentModel = DocumentModel
                };

                UdmInterop.Instance.udm_accessor_initialize(&elementAccessor, &accessor);
            }
        }
    }

    internal Accessor Add(ConstAccessor accessor)
    {
        ThrowIfInvalid();
        accessor.ThrowIfInvalid();

        fixed (Vector* vectorAccessorPtr = &this)
        {
            var ptr = UdmInterop.Instance.udm_vector_push_back_uninitialized(vectorAccessorPtr);
            var elementAccessor = new Accessor
            {
                Schema = ElementSchema,
                Data = ptr,
                DocumentModel = DocumentModel
            };

            UdmInterop.Instance.udm_accessor_initialize(&elementAccessor, &accessor);
            return elementAccessor;
        }
    }

    internal Accessor Add()
    {
        ThrowIfInvalid();

        fixed (Vector* vectorAccessorPtr = &this)
        {
            var ptr = UdmInterop.Instance.udm_vector_push_back_uninitialized(vectorAccessorPtr);
            var elementAccessor = new Accessor
            {
                Schema = ElementSchema,
                Data = ptr,
                DocumentModel = DocumentModel
            };
            var schemaAccessor = ElementSchema.GetAccessor();

            UdmInterop.Instance.udm_accessor_initialize(&elementAccessor, &schemaAccessor);
            return elementAccessor;
        }
    }

    private static void ThrowTypeIdMismatchException() => throw new InvalidOperationException("You tried adding to a vector of a different type.");

    internal Accessor ElementAt(ulong index)
    {
        ThrowIfInvalid();
        ulong length = GetLengthInternalUnsafe();

        if (index >= length)
            throw new IndexOutOfRangeException($"Index {index} is out of range. Vector size {length}");

        fixed (Vector* vectorAccessorPtr = &this)
        {
            var ptr = GetDataPtrInternalUnsafe() + ElementSchema.GetSize() * index;
            return new Accessor
            {
                Schema = ElementSchema,
                Data = (IntPtr)ptr,
                DocumentModel = DocumentModel
            };
        }
    }

    internal void Assign(void* data, ulong size)
    {
        ThrowIfInvalid();
        fixed (Vector* vectorAccessorPtr = &this)
        {
            UdmInterop.Instance.udm_vector_assign(vectorAccessorPtr, data, size);
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
    internal DocumentModel DocumentModel;
}
}
