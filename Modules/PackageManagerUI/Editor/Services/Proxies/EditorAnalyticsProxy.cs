// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Analytics;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class EditorAnalyticsProxy
    {
        private const int k_MaxEventsPerHour = 1000;
        private const int k_MaxNumberOfElementInStruct = 100;
        private const string k_VendorKey = "unity.package-manager-ui";

        private readonly HashSet<string> m_RegisteredEvents = new HashSet<string>();

        public virtual bool RegisterEvent(string eventName)
        {
            if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
                return false;

            if (!EditorAnalytics.enabled)
            {
                Console.WriteLine("[Package Manager Window] Editor analytics are disabled");
                return false;
            }

            if (m_RegisteredEvents.Contains(eventName))
                return true;

            var result = EditorAnalytics.RegisterEventWithLimit(eventName, k_MaxEventsPerHour, k_MaxNumberOfElementInStruct, k_VendorKey);
            switch (result)
            {
                case AnalyticsResult.Ok:
                case AnalyticsResult.TooManyRequests:
                    {
                        m_RegisteredEvents.Add(eventName);
                        return true;
                    }
                default:
                    {
                        Console.WriteLine($"[Package Manager Window] Failed to register analytics event '{eventName}'. Result: '{result}'");
                        break;
                    }
            }

            return false;
        }

        public virtual AnalyticsResult SendEventWithLimit(string eventName, object parameters)
        {
            return EditorAnalytics.SendEventWithLimit(eventName, parameters);
        }
    }
}
