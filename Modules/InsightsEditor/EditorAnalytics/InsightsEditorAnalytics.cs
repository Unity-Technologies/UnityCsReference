// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.EngineDiagnostics;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Bindings;

namespace UnityEditor.InsightsEditor.EditorAnalytics;

[VisibleToOtherModules]
[AnalyticInfo(eventName: "engineDiagnostics", vendorKey: "unity.enginediagnostics", version: 2)]
internal class InsightsEditorAnalytic : IAnalytic, IPostprocessBuildWithReport
{
    private const string k_NewProjectFlag = "-createproject";

    static bool s_HasCheckedCreateProjectFlag;
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
    [VisibleToOtherModules]
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
        public ProjectSettingsEngineDiagnosticsEnabledChange projectSettingsEngineDiagnosticsEnabledChange;
        public BuildProjectInfo buildProjectInfo;
        public DisablementPopupInteraction disablementPopupInteraction;
    }

    [Serializable]
    [VisibleToOtherModules]
    internal enum ActionType
    {
        BuildProject,
        DisablementPopupInteraction,
        EnterProjectSettingsMenu,
        ChangeEngineDiagnosticsEnabled,
        ChangeBuildProfileEngineDiagnosticsState
    }

    [Serializable]
    [VisibleToOtherModules]
    internal enum PopupInteraction
    {
        Accept,
        Cancel
    }

    [Serializable]
    [VisibleToOtherModules]
    internal class InteractionContext
    {
        public string platformGuid;
        public string profileName;
    }

    [Serializable]
    [VisibleToOtherModules]
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
    [VisibleToOtherModules]
    internal class ProjectSettingsEngineDiagnosticsEnabledChange
    {
        public bool FromValue { set => fromValue = value.ToString(); }
        public bool ToValue { set => toValue = value.ToString(); }

        [SerializeField]
        internal string fromValue;
        [SerializeField]
        internal string toValue;
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

    [Serializable]
    internal class DisablementPopupInteraction
    {
        public PopupInteraction PopupInteraction { set => popupInteraction = value.ToString(); }

        [SerializeField]
        internal string popupInteraction;
    }

    public int callbackOrder { get; }

    public void OnPostprocessBuild(BuildReport report)
    {
        LogAppInsights(new InsightsEditorAnalyticsEvent
        {
            ActionType = ActionType.BuildProject,
            buildProjectInfo = new BuildProjectInfo
            {
                EngineDiagnosticsEnabled = EngineDiagnosticsSettings.enabled,
                buildGuid = report.summary.guid.ToString(),
                buildSessionGuid = EditorApplication.buildSessionGUID.ToString()
            }
        });
    }
}
