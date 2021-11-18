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
        }

        public void OnEnable()
        {
            body.OnEnable();
            toolbar.OnEnable();
            multiSelectDetails.OnEnable();

            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;

            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
            m_PackageDatabase.onVerifiedGitPackageUpToDate += OnVerifiedGitPackageUpToDate;
            m_PackageDatabase.onTermOfServiceAgreementStatusChange += OnTermOfServiceAgreementStatusChange;

            m_PageManager.onSelectionChanged += OnSelectionChanged;

            m_UnityConnectProxy.onUserLoginStateChange += OnUserLoginStateChange;

            Refresh(m_PageManager.GetSelection());
        }

        public void OnDisable()
        {
            body.OnDisable();
            toolbar.OnDisable();
            multiSelectDetails.OnDisable();

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

        internal void OnSelectionChanged(PageSelection selections)
        {
            m_PackageManagerPrefs.packageDetailVerticalScrollOffset = 0;
            Refresh(selections);
        }

        internal void Refresh(PageSelection selections = null)
        {
            selections = selections ?? m_PageManager.GetSelection();
            scrollView.scrollOffset = new Vector2(0, m_PackageManagerPrefs.packageDetailVerticalScrollOffset);

            if (selections.Count == 1)
            {
                var selection = selections.FirstOrDefault();
                m_PackageDatabase.GetPackageAndVersion(selection.packageUniqueId, selection.versionUniqueId, out var package, out var version);
                Refresh(package, version);
            }
            else
            {
                // We call Refresh(null, null) to make sure that all single package details elements are hidden properly
                // We want to hide those elements when 1) there's nothing selected or 2) the multi select details view is visible.
                Refresh(null, null);
                UIUtils.SetElementDisplay(multiSelectDetails, multiSelectDetails.Refresh(selections));
            }
        }

        private void Refresh(IPackage package, IPackageVersion version)
        {
            version = version ?? package?.versions.primary;
            if (version?.isFullyFetched == false)
                m_PackageDatabase.FetchExtraInfo(version);

            var detailVisible = package != null && version != null && !inProgressView.Refresh(package);
            var detailEnabled = version == null || version.isFullyFetched;

            if (!detailVisible)
            {
                RefreshExtensions(null, null);
            }
            else
            {
                header.Refresh(package, version);
                body.Refresh(package, version);
                toolbar.Refresh(package, version);
                RefreshExtensions(package, version);
            }

            // Set visibility
            UIUtils.SetElementDisplay(detail, detailVisible);
            UIUtils.SetElementDisplay(toolbar, detailVisible);
            UIUtils.SetElementDisplay(multiSelectDetails, false);

            SetEnabled(detailEnabled);
            RefreshDetailError(package, version);
        }

        void RefreshExtensions(IPackage package, IPackageVersion version)
        {
            // For now packageInfo, package and packageVersion will all be null when there are multiple packages selected.
            // This way no single select UI will be displayed for multi-select. We might handle it differently in the future in a new story
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
            var selection = m_PageManager.GetSelection();
            if (added.Concat(removed).Concat(preUpdate).Concat(postUpdate).Any(p => selection.Contains(p.uniqueId)))
                Refresh(selection);
        }

        private void RefreshDetailError(IPackage package, IPackageVersion version)
        {
            var error = version?.errors?.FirstOrDefault(e => !e.HasAttribute(UIError.Attribute.IsClearable))
                ?? package?.errors?.FirstOrDefault(e => !e.HasAttribute(UIError.Attribute.IsClearable));
            if (error == null)
                detailError.ClearError();
            else
                detailError.SetError(error, version);
        }

        private VisualElementCache cache { get; set; }

        private MultiSelectDetails multiSelectDetails => cache.Get<MultiSelectDetails>("multiSelectDetails");
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
