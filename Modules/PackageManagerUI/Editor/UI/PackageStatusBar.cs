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
        }

        public void Setup()
        {
            UpdateStatusMessage();

            PackageDatabase.instance.onUpdateTimeChange += SetUpdateTimestamp;

            PackageDatabase.instance.onRefreshOperationStart += () => SetStatusMessage(StatusType.Loading, "Loading packages...");
            PackageDatabase.instance.onRefreshOperationFinish += UpdateStatusMessage;
            PackageDatabase.instance.onRefreshOperationError += (error) =>
            {
                m_LastErrorMessage = "Error loading packages, see console";
                UpdateStatusMessage();
            };

            statusLabel.RegisterCallback<MouseDownEvent>(e =>
            {
                // only react to left mouse button click outside of play mode
                if (e.button != 0 || EditorApplication.isPlaying)
                    return;

                PackageDatabase.instance.Refresh(RefreshOptions.SearchAll);
            });
        }

        private static string GetUpdateTimeLabel(long timestamp)
        {
            return new DateTime(timestamp).ToString("MMM d, HH:mm");
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
                EditorApplication.update -= CheckInternetReachability;
                EditorApplication.update += CheckInternetReachability;
                errorMessage = k_OfflineErrorMessage;
            }

            if (!string.IsNullOrEmpty(errorMessage))
                SetStatusMessage(StatusType.Error, errorMessage);
            else
                SetUpdateTimeLabel(GetUpdateTimeLabel(PackageDatabase.instance.lastUpdateTimestamp));
        }

        private void CheckInternetReachability()
        {
            if (!ApplicationUtil.instance.isInternetReachable)
                return;

            PackageDatabase.instance.Refresh(RefreshOptions.ListInstalled | RefreshOptions.SearchAll);

            EditorApplication.update -= CheckInternetReachability;
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
    }
}
