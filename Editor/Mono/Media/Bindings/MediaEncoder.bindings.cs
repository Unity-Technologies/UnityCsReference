// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Collections;
using UnityEditor;
using Object = UnityEngine.Object;

namespace UnityEditor.Media
{
    [NativeHeader("Editor/Mono/Media/Bindings/MediaEncoder.bindings.h")]

    public struct MediaRational
    {
        public MediaRational(int num)
        {
            numerator = num;
            denominator = 1;
        }

        public int numerator;
        public int denominator;
    }

    public struct VideoTrackAttributes
    {
        public MediaRational frameRate;
        public uint          width;
        public uint          height;
        public bool          includeAlpha; // For webm only; not applicable to mp4.
    }

    public struct AudioTrackAttributes
    {
        public MediaRational sampleRate;
        public ushort        channelCount;
        public string        language;
        //Future work:
        //public string      format;   // E.g.: "Stereo", "5.1", "Ambisonic 1st order", ...
        //public string      layout[]; // E.g.: ["Left", "Right", "Center"]
    }

    public class MediaEncoder : IDisposable
    {
        public IntPtr m_Ptr;

        public MediaEncoder(
            string filePath, VideoTrackAttributes videoAttrs, AudioTrackAttributes[] audioAttrs)
        {
            m_Ptr = Create(filePath, new[] {videoAttrs}, audioAttrs);
        }

        public MediaEncoder(
            string filePath, VideoTrackAttributes videoAttrs, AudioTrackAttributes audioAttrs)
            : this(filePath, videoAttrs, new[] {audioAttrs})
        {}

        public MediaEncoder(string filePath, VideoTrackAttributes videoAttrs)
            : this(filePath, videoAttrs, new AudioTrackAttributes[0])
        {}

        public MediaEncoder(string filePath, AudioTrackAttributes[] audioAttrs)
        {
            m_Ptr = Create(filePath, new VideoTrackAttributes[0], audioAttrs);
        }

        public MediaEncoder(string filePath, AudioTrackAttributes audioAttrs)
            : this(filePath, new[] {audioAttrs})
        {}

        ~MediaEncoder()
        {
            Dispose();
        }

        public bool AddFrame(Texture2D texture)
        {
            return Internal_AddFrame(m_Ptr, texture);
        }

        public bool AddSamples(ushort trackIndex, NativeArray<float> interleavedSamples)
        {
            return Internal_AddSamples(
                m_Ptr, trackIndex, interleavedSamples.UnsafeReadOnlyPtr,
                interleavedSamples.Length);
        }

        public bool AddSamples(NativeArray<float> interleavedSamples)
        {
            return AddSamples(0, interleavedSamples);
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Release(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        private IntPtr Create(
            string filePath, VideoTrackAttributes[] videoAttrs, AudioTrackAttributes[] audioAttrs)
        {
            IntPtr ptr = Internal_Create(filePath, videoAttrs, audioAttrs);
            if (ptr == IntPtr.Zero)
                throw new InvalidOperationException(
                    "MediaEncoder: Output file creation failed for " + filePath);
            return ptr;
        }

        [FreeFunction]
        extern private static IntPtr Internal_Create(
            string filePath, VideoTrackAttributes[] videoAttrs, AudioTrackAttributes[] audioAttrs);

        [FreeFunction]
        extern private static void Internal_Release(IntPtr encoder);

        [FreeFunction]
        extern private static bool Internal_AddFrame(IntPtr encoder, Texture2D texture);

        [FreeFunction]
        extern private static bool Internal_AddSamples(
            IntPtr encoder, ushort trackIndex, IntPtr buffer, int sampleCount);
    }
}
