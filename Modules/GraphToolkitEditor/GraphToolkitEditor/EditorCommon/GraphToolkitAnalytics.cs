// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.Analytics;

namespace Unity.GraphToolkit.Editor
{
    internal static class GraphToolkitAnalytics
    {
        const string k_EventName = "graphtoolCreated";
        const string k_VendorKey = "unity.graphtoolkit";
        const int k_Version = 1;
        
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
            // Sanitize the extension (remove the dot if present)
            if (extension.StartsWith("."))
                extension = extension.Substring(1);

            EditorAnalytics.SendAnalytic(new GraphtoolCreatedAnalytic(extension));
        }
    }
}
