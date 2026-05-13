// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    [UxmlElement]
    internal partial class PackageStatusBar : VisualElement
    {
        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal static readonly string k_OfflineErrorMessage = L10n.Tr("You seem to be offline");

        private enum StatusType { Normal, Loading, Error }

        private readonly IApplicationProxy m_Application;
        private readonly IBackgroundFetchHandler m_BackgroundFetchHandler;
        private readonly IPageRefreshHandler m_PageRefreshHandler;
        private readonly IPageManager m_PageManager;
        private readonly IUnityConnectProxy m_UnityConnect;

        public PackageStatusBar() : this(
            ServicesContainer.instance.Resolve<IResourceLoader>(),
            ServicesContainer.instance.Resolve<IApplicationProxy>(),
            ServicesContainer.instance.Resolve<IBackgroundFetchHandler>(),
            ServicesContainer.instance.Resolve<IPageRefreshHandler>(),
            ServicesContainer.instance.Resolve<IPageManager>(),
            ServicesContainer.instance.Resolve<IUnityConnectProxy>())
        {
        }

        public PackageStatusBar(
            IResourceLoader resourceLoader,
            IApplicationProxy application,
            IBackgroundFetchHandler backgroundFetchHandler,
            IPageRefreshHandler pageRefreshHandler,
            IPageManager pageManager,
            IUnityConnectProxy unityConnect)
        {
            m_Application = application;
            m_BackgroundFetchHandler = backgroundFetchHandler;
            m_PageRefreshHandler = pageRefreshHandler;
            m_PageManager = pageManager;
            m_UnityConnect = unityConnect;

            var root = resourceLoader.GetTemplate("PackageStatusBar.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            var dropdownButton = new DropdownButton();
            dropdownButton.name = "refreshButton";
            dropdownButton.SetIcon(Icon.Refresh);
            dropdownButton.mainButton.tooltip = L10n.Tr("Refresh list");
            dropdownButton.clicked += () =>
            {
                m_PageRefreshHandler.Refresh(m_PageManager.activePage);
                PackageManagerWindowAnalytics.SendEvent("refreshList");
            };
            refreshButtonContainer.Add(dropdownButton);

            statusLabel.ShowTextTooltipOnSizeChange();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_PageRefreshHandler.onRefreshOperationStart += UpdateStatusMessage;
            m_PageRefreshHandler.onRefreshOperationFinish += UpdateStatusMessage;
            m_PageRefreshHandler.onRefreshOperationError += OnRefreshOperationError;

            m_PageManager.onActivePageChanged += OnActivePageChanged;
            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            m_BackgroundFetchHandler.onCheckUpdateProgress += OnCheckUpdateProgress;

            Refresh(m_PageManager.activePage, m_UnityConnect.isUserLoggedIn);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
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
                var seeDetailInConsole = (UIError.Attribute.DetailInConsole & refreshError.attribute) != 0;
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
            var label = timestamp == 0 ? string.Empty : string.Format(L10n.Tr("Last refresh {0}"), dateAndTime);
            SetStatusMessage(StatusType.Normal, label);
        }

        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
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
                    _ =>
                    {
                        m_BackgroundFetchHandler.ForceCheckUpdateAllCachedAndImportedPackages();
                        PackageManagerWindowAnalytics.SendEvent("checkForUpdates");
                    },
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
