// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEditor.Connect;
using UnityEngine;
using UnityEditor.CrashReporting;

namespace UnityEditor.Web
{
    [InitializeOnLoad]
    internal class CrashReportingAccess : CloudServiceAccess
    {
        private const string kServiceName = "Game Performance";
        private const string kServiceDisplayName = "Game Performance";
        private const string kServiceUrl = "https://public-cdn.cloud.unity3d.com/editor/production/cloud/crash";

        public override string GetServiceName()
        {
            return kServiceName;
        }

        public override string GetServiceDisplayName()
        {
            return kServiceDisplayName;
        }

        override public bool IsServiceEnabled()
        {
            return CrashReportingSettings.enabled;
        }

        [Serializable]
        public struct CrashReportingServiceState { public bool crash_reporting; }
        override public void EnableService(bool enabled)
        {
            if (CrashReportingSettings.enabled != enabled)
            {
                CrashReportingSettings.enabled = enabled;
                EditorAnalytics.SendEventServiceInfo(new CrashReportingServiceState() { crash_reporting = enabled });
            }
        }

        static CrashReportingAccess()
        {
            var serviceData = new UnityConnectServiceData(kServiceName, kServiceUrl, new CrashReportingAccess(), "unity/project/cloud/crashreporting");
            UnityConnectServiceCollection.instance.AddService(serviceData);
        }

        public bool GetCaptureEditorExceptions()
        {
            return CrashReportingSettings.captureEditorExceptions;
        }

        public void SetCaptureEditorExceptions(bool captureEditorExceptions)
        {
            CrashReportingSettings.captureEditorExceptions = captureEditorExceptions;
        }
    }
}

