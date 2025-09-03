// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.LevelPlay
{
    [Serializable]
    internal class LevelPlayQuickInstallData : IAnalytic.IData
    {
        public string installationMethod;
    }

    [AnalyticInfo(eventName: "levelplay_quickinstall_packageInstalled", vendorKey: "unity.quickInstall")]
    internal class LevelPlayQuickInstallAnalytic : IAnalytic
    {
        private LevelPlayQuickInstallData m_data = new LevelPlayQuickInstallData();

        public LevelPlayQuickInstallAnalytic(string installationMethod)
        {
            m_data.installationMethod = installationMethod;
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = m_data;
            return data != null;
        }

        public static void SendEvent(string installationMethod)
        {
            LevelPlayQuickInstallAnalytic analytic = new LevelPlayQuickInstallAnalytic(installationMethod);
            EditorAnalytics.SendAnalytic(analytic);
        }
    }
}
