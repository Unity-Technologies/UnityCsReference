// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Audio;

namespace UnityEngine.Audio
{
    /// <summary>
    /// An audio configuration containing just the info used for signal processing, in eg. <see cref="ControlContext"/> and <see cref="Processor"/>.
    /// </summary>
    public struct DSPConfiguration
    {
        public DSPConfiguration(AudioConfiguration config) { m_Config = config; }
        public DSPConfiguration(AudioSpeakerMode speakerMode, int sampleRate, int bufferSize)
        {
            m_Config = new ()
            {
                sampleRate = sampleRate,
                dspBufferSize = bufferSize,
                speakerMode = speakerMode
            };
        }

        public readonly int channelCount => m_Config.speakerMode.ChannelCount();
        public readonly int bufferSize => m_Config.dspBufferSize;
        public readonly int sampleRate => m_Config.sampleRate;
        public readonly AudioSpeakerMode speakerMode => m_Config.speakerMode;

        internal readonly AudioConfiguration audioConfiguration => m_Config;

        AudioConfiguration m_Config;
    }
}
