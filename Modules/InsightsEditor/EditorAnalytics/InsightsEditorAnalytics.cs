// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.EngineDiagnostics;
using UnityEngine;
using UnityEngine.Analytics;

namespace UnityEditor.InsightsEditor.EditorAnalytics;

[AnalyticInfo(eventName: "engineDiagnostics", vendorKey: "unity.enginediagnostics", version: 2)]
internal class InsightsEditorAnalytic : IAnalytic, IPostprocessBuildWithReport
{
    private const string k_NewProjectFlag = "-createproject";

    [NoAutoStaticsCleanup] // caches a one-time read of process command-line args, which never change for the process lifetime (incl. across code reload)
    static bool s_HasCheckedCreateProjectFlag;
    [NoAutoStaticsCleanup] // cached -createproject command-line flag; immutable for the process lifetime, safe to persist across code reload
    static bool s_IsCreateProjectFlagSet;

    InsightsEditorAnalyticsEvent m_Data;

    public InsightsEditorAnalytic() { }

    public InsightsEditorAnalytic(InsightsEditorAnalyticsEvent data)
    {
        m_Data = data;
    }

    public bool TryGatherData(out IAnalytic.IData data, out Exception error)
    {
        error = null;
        data = m_Data;
        return data != null;
    }

    public static void LogAppInsights(InsightsEditorAnalyticsEvent data)
    {
        PopulateCommonData(ref data);
        UnityEditor.EditorAnalytics.SendAnalytic(new InsightsEditorAnalytic(data));
    }

    private static void PopulateCommonData(ref InsightsEditorAnalyticsEvent data)
    {
        if (!s_HasCheckedCreateProjectFlag)
        {
            var args = Environment.GetCommandLineArgs();
            s_IsCreateProjectFlagSet = Array.Exists(args, arg => arg == k_NewProjectFlag);
            s_HasCheckedCreateProjectFlag = true;
        }

        data.projectCreationFlag = s_IsCreateProjectFlagSet;
        data.cloudProjectId = CloudProjectSettings.projectId;
    }

    [Serializable]
    internal class InsightsEditorAnalyticsEvent : IAnalytic.IData
    {
        public ActionType ActionType { set => action = value.ToString(); }

        [SerializeField]
        internal bool projectCreationFlag;
        [SerializeField]
        internal string cloudProjectId = string.Empty;
        [SerializeField]
        internal string action = string.Empty;
        public InteractionContext interactionContext;
        public BuildProfileEngineDiagnosticsStateChange buildProfileEngineDiagnosticsStateChange;
        public BuildProjectInfo buildProjectInfo;
    }

    [Serializable]
    internal enum ActionType
    {
        BuildProject,
        ChangeBuildProfileEngineDiagnosticsState
    }

    [Serializable]
    internal class InteractionContext
    {
        public string platformGuid;
        public string profileName;
    }

    [Serializable]
    internal class BuildProfileEngineDiagnosticsStateChange
    {
        public BuildProfileEngineDiagnosticsState FromState { set => fromState = value.ToString(); }
        public BuildProfileEngineDiagnosticsState ToState { set => toState = value.ToString(); }

        [SerializeField]
        internal string fromState;
        [SerializeField]
        internal string toState;
    }

    [Serializable]
    internal class BuildProjectInfo
    {
        public bool EngineDiagnosticsEnabled { set => engineDiagnosticsEnabled = value.ToString(); }

        [SerializeField]
        internal string engineDiagnosticsEnabled;
        [SerializeField]
        internal string buildGuid;
        [SerializeField]
        internal string buildSessionGuid;
    }

    public int callbackOrder { get; }

    public void OnPostprocessBuild(BuildReport report)
    {
        LogAppInsights(new InsightsEditorAnalyticsEvent
        {
            ActionType = ActionType.BuildProject,
            buildProjectInfo = new BuildProjectInfo
            {
                EngineDiagnosticsEnabled = EngineDiagnosticsSettings.IsEnabled(),
                buildGuid = report.summary.guid.ToString(),
                buildSessionGuid = EditorApplication.buildSessionGUID.ToString()
            }
        });
    }
}
