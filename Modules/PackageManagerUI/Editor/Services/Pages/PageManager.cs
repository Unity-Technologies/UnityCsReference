// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IPageManager : IService
    {
        event Action<IPage> onActivePageChanged;
        event Action<IPage> onListRebuild;
        event Action<IPage, PageFilters> onFiltersChange;
        event Action<IPage, string> onTrimmedSearchTextChanged;
        event Action<PageSelectionChangeArgs> onSelectionChanged;
        event Action<VisualStateChangeArgs> onVisualStateChange;
        event Action<ListUpdateArgs> onListUpdate;
        event Action<IPage> onSupportedStatusFiltersChanged;

        IPage lastActivePage { get; }
        IPage activePage { get; set; }
        IEnumerable<IPage> orderedExtensionPages { get; }

        void AddExtensionPage(ExtensionPageArgs args);
        IPage GetPage(string pageId);
        IPage GetPage(RegistryInfo registryInfo);
        IPage FindPage(IPackage package, IPackageVersion version = null);
        IPage FindPage(IList<IPackageVersion> packageVersions);

        void OnWindowDestroy();
    }

    [Serializable]
    internal class PageManager : BaseService<IPageManager>, IPageManager, ISerializationCallbackReceiver
    {
        public const string k_DefaultPageId = InProjectPage.k_Id;

        public event Action<IPage> onActivePageChanged = delegate {};
        public event Action<IPage> onListRebuild = delegate {};
        public event Action<IPage, PageFilters> onFiltersChange = delegate {};
        public event Action<IPage, string> onTrimmedSearchTextChanged = delegate {};
        public event Action<PageSelectionChangeArgs> onSelectionChanged = delegate {};
        public event Action<VisualStateChangeArgs> onVisualStateChange = delegate {};
        public event Action<ListUpdateArgs> onListUpdate = delegate {};
        public event Action<IPage> onSupportedStatusFiltersChanged = delegate {};

        private Dictionary<string, IPage> m_Pages = new();

        [SerializeField]
        private string m_SerializedLastActivePageId;
        [SerializeField]
        private string m_SerializedActivePageId;
        public IPage lastActivePage { get; private set; }
        private IPage m_ActivePage;
        public IPage activePage
        {
            get
            {
                m_ActivePage ??= m_Pages.Values.FirstOrDefault(p => p.isActivePage);
                if (m_ActivePage != null)
                    return m_ActivePage;
                m_ActivePage = GetPage(k_DefaultPageId);
                m_ActivePage.OnActivated();
                return m_ActivePage;
            }
            set
            {
                lastActivePage = activePage;
                m_ActivePage = value;
                if (activePage == lastActivePage)
                    return;

                activePage.OnActivated();
                lastActivePage?.OnDeactivated();
                onActivePageChanged?.Invoke(activePage);
            }
        }

        [NonSerialized]
        private List<ExtensionPageArgs> m_OrderedExtensionPageArgs = new();
        public IEnumerable<IPage> orderedExtensionPages => m_OrderedExtensionPageArgs.Select(a => GetPage(a.id));

        [SerializeReference]
        private IPage[] m_SerializedPages = Array.Empty<IPage>();


        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IProjectSettingsProxy m_SettingsProxy;
        private readonly IUpmRegistryClient m_UpmRegistryClient;
        private readonly IPageFactory m_PageFactory;

        public PageManager(IUnityConnectProxy unityConnect,
            IPackageDatabase packageDatabase,
            IProjectSettingsProxy settingsProxy,
            IUpmRegistryClient upmRegistryClient,
            IPageFactory pageFactory)
        {
            m_UnityConnect = RegisterDependency(unityConnect);
            m_PackageDatabase = RegisterDependency(packageDatabase);
            m_SettingsProxy = RegisterDependency(settingsProxy);
            m_UpmRegistryClient = RegisterDependency(upmRegistryClient);
            m_PageFactory = RegisterDependency(pageFactory);
        }

        public void OnBeforeSerialize()
        {
            m_SerializedPages = m_Pages.Values.ToArray();
            m_SerializedLastActivePageId = lastActivePage?.id ?? string.Empty;
            m_SerializedActivePageId = m_ActivePage?.id ?? string.Empty;
        }

        public void OnAfterDeserialize()
        {
            foreach (var page in m_SerializedPages)
            {
                m_Pages[page.id] = page;
                RegisterPageEvents(page);
            }
            lastActivePage = string.IsNullOrEmpty(m_SerializedLastActivePageId) ? null : GetPage(m_SerializedLastActivePageId);
            m_ActivePage = string.IsNullOrEmpty(m_SerializedActivePageId) ? null : GetPage(m_SerializedActivePageId);

            foreach (var page in m_Pages.Values)
                m_PageFactory.ResolveDependenciesForPage(page);
        }

        private IPage CreatePageFromId(string pageId)
        {
            var page = m_PageFactory.CreatePageFromId(pageId);
            return page != null ? OnNewPageCreated(page) : null;
        }

        private IPage OnNewPageCreated(IPage page)
        {
            page.OnEnable();
            m_Pages[page.id] = page;
            RegisterPageEvents(page);
            return page;
        }

        private void RegisterPageEvents(IPage page)
        {
            page.onVisualStateChange += args => onVisualStateChange?.Invoke(args);
            page.onListUpdate += args => onListUpdate?.Invoke(args);
            page.onSelectionChanged += args => onSelectionChanged?.Invoke(args);
            page.onListRebuild += p => onListRebuild?.Invoke(p);
            page.onFiltersChange += filters => onFiltersChange?.Invoke(page, filters);
            page.onTrimmedSearchTextChanged += text => onTrimmedSearchTextChanged?.Invoke(page, text);
            page.onSupportedStatusFiltersChanged += p => onSupportedStatusFiltersChanged?.Invoke(p);
        }

        public void AddExtensionPage(ExtensionPageArgs args)
        {
            if (string.IsNullOrEmpty(args.name))
            {
                Debug.LogWarning(L10n.Tr("An extension page needs to have a non-empty unique name."));
                return;
            }

            if (m_OrderedExtensionPageArgs.Any(a => a.name == args.name))
            {
                Debug.LogWarning(string.Format(L10n.Tr("An extension page with name {0} already exists. Please use a different name."), args.name));
                return;
            }

            m_OrderedExtensionPageArgs.Add(args);
            m_OrderedExtensionPageArgs.Sort((x, y) => x.priority - y.priority);

            // Since the pages are serialized but m_OrderedExtensionPageArgs is not serialized, after domain reload
            // we will find an existing page even though we already checked for duplicates earlier. This is expected,
            // we will use as much of the existing page as we and update the fields that cannot be serialized (the functions)
            if (m_Pages.Get(ExtensionPage.GetIdFromName(args.name)) is ExtensionPage existingPage)
                existingPage.UpdateArgs(args);
            else
                OnNewPageCreated(m_PageFactory.CreateExtensionPage(args));
        }

        public IPage GetPage(string pageId)
        {
            return !string.IsNullOrEmpty(pageId) && m_Pages.TryGetValue(pageId, out var page) ? page : CreatePageFromId(pageId);
        }

        public IPage GetPage(RegistryInfo registryInfo)
        {
            if (registryInfo == null)
                return null;
            var pageId = ScopedRegistryPage.GetIdFromRegistry(registryInfo);
            return m_Pages.TryGetValue(pageId, out var page) ? page : OnNewPageCreated(m_PageFactory.CreateScopedRegistryPage(registryInfo));
        }

        private void OnPackagesChanged(PackagesChangeArgs args)
        {
            activePage.OnPackagesChanged(args);
        }

        private void OnRegistriesModified()
        {
            // Here we only want to remove outdated pages and update existing pages when needed
            // We will delay the creation of new pages to when the UI is displaying them to save some resources
            var scopedRegistries = m_SettingsProxy.scopedRegistries.ToDictionary(r => r.id, r => r);
            var scopedRegistryPages = m_Pages.Values.OfType<ScopedRegistryPage>().ToArray();
            var pagesToRemove = new HashSet<string>();
            foreach (var page in scopedRegistryPages)
            {
                if (scopedRegistries.TryGetValue(page.registry.id, out var registryInfo))
                    page.UpdateRegistry(registryInfo);
                else
                    pagesToRemove.Add(page.id);
            }

            if (pagesToRemove.Contains(activePage.id))
                activePage = GetPage(k_DefaultPageId);

            foreach (var pageId in pagesToRemove)
                m_Pages.Remove(pageId);

            if (scopedRegistries.Count > 0)
                return;
            GetPage(MyRegistriesPage.k_Id).ClearFilters(true);
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            GetPage(MyAssetsPage.k_Id).ClearFilters(true);
        }

        public IPage FindPage(IPackage package, IPackageVersion version = null)
        {
            return FindPage(new[] { version ?? package?.versions.primary });
        }

        public IPage FindPage(IList<IPackageVersion> packageVersions)
        {
            if (packageVersions?.Any() != true || packageVersions.All(v => activePage.visualStates.Contains(v.package.uniqueId) || activePage.ShouldInclude(v.package)))
                return activePage;

            var pageIdsToCheck = new[] { BuiltInPage.k_Id, InProjectPage.k_Id, UnityRegistryPage.k_Id, MyAssetsPage.k_Id, MyRegistriesPage.k_Id};
            foreach (var page in pageIdsToCheck.Select(GetPage).Where(p => !p.isActivePage))
                if (packageVersions.All(v => page.ShouldInclude(v.package)))
                    return page;

            if (!m_SettingsProxy.enablePreReleasePackages && packageVersions.Any(v => v.version?.Prerelease.StartsWith("pre.") == true))
                Debug.Log(L10n.Tr("You must check \"Enable Pre-release Packages\" in Project Settings > Package Manager in order to see this package."));
            return null;
        }

        [ExcludeFromCodeCoverage]
        public override void OnEnable()
        {
            foreach (var page in m_Pages.Values)
                page.OnEnable();

            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
            m_UpmRegistryClient.onRegistriesModified += OnRegistriesModified;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
        }

        [ExcludeFromCodeCoverage]
        public override void OnDisable()
        {
            foreach (var page in m_Pages.Values)
                page.OnDisable();

            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
            m_UpmRegistryClient.onRegistriesModified -= OnRegistriesModified;
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
        }

        public void OnWindowDestroy()
        {
            // Since extension pages are added on Window creation time we need to clear them on Window destroy, so the next
            // time the Package Manager window is created (which would happen when it is closed and reopened), we will not
            // report a false alarm of page name duplication.
            m_OrderedExtensionPageArgs.Clear();
        }
    }
}
