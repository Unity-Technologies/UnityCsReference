// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Analytics;

namespace UnityEditor.Audio.Analytics;

[InitializeOnLoad]
class AudioRandomContainerQuitAnalyticsEvent
{
    [AnalyticInfo(eventName: "audioRandomContainerQuit", vendorKey: "unity.audio", maxEventsPerHour: 60, maxNumberOfElements: 1)]
    internal class AudioRandomAnalytic : IAnalytic
    {
        public AudioRandomAnalytic(int count)
        {
            this.count = count;
        }

        [Serializable]
        struct Payload : IAnalytic.IData
        {
            public int count;
        }


        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = new Payload
            {
                count = count
            };

            return data != null;
        }

        private int count;
    }

    static AudioRandomContainerQuitAnalyticsEvent()
    {
        EditorApplication.wantsToQuit += OnEditorApplicationWantsToQuit;
    }

    static bool OnEditorApplicationWantsToQuit()
    {
        SendEvent();
        return true;
    }

    static void SendEvent()
    {
        if (!EditorAnalytics.enabled || AudioSettings.unityAudioDisabled)
        {
            return;
        }

        var assetPaths = AssetDatabase.FindAssets("t:AudioRandomContainer");
        EditorAnalytics.SendAnalytic(new AudioRandomAnalytic(assetPaths.Length));
    }
}
