// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace UnityEditor
{
    class StreamedAudioClipPreview : WaveformPreview
    {
        static class AudioClipMinMaxOverview
        {
            static Dictionary<AudioClip, float[]> s_Data = new Dictionary<AudioClip, float[]>();
            public static float[] GetOverviewFor(AudioClip clip)
            {
                if (!s_Data.ContainsKey(clip))
                {
                    var path = AssetDatabase.GetAssetPath(clip);
                    if (path == null)
                        return null;
                    var importer = AssetImporter.GetAtPath(path);
                    if (importer == null)
                        return null;

                    s_Data[clip] = AudioUtil.GetMinMaxData(importer as AudioImporter);
                }

                return s_Data[clip];
            }
        }

        struct ClipPreviewDetails
        {
            public float[] preview;
            public int previewSamples;
            public double normalizedDuration;
            public double normalizedStart;
            public double deltaStep;
            public AudioClip clip;
            public int previewPixelsToRender;
            public double localStart;
            public double localLength;
            public bool looping;

            public ClipPreviewDetails(AudioClip clip, bool isLooping, int size, double localStart, double localLength)
            {
                if (size < 2)
                    throw new ArgumentException("Size has to be larger than 1");

                if (localLength <= 0)
                    throw new ArgumentException("length has to be longer than zero", "localLength");

                if (localStart < 0)
                    throw new ArgumentException("localStart has to be positive", "localStart");

                if (clip == null)
                    throw new ArgumentNullException("clip");

                this.clip = clip;

                preview = AudioClipMinMaxOverview.GetOverviewFor(clip);

                if (preview == null)
                    throw new ArgumentException("Clip " + clip + "'s overview preview is null");

                looping = isLooping;

                this.localStart = localStart;
                this.localLength = localLength;

                if (looping)
                {
                    previewPixelsToRender = size;
                }
                else
                {
                    var clampedLength = Math.Min(clip.length - localStart, localLength);
                    previewPixelsToRender = (int)Math.Min(size, size * Math.Max(0, clampedLength / localLength));
                }

                previewSamples = preview.Length / (clip.channels * 2);
                normalizedDuration = localLength / clip.length;
                normalizedStart = localStart / clip.length;
                deltaStep = (previewSamples * normalizedDuration) / (size - 1);
            }

            public bool IsCandidateForStreaming()
            {
                // shortcut, no need to start the stream if the start extends beyond the first clip region and the clip is "hold"
                if (!looping && localStart >= clip.length)
                    return false;

                return deltaStep < 0.5;
            }
        }
        struct Segment
        {
            public WaveformStreamer streamer;
            public int streamingIndexOffset;
            public int textureOffset;
            public int segmentLength;
        }

        class StreamingContext
        {
            public int index;
        }

        Dictionary<WaveformStreamer, StreamingContext> m_Contexts = new Dictionary<WaveformStreamer, StreamingContext>();
        Segment[] m_StreamedSegments;
        AudioClip m_Clip;

        public StreamedAudioClipPreview(AudioClip clip, int initialSize)
            : base(clip, initialSize, clip.channels)
        {
            m_ClearTexture = false;
            m_Clip = clip;
            m_Start = 0;
            m_Length = clip.length;
        }

        protected override void InternalDispose()
        {
            base.InternalDispose();
            KillAndClearStreamers();
            m_StreamedSegments = null;
        }

        protected override void OnModifications(MessageFlags cFlags)
        {
            bool restartStreaming = false;

            if (HasFlag(cFlags, MessageFlags.TextureChanged) || HasFlag(cFlags, MessageFlags.Size) || HasFlag(cFlags, MessageFlags.Length) || HasFlag(cFlags, MessageFlags.Looping))
            {
                KillAndClearStreamers();

                if (length <= 0)
                    return;

                var details = new ClipPreviewDetails(m_Clip, looping, (int)Size.x, start, length);

                UploadPreview(details);

                if (details.IsCandidateForStreaming())
                    restartStreaming = true;
            }

            if (!optimized)
            {
                KillAndClearStreamers();
                restartStreaming = false;
            }
            else if (HasFlag(cFlags, MessageFlags.Optimization) && !restartStreaming)
            {
                // optimization toggled on, need to query whether we should start streaming
                var details = new ClipPreviewDetails(m_Clip, looping, (int)Size.x, start, length);

                if (details.IsCandidateForStreaming())
                    restartStreaming = true;
            }

            if (restartStreaming)
            {
                m_StreamedSegments = CalculateAndStartStreamers(start, length);

                if (m_StreamedSegments != null && m_StreamedSegments.Length > 0)
                {
                    foreach (var r in m_StreamedSegments)
                    {
                        if (!m_Contexts.ContainsKey(r.streamer))
                            m_Contexts.Add(r.streamer, new StreamingContext());
                    }
                }
            }

            base.OnModifications(cFlags);
        }

        void KillAndClearStreamers()
        {
            foreach (var c in m_Contexts)
            {
                c.Key.Stop();
            }

            m_Contexts.Clear();
        }

        Segment[] CalculateAndStartStreamers(double localStart, double localLength)
        {
            Segment[] segments = null;
            var originalStart = localStart;
            // we don't care about the global position, only the locally visible offset into the clip
            localStart %= m_Clip.length;

            var secondsPerPixel = localLength / Size.x;

            if (!looping)
            {
                // holding (= !looping) is a special case handled before everything
                // else, because it's very simple to implement since it defines a
                // section capped by length of the clip

                if (originalStart > m_Clip.length)
                    return null;

                var clampedLength = Math.Min(m_Clip.length - originalStart, localLength);
                var previewPixelsToRender = (int)Math.Min(Size.x, Size.x * Math.Max(0, clampedLength / localLength));

                if (previewPixelsToRender < 1)
                    return null;

                segments = new Segment[1];

                segments[0].streamer = new WaveformStreamer(m_Clip, originalStart, clampedLength, previewPixelsToRender, OnNewWaveformData);
                segments[0].segmentLength = (int)Size.x;
                segments[0].textureOffset = 0;
                segments[0].streamingIndexOffset = 0;

                return segments;
            }

            // epsilon added to discriminate between invisible floating point rounding errors
            // and actual loops (i.e. more than a single pixel larger than then length)
            if (localStart + localLength - secondsPerPixel > m_Clip.length)
            {
                var secondsToPixels = Size.x / localLength;

                // special case, the first part is clipped but at least one full length of the clip is available
                // we can then use one streamer to fill in all visible segments
                if (localLength >= m_Clip.length)
                {
                    var numberOfLoops = localLength / m_Clip.length;
                    var streamer = new WaveformStreamer(m_Clip, 0, m_Clip.length, (int)(Size.x / numberOfLoops), OnNewWaveformData);

                    var currentClipSegmentPart = m_Clip.length - localStart;

                    var localPosition = 0.0;

                    segments = new Segment[Mathf.CeilToInt((float)((localStart + localLength) / m_Clip.length))];

                    for (int i = 0; i < segments.Length; ++i)
                    {
                        var cappedLength = Math.Min(currentClipSegmentPart + localPosition, localLength) - localPosition;
                        segments[i].streamer = streamer;
                        segments[i].segmentLength = (int)(cappedLength * secondsToPixels);
                        segments[i].textureOffset = (int)(localPosition * secondsToPixels);
                        segments[i].streamingIndexOffset = (int)((m_Clip.length - currentClipSegmentPart) * secondsToPixels);

                        localPosition += currentClipSegmentPart;
                        currentClipSegmentPart = m_Clip.length;
                    }
                }
                else
                {
                    // two disjoint regions, since streaming is time-continuous we have to split it up in two portions
                    var firstPart = m_Clip.length - localStart;
                    var secondPart = localLength - firstPart;

                    segments = new Segment[2];

                    segments[0].streamer = new WaveformStreamer(m_Clip, localStart, firstPart, (int)(firstPart * secondsToPixels), OnNewWaveformData);
                    segments[0].segmentLength = (int)(firstPart * secondsToPixels);
                    segments[0].textureOffset = 0;
                    segments[0].streamingIndexOffset = 0;

                    segments[1].streamer = new WaveformStreamer(m_Clip, 0, secondPart, (int)(secondPart * secondsToPixels), OnNewWaveformData);
                    segments[1].segmentLength = (int)(secondPart * secondsToPixels);
                    segments[1].textureOffset = (int)(firstPart * secondsToPixels);
                    segments[1].streamingIndexOffset = 0;
                }
            }
            else
            {
                // handle single visible part of clip, that does not extend beyond the end
                // with a length less than a clip - equaling one streamer.
                segments = new Segment[1];

                segments[0].streamer = new WaveformStreamer(m_Clip, localStart, localLength, (int)Size.x, OnNewWaveformData);
                segments[0].segmentLength = (int)Size.x;
                segments[0].textureOffset = 0;
                segments[0].streamingIndexOffset = 0;
            }

            return segments;
        }

        void UploadPreview(ClipPreviewDetails details)
        {
            var channels = details.clip.channels;
            float[] resampledPreview = new float[(int)(channels * Size.x * 2)];

            if (details.localStart + details.localLength > details.clip.length)
            {
                ResamplePreviewLooped(details, resampledPreview);
            }
            else
                ResamplePreviewConfined(details, resampledPreview);

            SetMMWaveData(0, resampledPreview);
        }

        void ResamplePreviewConfined(ClipPreviewDetails details, float[] resampledPreview)
        {
            var channels = m_Clip.channels;
            var samples = details.previewSamples;
            var delta = details.deltaStep;
            var position = details.normalizedStart * samples;
            var preview = details.preview;

            if (delta > 0.5)
            {
                int oldPosition = (int)position, floorPosition = oldPosition;
                // for each step, there's more than one sample so we do min max on the min max data
                // to avoid aliasing issues
                for (int i = 0; i < details.previewPixelsToRender; ++i)
                {
                    for (int c = 0; c < channels; ++c)
                    {
                        var x = oldPosition;
                        floorPosition = (int)position;

                        float min = preview[2 * x * channels + c * 2];
                        float max = preview[2 * x * channels + c * 2 + 1];

                        while (++x < floorPosition)
                        {
                            // yes, the data contained in the min max audio util overview is actually swapped (maxmin data)
                            min = Mathf.Max(min, preview[2 * x * channels + c * 2]);
                            max = Mathf.Min(max, preview[2 * x * channels + c * 2 + 1]);
                        }

                        resampledPreview[2 * i * channels + c * 2] = max;
                        resampledPreview[2 * i * channels + c * 2 + 1] = min;
                    }

                    position += delta;
                    oldPosition = floorPosition;
                }
            }
            else
            {
                // fractionate interpolation
                for (int i = 0; i < details.previewPixelsToRender; ++i)
                {
                    var x = (int)(position - 1);
                    var x1 = x + 1;
                    float fraction = (float)((position - 1) - x);

                    x = Mathf.Max(0, x);
                    x1 = Mathf.Min(x1, samples - 1);

                    for (int c = 0; c < channels; ++c)
                    {
                        var minCurrent = preview[2 * x * channels + c * 2];
                        var maxCurrent = preview[2 * x * channels + c * 2 + 1];

                        var minNext = preview[2 * x1 * channels + c * 2];
                        var maxNext = preview[2 * x1 * channels + c * 2 + 1];

                        resampledPreview[2 * i * channels + c * 2] = fraction * maxNext + (1 - fraction) * maxCurrent;
                        resampledPreview[2 * i * channels + c * 2 + 1] = fraction * minNext + (1 - fraction) * minCurrent;
                    }

                    position += delta;
                }
            }
        }

        void ResamplePreviewLooped(ClipPreviewDetails details, float[] resampledPreview)
        {
            var previewSize = details.preview.Length;
            var channels = m_Clip.channels;
            var samples = details.previewSamples;

            var delta = details.deltaStep;
            var position = details.normalizedStart * samples;
            var preview = details.preview;

            if (delta > 0.5)
            {
                int oldPosition = (int)position, floorPosition = oldPosition;
                // for each step, there's more than one sample so we do min max on the min max data
                // to avoid aliasing issues
                for (int i = 0; i < details.previewPixelsToRender; ++i)
                {
                    for (int c = 0; c < channels; ++c)
                    {
                        var x = oldPosition;
                        floorPosition = (int)position;

                        var wrappedIndex = (2 * x * channels + c * 2) % previewSize;

                        float min = preview[wrappedIndex];
                        float max = preview[wrappedIndex + 1];

                        while (++x < floorPosition)
                        {
                            wrappedIndex = (2 * x * channels + c * 2) % previewSize;
                            // yes, the data contained in the min max audio util overview is actually swapped (maxmin data)
                            min = Mathf.Max(min, preview[wrappedIndex]);
                            max = Mathf.Min(max, preview[wrappedIndex + 1]);
                        }

                        resampledPreview[2 * i * channels + c * 2] = max;
                        resampledPreview[2 * i * channels + c * 2 + 1] = min;
                    }

                    position += delta;
                    oldPosition = floorPosition;
                }
            }
            else
            {
                // fractionate interpolation
                for (int i = 0; i < details.previewPixelsToRender; ++i)
                {
                    var x = (int)(position - 1);
                    var x1 = x + 1;
                    float fraction = (float)((position - 1) - x);

                    for (int c = 0; c < channels; ++c)
                    {
                        var xWrapped = (2 * x * channels + c * 2) % previewSize;

                        var minCurrent = preview[xWrapped];
                        var maxCurrent = preview[xWrapped + 1];

                        var x1Wrapped = (2 * x1 * channels + c * 2) % previewSize;

                        var minNext = preview[x1Wrapped];
                        var maxNext = preview[x1Wrapped + 1];

                        resampledPreview[2 * i * channels + c * 2] = fraction * maxNext + (1 - fraction) * maxCurrent;
                        resampledPreview[2 * i * channels + c * 2 + 1] = fraction * minNext + (1 - fraction) * minCurrent;
                    }

                    position += delta;
                }
            }
        }

        bool OnNewWaveformData(WaveformStreamer streamer, float[] data, int remaining)
        {
            StreamingContext c = m_Contexts[streamer];

            int pixelPos = c.index / m_Clip.channels;

            for (var i = 0; i < m_StreamedSegments.Length; i++)
            {
                if (m_StreamedSegments[i].streamer == streamer && pixelPos >= m_StreamedSegments[i].streamingIndexOffset && m_StreamedSegments[i].segmentLength > (pixelPos - m_StreamedSegments[i].streamingIndexOffset))
                {
                    SetMMWaveData((m_StreamedSegments[i].textureOffset - m_StreamedSegments[i].streamingIndexOffset) * m_Clip.channels + c.index, data);
                }
            }

            c.index += data.Length / 2;

            return remaining != 0;
        }
    }
}
