// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine
{
    ///<summary>Records the main audio output of the application.</summary>
    ///<remarks>Use this class for offline capture, such as rendering a cutscene or a trailer to a video file. Recording mutes the live output, so it's not suitable for capturing audio during gameplay.
    ///
    ///While recording, Unity moves audio rendering from the audio thread onto the main thread, which can increase frame times.
    ///
    ///Call <see cref="AudioRenderer.Start" /> to enter recording mode. While recording, Unity outputs silence to the audio device and delivers the mixed samples through <see cref="AudioRenderer.Render" /> instead. Call <see cref="AudioRenderer.Stop" /> to leave recording mode and make audio audible again.
    ///
    ///Call <see cref="AudioRenderer.Render" /> once per frame while recording. Use <see cref="AudioRenderer.GetSampleCountForCaptureFrame" /> to get the number of accumulated sample frames since the previous call. The buffer contains interleaved samples, so its length must equal that count multiplied by the speaker mode's channel count.
    ///
    ///**Web Platform:** This class isn't supported in Web player builds. The API works as normal in the Editor, including when the active build target is the Web Platform.</remarks>
    ///<example>
    ///  <code><![CDATA[using Unity.Collections;
    ///using UnityEngine;
    ///
    ///public class AudioCaptureExample : MonoBehaviour
    ///{
    ///    int m_ChannelCount;
    ///
    ///    void OnEnable()
    ///    {
    ///        AudioConfiguration config = AudioSettings.GetConfiguration();
    ///        AudioSpeakerMode speakerMode = config.speakerMode;
    ///        m_ChannelCount = speakerMode.ChannelCount();
    ///
    ///        AudioRenderer.Start();
    ///    }
    ///
    ///    void Update()
    ///    {
    ///        int sampleFrames = AudioRenderer.GetSampleCountForCaptureFrame();
    ///        int length = sampleFrames * m_ChannelCount;
    ///
    ///        NativeArray<float> buffer = new NativeArray<float>(
    ///            length, Allocator.Temp);
    ///
    ///        // Render runs on every frame, including frames that report no
    ///        // samples, because it advances the count for the next frame.
    ///        AudioRenderer.Render(buffer);
    ///
    ///        // Write the samples to a file or an encoder here.
    ///
    ///        buffer.Dispose();
    ///    }
    ///
    ///    void OnDisable()
    ///    {
    ///        AudioRenderer.Stop();
    ///    }
    ///}]]></code>
    ///</example>
    ///<seealso cref="AudioSettings" />
    ///<seealso cref="AudioSpeakerMode" />
    [NativeHeader("Modules/Audio/Public/ScriptBindings/AudioRenderer.bindings.h")]
    public class AudioRenderer
    {
        ///<summary>Enters audio recording mode. After this Unity will output silence until <see cref="AudioRenderer.Stop" /> is called.</summary>
        ///<returns>Returns `true` if the engine was switched into output recording mode. It returns `false` if the engine is already recording, or if the platform doesn't support this API.</returns>
        ///<remarks>While recording, Unity performs audio rendering on the main thread instead of the audio thread, which can increase frame times.</remarks>
        public static bool Start()
        {
            return Internal_AudioRenderer_Start();
        }

        ///<summary>Exits audio recording mode. After this audio output will be audible again.</summary>
        ///<returns>Returns `true` if the engine was recording when this method is called. Returns `false` if the engine wasn't recording, in which case Unity logs an error.</returns>
        ///<remarks>This returns audio rendering to the audio thread, which <see cref="AudioRenderer.Start" /> moves onto the main thread.</remarks>
        public static bool Stop()
        {
            return Internal_AudioRenderer_Stop();
        }

        ///<summary>Returns the number of sample frames available since the last time <see cref="AudioRenderer.Render" /> was called. This count depends on the frame capture rate.</summary>
        ///<returns>Number of sample frames available since the last recorded frame, or 0 if the engine isn't in recording mode.</returns>
        ///<remarks>A sample frame holds one sample for each output channel. Multiply this count by the channel count of the current speaker mode to get the length of the buffer that <see cref="AudioRenderer.Render" /> fills.
        ///
        ///The count follows <see cref="Time.captureDeltaTime" /> when that property is non-zero, and <see cref="Time.deltaTime" /> otherwise.
        ///
        ///Call <see cref="AudioRenderer.Render" /> on every frame, including the frames that report 0. Unity discards the audio for any frame you skip, and only <see cref="AudioRenderer.Render" /> carries the leftover samples forward to the next frame. </remarks>
        ///<example>
        ///  <code><![CDATA[using Unity.Collections;
        ///using UnityEngine;
        ///
        ///public class SampleCountExample : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        // Assumes AudioRenderer.Start has already been called.
        ///        int sampleFrames = AudioRenderer.GetSampleCountForCaptureFrame();
        ///
        ///        AudioConfiguration config = AudioSettings.GetConfiguration();
        ///        AudioSpeakerMode speakerMode = config.speakerMode;
        ///        int channels = speakerMode.ChannelCount();
        ///
        ///        NativeArray<float> buffer = new NativeArray<float>(
        ///            sampleFrames * channels, Allocator.Temp);
        ///
        ///        // Render runs even when the count is zero, so that the next
        ///        // frame reports the samples accumulated so far.
        ///        AudioRenderer.Render(buffer);
        ///        buffer.Dispose();
        ///    }
        ///}]]></code>
        ///</example>
        public static int GetSampleCountForCaptureFrame()
        {
            return Internal_AudioRenderer_GetSampleCountForCaptureFrame();
        }

        // We should consider making this delegate-based in order to provide information like channel count and format. Also the term "sink" is quite audio-domain specific.
        unsafe internal static bool AddMixerGroupSink(AudioMixerGroup mixerGroup, NativeArray<float> buffer, bool excludeFromMix)
        {
            return Internal_AudioRenderer_AddMixerGroupSink(mixerGroup, buffer.GetUnsafePtr(), buffer.Length, excludeFromMix);
        }

        ///<summary>Writes the audio rendered since the previous call into the supplied buffer.</summary>
        ///<param name="buffer">The buffer to write the sample data to. Its length must be the value that <see cref="AudioRenderer.GetSampleCountForCaptureFrame" /> returns, multiplied by the channel count of the current speaker mode.</param>
        ///<returns>Returns `true` if the recording succeeded and `false` if the engine isn't in recording mode.</returns>
        ///<remarks>Call this method once per frame while the engine is in recording mode. Unity writes interleaved samples for every output channel into the buffer, so the buffer length must match the sample frame count that <see cref="AudioRenderer.GetSampleCountForCaptureFrame" /> reports, multiplied by the channel count of the current speaker mode.
        ///
        ///An oversized buffer takes audio from ahead of the current frame, so the recording drifts out of sync with the visuals.
        ///
        ///Call this method on every frame, including the frames where <see cref="AudioRenderer.GetSampleCountForCaptureFrame" /> returns 0, otherwise the capture stops producing samples. Pass a zero-length buffer on those frames. Calling this method outside recording mode logs an error and returns false.</remarks>
        ///<example>
        ///  <code><![CDATA[using Unity.Collections;
        ///using UnityEngine;
        ///
        ///public class RenderExample : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        // Assumes AudioRenderer.Start has already been called.
        ///        int sampleFrames = AudioRenderer.GetSampleCountForCaptureFrame();
        ///        AudioConfiguration config = AudioSettings.GetConfiguration();
        ///        AudioSpeakerMode speakerMode = config.speakerMode;
        ///        int channels = speakerMode.ChannelCount();
        ///
        ///        int length = sampleFrames * channels;
        ///        NativeArray<float> buffer = new NativeArray<float>(
        ///            length, Allocator.Temp);
        ///
        ///        // A zero-length buffer is expected on frames that produce
        ///        // fewer samples than one DSP buffer.
        ///        if (AudioRenderer.Render(buffer) && sampleFrames > 0)
        ///            Debug.Log("Rendered " + sampleFrames + " sample frames.");
        ///
        ///        buffer.Dispose();
        ///    }
        ///}]]></code>
        ///</example>
        unsafe public static bool Render(NativeArray<float> buffer)
        {
            return Internal_AudioRenderer_Render(buffer.GetUnsafePtr(), buffer.Length);
        }

        internal static extern bool Internal_AudioRenderer_Start();
        internal static extern bool Internal_AudioRenderer_Stop();
        internal static extern int  Internal_AudioRenderer_GetSampleCountForCaptureFrame();
        unsafe internal static extern bool Internal_AudioRenderer_AddMixerGroupSink(AudioMixerGroup mixerGroup, void* ptr, int length, bool excludeFromMix);
        unsafe internal static extern bool Internal_AudioRenderer_Render(void* ptr, int length);
    }
}
