// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class PageManager : ISerializationCallbackReceiver
    {
        public const string k_DefaultPageId = InProjectPage.k_Id;

        public virtual event Action<IPage> onActivePageChanged = delegate {};
        public virtual event Action<IPage> onListRebuild = delegate {};
        public virtual event Action<IPage, PageFilters> onFiltersChange = delegate {};
        public virtual event Action<IPage, string> onTrimmedSearchTextChanged = delegate {};
        public virtual event Action<PageSelectionChangeArgs> onSelectionChanged = delegate {};
        public virtual event Action<VisualStateChangeArgs> onVisualStateChange = delegate {};
        public virtual event Action<ListUpdateArgs> onListUpdate = delegate {};

        private Dictionary<string, IPage> m_Pages = new();

        [SerializeField]
        private string m_SerializedLastActivePageId;
        [SerializeField]
        private string m_SerializedActivePageId;
        public virtual IPage lastActivePage { get; private set; }
        private IPage m_ActivePage;
        public virtual IPage activePage
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
        public virtual IEnumerable<IPage> orderedExtensionPages => m_OrderedExtensionPageArgs.Select(a => GetPage(a.id));

        [SerializeReference]
        private IPage[] m_SerializedPages = Array.Empty<IPage>();

        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private PackageManagerPrefs m_PackageManagerPrefs;
        [NonSerialized]
        private AssetStoreClientV2 m_AssetStoreClient;
        [NonSerialized]
        private PackageDatabase m_PackageDatabase;
        [NonSerialized]
        private UpmCache m_UpmCache;
        [NonSerialized]
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        [NonSerialized]
        private UpmRegistryClient m_UpmRegistryClient;
        public void ResolveDependencies(UnityConnectProxy unityConnect,
                                        PackageManagerPrefs packageManagerPrefs,
                                        AssetStoreClientV2 assetStoreClient,
                                        PackageDatabase packageDatabase,
                                        UpmCache upmCache,
                                        PackageManagerProjectSettingsProxy settingsProxy,
                                        UpmRegistryClient upmRegistryClient)
        {
            m_UnityConnect = unityConnect;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_AssetStoreClient = assetStoreClient;
            m_PackageDatabase = packageDatabase;
            m_UpmCache = upmCache;
            m_SettingsProxy = settingsProxy;
            m_UpmRegistryClient = upmRegistryClient;

            foreach (var page in m_Pages.Values)
            {
                switch (page)
                {
                    case MyAssetsPage myAssetsPage:
                        myAssetsPage.ResolveDependencies(packageDatabase, packageManagerPrefs, unityConnect, assetStoreClient);
                        break;
                    case ScopedRegistryPage scopedRegistryPage:
                        scopedRegistryPage.ResolveDependencies(packageDatabase, upmCache);
                        break;
                    case SimplePage simplePage:
                        simplePage.ResolveDependencies(packageDatabase);
                        break;
                }
            }
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
        }

        private IPage CreatePageFromId(string pageId)
        {
            IPage page = pageId switch
            {
                UnityRegistryPage.k_Id => new UnityRegistryPage(m_PackageDatabase),
                InProjectPage.k_Id => new InProjectPage(m_PackageDatabase),
                BuiltInPage.k_Id => new BuiltInPage(m_PackageDatabase),
                MyRegistriesPage.k_Id => new MyRegistriesPage(m_PackageDatabase),
                MyAssetsPage.k_Id => new MyAssetsPage(m_PackageDatabase, m_PackageManagerPrefs, m_UnityConnect, m_AssetStoreClient),
                _ => null
            };
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
        }

        public virtual void AddExtensionPage(ExtensionPageArgs args)
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
                OnNewPageCreated(new ExtensionPage(m_PackageDatabase, args));
        }

        public virtual IPage GetPage(string pageId)
        {
            return !string.IsNullOrEmpty(pageId) && m_Pages.TryGetValue(pageId, out var page) ? page : CreatePageFromId(pageId);
        }

        public virtual IPage GetPage(RegistryInfo registryInfo)
        {
            if (registryInfo == null)
                return null;
            var pageId = ScopedRegistryPage.GetIdFromRegistry(registryInfo);
            if (m_Pages.TryGetValue(pageId, out var page))
                return page;
            var newPage = new ScopedRegistryPage(m_PackageDatabase, m_UpmCache, registryInfo);
            return OnNewPageCreated(newPage);
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

        public virtual IPage FindPage(IPackage package, IPackageVersion version = null)
        {
            return FindPage(new[] { version ?? package?.versions.primary });
        }

        public virtual IPage FindPage(IList<IPackageVersion> packageVersions)
        {
            if (packageVersions?.Any() != true || packageVersions.All(v => activePage.visualStates.Contains(v.package.uniqueId)))
                return activePage;

            var pageIdsToCheck = new[] { BuiltInPage.k_Id, InProjectPage.k_Id, UnityRegistryPage.k_Id, MyAssetsPage.k_Id, MyRegistriesPage.k_Id};
            foreach (var page in pageIdsToCheck.Select(GetPage).Where(p => !p.isActivePage))
                if (packageVersions.All(v => page.ShouldInclude(v.package)))
                    return page;

            if (!m_SettingsProxy.enablePreReleasePackages && packageVersions.Any(v => v.version?.Prerelease.StartsWith("pre.") == true))
                Debug.Log("You must check \"Enable Pre-release Packages\" in Project Settings > Package Manager in order to see this package.");
            return null;
        }

        public void OnEnable()
        {
            foreach (var page in m_Pages.Values)
                page.OnEnable();

            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
            m_UpmRegistryClient.onRegistriesModified += OnRegistriesModified;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
        }

        public void OnDisable()
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
