// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [StaticAccessor("UnityEditorAnalytics", StaticAccessorType.DoubleColon)]
    [NativeHeader("Modules/UnityEditorAnalyticsEditor/UnityEditorAnalytics.h")]
    internal static partial class UsabilityAnalytics
    {
        internal static void SendEvent(string subType, DateTime startTime, TimeSpan duration, bool isBlocking, object parameters)
        {
            if (startTime.Kind == DateTimeKind.Local)
                throw new ArgumentException("Local DateTimes are not supported, use UTC instead.");

            SendUsabilityEventStatic(subType, startTime.Ticks, duration.Ticks, isBlocking, parameters);
        }

        extern private static void SendUsabilityEventStatic(
            string subType,
            Int64 startTimeTicks,
            long durationTicks,
            bool isBlocking,
            object parameters);
    }
}
