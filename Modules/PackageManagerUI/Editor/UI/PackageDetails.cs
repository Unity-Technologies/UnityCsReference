// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetails : VisualElement
    {
        [System.Serializable]
        internal new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance()
            {
                var container = ServicesContainer.instance;
                return new PackageDetails(
                    container.Resolve<IResourceLoader>(),
                    container.Resolve<IExtensionManager>(),
                    container.Resolve<IApplicationProxy>(),
                    container.Resolve<IUpmCache>(),
                    container.Resolve<IPackageManagerPrefs>(),
                    container.Resolve<IPackageDatabase>(),
                    container.Resolve<IPageManager>(),
                    container.Resolve<IUnityConnectProxy>());
            }
        }

        private readonly IExtensionManager m_ExtensionManager;
        private readonly IApplicationProxy m_Application;
        private readonly IUpmCache m_UpmCache;
        private readonly IPackageManagerPrefs m_PackageManagerPrefs;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPageManager m_PageManager;
        private readonly IUnityConnectProxy m_UnityConnectProxy;

        public PackageDetails(
            IResourceLoader resourceLoader,
            IExtensionManager extensionManager,
            IApplicationProxy application,
            IUpmCache upmCache,
            IPackageManagerPrefs packageManagerPrefs,
            IPackageDatabase packageDatabase,
            IPageManager pageManager,
            IUnityConnectProxy unityConnectProxy)
        {
            m_ExtensionManager = extensionManager;
            m_Application = application;
            m_UpmCache = upmCache;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_PackageDatabase = packageDatabase;
            m_PageManager = pageManager;
            m_UnityConnectProxy = unityConnectProxy;

            var root = resourceLoader.GetTemplate("PackageDetails.uxml");
            Add(root);
            root.StretchToParentSize();
            cache = new VisualElementCache(root);

            scrollView.verticalScroller.valueChanged += OnDetailScroll;
            scrollView.RegisterCallback<GeometryChangedEvent>(RefreshSelectedTabHeight);
            detail.RegisterCallback<GeometryChangedEvent>(RefreshSelectedTabHeight);
        }

        public void OnEnable()
        {
            body.OnEnable();
            toolbar.OnEnable();
            multiSelectDetails.OnEnable();

            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;

            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;

            m_PageManager.onSelectionChanged += OnSelectionChanged;

            m_PageManager.onVisualStateChange += OnVisualStateChange;

            m_UnityConnectProxy.onUserLoginStateChange += OnUserLoginStateChange;

            // We need this refresh because there is a small delay between OnEnable and OnCreateGUI
            // where the UI needs to be refreshed in order to keep a normal state
            Refresh(m_PageManager.activePage.GetSelection());
        }

        public void OnCreateGUI()
        {
            customContainer.Clear();

            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var extension in PackageManagerExtensions.Extensions)
                    customContainer.Add(extension.CreateExtensionUI());
            });
            PackageManagerExtensions.extensionsGUICreated = true;

            if (PackageManagerExtensions.Extensions.Count > 0)
                Refresh(m_PageManager.activePage.GetSelection());
        }

        public void OnDisable()
        {
            body.OnDisable();
            toolbar.OnDisable();
            multiSelectDetails.OnDisable();

            m_Application.onInternetReachabilityChange -= OnInternetReachabilityChange;

            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;

            m_PageManager.onSelectionChanged -= OnSelectionChanged;

            m_PageManager.onVisualStateChange -= OnVisualStateChange;

            m_UnityConnectProxy.onUserLoginStateChange -= OnUserLoginStateChange;
        }

        private void OnVisualStateChange(VisualStateChangeArgs args)
        {
            if (args.page == m_PageManager.activePage)
                Refresh(m_PageManager.activePage.GetSelection());
        }

        private void RefreshSelectedTabHeight(GeometryChangedEvent evt)
        {
            if (evt.oldRect.height == evt.newRect.height)
                return;

            tabView.GetTab(tabView.selectedTabId)?.RefreshHeight(detail.layout.height, scrollView.layout.height,
                header.layout.height, tabViewHeaderContainer.layout.height, customContainer.layout.height,
                extensionContainer.layout.height);
        }

        private void OnDetailScroll(float offset)
        {
            m_PackageManagerPrefs.packageDetailVerticalScrollOffset = offset;
        }

        private void OnInternetReachabilityChange(bool value)
        {
            Refresh();
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            Refresh();
        }

        public void OnSelectionChanged(PageSelectionChangeArgs args)
        {
            if (!args.page.isActivePage)
                return;

            m_PackageManagerPrefs.packageDetailVerticalScrollOffset = 0;
            Refresh(args.selection);
        }

        public virtual void Refresh(PageSelection selections = null)
        {
            selections ??= m_PageManager.activePage.GetSelection();
            scrollView.scrollOffset = new Vector2(0, m_PackageManagerPrefs.packageDetailVerticalScrollOffset);

            if (selections.Count == 1)
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var selection = selections.FirstOrDefault();
#pragma warning restore UA2001
                var package = m_PackageDatabase.GetPackage(selection);
                RefreshUI(package);
            }
            else
            {
                // We call Refresh(null, null) to make sure that all single package details elements are hidden properly
                // We want to hide those elements when 1) there's nothing selected or 2) the multi select details view is visible.
                RefreshUI(null);
                UIUtils.SetElementDisplay(multiSelectDetails, multiSelectDetails.Refresh(selections));
            }
        }

        private void RefreshUI(IPackage package)
        {
            var version = package?.versions.primary;
            var shouldDisplayProgress = inProgressView.ShouldDisplayProgress(package);
            inProgressView.Refresh(shouldDisplayProgress ? package : null);

            var detailVisible = package != null && version != null && !shouldDisplayProgress;
            var detailEnabled = version == null || version.isFullyFetched;

            if (!detailVisible)
            {
                RefreshExtensions(null);
            }
            else
            {
                header.Refresh(package);
                body.Refresh(package);
                toolbar.Refresh(package);
                RefreshExtensions(package);
            }

            // Set visibility
            UIUtils.SetElementDisplay(inProgressView, shouldDisplayProgress);
            UIUtils.SetElementDisplay(detail, detailVisible);
            UIUtils.SetElementDisplay(toolbar, detailVisible);
            UIUtils.SetElementDisplay(multiSelectDetails, false);

            SetEnabled(detailEnabled);
            RefreshDetailError(package, version);
        }

        void RefreshExtensions(IPackage package)
        {
            // For now packageInfo, package and packageVersion will all be null when there are multiple packages selected.
            // This way no single select UI will be displayed for multi-select. We might handle it differently in the future in a new story
            if (PackageManagerExtensions.extensionsGUICreated)
            {
                var version = package?.versions.primary;
                var packageInfo = version != null ? m_UpmCache.GetBestMatchPackageInfo(version.name, version.package.product?.id ?? 0, version.isInstalled, version.versionString) : null;
                PackageManagerExtensions.ExtensionCallback(() =>
                {
                    foreach (var extension in PackageManagerExtensions.Extensions)
                        extension.OnPackageSelectionChange(packageInfo);
                });
            }

            m_ExtensionManager.SendPackageSelectionChangedEvent(package);

            // We refresh the extension container visibility after the selection event is triggered because extensions could modify the child elements during the event
            UIUtils.SetElementDisplay(customContainer, customContainer.childCount > 0); // ExtensionV1
            UIUtils.SetElementDisplay(extensionContainer, extensionContainer.childCount > 0); // ExtensionV2
        }

        private void OnPackagesChanged(PackagesChangeArgs args)
        {
            Refresh(m_PageManager.activePage.GetSelection());
        }

        private void RefreshDetailError(IPackage package, IPackageVersion version)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var error = version?.errors?.FirstOrDefault(e => !e.HasAttribute(UIError.Attribute.Clearable | UIError.Attribute.HiddenFromUI))
#pragma warning restore UA2001
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                ?? package?.errors?.FirstOrDefault(e => !e.HasAttribute(UIError.Attribute.Clearable | UIError.Attribute.HiddenFromUI));
#pragma warning restore UA2001
            detailError.RefreshError(error, version);
        }

        private VisualElementCache cache { get; }

        private MultiSelectDetails multiSelectDetails => cache.Get<MultiSelectDetails>("multiSelectDetails");
        private InProgressView inProgressView => cache.Get<InProgressView>("inProgressView");
        private PackageDetailsHeader header => cache.Get<PackageDetailsHeader>("detailsHeader");
        private PackageDetailsBody body => cache.Get<PackageDetailsBody>("detailsBody");
        public PackageToolbar toolbar => cache.Get<PackageToolbar>("packageToolbar");

        private Alert detailError => cache.Get<Alert>("detailError");
        private ScrollView scrollView => cache.Get<ScrollView>("detailScrollView");
        private VisualElement detail => cache.Get<VisualElement>("detail");

        // customContainer is kept for Package Manager UI extension API v1 &
        // extensionContainer is for extension API v2
        private VisualElement customContainer => cache.Get<VisualElement>("detailCustomContainer");
        public VisualElement extensionContainer => cache.Get<VisualElement>("detailExtensionContainer");

        private PackageDetailsTabView tabView => cache.Get<PackageDetailsTabView>("packageDetailsTabView");
        private VisualElement tabViewHeaderContainer => cache.Get<VisualElement>("packageDetailsTabViewHeaderContainer");
    }
}
