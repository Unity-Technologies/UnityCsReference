// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Profiling.Analytics
{
    internal interface IAnalyticsService
    {
        bool RegisterEventWithLimit(string eventName, int maxEventPerHour, int maxItems, string vendorKey);
        bool SendEventWithLimit(string eventName, object parameters);
    }
}
