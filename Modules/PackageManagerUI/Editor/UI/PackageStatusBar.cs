// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageStatusBar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageStatusBar> {}

        internal static readonly string k_OfflineErrorMessage = "You seem to be offline";

        private enum StatusType { Normal, Loading, Error }

        private ResourceLoader m_ResourceLoader;
        private ApplicationProxy m_Application;
        private PackageFiltering m_PackageFiltering;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PageManager m_PageManager;
        private UpmClient m_UpmClient;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_PackageFiltering = container.Resolve<PackageFiltering>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_PageManager = container.Resolve<PageManager>();
            m_UpmClient = container.Resolve<UpmClient>();
        }

        public PackageStatusBar()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageStatusBar.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            statusLabel.ShowTextTooltipOnSizeChange();
        }

        public void OnEnable()
        {
            UpdateStatusMessage();

            m_PageManager.onRefreshOperationStart += UpdateStatusMessage;
            m_PageManager.onRefreshOperationFinish += OnRefreshOperationFinish;
            m_PageManager.onRefreshOperationError += OnRefreshOperationError;

            m_PackageFiltering.onFilterTabChanged += OnFilterTabChanged;
            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(L10n.Tr("Refresh list")), false, () =>
            {
                if (!EditorApplication.isPlaying)
                {
                    refreshButton.SetEnabled(false);
                    m_PageManager.Refresh(m_PackageFiltering.currentFilterTab, m_PackageManagerPrefs.numItemsPerPage ?? PageManager.k_DefaultPageSize);
                }
            });
            menu.AddItem(new GUIContent(L10n.Tr("Manual resolve")), false, () =>
            {
                if (!EditorApplication.isPlaying)
                {
                    refreshButton.SetEnabled(false);
                    m_UpmClient.Resolve();
                    refreshButton.SetEnabled(true);
                }
            });
            refreshButton.DropdownMenu = menu;
            refreshButton.Status = DropdownStatus.Refresh;
            refreshButton.clickable.clicked += refreshButton.OnDropdownButtonClicked;
            refreshButton.SetEnabled(true);
        }

        public void OnDisable()
        {
            m_PageManager.onRefreshOperationStart -= UpdateStatusMessage;
            m_PageManager.onRefreshOperationFinish -= OnRefreshOperationFinish;
            m_PageManager.onRefreshOperationError -= OnRefreshOperationError;

            m_PackageFiltering.onFilterTabChanged -= OnFilterTabChanged;
            m_Application.onInternetReachabilityChange -= OnInternetReachabilityChange;
        }

        private void OnInternetReachabilityChange(bool value)
        {
            refreshButton.SetEnabled(value);
            UpdateStatusMessage();
        }

        private void OnFilterTabChanged(PackageFilterTab tab)
        {
            UpdateStatusMessage();
        }

        private void OnRefreshOperationError(UIError error)
        {
            UpdateStatusMessage();
        }

        private void OnRefreshOperationFinish()
        {
            refreshButton.SetEnabled(true);
            UpdateStatusMessage();
        }

        private void UpdateStatusMessage()
        {
            var tab = m_PackageFiltering.currentFilterTab;

            if (m_PageManager.IsRefreshInProgress(tab))
            {
                SetStatusMessage(StatusType.Loading, L10n.Tr("Refreshing packages..."));
                return;
            }

            var errorMessage = string.Empty;
            var refreshError = m_PageManager.GetRefreshError(tab);

            if (!m_Application.isInternetReachable)
                errorMessage = L10n.Tr(k_OfflineErrorMessage);
            else if (refreshError != null)
            {
                var seeConsoleNotif = (UIError.Attribute.IsDetailInConsole & refreshError.attribute) != 0 ? ", see console" : "";
                errorMessage = L10n.Tr($"Error refreshing packages{seeConsoleNotif}");
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                SetStatusMessage(StatusType.Error, errorMessage);
                return;
            }

            var timestamp = m_PageManager.GetRefreshTimestamp(tab);
            var dt = new DateTime(timestamp);
            var label = timestamp == 0L ? string.Empty : L10n.Tr($"Last update {dt.ToString("MMM d, HH:mm", CultureInfo.CreateSpecificCulture("en-US"))}");
            SetStatusMessage(StatusType.Normal, label);
        }

        private void SetStatusMessage(StatusType status, string message)
        {
            if (status == StatusType.Loading)
                loadingSpinner.Start();
            else
                loadingSpinner.Stop();

            UIUtils.SetElementDisplay(errorIcon, status == StatusType.Error);
            statusLabel.text = message;
        }

        private VisualElementCache cache { get; set; }

        private LoadingSpinner loadingSpinner { get { return cache.Get<LoadingSpinner>("loadingSpinner"); }}
        private Label errorIcon { get { return cache.Get<Label>("errorIcon"); }}
        private Label statusLabel { get { return cache.Get<Label>("statusLabel"); }}
        private DropdownButton refreshButton { get { return cache.Get<DropdownButton>("refreshButton"); } }
    }
}
