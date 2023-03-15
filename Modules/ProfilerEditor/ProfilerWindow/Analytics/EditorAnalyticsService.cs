// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Analytics;

namespace UnityEditor.Profiling.Analytics
{
    internal class EditorAnalyticsService : IAnalyticsService
    {
        bool IAnalyticsService.RegisterEventWithLimit(string eventName, int maxEventPerHour, int maxItems, string vendorKey)
        {
            var result = EditorAnalytics.RegisterEventWithLimit(eventName, maxEventPerHour, maxItems, vendorKey);
            switch (result)
            {
                case AnalyticsResult.Ok:
                case AnalyticsResult.TooManyRequests:
                    return true;
                default:
                    return false;
            }
        }

        bool IAnalyticsService.SendEventWithLimit(string eventName, object parameters)
        {
            return EditorAnalytics.SendEventWithLimit(eventName, parameters) == AnalyticsResult.Ok;
        }
    }
}
