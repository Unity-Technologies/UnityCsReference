// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;
using System.Collections.Generic;
using UnityEditor.Web;
using UnityEditorInternal;
using UnityEditor;

namespace UnityEditor.Connect
{
    internal class UnityConnectServiceCollection
    {
        private static UnityConnectServiceCollection s_UnityConnectEditor; //singleton
        private const string kDrawerContainerTitle = "Services";

        private string m_CurrentServiceName = "";
        private string m_CurrentPageName = "";

        private readonly Dictionary<string, UnityConnectServiceData> m_Services;

        private UnityConnectServiceCollection()
        {
            m_Services = new Dictionary<string, UnityConnectServiceData>();
        }

        private void Init()
        {
            //Add this to the v8 global table so javascript can call ShowService("")
            JSProxyMgr.GetInstance().AddGlobalObject("UnityConnectEditor", this);
        }

        public bool isDrawerOpen
        {
            get
            {
                return false;
            }
        }
        private void EnsureDrawerIsVisible(bool forceFocus)
        {
        }

        public void CloseServices()
        {
            UnityConnect.instance.ClearCache();
        }

        public void ReloadServices()
        {
        }

        public static UnityConnectServiceCollection instance
        {
            get
            {
                if (s_UnityConnectEditor == null)
                {
                    s_UnityConnectEditor = new UnityConnectServiceCollection();
                    s_UnityConnectEditor.Init();
                }
                return s_UnityConnectEditor;
            }
        }

        [RequiredByNativeCode]
        public static void StaticEnableService(string serviceName, bool enabled)
        {
            instance.EnableService(serviceName, enabled);
        }

        [RequiredByNativeCode]
        public static void OnServicesConfigChanged()
        {
            instance.ReloadWindow();
        }

        public void ReloadWindow()
        {
        }

        public bool AddService(UnityConnectServiceData cloudService)
        {
            if (m_Services.ContainsKey(cloudService.serviceName))
                return false;

            m_Services[cloudService.serviceName] = cloudService;
            return true;
        }

        public bool RemoveService(string serviceName)
        {
            if (!m_Services.ContainsKey(serviceName))
                return false;

            return m_Services.Remove(serviceName);
        }

        public bool ServiceExist(string serviceName)
        {
            return m_Services.ContainsKey(serviceName);
        }

        public bool ShowService(string serviceName, bool forceFocus, string atReferrer)
        {
            return ShowService(serviceName, "", forceFocus, atReferrer);
        }

        [Serializable]
        public struct ShowServiceState
        {
            public string service;
            public string page;
            public string referrer;
        }

        public bool ShowService(string serviceName, string atPage, bool forceFocus, string atReferrer)
        {
            if (!m_Services.ContainsKey(serviceName))
            {
                return false;
            }

            ConnectInfo state = UnityConnect.instance.connectInfo;
            m_CurrentServiceName = GetActualServiceName(serviceName, state);
            m_CurrentPageName = atPage;
            EditorAnalytics.SendEventShowService(new ShowServiceState() { service = m_CurrentServiceName, page = atPage, referrer = atReferrer});
            EnsureDrawerIsVisible(forceFocus);
            return true;
        }

        private string GetActualServiceName(string desiredServiceName, ConnectInfo state)
        {
            if (!state.online)
                return ErrorHubAccess.kServiceName;

            if (!state.ready)
                return HubAccess.kServiceName;

            if (state.maintenance)
                return ErrorHubAccess.kServiceName;

            if ((desiredServiceName != HubAccess.kServiceName) && (state.online && !state.loggedIn))
            {
                return HubAccess.kServiceName;
            }

            if ((desiredServiceName == ErrorHubAccess.kServiceName) && state.online)
            {
                return HubAccess.kServiceName;
            }

            if (string.IsNullOrEmpty(desiredServiceName))
            {
                return HubAccess.kServiceName;
            }

            return desiredServiceName;
        }

        public void EnableService(string name, bool enabled)
        {
            if (!m_Services.ContainsKey(name))
                return;

            m_Services[name].EnableService(enabled);
        }

        public string GetUrlForService(string serviceName)
        {
            return m_Services.ContainsKey(serviceName) ? m_Services[serviceName].serviceUrl : String.Empty;
        }

        public UnityConnectServiceData GetServiceFromUrl(string searchUrl)
        {
            return m_Services.FirstOrDefault(kvp => kvp.Value.serviceUrl == searchUrl).Value;
        }

        public List<string> GetAllServiceNames()
        {
            return m_Services.Keys.ToList();
        }

        public List<string> GetAllServiceUrls()
        {
            return m_Services.Values.Select(unityConnectData => unityConnectData.serviceUrl).ToList();
        }

        public class ServiceInfo
        {
            public ServiceInfo(string name, string url, string unityPath, bool enabled)
            {
                this.name = name;
                this.url = url;
                this.unityPath = unityPath;
                this.enabled = enabled;
            }

            public string name;
            public string url;
            public string unityPath;
            public bool enabled;
        }

        public ServiceInfo[] GetAllServiceInfos()
        {
            if (UnityConnect.instance.isDisableServicesWindow)
            {
                return new ServiceInfo[0];
            }

            return m_Services.Select(item => new ServiceInfo(item.Value.serviceName, item.Value.serviceUrl, item.Value.serviceJsGlobalObjectName, item.Value.serviceJsGlobalObject.IsServiceEnabled())).ToArray();
        }

        public WebView GetWebViewFromServiceName(string serviceName)
        {
            return null;
        }

        public void UnbindAllServices()
        {
            foreach (var service in m_Services.Values)
            {
                service.OnProjectUnbound();
            }
        }
    }
}
