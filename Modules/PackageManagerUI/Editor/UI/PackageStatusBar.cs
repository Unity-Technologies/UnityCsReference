// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageStatusBar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageStatusBar> {}

        private readonly VisualElement root;
        private string LastErrorMessage;
        private string LastUpdateTime;

        private List<IBaseOperation> operationsInProgress;

        private enum StatusType { Normal, Loading, Error };

        public event Action OnCheckInternetReachability = delegate {};

        public PackageStatusBar()
        {
            root = Resources.GetTemplate("PackageStatusBar.uxml");
            Add(root);
            Cache = new VisualElementCache(root);

            LastErrorMessage = string.Empty;
            operationsInProgress = new List<IBaseOperation>();
        }

        public void Setup(PackageCollection collection)
        {
            LastUpdateTime = collection.lastUpdateTime;
            UpdateStatusMessage();

            StatusLabel.RegisterCallback<MouseDownEvent>(e =>
            {
                if (!EditorApplication.isPlaying)
                {
                    collection.FetchListOfflineCache(true);
                    collection.FetchListCache(true);
                    collection.FetchSearchCache(true);
                }
            });
        }

        public void SetUpdateTimeMessage(string lastUpdateTime)
        {
            LastUpdateTime = lastUpdateTime;
            if (!string.IsNullOrEmpty(LastUpdateTime))
                SetStatusMessage(StatusType.Normal, "Last update " + LastUpdateTime);
            else
                SetStatusMessage(StatusType.Normal, string.Empty);
        }

        internal void OnListOrSearchOperation(IBaseOperation operation)
        {
            if (operation == null || operation.IsCompleted)
                return;
            operationsInProgress.Add(operation);
            operation.OnOperationFinalized += () => { OnOperationFinalized(operation); };
            operation.OnOperationError += OnOperationError;

            SetStatusMessage(StatusType.Loading, "Loading packages...");
        }

        private void OnOperationFinalized(IBaseOperation operation)
        {
            operationsInProgress.Remove(operation);
            if (operationsInProgress.Any()) return;
            UpdateStatusMessage();
        }

        private void UpdateStatusMessage()
        {
            var errorMessage = LastErrorMessage;
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                EditorApplication.update -= CheckInternetReachability;
                EditorApplication.update += CheckInternetReachability;
                errorMessage = "You seem to be offline";
            }

            if (!string.IsNullOrEmpty(errorMessage))
                SetStatusMessage(StatusType.Error, errorMessage);
            else
                SetUpdateTimeMessage(LastUpdateTime);
        }

        private void OnOperationError(Error error)
        {
            LastErrorMessage = "Cannot load packages, see console";
        }

        private void CheckInternetReachability()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return;

            OnCheckInternetReachability();
            EditorApplication.update -= CheckInternetReachability;
        }

        private void SetStatusMessage(StatusType status, string message)
        {
            if (status == StatusType.Loading)
            {
                LoadingSpinnerContainer.AddToClassList("loading");
                LoadingSpinner.Start();
            }
            else
            {
                LoadingSpinner.Stop();
                LoadingSpinnerContainer.RemoveFromClassList("loading");
            }


            UIUtils.SetElementDisplay(ErrorIcon, status == StatusType.Error);
            StatusLabel.text = message;
        }

        private VisualElementCache Cache { get; set; }

        private VisualElement LoadingSpinnerContainer { get { return Cache.Get<VisualElement>("loadingSpinnerContainer"); }}
        private LoadingSpinner LoadingSpinner { get { return Cache.Get<LoadingSpinner>("packageSpinner"); }}
        private Label ErrorIcon { get { return Cache.Get<Label>("errorIcon"); }}
        private Label StatusLabel { get { return Cache.Get<Label>("statusLabel"); }}
    }
}
