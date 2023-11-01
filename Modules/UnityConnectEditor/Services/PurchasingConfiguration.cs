// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Connect
{
    /// <summary>
    /// Class to contain config stuff for the IAP service, URLs and such.
    /// </summary>
    internal class PurchasingConfiguration
    {
        static readonly PurchasingConfiguration k_Instance;

        readonly string m_PurchasingPackageUrl;
        readonly string m_GooglePlayDevConsoleUrl;

        readonly string m_GatewayTokenApiUrl;
        readonly string m_IapSettingssApiUrl;

        internal const string k_ProjectSettingsUrl = "https://dashboard.unity3d.com/admin-portal/organizations/{0}/projects/{1}/settings/general";

        static PurchasingConfiguration()
        {
            k_Instance = new PurchasingConfiguration();
        }

        PurchasingConfiguration()
        {
            m_PurchasingPackageUrl = "https://public-cdn.cloud.unity3d.com/UnityEngine.Cloud.Purchasing.unitypackage";
            m_GooglePlayDevConsoleUrl = "https://play.google.com/apps/publish/";

            const string k_BaseServicesApiUrl = "https://services.unity.com/api/";
            m_GatewayTokenApiUrl = $"{k_BaseServicesApiUrl}auth/v1/genesis-token-exchange/unity";
            m_IapSettingssApiUrl = k_BaseServicesApiUrl + "iap-settings/v1/projects/{0}/settings";
        }

        public static PurchasingConfiguration instance => k_Instance;

        public string purchasingPackageUrl
        {
            get { return m_PurchasingPackageUrl; }
        }

        internal string gatewayTokenApiUrl
        {
            get { return m_GatewayTokenApiUrl; }
        }

        internal string iapSettingssApiUrl
        {
            get { return m_IapSettingssApiUrl; }
        }

        public string googlePlayDevConsoleUrl
        {
            get { return m_GooglePlayDevConsoleUrl; }
        }
    }
}
