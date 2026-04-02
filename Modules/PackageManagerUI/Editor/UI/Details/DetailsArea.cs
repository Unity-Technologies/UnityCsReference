// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal sealed class DetailsArea : VisualElement
    {
        [System.Serializable]
        internal new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance()
            {
                var container = ServicesContainer.instance;
                return new DetailsArea(
                    container.Resolve<IResourceLoader>(),
                    container.Resolve<IApplicationProxy>(),
                    container.Resolve<IPackageDatabase>(),
                    container.Resolve<IPackageOperationDispatcher>(),
                    container.Resolve<IPageManager>(),
                    container.Resolve<IPackageManagerPrefs>(),
                    container.Resolve<IAssetStoreClient>(),
                    container.Resolve<IAssetStoreDownloadManager>(),
                    container.Resolve<IAssetStoreCache>(),
                    container.Resolve<IBackgroundFetchHandler>(),
                    container.Resolve<IUnityConnectProxy>(),
                    container.Resolve<IExtensionManager>(),
                    container.Resolve<IUpmCache>(),
                    container.Resolve<IIOProxy>(),
                    container.Resolve<ISampleImporter>());
            }
        }

        private readonly ScrollView m_ScrollView;

        private readonly PackageDetails m_PackageDetails;
        private SampleDetails m_SampleDetails;
        private MultiSelectDetails m_MultiSelectDetails;

        private BaseDetailsView m_CurrentView;

        public VisualElement toolbarExtensions => m_PackageDetails.toolbar.extensions;
        public VisualElement extensionContainer => m_PackageDetails.extensionContainer;
        public VisualElement legacyExtensionContainer => m_PackageDetails.legacyExtensionContainer;

        private readonly IResourceLoader m_ResourceLoader;
        private readonly IApplicationProxy m_Application;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPackageOperationDispatcher m_OperationDispatcher;
        private readonly IPageManager m_PageManager;
        private readonly IPackageManagerPrefs m_PackageManagerPrefs;
        private readonly IAssetStoreClient m_AssetStoreClient;
        private readonly IAssetStoreDownloadManager m_AssetStoreDownloadManager;
        private readonly IAssetStoreCache m_AssetStoreCache;
        private readonly IBackgroundFetchHandler m_BackgroundFetchHandler;
        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IExtensionManager m_ExtensionManager;
        private readonly IUpmCache m_UpmCache;
        private readonly IIOProxy m_IOProxy;
        private readonly ISampleImporter m_SampleImporter;
        public DetailsArea(
            IResourceLoader resourceLoader,
            IApplicationProxy application,
            IPackageDatabase packageDatabase,
            IPackageOperationDispatcher operationDispatcher,
            IPageManager pageManager,
            IPackageManagerPrefs packageManagerPrefs,
            IAssetStoreClient assetStoreClient,
            IAssetStoreDownloadManager assetStoreDownloadManager,
            IAssetStoreCache assetStoreCache,
            IBackgroundFetchHandler backgroundFetchHandler,
            IUnityConnectProxy unityConnect,
            IExtensionManager extensionManager,
            IUpmCache upmCache,
            IIOProxy ioProxy,
            ISampleImporter sampleImporter)
        {
            m_ResourceLoader = resourceLoader;
            m_Application = application;
            m_PackageDatabase = packageDatabase;
            m_OperationDispatcher = operationDispatcher;
            m_PageManager = pageManager;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_AssetStoreClient = assetStoreClient;
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_AssetStoreCache = assetStoreCache;
            m_BackgroundFetchHandler = backgroundFetchHandler;
            m_UnityConnect = unityConnect;
            m_ExtensionManager = extensionManager;
            m_UpmCache = upmCache;
            m_IOProxy = ioProxy;
            m_SampleImporter = sampleImporter;

            m_ScrollView = new ScrollView { name = "detailScrollView" };
            // We set the style here programatically instead of in the uss so that our style doesn't depend on a class or name that could change in the future
            m_ScrollView.contentContainer.style.minHeight = new Length(100.0f, LengthUnit.Percent);
            m_ScrollView.verticalScroller.valueChanged += OnDetailScroll;
            Add(m_ScrollView);

            // PackageDetails is the only details view we create by default, because it is needed by the extension mechanism on Window creation time
            // However, we don't add it to the parent container until it's needed
            m_PackageDetails = new PackageDetails(m_ResourceLoader, m_Application, m_PackageDatabase, m_PageManager, m_UnityConnect) { name = "packageDetails" };

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_PageManager.onActivePageChanged += OnActivePageChanged;
            m_PageManager.onSelectionChanged += OnSelectionChanged;
            m_PageManager.onVisualStateChange += OnVisualStateChange;

            Refresh(m_PageManager.activePage.GetSelection());
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_PageManager.onActivePageChanged -= OnActivePageChanged;
            m_PageManager.onSelectionChanged -= OnSelectionChanged;
            m_PageManager.onVisualStateChange -= OnVisualStateChange;
        }

        private void OnActivePageChanged(IPage page)
        {
            Refresh(m_PageManager.activePage.GetSelection());
        }

        private void OnVisualStateChange(VisualStateChangeArgs args)
        {
            if (!args.page.isActive)
                return;

            Refresh(args.page.GetSelection());
        }

        public void OnSelectionChanged(PageSelectionChangeArgs args)
        {
            if (!args.page.isActive)
                return;

            m_PackageManagerPrefs.detailVerticalScrollOffset = 0;
            Refresh(args.selection);
        }

        private void OnDetailScroll(float offset)
        {
            m_PackageManagerPrefs.detailVerticalScrollOffset = offset;
        }

        private BaseDetailsView UpdateCurrentViewIfNeeded(PageSelection selections)
        {
            BaseDetailsView newView = null;
            switch (selections.Count)
            {
                case 1 when m_PageManager.activePage.id == SamplesPage.k_Id:
                    newView = m_SampleDetails ??= new SampleDetails(m_PackageDatabase, m_PageManager, m_ResourceLoader) { name = "sampleDetails"};
                    break;
                case 1:
                    newView = m_PackageDetails;
                    break;
                case > 1:
                    newView = m_MultiSelectDetails ??= new MultiSelectDetails(m_ResourceLoader, m_Application, m_PackageDatabase,
                        m_OperationDispatcher, m_PageManager, m_PackageManagerPrefs, m_AssetStoreClient, m_AssetStoreDownloadManager,
                        m_AssetStoreCache, m_BackgroundFetchHandler, m_UnityConnect, m_IOProxy, m_SampleImporter) { name = "multiSelectDetails" };
                    break;
            }

            if (newView != m_CurrentView)
            {
                m_CurrentView = newView;
                m_ScrollView.Clear();
                if (m_CurrentView != null)
                    m_ScrollView.Add(m_CurrentView);
            }
            return m_CurrentView;
        }

        public void Refresh(PageSelection selections)
        {
            var selectedPackage = selections.Count == 1 && m_PageManager.activePage.id != SamplesPage.k_Id ? m_PackageDatabase.GetPackage(selections.first) : null;
            RefreshExtensions(selectedPackage);

            var currentView = UpdateCurrentViewIfNeeded(selections);
            currentView?.Refresh(selections);

            m_ScrollView.scrollOffset = new Vector2(0, m_PackageManagerPrefs.detailVerticalScrollOffset);
        }

        private void RefreshExtensions(IPackage package)
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
            UIUtils.SetElementDisplay(legacyExtensionContainer, legacyExtensionContainer.childCount > 0); // ExtensionV1
            UIUtils.SetElementDisplay(extensionContainer, extensionContainer.childCount > 0); // ExtensionV2
        }
    }
}
