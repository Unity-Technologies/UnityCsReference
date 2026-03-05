// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetails : BaseDetailsView
    {
        public VisualElement element => this;

        private readonly IApplicationProxy m_Application;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPageManager m_PageManager;
        private readonly IUnityConnectProxy m_UnityConnectProxy;

        private ScrollView m_ParentScrollView;

        public PackageDetails(IResourceLoader resourceLoader, IApplicationProxy application, IPackageDatabase packageDatabase, IPageManager pageManager, IUnityConnectProxy unityConnectProxy)
        {
            m_Application = application;
            m_PackageDatabase = packageDatabase;
            m_PageManager = pageManager;
            m_UnityConnectProxy = unityConnectProxy;

            var root = resourceLoader.GetTemplate("PackageDetails.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            detail.RegisterCallback<GeometryChangedEvent>(RefreshSelectedTabHeight);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;
            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
            m_UnityConnectProxy.onUserLoginStateChange += OnUserLoginStateChange;

            m_ParentScrollView = UIUtils.GetParentOfType<ScrollView>(this);
            m_ParentScrollView?.RegisterCallback<GeometryChangedEvent>(RefreshSelectedTabHeight);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_Application.onInternetReachabilityChange -= OnInternetReachabilityChange;
            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
            m_UnityConnectProxy.onUserLoginStateChange -= OnUserLoginStateChange;

            m_ParentScrollView?.UnregisterCallback<GeometryChangedEvent>(RefreshSelectedTabHeight);
        }

        private void RefreshSelectedTabHeight(GeometryChangedEvent evt)
        {
            if (Mathf.Approximately(evt.oldRect.height, evt.newRect.height) || m_ParentScrollView == null)
                return;

            tabView.GetTab(tabView.selectedTabId)?.RefreshHeight(detail.layout.height, m_ParentScrollView.layout.height,
                header.layout.height, tabViewHeaderContainer.layout.height, legacyExtensionContainer.layout.height,
                extensionContainer.layout.height);
        }

        private void OnInternetReachabilityChange(bool value)
        {
            Refresh(m_PageManager.activePage.GetSelection());
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            Refresh(m_PageManager.activePage.GetSelection());
        }

        public override void Refresh(PageSelection selections)
        {
            var package = m_PackageDatabase.GetPackage(selections.first);
            if (package == null)
                return;

            var inProgress = package is { progress: PackageProgress.Installing } && package.versions.primary.HasTag(PackageTag.Placeholder);
            inProgressView.UpdateProgress(inProgress);
            UIUtils.SetElementDisplay(inProgressView, inProgress);
            UIUtils.SetElementDisplay(detail, !inProgress);

            if (inProgress)
            {
                var title = package.versions.primary.HasTag(PackageTag.Custom) ? L10n.Tr("Please wait, creating a package...") : L10n.Tr("Please wait, installing a package...");
                var description = package.uniqueId;
                inProgressView.UpdateMessage(title, description);
                return;
            }

            var version = package.versions.primary;
            var detailEnabled = version == null || version.isFullyFetched;

            header.Refresh(package);
            body.Refresh(package);
            toolbar.Refresh(package);
            SetEnabled(detailEnabled);
            RefreshDetailError(package, version);
        }

        private void OnPackagesChanged(PackagesChangeArgs args)
        {
            // We refresh details regardless of if the packages changed contains the selected package
            // because a package's details could have information related to other packages (dependencies, dependants, etc)
            Refresh(m_PageManager.activePage.GetSelection());
        }

        private void RefreshDetailError(IPackage package, IPackageVersion version)
        {
            var error = version?.errors?.FirstMatch(e => !e.HasAttribute(UIError.Attribute.Clearable | UIError.Attribute.HiddenFromUI))
                ?? package?.errors?.FirstMatch(e => !e.HasAttribute(UIError.Attribute.Clearable | UIError.Attribute.HiddenFromUI));
            detailError.RefreshError(error, version);
        }

        private VisualElementCache cache { get; }

        private InProgressView inProgressView => cache.Get<InProgressView>("inProgressView");
        private PackageDetailsHeader header => cache.Get<PackageDetailsHeader>("detailsHeader");
        private PackageDetailsBody body => cache.Get<PackageDetailsBody>("detailsBody");
        private Alert detailError => cache.Get<Alert>("detailError");
        private VisualElement detail => cache.Get<VisualElement>("detail");
        public PackageToolbar toolbar => cache.Get<PackageToolbar>("packageToolbar");

        // customContainer is kept for Package Manager UI extension API v1 &
        // extensionContainer is for extension API v2
        public VisualElement legacyExtensionContainer => cache.Get<VisualElement>("detailCustomContainer");
        public VisualElement extensionContainer => cache.Get<VisualElement>("detailExtensionContainer");

        private PackageDetailsTabView tabView => cache.Get<PackageDetailsTabView>("packageDetailsTabView");
        private VisualElement tabViewHeaderContainer => cache.Get<VisualElement>("packageDetailsTabViewHeaderContainer");
    }
}
