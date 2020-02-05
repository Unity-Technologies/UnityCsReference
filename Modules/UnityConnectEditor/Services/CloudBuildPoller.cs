// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal class CloudBuildPoller
    {
        const string k_JsonNodeNameBuild = "build";
        const string k_JsonNodeNameBuildTargetId = "buildtargetid";
        const string k_JsonNodeNameBuildTargetName = "buildTargetName";
        const string k_JsonNodeNameBuildStatus = "buildStatus";

        const string k_BuildStatusCanceled = "canceled";
        const string k_BuildStatusFailure = "failure";
        const string k_BuildStatusQueued = "queued";
        const string k_BuildStatusSentToBuilder = "sentToBuilder";
        const string k_BuildStatusSentRestarted = "restarted";
        const string k_BuildStatusSuccess = "success";
        const string k_BuildStatusStarted = "started";
        const string k_BuildStatusStartedMessage = "building";
        const string k_BuildStatusUnknown = "unknown";

        const int k_IntervalSeconds = 15;

        const string k_BuildFinishedWithStatusMsg = "Build #{0} {1} {2}.";
        const string k_MessageErrorForApiStatusData = "An unexpected error occurred while querying Cloud Build for api status. See the console for more information.";

        bool m_Enabled;
        bool m_EnabledOnce;
        TickTimerHelper m_Timer = new TickTimerHelper(k_IntervalSeconds);
        string m_PollingUrl;
        List<string> m_BuildsToReportOn = new List<string>();

        static readonly CloudBuildPoller k_Instance;

        public static CloudBuildPoller instance => k_Instance;

        internal bool enabled => m_Enabled;
        internal bool enabledOnce => m_EnabledOnce;

        static CloudBuildPoller()
        {
            k_Instance = new CloudBuildPoller();
        }

        internal void Enable(string pollingUrl)
        {
            if (!m_Enabled)
            {
                m_EnabledOnce = true;
                m_PollingUrl = pollingUrl;
                m_Enabled = true;
                EditorApplication.update += Update;
                m_Timer.Reset();
            }
        }

        internal void Disable(bool resetPoller = false)
        {
            if (m_Enabled)
            {
                m_Enabled = false;
                EditorApplication.update -= Update;
            }

            if (resetPoller)
            {
                m_EnabledOnce = false;
            }
        }

        void Update()
        {
            if (m_Timer.DoTick())
            {
                var getCurrentBuildTargetStatusRequest = new UnityWebRequest(m_PollingUrl,
                    UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer() };
                getCurrentBuildTargetStatusRequest.suppressErrorsToConsole = true;
                getCurrentBuildTargetStatusRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {UnityConnect.instance.GetUserInfo().accessToken}");
                var operation = getCurrentBuildTargetStatusRequest.SendWebRequest();
                operation.completed += asyncOperation =>
                {
                    try
                    {
                        if (ServicesUtils.IsUnityWebRequestReadyForJsonExtract(getCurrentBuildTargetStatusRequest))
                        {
                            try
                            {
                                var jsonParser = new JSONParser(getCurrentBuildTargetStatusRequest.downloadHandler.text);
                                var json = jsonParser.Parse();
                                var buildList = json.AsList();
                                var trackedBuilds = new List<string>(m_BuildsToReportOn);
                                if (buildList.Count > 0)
                                {
                                    foreach (var rawBuild in buildList)
                                    {
                                        var build = rawBuild.AsDict();
                                        var buildNumber = build[k_JsonNodeNameBuild].AsFloat().ToString();
                                        var buildId = build[k_JsonNodeNameBuildTargetId].AsString() + "_" + buildNumber;
                                        var buildStatus = build[k_JsonNodeNameBuildStatus].AsString().ToLower();

                                        if (trackedBuilds.Contains(buildId))
                                        {
                                            trackedBuilds.Remove(buildId);
                                        }

                                        if (m_BuildsToReportOn.Contains(buildId)
                                            && (k_BuildStatusCanceled.Equals(buildStatus)
                                                || k_BuildStatusFailure.Equals(buildStatus)
                                                || k_BuildStatusSuccess.Equals(buildStatus)
                                                || k_BuildStatusUnknown.Equals(buildStatus)
                                            )
                                        )
                                        {
                                            if (!k_BuildStatusStarted.Equals(buildStatus)
                                                && !k_BuildStatusUnknown.Equals(buildStatus))
                                            {
                                                m_BuildsToReportOn.Remove(buildId);
                                            }
                                            var buildTargetName = build[k_JsonNodeNameBuildTargetName].AsString();

                                            var severity = Notification.Severity.Info;
                                            var message = string.Empty;
                                            switch (buildStatus)
                                            {
                                                case k_BuildStatusCanceled:
                                                    severity = Notification.Severity.Warning;
                                                    message = string.Format(L10n.Tr(k_BuildFinishedWithStatusMsg), buildNumber, buildTargetName, k_BuildStatusCanceled);
                                                    Debug.LogWarning(message);
                                                    break;
                                                case k_BuildStatusFailure:
                                                    severity = Notification.Severity.Error;
                                                    message = string.Format(L10n.Tr(k_BuildFinishedWithStatusMsg), buildNumber, buildTargetName, k_BuildStatusFailure);
                                                    Debug.LogError(message);
                                                    break;
                                                case k_BuildStatusStarted:
                                                    message = string.Format(L10n.Tr(k_BuildFinishedWithStatusMsg), buildNumber, buildTargetName, k_BuildStatusStartedMessage);
                                                    Debug.Log(message);
                                                    break;
                                                case k_BuildStatusSuccess:
                                                    message = string.Format(L10n.Tr(k_BuildFinishedWithStatusMsg), buildNumber, buildTargetName, k_BuildStatusSuccess);
                                                    Debug.Log(message);
                                                    break;
                                                case k_BuildStatusUnknown:
                                                    message = string.Format(L10n.Tr(k_BuildFinishedWithStatusMsg), buildNumber, buildTargetName, k_BuildStatusUnknown);
                                                    Debug.LogWarning(message);
                                                    break;
                                            }

                                            NotificationManager.instance.Publish(Notification.Topic.BuildService, severity, message);
                                        }
                                        else if (!m_BuildsToReportOn.Contains(buildId)
                                                 && (k_BuildStatusQueued.Equals(buildStatus)
                                                     || k_BuildStatusStarted.Equals(buildStatus)
                                                     || k_BuildStatusSentToBuilder.Equals(buildStatus)
                                                     || k_BuildStatusSentRestarted.Equals(buildStatus)
                                                 )
                                        )
                                        {
                                            if (k_BuildStatusSentRestarted.Equals(buildStatus))
                                            {
                                                var buildTargetName = build[k_JsonNodeNameBuildTargetName].AsString();
                                                var message = string.Format(L10n.Tr(k_BuildFinishedWithStatusMsg), buildNumber, buildTargetName, k_BuildStatusSentRestarted);
                                                Debug.Log(message);
                                                NotificationManager.instance.Publish(Notification.Topic.BuildService, Notification.Severity.Info, message);
                                            }

                                            m_BuildsToReportOn.Add(buildId);
                                        }
                                    }

                                    //If a build vanishes, we don't want to keep investigating it
                                    foreach (var missingTrackedBuild in trackedBuilds)
                                    {
                                        m_BuildsToReportOn.Remove(missingTrackedBuild);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                NotificationManager.instance.Publish(
                                    Notification.Topic.BuildService,
                                    Notification.Severity.Error,
                                    L10n.Tr(k_MessageErrorForApiStatusData));
                                Debug.LogException(ex);
                            }
                        }
                    }
                    finally
                    {
                        getCurrentBuildTargetStatusRequest.Dispose();
                    }
                };
            }
        }
    }
}
