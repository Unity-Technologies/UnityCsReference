// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Analytics;

namespace Unity.GraphToolkit.Editor
{
    internal static class GraphToolkitAnalytics
    {
        // flag disabled during unit tests.
        internal static bool EnableAnalytics { get; set; } = true;

        // Tracks extensions that have already sent an event this session to avoid duplicates.
        static readonly HashSet<string> s_SentExtensions = new HashSet<string>();

        const string k_EventName = "graphtoolCreated";
        const string k_VendorKey = "unity.graphtoolkit";
        const int k_Version = 3;

        [Serializable]
        internal struct GraphtoolCreatedEventData : UnityEngine.Analytics.IAnalytic.IData
        {
            public string fileExtId;
        }

        [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey, version: k_Version)]
        internal class GraphtoolCreatedAnalytic : IAnalytic
        {
            private string m_Extension;

            public GraphtoolCreatedAnalytic(string extension)
            {
                m_Extension = extension;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = new GraphtoolCreatedEventData
                {
                    fileExtId = m_Extension
                };
                return true;
            }
        }

        public static void SendGraphToolCreatedEvent(string extension)
        {
            if (!EnableAnalytics)
                return;

            // Sanitize the extension (remove the dot if present)
            if (extension.StartsWith("."))
                extension = extension.Substring(1);

            if (s_SentExtensions.Add(extension))
                EditorAnalytics.SendAnalytic(new GraphtoolCreatedAnalytic(extension));
        }
    }
}
