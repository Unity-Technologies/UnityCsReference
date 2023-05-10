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

        protected override bool TriggerAction(IPackageVersion version)
        {
            m_UnityConnectProxy.ShowLogin();
            PackageManagerWindowAnalytics.SendEvent("signInFromToolbar", version?.uniqueId);
            return true;
        }

        protected override bool IsVisible(IPackageVersion version) => !m_UnityConnectProxy.isUserLoggedIn &&
                                                                      (version?.package.hasEntitlementsError == true ||
                                                                       version?.package.product != null);

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            return string.Empty;
        }

        protected override string GetText(IPackageVersion version, bool isInProgress)
        {
            return L10n.Tr("Sign in");
        }

        protected override bool IsInProgress(IPackageVersion version) => false;
    }
}
