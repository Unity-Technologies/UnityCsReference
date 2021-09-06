// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetails : VisualElement
    {
        private const string k_TermsOfServicesURL = "https://assetstore.unity.com/account/term";
        internal new class UxmlFactory : UxmlFactory<PackageDetails> {}

        private ResourceLoader m_ResourceLoader;
        private ExtensionManager m_ExtensionManager;
        private ApplicationProxy m_Application;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private UnityConnectProxy m_UnityConnectProxy;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_ExtensionManager = container.Resolve<ExtensionManager>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_PageManager = container.Resolve<PageManager>();
            m_UnityConnectProxy = container.Resolve<UnityConnectProxy>();
        }

        private IPackage m_Package;
        private IPackageVersion m_Version;

        public PackageDetails()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageDetails.uxml");
            Add(root);
            root.StretchToParentSize();
            cache = new VisualElementCache(root);

            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var extension in PackageManagerExtensions.Extensions)
                    customContainer.Add(extension.CreateExtensionUI());
            });

            scrollView.verticalScroller.valueChanged += OnDetailScroll;
            scrollView.RegisterCallback<GeometryChangedEvent>(RecalculateFillerHeight);
            detail.RegisterCallback<GeometryChangedEvent>(RecalculateFillerHeight);

            Refresh();
        }

        public void OnEnable()
        {
            body.OnEnable();
            toolbar.OnEnable();

            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;

            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
            m_PackageDatabase.onVerifiedGitPackageUpToDate += OnVerifiedGitPackageUpToDate;
            m_PackageDatabase.onTermOfServiceAgreementStatusChange += OnTermOfServiceAgreementStatusChange;

            m_PageManager.onSelectionChanged += OnSelectionChanged;

            m_UnityConnectProxy.onUserLoginStateChange += OnUserLoginStateChange;

            Refresh(m_PageManager.GetSelectedVersion());
        }

        public void OnDisable()
        {
            body.OnDisable();
            toolbar.OnDisable();

            m_Application.onInternetReachabilityChange -= OnInternetReachabilityChange;

            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
            m_PackageDatabase.onVerifiedGitPackageUpToDate -= OnVerifiedGitPackageUpToDate;
            m_PackageDatabase.onTermOfServiceAgreementStatusChange -= OnTermOfServiceAgreementStatusChange;

            m_PageManager.onSelectionChanged -= OnSelectionChanged;

            m_UnityConnectProxy.onUserLoginStateChange -= OnUserLoginStateChange;
        }

        private void RecalculateFillerHeight(GeometryChangedEvent evt)
        {
            if (evt.oldRect.height == evt.newRect.height)
                return;
            featureDependencies.RecalculateFillerHeight(detail.layout.height, scrollView.layout.height);
        }

        private void OnDetailScroll(float offset)
        {
            m_PackageManagerPrefs.packageDetailVerticalScrollOffset = offset;
        }

        private void OnTermOfServiceAgreementStatusChange(TermOfServiceAgreementStatus status)
        {
            if (status == TermOfServiceAgreementStatus.Accepted)
                return;

            var result = m_Application.DisplayDialog(L10n.Tr("Package Manager"),
                L10n.Tr("You need to accept Asset Store Terms of Service and EULA before you can download/update any package."),
                L10n.Tr("Read and accept"), L10n.Tr("Close"));

            if (result)
                m_UnityConnectProxy.OpenAuthorizedURLInWebBrowser(k_TermsOfServicesURL);
        }

        private void OnInternetReachabilityChange(bool value)
        {
            Refresh();
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            Refresh();
        }

        internal void OnSelectionChanged(IPackageVersion version)
        {
            m_PackageManagerPrefs.packageDetailVerticalScrollOffset = 0;
            Refresh(version);
        }

        private void Refresh(IPackageVersion version)
        {
            Refresh(m_PackageDatabase.GetPackage(version), version);
        }

        public void Refresh(IPackage package, IPackageVersion version)
        {
            m_Package = package;
            m_Version = version ?? package?.versions.primary;

            if (version?.isFullyFetched == false)
                m_PackageDatabase.FetchExtraInfo(version);

            Refresh();
        }

        private void Refresh()
        {
            scrollView.scrollOffset = new Vector2(0, m_PackageManagerPrefs.packageDetailVerticalScrollOffset);

            var detailVisible = m_Package != null && m_Version != null && !inProgressView.Refresh(m_Package);
            var detailEnabled = m_Version == null || m_Version.isFullyFetched;

            if (!detailVisible)
            {
                RefreshExtensions(null, null);
            }
            else
            {
                header.Refresh(m_Package, m_Version);
                body.Refresh(m_Package, m_Version);
                toolbar.Refresh(m_Package, m_Version);
                RefreshExtensions(m_Package, m_Version);
            }

            // Set visibility
            UIUtils.SetElementDisplay(detail, detailVisible);
            UIUtils.SetElementDisplay(toolbar, detailVisible);

            SetEnabled(detailEnabled);
            RefreshDetailError();
        }

        void RefreshExtensions(IPackage package, IPackageVersion version)
        {
            var packageInfo = version?.packageInfo;
            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var extension in PackageManagerExtensions.Extensions)
                    extension.OnPackageSelectionChange(packageInfo);
            });

            m_ExtensionManager.SendPackageSelectionChangedEvent(package, version);
        }

        internal void OnVerifiedGitPackageUpToDate(IPackage package)
        {
            Debug.Log(string.Format(L10n.Tr("{0} is already up-to-date."), package.displayName));
        }

        private void OnPackagesChanged(IEnumerable<IPackage> added,
            IEnumerable<IPackage> removed,
            IEnumerable<IPackage> preUpdate,
            IEnumerable<IPackage> postUpdate)
        {
            var packageUniqudId = m_Package?.uniqueId;
            if (string.IsNullOrEmpty(packageUniqudId) || !postUpdate.Any())
                return;

            var updatedPackage = postUpdate.FirstOrDefault(p => p.uniqueId == packageUniqudId);
            // if Git and updated, inform the user
            if (updatedPackage != null)
            {
                var updatedVersion = m_Version == null ? null : updatedPackage.versions.FirstOrDefault(v => v.uniqueId == m_Version.uniqueId);
                Refresh(updatedPackage, updatedVersion);
            }
        }

        private void RefreshDetailError()
        {
            var error = m_Version?.errors?.FirstOrDefault(e => !e.HasAttribute(UIError.Attribute.IsClearable))
                ?? m_Package?.errors?.FirstOrDefault(e => !e.HasAttribute(UIError.Attribute.IsClearable));
            if (error == null)
                detailError.ClearError();
            else
                detailError.SetError(error);
        }

        private VisualElementCache cache { get; set; }

        private InProgressView inProgressView => cache.Get<InProgressView>("inProgressView");
        private PackageDetailsHeader header => cache.Get<PackageDetailsHeader>("detailsHeader");
        private PackageDetailsBody body => cache.Get<PackageDetailsBody>("detailsBody");
        public PackageToolbar toolbar => cache.Get<PackageToolbar>("packageToolbar");

        internal Alert detailError => cache.Get<Alert>("detailError");
        private ScrollView scrollView => cache.Get<ScrollView>("detailScrollView");
        private VisualElement detail => cache.Get<VisualElement>("detail");

        // customContainer is kept for Package Manager UI extension API v1 &
        // extensionContainer is for extension API v2
        private VisualElement customContainer => cache.Get<VisualElement>("detailCustomContainer");
        internal VisualElement extensionContainer => cache.Get<VisualElement>("detailExtensionContainer");

        private FeatureDependencies featureDependencies => cache.Get<FeatureDependencies>("featureDependencies");
    }
}
