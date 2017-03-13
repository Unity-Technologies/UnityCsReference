// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEditor.Connect;

namespace UnityEditor.Web
{
    [InitializeOnLoad]
    internal class ErrorHubAccess : CloudServiceAccess
    {
        public const string kServiceName = "ErrorHub";
        private static string kServiceUrl = "file://" + EditorApplication.userJavascriptPackagesPath + "unityeditor-cloud-hub/dist/index.html?failure=unity_connect";

        public static ErrorHubAccess instance { get; private set; }
        public string errorMessage { get; set; }

        public override string GetServiceName()
        {
            return kServiceName;
        }

        static ErrorHubAccess()
        {
            instance = new ErrorHubAccess();
            var serviceData = new UnityConnectServiceData(kServiceName, kServiceUrl, instance, "unity/project/cloud/errorhub");
            UnityConnectServiceCollection.instance.AddService(serviceData);
        }
    }
}

