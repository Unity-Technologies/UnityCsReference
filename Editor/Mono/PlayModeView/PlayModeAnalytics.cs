// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

namespace UnityEditor
{
    internal class PlayModeAnalytics
    {
        class BaseData
        {
            public string PlayModeEvent;
        }

        class SimulatorEnableData : BaseData
        {
            public string[] SimulatorPlugins;
        }

        class SimulatorDeviceData : BaseData
        {
            public string DeviceName;
        }

        private static bool s_EventRegistered;
        const int k_MaxEventsPerHour = 1000;
        const int k_MaxNumberOfElements = 100;
        const string k_VendorKey = "unity.playModeUsage";
        const string k_EventName = "playModeUsage";

        static bool EnableAnalytics()
        {
            if (!UnityEngine.Analytics.Analytics.enabled)
                return false;

            if (!s_EventRegistered)
            {
                AnalyticsResult result = EditorAnalytics.RegisterEventWithLimit(k_EventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);
                if (result == AnalyticsResult.Ok)
                    s_EventRegistered = true;
            }

            return s_EventRegistered;
        }

        public static void GameViewEnableEvent()
        {
            SendPlayModeEvent(new BaseData() {PlayModeEvent = "Game OnEnable"});
        }

        public static void GameViewDisableEvent()
        {
            SendPlayModeEvent(new BaseData() {PlayModeEvent = "Game OnDisable"});
        }

        public static void SimulatorEnableEvent(string[] pluginNames)
        {
            SendPlayModeEvent(new SimulatorEnableData() {PlayModeEvent = "Simulator OnEnable", SimulatorPlugins = pluginNames});
        }

        public static void SimulatorDisableEvent()
        {
            SendPlayModeEvent(new BaseData() {PlayModeEvent = "Simulator OnDisable"});
        }

        public static void SimulatorSelectDeviceEvent(string deviceName)
        {
            SendPlayModeEvent(new SimulatorDeviceData() {PlayModeEvent = "Simulator Device", DeviceName = deviceName});
        }

        private static void SendPlayModeEvent(BaseData data)
        {
            if (!EnableAnalytics())
                return;

            EditorAnalytics.SendEventWithLimit(k_EventName, data);
        }
    }
}
