// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;
using UnityEngine.Profiling;
using System.Runtime.InteropServices;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum AudioClipProperty
    {
        Length = 0,
        SourceFileSize,
        ImportedFileSize,
        RuntimeSize,
        CompressionRatio,
        CompressionFormat,
        SampleRate,
        ForceToMono,
        LoadInBackground,
        PreloadAudioData,
        LoadType,

        Num
    }

    class AudioClipModule : ModuleWithAnalyzers<AudioClipModuleAnalyzer>
    {
        static readonly IssueLayout k_AudioClipLayout = new IssueLayout
        {
            Category = IssueCategory.AudioClip,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Name", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyType.FileType, Name = "Format", IsDefaultGroup = true },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.Length), Format = PropertyFormat.String, Name = "Length"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.SourceFileSize), Format = PropertyFormat.Bytes, Name = "Source File Size"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.ImportedFileSize), Format = PropertyFormat.Bytes, Name = "Imported File Size"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.RuntimeSize), Format = PropertyFormat.Bytes, Name = "Runtime Size (Estimate)"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.CompressionRatio), Format = PropertyFormat.String, Name = "Compression Ratio"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.CompressionFormat), Format = PropertyFormat.String, Name = "Compression Format"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.SampleRate), Format = PropertyFormat.String, Name = "Sample Rate"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.ForceToMono), Format = PropertyFormat.Bool, Name = "Force To Mono"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.LoadInBackground), Format = PropertyFormat.Bool, Name = "Load In Background"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.PreloadAudioData), Format = PropertyFormat.Bool, Name = "Preload Audio Data" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.LoadType), Format = PropertyFormat.String, Name = "Load Type" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", MaxAutoWidth = 500 }
            }
        };

        public override string Name => "Audio Clips";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_AudioClipLayout,
            AssetsModule.k_IssueLayout
        };

        const int k_StreamingBuffer = 64000;        // The per-instance streaming buffer, which Unity's FMOD implementation defaults to 64000.

        public override void Initialize()
        {
            base.Initialize();

            ProjectIssueExtensions.AddCustomComparer(IssueCategory.AudioClip, PropertyTypeUtil.FromCustom(AudioClipProperty.SampleRate),
                (a, b) =>
                {
                    // Use Split rather than Substring/AsSpan since it could end with Hz or KHz.
                    var strA = a.GetProperty(PropertyTypeUtil.FromCustom(AudioClipProperty.SampleRate)).Split(' ')[0];
                    var strB = b.GetProperty(PropertyTypeUtil.FromCustom(AudioClipProperty.SampleRate)).Split(' ')[0];

                    var floatA = Single.Parse(strA);
                    var floatB = Single.Parse(strB);

                    return floatA < floatB ? -1 : floatA > floatB ? 1 : 0;
                });
            ProjectIssueExtensions.AddCustomComparer(IssueCategory.AudioClip, PropertyTypeUtil.FromCustom(AudioClipProperty.CompressionRatio),
                (a, b) =>
                {
                    var strA = a.GetProperty(PropertyTypeUtil.FromCustom(AudioClipProperty.CompressionRatio));
                    var strB = b.GetProperty(PropertyTypeUtil.FromCustom(AudioClipProperty.CompressionRatio));

                    // Cut off the '%' at the end
                    var floatA = Single.Parse(strA.Substring(0, strA.Length - 1));
                    var floatB = Single.Parse(strB.Substring(0, strB.Length - 1));

                    return floatA < floatB ? -1 : floatA > floatB ? 1 : 0;
                });
        }

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var analyzers = GetCompatibleAnalyzers(analysisParams);

            var context = new AudioClipAnalysisContext
            {
                // Importer is set in the loop
                Params = analysisParams
            };

            var assetPaths = GetAssetPathsByFilter("t:AudioClip, a:assets", context);

            progress?.Start("Finding Audio Clips", "Search in Progress...", assetPaths.Length);

            var issues = new List<ReportItem>();

            foreach (var assetPath in assetPaths)
            {
                // Check if the operation was cancelled
                if (progress?.IsCancelled ?? false)
                    return AnalysisResult.Cancelled;

                var audioImporter = AssetImporter.GetAtPath(assetPath) as AudioImporter;
                if (audioImporter == null)
                {
                    continue;
                }

                var sampleSettings = audioImporter.GetOverrideSampleSettings(analysisParams.PlatformAsString);
                var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                var clipName = Path.GetFileNameWithoutExtension(assetPath);

                var origSize = (int)GetPropertyValue(audioImporter, "origSize");
                var compSize = (int)GetPropertyValue(audioImporter, "compSize");

                var runtimeSize = Profiler.GetRuntimeMemorySizeLong(audioClip);

                // The decompression buffer is defined as "400ms of float sample data".
                // 1 second of audio is (sizeof(float) * audioClip.frequency * audioClip.channels) bytes.
                // So 400ms is 400 * ((sizeof(float) * audioClip.frequency * audioClip.channels) / 1000).
                // We can simplify this to the following:
                int decompressionBufferSize = (int)(1.6 * audioClip.frequency * audioClip.channels);

                // NOTE: Actual runtime memory footprint at any given moment depends on the number of instances of an AudioClip that are currently playing.
                // Each instance will need its own decompression buffer (if Streaming or CompressedInMemory) and streaming buffer (if Streaming)
                // In static analysis, we can't calculate the maximum number of instances of a clip that could play simultaneously at runtime, so let's estimate it at its most likely value: 1.
                switch (audioClip.loadType)
                {
                    case AudioClipLoadType.DecompressOnLoad:
                        // Since the decompression buffer is only needed during loading, let's ignore it. Just calculate the size of the decompressed PCM data.
                        runtimeSize += sizeof(float) * audioClip.samples * audioClip.channels;
                        break;
                    case AudioClipLoadType.CompressedInMemory:
                        runtimeSize += compSize + decompressionBufferSize;
                        break;
                    case AudioClipLoadType.Streaming:
                        runtimeSize += k_StreamingBuffer + decompressionBufferSize;
                        break;
                }

                context.Name = clipName;
                context.Importer = audioImporter;
                context.AudioClip = audioClip;
                context.SampleSettings = sampleSettings;
                context.ImportedSize = compSize;
                context.RuntimeSize = runtimeSize;

                var ts = new TimeSpan(0, 0, 0, 0, (int)(audioClip.length * 1000.0f));

                issues.Add(context.CreateInsight(IssueCategory.AudioClip, clipName)
                    .WithCustomProperties(
                        new object[(int)AudioClipProperty.Num]
                        {
                            Formatting.FormatDurationWithMs(ts),
                            origSize,
                            compSize,
                            runtimeSize,
                            Formatting.FormatPercentage((float)compSize / (float)origSize, 2),
                            sampleSettings.compressionFormat,
                            Formatting.FormatHz(audioClip.frequency),
                            context.Importer.forceToMono,
                            context.Importer.loadInBackground,
                            sampleSettings.preloadAudioData,
                            sampleSettings.loadType,
                        })
                    .WithLocation(assetPath));

                foreach (var analyzer in analyzers)
                {
                    analysisParams.OnIncomingIssues(analyzer.Analyze(context));
                }

                progress?.Advance();
            }

            if (issues.Count > 0)
                context.Params.OnIncomingIssues(issues);

            progress?.Clear();

            return AnalysisResult.Success;
        }

        static object GetPropertyValue(AssetImporter assetImporter, string propertyName)
        {
            Type objType = assetImporter.GetType();
            PropertyInfo propInfo = objType.GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (propInfo == null)
                throw new ArgumentOutOfRangeException("propertyName",
                    string.Format("Couldn't find property {0} in type {1}", propertyName, objType.FullName));
            return propInfo.GetValue(assetImporter, null);
        }
    }
}
