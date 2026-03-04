// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.QuickInstall
{
    internal enum InstallMethod
    {
        Assets,
        MenuItem,
        PackageManager,
        ProjectSettings,
    }

    internal abstract class QuickInstallAnalytic : IAnalytic
    {
        [Serializable]
        class QuickInstallData : IAnalytic.IData
        {
            public string installationMethod;
        }
        readonly QuickInstallData m_Data = new();
        string m_InstallationMethod { get => m_Data.installationMethod; set => m_Data.installationMethod = value; }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = m_Data;
            return true;
        }

        internal static void SendEvent(QuickInstallAnalytic analytic, InstallMethod installationMethod)
        {
            analytic.m_InstallationMethod = installationMethod.ToString();
            EditorAnalytics.SendAnalytic(analytic);
        }
    }
}
