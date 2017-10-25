// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [StaticAccessor("UsabilityAnalytics", StaticAccessorType.DoubleColon)]
    [NativeHeader("Editor/Src/Utility/UsabilityAnalytics.h")]
    internal static partial class UsabilityAnalytics
    {
        // PageView tracking. /page/ can be any path that is meaningful in the context
        public static void Track(string page)
        {
            TrackPageView("editor.analytics.unity3d.com", page, "", false);
        }

        // Event tracking. /category/, /action/ and /label/ is the event type and /value/ is an attached value
        public static void Event(string category, string action, string label, int value)
        {
            TrackEvent(category, action, label, value, false);
        }

        internal static void SendEvent(string subType, DateTime startTime, TimeSpan duration, bool isBlocking, object parameters)
        {
            if (startTime.Kind == DateTimeKind.Local)
                throw new ArgumentException("Local DateTimes are not supported, use UTC instead.");

            SendUsabilityEvent(subType, startTime.Ticks, duration.Ticks, isBlocking, parameters);
        }

        extern private static void SendUsabilityEvent(
            string subType,
            Int64 startTimeTicks,
            long durationTicks,
            bool isBlocking,
            object parameters);

        [StaticAccessor("UsabilityAnalytics::GetDefault()", StaticAccessorType.Dot)]
        extern private static void TrackPageView(
            string hostname,
            string path,
            [UnityEngine.Internal.DefaultValue("")] string referrer,
            [UnityEngine.Internal.DefaultValue("false")] bool forceRequest);

        [StaticAccessor("UsabilityAnalytics::GetDefault()", StaticAccessorType.Dot)]
        extern private static void TrackEvent(
            string category,
            string action,
            [UnityEngine.Internal.DefaultValue("")] string label,
            [UnityEngine.Internal.DefaultValue("0")] int value,
            [UnityEngine.Internal.DefaultValue("false")] bool forceRequest);
    }
}
