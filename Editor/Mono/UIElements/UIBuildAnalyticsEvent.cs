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

    [AnalyticInfo("uiBuild", "unity.ui", 2, 60)]
    internal class UIBuildEvent : IAnalytic
    {
        [Serializable]
        public struct UIAssetCounters
        {
            public int PanelSettings;
            public int VisualTreeAsset;
            public int StyleSheet;
            public int ThemeStyleSheet;
        }

        [Serializable]
        struct Payload : IAnalytic.IData
        {
            public string build_guid;
            public string build_session_guid;
            public int build_type;
            public UIAssetCounters counters;
        }

        public string buildGuid;
        public string buildSessionGuid;
        public BuildType buildType;
        public UIAssetCounters counters;

        public UIBuildEvent(string buildGuid, string buildSessionGuid, BuildType buildType, UIAssetCounters counters)
        {
            this.buildGuid = buildGuid;
            this.buildSessionGuid = buildSessionGuid;
            this.buildType = buildType;
            this.counters = counters;
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = new Payload
            {
                build_guid = buildGuid,
                build_session_guid = buildSessionGuid,
                build_type = (int)buildType,
                counters = counters,
            };
            return data != null;
        }
    }

    public int callbackOrder { get; }

    public void OnPostprocessBuild(BuildReport report)
    {
        SendEvent(report);
    }

    static void SendEvent(BuildReport report)
    {
        if (!EditorAnalytics.enabled
            || report == null
            || report.packedAssets.Length == 0)
        {
            return;
        }

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
        EditorAnalytics.SendAnalytic(buildEvent);
    }
}
