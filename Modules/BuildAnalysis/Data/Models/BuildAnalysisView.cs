// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Everything the tabs bind for one selected build: the entry the user picked in the build list, plus the
    /// analysis and the log messages for it. Built once per selection by BuildAnalysisWindow and handed to
    /// every tab, so a tab reads what it needs and ignores the rest.
    /// </summary>
    internal sealed class BuildAnalysisView
    {
        public BuildEntry Entry { get; }

        // Null when the analysis could not be produced, which the tabs render as no-selection.
        public BuildAnalysis Analysis { get; }

        // Read from BuildLog.jsonl rather than the analysis, and never null: an absent log is reported
        // through BuildLogMessages.Status, not by leaving this unset.
        public BuildLogMessages Messages { get; }

        // The listed builds this one ran nested in or contains, ancestors first. Never null.
        public IReadOnlyList<BuildEntry> RelatedBuilds { get; }

        public BuildAnalysisView(BuildEntry entry, AnalyzedBuild analyzed, IReadOnlyList<BuildEntry> relatedBuilds = null)
        {
            Entry = entry;
            Analysis = analyzed.Analysis;
            Messages = analyzed.Messages;
            RelatedBuilds = relatedBuilds ?? Array.Empty<BuildEntry>();
        }
    }
}
