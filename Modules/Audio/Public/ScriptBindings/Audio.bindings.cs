// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Audio;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Playables;
using Unity.IntegerTime;
using Unity.Scripting.LifecycleManagement;

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

[assembly: InternalsVisibleTo("Unity.AudioMixer.Tests")]

namespace UnityEngine.Audio
{
    ///<summary>Represents an audio generator asset that you can play through an <see cref="AudioSource" />.</summary>
    ///<remarks>**Note**: Audio generators don’t provide direct access to properties like <c>length</c>. However, if your audio generator is an <see cref="AudioClip" />, you can access these properties through <see cref="AudioSource.clip" />. Other types of resources might not provide direct access to these properties because the resources are dynamic or the values might change every time you play the audio.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using System.Collections;
    ///using UnityEngine;
    ///using UnityEngine.Audio;
    ///
    /// // Play an AudioClip, then an AudioResource, through an AudioSource.
    ///[RequireComponent(typeof(AudioSource))]
    ///public class ExampleClass : MonoBehaviour
    ///{
    ///    public AudioClip m_Clip;
    ///    public AudioResource m_Resource;
    ///
    ///    IEnumerator Start()
    ///    {
    ///        AudioSource audioSource = GetComponent<AudioSource>();
    ///        audioSource.resource = m_Clip;
    ///        audioSource.Play();
    ///
    ///        yield return new WaitForSeconds(audioSource.clip.length);
    ///
    ///        audioSource.resource = m_Resource;
    ///        audioSource.Play();
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="AudioClip" />
    ///<seealso cref="AudioRandomContainer" />
    [NativeHeader("Modules/Audio/Public/AudioResource.h")]
    [NativeClass("AudioResource", PersistentTypeId = 0x1537E27D)]
    public abstract class AudioResource : Object
    {
        protected internal AudioResource() {}
    }

    [NativeHeader("Modules/Audio/Public/ScriptBindings/Audio.bindings.h")]
    sealed class AudioManagerTestProxy
    {
        [NativeMethod(Name = "AudioManagerTestProxy::ComputeAudibilityConsistency", IsFreeFunction = true)]
        internal static extern bool ComputeAudibilityConsistency();
    }

    [NativeHeader("Modules/Audio/Public/ScriptBindings/Audio.bindings.h")]
    sealed class PlatformAudioTestProxy
    {
        [NativeMethod(Name = "PlatformAudioTestProxy::HasDefaultPlaybackDevice", IsFreeFunction = true)]
        internal static extern bool HasDefaultPlaybackDevice();
    }
}

namespace UnityEngine
{
    // These are speaker types defined for use with [[AudioSettings.speakerMode]].
    // Must be kept in sync with its C++ counterpart `AudioSpeakerMode`.
    ///<summary>The speaker configurations that Unity supports.</summary>
    ///<remarks>
    ///  <para>Unity supports multiple speaker types. Each speaker type has a different number of channels and is suitable for different device types and situations. The operating system (OS) and hardware determine the final output. Unity requests a mode, but the system might override it. Always check the actual mode at runtime.
    ///
    ///**Note:** <c>AudioSpeakerMode</c> configures the global audio output layout for the entire application. It doesn't assign an <see cref="AudioSource" /> to an individual speaker channel (for example, front left or rear right). To place sounds in the mix, use <see cref="AudioSource.panStereo" /> for left-right balance on mono or stereo clips, <see cref="AudioSource.spatialBlend" /> and the source position for 3D spatialization, <see cref="AudioSource.spread" /> for multichannel clips in speaker space.
    ///
    ///The following are common uses for the speaker modes: 
    ///
    ///- Stereo is the standard default for most applications.
    ///- Use Mono for accessibility reasons.  
    ///- Use 5.1 or 7.1 for high-end PC and console setups, home televisions, theaters.
    ///- Use Quad and 5.0 for specific installations.
    ///- Only use Pro Logic if you must support matrix decoding.
    ///
    ///Use <see cref="AudioSettings.GetConfiguration" /> and <see cref="AudioSettings.Reset" /> to request a speaker mode at runtime. The device might not accept or support your requested speaker mode. In that case: 
    ///
    ///- On some platforms, Unity automatically reduces the audio channel count (downmixes) to one your device supports.
    ///- On other platforms, Unity runs the device's native channel count. 
    ///
    ///To set your project's default speaker mode, go to the [Audio Manager](xref:class-AudioManager): go to **Edit** &gt; **Project Settings** &gt; **Audio** and set **Default Speaker Mode** to your preferred speaker mode.
    ///
    ///The following example demonstrates how to check the current speaker mode at runtime.</para>
    ///  <para />
    ///</remarks>
    ///<example>
    ///  <code><![CDATA[using UnityEngine;
    ///
    ///public class SpeakerModeExample : MonoBehaviour
    ///{
    ///    void Start()
    ///    {
    ///        Debug.Log("Current Speaker Mode: " + AudioSettings.GetConfiguration().speakerMode);
    ///    }
    ///}]]></code>
    ///</example>
    ///<seealso cref="AudioSettings" />
    ///<seealso cref="AudioSource.panStereo" />
    ///<seealso cref="AudioSource.spatialBlend" />
    ///<seealso cref="AudioSource.spread" />
    ///<seealso cref="AudioMixer" />
    ///<seealso href="xref:class-AudioManager">Audio Manager</seealso>
    public enum AudioSpeakerMode
    {
        ///<exclude />
        [Obsolete("Raw speaker mode is not supported. Do not use.", true)] Raw = 0,
        ///<summary>The speakers are mono and contain one channel.</summary>
        ///<remarks>The Mono audio speaker mode contains one audio channel (channel count is set to 1). 
        ///
        ///Mono plays identical audio in the left and right channels, which makes it ideal for users with single-sided deafness, off‑center listening, or a missing/broken earbud. It also works better with many hearing‑assistive devices.
        ///
        ///Most devices use <see cref="AudioSpeakerMode.Stereo" />. If the device doesn't support mono, Unity uses stereo and reports the actual mode. The OS might still play mono as the same signal in left and right channels.
        ///
        ///Pros:
        ///
        ///- Mono works on almost all devices.
        ///- It's simple to mix and test.
        ///- It has low CPU and memory use.
        ///- Users can listen with one earphone without missing audio from other channels. 
        ///
        ///Cons:
        ///
        ///- Mono doesn't have stereo or surround. 
        ///- 3D sounds lose direction.
        ///- Sounds can crowd each other in one channel.
        ///- This speaker mode has less spatial immersion.
        ///- LFE information is often dropped in mono.</remarks>
        Mono = 1,
        ///<summary>The speakers are stereo and contain two channels.</summary>
        ///<remarks>The Stereo speaker mode has two output channels: left and right. Stereo clips and panning can place content in the left or right channel. This mode is the standard for most devices (speakers, TVs, headphones) and is best suited for mobile, PC, web, and virtual reality (VR) headsets. Channel count is set to 2.
        ///
        ///Pros: 
        ///
        ///- Stereo is universally supported across platforms.
        ///- It's ideal for headphones.
        ///- It allows straightforward 3D via head-related transfer function (HRTF) spatializers.
        ///- Stereo has efficient CPU and memory usage compared to multichannel.
        ///
        ///Cons:
        ///
        ///- Stereo has no discrete surround channels - Unity downmixes surround content to the left and right channels.</remarks>
        Stereo = 2,
        ///<summary>The Quad 4.0 speaker setup which contains four channels.</summary>
        ///<remarks>The Quad 4.0 speaker mode contains four channels: 
        ///
        ///- Front left
        ///- Front right
        ///- Rear left
        ///- Rear right
        ///
        ///Channel count is set to 4. The output layout provides four discrete channels (front and rear pairs of left and right). If a device doesn't support this mode, Unity picks a supported mode (often <see cref="AudioSpeakerMode.Stereo" />).
        ///
        ///This mode is common in fixed installations and museums. 
        ///
        ///Pros:
        ///
        ///- Quad is simple surround for four-speaker setups.
        ///
        ///Cons:
        ///
        ///- Quad is rare in homes.
        ///- It's harder to test.
        ///- It requires more setup.</remarks>
        Quad = 3,
        ///<summary>The Surround 5.0 speaker setup which contains five channels.</summary>
        ///<remarks>The Surround 5.0 speaker mode contains 5 channels: 
        ///
        ///- Front left
        ///- Front center
        ///- Front right
        ///- Surround left
        ///- Surround right (no subwoofer). 
        ///
        ///If not supported, Unity picks a supported mode (often <see cref="AudioSpeakerMode.Stereo" /> or <see cref="AudioSpeakerMode.Mode5point1" />) if that’s what the OS is set to. Best for venues or specs that require 5.0 and for rigs without a subwoofer.
        ///
        ///Pros:
        ///
        ///- Surround contains a center channel and you don't need to manage a subwoofer.
        ///
        ///Cons:
        ///
        ///- 5.0 Surround is less common than 5.1.
        ///- 5.0 Surround has no LFE channel so doesn't handle low sounds and rumbles as well as 5.1 and 7.1 speaker modes.</remarks>
        Surround = 4,
        ///<summary>The Surround 5.1 speaker setup which contains six channels.</summary>
        ///<remarks>Channel count is set to 6. The Surround 5.1 speaker mode contains six channels: 
        ///
        ///- Front left
        ///- Front right
        ///- Front center
        ///- Low-Frequency Effects (LFE subwoofer)
        ///- Rear left
        ///- Rear right
        ///
        ///LFE is a specialized, band-limited audio channel (typically from 3 to 120 Hz) designed to deliver intense, deep bass - such as explosions or rumbles.
        ///
        ///A subwoofer is a speaker that plays very low frequencies (bass), typically from 20 to 120 Hz.
        ///
        ///If not supported, Unity uses what the OS/hardware supports (often Stereo). This speaker mode is common for high-end PC or console setups, TV, movies, theaters, home cinemas, and more.
        ///
        ///Pros:
        ///
        ///- 5.1 has LFE so low sounds and rumbles are captured. 
        ///- 5.1 has more discrete surround than speaker modes with fewer channels. 
        ///
        ///Cons:
        ///
        ///- 5.1 isn't available on most mobile devices.
        ///- You need to test the audio more.
        ///- 5.1 uses more CPU resources than modes with fewer channels.
        ///- This mode might not sound good if Unity needs to downmix it to stereo.</remarks>
        Mode5point1 = 5,
        ///<summary>The Surround 7.1 speaker setup which contains eight channels.</summary>
        ///<remarks>The Surround 7.1 speaker mode contains eight channels: 
        ///
        ///- Front left
        ///- Front right
        ///- Front center
        ///- Low-Frequency Effects (LFE subwoofer)
        ///- Side left
        ///- Side right
        ///- Rear left
        ///- Rear right
        ///
        ///If a device doesn't support this speaker mode, Unity typically falls back to <see cref="AudioSpeakerMode.Mode5point1" /> or <see cref="AudioSpeakerMode.Stereo" />. This speaker mode is common for high-end PC or console setups, TV, movies, theaters, home cinemas, and more.
        ///
        ///Pros: 
        ///
        ///- 7.1 provides better immersion because of all the angles covered. 
        ///
        ///Cons:
        ///
        ///- Fewer devices support this speaker mode.
        ///- You need to test it more.
        ///- 7.1 uses more CPU resources than modes with fewer channels.</remarks>
        Mode7point1 = 6,
        ///<summary>Stereo output, but data is encoded in a way that is picked up by a Pro Logic or Pro Logic 2 decoder and split into a 5.1 speaker setup.</summary>
        ///<remarks>The Pro Logic speaker mode is a surround matrix (up to 5 channels) encoded into 2-channel stereo (Dolby Pro Logic–style). Channel count is set to 2. Only use this speaker mode if you must support matrix decoding.
        ///
        ///Without a decoder, it just plays as normal stereo. 
        ///
        ///Pros:
        ///
        ///- Pro Logic works as stereo.
        ///- You can decode to surround on compatible receivers.
        ///
        ///Cons:
        ///
        ///- This speaker mode isn't discrete.
        ///- Quality depends on the decoder.
        ///- Pro Logic is often treated as plain stereo today.</remarks>
        Prologic = 7,
        ///<summary>The Surround 7.1.4 speaker setup which contains twelve channels.</summary>
        ///<remarks>The Surround 7.1.4 speaker mode contains twelve channels:
        ///
        ///- Front left
        ///- Front right
        ///- Front center
        ///- Low-Frequency Effects (LFE subwoofer)
        ///- Side left
        ///- Side right
        ///- Rear left
        ///- Rear right
        ///- Top front left
        ///- Top front right
        ///- Top rear left
        ///- Top rear right
        ///
        ///This speaker mode is only available when the Enhanced Audio Foundation is active. It can be used through the **Output Channel Layout** in the [Audio Manager](xref:class-AudioManager) project settings. Requesting it through <see cref="AudioSettings.Reset" /> while the Classic Audio Foundation is active throws an ArgumentException.</remarks>
        Mode7point1point4 = 13
    }

    internal enum AudioFoundation
    {
        Classic = 0,
        Enhanced = 1,
    }

	internal enum ChannelLayoutBehavior
    {
        DeviceNative = 0,
        Mono = 1,
        Stereo = 2,
        Quadraphonic = 4,
        Surround_5_0 = 5,
        Surround_5_1 = 6,
        Surround_7_1 = 8,
        Surround_7_1_4 = 12
    };

    internal enum SamplingRateBehavior
    {
        DeviceNative = 0,
        Hz8000 = 8000,
        Hz16000 = 16000,
        Hz22050 = 22050,
        Hz24000 = 24000,
        Hz32000 = 32000,
        Hz44100 = 44100,
        Hz48000 = 48000
    };

    ///<summary>Helpful utility extensions on audio types.</summary>
    public static class AudioExtensions
    {
        [NativeMethod(Name = "AudioSpeakerModeBindings::InternalIAudioSpeakerModeChannelCount", IsFreeFunction = true)]
        internal static extern int InternalIAudioSpeakerModeChannelCount(AudioSpeakerMode speakerMode);

        ///<summary>Calculate the amount of channels represented by this <see cref="AudioSpeakerMode" />.</summary>
        ///<remarks>This is also documented in the page for <see cref="AudioSpeakerMode" />.</remarks>
        public static int ChannelCount(this AudioSpeakerMode speakerMode)
        {
            switch (speakerMode)
            {
                case AudioSpeakerMode.Mono: return 1;
                case AudioSpeakerMode.Stereo: return 2;
                case AudioSpeakerMode.Quad: return 4;
                case AudioSpeakerMode.Surround: return 5;
                case AudioSpeakerMode.Mode5point1: return 6;
                case AudioSpeakerMode.Mode7point1: return 8;
                case AudioSpeakerMode.Prologic: return 2;
                case AudioSpeakerMode.Mode7point1point4: return 12;
            }

            throw new ArgumentException($"{nameof(speakerMode)}");
        }

        [NativeMethod(Name = "AudioSpeakerModeBindings::InternaIAudioSpeakerModeIsCapped", IsFreeFunction = true)]
        internal static extern bool InternalAudioSpeakerModeIsCapped(AudioSpeakerMode speakerMode);
    }

    ///<summary>Value describes the current load state of the audio data associated with an <see cref="AudioClip" />.</summary>
    ///<remarks>This enumeration is useful if you want to: 
    ///
    ///* Only load audio data if the data isn’t already loaded. 
    ///* Perform actions while the audio data loads.  
    ///* Track progress and failures of the load. 
    ///* Make sure certain actions don’t start until the audio starts to load or has finished loading. 
    ///
    ///Use  <see cref="AudioClip.LoadAudioData" /> and <see cref="AudioClip.UnloadAudioData" /> to load and unload the audio data of the AudioClip.</remarks>
    ///<example>
    ///  <code><![CDATA[ // If you click the button, it will load and play the sound you attach to this GameObject.
    /// // If you click the button again, the sound will stop and the audio data will unload. 
    /// // Assign this script to a GameObject and assign a Button and an AudioClip in the Inspector. 
    ///
    ///using UnityEngine;
    ///using System.Collections;
    ///using UnityEngine.UI;
    ///using TMPro;
    ///
    ///public class AudioDataLoadStateExample : MonoBehaviour
    ///{
    ///    public Button playButton;
    ///    public AudioClip audioClip;
    ///    TextMeshProUGUI buttonText;
    ///    AudioSource audioSource;
    ///
    ///    void Awake()
    ///    {
    ///        // Create and attach an AudioSource to the GameObject to play the audio. 
    ///        audioSource = gameObject.AddComponent<AudioSource>();
    ///
    ///        if (audioClip != null)
    ///        {
    ///            audioSource.clip = audioClip;
    ///
    ///            if (playButton != null)
    ///            {
    ///                buttonText = playButton.GetComponentInChildren<TextMeshProUGUI>();
    ///                buttonText.text = "Play";
    ///
    ///                playButton.onClick.AddListener(OnPlayStopButtonClicked);
    ///            }
    ///            else Debug.LogError("Button not assigned in Inspector.");
    ///        }
    ///        else Debug.LogError("AudioClip not assigned in Inspector.");
    ///    }
    ///
    ///    void OnPlayStopButtonClicked()
    ///    {
    ///        // Load and play the audio if the audio isn't playing. 
    ///        if (audioSource.isPlaying == false)
    ///        {
    ///            if (!audioClip.preloadAudioData)
    ///            {
    ///                audioClip.LoadAudioData();
    ///            }
    ///            StartCoroutine(CheckLoadAudioClip());
    ///        }
    ///        // Button clicked in Stop state, so if the audio is playing, stop and unload. 
    ///        else
    ///        {
    ///            audioSource.Stop();
    ///            audioClip.UnloadAudioData();
    ///            // Don't want the audio to be playable again, so remove button. 
    ///            playButton.gameObject.SetActive(false);
    ///        }
    ///    }
    ///
    ///    private IEnumerator CheckLoadAudioClip()
    ///    {
    ///        // Check if the audio clip has finished loading.
    ///        while (audioClip.loadState == AudioDataLoadState.Loading)
    ///        {
    ///            Debug.Log($"AudioClip {audioClip.name} is still loading...");
    ///            yield return null;
    ///        }
    ///
    ///        switch (audioClip.loadState)
    ///        {
    ///            case AudioDataLoadState.Unloaded: 
    ///            { 
    ///                Debug.Log($"AudioClip {audioClip.name} is still unloaded."); 
    ///                break; 
    ///            }
    ///            case AudioDataLoadState.Failed: 
    ///            { 
    ///                Debug.Log($"AudioClip {audioClip.name} failed to load."); 
    ///                break; 
    ///            }
    ///            case AudioDataLoadState.Loaded: 
    ///            {
    ///                Debug.Log($"AudioClip {audioClip.name} is fully loaded.");
    ///                audioSource.Play();
    ///                buttonText.text = "Stop";
    ///                break;
    ///            }
    ///        }
    ///    }
    ///}]]></code>
    ///</example>
    ///<seealso cref="AudioClip.loadState" />
    ///<seealso cref="AudioClip.UnloadAudioData" />
    public enum AudioDataLoadState
    {
        ///<summary>Value returned by AudioClip.loadState for an AudioClip that has no audio data loaded and where loading has not been initiated yet.</summary>
        ///<remarks>This is the initial value of <see cref="AudioClip.loadState" /> that has the option "Preload audio data" unchecked.</remarks>
        Unloaded = 0,
        ///<summary>Value returned by AudioClip.loadState for an AudioClip that is currently loading audio data.</summary>
        Loading = 1,
        ///<summary>Value returned by AudioClip.loadState for an AudioClip that has succeeded loading its audio data.</summary>
        Loaded = 2,
        ///<summary>Value returned by AudioClip.loadState for an AudioClip that has failed loading its audio data.</summary>
        Failed = 3
    }

    ///<summary>Specifies the current properties or desired properties to be set for the audio system.</summary>
    ///<remarks>Use these properties to change how Unity outputs all audio in your project, including how many sounds can play at one time and what speaker mode to use. 
    ///
    ///For a longer example, refer to <see cref="AudioSettings.Reset" />.</remarks>
    ///<example>
    ///  <code><![CDATA[ // This script changes all the settings of the audio configuration programatically. 
    /// // Attach this script to a GameObject in your Scene. Also assign an [[AudioSource]] component in the Inspector and 
    /// // assign an audio clip to the AudioSource. 
    ///
    ///using UnityEngine;
    ///
    ///public class AudioConfigurationExample : MonoBehaviour
    ///{
    ///    void Start()
    ///    {
    ///        AudioSource source = GetComponent<AudioSource>();
    ///
    ///        AudioConfiguration config = AudioSettings.GetConfiguration();
    ///
    ///        // Change each configuration to your preferred setting. 
    ///        config.speakerMode = AudioSpeakerMode.Stereo;
    ///        config.dspBufferSize = 64;
    ///        config.sampleRate = 48000;
    ///        config.numRealVoices = 16;
    ///        config.numVirtualVoices = 128;
    ///
    ///        AudioSettings.Reset(config);
    ///        // Play the audio. 
    ///        source.Play();
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="AudioSpeakerMode" />
    ///<seealso cref="AudioSettings.GetConfiguration" />
    public struct AudioConfiguration
    {
        ///<summary>The current speaker mode used by the audio output device.</summary>
        ///<remarks>For an example of how to use this property, refer to <see cref="AudioSettings.Reset" />.</remarks>
        public AudioSpeakerMode speakerMode;
        ///<summary>The length of the DSP buffer in samples determining the latency of sounds by the audio output device.</summary>
        ///<remarks>This buffer size only accounts for the size of a single DSP buffer. It doesn't include additional latency caused by multiple DSP buffers or buffers from your platform's audio system.
        ///
        ///You can use <see cref="AudioConfiguration.dspBufferSize" /> or <see cref="AudioSettings.GetDSPBufferSize" /> to get the DSP buffer size but it's recommended you use <see cref="AudioConfiguration.dspBufferSize" />.
        ///For a code example that shows each of the DSP buffer sizes, refer to <see cref="AudioSettings.Reset" />.</remarks>
        ///<example>
        ///  <code><![CDATA[ // This script changes the settings of the audio configuration programatically. 
        /// // Attach this script to a GameObject in your Scene. Also assign an [[AudioSource]] component in the Inspector and 
        /// // assign an audio clip to the AudioSource. 
        ///
        ///using UnityEngine;
        ///
        ///public class AudioConfigurationExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        AudioSource source = GetComponent<AudioSource>();
        ///
        ///        AudioConfiguration config = AudioSettings.GetConfiguration();
        ///
        ///        // Change each configuration to your preferred setting. 
        ///        config.speakerMode = AudioSpeakerMode.Stereo;
        ///        config.dspBufferSize = 64;
        ///
        ///        AudioSettings.Reset(config);
        ///        // Play the audio. 
        ///        source.Play();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public int dspBufferSize;
        ///<summary>The current sample rate of the audio output device used.</summary>
        ///<remarks>For an example of how to use this property, refer to <see cref="AudioSettings.Reset" />.</remarks>
        public int sampleRate;
        ///<summary>The current maximum number of simultaneously audible sounds in the game.</summary>
        ///<remarks>**WebGL:** This setting doesn't affect WebGL because there is no limit on the number of audio channels in the WebGL platform.
        ///For an example of how to use this property, refer to <see cref="AudioSettings.Reset" />.</remarks>
        public int numRealVoices;
        ///<summary>The  maximum number of managed sounds in the game. Beyond this limit sounds will simply stop playing.</summary>
        ///<remarks>**WebGL:** This setting doesn't affect WebGL because there is no limit on the number of audio channels in the WebGL platform.
        ///For an example of how to use this property, refer to <see cref="AudioSettings.Reset" />.</remarks>
        public int numVirtualVoices;
    }

    internal struct EnhancedAudioConfiguration
    {
        public AudioFoundation audioFoundation;
        public ChannelLayoutBehavior outputChannelLayout;
        public SamplingRateBehavior outputSampleRate;
    }

    ///<summary>An enum containing different compression types.</summary>
    ///<remarks>This enum is used within the AudioImporter to define what type of compression will be used for an imported AudioClip.</remarks>
    public enum AudioCompressionFormat
    {
        ///<summary>Uncompressed pulse-code modulation.</summary>
        ///<remarks>PCM is uncompressed raw audio data.</remarks>
        PCM = 0,
        ///<summary>Vorbis compression format.</summary>
        ///<remarks>Raw vorbis format, without Ogg headers. This format is an optimised version of Ogg Vorbis that is more performant.</remarks>
        Vorbis = 1,
        ///<summary>Adaptive differential pulse-code modulation.</summary>
        ///<remarks>This compression format is cheap to decode but contains additional noise artifacts over other compression types.</remarks>
        ADPCM = 2,
        ///<summary>MPEG Audio Layer III.</summary>
        ///<remarks>This codec has poor looping characteristics.</remarks>
        MP3 = 3,
        ///<summary>Sony proprietary hardware format.</summary>
        VAG = 4,
        ///<summary>Sony proprietory hardware codec.</summary>
        HEVAG = 5,
        ///<summary>Xbox One proprietary hardware format.</summary>
        XMA = 6,
        ///<summary>AAC Audio Compression.</summary>
        AAC = 7,
        ///<summary>Nintendo ADPCM audio compression format.</summary>
        GCADPCM = 8,
        ///<summary>Sony proprietary hardware format.</summary>
        ATRAC9 = 9
    }

    ///<summary>Determines how the audio clip is loaded in.</summary>
    ///<remarks>Determines whether the audio clip will be either loaded in memory in compressed form, such that every playback will decode the data on the fly ("CompressedInMemory"), decompressed at Scene startup such that the clip can be played back at very low CPU usage and the audio data in it can be modified ("DecompressOnLoad"), or streamed directly from the disk which will typically result in the lowest memory usage, as only the data for a short stream buffer needs to be kept in memory ("Streaming").</remarks>
    public enum AudioClipLoadType
    {
        ///<summary>The audio data is decompressed when the audio clip is loaded.</summary>
        ///<remarks>The audio clip will load the data and make sure it's kept in memory in decompressed form, allowing scripts to modify the audio data.</remarks>
        DecompressOnLoad = 0,
        ///<summary>The audio data of the clip will be kept in memory in compressed form.</summary>
        ///<remarks>The data is fully loaded into memory, but in compressed form, and therefore takes up the least amount of space.</remarks>
        CompressedInMemory = 1,
        ///<summary>Streams audio data from disk.</summary>
        ///<remarks>This generally results in the lowest memory-usage and offloads decoding to a dedicated streaming thread, therefore reducing CPU usage on the mixer thread.</remarks>
        Streaming = 2
    }

    ///<summary>Describes when an <see cref="AudioSource" /> or <see cref="AudioListener" /> is updated.</summary>
    public enum AudioVelocityUpdateMode
    {
        ///<summary>Updates the source or listener in the <see cref="M:UnityEngine.MonoBehaviour.FixedUpdate" /> loop if it is attached to a <see cref="T:UnityEngine.Rigidbody" />, dynamic <see cref="M:UnityEngine.MonoBehaviour.Update" /> otherwise.</summary>
        ///<seealso cref="M:UnityEngine.MonoBehaviour.FixedUpdate" />
        ///<seealso cref="M:UnityEngine.MonoBehaviour.Update" />
        Auto = 0,
        ///<summary>Updates the source or listener in the <see cref="M:UnityEngine.MonoBehaviour.FixedUpdate" /> loop.</summary>
        ///<seealso cref="M:UnityEngine.MonoBehaviour.FixedUpdate" />
        Fixed = 1,
        ///<summary>Updates the source or listener in the dynamic <see cref="M:UnityEngine.MonoBehaviour.Update" /> loop.</summary>
        ///<seealso cref="M:UnityEngine.MonoBehaviour.Update" />
        Dynamic = 2
    }

    ///<summary>Window functions for FFT spectrum analysis.</summary>
    ///<remarks>Pass a value from this enum to the <c>window</c> parameter of <see cref="AudioListener.GetSpectrumData" /> or <see cref="AudioSource.GetSpectrumData" />. Unity applies the window to the time-domain samples before the FFT to reduce spectral leakage (energy spreading from one frequency bin into neighboring bins).
    ///
    ///Stronger windows taper the signal more at the edges of the analysis block, which typically lowers sidelobe leakage but widens the effective main lobe and can reduce frequency resolution. A more complex window can also be less efficient. For background on the tradeoff, see the notes on <see cref="AudioSource.GetSpectrumData" />.
    ///
    ///Typical choices:
    ///
    ///- <see cref="FFTWindow.Rectangular" /> — sharpest bins, most leakage; useful for quick checks or when leakage is acceptable.
    ///- <see cref="FFTWindow.Triangle" /> — light tapering; a step between rectangular and raised-cosine windows.
    ///- <see cref="FFTWindow.Hamming" /> or <see cref="FFTWindow.Hanning" /> — common general-purpose raised-cosine windows.
    ///- <see cref="FFTWindow.Blackman" /> or <see cref="FFTWindow.BlackmanHarris" /> — stronger sidelobe suppression when you need cleaner peaks at the cost of wider bins.</remarks>
    ///<seealso cref="AudioListener.GetSpectrumData" />
    ///<seealso cref="AudioSource.GetSpectrumData" />
    public enum FFTWindow
    {
        // w[n] = 1.0
        ///<summary>Rectangular (no) window for FFT spectrum analysis.</summary>
        ///<remarks>Window function: <c>W[n] = 1.0</c> (no tapering), where <c>n</c> is the sample index and <c>N</c> is the window length.
        ///
        ///Pass this value to the <c>window</c> parameter of <see cref="AudioListener.GetSpectrumData" /> or <see cref="AudioSource.GetSpectrumData" />. Unity still applies this unity window before the FFT.
        ///
        ///The rectangular window preserves the full sample block and gives the narrowest effective main lobe, but it has the most spectral leakage. Use it for quick visualization or when neighboring bins bleeding into each other is acceptable. For analysis where peaks must be distinct, prefer <see cref="FFTWindow.Hamming" />, <see cref="FFTWindow.Hanning" />, or a Blackman-family window.</remarks>
        ///<seealso cref="FFTWindow" />
        ///<seealso cref="FFTWindow.Hamming" />
        Rectangular = 0,
        // w[n] = TRI(2n/N)
        ///<summary>Triangular (Bartlett) window for FFT spectrum analysis.</summary>
        ///<remarks>Window function: <c>W[n] = 1 - abs(2n/N - 1)</c>, where <c>n</c> is the sample index and <c>N</c> is the window length.
        ///
        ///Pass this value to the <c>window</c> parameter of <see cref="AudioListener.GetSpectrumData" /> or <see cref="AudioSource.GetSpectrumData" />. The window is applied before the FFT to reduce spectral leakage between frequency bins.
        ///
        ///The triangular window linearly tapers samples toward the edges of the block. It reduces leakage compared with <see cref="FFTWindow.Rectangular" /> with less taper than <see cref="FFTWindow.Hanning" /> or <see cref="FFTWindow.Hamming" />. Use it as a middle ground when rectangular leakage is visible but a full raised-cosine window is not needed.</remarks>
        ///<seealso cref="FFTWindow" />
        ///<seealso cref="FFTWindow.Rectangular" />
        ///<seealso cref="FFTWindow.Hanning" />
        Triangle = 1,
        // w[n] = 0.54 - (0.46 * COS(n/N) )
        ///<summary>Hamming window for FFT spectrum analysis.</summary>
        ///<remarks>Window function: <c>W[n] = 0.54 - 0.46 * cos(2π * n/N)</c>, where <c>n</c> is the sample index and <c>N</c> is the window length.
        ///
        ///Pass this value to the <c>window</c> parameter of <see cref="AudioListener.GetSpectrumData" /> or <see cref="AudioSource.GetSpectrumData" />. The window is applied before the FFT to reduce spectral leakage between frequency bins.
        ///
        ///The Hamming window is a general-purpose choice when <see cref="FFTWindow.Rectangular" /> shows too much leakage and tighter sidelobe control from <see cref="FFTWindow.Blackman" /> or <see cref="FFTWindow.BlackmanHarris" /> is not required. It is similar to <see cref="FFTWindow.Hanning" /> but uses different coefficients, which affects the balance between frequency resolution and sidelobe levels.</remarks>
        ///<seealso cref="FFTWindow" />
        ///<seealso cref="FFTWindow.Hanning" />
        Hamming = 2,
        // w[n] = 0.5 * (1.0 - COS(n/N) )
        ///<summary>Hanning (Hann) window for FFT spectrum analysis.</summary>
        ///<remarks>Window function: <c>W[n] = 0.5 * (1.0 - cos(2π * n/N))</c>, where <c>n</c> is the sample index and <c>N</c> is the window length. This is also known as the Hann window.
        ///
        ///Pass this value to the <c>window</c> parameter of <see cref="AudioListener.GetSpectrumData" /> or <see cref="AudioSource.GetSpectrumData" />. The window is applied before the FFT to reduce spectral leakage between frequency bins.
        ///
        ///The Hanning window is a common general-purpose raised-cosine window, similar to <see cref="FFTWindow.Hamming" />. Use Hanning when you want smooth tapering at the block edges; compare with Hamming if you need slightly different sidelobe behavior. For stronger leakage control, use <see cref="FFTWindow.Blackman" /> or <see cref="FFTWindow.BlackmanHarris" />.</remarks>
        ///<seealso cref="FFTWindow" />
        ///<seealso cref="FFTWindow.Hamming" />
        Hanning = 3,
        // w[n] = 0.42 - (0.5 * COS(n/N) ) + (0.08 * COS(2.0 * n/N) )
        ///<summary>Blackman window for FFT spectrum analysis.</summary>
        ///<remarks>Window function: <c>W[n] = 0.42 - 0.5 * cos(2π * n/N) + 0.08 * cos(4π * n/N)</c>, where <c>n</c> is the sample index and <c>N</c> is the window length.
        ///
        ///Pass this value to the <c>window</c> parameter of <see cref="AudioListener.GetSpectrumData" /> or <see cref="AudioSource.GetSpectrumData" />. The window is applied before the FFT to reduce spectral leakage between frequency bins.
        ///
        ///Use the Blackman window when you need lower sidelobes than <see cref="FFTWindow.Hamming" /> or <see cref="FFTWindow.Hanning" /> and can accept a wider main lobe. For even stronger sidelobe suppression, use <see cref="FFTWindow.BlackmanHarris" />.</remarks>
        ///<seealso cref="FFTWindow" />
        ///<seealso cref="FFTWindow.BlackmanHarris" />
        ///<seealso cref="FFTWindow.Hamming" />
        Blackman = 4,
        // w[n] = 0.35875 - (0.48829 * COS(1.0 * n/N)) + (0.14128 * COS(2.0 * n/N)) - (0.01168 * COS(3.0 * n/N))
        ///<summary>Blackman-Harris window for FFT spectrum analysis.</summary>
        ///<remarks>Window function: <c>W[n] = 0.35875 - 0.48829 * cos(2π * n/N) + 0.14128 * cos(4π * n/N) - 0.01168 * cos(6π * n/N)</c>, where <c>n</c> is the sample index and <c>N</c> is the window length.
        ///
        ///Pass this value to the <c>window</c> parameter of <see cref="AudioListener.GetSpectrumData" /> or <see cref="AudioSource.GetSpectrumData" />. The window is applied before the FFT to reduce spectral leakage between frequency bins.
        ///
        ///The Blackman-Harris window provides the strongest sidelobe suppression in this enum. Choose it when peaks must be well isolated and leakage from <see cref="FFTWindow.Blackman" /> is still too high. Expect the widest main lobe and the lowest frequency resolution of the listed windows.</remarks>
        ///<seealso cref="FFTWindow" />
        ///<seealso cref="FFTWindow.Blackman" />
        BlackmanHarris = 5
    }

    // Rolloff modes that a 3D sound can have in an audio source.
    ///<summary>Rolloff modes that a 3D sound can have in an audio source.</summary>
    public enum AudioRolloffMode
    {
        ///<summary>Use this mode when you want a real-world rolloff.</summary>
        Logarithmic = 0,
        ///<summary>Use this mode when you want to lower the volume of your sound over the distance.</summary>
        Linear = 1,
        ///<summary>Use this when you want to use a custom rolloff.</summary>
        ///<remarks>**Note:** Currently is not possible to modify the volume curve via scripting.</remarks>
        Custom = 2
    }

    ///<summary>This defines the curve type of the different custom curves that can be queried and set within the AudioSource.</summary>
    ///<remarks>The AudioSource can hold a number of custom distance curves, this enum is used within the AudioSource API to differentiate between them.</remarks>
    public enum AudioSourceCurveType
    {
        ///<summary>Custom Volume Rolloff.</summary>
        ///<remarks>This defines how the AudioSource volume is attenuated with distance from the AudioListener.</remarks>
        CustomRolloff = 0,
        ///<summary>The Spatial Blend.</summary>
        ///<remarks>This defines how 2D or 3D an AudioSource is. 0 means the AudioSource is fully 2D 1 corresponds to the AudioSource being fully 3D.</remarks>
        SpatialBlend  = 1,
        ///<summary>Reverb Zone Mix.</summary>
        ///<remarks>This defines how much of the signal is given to the current Reverb Zone.</remarks>
        ReverbZoneMix = 2,
        ///<summary>The 3D Spread.</summary>
        ///<remarks>This defines the current 3D spread of the playing AudioSource.</remarks>
        Spread        = 3
    }

    ///<summary>Gamepad audio output types.</summary>
    ///<remarks>This API is only for PlayStation®4 and PlayStation®5 platforms. Enclose this API with the UNITY_EDITOR define for platforms other than PlayStation®4 and PlayStation®5.</remarks>
    public enum GamepadSpeakerOutputType
    {
        ///<summary>Audio output is through the gamepads audio speaker if the gamepad supports playing audio.</summary>
        Speaker = 0,
        ///<summary>Audio output is through the gamepads vibration device if the gamepad supports playing audio as vibration.</summary>
        Vibration = 1,
        ///<summary>Audio output is through a secondary gamepad's vibration device if supported.</summary>
        SecondaryVibration = 2
    }


    ///<summary>Reverb presets used by the Reverb Zone class and the audio reverb filter.</summary>
    ///<remarks>Component for audio sources.</remarks>
    public enum AudioReverbPreset
    {
        ///<summary>No reverb preset selected.</summary>
        ///<remarks>All the values are disabled and the sound is the default without
        ///any modifications.</remarks>
        Off = 0,

        ///<summary>Generic preset.</summary>
        Generic = 1,

        ///<summary>Padded cell preset.</summary>
        PaddedCell = 2,

        ///<summary>Room preset.</summary>
        Room = 3,

        ///<summary>Bathroom preset.</summary>
        Bathroom = 4,

        ///<summary>Livingroom preset.</summary>
        Livingroom = 5,

        ///<summary>Stoneroom preset.</summary>
        Stoneroom = 6,

        ///<summary>Auditorium preset.</summary>
        Auditorium = 7,

        ///<summary>Concert hall preset.</summary>
        Concerthall = 8,

        ///<summary>Cave preset.</summary>
        Cave = 9,

        ///<summary>Arena preset.</summary>
        Arena = 10,

        ///<summary>Hangar preset.</summary>
        Hangar = 11,

        ///<summary>Carpeted hallway preset.</summary>
        CarpetedHallway = 12,

        ///<summary>Hallway preset.</summary>
        Hallway = 13,

        ///<summary>Stone corridor preset.</summary>
        StoneCorridor = 14,

        ///<summary>Alley preset.</summary>
        Alley = 15,

        ///<summary>Forest preset.</summary>
        Forest = 16,

        ///<summary>City preset.</summary>
        City = 17,

        ///<summary>Mountains preset.</summary>
        Mountains = 18,

        ///<summary>Quarry preset.</summary>
        Quarry = 19,

        ///<summary>Plain preset.</summary>
        Plain = 20,

        ///<summary>Parking Lot preset.</summary>
        ParkingLot = 21,

        ///<summary>Sewer pipe preset.</summary>
        SewerPipe = 22,

        ///<summary>Underwater presset.</summary>
        Underwater = 23,

        ///<summary>Drugged preset.</summary>
        Drugged = 24,

        ///<summary>Dizzy preset.</summary>
        Dizzy = 25,

        ///<summary>Psychotic preset.</summary>
        Psychotic = 26,

        ///<summary>User defined preset.</summary>
        ///<remarks>Select this preset if you want to change manually the values of
        ///your preset.</remarks>
        User = 27
    }

    internal struct PlayableSettings
    {
        public AudioContainerElement element { get; }
        public double scheduledTime { get; }
        public float pitchOffset { get; }
        public float volumeOffset { get; }
        public double triggerTimeOffset { get; }
    }

    internal struct ActivePlayable
    {
        public PlayableSettings settings { get; }
        public PlayableHandle clipPlayableHandle { get; }
    }

    ///<summary>Defines the types of audio spatialization experience available in player and audio settings.</summary>
    ///<remarks>The <c>AudioSpatializationExperience</c> setting is only supported on visionOS.</remarks>
    ///<seealso cref="AudioSettings.audioSpatialExperience" />
    public enum AudioSpatialExperience
    {
        ///<summary>The spatialization post-processing step is bypassed.</summary>
        Bypassed = 0,
        ///<summary>Spatialization is performed based on the user's position and rotation, so head-tracking is enabled.</summary>
        HeadTracked = 1,
        ///<summary>Spatialization is performed in a fixed, consistent manner. User movements do not affect spatialization.</summary>
        Fixed = 2
    }

    ///<summary>Controls the global audio settings from script.</summary>
    ///<remarks>Setup speaker output and format.</remarks>
    [NativeHeader("Modules/Audio/Public/ScriptBindings/Audio.bindings.h")]
    [StaticAccessor("GetAudioManager()", StaticAccessorType.Dot)]
    public sealed partial class AudioSettings
    {
        extern static private AudioSpeakerMode GetSpeakerMode();
        [NativeMethod(Name = "AudioSettings::SetConfiguration", IsFreeFunction = true, ThrowsException = true)]
        extern static private bool SetConfiguration(AudioConfiguration config);

        [NativeMethod(Name = "AudioSettings::SetEnhancedConfiguration", IsFreeFunction = true)]
        extern static internal bool SetEnhancedConfiguration(EnhancedAudioConfiguration config);

        [NativeMethod(Name = "AudioSettings::GetSampleRate", IsFreeFunction = true)]
        extern static private int GetSampleRate();

        extern static private bool SetSpatializerName(string pluginName);

        ///<summary>Returns the speaker mode capability of the current audio driver. (RO)</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class DriverCapabilitiesExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Request the highest speaker mode supported by the current audio driver.
        ///        AudioConfiguration config = AudioSettings.GetConfiguration();
        ///        config.speakerMode = AudioSettings.driverCapabilities;
        ///        if (!AudioSettings.Reset(config))
        ///            Debug.LogWarning("Failed to apply speaker mode from driver capabilities.");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern static public AudioSpeakerMode driverCapabilities
        {
            [NativeName("GetSpeakerModeCaps")]
            get;
        }

        ///<summary>
        ///  <c>AudioSettings.speakerMode</c> is deprecated. Use <see cref="AudioSettings.GetConfiguration" /> and <see cref="AudioSettings.Reset" /> to adjust audio settings instead.</summary>
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
        ///<summary>Returns the current time of the audio system.</summary>
        ///<remarks>This is a value specified in seconds and based on the actual number of samples the audio system processes and is therefore much more precise than the time obtained via the <see cref="Time.time" /> property.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        /// // The code example shows how to implement a metronome that procedurally generates the click sounds via the OnAudioFilterRead callback.
        /// // While the game is paused or suspended, this time will not be updated and sounds playing will be paused. Therefore developers of music scheduling routines do not have to do any rescheduling after the app is unpaused
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public double bpm = 140.0F;
        ///    public float gain = 0.5F;
        ///    public int signatureHi = 4;
        ///    public int signatureLo = 4;
        ///    private double nextTick = 0.0F;
        ///    private float amp = 0.0F;
        ///    private float phase = 0.0F;
        ///    private double sampleRate = 0.0F;
        ///    private int accent;
        ///    private bool running = false;
        ///    void Start()
        ///    {
        ///        accent = signatureHi;
        ///        double startTick = AudioSettings.dspTime;
        ///        sampleRate = AudioSettings.outputSampleRate;
        ///        nextTick = startTick * sampleRate;
        ///        running = true;
        ///    }
        ///
        ///    void OnAudioFilterRead(float[] data, int channels)
        ///    {
        ///        if (!running)
        ///            return;
        ///
        ///        double samplesPerTick = sampleRate * 60.0F / bpm * 4.0F / signatureLo;
        ///        double sample = AudioSettings.dspTime * sampleRate;
        ///        int dataLen = data.Length / channels;
        ///        int n = 0;
        ///        while (n < dataLen)
        ///        {
        ///            float x = gain * amp * Mathf.Sin(phase);
        ///            int i = 0;
        ///            while (i < channels)
        ///            {
        ///                data[n * channels + i] += x;
        ///                i++;
        ///            }
        ///            while (sample + n >= nextTick)
        ///            {
        ///                nextTick += samplesPerTick;
        ///                amp = 1.0F;
        ///                if (++accent > signatureHi)
        ///                {
        ///                    accent = 1;
        ///                    amp *= 2.0F;
        ///                }
        ///                Debug.Log("Tick: " + accent + "/" + signatureHi);
        ///            }
        ///            phase += amp * 0.3F;
        ///            amp *= 0.993F;
        ///            n++;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern static public double dspTime
        {
            [NativeMethod(Name = "GetDSPTime", IsThreadSafe = true)]
            get;
        }

        ///<summary>Get the mixer's current output rate.</summary>
        ///<remarks>As of version 5.0 setting the sample rate from scripts is no longer supported. This has to be set in the Audio section of the project settings instead.</remarks>
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

        ///<summary>Get the mixer's buffer size in samples.</summary>
        ///<remarks>The buffer size can be set from 'Project Settings -&gt; Audio -&gt; DSP Buffer size'.
        ///
        ///The software mixer mixes to a ring buffer and the size of this ring buffer is determined here. It mixes a block of sound data every 'bufferLength' number of samples, and there are 'numBuffers' number of these blocks that make up the entire ring buffer. Adjusting these values can lead to extremely low latency performance (smaller values), or greater stability in sound output (larger values). The 'buffersize' is generally best left alone. Making the granularity smaller will just increase CPU usage (cache misses and DSP network overhead). Making it larger affects how often you hear commands update such as volume/pitch/pan changes. Anything above 20 ms will be noticeable and sound parameter changes will be obvious instead of smooth. Unity chooses the most optimal size by default for best stability, considering the output type and the drivers being used. It is not recommended changing this value unless you really need to. You may get worse performance than the default settings chosen by Unity.</remarks>
        ///<param name="bufferLength">Is the length of each buffer in the ring buffer.</param>
        ///<param name="numBuffers">Is number of buffers.</param>
        ///<seealso href="xref:class-AudioSettings">Audio Settings</seealso>
        [NativeMethod(Name = "AudioSettings::GetDSPBufferSize", IsFreeFunction = true)]
        extern static public void GetDSPBufferSize(out int bufferLength, out int numBuffers);

        extern static internal bool editingInPlaymode
        {
            [NativeName("IsEditingInPlaymode")]
            get;

            [NativeName("SetEditingInPlaymode")]
            set;
        }

        ///<summary>Returns an array with the names of all the available spatializer plugins.</summary>
        ///<remarks>This is an Editor-only function.</remarks>
        ///<returns>An array of spatializer names.</returns>
        [NativeMethod(Name = "AudioSettings::GetSpatializerNames", IsFreeFunction = true)]
        extern static public string[] GetSpatializerPluginNames();

        ///<summary>Returns the name of the spatializer selected on the currently-running platform.</summary>
        ///<remarks>This function can be used in the Editor and in player builds.</remarks>
        ///<returns>The spatializer plugin name.</returns>
        [NativeName("GetCurrentSpatializerDefinitionName")]
        extern static public string GetSpatializerPluginName();

        ///<summary>Sets the spatializer plugin for all platform groups. If a null or empty string is passed in, the existing spatializer plugin will be cleared.</summary>
        ///<remarks>This is an Editor-only function. This function will throw an argument exception on an invalid plugin name.</remarks>
        ///<param name="pluginName">The spatializer plugin name.</param>
        static public void SetSpatializerPluginName(string pluginName)
        {
            if (!SetSpatializerName(pluginName))
                throw new ArgumentException("Invalid spatializer plugin name");
        }


        ///<summary>Returns the current configuration of the audio device and system. The values in the struct may then be modified and reapplied via <see cref="AudioSettings.Reset" />.</summary>
        ///<remarks>See <see cref="AudioSettings.Reset" /> for an example.</remarks>
        ///<returns>The new configuration to be applied.</returns>
        extern static public AudioConfiguration GetConfiguration();
        extern static internal EnhancedAudioConfiguration GetEnhancedConfiguration();

        ///<summary>Changes the device configuration and invokes the <see cref="AudioSettings.OnAudioConfigurationChanged" /> delegate with the argument <c>deviceWasChanged=false</c>. There's no guarantee that the exact settings specified are used, but Unity automatically uses the closest match that it supports. **Note:** This can cause main thread stalls if <c>AudioSettings.Reset</c> is called when objects are loading asynchronously.</summary>
        ///<param name="config">The new configuration to be used.</param>
        ///<returns>True if all settings could be successfully applied.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class TestAudioConfiguration : MonoBehaviour
        ///{
        ///    void OnEnable()
        ///    {
        ///        AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        ///    }
        ///
        ///    void OnDisable()
        ///    {
        ///        AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        ///    }
        ///
        ///    void OnAudioConfigurationChanged(bool deviceWasChanged)
        ///    {
        ///        Debug.Log(deviceWasChanged ? "Device was changed" : "Reset was called");
        ///        if (deviceWasChanged)
        ///        {
        ///            AudioConfiguration config = AudioSettings.GetConfiguration();
        ///            config.dspBufferSize = 64;
        ///            AudioSettings.Reset(config);
        ///        }
        ///        GetComponent<AudioSource>().Play();
        ///    }
        ///
        ///    static int[] validSpeakerModes =
        ///    {
        ///        (int)AudioSpeakerMode.Mono,
        ///        (int)AudioSpeakerMode.Stereo,
        ///        (int)AudioSpeakerMode.Quad,
        ///        (int)AudioSpeakerMode.Surround,
        ///        (int)AudioSpeakerMode.Mode5point1,
        ///        (int)AudioSpeakerMode.Mode7point1
        ///    };
        ///
        ///    static int[] validDSPBufferSizes =
        ///    {
        ///        32, 64, 128, 256, 340, 480, 512, 1024, 2048, 4096, 8192
        ///    };
        ///
        ///    static int[] validSampleRates =
        ///    {
        ///        11025, 22050, 44100, 48000, 88200, 96000,
        ///    };
        ///
        ///    static int[] validNumRealVoices =
        ///    {
        ///        1, 2, 4, 8, 16, 32, 50, 64, 100, 128, 256, 512,
        ///    };
        ///
        ///    static int[] validNumVirtualVoices =
        ///    {
        ///        1, 2, 4, 8, 16, 32, 50, 64, 100, 128, 256, 512,
        ///    };
        ///
        ///    int GUIRow(string name, int[] valid, int value, ref bool modified)
        ///    {
        ///        GUILayout.BeginHorizontal();
        ///        GUILayout.Button(name + "=" + value);
        ///        for (int i = 0; i < valid.Length; i++)
        ///        {
        ///            string s = valid[i].ToString();
        ///            if (valid[i] == value)
        ///                s = "[" + s + "]";
        ///            if (GUILayout.Button(s))
        ///            {
        ///                value = valid[i];
        ///                modified = true;
        ///            }
        ///        }
        ///        GUILayout.EndHorizontal();
        ///        return value;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        AudioSource source = GetComponent<AudioSource>();
        ///        bool modified = false;
        ///
        ///        AudioConfiguration config = AudioSettings.GetConfiguration();
        ///
        ///        config.speakerMode = (AudioSpeakerMode)GUIRow("speakerMode", validSpeakerModes, (int)config.speakerMode, ref modified);
        ///        config.dspBufferSize = GUIRow("dspBufferSize", validDSPBufferSizes, config.dspBufferSize, ref modified);
        ///        config.sampleRate = GUIRow("sampleRate", validSampleRates, config.sampleRate, ref modified);
        ///        config.numRealVoices = GUIRow("RealVoices", validNumRealVoices, config.numRealVoices, ref modified);
        ///        config.numVirtualVoices = GUIRow("numVirtualVoices", validNumVirtualVoices, config.numVirtualVoices, ref modified);
        ///
        ///        if (modified)
        ///            AudioSettings.Reset(config);
        ///
        ///        if (GUILayout.Button("Start"))
        ///            source.Play();
        ///
        ///        if (GUILayout.Button("Stop"))
        ///            source.Stop();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public bool Reset(AudioConfiguration config)
        {
            return SetConfiguration(config);
        }

        static internal bool Reset(EnhancedAudioConfiguration config)
        {
            return SetEnhancedConfiguration(config);
        }

        ///<summary>A delegate called whenever the global audio settings are changed, either by <see cref="AudioSettings.Reset" /> or by an external device change such as the OS control panel changing the sample rate or because the default output device was changed, for example when plugging in an HDMI monitor or a USB headset.</summary>
        ///<remarks>See <see cref="AudioSettings.Reset" /> for an example.</remarks>
        ///<param name="deviceWasChanged">True if the change was caused by an device change.</param>
        public delegate void AudioConfigurationChangeHandler(bool deviceWasChanged);

        ///<summary>Unity calls this event whenever the global audio settings change.</summary>
        ///<remarks>The settings change when you use <see cref="AudioSettings.Reset" />, but an external factor can also change them. For example: 
        ///
        ///* The OS control panel changes the sample rate. 
        ///* The user changes the default output device, for example if they plug in an HDMI monitor or a USB headset.
        ///
        ///For a code example with a large range of setting options, refer to <see cref="AudioSettings.Reset" />.</remarks>
        ///<example>
        ///  <code><![CDATA[ // This script creates a row of buttons, one for each [[AudioSpeakerMode]]. 
        /// // When you press one of the buttons, Unity will play the audio with the new speaker mode. 
        /// // Attach this script and an AudioSource component (with an audio clip) to a GameObject in your Scene.
        /// // If any of the options are not available on your system, it will throw an error. 
        ///
        ///using UnityEngine;
        ///using System;
        ///
        ///public class AudioConfigurationChangedExample : MonoBehaviour
        ///{
        ///    void OnEnable()
        ///    {
        ///        AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        ///    }
        ///
        ///    void OnDisable()
        ///    {
        ///        AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        ///    }
        ///
        ///    void OnAudioConfigurationChanged(bool deviceWasChanged)
        ///    {
        ///        Debug.Log(deviceWasChanged ? "Device was changed" : "Reset was called");
        ///        if (deviceWasChanged)
        ///        {
        ///            AudioConfiguration config = AudioSettings.GetConfiguration();
        ///            config.dspBufferSize = 512;
        ///            if (!AudioSettings.Reset(config))
        ///            {
        ///                Debug.LogError("Failed to reset AudioConfiguration after device change.");
        ///            }
        ///        }
        ///        GetComponent<AudioSource>().Play();
        ///    }
        ///
        ///    AudioSpeakerMode GUIRow(AudioSpeakerMode value, ref bool modified)
        ///    {
        ///        // Add all the values in the enum to an array. 
        ///        Array audioSpeakerModes = Enum.GetValues(typeof(AudioSpeakerMode));
        ///
        ///        GUILayout.BeginHorizontal();
        ///        GUILayout.Button("Speaker mode = " + value.ToString());
        ///
        ///        // Loop through the AudioSpeakerMode enum. 
        ///        foreach (AudioSpeakerMode speakerMode in audioSpeakerModes)
        ///        {
        ///            // Set the button name to the name of the enum value. 
        ///            string s = speakerMode.ToString();
        ///
        ///            // Add brackets to the button name to show the current selected button. 
        ///            if (speakerMode == value)
        ///                s = "[" + s + "]";
        ///
        ///            // Create a button for each valid speaker mode. 
        ///            if (GUILayout.Button(s))
        ///            {
        ///                value = speakerMode;
        ///                modified = true;
        ///            }
        ///        }
        ///
        ///        GUILayout.EndHorizontal();
        ///        return value;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        bool modified = false;
        ///
        ///        AudioConfiguration config = AudioSettings.GetConfiguration();
        ///
        ///        config.speakerMode = GUIRow(config.speakerMode, ref modified);
        ///
        ///        if (modified)
        ///            AudioSettings.Reset(config);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [AutoStaticsCleanupOnCodeReload]
        static public event AudioConfigurationChangeHandler OnAudioConfigurationChanged;
        [AutoStaticsCleanupOnCodeReload]
        internal static event Action OnAudioSystemShuttingDown;
        [AutoStaticsCleanupOnCodeReload]
        internal static event Action OnAudioSystemStartedUp;

        [RequiredByNativeCode]
        static internal void InvokeOnAudioConfigurationChanged(bool deviceWasChanged)
        {
            if (OnAudioConfigurationChanged != null)
                OnAudioConfigurationChanged(deviceWasChanged);
        }

        [RequiredByNativeCode]
        internal static void InvokeOnAudioSystemShuttingDown()
            => OnAudioSystemShuttingDown?.Invoke();

        [RequiredByNativeCode]
        internal static void InvokeOnAudioSystemStartedUp()
            => OnAudioSystemStartedUp?.Invoke();

        extern static internal bool unityAudioDisabled
        {
            [NativeName("IsAudioDisabled")]
            get;
            [NativeName("DisableAudio")]
            set;
        }

        [NativeMethod(Name = "AudioSettings::GetCurrentAmbisonicDefinitionName", IsFreeFunction = true)]
        extern static internal string GetAmbisonicDecoderPluginName();

        [NativeMethod(Name = "AudioSettings::SetAmbisonicName", IsFreeFunction = true)]
        extern static internal void SetAmbisonicDecoderPluginName(string name);

        ///<summary>Determines if the Unity app's final mixed audio stream is spatialized.</summary>
        ///<remarks>At startup, the value set via the player settings is used. That initial setting can be changed dynamically at runtime via this C# property. This setting is only supported on visionOS.</remarks>
        static public AudioSpatialExperience audioSpatialExperience
        {
            get { return AudioSpatialExperience.Bypassed; }
            set { Debug.LogWarning("AudioSettings.audioSpatialExperience is not implemented on this platform."); }
        }

        ///<summary>This class encapsulates properties and methods to handle audio output thread on iOS/Android.</summary>
        ///<remarks>You may be able to reduce the power consumption of your app or game by using this class. In general, for apps with simple
        ///looped music and short sound effects, you should set <see cref="AudioSettings.Mobile.stopAudioOutputOnMute" /> to true, and also call
        ///<see cref="AudioSettings.Mobile.StopAudioOutput" /> if the user of your app sets music/sound volume to 0 in game settings. This helps to reduce
        ///power consumption on most mobile devices. Also you can check <see cref="AudioSettings.Mobile.muteState" /> property and listen to
        ///<see cref="AudioSettings.Mobile.OnMuteStateChanged" /> event to stop/start audio output thread when required.
        ///
        ///However, if your game or app has more complex sound or music logic, doing this could cause synchronization issues. In particular, any sounds playing
        ///when output is stopped are resumed from the same position when output is restarted, and so may be out of sync with any gameplay code that continued
        ///to run during that time. Therefore this setting may not be suitable if you are relying on gameplay elements that should be synchronized with parts of
        ///audio that may be continuing to play during output being switched off or on.</remarks>
        public static partial class Mobile
        {
            ///<summary>Returns true if current device media volume is 0.</summary>
            ///<remarks>**Note**: On iOS mute switch state is not detected because there is no native iOS API to detect if the mute switch is enabled/disabled.</remarks>
            static public bool muteState
            {
                get { return false; }
            }

            ///<summary>Set this property to true to make audio output thread automatically stop when device media volume is set to 0 and to start it again when volume is not 0.</summary>
            ///<remarks>Default value is false.
            ///
            ///**Note**: Setting this property to true when device media volume is 0 stops audio output thread. Setting it to false when audio output thread is stopped starts this thread.</remarks>
            static public bool stopAudioOutputOnMute
            {
                get { return false; }
                set
                {
                    Debug.LogWarning("Setting AudioSettings.Mobile.stopAudioOutputOnMute is possible on iOS and Android only");
                }
            }

            ///<summary>Returns true if audio output thread is working.</summary>
            static public bool audioOutputStarted
            {
                get { return true; }
            }

#pragma warning disable 0067
            ///<summary>A delegate called whenever the device mute state is changed.</summary>
            ///<remarks>**Note**: When this delegate is called <see cref="AudioSettings.Mobile.muteState" /> property is also updated.</remarks>
            [AutoStaticsCleanupOnCodeReload]
            static public event Action<bool> OnMuteStateChanged;
#pragma warning restore 0067

            ///<summary>Starts audio output thread on Android/iOS.</summary>
            ///<remarks>Has no effect if audio output thread is already running.</remarks>
            static public void StartAudioOutput()
            {
                Debug.LogWarning("AudioSettings.Mobile.StartAudioOutput is implemented for iOS and Android only");
            }

            ///<summary>Stops audio thread on Android/iOS.</summary>
            ///<remarks>Has no effect if audio output thread is not running.
            ///
            ///**Note**: When audio output thread is stopped <see cref="AudioSource.time" />, <see cref="AudioSource.timeSamples" /> and <see cref="AudioSettings.dspTime" /> are not changing.
            ///At the same time <see cref="AudioSource.isPlaying" /> remains true if it was true before stopping audio output thread.
            /// Also <see cref="M:UnityEngine.MonoBehaviour.OnAudioFilterRead" /> callback, Native Audio Plugins callbacks and Audio Spatializer callbacks are not being called.</remarks>
            static public void StopAudioOutput()
            {
                Debug.LogWarning("AudioSettings.Mobile.StopAudioOutput is implemented for iOS and Android only");
            }
        }
    }

    // A container for audio data.
    ///<summary>A container for audio data.</summary>
    ///<remarks>An AudioClip stores the audio file either compressed as ogg vorbis or uncompressed.
    ///AudioClips are referenced and used by AudioSources to play sounds.</remarks>
    ///<seealso href="xref:class-AudioClip">AudioClip component</seealso>
    [NativeHeader("Modules/Audio/Public/ScriptBindings/Audio.bindings.h")]
    [global::UnityEngine.NativeClass("AudioClip", PersistentTypeId = 83)]
    [StaticAccessor("AudioClipBindings", StaticAccessorType.DoubleColon)]
    public sealed partial class AudioClip : AudioResource, IAudioGenerator
    {
        private AudioClip() {}

        extern static private bool GetData([NotNull] AudioClip clip, Span<float> data, int samplesOffset);
        extern static private bool SetData([NotNull] AudioClip clip, ReadOnlySpan<float> data, int samplesOffset);
        extern static private AudioClip Construct_Internal();

        extern private string GetName();
        extern private void CreateUserSound(string name, int lengthSamples, int channels, int frequency, bool stream);
        extern private bool IsLegacyFormat();

        ///<summary>The length of the audio clip in seconds. (RO)</summary>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach an AudioSource component to a GameObject along with this script.
        /// //Click and drag or choose an Audio clip to the __AudioClip__ field in the AudioSource.
        /// //Click and drag or choose a different Audio clip for the Audio Clip 2 field in the Inspector window.
        ///
        /// //This script switches between two Audio clips and outputs each of their lengths in the console
        /// //In Play Mode, press the space key to switch between the Audio clips
        ///
        ///using UnityEngine;
        ///using UnityEngine.Audio;
        ///
        ///public class AudioClipLengthExample : MonoBehaviour
        ///{
        ///    //Make sure your GameObject has an AudioSource component first
        ///    AudioSource m_AudioSource;
        ///
        ///    //Make sure to set an Audio Clip in the AudioSource component
        ///    AudioClip m_AudioClip;
        ///
        ///    //Make sure you set an AudioClip in the Inspector window
        ///    public AudioClip m_AudioClip2;
        ///
        ///    void Start()
        ///    {
        ///        //Fetch the AudioSource from the GameObject
        ///        m_AudioSource = GetComponent<AudioSource>();
        ///
        ///        //Set the original AudioClip as this clip
        ///        m_AudioClip = m_AudioSource.clip;
        ///
        ///        //Output the current clip's length
        ///        Debug.Log("Audio clip length : " + m_AudioSource.clip.length);
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Press this key to switch Audio Clips
        ///        if (Input.GetKeyDown(KeyCode.Space))
        ///        {
        ///            SwitchAudio();
        ///        }
        ///    }
        ///
        ///    void SwitchAudio()
        ///    {
        ///        //If the current Audio clip is the original Audio clip, switch to the second clip
        ///        if (m_AudioSource.clip == m_AudioClip)
        ///        {
        ///            //Switch to the second clip
        ///            m_AudioSource.clip = m_AudioClip2;
        ///
        ///            //Play the second clip
        ///            m_AudioSource.Play();
        ///        }
        ///        //Otherwise, if the current Audio clip is the second clip, switch back
        ///        else if (m_AudioSource.clip == m_AudioClip2)
        ///        {
        ///            //Switch back to the original Audio clip
        ///            m_AudioSource.clip = m_AudioClip;
        ///
        ///            //Play the original clip
        ///            m_AudioSource.Play();
        ///        }
        ///
        ///        //Output the length of the current Audio clip
        ///        Debug.Log("Audio clip length : " + m_AudioSource.clip.length);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("LengthSec")]
        extern public float length { get; }

        // The length of the audio clip in samples (read-only)
        ///<summary>The length of the audio clip in samples. (RO)</summary>
        [NativeProperty("SampleCount")]
        extern public int samples { get; }

        ///<summary>The number of channels in the audio clip. (RO)</summary>
        [NativeProperty("ChannelCount")]
        extern public int channels { get; }

        ///<summary>The sample frequency of the clip in Hertz. (RO)</summary>
        extern public int frequency { get; }

        ///<summary>The load type of the clip (read-only).</summary>
        ///<remarks>The load type, which can be set up in the inspector of the AudioClip, controls how the clip is being loaded.</remarks>
        extern public AudioClipLoadType loadType { get; }

        ///<summary>Loads the asset data of an <see cref="AudioClip" /> into memory, so it will immediately be ready to play.</summary>
        ///<remarks>Use this method when your application can afford an upfront performance overhead. The overhead happens because this method loads resources in ahead-of-time. It's recommended that you use <c>AudioClip.LoadAudioData</c> in methods like <c>Start()</c> of MonoBehaviour.
        ///
        ///If you don't use this method and use <see cref="AudioSource.Play" /> on an unloaded audio clip, the clip still loads in before playback, but it can cause delays due to the clip loading in dynamically. In contrast, <c>AudioClip.LoadAudioData</c> loads your audio clips in advance so they're ready to immediately play when necessary.
        ///
        ///Key details about <c>AudioClip.LoadAudioData</c>:
        ///
        ///* If you call it on an audio clip that is already loaded, it does nothing and returns <c>true</c>.
        ///* It doesn't reload clips that have <see cref="AudioClip.preloadAudioData" /> set to <c>true</c>.
        ///* It loads the <see cref="AudioClip" /> synchronously, unless <see cref="AudioClip.loadInBackground" /> is set to <c>true</c>.
        ///* If you play a clip that is loading in the background, the audio source will delay playback until the clip is ready to play. 
        ///* You can use <see cref="AudioClip.loadState" /> to monitor the current load state of the audio data.</remarks>
        ///<returns>Returns true if the clip is loaded into memory.</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///public class LoadClipAtStart : MonoBehaviour
        ///{
        ///    AudioClip m_Clip;
        ///    void Start()
        ///    {
        ///        m_Clip.LoadAudioData();
        ///        //Components that use AudioClip, for example AudioSource, are ready to immediately use the audio data, rather
        ///        // than triggering a load themselves.
        ///
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool LoadAudioData();
        ///<summary>Unloads the audio data associated with the clip. This works only for AudioClips that are based on actual sound file assets.</summary>
        ///<remarks>This is useful because when you unload the audio data, you free up the memory the audio data uses. You can use this function to optimize memory and not have assets that you aren't currently using taking up space in memory. If you want to play or process the audio again, you need to use <see cref="AudioClip.LoadAudioData" />.
        ///
        ///If the AudioClip is being used as a generator via <see cref="AudioClip.CreateInstance" />, calling <c>UnloadAudioData</c> marks any active generator instances as finished. Those instances will drain any remaining buffered audio data before completing playback.</remarks>
        ///<returns>Returns `true` if the audio data unloads successfully.</returns>
        ///<example>
        ///  <code><![CDATA[ // If you click the button, it will load and play the sound you attach to this GameObject.
        /// // If you click the button again, the sound will stop and the audio data will unload. 
        /// // Assign this script to a GameObject and assign a Button and an AudioClip in the Inspector. 
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///using UnityEngine.UI;
        ///using TMPro;
        ///
        ///public class AudioUnloadExample : MonoBehaviour
        ///{
        ///    public Button playButton; 
        ///    public AudioClip audioClip;
        ///    TextMeshProUGUI buttonText;
        ///    AudioSource audioSource;
        ///
        ///    void Awake()
        ///    {
        ///        // Create and attach an AudioSource to the GameObject to play the audio. 
        ///        audioSource = gameObject.AddComponent<AudioSource>();
        ///
        ///        if (audioClip != null)
        ///        {
        ///            audioSource.clip = audioClip;
        ///            
        ///            if (playButton != null)
        ///            {
        ///                buttonText = playButton.GetComponentInChildren<TextMeshProUGUI>();
        ///                buttonText.text = "Play";
        ///
        ///                playButton.onClick.AddListener(OnPlayStopButtonClicked);
        ///            }
        ///            else Debug.LogError("Button not assigned in Inspector.");
        ///        }
        ///        else Debug.LogError("AudioClip not assigned in Inspector."); 
        ///    }
        ///
        ///    void OnPlayStopButtonClicked()
        ///    {
        ///        // Load and play the audio if the audio isn't playing. 
        ///        if(audioSource.isPlaying == false)
        ///        {
        ///            if (!audioClip.preloadAudioData)
        ///            {
        ///                audioClip.LoadAudioData();
        ///            }
        ///            StartCoroutine(CheckLoadAudioClip());
        ///        }
        ///        // Button clicked in Stop state, so if the audio is playing, stop and unload. 
        ///        else
        ///        {
        ///            audioSource.Stop();
        ///            audioClip.UnloadAudioData();
        ///            // Don't want the audio to be playable again, so remove button. 
        ///            playButton.gameObject.SetActive(false);
        ///        }
        ///    }
        ///
        ///    private IEnumerator CheckLoadAudioClip()
        ///    {
        ///        // Check if the audio clip has finished loading.
        ///        while (audioClip.loadState == AudioDataLoadState.Loading)
        ///        {
        ///            Debug.Log($"AudioClip {audioClip.name} is still loading...");
        ///            yield return null;
        ///        }
        ///        // When the audio loads, play the clip and change the button's text. 
        ///        if (audioClip.loadState == AudioDataLoadState.Loaded)
        ///        {
        ///            Debug.Log($"AudioClip {audioClip.name} is fully loaded.");
        ///            audioSource.Play();
        ///            buttonText.text = "Stop";
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool UnloadAudioData();

        ///<summary>Enable this property in the Inspector to preload audio data from the audio clip when loading the clip Asset (RO).</summary>
        ///<remarks>This can help prevent delays when you play an audio clip because the data is already loaded. If you disable this property, you need to call <see cref="AudioClip.LoadAudioData" /> to load the data before you play the clip. Properties like length, channels, and format are available before Unity loads the audio data.
        ///You can’t change this property during runtime. To change this setting before you enter Play mode, set **Preload Audio Data** in the Inspector of the audio clip or use <see cref="P:UnityEditor.AudioImporterSampleSettings.preloadAudioData" />.</remarks>
        extern public bool preloadAudioData { get; }

        ///<summary>Returns true if this audio clip is ambisonic (read-only).</summary>
        ///<remarks>Corresponds to the ambisonic flag in the AudioClip's inspector.</remarks>
        extern public bool ambisonic { get; }

        [NativeMethod(Name = "AudioClipBindings::IsValidAmbisonicChannelCount", IsFreeFunction = true)]
        internal static extern bool IsValidAmbisonicChannelCount(int channels);

        ///<summary>Enable this property to load the AudioClip asynchronously in the background instead of on the main thread. Set this property in the Inspector (RO).</summary>
        ///<remarks>This property is useful if you have a lot of files or large files to load. If you load them in a separate thread from the main thread, it can help prevent frame rate drops. You can’t change this property during runtime so if you need to set this property, do one of the following before you enter Play mode: 
        ///
        ///* Enable **Load In Background** in the AudioClip’s Inspector.
        ///* Use <see cref="P:UnityEditor.AudioImporter.loadInBackground" />.</remarks>
        ///<example>
        ///  <code><![CDATA[ // This script outputs the load status and the loadInBackground setting for each clip. 
        /// // Attach this script to a GameObject in your Scene. 
        /// // Fill the __Audio Clips To Preload__ array with audio clips. 
        /// // In each audio clip, enable __Preload Asset Data__ in the Inspector. 
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class LoadInBackgroundExample : MonoBehaviour
        ///{
        ///    public AudioClip[] audioClipsToLoad;
        ///
        ///    void Start()
        ///    {
        ///        // Preload audio clips. 
        ///        StartCoroutine(LoadAudioClips());
        ///    }
        ///
        ///    private IEnumerator LoadAudioClips()
        ///    {
        ///        foreach (AudioClip clip in audioClipsToLoad)
        ///        {
        ///            // Check if the clip is set to load in the background. 
        ///            if (clip.loadInBackground)
        ///            {
        ///                Debug.Log($"Loading {clip.name} in background.");
        ///            }
        ///            else
        ///            {
        ///                Debug.LogWarning($"AudioClip {clip.name} is NOT set to load in the background.");
        ///            }
        ///
        ///            // Check if the audio clip has finished loading. 
        ///            while (clip.loadState == AudioDataLoadState.Loading)
        ///            {
        ///                Debug.Log($"AudioClip {clip.name} is still loading.");
        ///                yield return null; 
        ///            }
        ///            Debug.Log($"AudioClip {clip.name} is fully loaded.");
        ///        }
        ///        Debug.Log("Loading complete.");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool loadInBackground { get; }

        ///<summary>Returns the current load state of the audio data associated with an AudioClip.</summary>
        extern public AudioDataLoadState loadState
        {
            [NativeMethod(Name = "AudioClipBindings::GetLoadState", HasExplicitThis = true)]
            get;
        }

        ///<summary>Fills an array with sample data from the audio clip.</summary>
        ///<remarks>
        ///  <para>The sample data contains float values within the range -1.0f to 1.0f. The sample count is the length of the Span or float array. Use the <c>offsetSamples</c> parameter to start the read from a specific position in the clip. If the read length from the offset is longer than the clip length, the read wraps around and reads the remaining samples from the start of the clip.
        ///
        ///For multi-channel audio clips, the data is interleaved, which means the samples in the returned float array alternate between channels. For example, in a stereo audio clip:
        ///
        ///* Index 0 corresponds to channel 1 (left).
        ///* Index 1 corresponds to channel 2 (right).
        ///* Index 2 corresponds to the next sample for channel 1, and so on.
        ///
        ///With compressed audio files, you can only retrieve the sample data if you set the **Load Type** to **Decompress on Load** in the AudioClip importer. <c>GetData</c> doesn't work with streamed audio clips, including if you stream clips from the disk or use <see cref="AudioClip.Create" /> and set its <c>stream</c> parameter to <c>true</c>.
        ///
        ///**Note**: Once an AudioClip has been used as a generator via <see cref="AudioClip.CreateInstance" />, <c>GetData</c> is permanently blocked for that clip instance. Calling it returns <c>false</c> and logs a warning.
        ///
        ///If <c>GetData</c> can't read the audio clip, the <c>data</c> parameter will contain zeroes for all sample values, the console will log an error, and <c>GetData</c> will return false.
        ///
        ///For the best performance, use the Span version because you don't need to allocate managed memory.</para>
        ///  <para>**WebGL:** The sample data of audio clips is loaded asynchronously in the WebGL platform. This makes it necessary to check the loadState of an AudioClip before reading the sample data.</para>
        ///</remarks>
        ///<param name="data">The array you want to fill with raw data from the audio clip.</param>
        ///<param name="offsetSamples">The index of where to start data extraction from the array of raw data. <c>offsetSamples</c> doesn't take audio channels into account, and uses frames instead. Don't multiply the audio channel count into the index.</param>
        ///<returns>Returns 'true' if <see cref="AudioClip" /> retrieves the data successfully. Returns 'false' if the operation was unsuccessful.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using Unity.Collections;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Read all the samples from the clip and halve the gain
        ///    void Start()
        ///    {
        ///        AudioSource audioSource = GetComponent<AudioSource>();
        ///        var numSamples = audioSource.clip.samples * audioSource.clip.channels;
        ///        var samples = new NativeArray<float>(numSamples, Allocator.Temp);
        ///        audioSource.clip.GetData(samples, 0);
        ///
        ///        for (int i = 0; i < samples.Length; ++i)
        ///        {
        ///            samples[i] = samples[i] * 0.5f;
        ///        }
        ///
        ///        audioSource.clip.SetData(samples, 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using Unity.Collections;
        ///using System.Collections;
        ///
        ///public class ExampleGetDataCoroutine : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(GetAudioData());
        ///    }
        ///
        ///    IEnumerator GetAudioData()
        ///    {
        ///        AudioSource audioSource = GetComponent<AudioSource>();
        ///        // Wait for sample data to be loaded
        ///        while (audioSource.clip.loadState != AudioDataLoadState.Loaded)
        ///        {
        ///            yield return null;
        ///        }
        ///
        ///        // Read all the samples from the clip and halve the gain
        ///        var numSamples = audioSource.clip.samples * audioSource.clip.channels;
        ///        var samples = new NativeArray<float>(numSamples, Allocator.Temp);
        ///        audioSource.clip.GetData(samples, 0);
        ///
        ///        for (int i = 0; i < samples.Length; ++i)
        ///        {
        ///            samples[i] = samples[i] * 0.5f;
        ///        }
        ///
        ///        audioSource.clip.SetData(samples, 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public unsafe bool GetData(Span<float> data, int offsetSamples)
        {
            if (channels <= 0)
            {
                Debug.Log("AudioClip.GetData failed; AudioClip " + GetName() + " contains no data");
                return false;
            }

            return GetData(this, data, offsetSamples);
        }

        ///<summary>Fills an array with sample data from the audio clip.</summary>
        ///<remarks>
        ///  <para>The sample data contains float values within the range -1.0f to 1.0f. The sample count is the length of the Span or float array. Use the <c>offsetSamples</c> parameter to start the read from a specific position in the clip. If the read length from the offset is longer than the clip length, the read wraps around and reads the remaining samples from the start of the clip.
        ///
        ///For multi-channel audio clips, the data is interleaved, which means the samples in the returned float array alternate between channels. For example, in a stereo audio clip:
        ///
        ///* Index 0 corresponds to channel 1 (left).
        ///* Index 1 corresponds to channel 2 (right).
        ///* Index 2 corresponds to the next sample for channel 1, and so on.
        ///
        ///With compressed audio files, you can only retrieve the sample data if you set the **Load Type** to **Decompress on Load** in the AudioClip importer. <c>GetData</c> doesn't work with streamed audio clips, including if you stream clips from the disk or use <see cref="AudioClip.Create" /> and set its <c>stream</c> parameter to <c>true</c>.
        ///
        ///**Note**: Once an AudioClip has been used as a generator via <see cref="AudioClip.CreateInstance" />, <c>GetData</c> is permanently blocked for that clip instance. Calling it returns <c>false</c> and logs a warning.
        ///
        ///If <c>GetData</c> can't read the audio clip, the <c>data</c> parameter will contain zeroes for all sample values, the console will log an error, and <c>GetData</c> will return false.
        ///
        ///For the best performance, use the Span version because you don't need to allocate managed memory.</para>
        ///  <para>**WebGL:** The sample data of audio clips is loaded asynchronously in the WebGL platform. This makes it necessary to check the loadState of an AudioClip before reading the sample data.</para>
        ///</remarks>
        ///<param name="data">The array you want to fill with raw data from the audio clip.</param>
        ///<param name="offsetSamples">The index of where to start data extraction from the array of raw data. <c>offsetSamples</c> doesn't take audio channels into account, and uses frames instead. Don't multiply the audio channel count into the index.</param>
        ///<returns>Returns 'true' if <see cref="AudioClip" /> retrieves the data successfully. Returns 'false' if the operation was unsuccessful.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using Unity.Collections;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Read all the samples from the clip and halve the gain
        ///    void Start()
        ///    {
        ///        AudioSource audioSource = GetComponent<AudioSource>();
        ///        var numSamples = audioSource.clip.samples * audioSource.clip.channels;
        ///        var samples = new NativeArray<float>(numSamples, Allocator.Temp);
        ///        audioSource.clip.GetData(samples, 0);
        ///
        ///        for (int i = 0; i < samples.Length; ++i)
        ///        {
        ///            samples[i] = samples[i] * 0.5f;
        ///        }
        ///
        ///        audioSource.clip.SetData(samples, 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using Unity.Collections;
        ///using System.Collections;
        ///
        ///public class ExampleGetDataCoroutine : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(GetAudioData());
        ///    }
        ///
        ///    IEnumerator GetAudioData()
        ///    {
        ///        AudioSource audioSource = GetComponent<AudioSource>();
        ///        // Wait for sample data to be loaded
        ///        while (audioSource.clip.loadState != AudioDataLoadState.Loaded)
        ///        {
        ///            yield return null;
        ///        }
        ///
        ///        // Read all the samples from the clip and halve the gain
        ///        var numSamples = audioSource.clip.samples * audioSource.clip.channels;
        ///        var samples = new NativeArray<float>(numSamples, Allocator.Temp);
        ///        audioSource.clip.GetData(samples, 0);
        ///
        ///        for (int i = 0; i < samples.Length; ++i)
        ///        {
        ///            samples[i] = samples[i] * 0.5f;
        ///        }
        ///
        ///        audioSource.clip.SetData(samples, 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool GetData(float[] data, int offsetSamples)
        {
            if (channels <= 0)
            {
                Debug.Log("AudioClip.GetData failed; AudioClip " + GetName() + " contains no data");
                return false;
            }

            return GetData(this, data.AsSpan(), offsetSamples);
        }

        ///<summary>Fills an audio clip with sample data from an array or Span. Overwrites existing data if necessary.</summary>
        ///<remarks>This is useful if you want to use procedural audio and change audio data during runtime. Only use samples with float values ranging from -1.0f to 1.0f. Don't exceed these limits because it can cause artifacts and undefined behavior.
        ///
        ///The length of the ReadOnlySpan or float array determines the sample count.
        ///
        ///Use the <c>offsetSamples</c> parameter to write into a certain position in the clip. If the length from the offset is longer than the clip length, the write will wrap around and write the remaining samples from the start of the clip.
        ///
        ///For compressed audio files, you can only set the sample data if you set **Load Type** to **Decompress on Load** in the [Audio Clip](xref:class-AudioClip) importer.
        ///
        ///**Note**: Once an AudioClip has been used as a generator via <see cref="AudioClip.CreateInstance" />, <c>SetData</c> is permanently blocked for that clip instance. Calling it returns <c>false</c> and logs a warning.
        ///
        ///For the best performance, use the Span version because you don't need to allocate managed memory.
        ///
        ///**Note:** The buffer provided contains a float value per sample and per channel. If your audio clip is stereo, the buffer contains interleaved float values for left channel, right channel, etc.</remarks>
        ///<param name="data">Linear buffer of samples to write to the audio clip buffer.</param>
        ///<param name="offsetSamples">Offset from the start of the audio clip at which to begin writing sample data. <c>offsetSamples</c> doesn't take audio channels into account, and instead uses frames. Don't multiply the audio channel count into the index.</param>
        ///<returns>Returns whether all samples were successfully written to the audio clip. This can return <c>false</c> if <c>offsetSamples</c> isn't a valid offset within the existing AudioClip, or if the data is empty.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using Unity.Collections;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Read all the samples from the clip and halve the gain
        ///    void Start()
        ///    {
        ///        AudioSource audioSource = GetComponent<AudioSource>();
        ///        var numSamples = audioSource.clip.samples * audioSource.clip.channels;
        ///        var samples = new NativeArray<float>(numSamples, Allocator.Temp);
        ///        audioSource.clip.GetData(samples, 0);
        ///
        ///        for (int i = 0; i < samples.Length; ++i)
        ///        {
        ///            samples[i] = samples[i] * 0.5f;
        ///        }
        ///
        ///        audioSource.clip.SetData(samples, 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
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

            return SetData(this, data.AsSpan(), offsetSamples);
        }

        ///<summary>Fills an audio clip with sample data from an array or Span. Overwrites existing data if necessary.</summary>
        ///<remarks>This is useful if you want to use procedural audio and change audio data during runtime. Only use samples with float values ranging from -1.0f to 1.0f. Don't exceed these limits because it can cause artifacts and undefined behavior.
        ///
        ///The length of the ReadOnlySpan or float array determines the sample count.
        ///
        ///Use the <c>offsetSamples</c> parameter to write into a certain position in the clip. If the length from the offset is longer than the clip length, the write will wrap around and write the remaining samples from the start of the clip.
        ///
        ///For compressed audio files, you can only set the sample data if you set **Load Type** to **Decompress on Load** in the [Audio Clip](xref:class-AudioClip) importer.
        ///
        ///**Note**: Once an AudioClip has been used as a generator via <see cref="AudioClip.CreateInstance" />, <c>SetData</c> is permanently blocked for that clip instance. Calling it returns <c>false</c> and logs a warning.
        ///
        ///For the best performance, use the Span version because you don't need to allocate managed memory.
        ///
        ///**Note:** The buffer provided contains a float value per sample and per channel. If your audio clip is stereo, the buffer contains interleaved float values for left channel, right channel, etc.</remarks>
        ///<param name="data">Linear buffer of samples to write to the audio clip buffer.</param>
        ///<param name="offsetSamples">Offset from the start of the audio clip at which to begin writing sample data. <c>offsetSamples</c> doesn't take audio channels into account, and instead uses frames. Don't multiply the audio channel count into the index.</param>
        ///<returns>Returns whether all samples were successfully written to the audio clip. This can return <c>false</c> if <c>offsetSamples</c> isn't a valid offset within the existing AudioClip, or if the data is empty.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using Unity.Collections;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Read all the samples from the clip and halve the gain
        ///    void Start()
        ///    {
        ///        AudioSource audioSource = GetComponent<AudioSource>();
        ///        var numSamples = audioSource.clip.samples * audioSource.clip.channels;
        ///        var samples = new NativeArray<float>(numSamples, Allocator.Temp);
        ///        audioSource.clip.GetData(samples, 0);
        ///
        ///        for (int i = 0; i < samples.Length; ++i)
        ///        {
        ///            samples[i] = samples[i] * 0.5f;
        ///        }
        ///
        ///        audioSource.clip.SetData(samples, 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public unsafe bool SetData(ReadOnlySpan<float> data, int offsetSamples)
        {
            if (channels <= 0)
            {
                Debug.Log("AudioClip.SetData failed; AudioClip " + GetName() + " contains no data");
                return false;
            }

            if ((offsetSamples < 0) || (offsetSamples >= samples))
                throw new ArgumentException("AudioClip.SetData failed; invalid offsetSamples");

            if (data.Length == 0)
                throw new ArgumentException("AudioClip.SetData failed; invalid data");

            return SetData(this, data, offsetSamples);
        }

        ///<summary>Creates a user AudioClip with a name and with the given length in samples, channels and frequency.</summary>
        ///<remarks>Set your own audio data with <see cref="AudioClip.SetData" />. Use the <see cref="AudioClip.PCMReaderCallback" /> and <see cref="AudioClip.PCMSetPositionCallback" /> delegates to get a callback whenever the clip reads data and changes the position. If <c>stream</c> is true, Unity reads in small chunks of data on demand. If it's false, Unity reads all the samples during the creation of the clip.
        ///
        ///**Note**: Unity expects you to pass an array with valid audio data (floating-point samples between -1.0 and 1.0) to <c>PCMReaderCallback</c>. If no audio data is available, you must fill the array with zeros. Otherwise it will result in unexpected noise or other unwanted sounds during playback.
        ///
        ///**Note**: You must use persistent, imported audio clips for generator functionality in the Scriptable Audio Pipeline. You can't use <c>AudioClip.CreateInstance</c> with clips you generate at runtime via <c>AudioClip.Create</c>.</remarks>
        ///<param name="name">Name of clip.</param>
        ///<param name="lengthSamples">Number of sample frames.</param>
        ///<param name="channels">Number of channels per frame.</param>
        ///<param name="frequency">Sample frequency of clip.</param>
        ///<param name="stream">True if clip is streamed, that is if the pcmreadercallback generates data on the fly.</param>
        ///<returns>A reference to the created AudioClip.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int position = 0;
        ///    public int samplerate = 44100;
        ///    public float frequency = 440;
        ///
        ///    void Start()
        ///    {
        ///        AudioClip myClip = AudioClip.Create("MySinusoid", samplerate * 2, 1, samplerate, true, OnAudioRead, OnAudioSetPosition);
        ///        AudioSource aud = GetComponent<AudioSource>();
        ///        aud.clip = myClip;
        ///        aud.Play();
        ///    }
        ///
        ///    void OnAudioRead(float[] data)
        ///    {
        ///        int count = 0;
        ///        while (count < data.Length)
        ///        {
        ///            data[count] = Mathf.Sin(2 * Mathf.PI * frequency * position / samplerate);
        ///            position++;
        ///            count++;
        ///        }
        ///    }
        ///
        ///    void OnAudioSetPosition(int newPosition)
        ///    {
        ///        position = newPosition;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream)
        {
            AudioClip clip = Create(name, lengthSamples, channels, frequency, stream, null, null);
            return clip;
        }

        ///<summary>Creates a user AudioClip with a name and with the given length in samples, channels and frequency.</summary>
        ///<remarks>Set your own audio data with <see cref="AudioClip.SetData" />. Use the <see cref="AudioClip.PCMReaderCallback" /> and <see cref="AudioClip.PCMSetPositionCallback" /> delegates to get a callback whenever the clip reads data and changes the position. If <c>stream</c> is true, Unity reads in small chunks of data on demand. If it's false, Unity reads all the samples during the creation of the clip.
        ///
        ///**Note**: Unity expects you to pass an array with valid audio data (floating-point samples between -1.0 and 1.0) to <c>PCMReaderCallback</c>. If no audio data is available, you must fill the array with zeros. Otherwise it will result in unexpected noise or other unwanted sounds during playback.
        ///
        ///**Note**: You must use persistent, imported audio clips for generator functionality in the Scriptable Audio Pipeline. You can't use <c>AudioClip.CreateInstance</c> with clips you generate at runtime via <c>AudioClip.Create</c>.</remarks>
        ///<param name="name">Name of clip.</param>
        ///<param name="lengthSamples">Number of sample frames.</param>
        ///<param name="channels">Number of channels per frame.</param>
        ///<param name="frequency">Sample frequency of clip.</param>
        ///<param name="stream">True if clip is streamed, that is if the pcmreadercallback generates data on the fly.</param>
        ///<param name="pcmreadercallback">This callback is invoked to generate a block of sample data. Non-streamed clips call this only once at creation time while streamed clips call this continuously.</param>
        ///<returns>A reference to the created AudioClip.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int position = 0;
        ///    public int samplerate = 44100;
        ///    public float frequency = 440;
        ///
        ///    void Start()
        ///    {
        ///        AudioClip myClip = AudioClip.Create("MySinusoid", samplerate * 2, 1, samplerate, true, OnAudioRead, OnAudioSetPosition);
        ///        AudioSource aud = GetComponent<AudioSource>();
        ///        aud.clip = myClip;
        ///        aud.Play();
        ///    }
        ///
        ///    void OnAudioRead(float[] data)
        ///    {
        ///        int count = 0;
        ///        while (count < data.Length)
        ///        {
        ///            data[count] = Mathf.Sin(2 * Mathf.PI * frequency * position / samplerate);
        ///            position++;
        ///            count++;
        ///        }
        ///    }
        ///
        ///    void OnAudioSetPosition(int newPosition)
        ///    {
        ///        position = newPosition;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream, PCMReaderCallback pcmreadercallback)
        {
            AudioClip clip = Create(name, lengthSamples, channels, frequency, stream, pcmreadercallback, null);
            return clip;
        }

        ///<summary>Creates a user AudioClip with a name and with the given length in samples, channels and frequency.</summary>
        ///<remarks>Set your own audio data with <see cref="AudioClip.SetData" />. Use the <see cref="AudioClip.PCMReaderCallback" /> and <see cref="AudioClip.PCMSetPositionCallback" /> delegates to get a callback whenever the clip reads data and changes the position. If <c>stream</c> is true, Unity reads in small chunks of data on demand. If it's false, Unity reads all the samples during the creation of the clip.
        ///
        ///**Note**: Unity expects you to pass an array with valid audio data (floating-point samples between -1.0 and 1.0) to <c>PCMReaderCallback</c>. If no audio data is available, you must fill the array with zeros. Otherwise it will result in unexpected noise or other unwanted sounds during playback.
        ///
        ///**Note**: You must use persistent, imported audio clips for generator functionality in the Scriptable Audio Pipeline. You can't use <c>AudioClip.CreateInstance</c> with clips you generate at runtime via <c>AudioClip.Create</c>.</remarks>
        ///<param name="name">Name of clip.</param>
        ///<param name="lengthSamples">Number of sample frames.</param>
        ///<param name="channels">Number of channels per frame.</param>
        ///<param name="frequency">Sample frequency of clip.</param>
        ///<param name="stream">True if clip is streamed, that is if the pcmreadercallback generates data on the fly.</param>
        ///<param name="pcmreadercallback">This callback is invoked to generate a block of sample data. Non-streamed clips call this only once at creation time while streamed clips call this continuously.</param>
        ///<param name="pcmsetpositioncallback">This callback is invoked whenever the clip loops or changes playback position.</param>
        ///<returns>A reference to the created AudioClip.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int position = 0;
        ///    public int samplerate = 44100;
        ///    public float frequency = 440;
        ///
        ///    void Start()
        ///    {
        ///        AudioClip myClip = AudioClip.Create("MySinusoid", samplerate * 2, 1, samplerate, true, OnAudioRead, OnAudioSetPosition);
        ///        AudioSource aud = GetComponent<AudioSource>();
        ///        aud.clip = myClip;
        ///        aud.Play();
        ///    }
        ///
        ///    void OnAudioRead(float[] data)
        ///    {
        ///        int count = 0;
        ///        while (count < data.Length)
        ///        {
        ///            data[count] = Mathf.Sin(2 * Mathf.PI * frequency * position / samplerate);
        ///            position++;
        ///            count++;
        ///        }
        ///    }
        ///
        ///    void OnAudioSetPosition(int newPosition)
        ///    {
        ///        position = newPosition;
        ///    }
        ///}
        ///]]></code>
        ///</example>
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

        ///<summary>Unity calls this delegate each time <see cref="AudioClip" /> reads data.</summary>
        ///<remarks>The AudioClip stores this raw sample data in an array of floats that range from -1 to 1. 
        ///For non-streamed clips, Unity calls this delegate when it creates the audio clip. If the clip is longer than the callback's maximum amount of samples, Unity calls the delegate multiple times so the engine can get all the clip's samples. Streamed clips call this delegate continuously, and takes various samples at varied intervals.
        ///When you use <see cref="AudioClip.Create" /> to create a clip, you can use this delegate as a parameter and define what happens whenever the audio reads data.</remarks>
        ///<param name="data">Array of floats containing data read from the clip.</param>
        ///<example>
        ///  <code><![CDATA[ // This script creates an audio clip, sets its data and creates a sinusoid graph when it reads the data and changes positions. 
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int position = 0;
        ///    public int samplerate = 44100;
        ///    public float frequency = 440;
        ///
        ///   void Start()
        ///    {
        ///        AudioClip myClip = AudioClip.Create("MySinusoid", samplerate * 2, 1, samplerate, true, OnAudioRead, OnAudioSetPosition);
        ///        AudioSource aud = GetComponent<AudioSource>();
        ///        aud.clip = myClip;
        ///        aud.Play();
        ///    }
        ///
        /// // When Unity calls PCMReaderCallback, create a graph from the audio clip’s data.  
        ///   void OnAudioRead(float[] data)
        ///    {
        ///        int count = 0;
        ///        while (count < data.Length)
        ///        {
        ///            data[count] = Mathf.Sin(2 * Mathf.PI * frequency * position / samplerate);
        ///            position++;
        ///            count++;
        ///        }
        ///    }
        /// //When Unity calls PCMSetPositionCallback, update the position variable. 
        ///   void OnAudioSetPosition(int newPosition)
        ///    {
        ///        position = newPosition;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AudioClip.Create" />
        public delegate void PCMReaderCallback(float[] data);
        private event PCMReaderCallback m_PCMReaderCallback = null;

        ///<summary>Unity calls this delegate each time <see cref="AudioClip" /> changes read position.</summary>
        ///<remarks>Unity uses this delegate to signal when the audio clip changes its playback position, for example: 
        ///
        ///* A user seeks through an audio clip. 
        ///* The audio clip restarts. 
        ///
        ///When you use <see cref="AudioClip.Create" /> to create a clip, use this delegate as a parameter and define what happens whenever the audio clip changes position. 
        ///
        ///The position Unity passes to PCMSetPositionCallback are sample frames, not individual samples. 
        ///For example, if you have a clip with a sampling rate of 44,100 Hz and you seek 1.0 s into the clip, Unity does the following: 
        ///
        ///* Calls <c>PCMSetPositionCallback</c>. 
        ///* Sets the position parameter to 44,100 regardless of the audio channel count. 
        ///
        ///If the audio clip is stereo (2 channels), each frame has 2 samples- one per channel. The total number of samples at 1 s into the clip is 88200 (44100 frames x 2 channels), but <c>position</c> is still 44100.</remarks>
        ///<param name="position">The audio clip's new playback position in sample frames.</param>
        ///<example>
        ///  <code><![CDATA[ // This script creates an audio clip, sets its data and creates a sinusoid graph when it reads the data and changes positions. 
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int position = 0;
        ///    public int samplerate = 44100;
        ///    public float frequency = 440;
        ///
        ///   void Start()
        ///    {
        ///        AudioClip myClip = AudioClip.Create("MySinusoid", samplerate * 2, 1, samplerate, true, OnAudioRead, OnAudioSetPosition);
        ///        AudioSource aud = GetComponent<AudioSource>();
        ///        aud.clip = myClip;
        ///        aud.Play();
        ///    }
        ///
        /// // When Unity calls PCMReaderCallback, create a graph from the audio clip’s data.  
        ///   void OnAudioRead(float[] data)
        ///    {
        ///        int count = 0;
        ///        while (count < data.Length)
        ///        {
        ///            data[count] = Mathf.Sin(2 * Mathf.PI * frequency * position / samplerate);
        ///            position++;
        ///            count++;
        ///        }
        ///    }
        /// //When Unity calls PCMSetPositionCallback, update the position variable. 
        ///   void OnAudioSetPosition(int newPosition)
        ///    {
        ///        position = newPosition;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AudioClip.Create" />
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

        #region Generator.IAudioGenerator

        void CheckIsNotPersistent()
        {
            if (IsLegacyFormat())
                throw new NotSupportedException($"AudioClip {name} is not a valid {nameof(IAudioGenerator)}. Only persistent {nameof(AudioClip)} can be used, not runtime created ones.");
        }

        bool GeneratorInstance.ICapabilities.isRealtime
        {
            get
            {
                CheckIsNotPersistent();
                return false;
            }
        }

        bool GeneratorInstance.ICapabilities.isFinite
        {
            get
            {
                CheckIsNotPersistent();
                return true;
            }
        }

        DiscreteTime? GeneratorInstance.ICapabilities.length
        {
            get
            {
                CheckIsNotPersistent();
                return DiscreteTime.FromTicks(GeneratorInstance.Configuration.FramesAndSampleRateToDiscreteTimeTicks(samples, (uint)frequency));
            }
        }

        ///<summary>Factory method for creating a GeneratorInstance of this clip.</summary>
        ///<remarks>Use this method to efficiently stream the AudioClip data as a <see cref="GeneratorInstance" />. You can use the instance directly with a <see cref="RealtimeContext" /> or nest it within other <see cref="GeneratorInstance" />s to facilitate advanced playback behaviours. Note that you can only use persistent (imported) audio clips as generators. Runtime-created clips (via <see cref="AudioClip.Create" />, <see cref="T:UnityEngine.Networking.DownloadHandlerAudioClip" />, or <see cref="Microphone" />) throw a NotSupportedException.
        ///
        ///Once an AudioClip has been used as a generator, <see cref="AudioClip.GetData" /> and <see cref="AudioClip.SetData" /> are permanently blocked for that clip instance.</remarks>
        ///<param name="context">The context in which the instance will be created.</param>
        ///<param name="nestedFormat">If not null, the instance will be created with the the given format as nested, to be used from within another processor.</param>
        ///<param name="creationParameters">Initialization parameters passed through.</param>
        ///<returns>Returns the generator instance of the clip.</returns>
        ///<seealso href="xref:audio-scriptable-processors-generators">Using AudioClips as generators</seealso>
        public GeneratorInstance CreateInstance(ControlContext context, AudioFormat? nestedFormat, GeneratorInstance.CreationParameters creationParameters)
        {
            CheckIsNotPersistent();

            unsafe
            {
                AudioConfiguration* configPtr = null;

                if (nestedFormat.HasValue)
                {
                    var config = nestedFormat.Value.audioConfiguration;
                    configPtr = &config;
                }

                var handle = SampleProviderBindings.CreateGenerator(this, context.Header, configPtr);

                return new GeneratorInstance(handle);
            }
        }

        #endregion
    }


    ///<exclude />
    [global::UnityEngine.NativeClass("AudioBehaviour", PersistentTypeId = 180)]
    public class AudioBehaviour : Behaviour
    {
    }

    ///<summary>Representation of a listener in 3D space.</summary>
    ///<remarks>This class implements a microphone-like device. It records the sounds around it and plays that through the player's speakers.
    ///You can only have one listener in a Scene.</remarks>
    ///<seealso cref="AudioSource" />
    ///<seealso href="xref:class-AudioListener">AudioListener component</seealso>
    [RequireComponent(typeof(Transform))]
    [global::UnityEngine.NativeClass("AudioListener", PersistentTypeId = 81)]
    [StaticAccessor("AudioListenerBindings", StaticAccessorType.DoubleColon)]
    public sealed partial class AudioListener : AudioBehaviour
    {
        [NativeMethod(ThrowsException = true)]
        extern static private void GetOutputDataHelper([Out] float[] samples, int channel);

        [NativeMethod(ThrowsException = true)]
        extern static private void GetSpectrumDataHelper([Out] float[] samples, int channel, FFTWindow window);

        ///<summary>Controls the game sound volume (0.0 to 1.0).</summary>
        extern static public float volume { get; set; }

        ///<summary>The paused state of the audio system.</summary>
        ///<remarks>If set to true, all AudioSources playing will be paused. This works in the same way as pausing the game in the editor. While the pause-state is true, the <see cref="AudioSettings.dspTime" /> will be frozen and further AudioSource play requests will start off paused. If you want certain sounds to still play during the pause, you need to set the ignoreListenerPause property on the AudioSource to true for these. This is typically menu item sounds or background music for the menu. Any scheduled play requests will be frozen in time, so that if you scheduled a sound to play after 3 seconds and paused the audio system 1 second after this, the scheduled sounds will start playing 2 seconds after unpausing.</remarks>
        [NativeProperty("ListenerPause")]
        extern static public bool pause { get; set; }

        ///<summary>This lets you set whether the Audio Listener should be updated in the fixed or dynamic update.</summary>
        ///<remarks>Make sure this is set to update in the same update loop as the Audio Listener is moved in if you are experiencing problems with Doppler effect simulation.
        ///The default setting will automatically set the listener to be updated in the fixed update loop if it is attached to a rigidbody, and dynamic otherwise.</remarks>
        extern public AudioVelocityUpdateMode velocityUpdateMode { get; set; }


        ///<summary>Provides a block of the listener (master)'s output data.</summary>
        ///<remarks>The array given in the samples parameter will be filled with the requested data.
        ///
        ///<c>GetOutputData</c> provides access to audio data from a short history window (for example, the last few milliseconds) for analysis purposes. Unity doesn't automatically allocate the buffers required to store this history because doing so would be expensive and memory-intensive. Instead, Unity only allocates buffers and starts to record when you first call this function, on a per-object basis. As a result, the output data will initially be empty until the engine processes sufficient audio to populate the buffer. Please note this function isn't suited for critical or chronological, real-time data analysis or processing, or scenarios where you require low latency.</remarks>
        ///<param name="samples">The array to populate with audio samples. Its length must be a power of 2.</param>
        ///<param name="channel">The channel to sample from.</param>
        ///<seealso cref="AudioListener.GetSpectrumData" />
        ///<seealso cref="AudioSource.GetSpectrumData" />
        ///<seealso cref="AudioSource.GetOutputData" />
        static public void GetOutputData(float[] samples, int channel)
        {
            GetOutputDataHelper(samples, channel);
        }

        ///<summary>Provides a block of the listener (master)'s spectrum data.</summary>
        ///<remarks>The array given in the samples parameter will be filled with the requested data.
        ///
        ///Number of values (the length of the samples array) must be a power of 2. (ie 128/256/512 etc). Min = 64. Max = 8192.
        ///Use <see cref="FFTWindow">window</see> to reduce leakage between frequency bins/bands.
        ///Note, the more complex window type, the better the quality, but reduced speed.
        ///
        ///This function will use the sampling rate specified in <see cref="AudioSettings.outputSampleRate" />, and NOT the sampling rate specified for the audio clip.
        ///
        ///**Note**: <c>GetSpectrumData</c> provides access to audio data from a short history window (for example, the last few milliseconds) for analysis purposes. Unity doesn't automatically allocate the buffers required to store this history because doing so would be expensive and memory-intensive. Instead, Unity only allocates buffers and starts to record when you first call this function, on a per-object basis. As a result, the output data will initially be empty until the engine processes sufficient audio to populate the buffer. Please note this function isn't suited for critical or chronological, real-time data analysis or processing, or scenarios where you require low latency.</remarks>
        ///<param name="samples">The array to populate with audio samples. Its length must be a power of 2.</param>
        ///<param name="channel">The channel to sample from.</param>
        ///<param name="window">The FFTWindow type to use when sampling.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///
        ///[RequireComponent(typeof(AudioListener))]
        ///public class GetSpectrumDataExample : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        float[] spectrum = new float[256];
        ///
        ///        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.Rectangular);
        ///
        ///        for (int i = 1; i < spectrum.Length - 1; i++)
        ///        {
        ///            Debug.DrawLine(new Vector3(i - 1, spectrum[i] + 10, 0), new Vector3(i, spectrum[i + 1] + 10, 0), Color.red);
        ///            Debug.DrawLine(new Vector3(i - 1, Mathf.Log(spectrum[i - 1]) + 10, 2), new Vector3(i, Mathf.Log(spectrum[i]) + 10, 2), Color.cyan);
        ///            Debug.DrawLine(new Vector3(Mathf.Log(i - 1), spectrum[i - 1] - 10, 1), new Vector3(Mathf.Log(i), spectrum[i] - 10, 1), Color.green);
        ///            Debug.DrawLine(new Vector3(Mathf.Log(i - 1), Mathf.Log(spectrum[i - 1]), 3), new Vector3(Mathf.Log(i), Mathf.Log(spectrum[i]), 3), Color.blue);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AudioListener.GetOutputData" />
        ///<seealso cref="AudioSource.GetSpectrumData" />
        ///<seealso cref="AudioSource.GetOutputData" />
        static public void GetSpectrumData(float[] samples, int channel, FFTWindow window)
        {
            GetSpectrumDataHelper(samples, channel, window);
        }
    }

    // A representation of audio sources in 3D.
    ///<summary>A representation of audio sources in 3D.</summary>
    ///<remarks>Attach an AudioSource to a <see cref="GameObject" /> to play back sounds in a 3D environment.
    ///To play 3D sounds you also need to have an <see cref="AudioListener" />.
    ///Usually, you can find the audio listener attached to the camera in your scene.
    ///If you set <see cref="AudioSource.spatialBlend" /> to 0.0f, then Unity will treat the audio clip as a 2D sound. If you set it to 1.0f, the clip is fully 3D. Anything in between is a blend of 2D and 3D.
    ///
    ///To play, pause, and stop a single audio clip, use <see cref="Play" />, <see cref="Pause" /> and <see cref="Stop" />.
    ///To adjust its volume while playing, use the <see cref="volume" /> property. Use <see cref="time" /> to seek through the audio track.
    ///To play multiple sounds on one AudioSource, use <see cref="PlayOneShot" />.
    ///To play a clip at a static position in 3D space, use <see cref="PlayClipAtPoint" />.</remarks>
    ///<example>
    ///  <code><![CDATA[
    /// //This script allows you to toggle music to play and stop.
    /// //Assign an AudioSource to a GameObject and attach an Audio Clip in the Audio Source. Attach this script to the GameObject.
    ///
    ///using UnityEngine;
    ///
    ///public class Example : MonoBehaviour
    ///{
    ///    AudioSource m_MyAudioSource;
    ///
    ///    //Play the music
    ///    bool m_Play;
    ///    //Detect when you use the toggle, ensures music isn’t played multiple times
    ///    bool m_ToggleChange;
    ///
    ///    void Start()
    ///    {
    ///        //Fetch the AudioSource from the GameObject
    ///        m_MyAudioSource = GetComponent<AudioSource>();
    ///        //Ensure the toggle is set to true for the music to play at start-up
    ///        m_Play = true;
    ///    }
    ///
    ///    void Update()
    ///    {
    ///        //Check to see if you just set the toggle to positive
    ///        if (m_Play == true && m_ToggleChange == true)
    ///        {
    ///            //Play the audio you attach to the AudioSource component
    ///            m_MyAudioSource.Play();
    ///            //Ensure audio doesn’t play more than once
    ///            m_ToggleChange = false;
    ///        }
    ///        //Check if you just set the toggle to false
    ///        if (m_Play == false && m_ToggleChange == true)
    ///        {
    ///            //Stop the audio
    ///            m_MyAudioSource.Stop();
    ///            //Ensure audio doesn’t play more than once
    ///            m_ToggleChange = false;
    ///        }
    ///    }
    ///
    ///    void OnGUI()
    ///    {
    ///        //Switch this toggle to activate and deactivate the parent GameObject
    ///        m_Play = GUI.Toggle(new Rect(10, 10, 100, 30), m_Play, "Play Music");
    ///
    ///        //Detect if there is a change with the toggle
    ///        if (GUI.changed)
    ///        {
    ///            //Change to true to show that there was just a change in the toggle state
    ///            m_ToggleChange = true;
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="AudioListener" />
    ///<seealso cref="AudioClip" />
    ///<seealso cref="AudioRandomContainer" />
    ///<seealso cref="IAudioGenerator" />
    ///<seealso href="xref:class-AudioSource">AudioSource component</seealso>
    [RequireComponent(typeof(Transform))]
    [global::UnityEngine.NativeClass("AudioSource", PersistentTypeId = 82)]
    [StaticAccessor("AudioSourceBindings", StaticAccessorType.DoubleColon)]
    public sealed partial class AudioSource : AudioBehaviour
    {
        extern static private float GetPitch([NotNull] AudioSource source);
        extern static private void SetPitch([NotNull] AudioSource source, float pitch);

        extern static private void PlayHelper([NotNull] AudioSource source, UInt64 delay);
        extern private void Play(double delay);

        extern static private void PlayOneShotHelper([NotNull] AudioSource source, [NotNull] AudioClip clip, float volumeScale);

        extern private void Stop(bool stopOneShots);

        [NativeMethod(ThrowsException = true)]
        extern static private void SetCustomCurveHelper([NotNull] AudioSource source, AudioSourceCurveType type, AnimationCurve curve);
        extern static private AnimationCurve GetCustomCurveHelper([NotNull] AudioSource source, AudioSourceCurveType type);

        extern static private void GetOutputDataHelper([NotNull] AudioSource source, [Out] float[] samples, int channel);
        [NativeMethod(ThrowsException = true)]
        extern static private void GetSpectrumDataHelper([NotNull] AudioSource source, [Out] float[] samples, int channel, FFTWindow window);

        ///<summary>The volume of the audio source (0.0 to 1.0).</summary>
        ///<remarks>The AudioSource’s volume property controls the level of sound coming from an AudioClip. The highest volume level is 1 and the lowest is 0 where no sound is heard.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    AudioSource m_MyAudioSource;
        ///    //Value from the slider, and it converts to volume level
        ///    float m_MySliderValue;
        ///
        ///    void Start()
        ///    {
        ///        //Initiate the Slider value to half way
        ///        m_MySliderValue = 0.5f;
        ///        //Fetch the AudioSource from the GameObject
        ///        m_MyAudioSource = GetComponent<AudioSource>();
        ///        //Play the AudioClip attached to the AudioSource on startup
        ///        m_MyAudioSource.Play();
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        //Create a horizontal Slider that controls volume levels. Its highest value is 1 and lowest is 0
        ///        m_MySliderValue = GUI.HorizontalSlider(new Rect(25, 25, 200, 60), m_MySliderValue, 0.0F, 1.0F);
        ///        //Makes the volume of the Audio match the Slider value
        ///        m_MyAudioSource.volume = m_MySliderValue;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float volume { get; set; }

        ///<summary>The pitch of the audio source.</summary>
        ///<remarks>
        ///  <para>Pitch makes a melody go higher or lower. For example, if you play an audio clip with pitch set to one, increasing the pitch as the clip plays will make the clip sound higher. Similarly, decreasing the pitch to less than one makes the clip sound lower. When <see cref="resource" /> is an <see cref="AudioClip" />, the pitch property is clamped to the range [-3..3]. When <see cref="resource" /> is an <see cref="AudioRandomContainer" />, the pitch property is ignored, and if it is not in the range [0.0001..3.0], a warning appears in the console. This is due to <see cref="AudioRandomContainer" /> not supporting reverse/pause playback from the pitch. Any values outside this range are clamped when changing from an <see cref="AudioClip" /> to an <see cref="AudioRandomContainer" />.</para>
        ///  <para>Another example:</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to a GameObject.
        /// //Attach an AudioSource to your GameObject (Click __Add Component__ and go to __Audio__>__Audio Source__). Choose an audio clip in the __AudioClip__ field.
        /// //This script sets the pitch of the audio at the start, and then gradually turns it down to 0 as time passes.
        ///
        ///using UnityEngine;
        ///
        /// //Make sure there is an Audio Source component on the GameObject
        ///[RequireComponent(typeof(AudioSource))]
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public int startingPitch = 4;
        ///    public int timeToDecrease = 5;
        ///    AudioSource audioSource;
        ///
        ///    void Start()
        ///    {
        ///        //Fetch the AudioSource from the GameObject
        ///        audioSource = GetComponent<AudioSource>();
        ///
        ///        //Initialize the pitch
        ///        audioSource.pitch = startingPitch;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //While the pitch is over 0, decrease it as time passes.
        ///        if (audioSource.pitch > 0)
        ///        {
        ///            audioSource.pitch -= Time.deltaTime * startingPitch / timeToDecrease;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        /// // A script that plays your chosen song.  The pitch starts at 1.0.
        /// // You can increase and decrease the pitch and hear the change
        /// // that is made.
        ///
        ///public class AudioExample : MonoBehaviour
        ///{
        ///    public float pitchValue = 1.0f;
        ///    public AudioClip mySong;
        ///
        ///    private AudioSource audioSource;
        ///    private float low = 0.75f;
        ///    private float high = 1.25f;
        ///
        ///    void Awake()
        ///    {
        ///        audioSource = GetComponent<AudioSource>();
        ///        audioSource.clip = mySong;
        ///        audioSource.loop = true;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        pitchValue = GUI.HorizontalSlider(new Rect(25, 75, 100, 30), pitchValue, low, high);
        ///        audioSource.pitch = pitchValue;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public float pitch
        {
            get { return GetPitch(this); }
            set { SetPitch(this, value); }
        }

        ///<summary>Audio source playback position in seconds.</summary>
        ///<remarks>Use this property to read the current playback time or to seek to a new playback time.
        ///
        ///If the audio source is **not playing**, <see cref="time" /> will always return 0, regardless of any previously set or expected playback position.
        ///
        ///If you need to know the playback position when the audio source is not playing, use <see cref="timeSamples" /> instead.
        ///
        ///Setting this property while the audio source is not playing will update the playback position, but it will only take effect once playback begins.
        ///
        ///**Be aware that:** On a compressed audio track, the reported playback position does not necessarily reflect the exact time in the track.
        ///
        ///Compressed audio is stored in packets, and the size of these packets depends on the compression settings. In many cases, a single packet can represent 2–3 seconds of audio.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    AudioSource audioSource;
        ///
        ///    void Start()
        ///    {
        ///        audioSource = GetComponent<AudioSource>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (Input.GetKeyDown(KeyCode.Return))
        ///        {
        ///            audioSource.Stop();
        ///            audioSource.Play();
        ///        }
        ///        Debug.Log(audioSource.time);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("SecPosition")]
        extern public float time { get; set; }

        ///<summary>The current playback position of the AudioSource in PCM samples.</summary>
        ///<remarks>Use this to read current playback time or to seek to a new playback time in samples,
        ///if you want more precise timing than what <see cref="time" /> variable allows.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    AudioSource audioSource;
        ///
        ///    void Start()
        ///    {
        ///        audioSource = GetComponent<AudioSource>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (Input.GetKeyDown(KeyCode.Return))
        ///        {
        ///            audioSource.Stop();
        ///            audioSource.Play();
        ///        }
        ///        Debug.Log(audioSource.timeSamples);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("SamplePosition")]
        extern public int timeSamples
        {
            [NativeMethod(IsThreadSafe = true)]
            get;

            [NativeMethod(IsThreadSafe = true)]
            set;
        }

        ///<summary>The default <see cref="AudioClip" /> to play.</summary>
        ///<remarks>AudioSource <see cref="clip" /> determines the audio clip that should play next. 
        ///
        ///When you assign a new audio clip to <see cref="clip" />, the old clip it replaces (if any) stops and is replaced by the new one. However, the new clip doesn't automatically play so you need to use <see cref="AudioSource.Play" /> (or similar) to play it.</remarks>
        ///<example>
        ///  <code><![CDATA[ // This script outputs a GUI play button. The script plays an audio clip on launch, but if you press the play button it switches to another clip and then plays that one instead. 
        /// // For this script to work, assign it to a GameObject. Then, assign an audio clip in the Inspector, and another clip to the Audio Source.  
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///public class AudioSourceClipExample : MonoBehaviour
        ///{
        ///    public AudioClip otherClip;
        ///    AudioSource audio; 
        ///
        ///    void Start()
        ///    {
        ///        audio = GetComponent<AudioSource>();
        ///        audio.Play();
        ///    }
        ///
        ///    private void OnGUI()
        ///    {
        ///        if (GUI.Button(new Rect(10, 70, 30, 30), "Switch clip"))
        ///        {
        ///            audio.clip = otherClip;
        ///            audio.Play();
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        public AudioClip clip
        {
            get => generatorObject as AudioClip;
            set => generatorObject = value;
        }

        ///<summary>The default <see cref="AudioResource" /> to play.</summary>
        ///<remarks>You can also use this property to set the <see cref="AudioResource" /> that plays next.
        ///
        ///**Note**: Audio generators don’t provide direct access to properties like <c>length</c>. However, if your audio generator is an <see cref="AudioClip" />, you can access these properties through <see cref="AudioSource.clip" />. Other types of resources might not provide direct access to these properties because the resources are dynamic or the values might change every time you play the audio.</remarks>
        ///<example nocheck="true">
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public AudioResource m_Resource;
        ///
        ///    public void PlayAudioResource()
        ///    {
        ///        AudioSource audioSource = GetComponent<AudioSource>();
        ///        audioSource.resource = m_Resource;
        ///        audioSource.Play();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public AudioResource resource
        {
            get => generatorObject as AudioResource;
            set => generatorObject = value;
        }

        ///<summary>The default <see cref="IAudioGenerator" /> to play next.</summary>
        ///<remarks>When you call <see cref="AudioSource.Play" />, the <see cref="AudioSource" /> will instantiate a <c>Generator</c> from the <c>CreateRuntime</c> function and render samples from it as the audio system mixes. The instantiated <c>Generator</c> instance will be assigned to <see cref="AudioSource.generatorInstance" /> for runtime scripting control while it is playing.</remarks>
        public IAudioGenerator generator
        {
            // These shall always succeed
            get => (IAudioGenerator)generatorObject;
            set => generatorObject = (Object)value;
        }

        ///<summary>A handle to the currently playing <c>Generator</c>, if it can be controlled by scripting.</summary>
        ///<remarks>You can use <see cref="ControlContext.builtIn" /> to script and issue commands to the <c>Generator</c>. Since the <see cref="AudioSource" /> owns the <c>Generator</c>, you can check for the existence using <see cref="ControlContext.Exists" />.</remarks>
        public ProcessorInstance generatorInstance => new GeneratorInstance(generatorInstanceHandle);

        extern internal DualThreadHandle generatorInstanceHandle { get; }

        extern internal Object generatorObject { get; set; }

        ///<summary>The target group to which the AudioSource should route its signal.</summary>
        extern public AudioMixerGroup outputAudioMixerGroup { get; set; }

        ///<summary>Enable the audio source to play through a specific gamepad.</summary>
        ///<remarks>Set your current <see cref="T:UnityEditor.BuildTarget" /> to **PS4** or **PS5** to avoid a build error in the Windows Editor.</remarks>
        ///<param name="slot">Slot number of the gamepad (0-3).</param>
        ///<returns>Returns TRUE if enabling audio output through this users controller was successful.</returns>
        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::PlayOnGamepad", HasExplicitThis = true, ThrowsException = true)]
        extern public bool PlayOnGamepad(Int32 slot);

        ///<summary>Disables audio output to a gamepad for this audio source.</summary>
        ///<returns>Returns true if successful.</returns>
        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::DisableGamepadOutput", HasExplicitThis = true)]
        extern public bool DisableGamepadOutput();

        ///<exclude />
        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetGamepadSpeakerMixLevel", HasExplicitThis = true, ThrowsException = true)]
        extern public bool SetGamepadSpeakerMixLevel(Int32 slot, Int32 mixLevel);

        ///<exclude />
        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetGamepadSpeakerMixLevelDefault", HasExplicitThis = true, ThrowsException = true)]
        extern public bool SetGamepadSpeakerMixLevelDefault(Int32 slot);

        ///<exclude />
        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetGamepadSpeakerRestrictedAudio", HasExplicitThis = true, ThrowsException = true)]
        extern public bool SetGamepadSpeakerRestrictedAudio(Int32 slot, bool restricted);

        ///<summary>Check if the platform supports an audio output type  on gamepads.</summary>
        ///<param name="outputType">The desired output type.</param>
        ///<returns>Returns true if the gamepad supports the specified audio output type.</returns>
        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "GamepadSpeakerSupportsOutputType", HasExplicitThis = false)]
        extern static public bool GamepadSpeakerSupportsOutputType(GamepadSpeakerOutputType outputType);

        ///<summary>Gets or sets the gamepad audio output type for this audio source.</summary>
        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        extern public GamepadSpeakerOutputType gamepadSpeakerOutputType { get; set; }

        // Plays the ::ref::clip with a certain delay (the optional delay argument is deprecated since 4.1a3) and the functionality has been replaced by PlayDelayed.
        [ExcludeFromDocs]
        public void Play()
        {
            PlayHelper(this, 0);
        }

        ///<summary>Plays the <see cref="clip" />.</summary>
        ///<remarks>
        ///  <para>The delay parameter is deprecated, please use the newer <see cref="AudioSource.PlayDelayed" /> function instead which specifies the delay in seconds.
        ///
        ///                    If <see cref="AudioSource.clip" /> is set to the same clip that is playing then
        ///                    the clip will sound like it is re-started.  <see cref="AudioSource" /> will assume
        ///                    any <see cref="Play" /> call will have a new audio clip to play.
        ///
        ///**Note:** The <see cref="AudioSource.PlayScheduled" /> API will give you more accurate control over when the audio clip is played.
        ///
        ///For a list of audio file types Unity supports, refer to [Audio Clip](xref:class-AudioClip).</para>
        ///  <para />
        ///</remarks>
        ///<param name="delay">Deprecated. Delay in number of samples, assuming a 44100Hz sample rate (meaning that Play(44100) will delay the playing by exactly 1 sec).</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        /// // The Audio Source component has an AudioClip option.  The audio
        /// // played in this example comes from AudioClip and is called audioData.
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    AudioSource audioData;
        ///
        ///    void Start()
        ///    {
        ///        audioData = GetComponent<AudioSource>();
        ///        audioData.Play(0);
        ///        Debug.Log("started");
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        if (GUI.Button(new Rect(10, 70, 150, 30), "Pause"))
        ///        {
        ///            audioData.Pause();
        ///            Debug.Log("Pause: " + audioData.time);
        ///        }
        ///
        ///        if (GUI.Button(new Rect(10, 170, 150, 30), "Continue"))
        ///        {
        ///            audioData.UnPause();
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Stop" />
        ///<seealso cref="Pause" />
        ///<seealso cref="clip" />
        ///<seealso cref="PlayScheduled" />
        public void Play([UnityEngine.Internal.DefaultValue("0")] UInt64 delay)
        {
            PlayHelper(this, delay);
        }

        ///<summary>Plays the <see cref="clip" /> with a delay specified in seconds. Users are advised to use this function instead of the old Play(delay) function that took a delay specified in samples relative to a reference rate of 44.1 kHz as an argument.</summary>
        ///<remarks>**Note:** This function replaces the Play(delay) function when called with the delay-argument. In that function the delay had to be specified as samples relative to a reference rate of 44100. This is inconvenient when the engine is running on a different sample rate and the source sound has an even different rate. Working with delays specified in seconds makes this independent of these.</remarks>
        ///<param name="delay">Delay time specified in seconds.</param>
        public void PlayDelayed(float delay)
        {
            Play((delay < 0.0f) ? 0.0 : -(double)delay);
        }

        ///<summary>Plays the <see cref="clip" /> at a specific time on the absolute time-line that AudioSettings.dspTime reads from.</summary>
        ///<remarks>
        ///  <para>This is the preferred way to stitch AudioClips in music players because it is independent of the frame rate and gives the audio system enough time to prepare the playback of the sound to fetch it from media where the opening and buffering takes a lot of time (streams) without causing sudden CPU spikes.
        ///
        ///If <c>time</c> is less than the current <see cref="AudioSettings.dspTime" />, playback starts as soon as possible (typically immediately), not at that past instant. Any audio that would have played while <c>time</c> was in the past is not reproduced.
        ///
        ///If <c>time</c> is negative, it is treated as <c>0</c> (start without an absolute schedule delay).
        ///
        ///If the <see cref="AudioSource.resource" /> is an <see cref="AudioRandomContainer" />, a schedule time in the past can seek into the clip to preserve timing. If the lateness exceeds the clip length, that playback might not occur.</para>
        ///  <para>The example at <see cref="AudioSource.SetScheduledEndTime" /> shows how you can play two audio clips without pops or clicks between the clips.  The approach is to have two AudioSources with clips attached, and queue up each clip using its AudioSource.
        ///
        ///</para>
        ///</remarks>
        ///<param name="time">Absolute start time in seconds on the <see cref="AudioSettings.dspTime" /> timeline. Schedule a time slightly in the future (~100-200ms) so the audio system can prepare playback.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        /// // Basic demonstration of a music system that uses PlayScheduled to preload and sample-accurately
        /// // stitch two AudioClips in an alternating fashion.  The code assumes that the music pieces are
        /// // each 16 bars (4 beats / bar) at a tempo of 140 beats per minute.
        /// // To make it stitch arbitrary clips just replace the line
        /// //   nextEventTime += (60.0 / bpm) * numBeatsPerSegment
        /// // by
        /// //   nextEventTime += clips[flip].length;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float bpm = 140.0f;
        ///    public int numBeatsPerSegment = 16;
        ///    public AudioClip[] clips = new AudioClip[2];
        ///
        ///    private double nextEventTime;
        ///    private int flip = 0;
        ///    private AudioSource[] audioSources = new AudioSource[2];
        ///    private bool running = false;
        ///
        ///    void Start()
        ///    {
        ///        for (int i = 0; i < 2; i++)
        ///        {
        ///            GameObject child = new GameObject("Player");
        ///            child.transform.parent = gameObject.transform;
        ///            audioSources[i] = child.AddComponent<AudioSource>();
        ///        }
        ///
        ///        nextEventTime = AudioSettings.dspTime + 2.0f;
        ///        running = true;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (!running)
        ///        {
        ///            return;
        ///        }
        ///
        ///        double time = AudioSettings.dspTime;
        ///
        ///        if (time + 1.0f > nextEventTime)
        ///        {
        ///            // We are now approx. 1 second before the time at which the sound should play,
        ///            // so we will schedule it now in order for the system to have enough time
        ///            // to prepare the playback at the specified time. This may involve opening
        ///            // buffering a streamed file and should therefore take any worst-case delay into account.
        ///            audioSources[flip].clip = clips[flip];
        ///            audioSources[flip].PlayScheduled(nextEventTime);
        ///
        ///            Debug.Log("Scheduled source " + flip + " to start at time " + nextEventTime);
        ///
        ///            // Place the next event 16 beats from here at a rate of 140 beats per minute
        ///            nextEventTime += 60.0f / bpm * numBeatsPerSegment;
        ///
        ///            // Flip between two audio sources so that the loading process of one does not interfere with the one that's playing out
        ///            flip = 1 - flip;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AudioSource.SetScheduledStartTime" />
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

        ///<summary>Plays an <see cref="AudioClip" />, and scales the <see cref="AudioSource" /> volume by volumeScale.</summary>
        ///<remarks>
        ///  <para>
        ///    <see cref="AudioSource.PlayOneShot" /> does not cancel clips that are already being played by <see cref="AudioSource.PlayOneShot" /> and <see cref="AudioSource.Play" />. For more information on how this method differs from <see cref="AudioSource.Play" />, see <see cref="AudioSource" />.</para>
        ///  <para />
        ///</remarks>
        ///<param name="clip">The clip being played.</param>
        ///<param name="volumeScale">The scale of the volume. Unity automatically clamps negative scales to zero. Note: Scales larger than one might cause clipping.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public AudioClip impact;
        ///    AudioSource audioSource;
        ///
        ///    void Start()
        ///    {
        ///        audioSource = GetComponent<AudioSource>();
        ///    }
        ///
        ///    void OnCollisionEnter()
        ///    {
        ///        audioSource.PlayOneShot(impact, 0.7F);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AudioSource.Play" />
        public void PlayOneShot(AudioClip clip, [UnityEngine.Internal.DefaultValue("1.0F")] float volumeScale)
        {
            if (clip == null)
            {
                Debug.LogWarning("PlayOneShot was called with a null AudioClip.");
                return;
            }

            PlayOneShotHelper(this, clip, volumeScale);
        }

        ///<summary>Changes the time at which a sound that has already been scheduled to play will start.</summary>
        ///<remarks>Notice that depending on the timing not all rescheduling requests can be fulfilled.
        ///
        ///One interesting use case for this is stinger sound effects that are initiated by game events, but that you also want to be synchronized to specific beats in music. Then this function can be used to defer the stinger until the next musical transition.
        ///
        ///**Note:** In general it is better to use <see cref="PlayScheduled" /> to cue up audio.  Only use SetScheduledStartTime if you have scheduled an audio clip to play in the future and you need to change the time at which it starts.  Calling SetScheduledStartTime will not cause an un-scheduled audio clip to play.</remarks>
        ///<param name="time">Time in seconds.</param>
        ///<seealso cref="PlayScheduled" />
        extern public void SetScheduledStartTime(double time);
        ///<summary>Changes the time at which a sound that has already been scheduled to play will end. Notice that depending on the timing not all rescheduling requests can be fulfilled.</summary>
        ///<remarks>
        ///  <para>Note that the time specified is still a time on the absolute time-line, meaning that the sound will stop when reaching that time, regardless of when it was started. So if you have a 5 second long sound and want it to play at time T and stop after 3 seconds (i.e. silencing the last 2 seconds of the sound), you need to specify the end time to be T+3. This function is useful in music systems to overcome the discontinuities in signals that frame-based lossy codecs cause.</para>
        ///  <para>**Note:** If possible create clips that overlap, and use the scheduled end time for the first, and <see cref="AudioSource.time" /> for the second to trim out the overlapped part, as the example above shows.</para>
        ///</remarks>
        ///<param name="time">Time in seconds.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        /// // While this may seem unnecessarily complicated to do this in the case of uncompressed sounds, you can now use
        /// // the SavWav code from https://gist.github.com/2317063 to save the generated clips into new assets,
        /// // run the program once with a specified sourceClip and the script will generate "cut1.wav" and "cut2.wav".
        /// // These can now be imported into Unity as assets and changed to compressed sounds.
        /// // Since psychoacoustic compression severely alters the waveforms and frequency content of sounds and
        /// // furthermore operates in a block-based fashion, it would cause very noticeable pops and clicks if we didn't
        /// // have the sound data after and before the cut point. By having it, even though we are not playing it, the decoder is "warmed up",
        /// // i.e. it has matching frequency content before and after the transition, so at least the
        /// // frequency spectrum will be more or less the same before and after the transition and so the click will be less audible
        /// // than if we had just cut up the sound without the 0.2s overlap regions.
        /// // This method may also be combined with cross-fading in order to further smoothen out any remaining artifacts.
        ///[RequireComponent(typeof(AudioSource))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public AudioClip sourceClip;
        ///    private AudioSource audio1;
        ///    private AudioSource audio2;
        ///    private AudioClip cutClip1;
        ///    private AudioClip cutClip2;
        ///    private float overlap = 0.2F;
        ///    private int len1 = 0;
        ///    private int len2 = 0;
        ///    void Start()
        ///    {
        ///        GameObject child;
        ///        child = new GameObject("Player1");
        ///        child.transform.parent = gameObject.transform;
        ///        audio1 = child.AddComponent<AudioSource>();
        ///        child = new GameObject("Player2");
        ///        child.transform.parent = gameObject.transform;
        ///        audio2 = child.AddComponent<AudioSource>();
        ///        int overlapSamples;
        ///        if (sourceClip != null)
        ///        {
        ///            len1 = sourceClip.samples / 2;
        ///            len2 = sourceClip.samples - len1;
        ///            overlapSamples = (int)(overlap * sourceClip.frequency);
        ///            cutClip1 = AudioClip.Create("cut1", len1 + overlapSamples, sourceClip.channels, sourceClip.frequency, false);
        ///            cutClip2 = AudioClip.Create("cut2", len2 + overlapSamples, sourceClip.channels, sourceClip.frequency, false);
        ///            float[] smp1 = new float[(len1 + overlapSamples) * sourceClip.channels];
        ///            float[] smp2 = new float[(len2 + overlapSamples) * sourceClip.channels];
        ///            sourceClip.GetData(smp1, 0);
        ///            sourceClip.GetData(smp2, len1 - overlapSamples);
        ///            cutClip1.SetData(smp1, 0);
        ///            cutClip2.SetData(smp2, 0);
        ///        }
        ///        else
        ///        {
        ///            overlapSamples = (int)overlap * cutClip1.frequency;
        ///            len1 = cutClip1.samples - overlapSamples;
        ///            len2 = cutClip2.samples - overlapSamples;
        ///        }
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        if (GUI.Button(new Rect(10, 50, 230, 40), "Trigger source"))
        ///            audio1.PlayOneShot(sourceClip);
        ///
        ///        if (GUI.Button(new Rect(10, 100, 230, 40), "Trigger cut 1"))
        ///            audio1.PlayOneShot(cutClip1);
        ///
        ///        if (GUI.Button(new Rect(10, 150, 230, 40), "Trigger cut 2"))
        ///            audio1.PlayOneShot(cutClip2);
        ///
        ///        if (GUI.Button(new Rect(10, 200, 230, 40), "Play stitched"))
        ///        {
        ///            audio1.clip = cutClip1;
        ///            audio2.clip = cutClip2;
        ///            double t0 = AudioSettings.dspTime + 3.0F;
        ///            double clipTime1 = len1;
        ///            clipTime1 /= cutClip1.frequency;
        ///            audio1.PlayScheduled(t0);
        ///            audio1.SetScheduledEndTime(t0 + clipTime1);
        ///            Debug.Log("t0 = " + t0 + ", clipTime1 = " + clipTime1 + ", cutClip1.frequency = " + cutClip1.frequency);
        ///            Debug.Log("cutClip2.frequency = " + cutClip2.frequency + ", samplerate = " + AudioSettings.outputSampleRate);
        ///            audio2.PlayScheduled(t0 + clipTime1);
        ///            audio2.time = overlap;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public void SetScheduledEndTime(double time);

        ///<summary>Stops playing the <see cref="clip" />.</summary>
        ///<remarks>The AudioSource.stop function stops the currently set Audio clip from playing. The Audio clip plays from the beginning the next time you play it.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// //This script allows you to toggle music to play and stop.
        /// //Assign an AudioSource to a GameObject and attach an Audio Clip in the Audio Source. Attach this script to the GameObject.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    AudioSource m_MyAudioSource;
        ///
        ///    //Play the music
        ///    bool m_Play;
        ///    //Detect when you use the toggle, ensures music isn’t played multiple times
        ///    bool m_ToggleChange;
        ///
        ///    void Start()
        ///    {
        ///        //Fetch the AudioSource from the GameObject
        ///        m_MyAudioSource = GetComponent<AudioSource>();
        ///        //Ensure the toggle is set to true for the music to play at start-up
        ///        m_Play = true;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Check to see if you just set the toggle to positive
        ///        if (m_Play == true && m_ToggleChange == true)
        ///        {
        ///            //Play the audio you attach to the AudioSource component
        ///            m_MyAudioSource.Play();
        ///            //Ensure audio doesn’t play more than once
        ///            m_ToggleChange = false;
        ///        }
        ///        //Check if you just set the toggle to false
        ///        if (m_Play == false && m_ToggleChange == true)
        ///        {
        ///            //Stop the audio
        ///            m_MyAudioSource.Stop();
        ///            //Ensure audio doesn’t play more than once
        ///            m_ToggleChange = false;
        ///        }
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        //Switch this toggle to activate and deactivate the parent GameObject
        ///        m_Play = GUI.Toggle(new Rect(10, 10, 100, 30), m_Play, "Play Music");
        ///
        ///        //Detect if there is a change with the toggle
        ///        if (GUI.changed)
        ///        {
        ///            //Change to true to show that there was just a change in the toggle state
        ///            m_ToggleChange = true;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Play" />
        ///<seealso cref="Pause" />
        public void Stop()
        {
            Stop(true);
        }

        ///<summary>Pauses playing the <see cref="clip" />.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// // Allow a song to be chosen and played.  If can be paused, and the song played further.
        /// // Two songs are supported.
        ///
        ///using System.Collections;
        ///using System.Collections.Generic;
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // two clips, perhaps songs for the game
        ///    public AudioClip song1;
        ///    public AudioClip song2;
        ///
        ///    private AudioSource audioSource;
        ///    private bool paused1;
        ///    private bool paused2;
        ///
        ///    // both songs are in paused state
        ///    void Start()
        ///    {
        ///        audioSource = GetComponent<AudioSource>();
        ///        paused1 = true;
        ///        paused2 = true;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        if (GUI.Button(new Rect(10, 10, 200, 100), "Play song1"))
        ///        {
        ///            if (paused1 && paused2)
        ///            {
        ///                audioSource.clip = song1;
        ///                audioSource.Play(0);
        ///                paused1 = false;
        ///            }
        ///        }
        ///
        ///        if (GUI.Button(new Rect(250, 10, 200, 100), "Pause song1"))
        ///        {
        ///            if (paused1 == false)
        ///            {
        ///                audioSource.Pause();
        ///                paused1 = true;
        ///            }
        ///        }
        ///
        ///        if (GUI.Button(new Rect(10, 180, 200, 100), "Play song2"))
        ///        {
        ///            if (paused2 && paused1)
        ///            {
        ///                audioSource.clip = song2;
        ///                audioSource.Play(0);
        ///                paused2 = false;
        ///            }
        ///        }
        ///
        ///        if (GUI.Button(new Rect(250, 180, 200, 100), "Pause song2"))
        ///        {
        ///            if (paused2 == false)
        ///            {
        ///                audioSource.Pause();
        ///                paused2 = true;
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Play" />
        ///<seealso cref="Stop" />
        extern public void Pause();

        ///<summary>Unpause the paused playback of this AudioSource.</summary>
        ///<remarks>This function is similar to if you call Play() on a paused AudioSource, except that it will not create a new playback voice if it is not currently paused.
        ///
        ///This is also useful if you have paused one-shots and want to resume playback without creating a new playback voice for the attached AudioClip.
        ///
        ///If you use <c>UnPause</c> on an AudioSource that hasn't played before or has stopped, the audio will not play. This is because <c>UnPause</c> resumes the clip from when the clip was last paused. You need to play and then pause the clip before you can unpause it.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        /// // The Audio Source component has an AudioClip option.  The audio
        /// // played in this example comes from AudioClip and is called audioData.
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    AudioSource audioSource;
        ///
        ///    void Start()
        ///    {
        ///        audioSource = GetComponent<AudioSource>();
        ///        audioSource.Play(0);
        ///        Debug.Log("started");
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        if (GUI.Button(new Rect(10, 70, 150, 30), "Pause"))
        ///        {
        ///            audioSource.Pause();
        ///            Debug.Log("Pause: " + audioSource.time);
        ///        }
        ///
        ///        if (GUI.Button(new Rect(10, 170, 150, 30), "Continue"))
        ///        {
        ///            audioSource.UnPause();
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public void UnPause();

        // Calls skip on any AudioContainers on the source
        internal extern void SkipToNextElementIfHasContainer();

        ///<summary>Returns whether the AudioSource is currently playing an <see cref="AudioResource" />(RO).</summary>
        ///<remarks>AudioSource.isPlaying returns true if the <see cref="AudioSource" /> is playing any <see cref="AudioResource" />, such as <see cref="AudioClip" /> or AudioRandomContainer. This includes if you use PlayOneShot() or if you play a video or timeline track through the AudioSource. 
        ///
        ///**Note:** <see cref="AudioSource.isPlaying" /> returns false when <c>AudioSource.Pause()</c> is called. If you use
        ///<c>AudioSource.Play()</c> back again, it returns true.
        ///
        ///**Note:** If you use <see cref="AudioSource.PlayDelayed" /> to play your clip, AudioSource.isPlaying returns true during the delay.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // When the audio component has stopped playing, play otherClip.
        /// // Remember to assign an AudioClip in the Inspector.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public AudioClip otherClip;
        ///    AudioSource audioSource;
        ///
        ///    void Start()
        ///    {
        ///        audioSource = GetComponent<AudioSource>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (!audioSource.isPlaying)
        ///        { 
        ///            audioSource.clip = otherClip;
        ///            audioSource.Play();
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool isPlaying
        {
            [NativeName("IsPlayingScripting")]
            get;
        }

        internal extern bool isContainerPlaying
        {
            [NativeName("IsContainerPlaying")]
            get;
        }

        // Whether the source is in the audio manager's per-frame update list. Test-only: lets tests
        // assert that an idle source is cleaned up again after external playback ends.
        internal extern bool isRegisteredWithAudioManager
        {
            [NativeName("IsRegisteredWithAudioManager")]
            get;
        }

        internal extern ActivePlayable[] containerActivePlayables { get; }

        ///<summary>True if all sounds played by the AudioSource, such as main sound started by Play() or playOnAwake, and one-shots are culled by the audio system.</summary>
        ///<remarks>A sound is culled when its resulting volume is lower than the volumes of the N loudest voices, where N is the number of maximum audible sounds specified in the audio Project Settings or via <see cref="AudioConfiguration" />.</remarks>
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

        ///<summary>Plays an AudioClip at a given position in world space.</summary>
        ///<remarks>This function creates an audio source but automatically disposes of it once the clip has finished playing.</remarks>
        ///<param name="clip">Audio data to play.</param>
        ///<param name="position">Position in world space from which sound originates.</param>
        ///<param name="volume">Playback volume (range from 0.0 - 1.0).</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    public AudioClip clip; //make sure you assign an actual clip here in the inspector
        ///
        ///    void Start()
        ///    {
        ///        AudioSource.PlayClipAtPoint(clip, new Vector3(5, 1, 2));
        ///    }
        ///}
        ///]]></code>
        ///</example>
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

        ///<summary>Checks if the audio clip is looping</summary>
        ///<remarks>Return or set whether the audio clip replays after it finishes playing.
        ///Disable looping on a playing AudioSource to stop the sound after the end of the current loop. Use the checkbox in the AudioSource component to enable or disable looping without code.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// //Create an empty GameObject and attach this script
        /// //Attach an AudioSource component. (Click on the GameObject, go to its Inspector and click __Add Component__ Button. Go to __Audio__>__Audio Source__)
        /// //Attach an Audio clip in the AudioClip field of the AudioSource
        /// //Create a Button (__Create__>__UI__>__Button__) and a Toggle (__Create__>__UI__>__Toggle__). Attach these in the Inspector of your GameObject.
        ///
        /// //This script allows you to toggle the loop of a sound on or off
        ///using UnityEngine;
        ///using UnityEngine.UI;
        ///
        ///public class AudioSourceLoop : MonoBehaviour
        ///{
        ///    AudioSource m_AudioSource;
        ///
        ///    public Toggle m_Toggle;
        ///    public Button m_Button;
        ///
        ///    void Start()
        ///    {
        ///        //Fetch the AudioSource component of the GameObject (make sure there is one in the Inspector)
        ///        m_AudioSource = GetComponent<AudioSource>();
        ///        //Stop the Audio playing
        ///        m_AudioSource.Stop();
        ///        //Call the PlayButton function when you click this Button
        ///        m_Button.onClick.AddListener(PlayButton);
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Turn the loop on and off depending on the Toggle status
        ///        m_AudioSource.loop = m_Toggle.isOn;
        ///    }
        ///
        ///    //This plays the Audio clip when you press the Button
        ///    void PlayButton()
        ///    {
        ///        m_AudioSource.Play();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool loop { get; set; }

        ///<summary>This makes the audio source not take into account the volume of the audio listener.</summary>
        ///<remarks>Enable this when playing back music.
        ///When playing back music you want a separate setting which is unaffected by the normal sound effects volume.</remarks>
        extern public bool ignoreListenerVolume { get; set; }

        ///<summary>Enable this property to automatically play the audio source when the component or GameObject becomes active.</summary>
        ///<remarks>If you enable this property and the GameObject isn't active or if the AudioSource component is disabled, the audio won't play until they become active. While this property is enabled, if you disable then enable the GameObject or the audio source, the audio will stop and then play again from the start. If you set this property to <c>false</c>, the audio doesn't play. In this case, you need to use <see cref="AudioSource.Play" /> to play the audio.</remarks>
        ///<example>
        ///  <code><![CDATA[ // This script creates and attaches an audio source to the GameObject and disables playOnAwake. 
        /// // This means the audio won't play on launch, instead you need to press the button this script creates to play the audio.
        /// // Make sure to assign an audio clip in the Inspector. 
        ///
        ///using UnityEngine;
        ///
        ///public class PlayOnAwakeExample : MonoBehaviour
        ///{
        ///    AudioSource audioSource;
        ///    public AudioClip audioClip; 
        ///
        ///    void Awake()
        ///    {
        ///        audioSource = gameObject.AddComponent<AudioSource>();
        ///        audioSource.playOnAwake = false;
        ///        audioSource.clip = audioClip; 
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        if(GUI.Button(new Rect(10, 70, 150, 30), "Play"))
        ///        {
        ///            audioSource.Play(); 
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[ // This script creates 2 toggles - 1 changes the status of [[AudioSource.playOnAwake]], the other activates or deactivates the audio source component. 
        /// // If you deactivate __Enable audio source__, any audio that is currently playing will stop. 
        /// // If you enable __Play on Awake__ and activate __Enable audio source__, the audio will play from the start. 
        /// // If you disable __Play on Awake__, the audio will not play when you activate the audio source. 
        ///
        /// // For this script to work, attach the script and an AudioSource component to a GameObject in your Scene. 
        /// // Also assign an audio generator to the AudioSource so it has audio to play.  
        ///
        ///using UnityEngine;
        ///
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///public class PlayOnAwakeExample : MonoBehaviour
        ///{
        ///    AudioSource audioSource;
        ///
        ///    void Awake()
        ///    {
        ///        audioSource = GetComponent<AudioSource>();
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        // Toggles that change the status of AudioSource.playOnAwake and AudioSource.enabled.
        ///        audioSource.playOnAwake = GUI.Toggle(new Rect(10, 0, 150, 30), audioSource.playOnAwake, "Play on Awake");
        ///        audioSource.enabled = GUI.Toggle(new Rect(10, 70, 150, 30), audioSource.enabled, "Enable audio source");
        ///    }
        ///}]]></code>
        ///</example>
        extern public bool playOnAwake { get; set; }

        ///<summary>Allows AudioSource to play even though <c>AudioListener.pause</c> is set to true. This is useful for the menu element sounds or background music in pause menus.</summary>
        ///<remarks>This property can only be set via the script and is not serialized.</remarks>
        extern public bool ignoreListenerPause { get; set; }

        ///<summary>Whether the Audio Source should be updated in the fixed or dynamic update.</summary>
        ///<remarks>Make sure this is set to update in the same update loop as the Audio Source is moved in if you are experiencing problems with Doppler effect simulation for this source.
        ///The default setting will automatically set the source to be updated in the fixed update loop if it is attached to a rigidbody, and dynamic otherwise.</remarks>
        extern public AudioVelocityUpdateMode velocityUpdateMode { get; set; }

        ///<summary>Pans a playing sound in a stereo way (left or right). This only applies to sounds that are Mono or Stereo.</summary>
        ///<remarks>This pan is applied before 3D panning calculations are considered. In other words, stereo panning affects the left right balance of the sound before it is spatialised in 3D.
        ///
        ///Mono sounds are panned from left to right using constant power panning (non linear fade). This means when pan = 0.0, the balance for the sound in each speaker is 71% left and 71% right, not 50% left and 50% right. This gives (audibly) smoother pans.
        ///
        ///Stereo sounds heave each left/right value faded up and down according to the specified pan position. This means when pan = 0.0, the balance for the sound in each speaker is 100% left and 100% right. When pan = -1.0, only the left channel of the stereo sound is audible, when pan = 1.0, only the right channel of the stereo sound is audible.
        ///
        ///Values range from -1.0 to 1.0.
        ///
        ///-1.0 = Full left
        ///0.0 = center
        ///1.0 = full right
        ///
        ///Default = 0.0.</remarks>
        ///<example>
        ///  <code><![CDATA[]]></code>
        ///</example>
        [NativeProperty("StereoPan")]
        extern public float panStereo { get; set; }

        ///<summary>Sets how much this AudioSource is affected by 3D spatialisation calculations (attenuation, doppler etc). 0.0 makes the sound full 2D, 1.0 makes it full 3D.</summary>
        ///<remarks>Aside from determining if this AudioSource is heard as a 2D or 3D source, this property is useful to morph between the two modes.
        ///
        ///3D spatial calculations are applied after stereo panning is determined and can be used in conjunction with panStereo.
        ///
        ///Morphing between the 2 modes is useful for sounds that should be progressively heard as normal 2D sounds the closer they are to the listener.</remarks>
        [NativeProperty("SpatialBlendMix")]
        extern public float spatialBlend { get; set; }

        ///<summary>Enables or disables spatialization.</summary>
        ///<remarks>Custom spatializer effects improve the realism of sound propagation by incorporating the binaural head-related transfer function (HRTF) such that the listener can better sense the directionality of the sound through the filtering of the head and the micro-delays between the ears. Unity supports custom spatialization effects as optional plugins through the native audio plugin system, and in case such plugins are present, will show a "Spatialize" checkbox on the AudioSource that corresponds to this property. If no plugin is present (and selected in the project audio settings), attempts to set this property to true will fail with a warning.</remarks>
        extern public bool spatialize { get; set; }

        ///<summary>Determines if the spatializer effect is inserted before or after the effect filters.</summary>
        ///<remarks>This flag only has an effect if the spatialize flag is enabled on the AudioSource. See <see cref="AudioSource.spatialize" /> for further information about spatialization.</remarks>
        extern public bool spatializePostEffects { get; set; }

        ///<summary>Set the custom curve for the given AudioSourceCurveType.</summary>
        ///<remarks>The curve will be scaled so that it is applied over <see cref="AudioSource.maxDistance" /> from the AudioSource.
        ///
        ///Note that the internal AnimationCurve to be rescaled to range from 0..1 for performance reasons. This means calling <see cref="AudioSource.GetCustomCurve" /> will return a potentially different curve to what was just set.</remarks>
        ///<param name="type">The curve type that should be set.</param>
        ///<param name="curve">The curve that should be applied to the given curve type.</param>
        public void SetCustomCurve(AudioSourceCurveType type, AnimationCurve curve)
        {
            SetCustomCurveHelper(this, type, curve);
        }

        ///<summary>Get the current custom curve for the given AudioSourceCurveType.</summary>
        ///<remarks>Note that if there is no curve set, or the corresponding curve type value setter has been  set, a single key AnimationCurve will be returned corresponding to the current value.</remarks>
        ///<param name="type">The curve type to get.</param>
        ///<returns>The custom AnimationCurve corresponding to the given curve type.</returns>
        public AnimationCurve GetCustomCurve(AudioSourceCurveType type)
        {
            return GetCustomCurveHelper(this, type);
        }

        ///<summary>The amount by which the signal from the AudioSource will be mixed into the global reverb associated with the Reverb Zones.</summary>
        ///<remarks>The range from 0 to 1 is linear (like the volume property) while the range from 1 to 1.1 is an extra boost range that allows you to boost the reverberated signal by 10 dB. The associated curve in combination with the distance-based attenuation curve is useful when trying to simulate transitions from near-field to distant sounds.
        ///
        ///Note that prior to Unity 5.0 reverb zones were not applied to 2D sounds. With the generalization of 2D and 3D sounds in Unity 5.0 through the Spatial Blend parameter the reverb can now be applied to any sound. Therefore when importing Unity projects made with versions prior to 5.0 this parameter will be set to 0 for all the sounds that were 2D sounds, but is now adjustable.</remarks>
        extern public float reverbZoneMix { get; set; }

        ///<summary>Bypass effects (Applied from filter components or global listener filters).</summary>
        extern public bool bypassEffects { get; set; }

        ///<summary>When set, global effects on the AudioListener doesn't apply to the audio signal generated by the AudioSource. It also doesn't apply, if the AudioSource is playing into a mixer group.</summary>
        extern public bool bypassListenerEffects { get; set; }

        ///<summary>When set, it doesn't route the signal from an AudioSource into the global reverb associated with reverb zones.</summary>
        extern public bool bypassReverbZones { get; set; }

        ///<summary>Sets the Doppler scale for this AudioSource.</summary>
        extern public float dopplerLevel { get; set; }

        ///<summary>Sets the spread angle (in degrees) of a 3d stereo or multichannel sound in speaker space.</summary>
        ///<remarks>0 = all sound channels are located at the same speaker location and is 'mono'. 360 = all subchannels are located at the opposite speaker location to the speaker location that it should be according to 3D position. Default = 0.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // when any AudioSource goes trough this transform, it will set it as 'mono'
        ///    // and will restore the value to 3D effect after exiting
        ///    // Make sure the audio source has a collider.
        ///    void OnTriggerEnter(Collider other)
        ///    {
        ///        AudioSource audio = other.GetComponent<AudioSource>();
        ///
        ///        if (audio)
        ///        {
        ///            audio.spread = 0;
        ///        }
        ///    }
        ///
        ///    void OnTriggerExit(Collider other)
        ///    {
        ///        AudioSource audio = other.GetComponent<AudioSource>();
        ///
        ///        if (audio)
        ///        {
        ///            audio.spread = 360;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float spread { get; set; }

        ///<summary>Sets the priority of the <see cref="AudioSource" />.</summary>
        ///<remarks>Unity virtualizes AudioSources when the number of active AudioSources exceeds the limit set by your project's maximum real voices. Real voices are audio sources that are audible in the scene. 
        ///                When Unity virtualizes an <see cref="AudioSource" />, it mutes the source while tracking its playback position and state, allowing it to resume playback if its priority or volume becomes higher than another audio source. Unity virtualizes <see cref="AudioSource" /> instances with the lowest priority first. If two sounds have the same priority, Unity virtualizes the one with the lower volume. Priority is an integer between 0 (highest priority) and 256 (lowest priority).
        ///
        ///To change the value of the maximum number of real or virtual voices: 
        ///
        ///1. In the menu go to **Edit** &gt; **Project Settings** &gt; **Audio**.
        ///2. Set **Maximum Real Voices** and **Maximum Virtual Voices** to your preferred values
        ///
        ///**WebGL:** This setting doesn't affect WebGL because there is no limit on the number of audio channels in the WebGL platform.</remarks>
        extern public int priority { get; set; }

        // Un- / Mutes the AudioSource. Mute sets the volume=0, Un-Mute restore the original volume.
        ///<summary>Un- / Mutes the AudioSource. Mute sets the volume=0, Un-Mute restore the original volume.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// // Mutes-Unmutes the sound from this object each time the user presses space.
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    AudioSource audioSource;
        ///
        ///    void Start()
        ///    {
        ///        audioSource = GetComponent<AudioSource>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (Input.GetKeyDown(KeyCode.Space))
        ///            audioSource.mute = !audioSource.mute;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool mute { get; set; }

        ///<summary>Within the Min distance the AudioSource will cease to grow louder in volume.</summary>
        ///<remarks>Outside the min distance the volume starts to attenuate.</remarks>
        extern public float minDistance { get; set; }

        ///<summary>The distance where sound either becomes inaudible or stops attenuation, depending on the rolloff mode.</summary>
        ///<remarks>
        ///  <see cref="AudioRolloffMode.Linear" />: For the linear rolloff mode, the <c>maxDistance</c> is the point where the volume reaches zero and the sound becomes inaudible. 
        ///
        ///<see cref="AudioRolloffMode.Custom" />: For the custom rolloff mode, the <c>maxDistance</c> sets the distance bounds of the curve. Any distance beyond holds the last available value.
        ///
        ///<see cref="AudioRolloffMode.Logarithmic" />: For the logarithmic rolloff mode, the audio source ignores this setting. The sound will continue to attenuate with distance indefinitely.</remarks>
        extern public float maxDistance { get; set; }

        ///<summary>Sets/Gets how the AudioSource attenuates over distance.</summary>
        extern public AudioRolloffMode rolloffMode { get; set; }

        ///<summary>Provides a block of the currently playing source's output data.</summary>
        ///<remarks>The array given in the samples parameter will be filled with the requested data. 
        ///
        ///<c>AudioSource.GetOutputData</c> provides access to audio data from a short history window (for example, the last few milliseconds) for analysis purposes. Unity doesn't automatically allocate the buffers required to store this history because doing so would be expensive and memory-intensive. Instead, Unity only allocates buffers and starts to record when you first call this function, on a per-object basis. As a result, the output data will initially be empty until the engine processes sufficient audio to populate the buffer. Please note this function isn't suited for critical or chronological, real-time data analysis or processing, or scenarios where you require low latency.</remarks>
        ///<param name="samples">The array to populate with audio samples. Its length must be a power of 2.</param>
        ///<param name="channel">The channel to sample from.</param>
        ///<seealso cref="AudioSource.GetSpectrumData" />
        ///<seealso cref="AudioListener.GetSpectrumData" />
        ///<seealso cref="AudioListener.GetOutputData" />
        public void GetOutputData(float[] samples, int channel)
        {
            GetOutputDataHelper(this, samples, channel);
        }

        ///<summary>Provides the block of audio frequencies (spectrum data) of the AudioSource that is currently playing.</summary>
        ///<remarks>This method fills the array you pass as the <c>samples</c> parameter with the spectrum data of the AudioSource.
        ///
        ///The frequency domain represents the frequencies and amplitude of an audio signal. Spectrum data provides the raw frequency domain data of the audio, which you can use to create a spectrogram to visualize the data.
        ///
        ///Audio frequency bands are ranges of sound frequencies that describe different parts of the audio spectrum (like sub-bass, bass, brilliance). The audio frequency bands are evenly spaced between 0 to half of the sampling rate. GetSpectrumData uses the sampling rate from <see cref="AudioSettings.outputSampleRate" />, not the sampling rate in the audio clip.
        ///
        ///Use <see cref="FFTWindow">window</see> to reduce leakage or scalloping loss between audio frequency bins/bands.
        ///
        ///**Note:** A more complex window type might be less efficient and worsen resolution (lobe width).
        ///
        ///**Note**: <c>GetSpectrumData</c> provides access to audio data from a short history window (for example, the last few milliseconds) for analysis purposes. Unity doesn't automatically allocate the buffers required to store this history because doing so would be expensive and memory-intensive. Instead, Unity only allocates buffers and starts to record when you first call this function, on a per-object basis. As a result, the output data will initially be empty until the engine processes sufficient audio to populate the buffer. Please note this function isn't suited for critical or chronological, real-time data analysis or processing, or scenarios where you require low latency.
        ///
        ///For related information, refer to <see cref="AudioSource.GetOutputData" />, <see cref="AudioListener.GetSpectrumData" />, <see cref="AudioListener.GetOutputData" />.</remarks>
        ///<param name="samples">The array to populate with frequency domain representations of audio samples. The array length must be a power of 2 (such as 128, 256, 512). Also, the length must not be less than 64 or greater than 8192.</param>
        ///<param name="channel">The channel to sample from.</param>
        ///<param name="window">The FFTWindow type to use when sampling.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///public class AudioSourceGetSpectrumDataExample : MonoBehaviour
        ///{
        ///    AudioSource m_MyAudioSource;
        ///
        ///    void Start()
        ///    {
        ///        m_MyAudioSource = GetComponent<AudioSource>();
        ///    }
        ///    
        ///    void Update()
        ///    {
        ///        float[] spectrum = new float[256];
        ///
        ///        m_MyAudioSource.GetSpectrumData(spectrum, 0, FFTWindow.Rectangular);
        ///
        ///        // Loop through the populated array
        ///        // Start the loop from 1 and to 1 less than the length, so the loop can draw lines between adjacent bins. 
        ///
        ///        for (int i = 1; i < spectrum.Length - 1; i++)
        ///        {
        ///            Debug.DrawLine(new Vector3(i - 1, spectrum[i] + 10, 0), new Vector3(i, spectrum[i + 1] + 10, 0), Color.red);
        ///            Debug.DrawLine(new Vector3(i - 1, Mathf.Log(spectrum[i - 1]) + 10, 2), new Vector3(i, Mathf.Log(spectrum[i]) + 10, 2), Color.cyan);
        ///            Debug.DrawLine(new Vector3(Mathf.Log(i - 1), spectrum[i - 1] - 10, 1), new Vector3(Mathf.Log(i), spectrum[i] - 10, 1), Color.green);
        ///            Debug.DrawLine(new Vector3(Mathf.Log(i - 1), Mathf.Log(spectrum[i - 1]), 3), new Vector3(Mathf.Log(i), Mathf.Log(spectrum[i]), 3), Color.blue);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void GetSpectrumData(float[] samples, int channel, FFTWindow window)
        {
            GetSpectrumDataHelper(this, samples, channel, window);
        }

        ///<exclude />
        [Obsolete("minVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead.", true)]
        public float minVolume
        {
            get { Debug.LogError("minVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead."); return 0.0f; }
            set { Debug.LogError("minVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead."); }
        }

        ///<exclude />
        [Obsolete("maxVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead.", true)]
        public float maxVolume
        {
            get { Debug.LogError("maxVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead."); return 0.0f; }
            set { Debug.LogError("maxVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead."); }
        }

        ///<exclude />
        [Obsolete("rolloffFactor is not supported anymore. Use min-, maxDistance and rolloffMode instead.", true)]
        public float rolloffFactor
        {
            get { Debug.LogError("rolloffFactor is not supported anymore. Use min-, maxDistance and rolloffMode instead."); return 0.0f; }
            set { Debug.LogError("rolloffFactor is not supported anymore. Use min-, maxDistance and rolloffMode instead."); }
        }

        ///<summary>Sets a user-defined parameter of a custom spatializer effect that is attached to an AudioSource.</summary>
        ///<remarks>Since this is for internal use in custom inspectors controlling custom spatializer effects implemented as native audio plugins, it is up to the spatializer to perform necessary clipping on the UI and native sides through the setparameter/getparameter callbacks of the native audio plugin.</remarks>
        ///<param name="index">Zero-based index of user-defined parameter to be set.</param>
        ///<param name="value">New value of the user-defined parameter.</param>
        ///<returns>True, if the parameter could be set.</returns>
        extern public bool SetSpatializerFloat(int index, float value);
        ///<summary>Reads a user-defined parameter of a custom spatializer effect that is attached to an AudioSource.</summary>
        ///<remarks>Since this is for internal use in custom inspectors controlling custom spatializer effects implemented as native audio plugins, it is up to the spatializer to perform necessary clipping on the UI and native sides through the setparameter/getparameter callbacks of the native audio plugin.</remarks>
        ///<param name="index">Zero-based index of user-defined parameter to be read.</param>
        ///<param name="value">Return value of the user-defined parameter that is read.</param>
        ///<returns>True, if the parameter could be read.</returns>
        extern public bool GetSpatializerFloat(int index, out float value);

        ///<summary>Reads a user-defined parameter of a custom ambisonic decoder effect that is attached to an AudioSource.</summary>
        ///<remarks>Since this is for internal use in custom inspectors controlling custom ambisonic decoder effects implemented as native audio plugins, it is up to the decoder to perform necessary clipping on the UI and native sides through the setparameter/getparameter callbacks of the native audio plugin.</remarks>
        ///<param name="index">Zero-based index of user-defined parameter to be read.</param>
        ///<param name="value">Return value of the user-defined parameter that is read.</param>
        ///<returns>True, if the parameter could be read.</returns>
        extern public bool GetAmbisonicDecoderFloat(int index, out float value);
        ///<summary>Sets a user-defined parameter of a custom ambisonic decoder effect that is attached to an AudioSource.</summary>
        ///<remarks>Since this is for internal use in custom inspectors controlling custom ambisonic decoder effects implemented as native audio plugins, it is up to the decoder to perform necessary clipping on the UI and native sides through the setparameter/getparameter callbacks of the native audio plugin.</remarks>
        ///<param name="index">Zero-based index of user-defined parameter to be set.</param>
        ///<param name="value">New value of the user-defined parameter.</param>
        ///<returns>True, if the parameter could be set.</returns>
        extern public bool SetAmbisonicDecoderFloat(int index, float value);

        extern internal float GetAudioRandomContainerRuntimeMeterValue();
    }

    ///<summary>Reverb Zones are used when you want to create location based ambient effects in the Scene.</summary>
    ///<remarks>As the Audio Listener moves into a Reverb Zone, the ambient effect associated with the zone is gradually applied.
    ///At the max distance there is no effect and at the min distance the effect is fully applied.
    ///For example you can gradually change your character's footsteps sounds and create the
    ///feeling like you where entering into a cavern, going trough a room,
    ///swimming underwater, etc.
    ///
    ///You can always mix reverb zones to have combined effects.
    ///For more info check [Reverb Zones](xref:class-AudioReverbZone) in the manual.</remarks>
    [RequireComponent(typeof(Transform))]
    [global::UnityEngine.NativeClass("AudioReverbZone", PersistentTypeId = 167)]
    [NativeHeader("Modules/Audio/Public/AudioReverbZone.h")]
    public sealed partial class AudioReverbZone : Behaviour
    {
        //  The distance from the centerpoint that the reverb will have full effect at. Default = 10.0.
        ///<summary>The distance from the centerpoint that the reverb will have full effect at. Default = 10.0.</summary>
        extern public float minDistance { get; set; }

        //  The distance from the centerpoint that the reverb will not have any effect. Default = 15.0.
        ///<summary>The distance from the centerpoint that the reverb will not have any effect. Default = 15.0.</summary>
        extern public float maxDistance { get; set; }

        ///<summary>Set/Get reverb preset properties.</summary>
        extern public AudioReverbPreset reverbPreset { get; set; }

        ///<summary>Room effect level (at mid frequencies).</summary>
        extern public int room { get; set; }

        ///<summary>Relative room effect level at high frequencies.</summary>
        extern public int roomHF { get; set; }

        ///<summary>Relative room effect level at low frequencies.</summary>
        extern public int roomLF { get; set; }

        ///<summary>Reverberation decay time at mid frequencies.</summary>
        extern public float decayTime { get; set; }

        ///<summary>High-frequency to mid-frequency decay time ratio.</summary>
        extern public float decayHFRatio { get; set; }

        ///<summary>Early reflections level relative to room effect.</summary>
        extern public int reflections { get; set; }

        ///<summary>Initial reflection delay time.</summary>
        extern public float reflectionsDelay { get; set; }

        ///<summary>Late reverberation level relative to room effect.</summary>
        extern public int reverb { get; set; }

        ///<summary>Late reverberation delay time relative to initial reflection.</summary>
        extern public float reverbDelay { get; set; }

        ///<summary>Reference high frequency (hz).</summary>
        extern public float HFReference { get; set; }

        ///<summary>Reference low frequency (hz).</summary>
        extern public float LFReference { get; set; }

        ///<summary>Value that controls the echo density in the late reverberation decay.</summary>
        extern public float diffusion { get; set; }

        ///<summary>Value that controls the modal density in the late reverberation decay.</summary>
        extern public float density { get; set; }

        extern internal bool active { get; set; }
    }

    ///<summary>The Audio Low Pass Filter passes low frequencies of an <see cref="AudioSource" /> or all sounds reaching an <see cref="AudioListener" />, and attenuates frequencies above the Cutoff Frequency.</summary>
    ///<seealso href="xref:class-AudioLowPassFilter">Audio Low Pass Filter</seealso>
    [global::UnityEngine.NativeClass("AudioLowPassFilter", PersistentTypeId = 169)]
    [RequireComponent(typeof(AudioBehaviour))]
    public sealed partial class AudioLowPassFilter : Behaviour
    {
        extern private AnimationCurve GetCustomLowpassLevelCurveCopy();

        [NativeMethod(Name = "AudioLowPassFilterBindings::SetCustomLowpassLevelCurveHelper", IsFreeFunction = true, ThrowsException = true)]
        extern static private void SetCustomLowpassLevelCurveHelper([NotNull] AudioLowPassFilter source, AnimationCurve curve);

        ///<summary>Returns or sets the current custom frequency cutoff curve.</summary>
        ///<remarks>The curve will be scaled so that it is applied over <see cref="AudioSource.maxDistance" /> from the AudioSource.
        ///
        ///Note that if there is no curve set, or <see cref="AudioLowPassFilter.cutoffFrequency" /> has been set, a single key AnimationCurve will be returned corresponding to the current cutoff frequency.
        ///
        ///Setting the curve will result in the internal AnimationCurve to be rescaled to range from 0..1 for performance reasons. This means calling <see cref="AudioLowPassFilter.customCutoffCurve" /> will return a potentially different curve to what was just set.</remarks>
        public AnimationCurve customCutoffCurve
        {
            get { return GetCustomLowpassLevelCurveCopy(); }
            set { SetCustomLowpassLevelCurveHelper(this, value); }
        }

        // Lowpass cutoff frequency in hz. 10.0 to 22000.0. Default = 5000.0.
        ///<summary>Cutoff frequency in hertz for the low-pass filter.</summary>
        ///<remarks>The cutoff frequency is usually defined by the point on the smooth curve where attenuation hits -3 dB (~0.71) and is also the center frequency where high values of <see cref="AudioLowPassFilter.lowpassResonanceQ" /> causes a boost. Frequencies at and below this value are gradually much less affected by the filter. A lower cutoff frequency attenuates more high-frequency content. A higher cutoff frequency attenuates less high-frequency content.
        ///
        ///The value ranges from 10.0 to 22000.0. The default is 5000.0. Values outside this range are clamped.
        ///
        ///Setting this property replaces any custom cutoff curve with a single constant cutoff frequency. To change the cutoff frequency based on distance, use <see cref="AudioLowPassFilter.customCutoffCurve" />.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///[RequireComponent(typeof(AudioLowPassFilter))]
        ///public class LowPassCutoffExample : MonoBehaviour
        ///{
        ///    const float lowCutoffHz = 800f;
        ///    const float highCutoffHz = 22000f;
        ///
        ///    AudioLowPassFilter lowPass;
        ///
        ///    void Awake()
        ///    {
        ///        lowPass = GetComponent<AudioLowPassFilter>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        // Mathf.Sin(Time.time) ranges from -1 to 1. Add 1 to get 0 to 2, then multiply by 0.5
        ///        // to get an interpolation factor t in the range 0 to 1 that changes smoothly over time.
        ///        float t = (Mathf.Sin(Time.time) + 1f) * 0.5f;
        ///        lowPass.cutoffFrequency = Mathf.Lerp(lowCutoffHz, highCutoffHz, t);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float cutoffFrequency { get; set; }

        ///<summary>Determines how much the filter's self-resonance is dampened.</summary>
        ///<remarks>Higher Lowpass Resonance Q indicates a lower rate of energy loss i.e. the oscillations die out more slowly.
        ///
        ///Lowpass resonance Q value goes from 1.0 to 10.0. Default = 1.0.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///[RequireComponent(typeof(AudioLowPassFilter))]
        ///public class Example : MonoBehaviour
        ///{
        ///    // Moves the Lowpass Resonance Quality Factor from 0 to 10 following a Sinus function
        ///    // Attach this to an audio source with a LowPassFilter to listen to it working.
        ///
        ///    void Update()
        ///    {
        ///        GetComponent<AudioLowPassFilter>().lowpassResonanceQ = (Mathf.Sin(Time.time) * 5 + 5);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float lowpassResonanceQ { get; set; }
    }

    ///<summary>The Audio High Pass Filter passes high frequencies of an AudioSource, and cuts off signals with frequencies lower than the Cutoff Frequency.</summary>
    ///<seealso href="xref:class-AudioHighPassFilter">Audio High Pass Filter</seealso>
    [global::UnityEngine.NativeClass("AudioHighPassFilter", PersistentTypeId = 165)]
    [RequireComponent(typeof(AudioBehaviour))]
    public sealed partial class AudioHighPassFilter : Behaviour
    {
        // Highpass cutoff frequency in hz. 10.0 to 22000.0. Default = 5000.0.
        ///<summary>Highpass cutoff frequency in hz. 10.0 to 22000.0. Default = 5000.0.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///[RequireComponent(typeof(AudioHighPassFilter))]
        ///public class Example : MonoBehaviour
        ///{
        ///    // Moves the cuttoutFrequency from 10 to 22000 following a Sinus function
        ///    // Attach this to an audio source with a HighPassFilter to listen it working.
        ///
        ///    void Update()
        ///    {
        ///        GetComponent<AudioHighPassFilter>().cutoffFrequency = (Mathf.Sin(Time.time) * 11010 + 11000);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float cutoffFrequency { get; set; }

        ///<summary>Determines how much the filter's self-resonance isdampened.</summary>
        ///<remarks>Higher Highpass resonance Q indicates a lower rate of
        ///energy loss i.e. the oscillations die out more slowly.
        ///
        ///Highpass resonance Q value goes from 1.0 to 10.0. Default = 1.0.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///[RequireComponent(typeof(AudioHighPassFilter))]
        ///public class Example : MonoBehaviour
        ///{
        ///    // Moves the Highpass Resonance Quality Factor from 0 to 10 following a Sinus function
        ///    // Attach this to an audio source with a HighPassFilter to listen it working.
        ///
        ///    void Update()
        ///    {
        ///        GetComponent<AudioHighPassFilter>().highpassResonanceQ = (Mathf.Sin(Time.time) * 5 + 5);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float highpassResonanceQ { get; set; }
    }

    ///<summary>The Audio Distortion Filter distorts the sound from an <see cref="AudioSource" /> or sounds reaching the <see cref="AudioListener" />.</summary>
    ///<seealso href="xref:class-AudioDistortionFilter">Audio Distortion Filter</seealso>
    [global::UnityEngine.NativeClass("AudioDistortionFilter", PersistentTypeId = 170)]
    [RequireComponent(typeof(AudioBehaviour))]
    public sealed class AudioDistortionFilter : Behaviour
    {
        // Distortion value. 0.0 to 1.0. Default = 0.5.
        ///<summary>Distortion value. 0.0 to 1.0. Default = 0.5.</summary>
        extern public float distortionLevel { get; set; }
    }

    ///<summary>The Audio Echo Filter repeats a sound after a given Delay, attenuating the repetitions based on the Decay Ratio.</summary>
    ///<seealso href="xref:class-AudioEchoFilter">Audio Echo Filter</seealso>
    [global::UnityEngine.NativeClass("AudioEchoFilter", PersistentTypeId = 168)]
    [RequireComponent(typeof(AudioBehaviour))]
    public sealed class AudioEchoFilter : Behaviour
    {
        // Echo delay in ms. 10 to 5000. Default = 500.
        ///<summary>Echo delay in ms. 10 to 5000. Default = 500.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///[RequireComponent(typeof(AudioEchoFilter))]
        ///public class Example : MonoBehaviour
        ///{
        ///    // Set the delay on the chorus filter to the max working value.
        ///
        ///    void Start()
        ///    {
        ///        GetComponent<AudioEchoFilter>().delay = 5000f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float delay { get; set; }

        // Echo decay per delay. 0 to 1. 1.0 = No decay, 0.0 = total decay (i.e. simple 1 line delay). Default = 0.5.
        ///<summary>Echo decay per delay. 0 to 1. 1.0 = No decay, 0.0 = total decay (i.e. simple 1 line delay). Default = 0.5.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///[RequireComponent(typeof(AudioEchoFilter))]
        ///public class Example : MonoBehaviour
        ///{
        ///    // Set the decayRatio on the chorus filter to total decay
        ///
        ///    void Start()
        ///    {
        ///        GetComponent<AudioEchoFilter>().decayRatio = 0.0f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float decayRatio { get; set; }

        // Volume of original signal to pass to output. 0.0 to 1.0. Default = 1.0.
        ///<summary>Volume of original signal to pass to output. 0.0 to 1.0. Default = 1.0.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///[RequireComponent(typeof(AudioEchoFilter))]
        ///public class Example : MonoBehaviour
        ///{
        ///    // Listen to only Echo (not the original audio source)
        ///    // Set the wet mix to 0 and you will disable the echo.
        ///
        ///    void Start()
        ///    {
        ///        GetComponent<AudioEchoFilter>().wetMix = 1.0f;
        ///        GetComponent<AudioEchoFilter>().dryMix = 0.0f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float dryMix { get; set; }

        // Volume of echo signal to pass to output. 0.0 to 1.0. Default = 1.0.
        ///<summary>Volume of echo signal to pass to output. 0.0 to 1.0. Default = 1.0.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///[RequireComponent(typeof(AudioEchoFilter))]
        ///public class Example : MonoBehaviour
        ///{
        ///    // Mixes both Echo generated sound and the original audio source
        ///    // if you set the wetMix to 0 you will not have Echo sounds.
        ///
        ///    void Start()
        ///    {
        ///        GetComponent<AudioEchoFilter>().wetMix = 1.0f;
        ///        GetComponent<AudioEchoFilter>().dryMix = 1.0f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float wetMix { get; set; }
    }

    ///<summary>The Audio Chorus Filter takes an Audio Clip and processes it creating a chorus effect.</summary>
    ///<remarks>The chorus effect modulates the original sound by a sinusoid low frequency oscillator (LFO). The output sounds like there are multiple sources emitting the same sound with slight variations (resembling a choir).</remarks>
    ///<seealso href="xref:class-AudioChorusFilter">Audio Chorus Filter</seealso>
    [global::UnityEngine.NativeClass("AudioChorusFilter", PersistentTypeId = 166)]
    [RequireComponent(typeof(AudioBehaviour))]
    public sealed partial class AudioChorusFilter : Behaviour
    {
        // Volume of original signal to pass to output. 0.0 to 1.0. Default = 0.5.
        ///<summary>Volume of original signal to pass to output. 0.0 to 1.0. Default = 0.5.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///[RequireComponent(typeof(AudioChorusFilter))]
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GetComponent<AudioChorusFilter>().dryMix = 0f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float dryMix { get; set; }

        // Volume of 1st chorus tap. 0.0 to 1.0. Default = 0.5.
        ///<summary>Volume of 1st chorus tap. 0.0 to 1.0. Default = 0.5.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///[RequireComponent(typeof(AudioChorusFilter))]
        ///public class Example : MonoBehaviour
        ///{
        ///    // Produce random mixes with the Chorus filter.
        ///
        ///    AudioChorusFilter filter;
        ///
        ///    void Start()
        ///    {
        ///        filter = GetComponent<AudioChorusFilter>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        filter.wetMix1 = Mathf.Sin(Time.time) * 0.5f + 0.5f;
        ///        filter.wetMix2 = Mathf.Cos(Time.time) * 0.5f + 0.5f;
        ///        filter.wetMix3 = Random.Range(0.0f, 1.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float wetMix1 { get; set; }

        // Volume of 2nd chorus tap. This tap is 90 degrees out of phase of the first tap. 0.0 to 1.0. Default = 0.5.
        ///<summary>Volume of 2nd chorus tap. This tap is 90 degrees out of phase of the first tap. 0.0 to 1.0. Default = 0.5.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///[RequireComponent(typeof(AudioChorusFilter))]
        ///public class Example : MonoBehaviour
        ///{
        ///    // Produce random mixes with the Chorus filter.
        ///
        ///    AudioChorusFilter filter;
        ///
        ///    void Start()
        ///    {
        ///        filter = GetComponent<AudioChorusFilter>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        filter.wetMix1 = Mathf.Sin(Time.time) * 0.5f + 0.5f;
        ///        filter.wetMix2 = Mathf.Cos(Time.time) * 0.5f + 0.5f;
        ///        filter.wetMix3 = Random.Range(0.0f, 1.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float wetMix2 { get; set; }

        // Volume of 3rd chorus tap. This tap is 90 degrees out of phase of the second tap. 0.0 to 1.0. Default = 0.5.
        ///<summary>Volume of 3rd chorus tap. This tap is 90 degrees out of phase of the second tap. 0.0 to 1.0. Default = 0.5.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///[RequireComponent(typeof(AudioChorusFilter))]
        ///public class Example : MonoBehaviour
        ///{
        ///    // Produce random mixes with the Chorus filter.
        ///
        ///    AudioChorusFilter filter;
        ///
        ///    void Start()
        ///    {
        ///        filter = GetComponent<AudioChorusFilter>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        filter.wetMix1 = Mathf.Sin(Time.time) * 0.5f + 0.5f;
        ///        filter.wetMix2 = Mathf.Cos(Time.time) * 0.5f + 0.5f;
        ///        filter.wetMix3 = Random.Range(0.0f, 1.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float wetMix3 { get; set; }

        // Chorus delay in ms. 0.1 to 100.0. Default = 40.0 ms.
        ///<summary>Chorus delay in ms. 0.1 to 100.0. Default = 40.0 ms.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///[RequireComponent(typeof(AudioChorusFilter))]
        ///public class Example : MonoBehaviour
        ///{
        ///    // Dont use delay on the filter.
        ///    void Start()
        ///    {
        ///        GetComponent<AudioChorusFilter>().delay = 0.1f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float delay { get; set; }

        // Chorus modulation rate in hz. 0.0 to 20.0. Default = 0.8 hz.
        ///<summary>Chorus modulation rate in hz. 0.0 to 20.0. Default = 0.8 hz.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(AudioSource))]
        ///[RequireComponent(typeof(AudioChorusFilter))]
        ///public class Example : MonoBehaviour
        ///{
        ///    // Set the rate on the chorus filter to 15hz.
        ///    void Start()
        ///    {
        ///        GetComponent<AudioChorusFilter>().rate = 15;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float rate { get; set; }

        //  Chorus modulation depth. 0.0 to 1.0. Default = 0.03.
        ///<summary>Chorus modulation depth. 0.0 to 1.0. Default = 0.03.</summary>
        extern public float depth { get; set; }
    }

    ///<summary>The Audio Reverb Filter takes an Audio Clip and distorts it to create a custom reverb effect.</summary>
    ///<seealso href="xref:class-AudioReverbFilter">Audio Reverb Filter</seealso>
    [global::UnityEngine.NativeClass("AudioReverbFilter", PersistentTypeId = 164)]
    [RequireComponent(typeof(AudioBehaviour))]
    public sealed partial class AudioReverbFilter : Behaviour
    {
        ///<summary>Set/Get reverb preset properties.</summary>
        extern public AudioReverbPreset reverbPreset { get; set; }

        ///<summary>Mix level of dry signal in output in millibels (mB). Ranges from -10000.0 to 0.0. Default is 0.</summary>
        extern public float dryLevel { get; set; }

        ///<summary>Room effect level at low frequencies in millibels (mB). Ranges from -10000.0 to 0.0. Default is 0.0.</summary>
        extern public float room { get; set; }

        ///<summary>Room effect high-frequency level re. low frequency level in millibels (mB). Ranges from -10000.0 to 0.0. Default is 0.0.</summary>
        extern public float roomHF { get; set; }

        ///<summary>Reverberation decay time at low-frequencies in seconds. Ranges from 0.1 to 20.0. Default is 1.0.</summary>
        extern public float decayTime { get; set; }

        ///<summary>Decay HF Ratio : High-frequency to low-frequency decay time ratio. Ranges from 0.1 to 2.0. Default is 0.5.</summary>
        extern public float decayHFRatio { get; set; }

        ///<summary>Early reflections level relative to room effect in millibels (mB). Ranges from -10000.0 to 1000.0. Default is -10000.0.</summary>
        extern public float reflectionsLevel { get; set; }

        ///<summary>Late reverberation level relative to room effect in millibels (mB). Ranges from -10000.0 to 2000.0. Default is 0.0.</summary>
        extern public float reflectionsDelay { get; set; }

        ///<summary>Late reverberation level relative to room effect in millibels (mB). Ranges from -10000.0 to 2000.0. Default is 0.0.</summary>
        extern public float reverbLevel { get; set; }

        ///<summary>Late reverberation delay time relative to first reflection in seconds. Ranges from 0.0 to 0.1. Default is 0.04.</summary>
        extern public float reverbDelay { get; set; }

        ///<summary>Reverberation diffusion (echo density) in percent. Ranges from 0.0 to 100.0. Default is 100.0.</summary>
        extern public float diffusion { get; set; }

        ///<summary>Reverberation density (modal density) in percent. Ranges from 0.0 to 100.0. Default is 100.0.</summary>
        extern public float density { get; set; }

        ///<summary>Reference high frequency in hertz (Hz). Ranges from 1000.0 to 20000.0. Default is 5000.0.</summary>
        extern public float hfReference { get; set; }

        ///<summary>Room effect low-frequency level in millibels (mB). Ranges from -10000.0 to 0.0. Default is 0.0.</summary>
        extern public float roomLF { get; set; }

        ///<summary>Reference low-frequency in hertz (Hz). Ranges from 20.0 to 1000.0. Default is 250.0.</summary>
        extern public float lfReference { get; set; }
    }

    ///<summary>Use this class to record to an <see cref="AudioClip" /> using a connected microphone.</summary>
    ///<remarks>You can get a list of connected microphones from the <see cref="devices" /> property and then use the <see cref="Start" /> and <see cref="End" /> functions to start or end a recording session using one of the available devices.
    ///
    ///**Note:** Unity mutes microphone recordings while the application is paused, for example when you pause it manually or when it loses focus and <see cref="Application.runInBackground" /> is disabled. The recording position keeps advancing, but the <see cref="AudioClip" /> receives silence until the application resumes.
    ///
    ///**Note:** On Unity Web, the <c>Microphone</c> class requires user authorization to function. Request authorization via <see cref="Application.RequestUserAuthorization" /> before use.</remarks>
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

        ///<summary>Start Recording with device.</summary>
        ///<remarks>If you pass a null or empty string for the device name then the default microphone is used. You can get a list of available microphone devices from the <see cref="devices" /> property and the range of sample rates supported by a microphone from the <see cref="GetDeviceCaps" /> property.</remarks>
        ///<param name="deviceName">The name of the device.</param>
        ///<param name="loop">Indicates whether the recording should continue recording if lengthSec is reached, and wrap around and record from the beginning of the AudioClip.</param>
        ///<param name="lengthSec">Is the length of the AudioClip produced by the recording.</param>
        ///<param name="frequency">The sample rate of the AudioClip produced by the recording. Use <see cref="AudioSettings.outputSampleRate" /> so the recording matches the project's output sample rate.</param>
        ///<returns>The function returns null if the recording fails to start.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Start recording with built-in Microphone and play the recorded audio right away
        ///    void Start()
        ///    {
        ///        AudioSource audioSource = GetComponent<AudioSource>();
        ///        audioSource.clip = Microphone.Start("Built-in Microphone", true, 10, AudioSettings.outputSampleRate);
        ///        audioSource.Play();
        ///    }
        ///}
        ///]]></code>
        ///</example>
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

        ///<summary>Stops recording.</summary>
        ///<remarks>If you pass a null or empty string for the device name then the default microphone will be used. You can get a list of available microphone devices from the <see cref="devices" /> property.</remarks>
        ///<param name="deviceName">The name of the device.</param>
        static public void End(string deviceName)
        {
            int deviceID = GetMicrophoneDeviceIDFromName(deviceName);
            if (deviceID == -1)
                return;

            EndRecord(deviceID);
        }

        ///<summary>A list of available microphone devices, identified by name.</summary>
        ///<remarks>
        ///  <para>You can use the name with the <see cref="Start" /> and <see cref="End" /> functions to specify which microphone you wish to start/stop recording.
        ///
        ///**Note**: On the Web platform, the list remains empty until the user provides authorization. Request authorization using <see cref="Application.RequestUserAuthorization" />.</para>
        ///  <para />
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Get list of Microphone devices and print the names to the log
        ///    void Start()
        ///    {
        ///        foreach (var device in Microphone.devices)
        ///        {
        ///            Debug.Log("Name: " + device);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Start" />
        ///<seealso cref="End" />
        ///<seealso cref="IsRecording" />
        extern static public string[] devices
        {
            [NativeName("GetRecordDevices")]
            get;
        }

        internal static extern bool isAnyDeviceRecording
        {
            [NativeName("IsAnyRecordDeviceActive")]
            get;
        }

        ///<summary>Query if a device is currently recording.</summary>
        ///<remarks>If you pass a null or empty string for the device name then the default microphone will be used. You can get a list of available microphone devices from the <see cref="devices" /> property.</remarks>
        ///<param name="deviceName">The name of the device.</param>
        static public bool IsRecording(string deviceName)
        {
            int deviceID = GetMicrophoneDeviceIDFromName(deviceName);
            if (deviceID == -1)
                return false;

            return IsRecording(deviceID);
        }

        ///<summary>Get the current recording position in samples.</summary>
        ///<remarks>If you pass a null or empty string for the device name then the default microphone will be used. You can get a list of available microphone devices from the <see cref="devices" /> property.
        ///
        ///You can use this to control latency. For example, to achieve roughly <c>30 ms</c> latency, poll <c>GetPosition</c> until the returned value advances by the number of samples that equal <c>30 ms</c> at the clip's sample rate. Then start playing the audio.</remarks>
        ///<param name="deviceName">The name of the device.</param>
        ///<returns>The current position in the recording buffer, in samples. To convert to seconds, divide by the clip's sample rate (frequency).</returns>
        static public int GetPosition(string deviceName)
        {
            int deviceID = GetMicrophoneDeviceIDFromName(deviceName);
            if (deviceID == -1)
                return 0;

            return GetRecordPosition(deviceID);
        }

        ///<summary>Get the frequency capabilities of a device.</summary>
        ///<remarks>Passing null or an empty string for the device name will select the default device. You can use the <see cref="devices" /> property to get a list of all available microphones.
        ///
        ///When both <c>minFreq</c> and <c>maxFreq</c> parameters return <c>0</c>, the device supports any frequency.
        ///
        ///**Note:** On Android and iOS, the returned values might not reflect the device's actual capabilities. The minimum and maximum frequencies are often reported as <c>16000</c> or <c>0</c> even when recording at other sample rates works. Use the returned values as a hint rather than a strict constraint on these platforms.</remarks>
        ///<param name="deviceName">The name of the device.</param>
        ///<param name="minFreq">Returns the minimum sampling frequency of the device.</param>
        ///<param name="maxFreq">Returns the maximum sampling frequency of the device.</param>
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
