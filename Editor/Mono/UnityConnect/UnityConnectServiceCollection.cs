// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Web;
using UnityEditorInternal;
using UnityEditor;

namespace UnityEditor.Connect
{
    internal class UnityConnectServiceCollection
    {
        private static UnityConnectServiceCollection s_UnityConnectEditor; //singleton
        private static UnityConnectEditorWindow s_UnityConnectEditorWindow; //This is the drawer
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

            // We want to show the service window when we create a project, but not every time we open one.
            if (Application.HasARGV("createProject"))
                ShowService(HubAccess.kServiceName, true, "init_create_project");
        }

        public bool isDrawerOpen
        {
            get
            {
                var wins = Resources.FindObjectsOfTypeAll(typeof(UnityConnectEditorWindow)) as UnityConnectEditorWindow[];
                return wins != null && wins.Any(win => win != null);
            }
        }
        private void EnsureDrawerIsVisible(bool forceFocus)
        {
            //Create the container in case it doesnt exist
            if (s_UnityConnectEditorWindow == null || !s_UnityConnectEditorWindow.UrlsMatch(GetAllServiceUrls()))
            {
                var fixTitle = kDrawerContainerTitle;

                var panelEnv = UnityConnectPrefs.GetServiceEnv(m_CurrentServiceName);
                if (panelEnv != UnityConnectPrefs.kProductionEnv)
                {
                    fixTitle += " [" + UnityConnectPrefs.kEnvironmentFamilies[panelEnv] + "]";
                }

                s_UnityConnectEditorWindow = UnityConnectEditorWindow.Create(fixTitle, GetAllServiceUrls());
                s_UnityConnectEditorWindow.ErrorUrl = m_Services[ErrorHubAccess.kServiceName].serviceUrl;
                s_UnityConnectEditorWindow.minSize = new Vector2(275, 50);
            }
            //Since s_UnityConnectEditorWindow.currentUrl is a property that load a page we must build the url before changing it
            var newUrl = m_Services[m_CurrentServiceName].serviceUrl;
            if (m_CurrentPageName.Length > 0)
            {
                newUrl += ("/#/" + m_CurrentPageName);
            }
            s_UnityConnectEditorWindow.currentUrl = newUrl;
            s_UnityConnectEditorWindow.ShowTab();

            if (InternalEditorUtility.isApplicationActive && forceFocus)
                s_UnityConnectEditorWindow.Focus();
        }

        public void CloseServices()
        {
            if (s_UnityConnectEditorWindow != null)
            {
                s_UnityConnectEditorWindow.Close();
                s_UnityConnectEditorWindow = null;
            }

            UnityConnect.instance.ClearCache();
        }

        public void ReloadServices()
        {
            if (s_UnityConnectEditorWindow != null)
            {
                s_UnityConnectEditorWindow.Reload();
            }
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

        public static void StaticEnableService(string serviceName, bool enabled)
        {
            instance.EnableService(serviceName, enabled);
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
            return m_Services.Select(item => new ServiceInfo(item.Value.serviceName, item.Value.serviceUrl, item.Value.serviceJsGlobalObjectName, item.Value.serviceJsGlobalObject.IsServiceEnabled())).ToArray();
        }

        public WebView GetWebViewFromServiceName(string serviceName)
        {
            if (s_UnityConnectEditorWindow == null || !s_UnityConnectEditorWindow.UrlsMatch(GetAllServiceUrls()))
                return null;

            if (!m_Services.ContainsKey(serviceName))
                return null;

            ConnectInfo state = UnityConnect.instance.connectInfo;
            string actualName = GetActualServiceName(serviceName, state);
            var url = m_Services[actualName].serviceUrl;
            return s_UnityConnectEditorWindow.GetWebViewFromURL(url);
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
