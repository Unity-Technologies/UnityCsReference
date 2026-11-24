// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Everything the UI needs for one build: the analysis artifact plus the build log messages, which
    /// live in a sibling file and are deliberately not part of the artifact. Cached and delivered as one
    /// unit so that a memory-cache hit can still be served without going async.
    /// </summary>
    internal sealed class AnalyzedBuild
    {
        [NoAutoStaticsCleanup]
        public static readonly AnalyzedBuild Unavailable = new AnalyzedBuild(null, null);

        public readonly BuildAnalysis Analysis;
        public readonly BuildLogMessages Messages;

        public AnalyzedBuild(BuildAnalysis analysis, BuildLogMessages messages)
        {
            Analysis = analysis;
            Messages = messages ?? BuildLogMessages.Missing;
        }
    }
}
