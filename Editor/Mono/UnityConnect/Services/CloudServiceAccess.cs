// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Analytics;


namespace UnityEditor.Web
{
    internal abstract class CloudServiceAccess
    {
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

        public virtual string GetPackageName()
        {
            return string.Empty;
        }

        public virtual bool IsServiceEnabled()
        {
            return PlayerSettings.GetCloudServiceEnabled(GetServiceName());
        }

        public virtual void EnableService(bool enabled)
        {
            PlayerSettings.SetCloudServiceEnabled(GetServiceName(), enabled);
        }

        public virtual string GetCurrentPackageVersion()
        {
            return UnityEditor.Connect.PackageUtils.instance.GetCurrentVersion(GetPackageName());
        }

        public virtual string GetLatestPackageVersion()
        {
            return UnityEditor.Connect.PackageUtils.instance.GetLatestVersion(GetPackageName());
        }

        public virtual void UpdateLatestPackage()
        {
            UnityEditor.Connect.PackageUtils.instance.UpdateLatest(GetPackageName());
        }


        public virtual void OnProjectUnbound()
        {
            // Do nothing
        }

        public void ShowServicePage()
        {
            UnityEditor.Connect.UnityConnectServiceCollection.instance.ShowService(GetServiceName(), true, "show_service_page");
        }

        public void GoBackToHub()
        {
            UnityEditor.Connect.UnityConnectServiceCollection.instance.ShowService(UnityEditor.Web.HubAccess.kServiceName, true, "go_back_to_hub");
        }
    }
}

