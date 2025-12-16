// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

namespace Unity.Hierarchy
{
    readonly struct ReadOnlyNativeVector<T> where T : unmanaged
    {
        readonly IntPtr m_Ptr;
        readonly int m_Count;

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Count;
        }

        public ReadOnlyNativeVector(IntPtr ptr, int size)
        {
            m_Ptr = ptr;
            m_Count = size;
        }

        public ref readonly T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index < 0 || index >= m_Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                unsafe
                {
                    return ref ((T*)m_Ptr)[index];
                }
            }
        }

        public ReadOnlySpan<T> AsReadOnlySpan()
        {
            unsafe
            {
                return new ReadOnlySpan<T>((T*)m_Ptr, m_Count);
            }
        }

        public static implicit operator ReadOnlySpan<T>(ReadOnlyNativeVector<T> vector) => vector.AsReadOnlySpan();
    }
}
