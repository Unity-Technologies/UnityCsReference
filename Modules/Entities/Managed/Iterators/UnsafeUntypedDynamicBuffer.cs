// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities.LowLevel.Unsafe
{
    // DynamicBuffer<T> for an element type that is only known at runtime: the same members, minus those that need T.
    // The native mirror (Modules/Entities/Public/UnsafeUntypedDynamicBuffer.h) depends on this field layout.
    unsafe struct UnsafeUntypedDynamicBuffer
    {
        [NativeDisableUnsafePtrRestriction]
        BufferHeader* m_Buffer;
        int m_InternalCapacity;
        int m_ElementSize;
        int m_ElementAlign;

        AtomicSafetyHandle m_Safety0;
        AtomicSafetyHandle m_Safety1;
        byte m_IsReadOnly;
        byte m_UseMemoryInitPattern;
        byte m_MemoryInitPattern;

        internal UnsafeUntypedDynamicBuffer(BufferHeader* header, int internalCapacity, int elementSize, int elementAlign,
            AtomicSafetyHandle safety, AtomicSafetyHandle arrayInvalidationSafety, bool isReadOnly, bool useMemoryInitPattern, byte memoryInitPattern)
        {
            m_Buffer = header;
            m_InternalCapacity = internalCapacity;
            m_ElementSize = elementSize;
            m_ElementAlign = elementAlign;
            m_Safety0 = safety;
            m_Safety1 = arrayInvalidationSafety;
            m_IsReadOnly = (byte)(isReadOnly ? 1 : 0);
            m_UseMemoryInitPattern = (byte)(useMemoryInitPattern ? 1 : 0);
            m_MemoryInitPattern = memoryInitPattern;
        }

        public int Length
        {
            get
            {
                CheckReadAccess();
                return m_Buffer->Length;
            }
        }

        public int Capacity
        {
            get
            {
                CheckReadAccess();
                return m_Buffer->Capacity;
            }
        }

        public bool IsEmpty => Length == 0;

        public int ElementSize => m_ElementSize;

        public void ResizeUninitialized(int length)
        {
            EnsureCapacity(length);
            m_Buffer->Length = length;
        }

        public void EnsureCapacity(int length)
        {
            CheckWriteAccessAndInvalidateArrayAliases();
            BufferHeader.EnsureCapacity(m_Buffer, length, m_ElementSize, m_ElementAlign, BufferHeader.TrashMode.RetainOldData, m_UseMemoryInitPattern == 1, m_MemoryInitPattern);
        }

        public void Clear()
        {
            CheckWriteAccessAndInvalidateArrayAliases();
            m_Buffer->Length = 0;
        }

        public void* GetUnsafePtr()
        {
            CheckWriteAccess();
            return BufferHeader.GetElementPointer(m_Buffer);
        }

        public void* GetUnsafeReadOnlyPtr()
        {
            CheckReadAccess();
            return BufferHeader.GetElementPointer(m_Buffer);
        }

        void CheckReadAccess()
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety1);
        }

        void CheckWriteAccess()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety0);
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety1);
        }

        void CheckWriteAccessAndInvalidateArrayAliases()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety0);
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety1);
        }
    }
}
