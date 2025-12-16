// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class StreamingAssetsFolderAnalyzer : SettingsModuleAnalyzer
    {
        internal const string PAA3002 = nameof(PAA3002);

        static readonly Descriptor k_StreamingAssetsFolderDescriptor = new Descriptor(
            PAA3002,
            "StreamingAssets folder size",
            Areas.BuildSize,
            $"There are many files in the <b>StreamingAssets folder</b>. Keeping them in the StreamingAssets folder will increase the build size.",
            $"Try to move files outside this folder and use Asset Bundles or Addressables when possible."
        )
        {
            Platforms = [BuildTarget.Android, BuildTarget.iOS],
            MessageFormat = "StreamingAssets folder contains {0} of data",
        };

#pragma warning disable CS0649
        [DiagnosticParameter("StreamingAssetsFolderSizeLimit", "StreamingAssets folder size limit (MB)", "If the StreamingAssets folder is larger than this threshold an Issue will be created (on mobile devices).", 50)]
        int m_FolderSizeLimit;
#pragma warning restore CS0649

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_StreamingAssetsFolderDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            // StreamingAssets folder is checked once, AssetsModule might not be the best place this check
            if (k_StreamingAssetsFolderDescriptor.IsApplicable(context.Params))
            {
                var issue = AnalyzeStreamingAssets(context);
                if (issue != null)
                    yield return issue;
            }
        }

        ReportItem AnalyzeStreamingAssets(AnalysisContext context)
        {
            if (!Directory.Exists("Assets/StreamingAssets"))
                return null;

            var totalBytes = 0L;
            var files = Directory.GetFiles("Assets/StreamingAssets", "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                totalBytes += fileInfo.Length;
            }

            var folderSizeLimitMB = m_FolderSizeLimit;

            if (totalBytes <= folderSizeLimitMB * 1024 * 1024)
                return null;

            return context.CreateIssue(IssueCategory.ProjectSetting, k_StreamingAssetsFolderDescriptor.Id,
                Formatting.FormatSize((ulong)totalBytes));
        }
    }
}
