// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Analytics;

namespace UnityEditor.Audio.Analytics;

class AudioRandomContainerBuildAnalyticsEvent : IPostprocessBuildWithReport
{
    [AnalyticInfo(eventName: "audioRandomContainerBuild", vendorKey: "unity.audio", maxEventsPerHour: 60, maxNumberOfElements: 2)]
    internal class AudioRandomAnalytic : IAnalytic
    {
        public AudioRandomAnalytic(string build_guid, int count)
        {
            this.build_guid = build_guid;
            this.count = count;
        }

        [Serializable]
        struct Payload : IAnalytic.IData
        {
            public string build_guid;
            public int count;
        }


        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = new Payload
            {
                build_guid = build_guid,
                count = count
            };

            return data != null;
        }

        private string build_guid;
        private int count;
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

        EditorAnalytics.SendAnalytic(new AudioRandomAnalytic(report.summary.guid.ToString(), count));
    }
    
   }
