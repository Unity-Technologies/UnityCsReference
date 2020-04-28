// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Audio;
using UnityEngine.Bindings;
using UnityEngine.Internal;

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine
{
    // These are speaker types defined for use with [[AudioSettings.speakerMode]].
    public enum AudioSpeakerMode
    {
        // Channel count is unaffected.
        [Obsolete("Raw speaker mode is not supported. Do not use.", true)] Raw = 0,
        // Channel count is set to 1. The speakers are monaural.
        Mono = 1,
        // Channel count is set to 2. The speakers are stereo. This is the editor default.
        Stereo = 2,
        // Channel count is set to 4. 4 speaker setup. This includes front left, front right, rear left, rear right.
        Quad = 3,
        // Channel count is set to 5. 5 speaker setup. This includes front left, front right, center, rear left, rear right.
        Surround = 4,
        // Channel count is set to 6. 5.1 speaker setup. This includes front left, front right, center, rear left, rear right and a subwoofer.
        Mode5point1 = 5,
        // Channel count is set to 8. 7.1 speaker setup. This includes front left, front right, center, rear left, rear right, side left, side right and a subwoofer.
        Mode7point1 = 6,
        // Channel count is set to 2. Stereo output, but data is encoded in a way that is picked up by a Prologic/Prologic2 decoder and split into a 5.1 speaker setup.
        Prologic = 7
    }

    public enum AudioDataLoadState
    {
        Unloaded = 0,
        Loading = 1,
        Loaded = 2,
        Failed = 3
    }

    public struct AudioConfiguration
    {
        public AudioSpeakerMode speakerMode;
        public int dspBufferSize;
        public int sampleRate;
        public int numRealVoices;
        public int numVirtualVoices;
    }

    // Imported audio format for [[AudioImporter]].
    public enum AudioCompressionFormat
    {
        PCM = 0,
        Vorbis = 1,
        ADPCM = 2,
        MP3 = 3,
        VAG = 4,
        HEVAG = 5,
        XMA = 6,
        AAC = 7,
        GCADPCM = 8,
        ATRAC9 = 9
    }

    // The way we load audio assets [[AudioImporter]].
    public enum AudioClipLoadType
    {
        DecompressOnLoad = 0,
        CompressedInMemory = 1,
        Streaming = 2
    }

    // Describes when an [[AudioSource]] or [[AudioListener]] is updated.
    public enum AudioVelocityUpdateMode
    {
        // Updates the source or listener in the fixed update loop if it is attached to a [[Rigidbody]], dynamic otherwise.
        Auto = 0,
        // Updates the source or listener in the fixed update loop.
        Fixed = 1,
        // Updates the source or listener in the dynamic update loop.
        Dynamic = 2
    }

    // Spectrum analysis windowing types
    public enum FFTWindow
    {
        // w[n] = 1.0
        Rectangular = 0,
        // w[n] = TRI(2n/N)
        Triangle = 1,
        // w[n] = 0.54 - (0.46 * COS(n/N) )
        Hamming = 2,
        // w[n] = 0.5 * (1.0 - COS(n/N) )
        Hanning = 3,
        // w[n] = 0.42 - (0.5 * COS(n/N) ) + (0.08 * COS(2.0 * n/N) )
        Blackman = 4,
        // w[n] = 0.35875 - (0.48829 * COS(1.0 * n/N)) + (0.14128 * COS(2.0 * n/N)) - (0.01168 * COS(3.0 * n/N))
        BlackmanHarris = 5
    }

    // Rolloff modes that a 3D sound can have in an audio source.
    public enum AudioRolloffMode
    {
        // Use this mode when you want a real-world rolloff.
        Logarithmic = 0,
        // Use this mode when you want to lower the volume of your sound over the distance
        Linear = 1,
        // Use this when you want to use a custom rolloff.
        Custom = 2
    }

    public enum AudioSourceCurveType
    {
        CustomRolloff = 0,
        SpatialBlend  = 1,
        ReverbZoneMix = 2,
        Spread        = 3
    }

    // Reverb presets used by the Reverb Zone class and the audio reverb filter
    public enum AudioReverbPreset
    {
        // No reverb preset selected
        Off = 0,

        // Generic preset.
        Generic = 1,

        // Padded cell preset.
        PaddedCell = 2,

        // Room preset.
        Room = 3,

        // Bathroom preset.
        Bathroom = 4,

        // Livingroom preset
        Livingroom = 5,

        // Stoneroom preset
        Stoneroom = 6,

        // Auditorium preset.
        Auditorium = 7,

        // Concert hall preset.
        Concerthall = 8,

        // Cave preset.
        Cave = 9,

        // Arena preset.
        Arena = 10,

        // Hangar preset.
        Hangar = 11,

        // Carpeted hallway preset.
        CarpetedHallway = 12,

        // Hallway preset.
        Hallway = 13,

        // Stone corridor preset.
        StoneCorridor = 14,

        // Alley preset.
        Alley = 15,

        // Forest preset.
        Forest = 16,

        // City preset.
        City = 17,

        // Mountains preset.
        Mountains = 18,

        // Quarry preset.
        Quarry = 19,

        // Plain preset.
        Plain = 20,

        // Parking Lot preset
        ParkingLot = 21,

        // Sewer pipe preset.
        SewerPipe = 22,

        // Underwater presset
        Underwater = 23,

        // Drugged preset
        Drugged = 24,

        // Dizzy preset.
        Dizzy = 25,

        // Psychotic preset.
        Psychotic = 26,

        // User defined preset.
        User = 27
    }

    // Controls the global audio settings from script.
    [NativeHeader("Modules/Audio/Public/ScriptBindings/Audio.bindings.h")]
    [StaticAccessor("GetAudioManager()", StaticAccessorType.Dot)]
    public sealed partial class AudioSettings
    {
        extern static private AudioSpeakerMode GetSpeakerMode();
        [NativeThrows, NativeMethod(Name = "AudioSettings::SetConfiguration", IsFreeFunction = true)]
        extern static private bool SetConfiguration(AudioConfiguration config);

        [NativeMethod(Name = "AudioSettings::GetSampleRate", IsFreeFunction = true)]
        extern static private int GetSampleRate();

        extern static private bool SetSpatializerName(string pluginName);

        // Returns the speaker mode capability of the current audio driver. Read only.
        extern static public AudioSpeakerMode driverCapabilities
        {
            [NativeName("GetSpeakerModeCaps")]
            get;
        }

        // Gets the current speaker mode. Default is 2 channel stereo.
        static public AudioSpeakerMode speakerMode
        {
            get
            {
                return GetSpeakerMode();
            }
            set
            {
                Debug.LogWarning("Setting AudioSettings.speakerMode is deprecated and has been replaced by audio project settings and the AudioSettings.GetConfiguration/AudioSettings.Reset API.");
                AudioConfiguration config = GetConfiguration();
                config.speakerMode = value;
                if (!SetConfiguration(config))
                    Debug.LogWarning("Setting AudioSettings.speakerMode failed");
            }
        }

        extern static internal int profilerCaptureFlags { get; }

        // Returns the current time of the audio system. This is based on the number of samples the audio system processes and is therefore more exact than the time obtained via the Time.time property.
        // It is constant while Unity is paused.
        extern static public double dspTime
        {
            [NativeMethod(Name = "GetDSPTime", IsThreadSafe = true)]
            get;
        }

        // Get and set the mixer's current output rate.
        static public int outputSampleRate
        {
            get
            {
                return GetSampleRate();
            }

            set
            {
                Debug.LogWarning("Setting AudioSettings.outputSampleRate is deprecated and has been replaced by audio project settings and the AudioSettings.GetConfiguration/AudioSettings.Reset API.");
                AudioConfiguration config = GetConfiguration();
                config.sampleRate = value;
                if (!SetConfiguration(config))
                    Debug.LogWarning("Setting AudioSettings.outputSampleRate failed");
            }
        }

        [NativeMethod(Name = "AudioSettings::GetDSPBufferSize", IsFreeFunction = true)]
        extern static public void GetDSPBufferSize(out int bufferLength, out int numBuffers);

        // Set the mixer's buffer size in samples.
        [Obsolete("AudioSettings.SetDSPBufferSize is deprecated and has been replaced by audio project settings and the AudioSettings.GetConfiguration/AudioSettings.Reset API.")]
        static public void SetDSPBufferSize(int bufferLength, int numBuffers)
        {
            Debug.LogWarning("AudioSettings.SetDSPBufferSize is deprecated and has been replaced by audio project settings and the AudioSettings.GetConfiguration/AudioSettings.Reset API.");
            AudioConfiguration config = GetConfiguration();
            config.dspBufferSize = bufferLength;
            if (!SetConfiguration(config))
                Debug.LogWarning("SetDSPBufferSize failed");
        }

        extern static internal bool editingInPlaymode
        {
            [NativeName("IsEditingInPlaymode")]
            get;

            [NativeName("SetEditingInPlaymode")]
            set;
        }

        [NativeMethod(Name = "AudioSettings::GetSpatializerNames", IsFreeFunction = true)]
        extern static public string[] GetSpatializerPluginNames();

        [NativeName("GetCurrentSpatializerDefinitionName")]
        extern static public string GetSpatializerPluginName();

        static public void SetSpatializerPluginName(string pluginName)
        {
            if (!SetSpatializerName(pluginName))
                throw new ArgumentException("Invalid spatializer plugin name");
        }


        extern static public AudioConfiguration GetConfiguration();

        static public bool Reset(AudioConfiguration config)
        {
            return SetConfiguration(config);
        }

        public delegate void AudioConfigurationChangeHandler(bool deviceWasChanged);

        static public event AudioConfigurationChangeHandler OnAudioConfigurationChanged;

        [RequiredByNativeCode]
        static internal void InvokeOnAudioConfigurationChanged(bool deviceWasChanged)
        {
            if (OnAudioConfigurationChanged != null)
                OnAudioConfigurationChanged(deviceWasChanged);
        }

        extern static internal bool unityAudioDisabled
        {
            [NativeName("IsAudioDisabled")]
            get;
        }

        [NativeMethod(Name = "AudioSettings::GetCurrentAmbisonicDefinitionName", IsFreeFunction = true)]
        extern static internal string GetAmbisonicDecoderPluginName();

        [NativeMethod(Name = "AudioSettings::SetAmbisonicName", IsFreeFunction = true)]
        extern static internal void SetAmbisonicDecoderPluginName(string name);

        public static class Mobile
        {
            static public bool muteState
            {
                get { return false; }
            }

            static public bool stopAudioOutputOnMute
            {
                get { return false; }
                set
                {
                    Debug.LogWarning("Setting AudioSettings.Mobile.stopAudioOutputOnMute is possible on iOS and Android only");
                }
            }

            static public bool audioOutputStarted
            {
                get { return true; }
            }

#pragma warning disable 0067
            static public event Action<bool> OnMuteStateChanged;
#pragma warning restore 0067

            static public void StartAudioOutput()
            {
                Debug.LogWarning("AudioSettings.Mobile.StartAudioOutput is implemented for iOS and Android only");
            }

            static public void StopAudioOutput()
            {
                Debug.LogWarning("AudioSettings.Mobile.StopAudioOutput is implemented for iOS and Android only");
            }
        }
    }

    // A container for audio data.
    [NativeHeader("Modules/Audio/Public/ScriptBindings/Audio.bindings.h")]
    [StaticAccessor("AudioClipBindings", StaticAccessorType.DoubleColon)]
    public sealed class AudioClip : Object
    {
        private AudioClip() {}

        extern static private bool GetData(AudioClip clip, [Out] float[] data, int numSamples, int samplesOffset);
        extern static private bool SetData(AudioClip clip, float[] data, int numsamples, int samplesOffset);
        extern static private AudioClip Construct_Internal();

        extern private string GetName();
        extern private void CreateUserSound(string name, int lengthSamples, int channels, int frequency, bool stream);

        // The length of the audio clip in seconds (read-only)
        [NativeProperty("LengthSec")]
        extern public float length { get; }

        // The length of the audio clip in samples (read-only)
        // Prints how many samples the attached audio source has
        [NativeProperty("SampleCount")]
        extern public int samples { get; }

        // Channels in audio clip (read-only)
        [NativeProperty("ChannelCount")]
        extern public int channels { get; }

        // Sample frequency (read-only)
        extern public int frequency { get; }

        // Is a streamed audio clip ready to play? (read-only)
        [Obsolete("Use AudioClip.loadState instead to get more detailed information about the loading process.")]
        extern public bool isReadyToPlay
        {
            [NativeName("ReadyToPlay")]
            get;
        }

        // AudioClip load type (read-only)
        extern public AudioClipLoadType loadType { get; }

        extern public bool LoadAudioData();
        extern public bool UnloadAudioData();

        extern public bool preloadAudioData { get; }

        extern public bool ambisonic { get; }

        extern public bool loadInBackground { get; }

        extern public AudioDataLoadState loadState
        {
            [NativeMethod(Name = "AudioClipBindings::GetLoadState", HasExplicitThis = true)]
            get;
        }

        // Fills an array with sample data from the clip. The samples are floats ranging from -1.0f to 1.0f. The sample count is determined by the length of the float array.
        public bool GetData(float[] data, int offsetSamples)
        {
            if (channels <= 0)
            {
                Debug.Log("AudioClip.GetData failed; AudioClip " + GetName() + " contains no data");
                return false;
            }

            int numSamples = (data != null) ? (data.Length / channels) : 0;
            return GetData(this, data, numSamples, offsetSamples);
        }

        // Set sample data in a clip. The samples should be floats ranging from 0.0f to 1.0f (exceeding these limits will lead to artifacts and undefined behaviour).
        public bool SetData(float[] data, int offsetSamples)
        {
            if (channels <= 0)
            {
                Debug.Log("AudioClip.SetData failed; AudioClip " + GetName() + " contains no data");
                return false;
            }

            if ((offsetSamples < 0) || (offsetSamples >= samples))
                throw new ArgumentException("AudioClip.SetData failed; invalid offsetSamples");

            if ((data == null) || (data.Length == 0))
                throw new ArgumentException("AudioClip.SetData failed; invalid data");

            return SetData(this, data, data.Length / channels, offsetSamples);
        }

        /// *listonly*
        [Obsolete("The _3D argument of AudioClip is deprecated. Use the spatialBlend property of AudioSource instead to morph between 2D and 3D playback.")]
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool _3D, bool stream)
        {
            return Create(name, lengthSamples, channels, frequency, stream);
        }

        [Obsolete("The _3D argument of AudioClip is deprecated. Use the spatialBlend property of AudioSource instead to morph between 2D and 3D playback.")]
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool _3D, bool stream, PCMReaderCallback pcmreadercallback)
        {
            return Create(name, lengthSamples, channels, frequency, stream, pcmreadercallback, null);
        }

        [Obsolete("The _3D argument of AudioClip is deprecated. Use the spatialBlend property of AudioSource instead to morph between 2D and 3D playback.")]
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool _3D, bool stream, PCMReaderCallback pcmreadercallback, PCMSetPositionCallback pcmsetpositioncallback)
        {
            return Create(name, lengthSamples, channels, frequency, stream, pcmreadercallback, pcmsetpositioncallback);
        }

        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream)
        {
            AudioClip clip = Create(name, lengthSamples, channels, frequency, stream, null, null);
            return clip;
        }

        /// *listonly*
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream, PCMReaderCallback pcmreadercallback)
        {
            AudioClip clip = Create(name, lengthSamples, channels, frequency, stream, pcmreadercallback, null);
            return clip;
        }

        // Creates a user AudioClip with a name and with the given length in samples, channels and frequency.
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream, PCMReaderCallback pcmreadercallback, PCMSetPositionCallback pcmsetpositioncallback)
        {
            if (name == null) throw new NullReferenceException();
            if (lengthSamples <= 0) throw new ArgumentException("Length of created clip must be larger than 0");
            if (channels <= 0) throw new ArgumentException("Number of channels in created clip must be greater than 0");
            if (frequency <= 0) throw new ArgumentException("Frequency in created clip must be greater than 0");

            AudioClip clip = Construct_Internal();
            if (pcmreadercallback != null)
                clip.m_PCMReaderCallback += pcmreadercallback;
            if (pcmsetpositioncallback != null)
                clip.m_PCMSetPositionCallback += pcmsetpositioncallback;

            clip.CreateUserSound(name, lengthSamples, channels, frequency, stream);

            return clip;
        }

        /// *listonly*
        public delegate void PCMReaderCallback(float[] data);
        private event PCMReaderCallback m_PCMReaderCallback = null;

        /// *listonly*
        public delegate void PCMSetPositionCallback(int position);
        private event PCMSetPositionCallback m_PCMSetPositionCallback = null;

        [RequiredByNativeCode]
        private void InvokePCMReaderCallback_Internal(float[] data)
        {
            if (m_PCMReaderCallback != null)
                m_PCMReaderCallback(data);
        }

        [RequiredByNativeCode]
        private void InvokePCMSetPositionCallback_Internal(int position)
        {
            if (m_PCMSetPositionCallback != null)
                m_PCMSetPositionCallback(position);
        }
    }

    public class AudioBehaviour : Behaviour
    {
    }

    // Representation of a listener in 3D space.
    [RequireComponent(typeof(Transform))]
    [StaticAccessor("AudioListenerBindings", StaticAccessorType.DoubleColon)]
    public sealed class AudioListener : AudioBehaviour
    {
        [NativeThrows]
        extern static private void GetOutputDataHelper([Out] float[] samples, int channel);

        [NativeThrows]
        extern static private void GetSpectrumDataHelper([Out] float[] samples, int channel, FFTWindow window);

        // Controls the game sound volume (0.0 to 1.0)
        extern static public float volume { get; set; }

        // The paused state of the audio. If set to True, the listener will not generate sound.
        [NativeProperty("ListenerPause")]
        extern static public bool pause { get; set; }

        // This lets you set whether the Audio Listener should be updated in the fixed or dynamic update.
        extern public AudioVelocityUpdateMode velocityUpdateMode { get; set; }

        // Returns a block of the listener (master)'s output data
        [Obsolete("GetOutputData returning a float[] is deprecated, use GetOutputData and pass a pre allocated array instead.")]
        static public float[] GetOutputData(int numSamples, int channel)
        {
            float[] samples = new float[numSamples];
            GetOutputDataHelper(samples, channel);
            return samples;
        }

        // Returns a block of the listener (master)'s output data
        static public void GetOutputData(float[] samples, int channel)
        {
            GetOutputDataHelper(samples, channel);
        }

        // Returns a block of the listener (master)'s spectrum data
        [Obsolete("GetSpectrumData returning a float[] is deprecated, use GetSpectrumData and pass a pre allocated array instead.")]
        static public float[] GetSpectrumData(int numSamples, int channel, FFTWindow window)
        {
            float[] samples = new float[numSamples];
            GetSpectrumDataHelper(samples, channel, window);
            return samples;
        }

        // Returns a block of the listener (master)'s spectrum data
        static public void GetSpectrumData(float[] samples, int channel, FFTWindow window)
        {
            GetSpectrumDataHelper(samples, channel, window);
        }
    }

    // A representation of audio sources in 3D.
    [RequireComponent(typeof(Transform))]
    [StaticAccessor("AudioSourceBindings", StaticAccessorType.DoubleColon)]
    public sealed partial class AudioSource : AudioBehaviour
    {
        extern static private float GetPitch([NotNull] AudioSource source);
        extern static private void SetPitch([NotNull] AudioSource source, float pitch);

        extern static private void PlayHelper([NotNull] AudioSource source, UInt64 delay);
        extern private void Play(double delay);

        extern static private void PlayOneShotHelper([NotNull] AudioSource source, AudioClip clip, float volumeScale);

        extern private void Stop(bool stopOneShots);

        [NativeThrows]
        extern static private void SetCustomCurveHelper([NotNull] AudioSource source, AudioSourceCurveType type, AnimationCurve curve);
        extern static private AnimationCurve GetCustomCurveHelper([NotNull] AudioSource source, AudioSourceCurveType type);

        extern static private void GetOutputDataHelper([NotNull] AudioSource source, [Out] float[] samples, int channel);
        [NativeThrows]
        extern static private void GetSpectrumDataHelper([NotNull] AudioSource source, [Out] float[] samples, int channel, FFTWindow window);

        // The volume of the audio source (0.0 to 1.0)
        extern public float volume { get; set; }

        // The pitch of the audio source.
        public float pitch
        {
            get { return GetPitch(this); }
            set { SetPitch(this, value); }
        }

        // Playback position in seconds.
        [NativeProperty("SecPosition")]
        extern public float time { get; set; }

        // Playback position in PCM samples.
        [NativeProperty("SamplePosition")]
        extern public int timeSamples
        {
            [NativeMethod(IsThreadSafe = true)]
            get;

            [NativeMethod(IsThreadSafe = true)]
            set;
        }

        // The default [[AudioClip]] to play
        [NativeProperty("AudioClip")]
        extern public AudioClip clip { get; set; }

        extern public AudioMixerGroup outputAudioMixerGroup { get; set; }

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::PlayOnDualShock4", HasExplicitThis = true, ThrowsException = true)]
        [Obsolete("Use PlayOnGamepad instead")]
        extern public bool PlayOnDualShock4(Int32 userId);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetDualShock4SpeakerMixLevel", HasExplicitThis = true, ThrowsException = true)]
        [Obsolete("Use SetGamepadSpeakerMixLevel instead")]
        extern public bool SetDualShock4PadSpeakerMixLevel(Int32 userId, Int32 mixLevel);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetDualShock4SpeakerMixLevelDefault", HasExplicitThis = true, ThrowsException = true)]
        [Obsolete("Use SetGamepadSpeakerMixLevelDefault instead")]
        extern public bool SetDualShock4PadSpeakerMixLevelDefault(Int32 userId);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetDualShock4SpeakerRestrictedAudio", HasExplicitThis = true, ThrowsException = true)]
        [Obsolete("Use SetgamepadSpeakerRestrictedAudio instead")]
        extern public bool SetDualShock4PadSpeakerRestrictedAudio(Int32 userId, bool restricted);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::PlayOnGamepad", HasExplicitThis = true, ThrowsException = true)]
        [Obsolete("Use PlayOnGamepad instead")]
        extern public bool PlayOnDualShock4PadIndex(Int32 slot);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::DisableGamepadOutput", HasExplicitThis = true)]
        [Obsolete("Use DisableGamepadOutput instead")]
        extern public bool DisableDualShock4Output();

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetGamepadSpeakerMixLevel", HasExplicitThis = true, ThrowsException = true)]
        [Obsolete("Use SetGamepadSpeakerMixLevel instead")]
        extern public bool SetDualShock4PadSpeakerMixLevelPadIndex(Int32 slot, Int32 mixLevel);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetGamepadSpeakerMixLevelDefault", HasExplicitThis = true, ThrowsException = true)]
        [Obsolete("Use SetGamepadSpeakerMixLevelDefault instead")]
        extern public bool SetDualShock4PadSpeakerMixLevelDefaultPadIndex(Int32 slot);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetGamepadSpeakerRestrictedAudio", HasExplicitThis = true, ThrowsException = true)]
        [Obsolete("Use SetGamepadSpeakerRestrictedAudio instead")]
        extern public bool SetDualShock4PadSpeakerRestrictedAudioPadIndex(Int32 slot, bool restricted);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::PlayOnGamepad", HasExplicitThis = true, ThrowsException = true)]
        extern public bool PlayOnGamepad(Int32 slot);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::DisableGamepadOutput", HasExplicitThis = true)]
        extern public bool DisableGamepadOutput();

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetGamepadSpeakerMixLevel", HasExplicitThis = true, ThrowsException = true)]
        extern public bool SetGamepadSpeakerMixLevel(Int32 slot, Int32 mixLevel);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetGamepadSpeakerMixLevelDefault", HasExplicitThis = true, ThrowsException = true)]
        extern public bool SetGamepadSpeakerMixLevelDefault(Int32 slot);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetGamepadSpeakerRestrictedAudio", HasExplicitThis = true, ThrowsException = true)]
        extern public bool SetGamepadSpeakerRestrictedAudio(Int32 slot, bool restricted);

        public enum GamepadSpeakerOutputType
        {
            Speaker = 0,
            Vibration = 1,
        }

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "GamepadSpeakerSupportsOutputType", HasExplicitThis = false)]
        extern static public bool GamepadSpeakerSupportsOutputType(GamepadSpeakerOutputType outputType);


        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        extern public GamepadSpeakerOutputType gamepadSpeakerOutputType { get; set; }

        // Plays the ::ref::clip with a certain delay (the optional delay argument is deprecated since 4.1a3) and the functionality has been replaced by PlayDelayed.
        [ExcludeFromDocs]
        public void Play()
        {
            PlayHelper(this, 0);
        }

        public void Play([UnityEngine.Internal.DefaultValue("0")] UInt64 delay)
        {
            PlayHelper(this, delay);
        }

        // Plays the ::ref::clip with a delay specified in seconds. Users are advised to use this function instead of the old Play(delay) function that took a delay specified in samples relative to a reference rate of 44.1 kHz as an argument.
        public void PlayDelayed(float delay)
        {
            Play((delay < 0.0f) ? 0.0 : -(double)delay);
        }

        // Schedules the ::ref::clip to play at the specified absolute time. This is the preferred way to stitch AudioClips in music players because it is independent of the frame rate and gives the audio system enough time to prepare the playback of the sound to fetch it from media where the opening and buffering takes a lot of time (streams) without causing sudden performance peaks.
        public void PlayScheduled(double time)
        {
            Play((time < 0.0) ? 0.0 : time);
        }

        // Plays an [[AudioClip]], and scales the [[AudioSource]] volume by volumeScale.
        [ExcludeFromDocs]
        public void PlayOneShot(AudioClip clip)
        {
            PlayOneShot(clip, 1.0f);
        }

        public void PlayOneShot(AudioClip clip, [UnityEngine.Internal.DefaultValue("1.0F")] float volumeScale)
        {
            if (clip == null)
            {
                Debug.LogWarning("PlayOneShot was called with a null AudioClip.");
                return;
            }

            PlayOneShotHelper(this, clip, volumeScale);
        }

        extern public void SetScheduledStartTime(double time);
        extern public void SetScheduledEndTime(double time);

        // Stops playing the ::ref::clip.
        public void Stop()
        {
            Stop(true);
        }

        // Pauses playing the ::ref::clip.
        extern public void Pause();

        // Unpauses the paused source, different from play in that it does not start any new playback.
        extern public void UnPause();

        // Is the ::ref::clip playing right now (RO)?
        extern public bool isPlaying
        {
            [NativeName("IsPlayingScripting")]
            get;
        }

        extern public bool isVirtual
        {
            [NativeName("GetLastVirtualState")]
            get;
        }

        // Plays the clip at position. Automatically cleans up the audio source after it has finished playing.
        [ExcludeFromDocs]
        static public void PlayClipAtPoint(AudioClip clip, Vector3 position)
        {
            PlayClipAtPoint(clip, position, 1.0f);
        }

        static public void PlayClipAtPoint(AudioClip clip, Vector3 position, [UnityEngine.Internal.DefaultValue("1.0F")] float volume)
        {
            GameObject go = new GameObject("One shot audio");
            go.transform.position = position;
            AudioSource source = (AudioSource)go.AddComponent(typeof(AudioSource));
            source.clip = clip;
            source.spatialBlend = 1.0f;
            source.volume = volume;
            source.Play();

            // Note: timeScale > 1 means that game time is accelerated. However, the sounds play at their normal speed,
            // so we need to postpone the point in time, when the sound is stopped.
            // Conversly, when timescale approaches 0, the inaccuracies of float precision mean that it kills the sound early
            // Also when timescale is 0, the object is destroyed immediately.
            // Note: The behaviour here means that when the timescale is 0, GameObjects will pile up until the timescale
            // is taken above 0 again.
            Destroy(go, clip.length * (Time.timeScale < 0.01f ? 0.01f : Time.timeScale));
        }

        // Is the audio clip looping?
        extern public bool loop { get; set; }

        // This makes the audio source not take into account the volume of the audio listener.
        extern public bool ignoreListenerVolume { get; set; }

        // If set to true, the audio source will automatically start playing on awake
        extern public bool playOnAwake { get; set; }

        // If set to true, the audio source will be playable while the AudioListener is paused
        extern public bool ignoreListenerPause { get; set; }

        // Whether the Audio Source should be updated in the fixed or dynamic update.
        extern public AudioVelocityUpdateMode velocityUpdateMode { get; set; }

        // Sets how a Mono or 2D sound is panned linearly to the left or right.
        [NativeProperty("StereoPan")]
        extern public float panStereo { get; set; }

        // Sets how much a playing sound is treated as a 3D source
        [NativeProperty("SpatialBlendMix")]
        extern public float spatialBlend { get; set; }

        // Enables/disables custom spatialization
        extern public bool spatialize { get; set; }

        // Determines if the spatializer effect is inserted before or after the effect filters.
        extern public bool spatializePostEffects { get; set; }

        public void SetCustomCurve(AudioSourceCurveType type, AnimationCurve curve)
        {
            SetCustomCurveHelper(this, type, curve);
        }

        public AnimationCurve GetCustomCurve(AudioSourceCurveType type)
        {
            return GetCustomCurveHelper(this, type);
        }

        // Sets how much a playing sound is mixed into the reverb zones
        extern public float reverbZoneMix { get; set; }

        // Bypass effects
        extern public bool bypassEffects { get; set; }

        // Bypass listener effects
        extern public bool bypassListenerEffects { get; set; }

        // Bypass reverb zones
        extern public bool bypassReverbZones { get; set; }

        // Sets the Doppler scale for this AudioSource
        extern public float dopplerLevel { get; set; }

        // Sets the spread angle a 3d stereo or multichannel sound in speaker space.
        extern public float spread { get; set; }

        // Sets the priority of the [[AudioSource]]
        extern public int priority { get; set; }

        // Un- / Mutes the AudioSource. Mute sets the volume=0, Un-Mute restore the original volume.
        extern public bool mute { get; set; }

        // Within the Min distance the AudioSource will cease to grow louder in volume.
        extern public float minDistance { get; set; }

        // (Logarithmic rolloff) MaxDistance is the distance a sound stops attenuating at.
        extern public float maxDistance { get; set; }

        // Sets/Gets how the AudioSource attenuates over distance
        extern public AudioRolloffMode rolloffMode { get; set; }

        // Returns a block of the currently playing source's output data
        [Obsolete("GetOutputData returning a float[] is deprecated, use GetOutputData and pass a pre allocated array instead.")]
        public float[] GetOutputData(int numSamples, int channel)
        {
            float[] samples = new float[numSamples];
            GetOutputDataHelper(this, samples, channel);
            return samples;
        }

        // Returns a block of the currently playing source's output data
        public void GetOutputData(float[] samples, int channel)
        {
            GetOutputDataHelper(this, samples, channel);
        }

        // Returns a block of the currently playing source's spectrum data
        [Obsolete("GetSpectrumData returning a float[] is deprecated, use GetSpectrumData and pass a pre allocated array instead.")]
        public float[] GetSpectrumData(int numSamples, int channel, FFTWindow window)
        {
            float[] samples = new float[numSamples];
            GetSpectrumDataHelper(this, samples, channel, window);
            return samples;
        }

        // Returns a block of the currently playing source's spectrum data
        public void GetSpectrumData(float[] samples, int channel, FFTWindow window)
        {
            GetSpectrumDataHelper(this, samples, channel, window);
        }

        [Obsolete("minVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead.", true)]
        public float minVolume
        {
            get { Debug.LogError("minVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead."); return 0.0f; }
            set { Debug.LogError("minVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead."); }
        }

        [Obsolete("maxVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead.", true)]
        public float maxVolume
        {
            get { Debug.LogError("maxVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead."); return 0.0f; }
            set { Debug.LogError("maxVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead."); }
        }

        [Obsolete("rolloffFactor is not supported anymore. Use min-, maxDistance and rolloffMode instead.", true)]
        public float rolloffFactor
        {
            get { Debug.LogError("rolloffFactor is not supported anymore. Use min-, maxDistance and rolloffMode instead."); return 0.0f; }
            set { Debug.LogError("rolloffFactor is not supported anymore. Use min-, maxDistance and rolloffMode instead."); }
        }

        extern public bool SetSpatializerFloat(int index, float value);
        extern public bool GetSpatializerFloat(int index, out float value);

        extern public bool GetAmbisonicDecoderFloat(int index, out float value);
        extern public bool SetAmbisonicDecoderFloat(int index, float value);
    }

    // Reverb Zones are used when you want to gradually change from a point
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Audio/Public/AudioReverbZone.h")]
    public sealed class AudioReverbZone : Behaviour
    {
        //  The distance from the centerpoint that the reverb will have full effect at. Default = 10.0.
        extern public float minDistance { get; set; }

        //  The distance from the centerpoint that the reverb will not have any effect. Default = 15.0.
        extern public float maxDistance { get; set; }

        // Set/Get reverb preset properties
        extern public AudioReverbPreset reverbPreset { get; set; }

        // room effect level (at mid frequencies)
        extern public int room { get; set; }

        // relative room effect level at high frequencies
        extern public int roomHF { get; set; }

        // relative room effect level at low frequencies
        extern public int roomLF { get; set; }

        // reverberation decay time at mid frequencies
        extern public float decayTime { get; set; }

        //  high-frequency to mid-frequency decay time ratio
        extern public float decayHFRatio { get; set; }

        // early reflections level relative to room effect
        extern public int reflections { get; set; }

        //  initial reflection delay time
        extern public float reflectionsDelay { get; set; }

        // late reverberation level relative to room effect
        extern public int reverb { get; set; }

        //  late reverberation delay time relative to initial reflection
        extern public float reverbDelay { get; set; }

        //  reference high frequency (hz)
        extern public float HFReference { get; set; }

        // reference low frequency (hz)
        extern public float LFReference { get; set; }

        // like rolloffscale in global settings, but for reverb room size effect
        [Obsolete("Warning! roomRolloffFactor is no longer supported.")]
        public float roomRolloffFactor
        {
            get { Debug.LogWarning("Warning! roomRolloffFactor is no longer supported."); return 10.0f; }
            set { Debug.LogWarning("Warning! roomRolloffFactor is no longer supported."); }
        }

        // Value that controls the echo density in the late reverberation decay
        extern public float diffusion { get; set; }

        // Value that controls the modal density in the late reverberation decay
        extern public float density { get; set; }

        extern internal bool active { get; set; }
    }

    [RequireComponent(typeof(AudioBehaviour))]
    public sealed partial class AudioLowPassFilter : Behaviour
    {
        extern private AnimationCurve GetCustomLowpassLevelCurveCopy();

        [NativeThrows]
        [NativeMethod(Name = "AudioLowPassFilterBindings::SetCustomLowpassLevelCurveHelper", IsFreeFunction = true)]
        extern static private void SetCustomLowpassLevelCurveHelper(AudioLowPassFilter source, AnimationCurve curve);

        public AnimationCurve customCutoffCurve
        {
            get { return GetCustomLowpassLevelCurveCopy(); }
            set { SetCustomLowpassLevelCurveHelper(this, value); }
        }

        // Lowpass cutoff frequency in hz. 10.0 to 22000.0. Default = 5000.0.
        extern public float cutoffFrequency { get; set; }

        // Determines how much the filter's self-resonance is dampened.
        extern public float lowpassResonanceQ { get; set; }
    }

    [RequireComponent(typeof(AudioBehaviour))]
    public sealed partial class AudioHighPassFilter : Behaviour
    {
        // Highpass cutoff frequency in hz. 10.0 to 22000.0. Default = 5000.0.
        extern public float cutoffFrequency { get; set; }

        // Determines how much the filter's self-resonance isdampened.
        extern public float highpassResonanceQ {get; set; }
    }

    // The Audio Distortion Filter distorts the sound from an AudioSource or
    [RequireComponent(typeof(AudioBehaviour))]
    public sealed class AudioDistortionFilter : Behaviour
    {
        // Distortion value. 0.0 to 1.0. Default = 0.5.
        extern public float distortionLevel { get; set; }
    }

    // The Audio Echo Filter repeats a sound after a given Delay, attenuating
    [RequireComponent(typeof(AudioBehaviour))]
    public sealed class AudioEchoFilter : Behaviour
    {
        // Echo delay in ms. 10 to 5000. Default = 500.
        extern public float delay { get; set; }

        // Echo decay per delay. 0 to 1. 1.0 = No decay, 0.0 = total decay (i.e. simple 1 line delay). Default = 0.5.
        extern public float decayRatio { get; set; }

        // Volume of original signal to pass to output. 0.0 to 1.0. Default = 1.0.
        extern public float dryMix { get; set; }

        // Volume of echo signal to pass to output. 0.0 to 1.0. Default = 1.0.
        extern public float wetMix {get; set; }
    }

    // The Audio Chorus Filter takes an Audio Clip and processes it creating a chorus effect.
    [RequireComponent(typeof(AudioBehaviour))]
    public sealed class AudioChorusFilter : Behaviour
    {
        // Volume of original signal to pass to output. 0.0 to 1.0. Default = 0.5.
        extern public float dryMix { get; set; }

        // Volume of 1st chorus tap. 0.0 to 1.0. Default = 0.5.
        extern public float wetMix1 { get; set; }

        // Volume of 2nd chorus tap. This tap is 90 degrees out of phase of the first tap. 0.0 to 1.0. Default = 0.5.
        extern public float wetMix2 { get; set; }

        // Volume of 3rd chorus tap. This tap is 90 degrees out of phase of the second tap. 0.0 to 1.0. Default = 0.5.
        extern public float wetMix3 { get; set; }

        // Chorus delay in ms. 0.1 to 100.0. Default = 40.0 ms.
        extern public float delay { get; set; }

        // Chorus modulation rate in hz. 0.0 to 20.0. Default = 0.8 hz.
        extern public float rate { get; set; }

        //  Chorus modulation depth. 0.0 to 1.0. Default = 0.03.
        extern public float depth { get; set; }

        // Chorus feedback. Controls how much of the wet signal gets fed back into the chorus buffer. 0.0 to 1.0. Default = 0.0.
        [Obsolete("Warning! Feedback is deprecated. This property does nothing.")]
        public float feedback
        {
            get { Debug.LogWarning("Warning! Feedback is deprecated. This property does nothing."); return 0.0f; }
            set { Debug.LogWarning("Warning! Feedback is deprecated. This property does nothing."); }
        }
    }

    // The Audio Reverb Filter takes an Audio Clip and distortionates it in a
    [RequireComponent(typeof(AudioBehaviour))]
    public sealed partial class AudioReverbFilter : Behaviour
    {
        // Set/Get reverb preset properties
        extern public AudioReverbPreset reverbPreset { get; set; }

        // Mix level of dry signal in output in mB. Ranges from -10000.0 to 0.0. Default is 0.
        extern public float dryLevel { get; set; }

        // Room effect level at low frequencies in mB. Ranges from -10000.0 to 0.0. Default is 0.0.
        extern public float room { get; set; }

        // Room effect high-frequency level re. low frequency level in mB. Ranges from -10000.0 to 0.0. Default is 0.0.
        extern public float roomHF { get; set; }

        // Rolloff factor for room effect. Ranges from 0.0 to 10.0. Default is 10.0
        [Obsolete("Warning! roomRolloffFactor is no longer supported.")]
        public float roomRolloffFactor
        {
            get { Debug.LogWarning("Warning! roomRolloffFactor is no longer supported."); return 10.0f; }
            set { Debug.LogWarning("Warning! roomRolloffFactor is no longer supported."); }
        }

        // Reverberation decay time at low-frequencies in seconds. Ranges from 0.1 to 20.0. Default is 1.0.
        extern public float decayTime { get; set; }

        // Decay HF Ratio : High-frequency to low-frequency decay time ratio. Ranges from 0.1 to 2.0. Default is 0.5.
        extern public float decayHFRatio { get; set; }

        //  Early reflections level relative to room effect in mB. Ranges from -10000.0 to 1000.0. Default is -10000.0.
        extern public float reflectionsLevel { get; set; }

        // Late reverberation level relative to room effect in mB. Ranges from -10000.0 to 2000.0. Default is 0.0.
        extern public float reflectionsDelay { get; set; }

        //  Late reverberation level relative to room effect in mB. Ranges from -10000.0 to 2000.0. Default is 0.0.
        extern public float reverbLevel { get; set; }

        // Late reverberation delay time relative to first reflection in seconds. Ranges from 0.0 to 0.1. Default is 0.04.
        extern public float reverbDelay { get; set; }

        // Reverberation diffusion (echo density) in percent. Ranges from 0.0 to 100.0. Default is 100.0.
        extern public float diffusion { get; set; }

        // Reverberation density (modal density) in percent. Ranges from 0.0 to 100.0. Default is 100.0.
        extern public float density { get; set; }

        // Reference high frequency in Hz. Ranges from 20.0 to 20000.0. Default is 5000.0.
        extern public float hfReference { get; set; }

        // Room effect low-frequency level in mB. Ranges from -10000.0 to 0.0. Default is 0.0.
        extern public float roomLF { get; set; }

        // Reference low-frequency in Hz. Ranges from 20.0 to 1000.0. Default is 250.0.
        extern public float lfReference { get; set; }
    }

    // Use this class to record to an [[AudioClip|audio clip]] using a connected microphone.
    [StaticAccessor("GetAudioManager()", StaticAccessorType.Dot)]
    public sealed class Microphone
    {
        [NativeMethod(IsThreadSafe = true)]
        extern static private int GetMicrophoneDeviceIDFromName(string name);

        extern static private AudioClip StartRecord(int deviceID, bool loop, float lengthSec, int frequency);

        extern static private void EndRecord(int deviceID);

        extern static private bool IsRecording(int deviceID);

        [NativeMethod(IsThreadSafe = true)]
        extern static private int GetRecordPosition(int deviceID);

        extern static private void GetDeviceCaps(int deviceID, out int minFreq, out int maxFreq);

        // Start Recording with device
        static public AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency)
        {
            int deviceID = GetMicrophoneDeviceIDFromName(deviceName);

            if (deviceID == -1)
                throw new ArgumentException("Couldn't acquire device ID for device name " + deviceName);

            if (lengthSec <= 0)
                throw new ArgumentException("Length of recording must be greater than zero seconds (was: " + lengthSec + " seconds)");

            if (lengthSec > 60 * 60)
                throw new ArgumentException("Length of recording must be less than one hour (was: " + lengthSec + " seconds)");

            if (frequency <= 0)
                throw new ArgumentException("Frequency of recording must be greater than zero (was: " + frequency + " Hz)");

            return StartRecord(deviceID, loop, lengthSec, frequency);
        }

        // Stops recording
        static public void End(string deviceName)
        {
            int deviceID = GetMicrophoneDeviceIDFromName(deviceName);
            if (deviceID == -1)
                return;

            EndRecord(deviceID);
        }

        // Gives you a list microphone devices, identified by name.
        extern static public string[] devices
        {
            [NativeName("GetRecordDevices")]
            get;
        }

        // Query if a device is currently recording.
        static public bool IsRecording(string deviceName)
        {
            int deviceID = GetMicrophoneDeviceIDFromName(deviceName);
            if (deviceID == -1)
                return false;

            return IsRecording(deviceID);
        }

        // Get the position in samples of the recording.
        static public int GetPosition(string deviceName)
        {
            int deviceID = GetMicrophoneDeviceIDFromName(deviceName);
            if (deviceID == -1)
                return 0;

            return GetRecordPosition(deviceID);
        }

        // Get the frequency capabilities of a device.
        static public void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq)
        {
            minFreq = 0;
            maxFreq = 0;

            int deviceID = GetMicrophoneDeviceIDFromName(deviceName);
            if (deviceID == -1)
                return;

            GetDeviceCaps(deviceID, out minFreq, out maxFreq);
        }
    }
}
