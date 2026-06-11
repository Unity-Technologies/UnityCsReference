// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.UIElements.UIR
{
    // Element-strided buffer for data whose layout is only known at runtime.
    //
    // NativeArray<T>/NativeSlice<T> bake the element type into the generic parameter. When the stride is
    // dynamic (e.g. UIR custom vertex extras, where the per-vertex layout depends on which channels the
    // panel opted into), the only escape is NativeArray<byte> + a separately-carried `stride` int. From
    // then on, `Length` means bytes (not elements), every `start` and `count` has to be multiplied by the
    // stride at the call site, and forgetting that multiplication produces a buffer overrun that the type
    // system can't catch. The stride and the buffer travel through call sites independently, which makes
    // refactors brittle.
    //
    // RawArray owns the buffer and the stride together. `Length` is in elements, the indexer returns
    // `IntPtr` at `base + index*stride`, and `Slice(start, count)` takes element-space arguments — there
    // is no byte/element ambiguity to confuse. Use `ByteLength` when you genuinely need the byte count
    // (e.g. for MemCpy). For statically-typed strides (vertex/index buffers where `sizeof(T) == stride`),
    // `SliceAs<T>` bridges back to a typed `NativeSlice<T>`.
    struct RawArray : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        unsafe byte* m_Buffer;
        int m_Length;
        readonly int m_Stride;
        MemoryLabel m_Label;

        AtomicSafetyHandle m_Safety;
        static int s_StaticSafetyId;

        public int Length { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_Length; }
        public int Stride { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_Stride; }
        public int ByteLength { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_Length * m_Stride; }

        public unsafe RawArray(int length, int stride, MemoryLabel label)
        {
            Debug.Assert(length >= 0);
            Debug.Assert(stride > 0);

            m_Length = length;
            m_Stride = stride;
            m_Label = label;
            m_Buffer = length == 0
                ? null
                : (byte*)UnsafeUtility.MallocTracked((long)length * stride, 16, label, 0);

            m_Safety = AtomicSafetyHandle.Create();
            if (s_StaticSafetyId == 0)
                s_StaticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<RawArray>();
            AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, s_StaticSafetyId);
        }

        public unsafe IntPtr this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                if ((uint)index >= (uint)m_Length)
                    throw new IndexOutOfRangeException($"Index {index} is out of range [0, {m_Length}).");
                return (IntPtr)(m_Buffer + index * m_Stride);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe IntPtr GetUnsafePtr()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            return (IntPtr)m_Buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe RawSlice Slice(int start, int count)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            if (start < 0 || count < 0 || start + count > m_Length)
                throw new ArgumentOutOfRangeException($"Slice({start}, {count}) is out of range [0, {m_Length}].");
            return new RawSlice(m_Buffer + start * m_Stride, count, m_Stride, m_Safety);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<T> SliceAs<T>() where T : struct => SliceAs<T>(0, m_Length);

        public unsafe NativeSlice<T> SliceAs<T>(int start, int count) where T : struct
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            if (UnsafeUtility.SizeOf<T>() > m_Stride)
                throw new InvalidOperationException($"SliceAs<{typeof(T).Name}>: sizeof({typeof(T).Name})={UnsafeUtility.SizeOf<T>()} is larger than RawArray stride {m_Stride}.");
            if (start < 0 || count < 0 || start + count > m_Length)
                throw new ArgumentOutOfRangeException($"SliceAs({start}, {count}) is out of range [0, {m_Length}].");
            var slice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(m_Buffer + start * m_Stride, m_Stride, count);
            NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice, m_Safety);
            return slice;
        }

        public unsafe void Dispose()
        {
            if (m_Buffer != null)
            {
                UnsafeUtility.FreeTracked(m_Buffer, m_Label);
                m_Buffer = null;
            }
            m_Length = 0;

            AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
            AtomicSafetyHandle.Release(m_Safety);
        }
    }

    // Non-owning view into a RawArray (or a sub-slice). Like RawArray, carries its stride so `Length` is
    // unambiguously in elements (not bytes) and the indexer returns `IntPtr` at the requested element.
    // Default-constructed slices are empty (`IsEmpty == true`).
    struct RawSlice
    {
        [NativeDisableUnsafePtrRestriction]
        readonly unsafe byte* m_Buffer;
        readonly int m_Length;
        readonly int m_Stride;

        AtomicSafetyHandle m_Safety;

        public int Length { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_Length; }
        public int Stride { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_Stride; }
        public int ByteLength { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_Length * m_Stride; }
        public bool IsEmpty { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_Length == 0; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe RawSlice(byte* buffer, int length, int stride, AtomicSafetyHandle safety)
        {
            m_Buffer = buffer;
            m_Length = length;
            m_Stride = stride;
            m_Safety = safety;
        }

        public unsafe IntPtr this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                if ((uint)index >= (uint)m_Length)
                    throw new IndexOutOfRangeException($"Index {index} is out of range [0, {m_Length}).");
                return (IntPtr)(m_Buffer + index * m_Stride);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe IntPtr GetUnsafePtr()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            return (IntPtr)m_Buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe IntPtr GetUnsafeReadOnlyPtr()
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return (IntPtr)m_Buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe RawSlice Slice(int start, int count)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            if (start < 0 || count < 0 || start + count > m_Length)
                throw new ArgumentOutOfRangeException($"Slice({start}, {count}) is out of range [0, {m_Length}].");
            return new RawSlice(m_Buffer + start * m_Stride, count, m_Stride, m_Safety);
        }
    }
}
