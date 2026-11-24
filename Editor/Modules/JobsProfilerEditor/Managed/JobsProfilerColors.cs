// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.JobsProfiling;

/// <summary>
/// Simple color constants for the Jobs Profiler. This class has no static constructor
/// and can be safely used from Burst-compiled code. Scalar fields rather than arrays,
/// because Burst cannot access managed arrays held in statics.
/// </summary>
internal static class JobsProfilerColors
{
    // Semantic colors (same in both themes - these convey meaning)
    internal static readonly Color32 ScheduleColor = new Color32(114, 114, 255, 255);
    internal static readonly Color32 CompletedWaitColor = new Color32(255, 0, 0, 255);
    internal static readonly Color32 CompletedNoWaitColor = new Color32(0, 255, 0, 255);
    // Unity's Color.yellow, which is not pure yellow
    internal static readonly Color32 DependencyColor = new Color32(255, 235, 4, 255);
    internal static readonly Color32 FallbackColor = new Color32(255, 255, 255, 255);

    // Okabe-Ito. JobsProfilerSettings.GetColorBlindSafeColor only maps the 17 profiler
    // category colors, so it returns null for these and cannot be used.
    internal static readonly Color32 ScheduleColorColorBlind = new Color32(0, 114, 178, 255);        // blue
    internal static readonly Color32 CompletedWaitColorColorBlind = new Color32(213, 94, 0, 255);    // vermillion
    internal static readonly Color32 CompletedNoWaitColorColorBlind = new Color32(0, 158, 115, 255); // bluish green
    internal static readonly Color32 DependencyColorColorBlind = new Color32(240, 228, 66, 255);     // yellow
    internal static readonly Color32 FallbackColorColorBlind = new Color32(255, 255, 255, 255);

    internal static Color32 GetScheduleColor(bool colorBlind) => colorBlind ? ScheduleColorColorBlind : ScheduleColor;
    internal static Color32 GetCompletedWaitColor(bool colorBlind) => colorBlind ? CompletedWaitColorColorBlind : CompletedWaitColor;
    internal static Color32 GetCompletedNoWaitColor(bool colorBlind) => colorBlind ? CompletedNoWaitColorColorBlind : CompletedNoWaitColor;
    internal static Color32 GetDependencyColor(bool colorBlind) => colorBlind ? DependencyColorColorBlind : DependencyColor;
    internal static Color32 GetFallbackColor(bool colorBlind) => colorBlind ? FallbackColorColorBlind : FallbackColor;

    internal static readonly Color StripeColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
}
