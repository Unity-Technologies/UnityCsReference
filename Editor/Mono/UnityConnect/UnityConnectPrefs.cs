// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Connect
{
    internal class UnityConnectPrefs
    {
        public static string[] kEnvironmentFamilies = new string[] {"Production", "Staging", "Dev", "Custom"};
        public const int kProductionEnv = 0;
        public const int kCustomEnv = 3;

        public const string kSvcEnvPref = "CloudPanelServer";
        public const string kSvcCustomUrlPref = "CloudPanelCustomUrl";
        public const string kSvcCustomPortPref = "CloudPanelCustomPort";

        protected class CloudPanelPref
        {
            public CloudPanelPref(string serviceName)
            {
                m_ServiceName = serviceName;
                m_CloudPanelServer = GetServiceEnv(m_ServiceName);
                m_CloudPanelCustomUrl = EditorPrefs.GetString(ServicePrefKey(kSvcCustomUrlPref , m_ServiceName));
                m_CloudPanelCustomPort = EditorPrefs.GetInt(ServicePrefKey(kSvcCustomPortPref , m_ServiceName));
            }

            public void StoreCloudServicePref()
            {
                EditorPrefs.SetInt(ServicePrefKey(kSvcEnvPref, m_ServiceName), m_CloudPanelServer);
                EditorPrefs.SetString(ServicePrefKey(kSvcCustomUrlPref , m_ServiceName), m_CloudPanelCustomUrl);
                EditorPrefs.SetInt(ServicePrefKey(kSvcCustomPortPref , m_ServiceName), m_CloudPanelCustomPort);
            }

            public string m_ServiceName;
            public int m_CloudPanelServer;
            public string m_CloudPanelCustomUrl;
            public int m_CloudPanelCustomPort;
        };

        protected static CloudPanelPref GetPanelPref(string serviceName)
        {
            if (m_CloudPanelPref.ContainsKey(serviceName))
                return m_CloudPanelPref[serviceName];

            CloudPanelPref  pref = new CloudPanelPref(serviceName);
            m_CloudPanelPref.Add(serviceName, pref);
            return pref;
        }

        protected static Dictionary<string, CloudPanelPref>  m_CloudPanelPref = new Dictionary<string, CloudPanelPref>();

        public static int GetServiceEnv(string serviceName)
        {
            if (Unsupported.IsDeveloperBuild() || UnityConnect.preferencesEnabled)
                return EditorPrefs.GetInt(ServicePrefKey(kSvcEnvPref, serviceName));

            for (var i = 0; i < kEnvironmentFamilies.Length; i++)
            {
                var environmentName = kEnvironmentFamilies[i];
                //By using the configuration it should default to production if there is no
                //-cloudEnvironment or to the switch value in case it is specified
                if (environmentName.Equals(UnityConnect.instance.configuration, StringComparison.InvariantCultureIgnoreCase))
                    return i;
            }
            return 0; //Return production if there is an error
        }

        public static string ServicePrefKey(string baseKey, string serviceName)
        {
            return baseKey + "/" + serviceName;
        }

        public static string FixUrl(string url, string serviceName)
        {
            var fixUrl = url;
            var panelEnv = GetServiceEnv(serviceName);
            if (panelEnv != kProductionEnv)
            {
                if (fixUrl.StartsWith("http://") || fixUrl.StartsWith("https://"))
                {
                    if (panelEnv == kCustomEnv)
                    {
                        var devUrl = EditorPrefs.GetString(ServicePrefKey(kSvcCustomUrlPref , serviceName));
                        var devPort = EditorPrefs.GetInt(ServicePrefKey(kSvcCustomPortPref , serviceName));
                        fixUrl = (devPort == 0) ? devUrl : (devUrl + ":" + devPort);
                    }
                    else
                    {
                        fixUrl = fixUrl.ToLower();
                        fixUrl = fixUrl.Replace("/" + kEnvironmentFamilies[kProductionEnv].ToLower() + "/", "/" + kEnvironmentFamilies[panelEnv].ToLower() + "/");
                    }
                    return fixUrl;
                }

                if (fixUrl.StartsWith("file://"))
                {
                    fixUrl = fixUrl.Substring(7);

                    if (panelEnv == kCustomEnv)
                    {
                        var devUrl = EditorPrefs.GetString(ServicePrefKey(kSvcCustomUrlPref , serviceName));
                        var devPort = EditorPrefs.GetInt(ServicePrefKey(kSvcCustomPortPref , serviceName));
                        fixUrl = devUrl + ":" + devPort;
                    }

                    return fixUrl;
                }

                if (!fixUrl.StartsWith("file://") && !fixUrl.StartsWith("http://") && !fixUrl.StartsWith("https://"))
                {
                    fixUrl = "http://" + fixUrl;
                    return fixUrl;
                }
            }

            return fixUrl;
        }

        static public void ShowPanelPrefUI()
        {
            List<string> cloudServiceNames = UnityConnectServiceCollection.instance.GetAllServiceNames();

            bool changed = false;

            foreach (string service in cloudServiceNames)
            {
                CloudPanelPref pref = GetPanelPref(service);

                int nVal = EditorGUILayout.Popup(service, pref.m_CloudPanelServer, kEnvironmentFamilies);
                if (nVal != pref.m_CloudPanelServer)
                {
                    pref.m_CloudPanelServer = nVal;
                    changed = true;
                }

                if (pref.m_CloudPanelServer == kCustomEnv)
                {
                    EditorGUI.indentLevel++;
                    string nUrl = EditorGUILayout.TextField("Custom server URL", pref.m_CloudPanelCustomUrl);
                    if (nUrl != pref.m_CloudPanelCustomUrl)
                    {
                        pref.m_CloudPanelCustomUrl = nUrl;
                        changed = true;
                    }

                    Int32.TryParse(EditorGUILayout.TextField("Custom server port", pref.m_CloudPanelCustomPort.ToString()), out nVal);

                    if (nVal != pref.m_CloudPanelCustomPort)
                    {
                        pref.m_CloudPanelCustomPort = nVal;
                        changed = true;
                    }
                    EditorGUI.indentLevel--;
                }
            }

            if (changed)
                UnityConnectServiceCollection.instance.ReloadServices();
        }

        public static void StorePanelPrefs()
        {
            if (!Unsupported.IsDeveloperBuild() && !UnityConnect.preferencesEnabled)
                return;

            foreach (KeyValuePair<string, CloudPanelPref> kvp in m_CloudPanelPref)
            {
                kvp.Value.StoreCloudServicePref();
            }
        }
    }
}
