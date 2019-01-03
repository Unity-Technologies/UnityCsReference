// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio.Tests")]

namespace Unity.Experimental.Audio
{
    [AttributeUsage(AttributeTargets.Field)]
    internal class SampleProviderArrayAttribute : Attribute
    {
        internal int size;

        public SampleProviderArrayAttribute(int sz = -1)
        {
            this.size = sz;
        }
    }

    internal unsafe partial struct DSPSampleProvider
    {
        internal void* m_SampleProvider;
    }

    internal unsafe struct SampleProvider
    {
        public enum NativeFormatType
        {
            FLOAT_BE,
            FLOAT_LE,
            PCM8,
            PCM16_BE,
            PCM16_LE,
            PCM24_BE,
            PCM24_LE,
        }

        public bool Valid
        {
            get
            {
                return m_SampleProviders != null &&
                    m_SampleProviders[m_Index].m_SampleProvider != null
                    && AtomicSafetyHandle.GetAllowReadOrWriteAccess(m_Safety)
                ;
            }
        }

        public int Read(NativeSlice<byte> destination, NativeFormatType format)
        {
            CheckValidAndThrow();
            // Not doing any format/size checks here. byte is the only valid choice for 8-bits,
            // 24-bits as well as big-endian float. Users may have reasons to want 16-bit samples
            // carried via a buffer-of-bytes, so we're being totally flexible here.
            return DSPSampleProvider.Internal_ReadUInt8FromSampleProvider(
                m_SampleProviders[m_Index], format, destination.GetUnsafePtr<byte>(),
                destination.Length);
        }

        public int Read(NativeSlice<short> destination, NativeFormatType format)
        {
            CheckValidAndThrow();
            if (format != NativeFormatType.PCM16_LE && format != NativeFormatType.PCM16_BE)
                throw new ArgumentException(
                    "Using buffer of short to capture samples of a different size.");

            return DSPSampleProvider.Internal_ReadSInt16FromSampleProvider(
                m_SampleProviders[m_Index], format, destination.GetUnsafePtr<short>(),
                destination.Length);
        }

        public int Read(NativeSlice<float> destination)
        {
            CheckValidAndThrow();
            return DSPSampleProvider.Internal_ReadFloatFromSampleProvider(
                m_SampleProviders[m_Index], destination.GetUnsafePtr<float>(), destination.Length);
        }

        public NativeFormatType NativeFormat
        {
            get { return NativeFormatType.FLOAT_LE; }
        }

        public ushort ChannelCount
        {
            get { return DSPSampleProvider.Internal_GetChannelCount(m_SampleProviders[m_Index]); }
        }

        public uint SampleRate
        {
            get { return DSPSampleProvider.Internal_GetSampleRate(m_SampleProviders[m_Index]); }
        }

        public void Release()
        {
            CheckValidAndThrow();
            m_SampleProviders[m_Index].m_SampleProvider = null;
        }

        private void CheckValidAndThrow()
        {
            if (!Valid)
                throw new InvalidOperationException("Invalid SampleProvider being used.");
        }

        internal DSPSampleProvider* m_SampleProviders;
        internal uint               m_Index;

        internal AtomicSafetyHandle m_Safety;
    }

    internal unsafe struct SampleProviderContainer<Provs> where Provs : struct, IConvertible
    {
        public int Count { get { return (int)m_SampleProviderIndicesCount; } }

        public int GetCount(Provs p)
        {
            var itemIndex = UnsafeUtility.EnumToInt(p);
            if (itemIndex < 0 || itemIndex >= m_SampleProviderIndicesCount)
                throw new IndexOutOfRangeException("itemIndex");

            int globalIndex = m_SampleProviderIndices[itemIndex];

            // Happens if the 'itemIndex'th item is an empty array.
            if (globalIndex < 0)
                return 0;

            // Find the index of the next non-empty item.
            int nextItemIndex = itemIndex + 1;
            int nextGlobalIndex = -1;
            for (; nextItemIndex < m_SampleProviderIndicesCount; ++nextItemIndex)
            {
                nextGlobalIndex = m_SampleProviderIndices[nextItemIndex];
                if (nextGlobalIndex >= 0)
                    break;
            }

            // All items after itemIndex are empty containers.
            if (nextGlobalIndex < 0)
                return (int)(m_SampleProvidersCount - globalIndex);

            return nextGlobalIndex - globalIndex;
        }

        public SampleProvider GetSampleProvider(Provs p, int arrayIndex = 0)
        {
            return GetSampleProvider(UnsafeUtility.EnumToInt(p), arrayIndex);
        }

        public SampleProvider GetSampleProvider(int itemIndex = 0, int arrayIndex = 0)
        {
            if (itemIndex < 0 || itemIndex >= m_SampleProviderIndicesCount)
                throw new IndexOutOfRangeException("itemIndex");

            int globalIndex = m_SampleProviderIndices[itemIndex];

            // Happens if the 'index'th item is an empty array.
            if (globalIndex < 0)
                throw new IndexOutOfRangeException("arrayIndex");

            globalIndex += arrayIndex;

            // Find the index of the next non-empty item.
            int nextItemIndex = itemIndex + 1;
            int nextGlobalIndex = -1;
            for (; nextItemIndex < m_SampleProviderIndicesCount; ++nextItemIndex)
            {
                nextGlobalIndex = m_SampleProviderIndices[nextItemIndex];
                if (nextGlobalIndex != -1)
                    break;
            }

            if (nextGlobalIndex == -1)
            {
                // Happens if indexing beyond the end of the last item.
                if (globalIndex >= m_SampleProvidersCount)
                    throw new IndexOutOfRangeException("arrayIndex");
            }
            else
            {
                // Happens if indexing beyond the end of the current item.
                if (globalIndex >= nextGlobalIndex)
                    throw new IndexOutOfRangeException("arrayIndex");
            }

            return new SampleProvider
            {
                m_SampleProviders = m_SampleProviders,
                m_Index = (uint)globalIndex,
                m_Safety = m_Safety
            };
        }

        internal uint m_SampleProviderIndicesCount;
        internal int* m_SampleProviderIndices;

        internal uint m_SampleProvidersCount;
        internal DSPSampleProvider* m_SampleProviders;

        internal AtomicSafetyHandle m_Safety;
    }
}

