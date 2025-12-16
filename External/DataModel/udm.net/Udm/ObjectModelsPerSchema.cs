#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

namespace Unity.DataModel
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ObjectModelPairEntry
    {
        internal UdmObjectId ObjectId;
        internal IntPtr Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ObjectModelsPerSchema : IEnumerable
    {
        internal ObjectModelsPerSchema(DocumentModel document, Schema Schema)
        {
            fixed (ObjectModelsPerSchema* thisPtr = &this)
            {
                UdmInterop.Instance.udm_document_model_get_objects_per_schema(document.DocumentPtr, (SchemaImpl*)Schema.SchemaPtr, thisPtr);
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

            internal Enumerator(ObjectModelsPerSchema objectModelsPerSchema)
            {
                Entries = (ObjectModelPairEntry*)objectModelsPerSchema.Entries.ToPointer();
                Index = -1;
                EntryCount = objectModelsPerSchema.EntryCount;
                objectModelPtr = (ObjectModel*)Marshal.AllocHGlobal(sizeof(ObjectModel)).ToPointer();
                objectModelPtr->ObjectId = 0;
                objectModelPtr->Accessor.Schema = objectModelsPerSchema.Schema;
                objectModelPtr->Accessor.Data = IntPtr.Zero;
                objectModelPtr->Accessor.DocumentModel = objectModelsPerSchema.DocumentModel;
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

            internal ref readonly ObjectModel Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref *objectModelPtr;
            }

            object IEnumerator.Current => Current;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
            private ObjectModel* objectModelPtr;
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
        internal DocumentModel DocumentModel;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    }
}
