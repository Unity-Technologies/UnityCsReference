// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal class ServicesRepository
    {
        static readonly ServicesRepository k_Instance;

        public static ServicesRepository instance => k_Instance;

        static readonly Dictionary<string, SingleService> k_Services = new Dictionary<string, SingleService>();

        static List<string> s_InitializedServices = new List<string>();

        UnityWebRequest m_ServiceRequest;
        const string k_serviceFlagsKey = "service_flags";

        const string k_NotInitializedMsg = "Service {0} was not initialized. See the console for more details.";

        static ServicesRepository()
        {
            k_Instance = new ServicesRepository();
        }

        ServicesRepository()
        {
            InitializeServicesHandlers();
        }

        internal static void InitializeServicesHandlers()
        {
            if (s_InitializedServices.Count < k_Services.Count)
            {
                foreach (var serviceKey in k_Services.Keys)
                {
                    if (!s_InitializedServices.Contains(serviceKey))
                    {
                        var service = k_Services[serviceKey];
                        try
                        {
                            service.InitializeServiceEventHandlers();
                            s_InitializedServices.Add(serviceKey);
                        }
                        catch (Exception ex)
                        {
                            NotificationManager.instance.Publish(service.notificationTopic, Notification.Severity.Error, string.Format(L10n.Tr(k_NotInitializedMsg), L10n.Tr(service.title)));
                            Debug.LogException(ex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds a new service in the services window list.
        /// Sorting and duplicates are already handled.
        /// Each service has the responsibility to add itself to this list.
        /// This should not be called by a service that isn't registering itself.
        /// </summary>
        /// <param name="singleService">Configuration for the service to add</param>
        public static void AddService(SingleService singleService)
        {
            if (k_Services.ContainsKey(singleService.name))
                return;

            k_Services.Add(singleService.name, singleService);
        }

        public static List<SingleService> GetServices()
        {
            return k_Services.Values.ToList();
        }

        public static SingleService GetService(string name)
        {
            return k_Services[name];
        }

        public static void DisableAllServices(bool shouldUpdateApiFlag)
        {
            GetServices().ForEach(service => service.EnableService(false, shouldUpdateApiFlag));
        }

        public static void EnableServicesOnProjectCreation()
        {
            GetServices().ForEach(service =>
            {
                if (service.shouldEnableOnProjectCreation)
                {
                    service.EnableService(true);
                }
            });
        }

        public static void SyncServicesOnProjectRebind()
        {
            k_Instance.RequestServicesApiFlags(flags =>
            {
                if (flags != null)
                {
                    GetServices().ForEach(service =>
                    {
                        if (service.shouldSyncOnProjectRebind && flags.ContainsKey(service.serviceFlagName))
                        {
                            service.EnableService(flags[service.serviceFlagName]);
                        }
                    });
                }
            });
        }

        private void RequestServicesApiFlags(Action<Dictionary<string, bool>> callback)
        {
            if (m_ServiceRequest != null)
            {
                m_ServiceRequest?.Abort();
                m_ServiceRequest?.Dispose();
                m_ServiceRequest = null;
            }

            ServicesConfiguration.instance.RequestCurrentProjectApiUrl(currentProjectApiUrl =>
            {
                m_ServiceRequest = new UnityWebRequest(currentProjectApiUrl,
                    UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer() };
                m_ServiceRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {UnityConnect.instance.GetUserInfo().accessToken}");
                m_ServiceRequest.SendWebRequest().completed += op =>
                {
                    if (op.isDone)
                    {
                        Dictionary<string, bool> serviceFlags = null;
                        if (ServicesUtils.IsUnityWebRequestReadyForJsonExtract(m_ServiceRequest))
                        {
                            if (m_ServiceRequest.downloadHandler.text.Length != 0)
                            {
                                var jsonParser = new JSONParser(m_ServiceRequest.downloadHandler.text);
                                try
                                {
                                    var json = jsonParser.Parse();
                                    serviceFlags = new Dictionary<string, bool>();
                                    Dictionary<string, JSONValue> jsonFlags = null;

                                    if (json.AsDict().ContainsKey(k_serviceFlagsKey))
                                    {
                                        jsonFlags = json.AsDict()[k_serviceFlagsKey].AsDict();
                                    }

                                    if (jsonFlags != null)
                                    {
                                        foreach (var key in jsonFlags.Keys)
                                        {
                                            serviceFlags.Add(key, jsonFlags[key].AsBool());
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogException(ex);
                                    NotificationManager.instance.Publish(Notification.Topic.AdsService, Notification.Severity.Error, ex.Message);
                                }
                            }
                        }

                        m_ServiceRequest?.Abort();
                        m_ServiceRequest?.Dispose();
                        m_ServiceRequest = null;

                        callback?.Invoke(serviceFlags);
                    }
                };
            });
        }
    }
}
