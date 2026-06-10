// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.Toolbars
{
    enum UITriggerLocalEventSubType
    {
        AIDropdownOpened,
        AIInstallAccepted,
    }

    internal static class EditorAIAssistantAnalytics
    {
        [Serializable]
        class UITriggerLocalEventData : IAnalytic.IData
        {
            public UITriggerLocalEventData(UITriggerLocalEventSubType subType) => SubType = subType.ToString();

            public string SubType;
        }

        [AnalyticInfo(eventName: "AIAssistantUITriggerLocalEvent", vendorKey: "unity.ai.assistant")]
        class UITriggerLocalEvent : IAnalytic
        {
            readonly UITriggerLocalEventData m_Data;

            public UITriggerLocalEvent(UITriggerLocalEventData data) => m_Data = data;

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_Data;
                return true;
            }
        }

        static void ReportUITriggerLocalEvent(UITriggerLocalEventData data)
        {
            if (EditorAnalytics.enabled)
                EditorAnalytics.SendAnalytic(new UITriggerLocalEvent(data));
        }

        internal static void ReportAIDropdownOpenedEvent()
        {
            ReportUITriggerLocalEvent(new UITriggerLocalEventData(UITriggerLocalEventSubType.AIDropdownOpened));
        }

        internal static void ReportAIInstallAcceptedEvent()
        {
            ReportUITriggerLocalEvent(new UITriggerLocalEventData(UITriggerLocalEventSubType.AIInstallAccepted));
        }
    }
}
