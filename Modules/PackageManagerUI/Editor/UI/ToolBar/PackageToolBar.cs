// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageToolbar : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance()
            {
                var container = ServicesContainer.instance;
                return new PackageToolbar(
                    container.Resolve<IResourceLoader>(),
                    container.Resolve<IApplicationProxy>(),
                    container.Resolve<IAssetStoreDownloadManager>(),
                    container.Resolve<IUpmCache>(),
                    container.Resolve<IPackageManagerPrefs>(),
                    container.Resolve<IPackageDatabase>(),
                    container.Resolve<IPackageOperationDispatcher>(),
                    container.Resolve<IPageManager>(),
                    container.Resolve<IUnityConnectProxy>(),
                    container.Resolve<IModalManager>());
            }
        }

        private IPackage m_Package;
        private IPackageVersion m_Version;

        private IList<IPackageToolBarButton> m_BuiltInToolBarButtons;
        private IList<IPackageToolBarButton> m_ProgressControlButtons;

        private VisualElement m_MainContainer;
        private VisualElement m_ProgressContainer;
        private PackageToolBarError m_ErrorState;

        private ProgressBar m_DownloadProgress;

        private VisualElement m_BuiltInActionsContainer;
        public VisualElement extensions { get; }

        private readonly IApplicationProxy m_Application;
        private readonly IAssetStoreDownloadManager m_AssetStoreDownloadManager;
        private readonly IUpmCache m_UpmCache;
        private readonly IPackageManagerPrefs m_PackageManagerPrefs;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPackageOperationDispatcher m_OperationDispatcher;
        private readonly IPageManager m_PageManager;
        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IModalManager m_ModalManager;
        public PackageToolbar(
            IResourceLoader resourceLoader,
            IApplicationProxy application,
            IAssetStoreDownloadManager assetStoreDownloadManager,
            IUpmCache upmCache,
            IPackageManagerPrefs packageManagerPrefs,
            IPackageDatabase packageDatabase,
            IPackageOperationDispatcher operationDispatcher,
            IPageManager pageManager,
            IUnityConnectProxy unityConnect,
            IModalManager modalManager)
        {
            m_Application = application;
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_UpmCache = upmCache;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_PackageDatabase = packageDatabase;
            m_OperationDispatcher = operationDispatcher;
            m_PageManager = pageManager;
            m_UnityConnect = unityConnect;
            m_ModalManager = modalManager;

            m_MainContainer = new VisualElement { name = "toolbarMainContainer" };
            Add(m_MainContainer);

            m_ErrorState = new PackageToolBarError { name = "toolbarErrorState" };
            m_MainContainer.Add(m_ErrorState);

            var actionButtons = new VisualElement();
            actionButtons.AddToClassList("actionButtons");
            m_MainContainer.Add(actionButtons);

            extensions = new VisualElement { name = "extensionItems" };
            actionButtons.Add(extensions);

            m_BuiltInActionsContainer = new VisualElement { name = "builtInActions" };
            m_BuiltInActionsContainer.AddToClassList("actionButtons");
            m_MainContainer.Add(m_BuiltInActionsContainer);

            m_ProgressContainer = new VisualElement { name = "toolbarProgressContainer" };
            Add(m_ProgressContainer);

            m_DownloadProgress = new ProgressBar(resourceLoader) { name = "downloadProgress" };
            m_ProgressContainer.Add(m_DownloadProgress);

            InitializeButtons();
        }

        private void InitializeButtons()
        {
            m_BuiltInToolBarButtons = new IPackageToolBarButton[]
            {
                new PackageToolBarSimpleButton(new LocateAction(m_Application)),
                new PackageToolBarSimpleButton(new SignInAction(m_UnityConnect, m_Application)),
                new PackageToolBarSimpleButton(new ExportAction(m_ModalManager)),
                new PackageToolBarSimpleButton(new AddAction(m_OperationDispatcher, m_Application, m_PackageDatabase)),
                new LegacyFormatDropdownButton(m_OperationDispatcher, m_AssetStoreDownloadManager, m_UnityConnect, m_Application),
                new ManageDropdownButton(m_Application, m_UpmCache, m_PackageManagerPrefs, m_PackageDatabase, m_OperationDispatcher, m_PageManager)
            };

            foreach (var button in m_BuiltInToolBarButtons)
            {
                button.onActionTriggered += Refresh;
                m_BuiltInActionsContainer.Add(button.element);
            }

            // Since pause, resume, cancel buttons are only used to control the download progress, we want to put them in the progress container instead
            m_ProgressControlButtons = new IPackageToolBarButton[]
            {
                new PackageToolBarIconOnlyButton(new ResumeDownloadAction(m_OperationDispatcher, m_AssetStoreDownloadManager, m_Application)),
                new PackageToolBarIconOnlyButton(new PauseDownloadAction(m_OperationDispatcher, m_AssetStoreDownloadManager, m_Application)),
                new PackageToolBarIconOnlyButton(new CancelDownloadAction(m_OperationDispatcher, m_AssetStoreDownloadManager, m_Application))
            };

            foreach (var button in m_ProgressControlButtons)
            {
                button.onActionTriggered += Refresh;
                m_ProgressContainer.Add(button.element);
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
            var activeDisableCondition = GetDisableConditions().FirstMatch(c => c.active);
            foreach (var item in extensions.Children())
                item.SetEnabled(activeDisableCondition == null);
            extensions.tooltip = activeDisableCondition?.tooltip ?? string.Empty;
        }

        private IEnumerable<DisableCondition> GetDisableConditions()
        {
            yield return new DisableIfInstallOrEmbedOrUninstallInProgress(m_OperationDispatcher);
            yield return new DisableIfCompiling(m_Application);
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
