// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageStatusBar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageStatusBar> {}

        private static readonly string k_OfflineErrorMessage = "You seem to be offline";

        private string[] m_LastErrorMessages;

        private enum StatusType { Normal, Loading, Error };

        public PackageStatusBar()
        {
            var root = Resources.GetTemplate("PackageStatusBar.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            m_LastErrorMessages = new string[Enum.GetNames(typeof(PackageFilterTab)).Length];


            var refreshIconButton = new IconButton(Resources.GetIconPath("refresh"));
            refreshIconButton.clickable.clicked += () =>
            {
                if (!EditorApplication.isPlaying)
                    PageManager.instance.Refresh(RefreshOptions.CurrentFilter);
            };
            refreshButtonContainer.Add(refreshIconButton);
        }

        public void OnEnable()
        {
            UpdateStatusMessage(PackageFiltering.instance.currentFilterTab);

            PackageDatabase.instance.onUpdateTimeChange += SetUpdateTimestamp;

            PackageDatabase.instance.onRefreshOperationStart += () =>
            {
                if (PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore)
                    SetStatusMessage(StatusType.Loading, L10n.Tr("Fetching packages..."));
                else
                    SetStatusMessage(StatusType.Loading, L10n.Tr("Loading packages..."));
            };
            PackageDatabase.instance.onRefreshOperationFinish += UpdateStatusMessage;
            PackageDatabase.instance.onRefreshOperationError += error =>
            {
                var errorMessage = string.Empty;
                if (error != null)
                {
                    errorMessage = PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore ? L10n.Tr("Error fetching packages, see console") : L10n.Tr("Error loading packages, see console");
                }

                m_LastErrorMessages[(int)PackageFiltering.instance.currentFilterTab] = errorMessage;
                UpdateStatusMessage(PackageFiltering.instance.currentFilterTab);
            };

            PackageFiltering.instance.onFilterTabChanged += UpdateStatusMessage;

            ApplicationUtil.instance.onInternetReachabilityChange += OnInternetReachabilityChange;
        }

        public void OnDisable()
        {
            PackageDatabase.instance.onUpdateTimeChange -= SetUpdateTimestamp;
            PackageDatabase.instance.onRefreshOperationFinish -= UpdateStatusMessage;
            PackageFiltering.instance.onFilterTabChanged -= UpdateStatusMessage;
            ApplicationUtil.instance.onInternetReachabilityChange -= OnInternetReachabilityChange;
        }

        private void OnInternetReachabilityChange(bool value)
        {
            UpdateStatusMessage(PackageFiltering.instance.currentFilterTab);
            if (value && !EditorApplication.isPlaying)
                PageManager.instance.Refresh(PackageFiltering.instance.currentFilterTab);
        }

        private static string GetUpdateTimeLabel(long timestamp)
        {
            return $"{new DateTime(timestamp):MMM d, HH:mm}";
        }

        private void SetUpdateTimestamp(long lastUpdateTimestamp)
        {
            // Only refresh update time after a search operation successfully returns while online
            if (ApplicationUtil.instance.isInternetReachable && lastUpdateTimestamp != 0)
                SetUpdateTimeLabel(GetUpdateTimeLabel(lastUpdateTimestamp));
        }

        private void SetUpdateTimeLabel(string lastUpdateTime)
        {
            if (!string.IsNullOrEmpty(lastUpdateTime))
                SetStatusMessage(StatusType.Normal, "Last update " + lastUpdateTime);
            else
                SetStatusMessage(StatusType.Normal, string.Empty);
        }

        private void UpdateStatusMessage(PackageFilterTab tab)
        {
            var errorMessage = m_LastErrorMessages[(int)tab];
            if (!ApplicationUtil.instance.isInternetReachable)
                errorMessage = L10n.Tr(k_OfflineErrorMessage);

            if (!string.IsNullOrEmpty(errorMessage))
                SetStatusMessage(StatusType.Error, errorMessage);
            else
                SetUpdateTimeLabel(PackageDatabase.instance.lastUpdateTimestamp != 0 ? GetUpdateTimeLabel(PackageDatabase.instance.lastUpdateTimestamp) : string.Empty);
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
        private VisualElement refreshButtonContainer { get { return cache.Get<VisualElement>("refreshButtonContainer"); } }
    }
}
