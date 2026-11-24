// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.JobsProfiling;

/// <summary>
/// Access to the UXML/USS assets shipped in this module's EditorResourcesPackage. Paths are relative to
/// the package's 'Editor Default Resources' folder, which is what <see cref="EditorGUIUtility.Load"/> expects.
/// </summary>
internal static class JobsProfilerResources
{
    const string k_Root = "JobsProfiler/";

    internal const string MainUxml = k_Root + "main.uxml";
    internal const string TimelineUxml = k_Root + "timeline.uxml";
    internal const string TimelineUss = k_Root + "timeline.uss";
    internal const string StatsUss = k_Root + "stats.uss";

    internal static VisualTreeAsset LoadVisualTreeAsset(string resourcePath)
    {
        var asset = EditorGUIUtility.Load(resourcePath) as VisualTreeAsset;

        if (asset == null)
            UnityEngine.Debug.LogError($"Jobs Profiler could not load the UXML resource '{resourcePath}'.");

        return asset;
    }

    internal static StyleSheet LoadStyleSheet(string resourcePath)
    {
        var styleSheet = EditorGUIUtility.Load(resourcePath) as StyleSheet;

        if (styleSheet == null)
            UnityEngine.Debug.LogError($"Jobs Profiler could not load the USS resource '{resourcePath}'.");

        return styleSheet;
    }
}
