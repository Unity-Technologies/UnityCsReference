// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.U2D.Physics
{
    internal static partial class Scripting2D
    {
        /// <summary>
        /// Internal buffer used to marshal results efficiently from the native engine.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct PhysicsBuffer : IDisposable
        {
            /// <undoc/>
            public readonly IntPtr buffer => m_Buffer;

            /// <undoc/>
            public readonly int size => m_Size;

            /// <undoc/>
            public readonly Allocator allocator => m_Allocator;

            /// <undoc/>
            public PhysicsBuffer()
            {
                m_Buffer = IntPtr.Zero;
                m_Size = 0;
                m_Allocator = Allocator.None;
            }

            /// <undoc/>
            public PhysicsBuffer(IntPtr buffer, int size, Allocator allocator)
            {
                m_Buffer = buffer;
                m_Size = size;
                m_Allocator = allocator;
            }

            /// <undoc/>
            public static unsafe PhysicsBuffer FromNativeArray<T>(NativeArray<T> nativeArray) where T : struct
            {
                return new PhysicsBuffer((IntPtr)nativeArray.GetUnsafePtr(), nativeArray.Length, Allocator.None);
            }

            /// <undoc/>
            public static unsafe PhysicsBuffer FromSpan<T>(ReadOnlySpan<T> span) where T : unmanaged
            {
                fixed (T* addr = span)
                {
                    return new PhysicsBuffer((IntPtr)addr, span.Length, Allocator.None);
                }
            }

            /// <undoc/>
            public readonly unsafe NativeArray<T> ToNativeArray<T>() where T : struct
            {
                if (m_Size == 0)
                    return new NativeArray<T>();

                var nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(dataPointer: m_Buffer.ToPointer(), length: m_Size, allocator: m_Allocator);
                var safetyHandle = (m_Allocator == Allocator.Temp) ? AtomicSafetyHandle.GetTempMemoryHandle() : AtomicSafetyHandle.Create();
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, safetyHandle);
                return nativeArray;
            }

            /// <undoc/>
            public readonly unsafe Span<T> ToSpan<T>() where T : struct
            {
                return new Span<T>(m_Buffer.ToPointer(), m_Size);
            }

            /// <undoc/>
            public readonly unsafe ReadOnlySpan<T> ToReadOnlySpan<T>() where T : struct => new ReadOnlySpan<T>(m_Buffer.ToPointer(), m_Size);

            /// <undoc/>
            public unsafe readonly T AsEngineObject<T>(int index) where T : class
            {
                if (index < 0 || index >= size)
                    throw new ArgumentOutOfRangeException("Index argument is invalid.", nameof(index));

                var entityId = UnsafeUtility.ArrayElementAsRef<EntityId>(m_Buffer.ToPointer(), index);
                return Resources.EntityIdIsValid(entityId) ? Resources.EntityIdToObject(entityId) as T : null;
            }

            /// <undoc/>
            public unsafe readonly T As<T>(int index) where T : struct
            {
                if (index < 0 || index >= size)
                    throw new ArgumentOutOfRangeException("Index argument is invalid.", nameof(index));

                return UnsafeUtility.ArrayElementAsRef<T>(m_Buffer.ToPointer(), index);
            }

            /// <summary>
            /// This should NOT be called if a NativeArray or Span are currently active and being accessed otherwise bad things will happen.
            /// Typically, the NativeArray should be disposed of but in other cases, this can be used.
            /// </summary>
            public unsafe void Dispose()
            {
                if (m_Buffer == null || m_Size == 0)
                    return;

                // Free the allocation.
                UnsafeUtility.FreeTracked(m_Buffer.ToPointer(), m_Allocator);
                m_Buffer = IntPtr.Zero;
                m_Size = 0;
                m_Allocator = Allocator.None;
            }

            /// <undoc/>
            public readonly bool isEmpty => m_Size == 0;

            /// <undoc/>
            public readonly bool isValid => !isEmpty;

            /// <undoc/>
            public readonly override string ToString() { return $"size={m_Size}, allocator={m_Allocator}"; }

            #region Internal

            IntPtr m_Buffer;
            int m_Size;
            Allocator m_Allocator;

            #endregion
        }

        /// <summary>
        /// Internal buffer pair used to marshal results efficiently from the native engine.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct PhysicsBufferPair
        {
            #region Internal

            /// <undoc/>
            public readonly PhysicsBuffer buffer1;

            /// <undoc/>
            public readonly PhysicsBuffer buffer2;

            #endregion
        }
    }
}
