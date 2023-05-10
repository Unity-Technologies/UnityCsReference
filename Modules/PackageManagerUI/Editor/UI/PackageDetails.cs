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
        internal new class UxmlFactory : UxmlFactory<PackageDetails> {}

        private ResourceLoader m_ResourceLoader;
        private ExtensionManager m_ExtensionManager;
        private ApplicationProxy m_Application;
        private UpmCache m_UpmCache;
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
            m_UpmCache = container.Resolve<UpmCache>();
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

            if (PackageManagerExtensions.Extensions.Any())
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

            m_UnityConnectProxy.onUserLoginStateChange -= OnUserLoginStateChange;
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

        internal void OnSelectionChanged(PageSelectionChangeArgs args)
        {
            if (!args.page.isActivePage)
                return;

            m_PackageManagerPrefs.packageDetailVerticalScrollOffset = 0;
            Refresh(args.selection);
        }

        internal void Refresh(PageSelection selections = null)
        {
            selections = selections ?? m_PageManager.activePage.GetSelection();
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
            var shouldDisplayProgress = inProgressView.ShouldDisplayProgress(package);
            inProgressView.Refresh(shouldDisplayProgress ? package : null);

            var detailVisible = package != null && version != null && !shouldDisplayProgress;
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
            UIUtils.SetElementDisplay(inProgressView, shouldDisplayProgress);
            UIUtils.SetElementDisplay(detail, detailVisible);
            UIUtils.SetElementDisplay(toolbar, detailVisible);
            UIUtils.SetElementDisplay(multiSelectDetails, false);

            SetEnabled(detailEnabled);
            RefreshDetailError(package, version);
        }

        void RefreshExtensions(IPackage package, IPackageVersion version)
        {
            UIUtils.SetElementDisplay(customContainer, customContainer.childCount > 0); // ExtensionV1
            UIUtils.SetElementDisplay(extensionContainer, extensionContainer.childCount > 0); // ExtensionV2

            // For now packageInfo, package and packageVersion will all be null when there are multiple packages selected.
            // This way no single select UI will be displayed for multi-select. We might handle it differently in the future in a new story
            if (PackageManagerExtensions.extensionsGUICreated)
            {
                var packageInfo = version != null ? m_UpmCache.GetBestMatchPackageInfo(version.name, version.isInstalled, version.versionString) : null;
                PackageManagerExtensions.ExtensionCallback(() =>
                {
                    foreach (var extension in PackageManagerExtensions.Extensions)
                        extension.OnPackageSelectionChange(packageInfo);
                });
            }

            m_ExtensionManager.SendPackageSelectionChangedEvent(package, version);
        }

        private void OnPackagesChanged(PackagesChangeArgs args)
        {
            var selection = m_PageManager.activePage.GetSelection();
            if (args.added.Concat(args.removed).Concat(args.updated).Any(p => selection.Contains(p.uniqueId)))
                Refresh(selection);
        }

        private void RefreshDetailError(IPackage package, IPackageVersion version)
        {
            var error = version?.errors?.FirstOrDefault(e => !e.HasAttribute(UIError.Attribute.Clearable | UIError.Attribute.HiddenFromUI))
                ?? package?.errors?.FirstOrDefault(e => !e.HasAttribute(UIError.Attribute.Clearable | UIError.Attribute.HiddenFromUI));
            detailError.RefreshError(error, version);
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

        private PackageDetailsTabView tabView => cache.Get<PackageDetailsTabView>("packageDetailsTabView");
        private VisualElement tabViewHeaderContainer => cache.Get<VisualElement>("packageDetailsTabViewHeaderContainer");
    }
}
