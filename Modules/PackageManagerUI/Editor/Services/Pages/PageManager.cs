// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IPageManager : IService
    {
        event Action<IPage> onActivePageChanged;
        event Action<IPage> onListRebuild;
        event Action<PageFiltersChangeArgs> onFiltersChange;
        event Action<IPage> onTrimmedSearchTextChanged;
        event Action<PageSelectionChangeArgs> onSelectionChanged;
        event Action<VisualStateChangeArgs> onVisualStateChange;
        event Action<ListUpdateArgs> onListUpdate;
        event Action<PageStateChangeArgs> onStateChanged;
        event Action onExtensionPagesChanged;
        event Action onScopedRegistryPagesChanged;

        IPage lastActivePage { get; }
        IPage activePage { get; set; }
        IEnumerable<IPage> orderedExtensionPages { get; }
        IEnumerable<IPage> orderedScopedRegistryPages { get; }

        void AddExtensionPage(ExtensionPageArgs args);
        IPage GetPage(string pageId);
        IPage FindPage(IPackage package, string pageIdToPrioritize = null);

        void OnWindowDestroy();
    }

    [Serializable]
    internal class PageManager : BaseService<IPageManager>, IPageManager, ISerializationCallbackReceiver
    {
        public const string k_DefaultPageId = InProjectPage.k_Id;

        public event Action<IPage> onActivePageChanged = delegate {};
        public event Action<IPage> onListRebuild = delegate {};
        public event Action<PageFiltersChangeArgs> onFiltersChange = delegate {};
        public event Action<IPage> onTrimmedSearchTextChanged = delegate {};
        public event Action<PageSelectionChangeArgs> onSelectionChanged = delegate {};
        public event Action<VisualStateChangeArgs> onVisualStateChange = delegate {};
        public event Action<ListUpdateArgs> onListUpdate = delegate {};
        public event Action<PageStateChangeArgs> onStateChanged = delegate {};
        public event Action onExtensionPagesChanged = delegate {};
        public event Action onScopedRegistryPagesChanged = delegate {};

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
                m_ActivePage ??= m_Pages.Values.FirstMatch(p => p.isActive);
                if (m_ActivePage != null)
                    return m_ActivePage;
                m_ActivePage = GetPage(k_DefaultPageId);
                m_ActivePage.Activate();
                return m_ActivePage;
            }
            set
            {
                if (activePage == value)
                    return;

                lastActivePage = activePage;
                m_ActivePage = value;

                activePage.Activate();
                lastActivePage?.Deactivate();
                onActivePageChanged?.Invoke(activePage);
            }
        }

        [NonSerialized]
        private List<ExtensionPageArgs> m_OrderedExtensionPageArgs = new();
        public IEnumerable<IPage> orderedExtensionPages => m_OrderedExtensionPageArgs.SelectAsEnumerable(a => GetPage(a.id));

        [SerializeField]
        private bool m_ScopedRegistryPagesInitialized = false;
        [SerializeField]
        private string[] m_OrderedScopedRegistryPageIds = Array.Empty<string>();
        public IEnumerable<IPage> orderedScopedRegistryPages
        {
            get
            {
                if (!m_ScopedRegistryPagesInitialized)
                    UpdateScopedRegistryPages(false);
                return m_OrderedScopedRegistryPageIds.SelectAsEnumerable(GetPage);
            }
        }

        [SerializeReference]
        private IPage[] m_SerializedPages = Array.Empty<IPage>();

        private readonly IProjectSettingsProxy m_SettingsProxy;
        private readonly IUpmRegistryClient m_UpmRegistryClient;
        private readonly IPageFactory m_PageFactory;

        public PageManager(IProjectSettingsProxy settingsProxy,
            IUpmRegistryClient upmRegistryClient,
            IPageFactory pageFactory)
        {
            m_SettingsProxy = RegisterDependency(settingsProxy);
            m_UpmRegistryClient = RegisterDependency(upmRegistryClient);
            m_PageFactory = RegisterDependency(pageFactory);
        }

        public void OnBeforeSerialize()
        {
            m_Pages.Values.ToArray(ref m_SerializedPages);
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

        private IPage OnNewPageCreated(IPage page)
        {
            if (page != null)
            {
                page.OnEnable();
                m_Pages[page.id] = page;
                RegisterPageEvents(page);
            }
            return page;
        }

        private void RegisterPageEvents(IPage page)
        {
            page.onVisualStateChange += args => onVisualStateChange?.Invoke(args);
            page.onListUpdate += args => onListUpdate?.Invoke(args);
            page.onSelectionChanged += args => onSelectionChanged?.Invoke(args);
            page.onListRebuild += () => onListRebuild?.Invoke(page);
            page.onStageChanged += args =>
            {
                if (page.isActive && !page.visible)
                    activePage = GetPage(k_DefaultPageId);
                onStateChanged?.Invoke(args);
            };
            page.onFiltersChanged += args => onFiltersChange?.Invoke(args);
            page.onTrimmedSearchTextChanged += () => onTrimmedSearchTextChanged?.Invoke(page);
        }

        public void AddExtensionPage(ExtensionPageArgs args)
        {
            if (string.IsNullOrEmpty(args.name))
            {
                Debug.LogWarning(L10n.Tr("An extension page needs to have a non-empty unique name."));
                return;
            }

            if (m_OrderedExtensionPageArgs.Exists(a => a.name == args.name))
            {
                Debug.LogWarning(string.Format(L10n.Tr("An extension page with name {0} already exists. Please use a different name."), args.name));
                return;
            }

            m_OrderedExtensionPageArgs.Add(args);
            m_OrderedExtensionPageArgs.Sort((x, y) => x.priority - y.priority);

            // Since the pages are serialized but m_OrderedExtensionPageArgs is not serialized, after domain reload
            // we will find an existing page even though we already checked for duplicates earlier. This is expected,
            // we will use as much of the existing page as we can and update the fields that cannot be serialized (the functions)
            if (m_Pages.Get(ExtensionPage.GetIdFromName(args.name)) is ExtensionPage existingPage)
                existingPage.UpdateArgs(args);
            else
                OnNewPageCreated(m_PageFactory.CreateExtensionPage(args));

            onExtensionPagesChanged?.Invoke();
        }

        public IPage GetPage(string pageId)
        {
            if (string.IsNullOrEmpty(pageId))
                return null;
            return m_Pages.TryGetValue(pageId, out var page) ? page : OnNewPageCreated(m_PageFactory.CreatePageFromId(pageId));
        }

        public IPage<T> GetPage<T>(string pageId)
        {
            return GetPage(pageId) as IPage<T>;
        }

        private void UpdateScopedRegistryPages(bool triggerChangeEvent)
        {
            string[] pageIdsToRemove;
            var numPageCreated = 0;
            var scopedRegistries = m_SettingsProxy.scopedRegistries;
            if (scopedRegistries.Count == 0)
            {
                pageIdsToRemove = m_OrderedScopedRegistryPageIds;
                m_OrderedScopedRegistryPageIds = Array.Empty<string>();
            }
            else
            {
                var myRegistriesPage = GetPage(MyRegistriesPage.k_Id) ?? OnNewPageCreated(m_PageFactory.CreateMyRegistriesPage());
                var newOrderedScopedRegistryPageIds = new List<string> { myRegistriesPage.id };

                var pagesToReuse = m_Pages.Values.FilterByType<ScopedRegistryPage>().ToNewDictionary(p => p.scopedRegistry.id);
                foreach (var registry in scopedRegistries)
                {
                    // We remove the page after reusing it so that whatever is left behind at the end will become the list of not reusable pages to remove,
                    // these non-reusable pages corresponds to scoped registries that has been removed
                    if (pagesToReuse.Remove(registry.id, out var page))
                    {
                        newOrderedScopedRegistryPageIds.Add(page.id);
                        page.UpdateRegistry(registry);
                    }
                    else
                    {
                        newOrderedScopedRegistryPageIds.Add(OnNewPageCreated(m_PageFactory.CreateScopedRegistryPage(registry)).id);
                        ++numPageCreated;
                    }
                }
                pageIdsToRemove = pagesToReuse.Values.SelectToNewArray(p => p.id);
                m_OrderedScopedRegistryPageIds = newOrderedScopedRegistryPageIds.ToArray();
            }

            if (pageIdsToRemove.ContainsMatches(activePage.id))
                activePage = GetPage(k_DefaultPageId);

            foreach (var pageId in pageIdsToRemove)
                m_Pages.Remove(pageId);

            m_ScopedRegistryPagesInitialized = true;

            if (triggerChangeEvent && pageIdsToRemove.Length + numPageCreated > 0)
                onScopedRegistryPagesChanged?.Invoke();
        }

        private void OnRegistriesModified()
        {
            UpdateScopedRegistryPages(true);
        }

        public IPage FindPage(IPackage package, string pageIdToPrioritize = null)
        {
            if (package == null)
                return null;

            var pagesChecked = new HashSet<string>();
            var pageIdsToCheck = new[] { pageIdToPrioritize, activePage.id, BuiltInPage.k_Id, InProjectPage.k_Id, UnityRegistryPage.k_Id, MyAssetsPage.k_Id, MyRegistriesPage.k_Id };
            foreach (var pageId in pageIdsToCheck)
            {
                if (string.IsNullOrEmpty(pageId) || !pagesChecked.Add(pageId))
                    continue;
                var page = GetPage<IPackage>(pageId);
                if (page == null)
                    continue;
                if (page.visualStates.Contains(package.uniqueId) || page.ShouldInclude(package))
                    return page;
            }
            return null;
        }

        [ExcludeFromCodeCoverage]
        public override void OnEnable()
        {
            foreach (var page in m_Pages.Values)
                page.OnEnable();

            m_UpmRegistryClient.onRegistriesModified += OnRegistriesModified;
        }

        [ExcludeFromCodeCoverage]
        public override void OnDisable()
        {
            foreach (var page in m_Pages.Values)
                page.OnDisable();

            m_UpmRegistryClient.onRegistriesModified -= OnRegistriesModified;
        }

        public void OnWindowDestroy()
        {
            // When the window gets closed, we want to reset some states as if the page is deactivated without actually deactivating the page
            // so that the next time user opens the package manager windows, the UI will show up in a clean state
            activePage.ResetStatesOnDeactivate();

            // Since extension pages are added on Window creation time we need to clear them on Window destroy, so the next
            // time the Package Manager window is created (which would happen when it is closed and reopened), we will not
            // report a false alarm of page name duplication
            m_OrderedExtensionPageArgs.Clear();
        }
    }
}
