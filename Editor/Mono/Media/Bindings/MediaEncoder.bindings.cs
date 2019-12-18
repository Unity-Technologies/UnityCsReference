// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngineInternal.Video;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("VideoTesting")]
namespace UnityEditor.Media
{
    [NativeHeader("Editor/Mono/Media/Bindings/MediaEncoder.bindings.h")]

    public struct MediaRational
    {
        public static readonly MediaRational Invalid = new MediaRational { numerator = 0, denominator = 0 };

        public MediaRational(int numerator)
        {
            this.numerator = numerator;
            this.denominator = 1;
        }

        public MediaRational(int numerator, int denominator)
        {
            this.numerator = numerator;
            this.denominator = denominator;
            Reduce();
        }

        public void Set(int numerator, int denominator = 1)
        {
            this.numerator = numerator;
            this.denominator = denominator;
            Reduce();
        }

        public static explicit operator double(MediaRational r)
        {
            return (r.denominator == 0) ? 0.0 : (r.numerator / r.denominator);
        }

        public MediaRational inverse
        {
            get { return new MediaRational(denominator, numerator); }
        }

        public bool isValid { get { return denominator != 0; } }
        public bool isZero { get { return isValid && numerator == 0; } }
        public bool isNegative
        { get { return isValid && ((numerator < 0) != (denominator < 0)); } }

        public int numerator;
        public int denominator;

        private void Reduce()
        {
            Internal_MediaRational_Reduce(ref numerator, ref denominator);
        }

        [FreeFunction]
        extern private static void Internal_MediaRational_Reduce(ref int numerator, ref int denominator);
    }

    public struct MediaTime
    {
        public static readonly MediaTime Invalid = new MediaTime { count = 0, rate = MediaRational.Invalid };

        public MediaTime(long seconds) : this(seconds, 1)
        {}

        public MediaTime(long count, uint rateNumerator, uint rateDenominator = 1)
        {
            this.count = count;
            m_Rate = new MediaRational(Convert.ToInt32(rateNumerator), Convert.ToInt32(rateDenominator));
        }

        public static explicit operator double(MediaTime t)
        {
            return t.count * (double)t.rate.inverse;
        }

        public long count { set; get; }
        public MediaRational rate
        {
            set
            {
                if (value.isNegative)
                    throw new ArgumentException("MediaTime expects a positive rate.");
                m_Rate.Set(value.numerator, value.denominator);
            }

            get { return m_Rate; }
        }

        private MediaRational m_Rate;
    }

    public struct VideoTrackAttributes
    {
        public MediaRational      frameRate;
        public uint               width;
        public uint               height;
        public bool               includeAlpha; // For webm only; not applicable to mp4.
        public VideoBitrateMode   bitRateMode;
        internal VideoCodec       codec;
        internal VideoAlphaLayout alphaLayout;
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
        IntPtr m_ThisPtr;

        [Obsolete("Was made public by mistake. Not meant to be used by user code.", true)]
        public IntPtr m_Ptr;

        public MediaEncoder(
            string filePath, VideoTrackAttributes videoAttrs, AudioTrackAttributes[] audioAttrs)
        {
            m_ThisPtr = Create(filePath, new[] {videoAttrs}, audioAttrs);
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
            m_ThisPtr = Create(filePath, new VideoTrackAttributes[0], audioAttrs);
        }

        public MediaEncoder(string filePath, AudioTrackAttributes audioAttrs)
            : this(filePath, new[] {audioAttrs})
        {}

        ~MediaEncoder()
        {
            Dispose();
        }

        unsafe public bool AddFrame(
            int width, int height, int rowBytes, TextureFormat format, NativeArray<byte> data)
        {
            return Internal_AddFrameRaw(
                m_ThisPtr, width, height, rowBytes, format, data.GetUnsafeReadOnlyPtr(), data.Length, MediaTime.Invalid);
        }

        unsafe public bool AddFrame(
            int width, int height, int rowBytes, TextureFormat format, NativeArray<byte> data, MediaTime time)
        {
            return Internal_AddFrameRaw(
                m_ThisPtr, width, height, rowBytes, format, data.GetUnsafeReadOnlyPtr(), data.Length, time);
        }

        public bool AddFrame(Texture2D texture)
        {
            return Internal_AddFrame(m_ThisPtr, texture, MediaTime.Invalid);
        }

        public bool AddFrame(Texture2D texture, MediaTime time)
        {
            return Internal_AddFrame(m_ThisPtr, texture, time);
        }

        unsafe public bool AddSamples(ushort trackIndex, NativeArray<float> interleavedSamples)
        {
            return Internal_AddSamples(
                m_ThisPtr, trackIndex, interleavedSamples.GetUnsafeReadOnlyPtr(),
                interleavedSamples.Length);
        }

        public bool AddSamples(NativeArray<float> interleavedSamples)
        {
            return AddSamples(0, interleavedSamples);
        }

        public void Dispose()
        {
            if (m_ThisPtr != IntPtr.Zero)
            {
                Internal_Release(m_ThisPtr);
                m_ThisPtr = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        private IntPtr Create(
            string filePath, VideoTrackAttributes[] videoAttrs, AudioTrackAttributes[] audioAttrs)
        {
            foreach (var v in videoAttrs)
            {
                var r = v.frameRate;
                if (r.isNegative)
                    throw new ArgumentException($"Negative frame rate not supported: {r.numerator}/{r.denominator}");
            }

            foreach (var a in audioAttrs)
            {
                var r = a.sampleRate;
                if (!r.isValid)
                    throw new ArgumentException($"Invalid sample rate: {r.numerator}/{r.denominator}");
                if (r.isZero)
                    throw new ArgumentException($"Zero sample rate not supported: {r.numerator}/{r.denominator}");
                if (r.isNegative)
                    throw new ArgumentException($"Negative sample rate not supported: {r.numerator}/{r.denominator}");
            }

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
        extern private static bool Internal_AddFrame(IntPtr encoder, Texture2D texture, MediaTime time);

        [FreeFunction]
        unsafe extern private static bool Internal_AddFrameRaw(
            IntPtr encoder, int width, int height, int rowBytes, TextureFormat format, void* buffer,
            int byteCount, MediaTime time);

        [FreeFunction]
        unsafe extern private static bool Internal_AddSamples(
            IntPtr encoder, ushort trackIndex, void* buffer, int sampleCount);
    }
}
