// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Audio
{
    /// <summary>
    /// Represents a multi-channel audio buffer, allowing channel/frame-based access to the audio samples.
    /// </summary>
    /// <remarks>
    /// A <see cref="ChannelBuffer"/> provides a uniform interface for audio processing code regardless of how the samples are stored, internally.
    /// When using such a buffer together with Unity APIs, Unity will interpret the buffer layout as get/set through the indexer.
    /// </remarks>
    public ref struct ChannelBuffer
    {
        /// <summary>
        /// Gets the number of audio channels represented in the buffer.
        /// </summary>
        public int channelCount => m_ChannelCount;

        /// <summary>
        /// Gets the number of frame indices available in the buffer.
        /// </summary>
        /// <remarks>
        /// This is also understood as the amount of samples per channel.
        /// </remarks>
        public int frameCount => m_FrameCount;

        /// <summary>
        /// Gets or sets the sample value at the specified channel and frame.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown if channel or frame are outside valid bounds.
        /// </exception>
        public float this[int channel, int frame]
        {
            // Interleaved for now.
            get { return Buffer[frame * m_ChannelCount + channel]; }
            set { Buffer[frame * m_ChannelCount + channel] = value; }
        }

        internal Span<float> Buffer;
        int m_ChannelCount;
        int m_FrameCount;

        /// <summary>
        /// Sets all samples in the buffer to zero.
        /// </summary>
        public void Clear()
        {
            Buffer.Clear();
        }

        /// <summary>
        /// Creates a new <see cref="ChannelBuffer"/> instance using the <paramref name="buffer"/> as a backing memory store.
        /// </summary>
        /// <remarks>
        /// Setting content using <see cref="this[int, int]"/> will be reflected if another <see cref="ChannelBuffer"/> is created
        /// and the same indices are being read back. No other guarantees are given with respect to layout / packing.
        /// </remarks>
        /// <param name="buffer">A span of floats that will work as the backing buffer.</param>
        /// <param name="channels">The number of audio channels in the buffer</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="channels"/> is less than 1.</exception>
        public ChannelBuffer(Span<float> buffer, int channels)
        {
            if (channels < 1)
                throw new ArgumentException($"{nameof(channels)} must be positive and non-zero");

            Buffer = buffer;
            m_ChannelCount = channels;
            m_FrameCount = buffer.Length / channels;
        }
    }
}
