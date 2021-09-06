// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageSignInButton : PackageToolBarRegularButton
    {
        private UnityConnectProxy m_UnityConnectProxy;
        public PackageSignInButton(UnityConnectProxy unityConnectProxy)
        {
            m_UnityConnectProxy = unityConnectProxy;
        }

        protected override bool TriggerAction()
        {
            m_UnityConnectProxy.ShowLogin();
            PackageManagerWindowAnalytics.SendEvent("signInFromToolbar", m_Version?.uniqueId);
            return true;
        }

        protected override bool isVisible => !m_UnityConnectProxy.isUserLoggedIn && m_Package?.hasEntitlementsError == true;

        protected override string GetTooltip(bool isInProgress)
        {
            return string.Empty;
        }

        protected override string GetText(bool isInProgress)
        {
            return L10n.Tr("Sign In");
        }

        protected override bool isInProgress => false;
    }
}
