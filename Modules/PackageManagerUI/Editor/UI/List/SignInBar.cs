// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.PackageManager.UI.Internal
{
    [UxmlElement]
    internal partial class SignInBar : VisualElement
    {
        private static readonly string k_Message = L10n.Tr("to manage Asset Store packages");
        private static readonly string k_ButtonText = L10n.Tr("Sign in");

        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IPageManager m_PageManager;
        private readonly IPackageDatabase m_PackageDatabase;

        public SignInBar() : this(
            ServicesContainer.instance.Resolve<IUnityConnectProxy>(),
            ServicesContainer.instance.Resolve<IPageManager>(),
            ServicesContainer.instance.Resolve<IPackageDatabase>())
        {
        }

        public SignInBar(
            IUnityConnectProxy unityConnect,
            IPageManager pageManager,
            IPackageDatabase packageDatabase)
        {
            m_UnityConnect = unityConnect;
            m_PageManager = pageManager;
            m_PackageDatabase = packageDatabase;

            Add(new Button(OnSignInButtonClicked) { text = k_ButtonText });
            Add(new Label(k_Message));

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_PageManager.onActivePageChanged += OnActivePageChanged;

            Refresh();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
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
                if (m_PackageDatabase.GetPackage(visualState.itemUniqueId)?.product != null)
                {
                    containsAssetStorePackages = true;
                    break;
                }
            }

            UIUtils.SetElementDisplay(this, activePage.id == InProjectPage.k_Id && !m_UnityConnect.isUserLoggedIn && containsAssetStorePackages);
        }
    }
}
