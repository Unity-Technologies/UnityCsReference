// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Audio.Analytics;

[InitializeOnLoad]
class AudioRandomContainerQuitAnalyticsEvent
{
    const string k_EventName = "audioRandomContainerQuit";
    const int k_MaxEventsPerHour = 60;
    const int k_MaxNumberOfElements = 1;

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
        if (!EditorAnalytics.enabled || AudioSettings.unityAudioDisabled || !Initialized)
        {
            return;
        }

        var assetPaths = AssetDatabase.FindAssets("t:AudioRandomContainer");
        var payload = new Payload { count = assetPaths.Length };
        EditorAnalytics.SendEventWithLimit(k_EventName, payload);
    }

    struct Payload
    {
        public int count;
    }
}
