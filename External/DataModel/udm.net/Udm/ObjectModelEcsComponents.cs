#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

namespace Unity.DataModel
{
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ObjectModelSchemaDataPair
{
    internal Schema Schema;
    internal IntPtr Data;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ObjectModelEcsComponents : IEnumerable
{
    internal ObjectModelEcsComponents(DocumentModel document, UdmObjectId objectId)
    {
        fixed (ObjectModelEcsComponents* thisPtr = &this)
        {
            UdmInterop.Instance.udm_document_model_get_ecs_components(document.DocumentPtr, objectId, thisPtr);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Enumerator : IEnumerator
    {
        public Enumerator()
        {
            accessorPtr = default;
            Entries = null;
            Index = -1;
            EntryCount = 0;
        }

        internal Enumerator(ObjectModelEcsComponents objectModelComponents)
        {
            Entries = (ObjectModelSchemaDataPair*)objectModelComponents.Entries.ToPointer();
            Index = -1;
            EntryCount = objectModelComponents.EntryCount;
            accessorPtr = (Accessor*)Marshal.AllocHGlobal(sizeof(Accessor)).ToPointer();
            accessorPtr->DocumentModel = objectModelComponents.DocumentModel;
            accessorPtr->Schema = default;
            accessorPtr->Data = IntPtr.Zero;
        }

        internal void Dispose()
        {
            if (accessorPtr != null)
            {
                Marshal.FreeHGlobal(new IntPtr(accessorPtr));
                accessorPtr = null;
            }
        }

        public bool MoveNext()
        {
            Index++;
            if (Index < (long)EntryCount)
            {
                accessorPtr->Data = Entries[Index].Data;
                accessorPtr->Schema = Entries[Index].Schema;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            Index = -1;
        }

        public ref readonly Accessor Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref *accessorPtr;
        }

        object IEnumerator.Current => Current;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        private Accessor* accessorPtr;
        private ObjectModelSchemaDataPair* Entries;
        private long Index;
        private ulong EntryCount;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return (IEnumerator)GetEnumerator();
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    internal ulong GetCount()
    {
        return EntryCount;
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    internal IntPtr Entries;
    internal ulong EntryCount;
    internal DocumentModel DocumentModel;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
}
}
