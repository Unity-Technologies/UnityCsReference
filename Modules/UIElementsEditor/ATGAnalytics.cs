// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.UIElements
{
    internal static class ATGAnalytics
    {
        public const string k_EventName = "AdvancedTextGenerator";
        public const string k_VendorKey = "unity.advanced-text-generator";

        [Serializable]
        internal class ATGEnabledData : IAnalytic.IData
        {
            public bool enabled;
        }

        [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey)]
        class ATGEnabledAnalytic : IAnalytic
        {
            readonly bool m_Enabled;

            public ATGEnabledAnalytic(bool enabled)
            {
                m_Enabled = enabled;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = new ATGEnabledData
                {
                    enabled = m_Enabled
                };
                return true;
            }
        }

        public static void ReportATGEnabled(bool enabled)
        {
            var analytic = new ATGEnabledAnalytic(enabled);
            EditorAnalytics.SendAnalytic(analytic);
        }
    }
}
