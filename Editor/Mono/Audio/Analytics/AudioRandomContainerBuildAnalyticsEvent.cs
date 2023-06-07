// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Audio;

namespace UnityEditor.Audio.Analytics;

class AudioRandomContainerBuildAnalyticsEvent : IPostprocessBuildWithReport
{
    const string k_EventName = "audioRandomContainerBuild";
    const int k_MaxEventsPerHour = 60;
    const int k_MaxNumberOfElements = 2;

    static bool s_Initialized;

    static bool Initialized
    {
        get
        {
            if (!s_Initialized)
            {
                s_Initialized = AudioAnalytics.RegisterEvent(k_EventName, k_MaxEventsPerHour, k_MaxNumberOfElements);
            }

            return s_Initialized;
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
            || AudioSettings.unityAudioDisabled
            || !Initialized
            || report == null
            || report.packedAssets.Length == 0)
        {
            return;
        }

        var count = 0;

        for (var i = 0; i < report.packedAssets.Length; ++i)
        {
            var infos = report.packedAssets[i].GetContents();

            for (var j = 0; j < infos.Length; ++j)
            {
                if (infos[j].type == typeof(AudioRandomContainer))
                {
                    ++count;
                }
            }
        }

        var payload = new Payload
        {
            build_guid = report.summary.guid.ToString(),
            count = count
        };

        EditorAnalytics.SendEventWithLimit(k_EventName, payload);
    }

    struct Payload
    {
        public string build_guid;
        public int count;
    }
}
