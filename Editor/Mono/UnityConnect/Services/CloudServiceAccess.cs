// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


namespace UnityEditor.Web
{
    internal abstract class CloudServiceAccess
    {
        public CloudServiceAccess()
        {
        }

        public abstract string GetServiceName();

        protected WebView GetWebView()
        {
            return UnityEditor.Connect.UnityConnectServiceCollection.instance.GetWebViewFromServiceName(GetServiceName());
        }

        protected string GetSafeServiceName()
        {
            return GetServiceName().Replace(' ', '_');
        }

        public virtual string GetServiceDisplayName()
        {
            return GetServiceName();
        }

        public virtual bool IsServiceEnabled()
        {
            return PlayerSettings.GetCloudServiceEnabled(GetServiceName());
        }

        public virtual void EnableService(bool enabled)
        {
            PlayerSettings.SetCloudServiceEnabled(GetServiceName(), enabled);
        }

        public virtual void OnProjectUnbound()
        {
            // Do nothing
        }

        public void ShowServicePage()
        {
            UnityEditor.Connect.UnityConnectServiceCollection.instance.ShowService(GetServiceName(), true);
        }

        public void GoBackToHub()
        {
            UnityEditor.Connect.UnityConnectServiceCollection.instance.ShowService(UnityEditor.Web.HubAccess.kServiceName, true);
        }
    }
}

