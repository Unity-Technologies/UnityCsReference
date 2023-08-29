// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class SignInBar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<SignInBar> {}

        private static readonly string k_Message = L10n.Tr("Sign in to manage Asset Store packages");
        private static readonly string k_ButtonText = L10n.Tr("Sign in");

        private IUnityConnectProxy m_UnityConnect;
        private IPageManager m_PageManager;
        private IPackageDatabase m_PackageDatabase;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_UnityConnect = container.Resolve<IUnityConnectProxy>();
            m_PageManager = container.Resolve<IPageManager>();
            m_PackageDatabase = container.Resolve<IPackageDatabase>();
        }

        public SignInBar()
        {
            Add(new Label(k_Message));
            Add(new Button(OnSignInButtonClicked) { text = k_ButtonText });

            ResolveDependencies();
        }

        public void OnEnable()
        {
            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_PageManager.onActivePageChanged += OnActivePageChanged;

            Refresh();
        }

        public void OnDisable()
        {
            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
            m_PageManager.onActivePageChanged -= OnActivePageChanged;
        }

        private void OnPackagesChanged(PackagesChangeArgs args)
        {
            Refresh();
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            Refresh();
        }

        private void OnActivePageChanged(IPage page)
        {
            Refresh();
        }

        private void OnSignInButtonClicked()
        {
            m_UnityConnect.ShowLogin();
        }

        private void Refresh()
        {
            var activePage = m_PageManager.activePage;
            var containsAssetStorePackages = false;

            foreach (var visualState in activePage.visualStates)
            {
                if (m_PackageDatabase.GetPackage(visualState.packageUniqueId)?.product != null)
                {
                    containsAssetStorePackages = true;
                    break;
                }
            }

            UIUtils.SetElementDisplay(this, activePage.id == InProjectPage.k_Id && !m_UnityConnect.isUserLoggedIn && containsAssetStorePackages);
        }
    }
}
