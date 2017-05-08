// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEditor.Connect;

namespace UnityEditor.Web
{
    [InitializeOnLoad]
    internal class BuildAccess : CloudServiceAccess
    {
        private const string kServiceName = "Build";
        private const string kServiceDisplayName = "Unity Build";
        private const string kServiceUrl = "https://public-cdn.cloud.unity3d.com/editor/production/cloud/build";

        public override string GetServiceName()
        {
            return kServiceName;
        }

        public override string GetServiceDisplayName()
        {
            return kServiceDisplayName;
        }

        public void ShowBuildForCommit(string commitId)
        {
            ShowServicePage();

            string eventCmd = string.Format("window.unityEvents ? window.unityEvents.broadcast('build.showForCommit', '{0}'): '';", commitId);
            var webview = GetWebView();
            webview.ExecuteJavascript(eventCmd);
        }

        static BuildAccess()
        {
            var serviceData = new UnityConnectServiceData(kServiceName, kServiceUrl, new BuildAccess(), "unity/project/cloud/build");
            UnityConnectServiceCollection.instance.AddService(serviceData);
        }
    }
}

