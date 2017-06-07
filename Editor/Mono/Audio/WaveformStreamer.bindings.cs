// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeType(Header = "Editor/Mono/Audio/WaveformStreamer.bindings.h")]
    internal sealed partial class WaveformStreamer
    {
        internal IntPtr m_Data;

        public bool done
        {
            get { return Internal_WaveformStreamerQueryFinishedStatus(m_Data); }
        }
        public void Stop()
        {
            Internal_WaveformStreamerStop(m_Data);
        }

        public WaveformStreamer(AudioClip clip, double start, double duration,
                                int numOutputSamples, Func<WaveformStreamer, float[], int, bool> onNewWaveformData)
        {
            m_Data = Internal_WaveformStreamerCreate(this, clip, start, duration, numOutputSamples, onNewWaveformData);
        }

        private WaveformStreamer(AudioClip clip, double start, double duration,
                                 int numOutputSamples, Func<object, float[], int, bool> onNewWaveformData)
        {
            m_Data = Internal_WaveformStreamerCreateUntyped(this, clip, start, duration, numOutputSamples, onNewWaveformData);
        }

        ~WaveformStreamer()
        {
            if (m_Data != IntPtr.Zero)
                Internal_WaveformStreamerDestroy(m_Data);
        }

        internal static object CreateUntypedWaveformStreamer(AudioClip clip, double start, double duration,
            int numOutputSamples, Func<object, float[], int, bool> onNewWaveformData)
        {
            return new WaveformStreamer(clip, start, duration, numOutputSamples, onNewWaveformData);
        }

        [NativeThrows]
        internal static extern IntPtr Internal_WaveformStreamerCreate(WaveformStreamer instance, [NotNull] AudioClip clip, double start, double duration,
            int numOutputSamples, [NotNull] Func<WaveformStreamer, float[], int, bool> onNewWaveformData);

        internal static extern bool Internal_WaveformStreamerQueryFinishedStatus(IntPtr streamer);

        internal static extern void Internal_WaveformStreamerStop(IntPtr streamer);

        [NativeThrows]
        internal static extern IntPtr Internal_WaveformStreamerCreateUntyped(object instance, [NotNull] AudioClip clip, double start, double duration,
            int numOutputSamples, [NotNull] Func<object, float[], int, bool> onNewWaveformData);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern void Internal_WaveformStreamerDestroy(IntPtr streamer);
    }
}
