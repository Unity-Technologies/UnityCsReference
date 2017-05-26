// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEditor.Connect;

namespace UnityEditor.Web
{
    [InitializeOnLoad]
    internal class HubAccess : CloudServiceAccess
    {
        public const string kServiceName = "Hub";
        private const string kServiceDisplayName = "Services";
        private const string kServiceUrl = "https://public-cdn.cloud.unity3d.com/editor/production/cloud/hub";

        static private HubAccess s_Instance;
        public static HubAccess instance
        {
            get
            {
                return s_Instance;
            }
        }

        public override string GetServiceName()
        {
            return kServiceName;
        }

        public override string GetServiceDisplayName()
        {
            return kServiceDisplayName;
        }

        public UnityConnectServiceCollection.ServiceInfo[] GetServices()
        {
            return UnityConnectServiceCollection.instance.GetAllServiceInfos();
        }

        public void ShowService(string name)
        {
            UnityConnectServiceCollection.instance.ShowService(name, true, "show_service_method");
        }

        public void EnableCloudService(string name, bool enabled)
        {
            UnityConnectServiceCollection.instance.EnableService(name, enabled);
        }

        static HubAccess()
        {
            s_Instance = new HubAccess();
            var serviceData = new UnityConnectServiceData(kServiceName, kServiceUrl, s_Instance, "unity/project/cloud/hub");
            UnityConnectServiceCollection.instance.AddService(serviceData);
        }

        [MenuItem("Window/Services %0", false, 1999)]
        private static void ShowMyWindow()
        {
            UnityConnectServiceCollection.instance.ShowService(kServiceName, true, "window_menu_item");
        }
    }
}

