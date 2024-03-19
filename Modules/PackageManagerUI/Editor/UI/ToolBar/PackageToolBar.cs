// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageToolbar : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new PackageToolbar();
        }

        private IApplicationProxy m_Application;
        private IAssetStoreDownloadManager m_AssetStoreDownloadManager;
        private IUpmCache m_UpmCache;
        private IPackageManagerPrefs m_PackageManagerPrefs;
        private IPackageDatabase m_PackageDatabase;
        private IPackageOperationDispatcher m_OperationDispatcher;
        private IPageManager m_PageManager;
        private IUnityConnectProxy m_UnityConnect;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_Application = container.Resolve<IApplicationProxy>();
            m_AssetStoreDownloadManager = container.Resolve<IAssetStoreDownloadManager>();
            m_UpmCache = container.Resolve<IUpmCache>();
            m_PackageManagerPrefs = container.Resolve<IPackageManagerPrefs>();
            m_PackageDatabase = container.Resolve<IPackageDatabase>();
            m_OperationDispatcher = container.Resolve<IPackageOperationDispatcher>();
            m_PageManager = container.Resolve<IPageManager>();
            m_UnityConnect = container.Resolve<IUnityConnectProxy>();
        }

        private IPackage m_Package;
        private IPackageVersion m_Version;

        private IList<PackageToolBarButton> m_BuiltInToolBarButtons;
        private IList<PackageToolBarButton> m_ProgressControlButtons;

        private VisualElement m_MainContainer;
        private VisualElement m_ProgressContainer;
        private PackageToolBarError m_ErrorState;

        private ProgressBar m_DownloadProgress;

        private VisualElement m_BuiltInActionsContainer;
        public VisualElement extensions { get; }

        public PackageToolbar()
        {
            ResolveDependencies();

            m_MainContainer = new VisualElement { name = "toolbarMainContainer" };
            Add(m_MainContainer);

            m_ErrorState = new PackageToolBarError { name = "toolbarErrorState" };
            m_MainContainer.Add(m_ErrorState);

            var leftItems = new VisualElement();
            leftItems.AddToClassList("leftItems");
            m_MainContainer.Add(leftItems);

            extensions = new VisualElement { name = "extensionItems" };
            leftItems.Add(extensions);

            m_BuiltInActionsContainer = new VisualElement { name = "builtInActions" };
            m_BuiltInActionsContainer.AddToClassList("rightItems");
            m_MainContainer.Add(m_BuiltInActionsContainer);

            m_ProgressContainer = new VisualElement { name = "toolbarProgressContainer" };
            Add(m_ProgressContainer);

            m_DownloadProgress = new ProgressBar { name = "downloadProgress" };
            m_ProgressContainer.Add(m_DownloadProgress);

            InitializeButtons();
        }

        private void InitializeButtons()
        {
            m_BuiltInToolBarButtons = new PackageToolBarButton[]
            {
                new PackageToolBarSimpleButton(new UnlockAction(m_PageManager)),
                new PackageToolBarSimpleButton(new SignInAction(m_UnityConnect, m_Application)),
                new PackageToolBarSimpleButton(new AddAction(m_OperationDispatcher, m_Application, m_PackageDatabase)),
                new PackageToolBarSimpleButton(new UpdateAction(m_OperationDispatcher, m_Application, m_PackageDatabase, m_PageManager)),
                new PackageToolBarSimpleButton(new GitUpdateAction(m_OperationDispatcher, m_UpmCache, m_Application)),
                new PackageToolBarSimpleButton(new RemoveAction(m_OperationDispatcher, m_Application, m_PackageManagerPrefs, m_PackageDatabase, m_PageManager)),
                new PackageToolBarSimpleButton(new RemoveCustomAction(m_OperationDispatcher, m_Application)),
                new PackageToolBarButtonWithIcon(new ResetAction(m_OperationDispatcher, m_Application, m_PackageDatabase, m_PageManager)),
                new LegacyFormatDropdownButton(m_OperationDispatcher, m_AssetStoreDownloadManager, m_UnityConnect, m_Application)
            };

            foreach (var button in m_BuiltInToolBarButtons)
            {
                button.onActionTriggered += Refresh;
                m_BuiltInActionsContainer.Add(button);
            }

            // Since pause, resume, cancel buttons are only used to control the download progress, we want to put them in the progress container instead
            m_ProgressControlButtons = new PackageToolBarButton[]
            {
                new PackageToolBarIconOnlyButton(new ResumeDownloadAction(m_OperationDispatcher, m_AssetStoreDownloadManager, m_Application)),
                new PackageToolBarIconOnlyButton(new PauseDownloadAction(m_OperationDispatcher, m_AssetStoreDownloadManager, m_Application)),
                new PackageToolBarIconOnlyButton(new CancelDownloadAction(m_OperationDispatcher, m_AssetStoreDownloadManager, m_Application))
            };

            foreach (var button in m_ProgressControlButtons)
            {
                button.onActionTriggered += Refresh;
                m_ProgressContainer.Add(button);
            }
        }

        public void OnEnable()
        {
            m_Application.onFinishCompiling += Refresh;

            m_AssetStoreDownloadManager.onDownloadProgress += OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadFinalized += OnDownloadProgress;
        }

        public void OnDisable()
        {
            m_Application.onFinishCompiling -= Refresh;

            m_AssetStoreDownloadManager.onDownloadProgress -= OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadFinalized -= OnDownloadProgress;
        }

        public void Refresh(IPackage package)
        {
            m_Package = package;
            m_Version = package.versions.primary;

            Refresh();
        }

        private void Refresh()
        {
            // Since only one of `progressContainer` or `mainContainer` can be visible at the same time
            // we can use `chain` refresh mechanism in the order of priority (progress > main)
            if (RefreshProgressContainer())
                return;

            RefreshMainContainer();
        }

        // Returns true if the progress bar is visible and there's no need to further check other containers
        private bool RefreshProgressContainer(IOperation operation = null)
        {
            operation ??= m_AssetStoreDownloadManager.GetDownloadOperation(m_Package?.product?.id);
            var progressVisible = operation != null && m_Version?.package?.uniqueId == operation.packageUniqueId && m_DownloadProgress.UpdateProgress(operation);
            UIUtils.SetElementDisplay(m_ProgressContainer, progressVisible);
            if (progressVisible)
            {
                UIUtils.SetElementDisplay(m_MainContainer, false);
                RefreshProgressControlButtons();
            }
            return progressVisible;
        }

        private void RefreshMainContainer()
        {
            UIUtils.SetElementDisplay(m_ErrorState, m_ErrorState.Refresh(m_Package, m_Version));
            UIUtils.SetElementDisplay(m_ProgressContainer, false);
            UIUtils.SetElementDisplay(m_MainContainer, true);

            RefreshBuiltInButtons();
            RefreshExtensionItems();
        }

        private void RefreshBuiltInButtons()
        {
            foreach (var button in m_BuiltInToolBarButtons)
                button.Refresh(m_Version);
        }

        private void RefreshProgressControlButtons()
        {
            foreach (var button in m_ProgressControlButtons)
                button.Refresh(m_Version);
        }

        private void RefreshExtensionItems()
        {
            var activeDisableCondition = new DisableCondition[]
            {
                new DisableIfInstallOrUninstallInProgress(m_OperationDispatcher),
                new DisableIfCompiling(m_Application)
            }.FirstOrDefault(c => c.active);
            foreach (var item in extensions.Children())
                item.SetEnabled(activeDisableCondition == null);
            extensions.tooltip = activeDisableCondition?.tooltip ?? string.Empty;
        }

        private void OnDownloadProgress(IOperation operation)
        {
            if (m_Version?.package.uniqueId != operation.packageUniqueId)
                return;

            // We call `RefreshProgressContainer` here instead of calling `Refresh` here directly when the download is progressing to save some time
            // We only want to do a proper refresh in cases where `RefreshProgressContainer` would return false (progress bar no longer visible)
            if (UIUtils.IsElementVisible(m_ProgressContainer) && RefreshProgressContainer(operation))
                return;

            Refresh();
        }
    }
}
