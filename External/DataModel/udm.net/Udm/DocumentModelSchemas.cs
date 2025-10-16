#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using udm_document_model_ptr = System.IntPtr;

namespace Unity.DataModel
{
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DocumentModelSchemas : IEnumerable
{
    internal DocumentModelSchemas(DocumentModel doc)
    {
        document = doc;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Enumerator : IEnumerator, IDisposable
    {
        public Enumerator()
        {
            first = true;
            iterator = null;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Iterator
        {
            internal IntPtr data_structure;
            internal IntPtr iterator;
            internal Schema schema;
        }

        internal Enumerator(udm_document_model_ptr documentPtr)
        {
            first = true;
            iterator = UdmInterop.Instance.udm_document_model_schema_iterator_new(documentPtr);
        }

        public void Dispose()
        {
            if (iterator != null)
            {
                UdmInterop.Instance.udm_document_model_schema_iterator_delete(iterator);
                iterator = null;
            }
        }

        public bool MoveNext()
        {
            if (!first)
            {
                UdmInterop.Instance.udm_document_model_schema_iterator_next(iterator);
            }
            else
                first = false;
            return iterator->schema.SchemaPtr != IntPtr.Zero;
        }

        internal bool Valid()
        {
            return iterator->schema.SchemaPtr != IntPtr.Zero;
        }

        public void Reset()
        {
            UdmInterop.Instance.udm_document_model_schema_iterator_reset(iterator);
            first = true;
        }

        public ref readonly Schema Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref iterator->schema;
        }

        object IEnumerator.Current => Current;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        // After profiling, having a pointer to dynamically allocated memory is faster in release than having just the field and fix its address on every call.
        // This whole implementation will be revisited once we get a map implementation that can be used in C# and C++
        // private Iterator iterator;
        private Iterator* iterator;
        private bool first = true;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return (IEnumerator)GetEnumerator();
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(document.DocumentPtr);
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    internal DocumentModel document;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
}
}
