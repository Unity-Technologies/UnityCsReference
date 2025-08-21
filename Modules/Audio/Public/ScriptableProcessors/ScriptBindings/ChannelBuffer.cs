// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Audio
{
    public ref struct ChannelBuffer
    {
        public int channelCount => m_ChannelCount;
        public int frameCount => m_FrameCount;

        public float this[int channel, int frame]
        {
            // Interleaved for now.
            get { return Buffer[frame * m_ChannelCount + channel]; }
            set { Buffer[frame * m_ChannelCount + channel] = value; }
        }

        public void Clear()
        {
            Buffer.Clear();
        }

        internal Span<float> Buffer;
        int m_ChannelCount;
        int m_FrameCount;

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
