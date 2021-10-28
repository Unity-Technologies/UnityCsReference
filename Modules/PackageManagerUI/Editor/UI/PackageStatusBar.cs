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
        private PackageFiltering m_PackageFiltering;
        private PageManager m_PageManager;
        private AssetStoreCallQueue m_AssetStoreCallQueue;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_PackageFiltering = container.Resolve<PackageFiltering>();
            m_PageManager = container.Resolve<PageManager>();
            m_AssetStoreCallQueue = container.Resolve<AssetStoreCallQueue>();
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
            UpdateStatusMessage();

            m_PageManager.onRefreshOperationStart += UpdateStatusMessage;
            m_PageManager.onRefreshOperationFinish += UpdateStatusMessage;
            m_PageManager.onRefreshOperationError += OnRefreshOperationError;

            m_PackageFiltering.onFilterTabChanged += OnFilterTabChanged;
            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;

            m_AssetStoreCallQueue.onCheckUpdateProgress += OnCheckUpdateProgress;

            refreshButton.SetIcon("refresh");
            refreshButton.iconTooltip = L10n.Tr("Refresh list");
            refreshButton.clicked += () =>
            {
                if (!EditorApplication.isPlaying)
                {
                    m_PageManager.Refresh(m_PackageFiltering.currentFilterTab);
                }
            };
            refreshButton.SetEnabled(true);

            RefreshCheckUpdateMenuOption(m_PackageFiltering.currentFilterTab);
        }

        public void OnDisable()
        {
            m_PageManager.onRefreshOperationStart -= UpdateStatusMessage;
            m_PageManager.onRefreshOperationFinish -= UpdateStatusMessage;
            m_PageManager.onRefreshOperationError -= OnRefreshOperationError;

            m_PackageFiltering.onFilterTabChanged -= OnFilterTabChanged;
            m_Application.onInternetReachabilityChange -= OnInternetReachabilityChange;

            m_AssetStoreCallQueue.onCheckUpdateProgress -= OnCheckUpdateProgress;
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

        private void OnFilterTabChanged(PackageFilterTab tab)
        {
            RefreshCheckUpdateMenuOption(tab);
            UpdateStatusMessage();
        }

        private void OnRefreshOperationError(UIError error)
        {
            UpdateStatusMessage();
        }

        private void UpdateStatusMessage()
        {
            var tab = m_PackageFiltering.currentFilterTab;

            var contentType = m_PageManager.GetCurrentPage().contentType ?? L10n.Tr("packages");

            if (m_PageManager.IsRefreshInProgress(tab))
            {
                SetStatusMessage(StatusType.Loading, string.Format(L10n.Tr("Refreshing {0}..."), contentType));
                return;
            }

            if (tab == PackageFilterTab.AssetStore && m_AssetStoreCallQueue.isCheckUpdateInProgress)
            {
                SetStatusMessage(StatusType.Loading, L10n.Tr("Checking for updates..."));
                return;
            }

            var errorMessage = string.Empty;
            var refreshError = m_PageManager.GetRefreshError(tab);

            if (!m_Application.isInternetReachable)
                errorMessage = k_OfflineErrorMessage;
            else if (refreshError != null)
            {
                var seeDetailInConsole = (UIError.Attribute.IsDetailInConsole & refreshError.attribute) != 0;
                errorMessage = seeDetailInConsole ?
                    string.Format(L10n.Tr("Error refreshing {0}, see console"), contentType) :
                    string.Format(L10n.Tr("Error refreshing {0}"), contentType);
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
                UIUtils.SetElementDisplay(refreshButton,false);
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
            var tab = m_PackageFiltering.currentFilterTab;
            var timestamp = m_PageManager.GetRefreshTimestamp(tab);
            var dt = new DateTime(timestamp);
            var dateAndTime = dt.ToString("MMM d, HH:mm", CultureInfo.CreateSpecificCulture("en-US"));
            var label = timestamp == 0L ? string.Empty : string.Format(L10n.Tr("Last update {0}"), dateAndTime);
            SetStatusMessage(StatusType.Normal, label);
        }

        internal void OnCheckUpdateProgress()
        {
            if (m_PackageFiltering.currentFilterTab != PackageFilterTab.AssetStore)
                return;

            UpdateStatusMessage();
        }

        private void RefreshCheckUpdateMenuOption(PackageFilterTab tab)
        {
            if (tab == PackageFilterTab.AssetStore)
            {
                var menu = new DropdownMenu();
                menu.AppendAction(L10n.Tr("Check for updates"), a =>
                {
                    m_AssetStoreCallQueue.ForceCheckUpdateForAllLocalInfos();
                },action => m_AssetStoreCallQueue.isCheckUpdateInProgress ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
                refreshButton.menu = menu;
            }
            else
                refreshButton.menu = null;
        }

        private VisualElementCache cache { get; set; }

        private LoadingSpinner loadingSpinner { get { return cache.Get<LoadingSpinner>("loadingSpinner"); }}
        private Label errorIcon { get { return cache.Get<Label>("errorIcon"); }}
        private Label statusLabel { get { return cache.Get<Label>("statusLabel"); }}
        private VisualElement refreshButtonContainer { get { return cache.Get<VisualElement>("refreshButtonContainer"); } }
        private DropdownButton refreshButton { get { return cache.Get<DropdownButton>("refreshButton"); } }
    }
}
