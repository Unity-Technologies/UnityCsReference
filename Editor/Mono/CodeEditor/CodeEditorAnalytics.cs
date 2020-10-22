// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Analytics;

namespace Unity.CodeEditor
{
    class CodeEditorAnalytics
    {
        static bool s_EventRegistered = false;
        const int k_MaxEventsPerHour = 1000;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.codeeditor";
        const string k_EventName = "CodeEditorUsage";

        struct AnalyticsData
        {
            public string code_editor;
        }

        static bool EnableAnalytics()
        {
            AnalyticsResult result = EditorAnalytics.RegisterEventWithLimit(k_EventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);
            if (result == AnalyticsResult.Ok)
                s_EventRegistered = true;

            return s_EventRegistered;
        }

        public static void SendCodeEditorUsage(IExternalCodeEditor codeEditor)
        {
            if (!UnityEngine.Analytics.Analytics.enabled)
                return;

            if (!EnableAnalytics())
                return;

            var data = new AnalyticsData()
            {
                code_editor = codeEditor.GetType().FullName
            };
            EditorAnalytics.SendEventWithLimit(k_EventName, data);
        }
    }
}
