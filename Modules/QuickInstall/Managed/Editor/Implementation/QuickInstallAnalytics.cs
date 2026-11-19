// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.QuickInstall
{
    internal enum InstallMethod
    {
        Unknown = 0,
        Assets,
        MenuItem,
        PackageManager,
        ProjectSettings,
    }

    // Schema com.unity3d.data.schemas.editor.analytics.quickinstall_packageInstalled_v1
    [AnalyticInfo(eventName: "quickinstall_packageInstalled", vendorKey: "unity.quickInstall")]
    internal class QuickInstallAnalytic : IAnalytic
    {
        [Serializable]
        class QuickInstallData : IAnalytic.IData
        {
            public string packageName;
            public string packageVersion;
            public string installationMethod;
        }
        readonly QuickInstallData m_Data = new();
        string m_PackageName { get => m_Data.packageName; set => m_Data.packageName = value; }
        string m_PackageVersion { get => m_Data.packageVersion; set => m_Data.packageVersion = value; }
        string m_InstallationMethod { get => m_Data.installationMethod; set => m_Data.installationMethod = value; }
        readonly AnalyticConfig m_Config;

        internal QuickInstallAnalytic(string packageName, AnalyticConfig config)
        {
            m_PackageName = packageName;
            m_Config = config;
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = m_Data;
            return true;
        }

        internal void SendEvent(string packageVersion, InstallMethod installationMethod)
        {
            if (installationMethod == InstallMethod.Assets && !m_Config.SendAssetInstallAnalytic)
                return;
            
            m_PackageVersion = packageVersion;
            m_InstallationMethod = installationMethod.ToString();
            EditorAnalytics.SendAnalytic(this);
        }

    }
}
