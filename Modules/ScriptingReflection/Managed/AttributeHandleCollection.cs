// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Scripting.Reflection
{
    internal unsafe struct AttributeHandleCollection : IDisposable, IEquatable<AttributeHandleCollection>
    {
        internal const int k_StackCapacity = 8;

        NativeArray<AttributeHandle> m_Handles;

        // One per query when the handles span several; each block is freed from its first entry,
        // which filtering may have dropped from m_Handles
        NativeArray<AttributeHandle> m_BlockHeads;

        // m_Handles and every block come from it
        Allocator m_Allocator;

        AttributeHandleCollection(int count, Allocator allocator)
        {
            m_Handles = new NativeArray<AttributeHandle>(count, allocator);
            m_BlockHeads = default;
            m_Allocator = allocator;
        }

        public static AttributeHandleCollection Empty => default;

        public bool Equals(AttributeHandleCollection other) => m_Handles == other.m_Handles;
        public override bool Equals(object obj) => obj is AttributeHandleCollection other && Equals(other);
        public override int GetHashCode() => m_Handles.GetHashCode();
        public static bool operator ==(AttributeHandleCollection left, AttributeHandleCollection right) => left.Equals(right);
        public static bool operator !=(AttributeHandleCollection left, AttributeHandleCollection right) => !left.Equals(right);

        public int Length => m_Handles.Length;

        public AttributeHandle this[int index] => m_Handles[index];

        public NativeArray<AttributeHandle>.Enumerator GetEnumerator()
        {
            return m_Handles.GetEnumerator();
        }

        internal IntPtr UnsafeBuffer => (IntPtr)m_Handles.GetUnsafePtr();

        internal static AttributeHandleCollection Allocate(int count, Allocator allocator)
        {
            return new AttributeHandleCollection(count, allocator);
        }

        internal static AttributeHandleCollection FromBuffer(IntPtr* handles, int count, Allocator allocator)
        {
            if (count == 0)
                return Empty;

            var collection = Allocate(count, allocator);
            for (int i = 0; i < count; i++)
                collection.m_Handles[i] = new AttributeHandle(handles[i]);

            return collection;
        }

        internal static AttributeHandleCollection FromBlocks(NativeList<AttributeHandle> handles, NativeList<AttributeHandle> blockHeads, Allocator allocator)
        {
            if (handles.Length == 0)
                return Empty;

            var collection = new AttributeHandleCollection(handles.Length, allocator);
            NativeArray<AttributeHandle>.Copy(handles.AsArray(), collection.m_Handles);
            collection.m_BlockHeads = new NativeArray<AttributeHandle>(blockHeads.AsArray(), allocator);
            return collection;
        }

        // A query allocates its block from allocator before any NativeArray can reject it
        internal static bool IsSupported(Allocator allocator) => allocator > Allocator.None && allocator < Allocator.FirstUserIndex;

        internal static void FreeBlock(AttributeHandle head, Allocator allocator)
        {
            UnsafeUtility.Free((void*)head.Value, allocator);
        }

        public void Dispose()
        {
            if (!m_Handles.IsCreated)
                return;

            if (m_BlockHeads.IsCreated)
            {
                for (int i = 0; i < m_BlockHeads.Length; i++)
                    FreeBlock(m_BlockHeads[i], m_Allocator);

                m_BlockHeads.Dispose();
            }
            else if (m_Handles.Length > 0)
            {
                FreeBlock(m_Handles[0], m_Allocator);
            }

            m_Handles.Dispose();
        }
    }
}
