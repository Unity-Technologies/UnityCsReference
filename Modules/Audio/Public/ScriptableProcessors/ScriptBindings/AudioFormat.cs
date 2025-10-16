// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Audio
{
    /// <summary>
    /// Represents an audio format containing information used for signal processing.
    /// </summary>
    /// <remarks>
    /// An <see cref="AudioFormat"/> provides information such as sample rate, speaker setup,
    /// and buffer size. It is used by types such as <see cref="ControlContext"/> and <see cref="ProcessorInstance"/>.
    /// </remarks>
    /// <seealso cref="AudioConfiguration"/>
    public struct AudioFormat
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AudioFormat"/> using the provided <see cref="AudioConfiguration"/>.
        /// </summary>
        /// <seealso cref="AudioSettings.GetConfiguration"/>
        public AudioFormat(AudioConfiguration config) { m_Config = config; }

        /// <summary>
        /// Initializes a new instance of <see cref="AudioFormat"/> using the specified parameters.
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
        /// The number of channels represented by the current <see cref="speakerMode"/>.
        /// </summary>
        /// <seealso cref="AudioExtensions.ChannelCount(AudioSpeakerMode)"/>
        public readonly int channelCount => m_Config.speakerMode.ChannelCount();

        /// <summary>
        /// The number of audio frames processed in each audio callback.
        /// </summary>
        /// <remarks>
        /// Depending on the context within the <see cref="AudioFormat"/> is used, this value specifies either the exact number of frames
        /// or the maximum number of frames in the <see cref="ChannelBuffer"/> passed to processing callbacks.
        /// </remarks>
        /// <seealso cref="AudioConfiguration.dspBufferSize"/>
        public readonly int bufferFrameCount => m_Config.dspBufferSize;

        /// <summary>
        /// The sample rate this <see cref="AudioFormat"/> is configured to run at.
        /// </summary>
        /// <seealso cref="AudioConfiguration.sampleRate"/>
        public readonly int sampleRate => m_Config.sampleRate;

        /// <summary>
        /// The speaker mode this <see cref="AudioFormat"/> is configured to run in.
        /// </summary>
        /// <remarks>
        /// The speaker mode defines how channels are mapped to speakers and how many channels are available.
        /// </remarks>
        /// <seealso cref="AudioConfiguration.speakerMode"/>
        /// <seealso cref="channelCount"/>
        public readonly AudioSpeakerMode speakerMode => m_Config.speakerMode;

        internal readonly AudioConfiguration audioConfiguration => m_Config;

        AudioConfiguration m_Config;
    }
}
