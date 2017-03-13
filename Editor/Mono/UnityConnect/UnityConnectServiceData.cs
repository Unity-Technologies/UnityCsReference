// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Web;

namespace UnityEditor.Connect
{
    internal class UnityConnectServiceData
    {
        private readonly string m_ServiceName;
        private readonly string m_HtmlSourcePath;
        private readonly CloudServiceAccess m_JavascriptGlobalObject;
        private readonly string m_JsGlobalObjectName;
        public string serviceName { get { return m_ServiceName; }}
        public string serviceUrl
        {
            get
            {
                return UnityConnectPrefs.FixUrl(m_HtmlSourcePath, m_ServiceName);
            }
        }
        public CloudServiceAccess serviceJsGlobalObject { get { return m_JavascriptGlobalObject; }}
        public string serviceJsGlobalObjectName {get { return m_JsGlobalObjectName; }}


        public UnityConnectServiceData(string serviceName, string htmlSourcePath, CloudServiceAccess jsGlobalObject, string jsGlobalObjectName)
        {
            if (string.IsNullOrEmpty(serviceName))
                throw new ArgumentNullException("serviceName");

            if (string.IsNullOrEmpty(htmlSourcePath))
                throw new ArgumentNullException("htmlSourcePath");

            m_ServiceName = serviceName;
            m_HtmlSourcePath = htmlSourcePath;
            m_JavascriptGlobalObject = jsGlobalObject;
            m_JsGlobalObjectName = jsGlobalObjectName;
            if (m_JavascriptGlobalObject != null)
            {
                //If no name is specified use the service name
                if (string.IsNullOrEmpty(m_JsGlobalObjectName))
                    m_JsGlobalObjectName = m_ServiceName;

                JSProxyMgr.GetInstance().AddGlobalObject(m_JsGlobalObjectName, m_JavascriptGlobalObject);
            }
        }

        public void EnableService(bool enabled)
        {
            if (m_JavascriptGlobalObject != null)
            {
                m_JavascriptGlobalObject.EnableService(enabled);
            }
        }

        public void OnProjectUnbound()
        {
            if (m_JavascriptGlobalObject != null)
            {
                m_JavascriptGlobalObject.OnProjectUnbound();
            }
        }
    }
}
