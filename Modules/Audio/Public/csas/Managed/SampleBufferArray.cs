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
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct NativeSampleBuffer
    {
        public uint m_Channels;
        public SoundFormat m_Format;
        public float* m_Buffer;
        private bool m_Initialized;
    }

    internal enum SoundFormat
    {
        Raw,
        Mono,
        Stereo,
        Quad,
        Surround,
        FiveDot1,
        SevenDot1
    }

    internal unsafe struct SampleBuffer
    {
        public uint Samples { get { return m_SampleCount; } }

        public uint Channels { get { return m_NativeBuffer->m_Channels; } }

        public SoundFormat Format { get { return m_NativeBuffer->m_Format; } }

        public NativeArray<float> Buffer
        {
            get
            {
                var length = (int)(m_SampleCount * m_NativeBuffer->m_Channels);
                var nBuffer = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<float>(m_NativeBuffer->m_Buffer, length, Allocator.Invalid);

                NativeArrayUnsafeUtility.SetAtomicSafetyHandle<float>(ref nBuffer, m_Safety);

                return nBuffer;
            }
        }

        internal NativeSampleBuffer* m_NativeBuffer;
        internal uint m_SampleCount;

        internal AtomicSafetyHandle m_Safety;
    }

    internal unsafe struct SampleBufferArray
    {
        public int Count { get { return (int)m_SampleBufferCount; } }

        public SampleBuffer GetSampleBuffer(int index)
        {
            if (index < 0 || index >= m_SampleBufferCount)
                throw new ArgumentException("Index out of range (GetSampleBuffer)");

            var sBuffer = new SampleBuffer
            {
                m_NativeBuffer = &m_Buffers[index],
                m_SampleCount = m_SampleCount,
                m_Safety = m_Safety
            };

            return sBuffer;
        }

        internal uint m_SampleBufferCount;
        internal NativeSampleBuffer* m_Buffers;
        internal uint m_SampleCount;

        internal AtomicSafetyHandle m_Safety;
    }
}

