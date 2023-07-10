// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

namespace UnityEditor
{

    internal class PlayModeAnalytics
    {
        internal class BaseData : IAnalytic.IData
        {
            [SerializeField] public string PlayModeEvent;
        }

        internal class SimulatorEnableData : BaseData
        {
            [SerializeField] public string[] SimulatorPlugins;
        }

        internal class SimulatorDeviceData : BaseData
        {
            [SerializeField] public string DeviceName;
        }

        [AnalyticInfo(eventName: "playModeUsage", vendorKey: "unity.playModeUsage")]
        internal class Analytic : IAnalytic
        {
            public Analytic(BaseData data)
            {
                m_data = data;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_data;
                return data != null;
            }

            private BaseData m_data = null;
        }

        public static void GameViewEnableEvent()
        {
            SendPlayModeEvent(new BaseData() { PlayModeEvent = "Game OnEnable" });
        }

        public static void GameViewDisableEvent()
        {
            SendPlayModeEvent(new BaseData() { PlayModeEvent = "Game OnDisable" });
        }

        public static void SimulatorEnableEvent(string[] pluginNames)
        {
            SendPlayModeEvent(new SimulatorEnableData() { PlayModeEvent = "Simulator OnEnable", SimulatorPlugins = pluginNames });
        }

        public static void SimulatorDisableEvent()
        {
            SendPlayModeEvent(new BaseData() { PlayModeEvent = "Simulator OnDisable" });
        }

        public static void SimulatorSelectDeviceEvent(string deviceName)
        {
            SendPlayModeEvent(new SimulatorDeviceData() { PlayModeEvent = "Simulator Device", DeviceName = deviceName });
        }

        private static void SendPlayModeEvent(BaseData data)
        {
            EditorAnalytics.SendAnalytic(new PlayModeAnalytics.Analytic(data));
        }
    }
}
