// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Connect
{
    /// <summary>
    /// Class to contain config stuff for the Analytics service, URLs and such.
    /// </summary>
    internal class AnalyticsConfiguration
    {
        static readonly AnalyticsConfiguration k_Instance;

        readonly string m_LearnMoreUrl;
        readonly string m_ApiUrl;
        readonly string m_SupportUrl;
        readonly string k_CustomLearnUrl;
        readonly string k_MonetizationLearnUrl;
        readonly string m_ValidatorUrl;
        readonly string m_ClearUrl;
        readonly string m_CoreProjectsUrl;

        static AnalyticsConfiguration()
        {
            k_Instance = new AnalyticsConfiguration();
        }

        AnalyticsConfiguration()
        {
            m_LearnMoreUrl = "http://unity3d.com/services/analytics";
            m_ApiUrl = "https://analytics.cloud.unity3d.com";
            m_SupportUrl = m_ApiUrl + "/support";
            m_ValidatorUrl = m_ApiUrl + "/api/v2/projects/{0}/validation";
            m_ClearUrl = m_ValidatorUrl + "/clearEvent";
            m_CoreProjectsUrl = "https://core.cloud.unity3d.com/api/projects/{0}";
            k_CustomLearnUrl = "https://docs.unity3d.com/Manual/UnityAnalyticsCustomEvents.html";
            k_MonetizationLearnUrl = "https://docs.unity3d.com/Manual/UnityAnalyticsMonetization.html";
        }

        public static AnalyticsConfiguration instance => k_Instance;

        public string learnMoreUrl
        {
            get { return m_LearnMoreUrl; }
        }

        public string supportUrl
        {
            get { return m_SupportUrl; }
        }

        public string customLearnUrl
        {
            get { return k_CustomLearnUrl; }
        }

        public string monetizationLearnUrl
        {
            get { return k_MonetizationLearnUrl; }
        }

        public string validatorUrl
        {
            get { return m_ValidatorUrl; }
        }

        public string clearUrl
        {
            get { return m_ClearUrl; }
        }

        public string coreProjectsUrl
        {
            get { return m_CoreProjectsUrl;  }
        }
    }
}
