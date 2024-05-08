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
        [SerializeField]
        internal new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new PackageDetails();
        }

        private IResourceLoader m_ResourceLoader;
        private IExtensionManager m_ExtensionManager;
        private IApplicationProxy m_Application;
        private IUpmCache m_UpmCache;
        private IPackageManagerPrefs m_PackageManagerPrefs;
        private IPackageDatabase m_PackageDatabase;
        private IPageManager m_PageManager;
        private IUnityConnectProxy m_UnityConnectProxy;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<IResourceLoader>();
            m_ExtensionManager = container.Resolve<IExtensionManager>();
            m_Application = container.Resolve<IApplicationProxy>();
            m_UpmCache = container.Resolve<IUpmCache>();
            m_PackageManagerPrefs = container.Resolve<IPackageManagerPrefs>();
            m_PackageDatabase = container.Resolve<IPackageDatabase>();
            m_PageManager = container.Resolve<IPageManager>();
            m_UnityConnectProxy = container.Resolve<IUnityConnectProxy>();
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
                var selection = selections.FirstOrDefault();
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
            UIUtils.SetElementDisplay(customContainer, customContainer.childCount > 0); // ExtensionV1
            UIUtils.SetElementDisplay(extensionContainer, extensionContainer.childCount > 0); // ExtensionV2

            // For now packageInfo, package and packageVersion will all be null when there are multiple packages selected.
            // This way no single select UI will be displayed for multi-select. We might handle it differently in the future in a new story
            if (PackageManagerExtensions.extensionsGUICreated)
            {
                var version = package?.versions.primary;
                var packageInfo = version != null ? m_UpmCache.GetBestMatchPackageInfo(version.name, version.isInstalled, version.versionString) : null;
                PackageManagerExtensions.ExtensionCallback(() =>
                {
                    foreach (var extension in PackageManagerExtensions.Extensions)
                        extension.OnPackageSelectionChange(packageInfo);
                });
            }

            m_ExtensionManager.SendPackageSelectionChangedEvent(package);
        }

        private void OnPackagesChanged(PackagesChangeArgs args)
        {
            Refresh(m_PageManager.activePage.GetSelection());
        }

        private void RefreshDetailError(IPackage package, IPackageVersion version)
        {
            var error = version?.errors?.FirstOrDefault(e => !e.HasAttribute(UIError.Attribute.Clearable | UIError.Attribute.HiddenFromUI))
                ?? package?.errors?.FirstOrDefault(e => !e.HasAttribute(UIError.Attribute.Clearable | UIError.Attribute.HiddenFromUI));
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
