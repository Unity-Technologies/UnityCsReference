// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageLoadBar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageLoadBar> {}

        public enum AssetsToLoad
        {
            Min = 25,
            Max = 50,
            All = -1
        }

        private long m_Total;
        private long m_NumberOfPackagesShown;
        private const string k_All = "All";

        private long m_LoadMore;
        private string m_LoadedText;
        private bool m_DoShowLoadMoreLabel;
        private bool m_LoadMoreInProgress;
        private bool m_LoadAllDiff;

        private ResourceLoader m_ResourceLoader;
        private ApplicationProxy m_Application;
        private UnityConnectProxy m_UnityConnect;
        private PageManager m_PageManager;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_UnityConnect = container.Resolve<UnityConnectProxy>();
            m_PageManager = container.Resolve<PageManager>();
            m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
        }

        public PackageLoadBar()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageLoadBar.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            loadMoreLabel.OnLeftClick(LoadItemsClicked);
        }

        public void OnEnable()
        {
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;
            m_PageManager.onRefreshOperationFinish += Refresh;

            loadAssetsDropdown.clickable.clicked += loadAssetsDropdown.OnDropdownButtonClicked;

            Refresh();
            UpdateMenu();

            loadMoreLabel.SetEnabled(m_Application.isInternetReachable);
        }

        public void UpdateMenu()
        {
            var menu = new GenericMenu();

            if (m_Total == 0)
                EditorApplication.delayCall += () => UpdateMenu();

            AddDropdownItems(menu);
            loadAssetsDropdown.DropdownMenu = menu.GetItemCount() > 0 ? menu : null;
        }

        public void AddDropdownItems(GenericMenu menu)
        {
            m_LoadAllDiff = m_Total - m_NumberOfPackagesShown <= (int)AssetsToLoad.Min;
            var minDiff = m_LoadAllDiff;
            if (!minDiff)
                AddDropdownItem(menu, (int)AssetsToLoad.Min);

            m_LoadAllDiff = m_Total - m_NumberOfPackagesShown <= (int)AssetsToLoad.Max;
            var maxDiff = m_LoadAllDiff;
            if (!maxDiff)
                AddDropdownItem(menu, (int)AssetsToLoad.Max);

            var showDropDownArea = !minDiff || !maxDiff;
            if (showDropDownArea)
                AddDropdownItem(menu, (int)AssetsToLoad.All);
        }

        public void AddDropdownItem(GenericMenu menu, int value)
        {
            var textValue = value == (int)AssetsToLoad.All ? k_All : value.ToString();
            menu.AddItem(new GUIContent(L10n.Tr(textValue)), m_SettingsProxy.loadAssets == value ? true : false, () =>
            {
                loadAssetsDropdown.text = L10n.Tr(textValue);
                m_SettingsProxy.loadAssets = value;
                m_SettingsProxy.Save();
                UpdateLoadBarMessage();
                LoadItemsClicked();
                UpdateMenu();

                PackageManagerWindowAnalytics.SendEvent($"load {value}");
            });
        }

        public void OnDisable()
        {
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
            m_Application.onInternetReachabilityChange -= OnInternetReachabilityChange;

            m_PageManager.onRefreshOperationFinish -= Refresh;
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            UpdateLoadBarMessage();
        }

        private void OnInternetReachabilityChange(bool value)
        {
            loadMoreLabel.SetEnabled(value && !m_LoadMoreInProgress);
        }

        public void Refresh()
        {
            var page = m_PageManager.GetCurrentPage();
            Set(page?.numTotalItems ?? 0, page?.numCurrentItems ?? 0);
            UpdateMenu();
        }

        internal void Set(long total, long current)
        {
            Reset();
            m_Total = total;
            m_NumberOfPackagesShown = current;

            loadMoreLabel.SetEnabled(true);
            m_LoadMoreInProgress = false;
            UpdateLoadBarMessage();
        }

        internal void Reset()
        {
            m_DoShowLoadMoreLabel = true;
        }

        public void LoadItemsClicked()
        {
            loadMoreLabel.SetEnabled(false);
            m_LoadMoreInProgress = true;
            m_PageManager.LoadMore(m_LoadMore);
            UpdateMenu();
        }

        private void UpdateLoadBarMessage()
        {
            if (!m_UnityConnect.isUserLoggedIn || m_Total == 0 || m_NumberOfPackagesShown == 0)
            {
                UIUtils.SetElementDisplay(loadBarContainer, false);
                return;
            }

            if (m_Total == m_NumberOfPackagesShown)
            {
                m_DoShowLoadMoreLabel = false;
                m_LoadedText = m_Total == 1 ? L10n.Tr("One package shown") : string.Format(L10n.Tr("All {0} packages shown"), m_NumberOfPackagesShown);
            }
            else
            {
                var diff = m_Total - m_NumberOfPackagesShown;
                var max = m_SettingsProxy.loadAssets == (long)AssetsToLoad.All ? m_Total : m_SettingsProxy.loadAssets;

                if (diff >= max)
                {
                    m_LoadAllDiff = false;
                    m_LoadMore = max;
                }
                else
                {
                    m_LoadAllDiff = true;
                    m_LoadMore = diff;
                }

                m_LoadedText = string.Format(L10n.Tr("{0} of {1}"), m_NumberOfPackagesShown, m_Total);
            }
            SetLabels();
        }

        private void SetLabels()
        {
            var loadAll = m_SettingsProxy.loadAssets == (int)AssetsToLoad.All ? true : false;
            loadAssetsDropdown.text = loadAll || m_LoadAllDiff ? L10n.Tr(k_All) : m_LoadMore.ToString();

            loadedLabel.text = m_LoadedText;

            UIUtils.SetElementDisplay(loadAssetsDropdown, m_DoShowLoadMoreLabel);
            UIUtils.SetElementDisplay(loadMoreLabel, m_DoShowLoadMoreLabel);

            UIUtils.SetElementDisplay(loadBarContainer, true);
        }

        private VisualElementCache cache { get; set; }

        private Label loadedLabel { get { return cache.Get<Label>("loadedLabel"); } }
        private Label loadMoreLabel { get { return cache.Get<Label>("loadMoreLabel"); } }
        private VisualElement loadBarContainer { get { return cache.Get<VisualElement>("loadBarContainer"); } }
        private DropdownButton loadAssetsDropdown { get { return cache.Get<DropdownButton>("loadAssetsDropdown"); } }
    }
}
