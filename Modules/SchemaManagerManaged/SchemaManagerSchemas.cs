// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine;
using Unity.DataModel;

namespace UnityEngine
{
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct SchemaManagerSchemas : IEnumerable
{
    private IntPtr _schemaManagersPtr;
    public SchemaManagerSchemas(IntPtr schemaManagerPtr)
    {
        _schemaManagersPtr = schemaManagerPtr;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Enumerator : IEnumerator, IDisposable
    {
        public Enumerator(IntPtr schemaManager)
        {
            first = true;
            iterator = SchemaManagerNative.schema_iterator_new(schemaManager);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Iterator
        {
            public IntPtr data_structure;
            public IntPtr iterator;
            public Schema schema;
        }

        public void Dispose()
        {
            if (iterator != null)
            {
                SchemaManagerNative.schema_iterator_delete(iterator);
                iterator = null;
            }
        }

        public bool MoveNext()
        {
            if (!first)
            {
                SchemaManagerNative.schema_iterator_next(iterator);
            }
            else
                first = false;
            return iterator->schema.IsValid();
        }

        public bool Valid()
        {
            return iterator->schema.IsValid();
        }

        public void Reset()
        {
            SchemaManagerNative.schema_iterator_reset(iterator);
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
        return new Enumerator(_schemaManagersPtr);
    }
}
}
