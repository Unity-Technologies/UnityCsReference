// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal sealed class PackageLoadBar : VisualElement
    {
        public const int k_FixedHeight = 30;

        public enum AssetsToLoad
        {
            Min = 25,
            Max = 50,
            All = -1
        }

        private long m_Total;
        private long m_NumberOfPackagesShown;
        private static readonly string k_All = L10n.Tr("All");

        private long m_LoadMore;
        private string m_LoadedText;
        private bool m_ShowLoadMoreButton;
        private bool m_LoadMoreInProgress;
        private bool m_LoadAllDiff;

        private readonly IApplicationProxy m_Application;
        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IPageManager m_PageManager;
        private readonly IProjectSettingsProxy m_SettingsProxy;
        private readonly IPageRefreshHandler m_PageRefreshHandler;

        public PackageLoadBar(
            IResourceLoader resourceLoader,
            IApplicationProxy application,
            IUnityConnectProxy unityConnect,
            IPageManager pageManager,
            IProjectSettingsProxy settingsProxy,
            IPageRefreshHandler pageRefreshHandler)
        {
            m_Application = application;
            m_UnityConnect = unityConnect;
            m_PageManager = pageManager;
            m_SettingsProxy = settingsProxy;
            m_PageRefreshHandler = pageRefreshHandler;

            var root = resourceLoader.GetTemplate("PackageLoadBar.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            var dropdownButton = new DropdownButton();
            dropdownButton.name = "loadAssetsDropdown";
            loadAssetsDropdownContainer.Add(dropdownButton);

            loadMoreButton.clickable.clicked += LoadItemsClicked;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;
            m_PageRefreshHandler.onRefreshOperationFinish += Refresh;

            m_PageManager.onListRebuild += OnListRebuild;
            m_PageManager.onListUpdate += OnListUpdate;

            Refresh();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
            m_Application.onInternetReachabilityChange -= OnInternetReachabilityChange;
            m_PageRefreshHandler.onRefreshOperationFinish -= Refresh;

            m_PageManager.onListRebuild -= OnListRebuild;
            m_PageManager.onListUpdate -= OnListUpdate;
        }

        public void UpdateMenu()
        {
            var menu = new DropdownMenu();

            EditorApplication.delayCall -= UpdateMenu;
            if (panel != null && m_Total == 0)
                EditorApplication.delayCall += UpdateMenu;

            AddDropdownItems(menu);
            loadAssetsDropdown.menu = menu.MenuItems().Count > 0 ? menu : null;
        }

        public void AddDropdownItems(DropdownMenu menu)
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

        public void AddDropdownItem(DropdownMenu menu, int value)
        {
            var textValue = value == (int)AssetsToLoad.All ? k_All : value.ToString();
            textValue = L10n.Tr(textValue);
            menu.AppendAction(textValue, a =>
            {
                loadAssetsDropdown.text = textValue;
                m_SettingsProxy.loadAssets = value;
                m_SettingsProxy.Save();
                UpdateLoadBarMessage();
                LoadItemsClicked();
                UpdateMenu();

                PackageManagerWindowAnalytics.SendEvent($"load {value}");
            }, a => m_SettingsProxy.loadAssets == value ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            UpdateLoadBarMessage();
        }

        private void OnInternetReachabilityChange(bool value)
        {
            loadMoreButton.SetEnabled(value && !m_LoadMoreInProgress);
        }

        private void OnListUpdate(ListUpdateArgs args)
        {
            // Since load bar only shows the number of items on a page, we don't refresh when the number hasn't changed
            if (args.added.Count > 0 || args.removed.Count > 0)
                Refresh();
        }

        private void OnListRebuild(IPage page)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (m_PageManager.activePage?.id != MyAssetsPage.k_Id)
                return;

            var visualStates = m_PageManager.activePage.visualStates;
            Set(visualStates?.countTotal ?? 0, visualStates?.countLoaded?? 0);
            UpdateMenu();
            OnInternetReachabilityChange(m_Application.isInternetReachable);
        }

        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal void Set(long total, long current)
        {
            Reset();
            m_Total = total;
            m_NumberOfPackagesShown = current;

            loadMoreButton.SetEnabled(true);
            m_LoadMoreInProgress = false;
            UpdateLoadBarMessage();
        }

        private void Reset()
        {
            m_ShowLoadMoreButton = true;
        }

        public void LoadItemsClicked()
        {
            loadMoreButton.SetEnabled(false);
            m_LoadMoreInProgress = true;
            m_PageManager.activePage.LoadMore(m_LoadMore);
            UpdateMenu();
        }

        private void UpdateLoadBarMessage()
        {
            if (!m_UnityConnect.isUserLoggedIn || m_Total == 0 || m_NumberOfPackagesShown == 0)
            {
                UIUtils.SetElementDisplay(loadBarContainer, false);
                return;
            }

            if (m_Total <= m_NumberOfPackagesShown)
            {
                m_ShowLoadMoreButton = false;
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
            var loadAll = m_SettingsProxy.loadAssets == (int)AssetsToLoad.All;
            loadAssetsDropdown.text = loadAll || m_LoadAllDiff ? k_All : m_LoadMore.ToString();

            loadedLabel.text = m_LoadedText;

            UIUtils.SetElementDisplay(loadAssetsDropdown, m_ShowLoadMoreButton);
            UIUtils.SetElementDisplay(loadMoreButton, m_ShowLoadMoreButton);

            UIUtils.SetElementDisplay(loadBarContainer, true);
        }

        private VisualElementCache cache { get; }

        private Label loadedLabel => cache.Get<Label>("loadedLabel");
        private Button loadMoreButton =>  cache.Get<Button>("loadMoreButton");
        private VisualElement loadBarContainer =>  cache.Get<VisualElement>("loadBarContainer");
        private VisualElement loadAssetsDropdownContainer => cache.Get<VisualElement>("loadAssetsDropdownContainer");
        private DropdownButton loadAssetsDropdown => cache.Get<DropdownButton>("loadAssetsDropdown");
    }
}
