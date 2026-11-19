// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.Shaders
{
    internal static class ShaderBuildSettingsAnalytics
    {
        const string k_EventName = "shaderBuildSettingsApplied";
        const string k_VendorKey = "unity.graphics";
        const int k_Version = 1;

        // Field names match the PascalCase fields of the native shaderBuildInfo event, so both events read alike.
        [Serializable]
        internal class Data : IAnalytic.IData
        {
            public string BuildTarget;
            public bool UsesSettingsOnBuildProfile;
            public bool UsesFastBuildMode;
            public bool UsesKeywordOverrides;
            public bool UsesConstantDefines;
            public bool UsesCompilerSettings;
        }

        [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey, version: k_Version)]
        class SettingsAppliedAnalytic : IAnalytic
        {
            readonly Data m_Data;
            public SettingsAppliedAnalytic(Data data) => m_Data = data;

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }
        }

        internal static Data CreateData(BuildTarget buildTarget, bool usesSettingsOnBuildProfile, bool usesFastBuildMode,
            bool usesKeywordOverrides, bool usesConstantDefines, ShaderBuildSettings.ShaderCompilerSettings[] compilerSettings)
        {
            return new Data
            {
                BuildTarget = buildTarget.ToString(),
                UsesSettingsOnBuildProfile = usesSettingsOnBuildProfile,
                UsesFastBuildMode = usesFastBuildMode,
                UsesKeywordOverrides = usesKeywordOverrides,
                UsesConstantDefines = usesConstantDefines,
                UsesCompilerSettings = Array.Exists(compilerSettings, row => !ShaderBuildSettings.IsAllDefaultRow(row)),
            };
        }

        internal static void SendSettingsApplied(Data data)
        {
            EditorAnalytics.SendAnalytic(new SettingsAppliedAnalytic(data));
        }
    }
}
