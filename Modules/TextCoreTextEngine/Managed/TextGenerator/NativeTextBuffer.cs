// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using System;
using System.Text;
using Unity.Collections;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore
{
    [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
    internal struct NativeTextBuffer : IDisposable
    {
        NativeArray<char> m_Buffer;
        int m_Length;

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

        public void EnsureCapacity(int requiredLength, bool preserveContent = false)
        {
            if (m_Buffer.IsCreated && m_Buffer.Length >= requiredLength)
                return;

            int newCapacity = m_Buffer.IsCreated ? m_Buffer.Length : 4;
            while (newCapacity < requiredLength)
                newCapacity *= 2;

            var newBuffer = new NativeArray<char>(newCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
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

        // Chunk size (in UTF-16 code units) used to transcode/compare UTF-8 in bounded pieces, so
        // payloads of any length need neither a large stack buffer nor a heap allocation.
        const int k_Utf8ChunkSize = 256;

        [NoAutoStaticsCleanup] // BCL decoder, Reset() before each use; holds no user-code refs
        static Decoder s_Utf8Decoder;

        static Decoder GetUtf8Decoder()
        {
            var decoder = s_Utf8Decoder ??= Encoding.UTF8.GetDecoder();
            decoder.Reset();
            return decoder;
        }

        /// <summary>
        /// Transcodes a UTF-8 byte sequence directly into the buffer as UTF-16.
        /// </summary>
        /// <param name="maxLength">
        /// Maximum number of UTF-16 code units to keep, or -1 for no limit.
        /// </param>
        public int CopyFromUtf8(ReadOnlySpan<byte> utf8, int maxLength = -1)
        {
            if (utf8.IsEmpty || maxLength == 0)
            {
                m_Length = 0;
                return 0;
            }

            if (maxLength < 0 || utf8.Length <= maxLength)
            {
                EnsureCapacity(utf8.Length);
                m_Length = Encoding.UTF8.GetChars(utf8, m_Buffer.AsSpan());
                return m_Length;
            }

            if (maxLength == 1)
            {
                Span<char> first = stackalloc char[2];
                GetUtf8Decoder().Convert(utf8, first, flush: true, out _, out int firstChars, out _);

                if (firstChars == 0 || char.IsHighSurrogate(first[0]))
                {
                    m_Length = 0;
                    return 0;
                }
                EnsureCapacity(1);
                m_Buffer[0] = first[0];
                m_Length = 1;
                return 1;
            }

            EnsureCapacity(maxLength);
            GetUtf8Decoder().Convert(utf8, m_Buffer.AsSpan().Slice(0, maxLength), flush: true, out _, out int written, out _);
            m_Length = written;
            return written;
        }

        /// <summary>
        /// Returns true when decoding <paramref name="utf8"/> (clamped to <paramref name="maxLength"/>)
        /// would yield content identical to what the buffer already holds.
        /// </summary>
        public bool MatchesUtf8(ReadOnlySpan<byte> utf8, int maxLength)
        {
            int currentLength = isCreated ? m_Length : 0;

            // Compare the decoded text against the buffer in bounded chunks, so payloads of any
            // length are handled without a large stack buffer or a heap allocation.
            var decoder = GetUtf8Decoder();
            Span<char> chunk = stackalloc char[k_Utf8ChunkSize];
            int compared = 0;
            ReadOnlySpan<byte> remaining = utf8;
            while (true)
            {
                decoder.Convert(remaining, chunk, flush: remaining.IsEmpty,
                    out int bytesUsed, out int charsUsed, out bool completed);
                remaining = remaining.Slice(bytesUsed);

                for (int i = 0; i < charsUsed; i++)
                {
                    // Stop at the surrogate-safe maxLength boundary: never keep a lone high surrogate
                    if (maxLength >= 0 && (compared == maxLength ||
                        (compared == maxLength - 1 && char.IsHighSurrogate(chunk[i]))))
                        return compared == currentLength;

                    if (compared >= currentLength || m_Buffer[compared] != chunk[i])
                        return false;
                    compared++;
                }

                if (remaining.IsEmpty && completed)
                    break;
                if (bytesUsed == 0 && charsUsed == 0)
                    break; // No progress (malformed/truncated input) - treat as changed.
            }
            return compared == currentLength;
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
