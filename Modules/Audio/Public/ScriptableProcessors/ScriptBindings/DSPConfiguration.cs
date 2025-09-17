// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Audio;

namespace UnityEngine.Audio
{
    /// <summary>
    /// An audio configuration containing just the info used for signal processing,
    /// in eg. <see cref="ControlContext"/> and <see cref="Processor"/>.
    /// </summary>
    /// <seealso cref="AudioConfiguration"/>
    public struct DSPConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DSPConfiguration"/> using the provided <see cref="AudioConfiguration"/>.
        /// </summary>
        /// <seealso cref="AudioSettings.GetConfiguration"/>"/>
        public DSPConfiguration(AudioConfiguration config) { m_Config = config; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DSPConfiguration"/> using the provided parameters.
        /// </summary>
        public DSPConfiguration(AudioSpeakerMode speakerMode, int sampleRate, int bufferSize)
        {
            m_Config = new ()
            {
                sampleRate = sampleRate,
                dspBufferSize = bufferSize,
                speakerMode = speakerMode
            };
        }

        /// <summary>
        /// A helper function to return the amount of channels represented by the <see cref="DSPConfiguration.speakerMode"/>.
        /// </summary>
        /// <seealso cref="AudioExtensions.ChannelCount(AudioSpeakerMode)"/>
        public readonly int channelCount => m_Config.speakerMode.ChannelCount();

        /// <summary>
        /// The batch size of samples being processed.
        /// </summary>
        /// <remarks>
        /// This determines the maximum size of the <see cref="ChannelBuffer"/> passed into processing callbacks,
        /// like <see cref="Generator.Process"/>.
        /// </remarks>
        /// <seealso cref="AudioConfiguration.dspBufferSize"/>
        public readonly int bufferSize => m_Config.dspBufferSize;

        /// <summary>
        /// The sample rate this <see cref="DSPConfiguration"/> is configured to run at.
        /// </summary>
        /// <seealso cref="AudioConfiguration.sampleRate"/>"/>
        public readonly int sampleRate => m_Config.sampleRate;

        /// <summary>
        /// The speaker mode configuration this <see cref="DSPConfiguration"/> is configured to run in.
        /// </summary>
        /// <remarks>
        /// This determines how the channel layout is mapped to speakers, and how many channels are available.
        /// </remarks>
        /// <seealso cref="AudioConfiguration.speakerMode"/>"/>
        /// <seealso cref="DSPConfiguration.channelCount"/>
        public readonly AudioSpeakerMode speakerMode => m_Config.speakerMode;

        internal readonly AudioConfiguration audioConfiguration => m_Config;

        AudioConfiguration m_Config;
    }
}
