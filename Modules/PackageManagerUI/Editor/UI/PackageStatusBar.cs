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

        private const string k_OfflineErrorMessage = "You seem to be offline";

        private string m_LastErrorMessage;

        private enum StatusType { Normal, Loading, Error };

        public PackageStatusBar()
        {
            var root = Resources.GetTemplate("PackageStatusBar.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            m_LastErrorMessage = string.Empty;

            var refreshIconButton = new IconButton(Resources.GetIconPath("refresh"));
            refreshIconButton.clickable.clicked += () =>
            {
                if (!EditorApplication.isPlaying)
                    PageManager.instance.Refresh(RefreshOptions.CurrentFilter);
            };
            refreshButtonContainer.Add(refreshIconButton);
        }

        public void Setup()
        {
            UpdateStatusMessage();

            PackageDatabase.instance.onUpdateTimeChange += SetUpdateTimestamp;

            PackageDatabase.instance.onRefreshOperationStart += () =>
            {
                m_LastErrorMessage = string.Empty;
                SetStatusMessage(StatusType.Loading, "Loading packages...");
            };
            PackageDatabase.instance.onRefreshOperationFinish += UpdateStatusMessage;
            PackageDatabase.instance.onRefreshOperationError += error =>
            {
                m_LastErrorMessage = error == null ? string.Empty : L10n.Tr("Error loading packages, see console");
                UpdateStatusMessage();
            };

            ApplicationUtil.instance.onInternetReachabilityChange += OnInternetReachabilityChange;
        }

        private void OnInternetReachabilityChange(bool value)
        {
            UpdateStatusMessage();
            if (value)
            {
                PageManager.instance.Refresh(RefreshOptions.AllOnline);
            }
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

        private void UpdateStatusMessage()
        {
            var errorMessage = m_LastErrorMessage;
            if (!ApplicationUtil.instance.isInternetReachable)
            {
                errorMessage = k_OfflineErrorMessage;
            }

            if (!string.IsNullOrEmpty(errorMessage))
                SetStatusMessage(StatusType.Error, errorMessage);
            else
            {
                SetUpdateTimeLabel(PackageDatabase.instance.lastUpdateTimestamp != 0 ? GetUpdateTimeLabel(PackageDatabase.instance.lastUpdateTimestamp) : string.Empty);
            }
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
