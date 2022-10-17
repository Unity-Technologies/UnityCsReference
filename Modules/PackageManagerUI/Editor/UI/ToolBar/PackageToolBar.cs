// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageToolbar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageToolbar> {}

        private ApplicationProxy m_Application;
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private AssetStoreCache m_AssetStoreCache;
        private UpmCache m_UpmCache;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private UnityConnectProxy m_UnityConnectProxy;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_Application = container.Resolve<ApplicationProxy>();
            m_AssetStoreDownloadManager = container.Resolve<AssetStoreDownloadManager>();
            m_AssetStoreCache = container.Resolve<AssetStoreCache>();
            m_UpmCache = container.Resolve<UpmCache>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_PageManager = container.Resolve<PageManager>();
            m_UnityConnectProxy = container.Resolve<UnityConnectProxy>();
        }

        private IPackage m_Package;
        private IPackageVersion m_Version;

        private ButtonDisableCondition m_DisableIfCompiling;
        private ButtonDisableCondition m_DisableIfInstallOrUninstallInProgress;
        private ButtonDisableCondition m_DisableIfNoNetwork;

        private PackageAddButton m_AddButton;
        private PackageUpdateButton m_UpdateButton;
        private PackageGitUpdateButton m_GitUpdateButton;
        private PackageRemoveButton m_RemoveButton;
        private PackageRemoveCustomButton m_RemoveCustomButton;
        private PackageResetButton m_ResetButton;

        private PackagePauseDownloadButton m_PauseButton;
        private PackageResumeDownloadButton m_ResumeButton;
        private PackageCancelDownloadButton m_CancelButton;

        private PackageImportButton m_ImportButton;
        private PackageRedownloadButton m_RedownloadButton;
        private PackageDownloadButton m_DownloadButton;
        private PackageDownloadUpdateButton m_DownloadUpdateButton;
        private PackageDowngradeButton m_DowngradeButton;

        private PackageUnlockButton m_UnlockButton;
        private PackageSignInButton m_SignInButton;

        private VisualElement m_MainContainer;
        private VisualElement m_ProgressContainer;
        private PackageToolBarError m_ErrorState;

        private ProgressBar m_DownloadProgress;

        private VisualElement m_BuiltInActions;
        public VisualElement extensions { get; private set; }

        public PackageToolbar()
        {
            ResolveDependencies();

            m_MainContainer = new VisualElement { name = "toolbarMainContainer" };
            Add(m_MainContainer);

            m_ErrorState = new PackageToolBarError() { name = "toolbarErrorState" };
            m_MainContainer.Add(m_ErrorState);

            var leftItems = new VisualElement();
            leftItems.AddToClassList("leftItems");
            m_MainContainer.Add(leftItems);

            extensions = new VisualElement { name = "extensionItems" };
            leftItems.Add(extensions);

            m_BuiltInActions = new VisualElement { name = "builtInActions" };
            m_BuiltInActions.AddToClassList("rightItems");
            m_MainContainer.Add(m_BuiltInActions);

            m_ProgressContainer = new VisualElement { name = "toolbarProgressContainer" };
            Add(m_ProgressContainer);

            m_DownloadProgress = new ProgressBar { name = "downloadProgress" };
            m_ProgressContainer.Add(m_DownloadProgress);

            InitializeButtons();
        }

        private void InitializeButtons()
        {
            m_DisableIfCompiling = new ButtonDisableCondition(() => m_Application.isCompiling,
                L10n.Tr("You need to wait until the compilation is finished to perform this action."));
            m_DisableIfInstallOrUninstallInProgress = new ButtonDisableCondition(() => m_PackageDatabase.isInstallOrUninstallInProgress,
                L10n.Tr("You need to wait until other install or uninstall operations are finished to perform this action."));
            m_DisableIfNoNetwork = new ButtonDisableCondition(() => !m_Application.isInternetReachable,
                L10n.Tr("You need to restore your network connection to perform this action."));

            m_UnlockButton = new PackageUnlockButton(m_PageManager);
            m_UnlockButton.onAction += RefreshBuiltInButtons;
            m_BuiltInActions.Add(m_UnlockButton.element);

            m_AddButton = new PackageAddButton(m_Application, m_PackageDatabase);
            m_AddButton.SetGlobalDisableConditions(m_DisableIfInstallOrUninstallInProgress, m_DisableIfCompiling);
            m_AddButton.onAction += RefreshBuiltInButtons;
            m_BuiltInActions.Add(m_AddButton.element);

            m_UpdateButton = new PackageUpdateButton(m_Application, m_PackageDatabase, m_PageManager);
            m_UpdateButton.SetGlobalDisableConditions(m_DisableIfInstallOrUninstallInProgress, m_DisableIfCompiling);
            m_UpdateButton.onAction += RefreshBuiltInButtons;
            m_BuiltInActions.Add(m_UpdateButton.element);

            m_GitUpdateButton = new PackageGitUpdateButton(m_UpmCache, m_PackageDatabase);
            m_GitUpdateButton.SetGlobalDisableConditions(m_DisableIfInstallOrUninstallInProgress, m_DisableIfCompiling);
            m_GitUpdateButton.onAction += RefreshBuiltInButtons;
            m_BuiltInActions.Add(m_GitUpdateButton.element);

            m_RemoveButton = new PackageRemoveButton(m_Application, m_PackageManagerPrefs, m_PackageDatabase, m_PageManager);
            m_RemoveButton.SetGlobalDisableConditions(m_DisableIfInstallOrUninstallInProgress, m_DisableIfCompiling);
            m_RemoveButton.onAction += RefreshBuiltInButtons;
            m_BuiltInActions.Add(m_RemoveButton.element);

            m_RemoveCustomButton = new PackageRemoveCustomButton(m_Application, m_PackageDatabase, m_PageManager);
            m_RemoveCustomButton.SetGlobalDisableConditions(m_DisableIfInstallOrUninstallInProgress, m_DisableIfCompiling);
            m_RemoveCustomButton.onAction += RefreshBuiltInButtons;
            m_BuiltInActions.Add(m_RemoveCustomButton.element);

            m_ResetButton = new PackageResetButton(m_Application, m_PackageDatabase, m_PageManager);
            m_ResetButton.SetGlobalDisableConditions(m_DisableIfInstallOrUninstallInProgress, m_DisableIfCompiling);
            m_ResetButton.onAction += RefreshBuiltInButtons;
            m_ResetButton.element.SetIcon("customizedIcon");
            m_BuiltInActions.Add(m_ResetButton.element);

            m_ImportButton = new PackageImportButton(m_AssetStoreDownloadManager, m_PackageDatabase);
            m_ImportButton.SetGlobalDisableConditions(m_DisableIfCompiling);
            m_ImportButton.onAction += RefreshBuiltInButtons;
            m_BuiltInActions.Add(m_ImportButton.element);

            m_RedownloadButton = new PackageRedownloadButton(m_AssetStoreDownloadManager, m_AssetStoreCache, m_PackageDatabase);
            m_RedownloadButton.SetGlobalDisableConditions(m_DisableIfNoNetwork, m_DisableIfCompiling);
            m_RedownloadButton.onAction += RefreshBuiltInButtons;
            m_BuiltInActions.Add(m_RedownloadButton.element);

            m_DownloadButton = new PackageDownloadButton(m_AssetStoreDownloadManager, m_AssetStoreCache, m_PackageDatabase);
            m_DownloadButton.SetGlobalDisableConditions(m_DisableIfNoNetwork, m_DisableIfCompiling);
            m_DownloadButton.onAction += Refresh;
            m_BuiltInActions.Add(m_DownloadButton.element);

            m_DownloadUpdateButton = new PackageDownloadUpdateButton(m_AssetStoreDownloadManager, m_AssetStoreCache, m_PackageDatabase);
            m_DownloadUpdateButton.SetGlobalDisableConditions(m_DisableIfNoNetwork, m_DisableIfCompiling);
            m_DownloadUpdateButton.onAction += Refresh;
            m_BuiltInActions.Add(m_DownloadUpdateButton.element);

            m_DowngradeButton = new PackageDowngradeButton(m_AssetStoreDownloadManager, m_AssetStoreCache, m_PackageDatabase);
            m_DowngradeButton.SetGlobalDisableConditions(m_DisableIfNoNetwork, m_DisableIfCompiling);
            m_DowngradeButton.onAction += Refresh;
            m_BuiltInActions.Add(m_DowngradeButton.element);

            m_SignInButton = new PackageSignInButton(m_UnityConnectProxy);
            m_SignInButton.SetGlobalDisableConditions(m_DisableIfNoNetwork);
            m_SignInButton.onAction += RefreshBuiltInButtons;
            m_BuiltInActions.Add(m_SignInButton.element);

            // Since pause, resume, cancel buttons are only used to control the download progress, we want to put them in the progress container instead
            m_ResumeButton = new PackageResumeDownloadButton(m_AssetStoreDownloadManager, m_PackageDatabase, true);
            m_ResumeButton.SetGlobalDisableConditions(m_DisableIfNoNetwork, m_DisableIfCompiling);
            m_ResumeButton.onAction += RefreshProgressControlButtons;
            m_ProgressContainer.Add(m_ResumeButton.element);

            m_PauseButton = new PackagePauseDownloadButton(m_AssetStoreDownloadManager, m_PackageDatabase, true);
            m_PauseButton.SetGlobalDisableConditions(m_DisableIfCompiling);
            m_PauseButton.onAction += RefreshProgressControlButtons;
            m_ProgressContainer.Add(m_PauseButton.element);

            m_CancelButton = new PackageCancelDownloadButton(m_AssetStoreDownloadManager, m_PackageDatabase, true);
            m_CancelButton.SetGlobalDisableConditions(m_DisableIfCompiling);
            m_CancelButton.onAction += Refresh;
            m_ProgressContainer.Add(m_CancelButton.element);
        }

        public void OnEnable()
        {
            m_Application.onFinishCompiling += Refresh;

            m_AssetStoreDownloadManager.onDownloadProgress += OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadFinalized += OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadPaused += OnDownloadProgress;
        }

        public void OnDisable()
        {
            m_Application.onFinishCompiling -= Refresh;

            m_AssetStoreDownloadManager.onDownloadProgress -= OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadFinalized -= OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadPaused -= OnDownloadProgress;
        }

        public void Refresh(IPackage package, IPackageVersion version)
        {
            m_Package = package;
            m_Version = version;

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
            operation ??= m_AssetStoreDownloadManager.GetDownloadOperation(m_Version?.packageUniqueId);
            var progressVisible = operation != null && m_Version?.packageUniqueId == operation.packageUniqueId && m_DownloadProgress.UpdateProgress(operation);
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
            m_SignInButton.Refresh(m_Version);

            m_UnlockButton.Refresh(m_Version);
            m_GitUpdateButton.Refresh(m_Version);
            m_AddButton.Refresh(m_Version);
            m_UpdateButton.Refresh(m_Version);
            m_RemoveButton.Refresh(m_Version);
            m_RemoveCustomButton.Refresh(m_Version);
            m_ResetButton.Refresh(m_Version);

            m_ImportButton.Refresh(m_Version);
            m_RedownloadButton.Refresh(m_Version);
            m_DownloadButton.Refresh(m_Version);
            m_DownloadUpdateButton.Refresh(m_Version);
            m_DowngradeButton.Refresh(m_Version);
        }

        private void RefreshProgressControlButtons()
        {
            m_PauseButton.Refresh(m_Version);
            m_ResumeButton.Refresh(m_Version);
            m_CancelButton.Refresh(m_Version);
        }

        private void RefreshExtensionItems()
        {
            var disableCondition = new[] { m_DisableIfInstallOrUninstallInProgress, m_DisableIfCompiling }.FirstOrDefault(c => c.value);
            foreach (var item in extensions.Children())
                item.SetEnabled(disableCondition == null);
            extensions.tooltip = disableCondition?.tooltip ?? string.Empty;
        }

        private void OnDownloadProgress(IOperation operation)
        {
            if (m_Version?.packageUniqueId != operation.packageUniqueId)
                return;

            // We call `RefreshProgressContainer` here instead of calling `Refresh` here directly when the download is progressing to save some time
            // We only want to do a proper refresh in cases where `RefreshProgressContainer` would return false (progress bar no longer visible)
            if (UIUtils.IsElementVisible(m_ProgressContainer) && RefreshProgressContainer(operation))
                return;

            Refresh();
        }
    }
}
