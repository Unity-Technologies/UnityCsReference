#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

namespace Unity.DataModel
{
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ConstObjectModelsPerSchema : IEnumerable
{
    internal ConstObjectModelsPerSchema(DocumentModel document, Schema Schema)
    {
        fixed (ConstObjectModelsPerSchema* thisPtr = &this)
        {
            UdmInterop.Instance.udm_document_model_get_const_objects_per_schema(document.DocumentPtr, (SchemaImpl*)Schema.SchemaPtr, thisPtr);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Enumerator : IEnumerator
    {
        public Enumerator()
        {
            objectModelPtr = default;
            Entries = null;
            Index = -1;
            EntryCount = 0;
        }

        internal Enumerator(ConstObjectModelsPerSchema objectModelsPerSchema)
        {
            Entries = (ObjectModelPairEntry*)objectModelsPerSchema.Entries.ToPointer();
            Index = -1;
            EntryCount = objectModelsPerSchema.EntryCount;
            objectModelPtr = (ConstObjectModel*)Marshal.AllocHGlobal(sizeof(ConstObjectModel)).ToPointer();
            objectModelPtr->ObjectId = 0;
            objectModelPtr->Accessor.Schema = objectModelsPerSchema.Schema;
            objectModelPtr->Accessor.Data = IntPtr.Zero;
        }

        internal void Dispose()
        {
            if (objectModelPtr != null)
            {
                Marshal.FreeHGlobal(new IntPtr(objectModelPtr));
                objectModelPtr = null;
            }
        }

        public bool MoveNext()
        {
            Index++;
            if (Index < (long)EntryCount)
            {
                objectModelPtr->ObjectId = Entries[Index].ObjectId;
                objectModelPtr->Accessor.Data = Entries[Index].Data;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            Index = -1;
        }

        internal ref readonly ConstObjectModel Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref *objectModelPtr;
        }

        object IEnumerator.Current => Current;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        private ConstObjectModel* objectModelPtr;
        private ObjectModelPairEntry* Entries;
        private long Index;
        private ulong EntryCount;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return (IEnumerator)GetEnumerator();
    }

    internal Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    internal Schema GetSchema()
    {
        return Schema;
    }

    internal ulong GetCount()
    {
        return EntryCount;
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    internal Schema Schema;
    internal IntPtr Entries;
    internal ulong EntryCount;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    internal DocumentModel DocumentModel;
}
}
