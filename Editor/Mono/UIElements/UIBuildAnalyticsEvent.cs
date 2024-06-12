// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace UnityEditor.Audio.Analytics;

class UIBuildAnalyticsEvent : IPostprocessBuildWithReport
{
    static bool s_EventRegistered;
    const string k_EventName = "uiBuild";
    const string k_VendorKey = "unity.ui";
    const int k_MaxEventsPerHour = 60;
    const int k_MaxNumberOfElements = 1000;
    private const int k_EventVersion = 2;

    [Serializable]
    internal class UIBuildEvent
    {
        [Serializable]
        public struct UIAssetCounters
        {
            public int PanelSettings;
            public int VisualTreeAsset;
            public int StyleSheet;
            public int ThemeStyleSheet;
        }

        public string build_guid;
        public string build_session_guid;
        public int build_type;
        public UIAssetCounters counters;

        public UIBuildEvent(string buildGuid, string buildSessionGuid, BuildType buildType, UIAssetCounters counters)
        {
            this.build_guid = buildGuid;
            this.build_session_guid = buildSessionGuid;
            this.build_type = (int)buildType;
            this.counters = counters;
        }
    }

    public int callbackOrder { get; }

    public void OnPostprocessBuild(BuildReport report)
    {
        SendEvent(report);
    }


    static bool EnableAnalytics()
    {        
        if (!EditorAnalytics.enabled)
            return false;

        if (!s_EventRegistered)
        {
            AnalyticsResult result = EditorAnalytics.RegisterEventWithLimit(k_EventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey, k_EventVersion);
            if (result == AnalyticsResult.Ok)
                s_EventRegistered = true;
        }

        return s_EventRegistered;
    }

    static void SendEvent(BuildReport report)
    {
        if ( report == null
            || report.packedAssets.Length == 0)
        {
            return;
        }

        if (!EnableAnalytics())
            return;

        UIBuildEvent.UIAssetCounters counters = default;

        var registeredVTAs = new HashSet<GUID>();

        for (var i = 0; i < report.packedAssets.Length; ++i)
        {
            var infos = report.packedAssets[i].GetContents();

            for (var j = 0; j < infos.Length; ++j)
            {
                PackedAssetInfo info = infos[j];

                // Skip any default/builtin resource
                if (info.sourceAssetGUID == default)
                    continue;

                if (info.type == typeof(MonoBehaviour))
                {
                    // We want the actual user type, which is not given by PackedAssetInfo.type
                    Type type = AssetDatabase.GetMainAssetTypeFromGUID(info.sourceAssetGUID);

                    if (type == null)
                        continue;

                    if (type == typeof(VisualTreeAsset))
                    {
                        // The same VisualTreeAsset may appear multiple times because it contains a StyleSheet subasset
                        // Which seems to cause it to appear twice in the BuildReport content list
                        // So make sure we only count unique GUIDs to avoid counting them twice
                        if (registeredVTAs.Add(info.sourceAssetGUID))
                        {
                            counters.VisualTreeAsset++;
                        }
                    }
                    else if (type == typeof(StyleSheet))
                    {
                        counters.StyleSheet++;
                    }
                    else if (type == typeof(ThemeStyleSheet))
                    {
                        counters.ThemeStyleSheet++;
                    }
                    else if (type == typeof(PanelSettings))
                    {
                        counters.PanelSettings++;
                    }
                }
            }
        }

        string buildSessionGuid = EditorApplication.buildSessionGUID.ToString();

        var buildEvent = new UIBuildEvent(report.summary.guid.ToString(), buildSessionGuid, report.summary.buildType, counters);
        EditorAnalytics.SendEventWithLimit(k_EventName, buildEvent, k_EventVersion);
    }
}
