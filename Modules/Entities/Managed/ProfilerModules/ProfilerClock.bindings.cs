// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Entities
{
    /// <summary>
    /// The profiler's clock, the one ProfilerUnsafeUtility.Timestamp in the Core module reads.
    /// </summary>
    [NativeHeader("Modules/Entities/Public/EntitiesProfilerClock.bindings.h")]
    internal static unsafe class ProfilerClock
    {
        [FreeFunction("EntitiesProfilerClock::GetTimestamp", IsThreadSafe = true)]
        internal static extern long GetTimestamp();

        // Whether the profiler is recording and the category is enabled; the native state Profiler.enabled and
        // Profiler.IsCategoryEnabled read in the Core module.
        [FreeFunction("EntitiesProfilerClock::IsCategoryRecording", IsThreadSafe = true)]
        internal static extern bool IsCategoryRecording(ushort categoryId);

        [FreeFunction("EntitiesProfilerClock::GetTimestampToNanosecondsConversionRatio", IsThreadSafe = true)]
        static extern void GetTimestampToNanosecondsConversionRatio(long* numerator, long* denominator);

        internal static long ToNanoseconds(long timestampDelta)
        {
            long numerator, denominator;
            GetTimestampToNanosecondsConversionRatio(&numerator, &denominator);
            return timestampDelta * numerator / denominator;
        }
    }
}
