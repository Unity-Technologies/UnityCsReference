// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class QualitySettingsAnalyzer : SettingsModuleAnalyzer
    {
        internal const string PAS0018 = nameof(PAS0018);
        internal const string PAS0019 = nameof(PAS0019);
        internal const string PAS0020 = nameof(PAS0020);
        internal const string PAS0021 = nameof(PAS0021);
        internal const string PAS1007 = nameof(PAS1007);

        static readonly Descriptor k_DefaultSettingsDescriptor = new Descriptor(
            PAS0018,
            "Quality: Using default Quality Levels",
            Areas.CPU | Areas.GPU | Areas.BuildSize | Areas.LoadTime,
            "This project is using the default set of <b>Quality Levels</b> defined in Quality Settings. This can make it difficult to understand the range of rendering settings used in the project, and can result in an unnecessarily large number of shader variants, impacting build times and runtime memory usage.",
            "Check the quality setting for each platform the project supports in the grid - it's the level with the green tick. Remove quality levels you are not using, to make the Quality Settings simpler to see and edit. Adjust the setting for each platform if necessary, then select the appropriate levels to examine their settings in the panel below.");

        static readonly Descriptor k_UsingLowQualityTexturesDescriptor = new Descriptor(
            PAS0019,
            "Quality: Texture Quality is not set to Full Res",
            Areas.GPU | Areas.BuildSize,
            "One or more of the <b>Quality Levels</b> in the project's Quality Settings has <b>Texture Quality</b> set to something other than <b>Full Res</b>. This option can save memory on lower-spec devices and platforms by discarding higher-resolution mip levels on mipmapped textures before uploading them to the GPU. However, this option has no effect on textures which don't have mipmaps enabled (as is frequently the case with UI textures, for instance), does nothing to reduce download or install size, and gives you no control over the texture resize algorithm.",
            "For devices which must use lower-resolution versions of textures, consider creating these lower resolution textures separately, and choosing the appropriate content to load at runtime using AssetBundle variants.");

        static readonly Descriptor k_DefaultAsyncUploadTimeSliceDescriptor = new Descriptor(
            PAS0020,
            "Quality: Async Upload Time Slice is set to default value",
            Areas.LoadTime,
            "The <b>Async Upload Time Slice</b> option for one or more <b>Quality Levels</b> in the project's Quality Settings is set to the default value of <b>2ms</b>.",
            "If the project encounters long loading times when loading large amount of texture and/or mesh data, experiment with increasing this value to see if it allows content to be uploaded to the GPU more quickly.");

        static readonly Descriptor k_DefaultAsyncUploadBufferSizeSliceDescriptor = new Descriptor(
            PAS0021,
            "Quality: Async Upload Buffer Size is set to default value",
            Areas.LoadTime,
            "The <b>Async Upload Buffer Size</b> option for one or more <b>Quality Levels</b> in the project's Quality Settings is set to the default value.",
            "If the project encounters long loading times when loading large amount of texture and/or mesh data, experiment with increasing this value to see if it allows content to be uploaded to the GPU more quickly. This is most likely to help if you are loading large textures. Note that this setting controls a buffer size in megabytes, so exercise caution if memory is limited in your application.");

        static readonly Descriptor k_TextureStreamingDisabledDescriptor = new Descriptor(
            PAS1007,
            "Quality: Mipmap streaming is disabled",
            Areas.Memory,
            "<b>Mipmap Streaming</b> is disabled in Quality Settings. As a result, all mip levels for all loaded textures are loaded into GPU memory, potentially resulting in excessive texture memory usage.",
            "If your project contains many high resolution mipmapped textures, enable <b>Mipmap Streaming</b> in Quality Settings.")
        {
            Fixer = (issue, analysisParams) =>
            {
                EnableStreamingMipmap(issue.GetCustomPropertyInt32(0));
            },

            DocumentationUrl = "https://docs.unity3d.com/Manual/TextureStreaming.html",
            MessageFormat = "Quality: Mipmap streaming on Quality Level '{0}' is turned off"
        };

        static readonly string k_QualityLocation = "Project/Quality";

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_DefaultSettingsDescriptor);
            registerDescriptor(k_UsingLowQualityTexturesDescriptor);
            registerDescriptor(k_DefaultAsyncUploadTimeSliceDescriptor);
            registerDescriptor(k_DefaultAsyncUploadBufferSizeSliceDescriptor);
            registerDescriptor(k_TextureStreamingDisabledDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            if (IsUsingDefaultSettings())
            {
                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_DefaultSettingsDescriptor.Id)
                    .WithLocation(k_QualityLocation);
            }

            if (IsUsingLowQualityTextures())
            {
                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_UsingLowQualityTexturesDescriptor.Id)
                    .WithLocation(k_QualityLocation);
            }

            if (IsDefaultAsyncUploadTimeSlice())
            {
                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_DefaultAsyncUploadTimeSliceDescriptor.Id)
                    .WithLocation(k_QualityLocation);
            }

            if (IsDefaultAsyncUploadBufferSize())
            {
                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_DefaultAsyncUploadBufferSizeSliceDescriptor.Id)
                    .WithLocation(k_QualityLocation);
            }

            if (GetTextureStreamingDisabledQualityLevelsIndex().Count != 0)
            {
                var qualityLevels = GetTextureStreamingDisabledQualityLevelsIndex();
                foreach (var levelIndex in qualityLevels)
                {
                    var levelName = QualitySettings.names[levelIndex];
                    yield return context.CreateIssue(IssueCategory.ProjectSetting, k_TextureStreamingDisabledDescriptor.Id, levelName)
                        .WithCustomProperties(new object[] {levelIndex})
                        .WithLocation(k_QualityLocation);
                }
            }
        }

        internal static bool IsUsingDefaultSettings()
        {
            return (QualitySettings.names.Length == 6 &&
                QualitySettings.names[0] == "Very Low" &&
                QualitySettings.names[1] == "Low" &&
                QualitySettings.names[2] == "Medium" &&
                QualitySettings.names[3] == "High" &&
                QualitySettings.names[4] == "Very High" &&
                QualitySettings.names[5] == "Ultra");
        }

        internal static bool IsUsingLowQualityTextures()
        {
            var usingLowTextureQuality = false;
            var initialQualityLevel = QualitySettings.GetQualityLevel();

            for (var i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);

                if (QualitySettings.globalTextureMipmapLimit > 0)
                {
                    usingLowTextureQuality = true;
                    break;
                }
            }

            if (initialQualityLevel != QualitySettings.GetQualityLevel())
                QualitySettings.SetQualityLevel(initialQualityLevel);
            return usingLowTextureQuality;
        }

        internal static bool IsDefaultAsyncUploadTimeSlice()
        {
            var usingDefaultAsyncUploadTimeslice = false;
            var initialQualityLevel = QualitySettings.GetQualityLevel();

            for (var i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);

                if (QualitySettings.asyncUploadTimeSlice == 2)
                {
                    usingDefaultAsyncUploadTimeslice = true;
                    break;
                }
            }

            if (initialQualityLevel != QualitySettings.GetQualityLevel())
                QualitySettings.SetQualityLevel(initialQualityLevel);
            return usingDefaultAsyncUploadTimeslice;
        }

        internal static bool IsDefaultAsyncUploadBufferSize()
        {
            var usingDefaultAsyncUploadBufferSize = false;
            var initialQualityLevel = QualitySettings.GetQualityLevel();

            for (var i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);

                if (QualitySettings.asyncUploadBufferSize == 4 || QualitySettings.asyncUploadBufferSize == 16)
                {
                    usingDefaultAsyncUploadBufferSize = true;
                    break;
                }
            }

            if (initialQualityLevel != QualitySettings.GetQualityLevel())
                QualitySettings.SetQualityLevel(initialQualityLevel);
            return usingDefaultAsyncUploadBufferSize;
        }

        internal static List<int> GetTextureStreamingDisabledQualityLevelsIndex()
        {
            var initialQualityLevel = QualitySettings.GetQualityLevel();
            var qualityIndexes = new List<int>();

            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);

                if (!QualitySettings.streamingMipmapsActive)
                {
                    qualityIndexes.Add(i);
                }
            }

            if (initialQualityLevel != QualitySettings.GetQualityLevel())
                QualitySettings.SetQualityLevel(initialQualityLevel);
            return qualityIndexes;
        }

        internal static void EnableStreamingMipmap(int qualityLevelIndex)
        {
            var initialQualityLevel = QualitySettings.GetQualityLevel();

            QualitySettings.SetQualityLevel(qualityLevelIndex);
            QualitySettings.streamingMipmapsActive = true;

            if (initialQualityLevel != QualitySettings.GetQualityLevel())
                QualitySettings.SetQualityLevel(initialQualityLevel);
        }
    }
}
