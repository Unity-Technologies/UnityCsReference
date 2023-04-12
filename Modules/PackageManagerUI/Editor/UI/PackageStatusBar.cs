// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageStatusBar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageStatusBar> {}

        internal static readonly string k_OfflineErrorMessage = L10n.Tr("You seem to be offline");

        private enum StatusType { Normal, Loading, Error }

        private ResourceLoader m_ResourceLoader;
        private ApplicationProxy m_Application;
        private BackgroundFetchHandler m_BackgroundFetchHandler;
        private PageRefreshHandler m_PageRefreshHandler;
        private PageManager m_PageManager;
        private UnityConnectProxy m_UnityConnect;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_BackgroundFetchHandler = container.Resolve<BackgroundFetchHandler>();
            m_PageRefreshHandler = container.Resolve<PageRefreshHandler>();
            m_PageManager = container.Resolve<PageManager>();
            m_UnityConnect = container.Resolve<UnityConnectProxy>();
        }

        public PackageStatusBar()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageStatusBar.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            var dropDownButton = new DropdownButton();
            dropDownButton.name = "refreshButton";
            refreshButtonContainer.Add(dropDownButton);

            statusLabel.ShowTextTooltipOnSizeChange();
        }

        public void OnEnable()
        {
            m_PageRefreshHandler.onRefreshOperationStart += UpdateStatusMessage;
            m_PageRefreshHandler.onRefreshOperationFinish += UpdateStatusMessage;
            m_PageRefreshHandler.onRefreshOperationError += OnRefreshOperationError;

            m_PageManager.onActivePageChanged += OnActivePageChanged;
            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            m_BackgroundFetchHandler.onCheckUpdateProgress += OnCheckUpdateProgress;

            refreshButton.SetIcon("refresh");
            refreshButton.iconTooltip = L10n.Tr("Refresh list");
            refreshButton.clicked += () =>
            {
                m_PageRefreshHandler.Refresh(m_PageManager.activePage);
            };
            refreshButton.SetEnabled(true);

            Refresh(m_PageManager.activePage, m_UnityConnect.isUserLoggedIn);
        }

        public void OnDisable()
        {
            m_PageRefreshHandler.onRefreshOperationStart -= UpdateStatusMessage;
            m_PageRefreshHandler.onRefreshOperationFinish -= UpdateStatusMessage;
            m_PageRefreshHandler.onRefreshOperationError -= OnRefreshOperationError;

            m_PageManager.onActivePageChanged -= OnActivePageChanged;
            m_Application.onInternetReachabilityChange -= OnInternetReachabilityChange;
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;

            m_BackgroundFetchHandler.onCheckUpdateProgress -= OnCheckUpdateProgress;
        }

        public void DisableRefresh()
        {
            refreshButton.SetEnabled(false);
        }

        private void OnInternetReachabilityChange(bool value)
        {
            refreshButton.SetEnabled(value);
            UpdateStatusMessage();
        }

        private void OnActivePageChanged(IPage page)
        {
            Refresh(page, m_UnityConnect.isUserLoggedIn);
        }

        private void OnUserLoginStateChange(bool isUserInfoReady, bool isUserLoggedIn)
        {
            Refresh(m_PageManager.activePage, isUserLoggedIn);
        }

        private void OnRefreshOperationError(UIError error)
        {
            UpdateStatusMessage();
        }

        private bool IsVisible(IPage page, bool isUserLoggedIn)
        {
            return page.id != MyAssetsPage.k_Id || isUserLoggedIn;
        }

        private void Refresh(IPage page, bool isUserLoggedIn)
        {
            var visibility = IsVisible(page, isUserLoggedIn);
            UIUtils.SetElementDisplay(this, visibility);

            if (!visibility)
                return;

            RefreshCheckUpdateMenuOption(page);
            UpdateStatusMessage();
        }

        private void UpdateStatusMessage()
        {
            var page = m_PageManager.activePage;
            if (m_PageRefreshHandler.IsRefreshInProgress(page))
            {
                SetStatusMessage(StatusType.Loading, L10n.Tr("Refreshing list..."));
                return;
            }

            if (page.id == MyAssetsPage.k_Id && m_BackgroundFetchHandler.isCheckUpdateInProgress)
            {
                SetStatusMessage(StatusType.Loading, L10n.Tr("Checking for updates..."));
                return;
            }

            var errorMessage = string.Empty;
            var refreshError = m_PageRefreshHandler.GetRefreshError(page);

            if (!m_Application.isInternetReachable)
                errorMessage = k_OfflineErrorMessage;
            else if (refreshError != null)
            {
                var seeDetailInConsole = (UIError.Attribute.IsDetailInConsole & refreshError.attribute) != 0;
                errorMessage = seeDetailInConsole
                    ? L10n.Tr("Refresh error, see Console window")
                    : L10n.Tr("Refresh error");
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                SetStatusMessage(StatusType.Error, errorMessage);
                return;
            }

            SetLastUpdateStatusMessage();
        }

        private void SetStatusMessage(StatusType status, string message)
        {
            if (status == StatusType.Loading)
            {
                loadingSpinner.Start();
                UIUtils.SetElementDisplay(refreshButton, false);
            }
            else
            {
                loadingSpinner.Stop();
                UIUtils.SetElementDisplay(refreshButton, true);
            }

            UIUtils.SetElementDisplay(errorIcon, status == StatusType.Error);
            statusLabel.text = message;
        }

        private void SetLastUpdateStatusMessage()
        {
            var page = m_PageManager.activePage;
            var timestamp = m_PageRefreshHandler.GetRefreshTimestamp(page);
            var dt = new DateTime(timestamp);
            var dateAndTime = dt.ToString("MMM d, HH:mm", CultureInfo.CreateSpecificCulture("en-US"));
            var label = timestamp == 0 ? string.Empty : string.Format(L10n.Tr("Last update {0}"), dateAndTime);
            SetStatusMessage(StatusType.Normal, label);
        }

        internal void OnCheckUpdateProgress()
        {
            if (m_PageManager.activePage.id != MyAssetsPage.k_Id)
                return;

            UpdateStatusMessage();
        }

        private void RefreshCheckUpdateMenuOption(IPage page)
        {
            if (page.id == MyAssetsPage.k_Id)
            {
                var menu = new DropdownMenu();
                menu.AppendAction(L10n.Tr("Check for updates"),
                    _ => m_BackgroundFetchHandler.ForceCheckUpdateForAllLocalInfos(),
                    _ => m_BackgroundFetchHandler.isCheckUpdateInProgress ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
                refreshButton.menu = menu;
            }
            else
                refreshButton.menu = null;
        }

        private VisualElementCache cache { get; set; }

        private LoadingSpinner loadingSpinner => cache.Get<LoadingSpinner>("loadingSpinner");
        private Label errorIcon => cache.Get<Label>("errorIcon");
        private Label statusLabel => cache.Get<Label>("statusLabel");
        private VisualElement refreshButtonContainer => cache.Get<VisualElement>("refreshButtonContainer");
        private DropdownButton refreshButton => cache.Get<DropdownButton>("refreshButton");
    }
}
