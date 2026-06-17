// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class AudioClipAnalyzer : AudioClipModuleAnalyzer
    {
        internal const string PAA4000 = nameof(PAA4000);    // Long AudioClips which aren’t set to streaming
        internal const string PAA4001 = nameof(PAA4001);    // Very small ACs (uncompressed size <200KB) that ARE set to streaming. These should probably be Decompress on Load
        internal const string PAA4002 = nameof(PAA4002);    // Stereo clips not forced to Mono on mobile platforms
        internal const string PAA4003 = nameof(PAA4003);    // Stereo clips not forced to Mono if they’re not streaming audio (only non-diagetic music should be stereo, really)
        internal const string PAA4004 = nameof(PAA4004);    // Decompress on Load used with long clips
        internal const string PAA4005 = nameof(PAA4005);    // Compressed In Memory used with compression formats that are not trivial to decompress (e.g. everything other than PCM or ADPCM)
        internal const string PAA4006 = nameof(PAA4006);    // Large compressed samples on mobile: Decrease quality or downsample
        internal const string PAA4007 = nameof(PAA4007);    // Bitrates > 48kHz
        internal const string PAA4008 = nameof(PAA4008);    // Preload Audio Data ticked (increases load times and is only needed for audio that must start IMMEDIATELY upon scene load)
        internal const string PAA4009 = nameof(PAA4009);    // If Load In Background isn’t enabled on ACs over (TUNEABLE) size/length (if it’s not ticked, loading will block the main thread)
        internal const string PAA4010 = nameof(PAA4010);    // If MP3 is used. Vorbis is better
        internal const string PAA4011 = nameof(PAA4011);    // Source assets that aren’t .WAV or .AIFF. Other formats (.MP3, .OGG, etc.) are lossy

        internal static readonly Descriptor k_AudioLongClipDoesNotStreamDescriptor = new Descriptor(
            PAA4000,
            "Audio: Long AudioClip is not set to Streaming",
            Areas.Memory,
            "The AudioClip has a runtime memory footprint larger than the streaming buffer size of 200KB, but its <b>Load Type</b> is not set to <b>Streaming</b>. Storing the whole clip in memory rather than streaming it may be an inefficient use of memory.",
            "Consider setting <b>Load Type</b> to <b>Streaming</b> in the AudioClip Import Settings."
        )
        {
            MessageFormat = "AudioClip '{0}' Load Type is not set to Streaming",
            Fixer = (issue, analysisParams) =>
            {
                var audioImporter = AssetImporter.GetAtPath(issue.RelativePath) as AudioImporter;
                if (audioImporter != null)
                {
                    var sampleSettings = audioImporter.GetOverrideSampleSettings(analysisParams.PlatformAsString);
                    sampleSettings.loadType = AudioClipLoadType.Streaming;
                    audioImporter.SetOverrideSampleSettings(analysisParams.PlatformAsString, sampleSettings);
                    audioImporter.SaveAndReimport();
                }
            }
        };

        internal static readonly Descriptor k_AudioShortClipStreamsDescriptor = new Descriptor(
            PAA4001,
            "Audio: Short AudioClip is set to streaming",
            Areas.Memory,
            "The AudioClip has a runtime memory footprint smaller than the streaming buffer size of 200KB, but its <b>Load Type</b> is set to <b>Streaming</b>. Requiring a streaming buffer for this clip is an inefficient use of memory.",
            "Set <b>Load Type</b> to <b>Compressed in Memory</b> or <b>Decompress On Load</b> in the AudioClip Import Settings."
        )
        {
            MessageFormat = "AudioClip '{0}' Load Type is set to Streaming",
        };

        internal static readonly Descriptor k_AudioStereoClipsOnMobileDescriptor = new Descriptor(
            PAA4002,
            "Audio: AudioClip is stereo",
            Areas.Memory,
            "The audio source asset is in stereo, and <b>Force To Mono</b> is not enabled in the AudioClip Import Settings. Stereo clips are generally not needed on mobile platforms, and have double the memory footprint of mono clips.",
            "Tick the <b>Force To Mono</b> checkbox in the AudioClip Import Settings."
        )
        {
            MessageFormat = "AudioClip '{0}' is stereo",
            Fixer = (issue, analysisParams) =>
            {
                var audioImporter = AssetImporter.GetAtPath(issue.RelativePath) as AudioImporter;
                if (audioImporter != null)
                {
                    audioImporter.forceToMono = true;
                    audioImporter.SaveAndReimport();
                }
            },
            Platforms = new SerializableEnum<BuildTarget>[] { BuildTarget.Android, BuildTarget.iOS}
        };

        internal static readonly Descriptor k_AudioStereoClipWhichIsNotStreamingDescriptor = new Descriptor(
            PAA4003,
            "Audio: AudioClip is stereo",
            Areas.Memory | Areas.Quality,
            "The audio source asset is in stereo, <b>Force To Mono</b> is not enabled in the AudioClip Import Settings, and the <b>Load Type</b> is not <b>Streaming</b>, which implies the AudioClip may be used as a diagetic positional sound effect. Positional effects should be mono; only non-diagetic music and effects should be stereo.",
            "Tick the <b>Force To Mono</b> checkbox in the AudioClip Import Settings."
        )
        {
            MessageFormat = "AudioClip '{0}' is stereo",
            Fixer = (issue, analysisParams) =>
            {
                var audioImporter = AssetImporter.GetAtPath(issue.RelativePath) as AudioImporter;
                if (audioImporter != null)
                {
                    audioImporter.forceToMono = true;
                    audioImporter.SaveAndReimport();
                }
            }
        };

        internal static readonly Descriptor k_AudioLongDecompressedClipDescriptor = new Descriptor(
            PAA4004,
            "Audio: AudioClip is set to Decompress On Load",
            Areas.Memory | Areas.LoadTime,
            "The AudioClip is long, and its <b>Load Type</b> is set to <b>Decompress On Load</b>. The clip's memory footprint may be excessive, and decompression may impact load times.",
            "Consider setting the <b>Load Type</b> to <b>Compressed In Memory</b> or <b>Streaming</b>. If you have concerns about the CPU cost of decompressing <b>Compressed In Memory</b> clips for playback, consider a format which is fast to decompress, such as <b>ADPCM</b>."
        )
        {
            MessageFormat = "AudioClip '{0}' is set to Decompress On Load",
        };

        internal static readonly Descriptor k_AudioCompressedInMemoryDescriptor = new Descriptor(
            PAA4005,
            "Audio: Compressed AudioClip is Compressed In Memory",
            Areas.CPU,
            "The AudioClip's <b>Load Type</b> is set to <b>Compressed In Memory</b> but the clip is imported with a format that is not trivial to decompress. Decompression will be performed every time the clip is played, and may impact CPU performance.",
            "If runtime performance is impacted, either set the <b>Load Type</b> to <b>Decompress On Load</b> or set the <b>Compression Format</b> to <b>ADPCM</b>, which is fast to decompress."
        )
        {
            MessageFormat = "AudioClip '{0}' is Compressed In Memory",
        };

        // Large compressed samples on mobile: Decrease quality or downsample
        internal static readonly Descriptor k_AudioLargeCompressedMobileDescriptor = new Descriptor(
            PAA4006,
            "Audio: Compressed clip could be optimized for mobile",
            Areas.Memory | Areas.BuildSize,
            "The AudioClip has a large file size despite using compression. Mobile speakers and headphones are generally of mediocre quality and cannot discernibly reproduce very high-fidelity sounds, so there may be an opportunity to optimize the clip's file size and memory footprint.",
            "Reduce the <b>Quality</b> slider as far as possible without introducing audible artefacts. Alternatively, try setting the <b>Sample Rate Setting</b> to <b>Override</b> and the <b>Sample Rate</b> to a suitable value. <b>22050</b> Hz or is fine for most sounds, and <b>44100</b> Hz (CD Quality) can be useful for prominent sounds or music if they include high frequencies."
        )
        {
            MessageFormat = "AudioClip '{0}' Compressed clip could be optimized for mobile",
            Platforms = new SerializableEnum<BuildTarget>[] {BuildTarget.Android, BuildTarget.iOS}
        };

        internal static readonly Descriptor k_Audio48kHzDescriptor = new Descriptor(
            PAA4007,
            "Audio: Sample Rate is over 48 kHz",
            Areas.Memory | Areas.BuildSize | Areas.LoadTime,
            "The AudioClip's source sample rate is higher than 48 kHz, and the <b>Sample Rate Setting</b> does not override it. Most Blu-Rays are at 48kHz, and higher sample rates are generally only used during the recording process or for scientific data. If compression is applied during the import process the sample rate gets capped at 48kHz. If compression isn't applied, the runtime memory footprint for this clip will be excessive. In both cases, the source file size is excessive.",
            "Set the <b>Sample Rate Setting</b> to <b>Override</b> and the <b>Sample Rate</b> to <b>48000</b> Hz or lower."
        )
        {
            MessageFormat = "AudioClip '{0}' Sample Rate is over 48kHz",
            Fixer = (issue, analysisParams) =>
            {
                var audioImporter = AssetImporter.GetAtPath(issue.RelativePath) as AudioImporter;
                if (audioImporter != null)
                {
                    var sampleSettings = audioImporter.GetOverrideSampleSettings(analysisParams.PlatformAsString);
                    sampleSettings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
                    sampleSettings.sampleRateOverride = 48000;
                    audioImporter.SetOverrideSampleSettings(analysisParams.PlatformAsString, sampleSettings);
                    audioImporter.SaveAndReimport();
                }
            }
        };

        internal static readonly Descriptor k_AudioPreloadDescriptor = new Descriptor(
            PAA4008,
            "Audio: Preload Audio Data is enabled",
            Areas.LoadTime,
            "The <b>Preload Audio Data</b> checkbox is ticked for this AudioClip. This forces scene/prefab loading to wait synchronously until the AudioClip has completed loading before continuing running, and can impact scene load/initialization times.",
            "Consider un-ticking the <b>Preload Audio Data</b> checkbox. Audio preloading is only required when the AudioClip must play at the exact moment the scene begins simulating, or if the audio timing must be very precise the first time it is played."
        )
        {
            MessageFormat = "AudioClip '{0}' is set to Preload Audio Data",
            Fixer = (issue, analysisParams) =>
            {
                var audioImporter = AssetImporter.GetAtPath(issue.RelativePath) as AudioImporter;
                if (audioImporter != null)
                {
                    var sampleSettings = audioImporter.GetOverrideSampleSettings(analysisParams.PlatformAsString);
                    sampleSettings.preloadAudioData = false;
                    audioImporter.SetOverrideSampleSettings(analysisParams.PlatformAsString, sampleSettings);
                    audioImporter.SaveAndReimport();
                }
            }
        };

        internal static readonly Descriptor k_AudioLoadInBackgroundDisabledDescriptor = new Descriptor(
            PAA4009,
            "Audio: Load In Background is not enabled",
            Areas.CPU | Areas.LoadTime,
            "This AudioClip is large, and the <b>Load In Background</b> checkbox is not ticked. Loading will be performed synchronously and will block the main thread. This may impact load times or create CPU spikes, depending on when the clip is loaded.",
            "Tick the <b>Load In Background</b> checkbox in the AudioClip Import Settings."
        )
        {
            MessageFormat = "AudioClip '{0}' Load In Background is not enabled",
            Fixer = (issue, analysisParams) =>
            {
                var audioImporter = AssetImporter.GetAtPath(issue.RelativePath) as AudioImporter;
                if (audioImporter != null)
                {
                    audioImporter.loadInBackground = true;
                    audioImporter.SaveAndReimport();
                }
            }
        };

        internal static readonly Descriptor k_AudioMP3Descriptor = new Descriptor(
            PAA4010,
            "Audio: Compression Format is MP3",
            Areas.Quality,
            "The AudioClip's <b>Compression Format</b> is set to <b>MP3</b>. MP3 is an old compression format which has been surpassed in efficiency and quality by newer formats such as Vorbis.",
            "Set the <b>Compression Format</b> to <b>Vorbis</b> in the AudioClip's Import Settings."
        )
        {
            MessageFormat = "AudioClip '{0}' Compression Format is MP3",
            Fixer = (issue, analysisParams) =>
            {
                var audioImporter = AssetImporter.GetAtPath(issue.RelativePath) as AudioImporter;
                if (audioImporter != null)
                {
                    var sampleSettings = audioImporter.GetOverrideSampleSettings(analysisParams.PlatformAsString);
                    sampleSettings.compressionFormat = AudioCompressionFormat.Vorbis;
                    audioImporter.SetOverrideSampleSettings(analysisParams.PlatformAsString, sampleSettings);
                    audioImporter.SaveAndReimport();
                }
            }
        };

        // Source assets that aren’t .WAV or .AIFF. Other formats (.MP3, .OGG, etc.) are lossy
        internal static readonly Descriptor k_AudioCompressedSourceAssetDescriptor = new Descriptor(
            PAA4011,
            "Audio: Source asset is in a lossy compressed format",
            Areas.Quality,
            "The file format used by the source asset for the AudioClip uses a lossy compression format. The Asset Import process decompresses the audio data and recompresses it in the chosen runtime format. This may result in a further loss of sound quality.",
            "Wherever possible, select a lossless file format such as .WAV or .AIFF for source assets."
        )
        {
            MessageFormat = "AudioClip '{0}' source asset is in a lossy compressed format",
        };

#pragma warning disable CS0649
        [DiagnosticParameter("StreamingClipThresholdBytes", "Streaming Audio Clip Threshold (Bytes)", "Issues will be raised if (a) clips larger than this are not streaming and (b) clips smaller than this are streaming.  'Runtime Size (Estimate)' is used for this comparison.", 1 * (64000 + (int)(1.6 * 48000 * 2)) + 694)]
        int m_StreamingClipThresholdBytes;

        [DiagnosticParameter("LongDecompressedClipThresholdBytes","Decompressed Audio Clip Threshold (Bytes)", "Maximum 'Runtime Size (Estimate)' of a decompressed audio clip (in Bytes) before an Issue is created.", 200 * 1024)]
        int m_LongDecompressedClipThresholdBytes;

        [DiagnosticParameter("LongCompressedMobileClipThresholdBytes","Compressed Audio Clip Threshold (Bytes)", "Maximum 'Imported File Size' of a compressed audio clip (in Bytes) before an Issue is created (on mobile devices).", 200 * 1024)]
        int m_LongCompressedMobileClipThresholdBytes;

        [DiagnosticParameter("LoadInBackGroundClipSizeThresholdBytes","Load In Background Audio Clip Threshold (Bytes)", "Maximum 'Imported File Size' of a 'Load In Background' audio clip (in Bytes) before an Issue is created.", 200 * 1024)]
        int m_LoadInBackGroundClipSizeThresholdBytes;
#pragma warning restore CS0649

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_AudioLongClipDoesNotStreamDescriptor);
            registerDescriptor(k_AudioShortClipStreamsDescriptor);
            registerDescriptor(k_AudioStereoClipsOnMobileDescriptor);
            registerDescriptor(k_AudioStereoClipWhichIsNotStreamingDescriptor);
            registerDescriptor(k_AudioLongDecompressedClipDescriptor);
            registerDescriptor(k_AudioCompressedInMemoryDescriptor);
            registerDescriptor(k_AudioLargeCompressedMobileDescriptor);
            registerDescriptor(k_Audio48kHzDescriptor);
            registerDescriptor(k_AudioPreloadDescriptor);
            registerDescriptor(k_AudioLoadInBackgroundDisabledDescriptor);
            registerDescriptor(k_AudioMP3Descriptor);
            registerDescriptor(k_AudioCompressedSourceAssetDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(AudioClipAnalysisContext context)
        {
            var clipName = context.Name;
            var audioImporter = context.Importer;
            var assetPath = audioImporter.assetPath;
            var audioClip = context.AudioClip;
            var sampleSettings = context.SampleSettings;

            bool isMobileTarget = (context.Params.Platform == BuildTarget.Android ||
                context.Params.Platform == BuildTarget.iOS ||
                context.Params.Platform == BuildTarget.Switch);

            bool isStreaming = sampleSettings.loadType == AudioClipLoadType.Streaming;

            // Size (bytes) of the decompressed PCM data for the clip.
            int decompressedClipSize = audioClip.samples * audioClip.channels * sizeof(float);

            var sourceFileExtension = System.IO.Path.GetExtension(assetPath).ToUpper() ?? string.Empty;
            if (sourceFileExtension.StartsWith("."))
                sourceFileExtension = sourceFileExtension.Substring(1);

            var preloadAudioData = sampleSettings.preloadAudioData;
            if (context.RuntimeSize > m_StreamingClipThresholdBytes && !isStreaming)
            {
                yield return context.CreateIssue(
                    IssueCategory.AssetIssue, k_AudioLongClipDoesNotStreamDescriptor.Id, clipName)
                    .WithLocation(assetPath);
            }

            if (decompressedClipSize < m_StreamingClipThresholdBytes && isStreaming)
            {
                yield return context.CreateIssue(
                    IssueCategory.AssetIssue, k_AudioShortClipStreamsDescriptor.Id, clipName)
                    .WithLocation(assetPath);
            }

            if (audioClip.channels > 1 && context.Importer.forceToMono == false)
            {
                if (isMobileTarget)
                {
                    yield return context.CreateIssue(
                        IssueCategory.AssetIssue, k_AudioStereoClipsOnMobileDescriptor.Id, clipName)
                        .WithLocation(assetPath);
                }
                else if (!isStreaming)
                {
                    yield return context.CreateIssue(
                        IssueCategory.AssetIssue, k_AudioStereoClipWhichIsNotStreamingDescriptor.Id, clipName)
                        .WithLocation(assetPath);
                }
            }

            if (context.RuntimeSize > m_LongDecompressedClipThresholdBytes &&
                sampleSettings.loadType == AudioClipLoadType.DecompressOnLoad)
            {
                yield return context.CreateIssue(
                    IssueCategory.AssetIssue, k_AudioLongDecompressedClipDescriptor.Id, clipName)
                    .WithLocation(assetPath);
            }

            if (sampleSettings.loadType == AudioClipLoadType.CompressedInMemory &&
                sampleSettings.compressionFormat != AudioCompressionFormat.PCM &&
                sampleSettings.compressionFormat != AudioCompressionFormat.ADPCM)
            {
                yield return context.CreateIssue(
                    IssueCategory.AssetIssue, k_AudioCompressedInMemoryDescriptor.Id, clipName)
                    .WithLocation(assetPath);
            }

            if (isMobileTarget &&
                context.ImportedSize > m_LongCompressedMobileClipThresholdBytes &&
                sampleSettings.compressionFormat != AudioCompressionFormat.PCM &&
                sampleSettings.compressionFormat != AudioCompressionFormat.ADPCM &&
                audioClip.frequency >= 48000 &&
                sampleSettings.quality == 1.0f)
            {
                yield return context.CreateIssue(
                    IssueCategory.AssetIssue, k_AudioLargeCompressedMobileDescriptor.Id, clipName)
                    .WithLocation(assetPath);
            }

            // Annoyingly, if a clip is compressed, it can't go higher than 48kHz: The frequency gets clamped when it's
            // passed to FMOD and it's not trivial to get the sample rate of the original source audio file. If we find
            // a workaround for that, we should change this. In the meantime, it's useful for uncompressed samples at least.
            if (audioClip.frequency > 48000)
            {
                yield return context.CreateIssue(
                    IssueCategory.AssetIssue, k_Audio48kHzDescriptor.Id, clipName)
                    .WithLocation(assetPath);
            }

            // Preload Audio Data is disabled by Unity when Load Type is Streaming, so the serialized value is irrelevant.
            if (preloadAudioData && !isStreaming)
            {
                yield return context.CreateIssue(
                    IssueCategory.AssetIssue, k_AudioPreloadDescriptor.Id, clipName)
                    .WithLocation(assetPath);
            }

            if (!context.Importer.loadInBackground && context.ImportedSize > m_LoadInBackGroundClipSizeThresholdBytes)
            {
                yield return context.CreateIssue(
                    IssueCategory.AssetIssue, k_AudioLoadInBackgroundDisabledDescriptor.Id, clipName)
                    .WithLocation(assetPath);
            }

            if (sampleSettings.compressionFormat == AudioCompressionFormat.MP3)
            {
                yield return context.CreateIssue(
                    IssueCategory.AssetIssue, k_AudioMP3Descriptor.Id, clipName)
                    .WithLocation(assetPath);
            }

            if (sourceFileExtension != "WAV" &&
                sourceFileExtension != "AIFF" &&
                sourceFileExtension != "AIF")
            {
                yield return context.CreateIssue(
                    IssueCategory.AssetIssue, k_AudioCompressedSourceAssetDescriptor.Id, clipName)
                    .WithLocation(assetPath);
            }
        }
    }
}
