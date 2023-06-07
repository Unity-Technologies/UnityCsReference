// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Analytics;

namespace UnityEditor.Audio.Analytics;

static class AudioAnalytics
{
    const string k_VendorKey = "unity.audio";

    internal static bool RegisterEvent(string eventName, int maxEventsPerHour, int maxNumberOfElements)
    {
        var result = EditorAnalytics.RegisterEventWithLimit(eventName, maxEventsPerHour, maxNumberOfElements, k_VendorKey);

        if (result == AnalyticsResult.Ok)
        {
            return true;
        }

        Console.WriteLine($"Event '{eventName}' could not be registered.");
        return false;
    }
}
