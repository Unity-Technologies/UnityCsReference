// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
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
        }

        public void OnEnable()
        {
            UpdateStatusMessage(PackageFiltering.instance.currentFilterTab);

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

            refreshButton.clickable.clicked += () =>
            {
                if (!EditorApplication.isPlaying)
                    PageManager.instance.Refresh(RefreshOptions.CurrentFilter);
            };
        }

        public void OnDisable()
        {
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
            var dt = new DateTime(timestamp);
            return $"Last update {dt.ToString("MMM d, HH:mm", CultureInfo.CreateSpecificCulture("en-US"))}";
        }

        private void SetUpdateTimeLabel(string lastUpdateTime)
        {
            if (!string.IsNullOrEmpty(lastUpdateTime))
                SetStatusMessage(StatusType.Normal, lastUpdateTime);
            else
                SetStatusMessage(StatusType.Normal, string.Empty);
        }

        private void UpdateStatusMessage(PackageFilterTab tab)
        {
            var errorMessage = m_LastErrorMessages[(int)tab];
            if (!ApplicationUtil.instance.isInternetReachable)
                errorMessage = L10n.Tr(k_OfflineErrorMessage);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                SetStatusMessage(StatusType.Error, errorMessage);
                return;
            }

            var timestamp = PackageDatabase.instance.GetRefreshTimestamp(tab);
            SetUpdateTimeLabel(timestamp != 0 ? GetUpdateTimeLabel(timestamp) : string.Empty);
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
        private Button refreshButton { get { return cache.Get<Button>("refreshButton"); } }
    }
}
