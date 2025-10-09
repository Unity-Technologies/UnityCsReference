// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Audio;

namespace UnityEngine.Audio
{
    /// <summary>
    /// An audio format containing information required for signal processing in,
    /// for example, <see cref="ControlContext"/> and <see cref="ProcessorInstance"/>.
    /// </summary>
    /// <seealso cref="AudioConfiguration"/>
    public struct AudioFormat
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioFormat"/> using the provided <see cref="AudioConfiguration"/>.
        /// </summary>
        /// <seealso cref="AudioSettings.GetConfiguration"/>"/>
        public AudioFormat(AudioConfiguration config) { m_Config = config; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioFormat"/> using the provided parameters.
        /// </summary>
        public AudioFormat(AudioSpeakerMode speakerMode, int sampleRate, int bufferSize)
        {
            m_Config = new ()
            {
                sampleRate = sampleRate,
                dspBufferSize = bufferSize,
                speakerMode = speakerMode
            };
        }

        /// <summary>
        /// A helper function to return the amount of channels represented by the <see cref="AudioFormat.speakerMode"/>.
        /// </summary>
        /// <seealso cref="AudioExtensions.ChannelCount(AudioSpeakerMode)"/>
        public readonly int channelCount => m_Config.speakerMode.ChannelCount();

        /// <summary>
        /// The batch size of samples being processed.
        /// </summary>
        /// <remarks>
        /// This determines the maximum size of the <see cref="ChannelBuffer"/> passed into processing callbacks,
        /// like <see cref="GeneratorInstance.Process"/>.
        /// </remarks>
        /// <seealso cref="AudioConfiguration.dspBufferSize"/>
        public readonly int bufferSize => m_Config.dspBufferSize;

        /// <summary>
        /// The sample rate this <see cref="AudioFormat"/> is configured to run at.
        /// </summary>
        /// <seealso cref="AudioConfiguration.sampleRate"/>"/>
        public readonly int sampleRate => m_Config.sampleRate;

        /// <summary>
        /// The speaker mode this <see cref="AudioFormat"/> is configured to run in.
        /// </summary>
        /// <remarks>
        /// This determines how the channel layout is mapped to speakers, and how many channels are available.
        /// </remarks>
        /// <seealso cref="AudioConfiguration.speakerMode"/>"/>
        /// <seealso cref="AudioFormat.channelCount"/>
        public readonly AudioSpeakerMode speakerMode => m_Config.speakerMode;

        internal readonly AudioConfiguration audioConfiguration => m_Config;

        AudioConfiguration m_Config;
    }
}
