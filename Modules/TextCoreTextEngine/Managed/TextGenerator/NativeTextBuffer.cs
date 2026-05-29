// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore
{
    [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
    internal struct NativeTextBuffer : IDisposable
    {
        NativeArray<char> m_Buffer;
        int m_Length;
        Allocator m_Allocator;

        Allocator effectiveAllocator => m_Allocator != Allocator.Invalid ? m_Allocator : Allocator.Persistent;

        public NativeArray<char> buffer => m_Buffer;

        public int length
        {
            get => m_Length;
            set => m_Length = value;
        }

        public bool isCreated => m_Buffer.IsCreated;

        public char this[int index]
        {
            get => m_Buffer[index];
            set => m_Buffer[index] = value;
        }

        /// <summary>
        /// Creates a buffer whose backing NativeArray uses <see cref="Allocator.Domain"/>
        /// so that it is automatically freed on domain unload. Use for static buffers
        /// that have no guaranteed Dispose path before domain reload.
        /// </summary>
        public static NativeTextBuffer CreateDomainScoped()
        {
            return new NativeTextBuffer { m_Allocator = Allocator.Domain };
        }

        public void EnsureCapacity(int requiredLength, bool preserveContent = false)
        {
            if (m_Buffer.IsCreated && m_Buffer.Length >= requiredLength)
                return;

            int newCapacity = m_Buffer.IsCreated ? m_Buffer.Length : 4;
            while (newCapacity < requiredLength)
                newCapacity *= 2;

            var newBuffer = new NativeArray<char>(newCapacity, effectiveAllocator, NativeArrayOptions.UninitializedMemory);
            if (m_Buffer.IsCreated)
            {
                if (preserveContent && m_Length > 0)
                    NativeArray<char>.Copy(m_Buffer, newBuffer, m_Length);
                m_Buffer.Dispose();
            }
            m_Buffer = newBuffer;
        }

        public void CopyFrom(string value)
        {
            int len = value?.Length ?? 0;
            if (len == 0)
            {
                m_Length = 0;
                return;
            }

            EnsureCapacity(len);
            for (int i = 0; i < len; i++)
                m_Buffer[i] = value[i];
            m_Length = len;
        }

        public void CopyFrom(ReadOnlySpan<char> source, int count)
        {
            if (count == 0)
            {
                m_Length = 0;
                return;
            }

            EnsureCapacity(count);
            for (int i = 0; i < count; i++)
                m_Buffer[i] = source[i];
            m_Length = count;
        }

        public string Materialize()
        {
            if (m_Length == 0)
                return string.Empty;

            var buf = m_Buffer;
            return string.Create(m_Length, buf, static (span, b) =>
            {
                for (int i = 0; i < span.Length; i++)
                    span[i] = b[i];
            });
        }

        public void Dispose()
        {
            if (m_Buffer.IsCreated)
            {
                m_Buffer.Dispose();
                m_Buffer = default;
            }
            m_Length = 0;
        }
    }
}
