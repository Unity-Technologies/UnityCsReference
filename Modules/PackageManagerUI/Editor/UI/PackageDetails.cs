// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetails : VisualElement
    {
        private const string k_TermsOfServicesURL = "https://assetstore.unity.com/account/term";
        internal new class UxmlFactory : UxmlFactory<PackageDetails> { }

        private const string k_CustomizedIcon = "customizedIcon";
        private const string k_WarningIcon = "warningIcon";

        private IPackageVersion m_Version;
        private IPackage package { set; get; }

        private IPackageVersion displayVersion
        {
            get { return m_Version ?? package?.versions.primary; }
            set { m_Version = value; }
        }

        private IPackageVersion targetVersion
        {
            get
            {
                var isInstalledVersion = displayVersion?.isInstalled ?? false;

                if (isInstalledVersion)
                {
                    if (displayVersion != package.versions.recommended)
                        return package.versions.latest ?? displayVersion;

                    return package.versions.recommended ?? displayVersion;
                }
                return displayVersion;
            }
        }

        private bool isRequestedButOverriddenVersion => UpmPackageVersion.IsRequestedButOverriddenVersion(package, displayVersion);

        private IEnumerable<IPackageVersion> featureSetDependents;

        internal enum PackageAction
        {
            None,
            Add,
            Remove,
            Update,
            Enable,
            Disable,
            Download,
            Upgrade,
            Pause,
            Resume,
            Cancel,
            Import,
            Reset,
            GitUpdate,
            ReDownload,
            Downgrade
        }

        internal static readonly string[] k_PackageActionVerbs =
        {
            string.Empty,
            L10n.Tr("Install"),
            L10n.Tr("Remove"),
            L10n.Tr("Update to"),
            L10n.Tr("Enable"),
            L10n.Tr("Disable"),
            L10n.Tr("Download"),
            L10n.Tr("Update"),
            L10n.Tr("Pause"),
            L10n.Tr("Resume"),
            L10n.Tr("Cancel"),
            L10n.Tr("Import"),
            L10n.Tr("Reset"),
            L10n.Tr("Update"),
            L10n.Tr("Re-Download"),
            L10n.Tr("Update")
        };

        private static readonly string[] k_PackageActionInProgressVerbs =
        {
            string.Empty,
            L10n.Tr("Installing"),
            L10n.Tr("Removing"),
            L10n.Tr("Updating to"),
            L10n.Tr("Enabling"),
            L10n.Tr("Disabling"),
            L10n.Tr("Download"),
            L10n.Tr("Update"),
            L10n.Tr("Pause"),
            L10n.Tr("Resume"),
            L10n.Tr("Cancel"),
            L10n.Tr("Import"),
            L10n.Tr("Resetting"),
            L10n.Tr("Update"),
            L10n.Tr("Re-Download"),
            L10n.Tr("Update")
        };

        internal static readonly string[] k_PackageActionTooltips =
        {
            string.Empty,
            L10n.Tr("Click to install this {0} into your project."),
            L10n.Tr("Click to remove this {0} from your project."),
            L10n.Tr("Click to update this {0} to the specified version."),
            L10n.Tr("Enable the use of this {0} in your project."),
            L10n.Tr("Disable the use of this {0} in your project."),
            L10n.Tr("Click to download this {0} for later use."),
            L10n.Tr("Click to download the latest version of this {0}."),
            L10n.Tr("Click to pause the download of this {0}."),
            L10n.Tr("Click to resume the download of this {0}."),
            L10n.Tr("Click to cancel the download of this {0}."),
            L10n.Tr("Click to import assets from the {0} into your project."),
            L10n.Tr("Click to reset this {0} dependencies to their default versions."),
            L10n.Tr("Click to check for updates and update to latest version"),
            L10n.Tr("Click to re-download the latest version of this {0}."),
            string.Empty
        };

        private ResourceLoader m_ResourceLoader;
        private ExtensionManager m_ExtensionManager;
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
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_ExtensionManager = container.Resolve<ExtensionManager>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_AssetStoreDownloadManager = container.Resolve<AssetStoreDownloadManager>();
            m_AssetStoreCache = container.Resolve<AssetStoreCache>();
            m_UpmCache = container.Resolve<UpmCache>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_PageManager = container.Resolve<PageManager>();
            m_UnityConnectProxy = container.Resolve<UnityConnectProxy>();
        }

        public PackageDetails()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageDetails.uxml");
            Add(root);

            cache = new VisualElementCache(root);

            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var extension in PackageManagerExtensions.Extensions)
                    customContainer.Add(extension.CreateExtensionUI());
            });

            root.StretchToParentSize();

            unlockButton.clickable.clicked += UnlockClick;
            updateButton.clickable.clicked += UpdateClick;
            removeButton.clickable.clicked += RemoveClick;
            resetButton.clicked += ResetClick;
            resetButton.SetIcon(k_CustomizedIcon);
            importButton.clickable.clicked += ImportClick;
            downloadButton.clickable.clicked += DownloadClick;
            redownloadButton.clickable.clicked += ReDownloadClick;
            downgradeButton.clicked += DowngradeClick;
            downgradeButton.SetIcon(k_WarningIcon);
            cancelButton.clickable.clicked += CancelClick;
            pauseButton.clickable.clicked += PauseClick;
            resumeButton.clickable.clicked += ResumeClick;

            okButton.clickable.clicked += ClearError;

            signInButton.clickable.clicked += m_UnityConnectProxy.ShowLogin;

            detailScrollView.verticalScroller.valueChanged += OnDetailScroll;

            errorMessage.ShowTextTooltipOnSizeChange();

            detailScrollView.RegisterCallback<GeometryChangedEvent>(RecalculateFillerHeight);
            detail.RegisterCallback<GeometryChangedEvent>(RecalculateFillerHeight);

            RefreshContent();
        }

        private void RecalculateFillerHeight(GeometryChangedEvent evt)
        {
            if (evt.oldRect.height == evt.newRect.height)
                return;
            featureDependencies.RecalculateFillerHeight(detail.layout.height, detailScrollView.layout.height);
        }

        private void OnDetailScroll(float offset)
        {
            m_PackageManagerPrefs.packageDetailVerticalScrollOffset = offset;
        }

        public void OnEnable()
        {
            body.OnEnable();

            m_Application.onFinishCompiling += RefreshPackageActionButtons;
            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;

            m_PackageDatabase.onPackagesChanged += OnPackagesUpdated;
            m_PackageDatabase.onVerifiedGitPackageUpToDate += OnVerifiedGitPackageUpToDate;

            m_PackageDatabase.onPackageProgressUpdate += OnPackageProgressUpdate;

            m_PackageDatabase.onTermOfServiceAgreementStatusChange += OnTermOfServiceAgreementStatusChange;

            m_AssetStoreDownloadManager.onDownloadProgress += UpdateDownloadProgressBar;
            m_AssetStoreDownloadManager.onDownloadFinalized += StopDownloadProgressBar;
            m_AssetStoreDownloadManager.onDownloadPaused += PauseDownloadProgressBar;

            m_PageManager.onSelectionChanged += OnSelectionChanged;

            m_UnityConnectProxy.onUserLoginStateChange += OnUserLoginStateChange;

            RefreshUI(m_PageManager.GetSelectedVersion());
        }

        private void OnTermOfServiceAgreementStatusChange(TermOfServiceAgreementStatus status)
        {
            if (status == TermOfServiceAgreementStatus.Accepted)
                return;

            var result = m_Application.DisplayDialog(L10n.Tr("Package Manager"),
                L10n.Tr("You need to accept Asset Store Terms of Service and EULA before you can download/update any package."),
                L10n.Tr("Read and accept"), L10n.Tr("Close"));

            if (result)
                m_UnityConnectProxy.OpenAuthorizedURLInWebBrowser(k_TermsOfServicesURL);
        }

        public void OnDisable()
        {
            body.OnDisable();

            m_Application.onFinishCompiling -= RefreshPackageActionButtons;
            m_Application.onInternetReachabilityChange -= OnInternetReachabilityChange;

            m_PackageDatabase.onPackagesChanged -= OnPackagesUpdated;
            m_PackageDatabase.onVerifiedGitPackageUpToDate -= OnVerifiedGitPackageUpToDate;

            m_PackageDatabase.onPackageProgressUpdate -= OnPackageProgressUpdate;

            m_PackageDatabase.onTermOfServiceAgreementStatusChange -= OnTermOfServiceAgreementStatusChange;

            m_AssetStoreDownloadManager.onDownloadProgress -= UpdateDownloadProgressBar;
            m_AssetStoreDownloadManager.onDownloadFinalized -= StopDownloadProgressBar;
            m_AssetStoreDownloadManager.onDownloadPaused -= PauseDownloadProgressBar;

            m_PageManager.onSelectionChanged -= OnSelectionChanged;

            m_UnityConnectProxy.onUserLoginStateChange -= OnUserLoginStateChange;
        }

        private void OnInternetReachabilityChange(bool value)
        {
            var message = L10n.Tr("You need to restore your network connection to perform this action.");
            RefreshButtonStatusAndTooltip(resumeButton, PackageAction.Resume,
                new ButtonDisableCondition(!value, message));

            RefreshButtonStatusAndTooltip(downloadButton, PackageAction.Download,
                new ButtonDisableCondition(!value, message));

            RefreshButtonStatusAndTooltip(downgradeButton, PackageAction.Downgrade,
                new ButtonDisableCondition(!value, message));

            RefreshButtonStatusAndTooltip(redownloadButton, PackageAction.ReDownload,
                new ButtonDisableCondition(!value, message));

            if (value)
                RefreshErrorDisplay();

            if (package?.hasEntitlementsError ?? false)
                RefreshEntitlement();
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (!userInfoReady || package == null)
                return;

            RefreshEntitlement();
        }

        private void UpdateDownloadProgressBar(IOperation operation)
        {
            if (displayVersion?.packageUniqueId != operation.packageUniqueId)
                return;
            downloadProgress.UpdateProgress(operation);
            RefreshDownloadStatesButtons();
        }

        private void StopDownloadProgressBar(IOperation operation)
        {
            if (displayVersion?.packageUniqueId != operation.packageUniqueId)
                return;

            var downloadOperation = operation as AssetStoreDownloadOperation;
            if (downloadOperation.state == DownloadState.Error || downloadOperation.state == DownloadState.Aborted)
                RefreshErrorDisplay();
            downloadProgress.UpdateProgress(operation);

            RefreshDownloadStatesButtons();
        }

        private void PauseDownloadProgressBar(IOperation operation)
        {
            if (displayVersion?.packageUniqueId != operation.packageUniqueId)
                return;
            downloadProgress.UpdateProgress(operation);
            RefreshDownloadStatesButtons();
        }

        private void SetContentVisibility(bool visible)
        {
            UIUtils.SetElementDisplay(detail, visible);
            UIUtils.SetElementDisplay(packageToolbarContainer, visible);

            SetToolBarVisibility(visible);
        }

        private void SetToolBarVisibility(bool visible)
        {
            UIUtils.SetElementDisplay(packageToolbarMainContainer, visible);
            UIUtils.SetElementDisplay(packageToolbarLeftArea, visible);
            UIUtils.SetElementDisplay(packageToolbarErrorContainer, !visible);
        }

        internal void OnSelectionChanged(IPackageVersion version)
        {
            m_PackageManagerPrefs.packageDetailVerticalScrollOffset = 0;
            RefreshUI(version);
        }

        internal void RefreshUI(IPackageVersion version)
        {
            if (version != null)
                SetPackage(m_PackageDatabase.GetPackage(version), version);
            else
                SetPackage(null);
        }

        void RefreshExtensions(IPackage package, IPackageVersion version)
        {
            if (PackageManagerExtensions.Extensions.Any())
            {
                var packageInfo = version != null ? m_UpmCache.GetBestMatchPackageInfo(version.name, version.isInstalled, version.versionString) : null;
                PackageManagerExtensions.ExtensionCallback(() =>
                {
                    foreach (var extension in PackageManagerExtensions.Extensions)
                        extension.OnPackageSelectionChange(packageInfo);
                });
            }

            m_ExtensionManager.SendPackageSelectionChangedEvent(package, version);
        }

        private void RefreshContent()
        {
            detailScrollView.scrollOffset = new Vector2(0, m_PackageManagerPrefs.packageDetailVerticalScrollOffset);

            var detailVisible = package != null && displayVersion != null && !inProgressView.Refresh(package);
            var detailEnabled = displayVersion == null || displayVersion.isFullyFetched;

            if (!detailVisible)
            {
                UIUtils.SetElementDisplay(signInButton, false);
                UIUtils.SetElementDisplay(customContainer, false);
                UIUtils.SetElementDisplay(extensionContainer, false);
                RefreshExtensions(null, null);
            }
            else
            {
                featureSetDependents = m_PackageDatabase.GetFeatureDependents(package.versions.installed);
                header.Refresh(package, displayVersion, featureSetDependents);
                body.Refresh(package, displayVersion);

                RefreshEntitlement();

                UIUtils.SetElementDisplay(customContainer, true);
                UIUtils.SetElementDisplay(extensionContainer, true);
                RefreshExtensions(package, displayVersion);

                RefreshPackageActionButtons();

                // Here Do logic for Download/Cancel/Pause/Resume
                RefreshDownloadStatesButtons();
            }

            // Set visibility
            SetContentVisibility(detailVisible);
            SetEnabled(detailEnabled);
            RefreshErrorDisplay();
        }

        private void RefreshEntitlement()
        {
            header.RefreshEntitlement();

            var hasEntitlementsError = package.hasEntitlementsError;
            body.SetEnabled(!hasEntitlementsError);
            UIUtils.SetElementDisplay(signInButton, hasEntitlementsError && !m_UnityConnectProxy.isUserLoggedIn);
            RefreshButtonStatusAndTooltip(signInButton, disableIfNoNetwork);
        }

        public void SetPackage(IPackage package, IPackageVersion version = null)
        {
            this.package = package;
            displayVersion = version ?? package?.versions.primary;

            if (version?.isFullyFetched == false)
                m_PackageDatabase.FetchExtraInfo(version);

            RefreshContent();
        }

        internal void OnVerifiedGitPackageUpToDate(IPackage package)
        {
            Debug.Log(string.Format(L10n.Tr("{0} is already up-to-date."), package.displayName));
        }

        internal void OnPackagesUpdated(IEnumerable<IPackage> updatedPackages)
        {
            var packageUniqudId = package?.uniqueId;
            if (string.IsNullOrEmpty(packageUniqudId) || !updatedPackages.Any())
                return;

            var updatedPackage = updatedPackages.FirstOrDefault(p => p.uniqueId == packageUniqudId);
            // if Git and updated, inform the user
            if (updatedPackage != null)
            {
                var updatedVersion = displayVersion == null ? null : updatedPackage.versions.FirstOrDefault(v => v.uniqueId == displayVersion.uniqueId);
                SetPackage(updatedPackage, updatedVersion);

                if (updatedVersion?.isFullyFetched ?? false)
                    SetEnabled(true);
            }
        }

        private void OnPackagesUpdated(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> preUpdated, IEnumerable<IPackage> postUpdated)
        {
            OnPackagesUpdated(postUpdated);
        }

        private void RefreshErrorDisplay()
        {
            var error = displayVersion?.errors?.FirstOrDefault(e => !e.HasAttribute(UIError.Attribute.IsClearable)) ?? package?.errors?.FirstOrDefault(e => !e.HasAttribute(UIError.Attribute.IsClearable));
            detailError.RefreshError(error, package, displayVersion);

            var operationError = displayVersion?.errors?.FirstOrDefault(e => e.HasAttribute(UIError.Attribute.IsClearable)) ?? package?.errors?.FirstOrDefault(e => e.HasAttribute(UIError.Attribute.IsClearable));
            if (operationError == null)
            {
                ClearError();
            }
            else
            {
                SetError(operationError.message, operationError.HasAttribute(UIError.Attribute.IsWarning) ? PackageState.Warning : PackageState.Error);
                SetToolBarVisibility(false);
            }
        }

        public void ClearError()
        {
            UIUtils.SetElementDisplay(packageToolbarErrorContainer, false);
            errorMessage.text = string.Empty;
            errorStatus.ClearClassList();

            var error = displayVersion?.errors?.FirstOrDefault() ?? package?.errors?.FirstOrDefault();
            if (error != null)
            {
                package?.ClearErrors(e => e.HasAttribute(UIError.Attribute.IsClearable));
                RefreshEntitlement();
                RefreshAddButton();
            }

            SetToolBarVisibility(true);
        }

        public void SetError(string message, PackageState state)
        {
            switch (state)
            {
                case PackageState.Error:
                    errorStatus.AddClasses("error");
                    break;
                case PackageState.Warning:
                    errorStatus.AddClasses("warning");
                    break;
                default: break;
            }

            errorMessage.text = message;
            UIUtils.SetElementDisplay(packageToolbarErrorContainer, true);
        }

        internal void OnPackageProgressUpdate(IPackage package)
        {
            RefreshPackageActionButtons();
        }

        internal void RefreshPackageActionButtons()
        {
            RefreshUnlockButton();
            RefreshAddButton();
            RefreshRemoveButton();
            RefreshResetButton();
            RefreshToolbarExtensionsStatusAndTooltip();
        }

        private void RefreshAddButton()
        {
            var installed = package?.versions.installed;
            var targetVersion = this.targetVersion;
            var installable = targetVersion?.HasTag(PackageTag.Installable) ?? false;
            var isGitInstalled = installed?.HasTag(PackageTag.Git) == true;
            var visibleFlag = installed?.HasTag(PackageTag.VersionLocked) != true && displayVersion != null && installable && installed != targetVersion && !isRequestedButOverriddenVersion
                && m_PageManager.GetVisualState(package)?.isLocked != true;
            if (visibleFlag)
            {
                SemVersion? versionToUpdateTo = null;
                var action = displayVersion.HasTag(PackageTag.BuiltIn) ? PackageAction.Enable : PackageAction.Add;
                if (installed != null)
                {
                    action = PackageAction.Update;
                    versionToUpdateTo = targetVersion.version;
                }

                var currentPackageInstallInProgress = m_PackageDatabase.IsInstallInProgress(displayVersion);
                updateButton.text = GetButtonText(action, currentPackageInstallInProgress, versionToUpdateTo);

                RefreshButtonStatusAndTooltip(updateButton, action, disableIfInstallOrUninstallInProgress, disableIfCompiling, disableIfEntitlementsError);
            }
            else if (isGitInstalled)
            {
                var action = PackageAction.GitUpdate;
                var currentPackageInstallInProgress = m_PackageDatabase.IsInstallInProgress(displayVersion);
                updateButton.text = GetButtonText(action, currentPackageInstallInProgress, null);
                RefreshButtonStatusAndTooltip(updateButton, action, disableIfInstallOrUninstallInProgress, disableIfCompiling);
            }
            UIUtils.SetElementDisplay(updateButton, isGitInstalled || visibleFlag);
        }

        private void RefreshToolbarExtensionsStatusAndTooltip()
        {
            var disableCondition = new[] { disableIfInstallOrUninstallInProgress, disableIfCompiling }.FirstOrDefault(c => c.value);
            foreach (var item in toolbarExtensions.Children())
                item.SetEnabled(disableCondition == null);
            toolbarExtensions.tooltip = disableCondition?.tooltip ?? string.Empty;
        }

        private void RefreshRemoveButton()
        {
            var installed = package?.versions.installed;
            var removable = displayVersion?.HasTag(PackageTag.Removable) ?? false;
            var visibleFlag = installed != null && (installed == displayVersion || isRequestedButOverriddenVersion) && removable;
            if (visibleFlag)
            {
                var action = displayVersion.HasTag(PackageTag.BuiltIn) ? PackageAction.Disable : PackageAction.Remove;
                var currentPackageRemoveInProgress = m_PackageDatabase.IsUninstallInProgress(package);
                removeButton.text = GetButtonText(action, currentPackageRemoveInProgress);

                var isInstalledAsDependency = installed == displayVersion && (!displayVersion.isDirectDependency || UpmPackageVersion.IsDifferentVersionThanRequested(displayVersion));
                var disableIfUsedByOthers = new ButtonDisableCondition(isInstalledAsDependency,
                    string.Format(L10n.Tr("You cannot remove this {0} because another installed package or feature depends on it. See dependencies for more details."), package.GetDescriptor()));
                RefreshButtonStatusAndTooltip(removeButton, action,
                    disableIfUsedByOthers,
                    disableIfInstallOrUninstallInProgress,
                    disableIfCompiling);
            }
            UIUtils.SetElementDisplay(removeButton, visibleFlag);
        }

        private void RefreshResetButton()
        {
            var feature = package?.Is(PackageType.Feature) ?? false;
            var installed = package?.versions.installed;
            var removable = displayVersion?.HasTag(PackageTag.Removable) ?? false;
            var custom = displayVersion?.HasTag(PackageTag.Custom) ?? false;
            var visibleFlag = feature && !custom && installed != null && installed == displayVersion && removable;
            if (visibleFlag)
            {
                var inProgress = m_PackageDatabase.IsUninstallInProgress(package);
                resetButton.text = GetButtonText(PackageAction.Reset, inProgress);

                var customizedDependencies = m_PackageDatabase.GetCustomizedDependencies(displayVersion);
                if (customizedDependencies.Any())
                {
                    var oneDependencyIsInDevelopment = customizedDependencies.Any(p => p.versions.installed?.HasTag(PackageTag.Custom) ?? false);
                    var disableIfOneDependencyIsInDevelopment = new ButtonDisableCondition(oneDependencyIsInDevelopment,
                        string.Format(L10n.Tr("You cannot reset this {0} because one of its included packages is customized. You must remove them manually. See the list of packages in the {0} for more information."), package.GetDescriptor()));

                    var oneDependencyIsDirectAndMatchManifestVersion = customizedDependencies.Any(p => (p.versions.installed?.isDirectDependency ?? false) &&
                        p.versions.installed?.versionString == p.versions.installed?.versionInManifest);
                    var disableIfOneDependencyVersionIsOverridden = new ButtonDisableCondition(!oneDependencyIsDirectAndMatchManifestVersion,
                        string.Format(L10n.Tr("You cannot reset this {0} because one of its included packages has changed version. See the list of packages in the {0} for more information."), package.GetDescriptor()));

                    RefreshButtonStatusAndTooltip(resetButton, PackageAction.Reset,
                        disableIfInstallOrUninstallInProgress,
                        disableIfCompiling,
                        disableIfOneDependencyIsInDevelopment,
                        disableIfOneDependencyVersionIsOverridden);
                }
                else
                    visibleFlag = false;
            }

            UIUtils.SetElementDisplay(resetButton, visibleFlag);
        }

        private void RefreshDownloadStatesButtons()
        {
            var downloadable = displayVersion?.HasTag(PackageTag.Downloadable) ?? false;
            if (downloadable)
            {
                var operation = m_AssetStoreDownloadManager.GetDownloadOperation(displayVersion.packageUniqueId);

                var isResumeRequested = operation?.state == DownloadState.ResumeRequested;
                var showResumeButton = operation?.state == DownloadState.Paused || isResumeRequested;
                UIUtils.SetElementDisplay(resumeButton, showResumeButton);
                if (showResumeButton)
                {
                    RefreshButtonStatusAndTooltip(resumeButton, PackageAction.Resume,
                        new ButtonDisableCondition(isResumeRequested,
                            L10n.Tr("The resume request has been sent. Please wait for the download to resume.")),
                        disableIfCompiling);
                }

                var isPausing = operation?.state == DownloadState.Pausing;
                var showPauseButton = operation?.isInProgress == true || isPausing;
                UIUtils.SetElementDisplay(pauseButton, showPauseButton);
                if (showPauseButton)
                {
                    RefreshButtonStatusAndTooltip(pauseButton, PackageAction.Pause,
                        new ButtonDisableCondition(isPausing,
                            L10n.Tr("The pause request has been sent. Please wait for the download to pause.")),
                        disableIfCompiling);
                }

                var showCancelButton = showPauseButton || showResumeButton;
                UIUtils.SetElementDisplay(cancelButton, showCancelButton);
                if (showCancelButton)
                {
                    RefreshButtonStatusAndTooltip(cancelButton, PackageAction.Cancel,
                        new ButtonDisableCondition(isResumeRequested,
                            L10n.Tr("A resume request has been sent. You cannot cancel this download until it is resumed.")),
                        disableIfCompiling);
                }

                var state = package.state;
                var isAvailableOnDisk = displayVersion?.isAvailableOnDisk ?? false;
                var packageDisabled = displayVersion.HasTag(PackageTag.Disabled);
                var hasUpdateAvailable = state == PackageState.UpdateAvailable;
                var isLatestVersionOnDisk = isAvailableOnDisk && !hasUpdateAvailable;
                var isDownloadRequested = operation?.state == DownloadState.DownloadRequested;
                var showDownloadButton = !isLatestVersionOnDisk && (isDownloadRequested || operation == null || !showCancelButton);
                UIUtils.SetElementDisplay(downloadButton, showDownloadButton);
                if (showDownloadButton)
                {
                    var action = hasUpdateAvailable ? PackageAction.Upgrade : PackageAction.Download;
                    downloadButton.text = GetButtonText(action);

                    RefreshButtonStatusAndTooltip(downloadButton, action,
                        new ButtonDisableCondition(isDownloadRequested,
                            L10n.Tr("The download request has been sent. Please wait for the download to start.")),
                        new ButtonDisableCondition(packageDisabled,
                            L10n.Tr("This package is no longer available and can not be downloaded anymore.")),
                        new ButtonDisableCondition(!m_Application.isInternetReachable,
                            L10n.Tr("You need to restore your network connection to perform this action.")),
                        disableIfCompiling);
                }

                var localInfo = m_AssetStoreCache.GetLocalInfo(displayVersion.packageUniqueId);
                var hasDowngradeAvailable = m_AssetStoreCache.GetUpdateInfo(localInfo?.uploadId)?.canDowngrade == true;
                var showDowngradeButton = isLatestVersionOnDisk && hasDowngradeAvailable && (isDownloadRequested || operation == null || !showCancelButton);
                UIUtils.SetElementDisplay(downgradeButton, showDowngradeButton);
                if (showDowngradeButton)
                {
                    var action = PackageAction.Downgrade;
                    downgradeButton.text = GetButtonText(action);
                    RefreshButtonStatusAndTooltip(downgradeButton, action,
                        new ButtonDisableCondition(isDownloadRequested,
                            L10n.Tr("The download request has been sent. Please wait for the download to start.")),
                        new ButtonDisableCondition(packageDisabled,
                            L10n.Tr("This package is no longer available and can not be downloaded anymore.")),
                        new ButtonDisableCondition(!m_Application.isInternetReachable,
                            L10n.Tr("You need to restore your network connection to perform this action.")),
                        disableIfCompiling);
                }

                var showReDownloadButton = isLatestVersionOnDisk && !hasDowngradeAvailable && (isDownloadRequested || operation == null || !showCancelButton);
                UIUtils.SetElementDisplay(redownloadButton, showReDownloadButton);
                if (showReDownloadButton)
                {
                    var action = PackageAction.ReDownload;
                    redownloadButton.text = GetButtonText(action);

                    RefreshButtonStatusAndTooltip(redownloadButton, action,
                        new ButtonDisableCondition(isDownloadRequested,
                            L10n.Tr("The re-download request has been sent. Please wait for the download to start.")),
                        new ButtonDisableCondition(packageDisabled,
                            L10n.Tr("This package is no longer available and can not be downloaded anymore.")),
                        new ButtonDisableCondition(!m_Application.isInternetReachable,
                            L10n.Tr("You need to restore your network connection to perform this action.")),
                        disableIfCompiling);
                }

                var importable = displayVersion?.HasTag(PackageTag.Importable) ?? false;
                var showImportButton = !showCancelButton && importable && isAvailableOnDisk && package.state != PackageState.InProgress;
                UIUtils.SetElementDisplay(importButton, showImportButton);
                if (showImportButton)
                {
                    importButton.text = GetButtonText(PackageAction.Import);
                    RefreshButtonStatusAndTooltip(importButton, PackageAction.Import,
                        new ButtonDisableCondition(displayVersion.HasTag(PackageTag.Disabled),
                            L10n.Tr("This package is no longer available and can not be imported anymore.")),
                        disableIfCompiling);
                }

                downloadProgress.UpdateProgress(operation);
            }
            else
            {
                UIUtils.SetElementDisplay(downloadButton, false);
                UIUtils.SetElementDisplay(downgradeButton, false);
                UIUtils.SetElementDisplay(redownloadButton, false);
                UIUtils.SetElementDisplay(cancelButton, false);
                UIUtils.SetElementDisplay(pauseButton, false);
                UIUtils.SetElementDisplay(resumeButton, false);
                UIUtils.SetElementDisplay(downloadProgress, false);
                UIUtils.SetElementDisplay(importButton, false);
            }
        }

        private void RefreshUnlockButton()
        {
            var visualState = m_PageManager.GetVisualState(package);
            UIUtils.SetElementDisplay(unlockButton, visualState?.isLocked == true);
        }

        private class ButtonDisableCondition
        {
            public bool value { get; set; }
            public string tooltip { get; set; }

            public ButtonDisableCondition(bool value, string tooltip)
            {
                this.value = value;
                this.tooltip = tooltip;
            }
        }

        private ButtonDisableCondition disableIfCompiling =>
            new ButtonDisableCondition(m_Application.isCompiling, L10n.Tr("You need to wait until the compilation is finished to perform this action."));
        private ButtonDisableCondition disableIfInstallOrUninstallInProgress =>
            new ButtonDisableCondition(m_PackageDatabase.isInstallOrUninstallInProgress, L10n.Tr("You need to wait until other install or uninstall operations are finished to perform this action."));
        private ButtonDisableCondition disableIfNoNetwork =>
            new ButtonDisableCondition(!m_Application.isInternetReachable, L10n.Tr("You need to be online."));
        private ButtonDisableCondition disableIfEntitlementsError =>
            new ButtonDisableCondition(package?.hasEntitlementsError ?? false, L10n.Tr("You need to sign in with a licensed account to perform this action."));

        private void RefreshButtonStatusAndTooltip(VisualElement button, PackageAction action, params ButtonDisableCondition[] disableConditions)
        {
            foreach (var condition in disableConditions)
            {
                if (condition.value)
                {
                    button.SetEnabled(false);
                    // We set the tooltip on the parent (container) of the button rather than the button itself
                    // because when a VisualElement is disabled, tooltips won't show on them
                    button.parent.tooltip = condition.tooltip;
                    return;
                }
            }
            button.SetEnabled(true);
            button.parent.tooltip = GetButtonTooltip(action);
        }

        private void RefreshButtonStatusAndTooltip(VisualElement button, params ButtonDisableCondition[] disableConditions)
        {
            RefreshButtonStatusAndTooltip(button, PackageAction.None, disableConditions);
        }

        internal static string GetButtonText(PackageAction action, bool inProgress = false, SemVersion? version = null)
        {
            var actionText = inProgress ? k_PackageActionInProgressVerbs[(int)action] : k_PackageActionVerbs[(int)action];
            return version == null ? actionText : $"{actionText} {version}";
        }

        internal string GetButtonTooltip(PackageAction action)
        {
            if (action == PackageAction.Downgrade)
            {
                var localInfo = m_AssetStoreCache.GetLocalInfo(displayVersion.packageUniqueId);
                if (localInfo == null)
                    return string.Empty;
                return string.Format(""/*AssetStorePackage.k_IncompatibleWarningMessage*/, localInfo.supportedVersion);
            }
            return string.Format(k_PackageActionTooltips[(int)action], package.GetDescriptor());
        }

        private void UpdateClick()
        {
            var installedVersion = package.versions.installed;
            if (installedVersion?.HasTag(PackageTag.Git) == true)
            {
                var packageInfo = m_UpmCache.GetBestMatchPackageInfo(installedVersion.name, true);
                if (m_PackageDatabase.Install(packageInfo.packageId))
                    RefreshPackageActionButtons();
                return;
            }

            var targetVersion = this.targetVersion;
            if (installedVersion != null && !installedVersion.isDirectDependency && installedVersion != targetVersion)
            {
                // if the installed version is being used by a Feature Set show the more specific
                //  Feature Set dialog instead of the generic one
                if (featureSetDependents.Any())
                {
                    var message = string.Format(L10n.Tr("Changing a {0} that is part of a feature can lead to errors. Are you sure you want to proceed?"), package.GetDescriptor());
                    if (!m_Application.DisplayDialog(L10n.Tr("Warning"), message, L10n.Tr("Yes"), L10n.Tr("No")))
                        return;
                }
                else
                {
                    var message = L10n.Tr("This version of the package is being used by other packages. Upgrading a different version might break your project. Are you sure you want to continue?");
                    if (!m_Application.DisplayDialog(L10n.Tr("Unity Package Manager"), message, L10n.Tr("Yes"), L10n.Tr("No")))
                        return;
                }
            }

            IPackage[] packageToUninstall = null;
            if (targetVersion.HasTag(PackageTag.Feature))
            {
                var customizedDependencies = m_PackageDatabase.GetCustomizedDependencies(targetVersion, true);
                if (customizedDependencies.Any())
                {
                    var packageNameAndVersions = string.Join("\n\u2022 ",
                        customizedDependencies.Select(package => $"{package.displayName} - {package.versions.lifecycleVersion.version}").ToArray());

                    var message = customizedDependencies.Length == 1 ?
                        string.Format(
                        L10n.Tr("This {0} includes a package version that is different from what's already installed. Would you like to reset the following package to the required version?\n\u2022 {1}"),
                        package.GetDescriptor(), packageNameAndVersions) :
                        string.Format(
                        L10n.Tr("This {0} includes package versions that are different from what are already installed. Would you like to reset the following packages to the required versions?\n\u2022 {1}"),
                        package.GetDescriptor(), packageNameAndVersions);

                    var result = m_Application.DisplayDialogComplex(L10n.Tr("Unity Package Manager"), message, L10n.Tr("Install and Reset"), L10n.Tr("Cancel"), L10n.Tr("Install Only"));
                    if (result == 1) // Cancel
                        return;
                    if (result == 0) // Install and reset
                        packageToUninstall = customizedDependencies;
                }
            }

            if (packageToUninstall?.Any() == true)
            {
                m_PackageDatabase.InstallAndResetDependencies(targetVersion, packageToUninstall);

                PackageManagerWindowAnalytics.SendEvent("installAndReset", targetVersion?.uniqueId);
            }
            else
            {
                m_PackageDatabase.Install(targetVersion);

                var installType = installedVersion == null ? "New" : "Update";
                var installRecommended = package.versions.recommended == targetVersion ? "Recommended" : "NonRecommended";
                var eventName = $"install{installType}{installRecommended}";
                PackageManagerWindowAnalytics.SendEvent(eventName, targetVersion?.uniqueId);
            }
            RefreshPackageActionButtons();
        }

        private void RemoveClick()
        {
            if (displayVersion.HasTag(PackageTag.Custom))
            {
                if (!m_Application.DisplayDialog(L10n.Tr("Unity Package Manager"), L10n.Tr("You will lose all your changes (if any) if you delete a package in development. Are you sure?"), L10n.Tr("Yes"), L10n.Tr("No")))
                    return;

                m_PackageDatabase.RemoveEmbedded(package);
                RefreshPackageActionButtons();

                PackageManagerWindowAnalytics.SendEvent("removeEmbedded", displayVersion.uniqueId);
                return;
            }

            var result = 0;
            if (displayVersion.HasTag(PackageTag.BuiltIn))
            {
                if (!m_PackageManagerPrefs.skipDisableConfirmation)
                {
                    result = m_Application.DisplayDialogComplex(L10n.Tr("Disable Built-In Package"),
                        L10n.Tr("Are you sure you want to disable this built-in package?"),
                        L10n.Tr("Disable"), L10n.Tr("Cancel"), L10n.Tr("Never ask"));
                }
            }
            else
            {
                var isPartOfFeature = m_PackageDatabase.GetFeatureDependents(displayVersion).Any(featureSet => featureSet.isInstalled);
                if (isPartOfFeature || !m_PackageManagerPrefs.skipRemoveConfirmation)
                {
                    var descriptor = package.GetDescriptor();
                    var title = string.Format(L10n.Tr("Removing {0}"), CultureInfo.InvariantCulture.TextInfo.ToTitleCase(descriptor));
                    if (isPartOfFeature)
                    {
                        var message = string.Format(L10n.Tr("Are you sure you want to remove this {0} that is used by at least one installed feature?"), descriptor);
                        var removeIt = m_Application.DisplayDialog(title, message, L10n.Tr("Remove"), L10n.Tr("Cancel"));
                        result = removeIt ? 0 : 1;
                    }
                    else
                    {
                        var message = string.Format(L10n.Tr("Are you sure you want to remove this {0}?"), descriptor);
                        result = m_Application.DisplayDialogComplex(title, message, L10n.Tr("Remove"), L10n.Tr("Cancel"), L10n.Tr("Never ask"));
                    }
                }
            }

            // Cancel
            if (result == 1)
                return;

            // Do not ask again
            if (result == 2)
            {
                if (displayVersion.HasTag(PackageTag.BuiltIn))
                    m_PackageManagerPrefs.skipDisableConfirmation = true;
                else
                    m_PackageManagerPrefs.skipRemoveConfirmation = true;
            }

            // If the user is uninstalling a package that is part of a feature set, lock it after removing from manifest
            // Having this check condition should be more optimal once we implement caching of Feature Set Dependents for each package
            if (m_PackageDatabase.GetFeatureDependents(package.versions.installed)?.Any() == true)
                m_PageManager.SetPackagesUserUnlockedState(new List<string> { package.uniqueId }, false);

            // Remove
            m_PackageDatabase.Uninstall(package);
            RefreshPackageActionButtons();

            PackageManagerWindowAnalytics.SendEvent("uninstall", displayVersion?.uniqueId);
        }

        private void ResetClick()
        {
            if (package?.Is(PackageType.Feature) ?? false)
            {
                var packagesToUninstall = m_PackageDatabase.GetCustomizedDependencies(displayVersion, true);
                if (packagesToUninstall.Any())
                {
                    var packageNameAndVersions = string.Join("\n\u2022 ",
                        packagesToUninstall.Select(package => $"{package.displayName} - {package.versions.lifecycleVersion.version}").ToArray());
                    var message = packagesToUninstall.Length == 1 ?
                        string.Format(
                        L10n.Tr("Are you sure you want to reset this {0}?\nThe following included package will reset to the required version:\n\u2022 {1}"),
                        package.GetDescriptor(), packageNameAndVersions) :
                        string.Format(
                        L10n.Tr("Are you sure you want to reset this {0}?\nThe following included packages will reset to their required versions:\n\u2022 {1}"),
                        package.GetDescriptor(), packageNameAndVersions);

                    if (!m_Application.DisplayDialog(L10n.Tr("Unity Package Manager"), message, L10n.Tr("Continue"), L10n.Tr("Cancel")))
                        return;

                    m_PageManager.SetPackagesUserUnlockedState(packagesToUninstall.Select(p => p.uniqueId), false);
                    m_PackageDatabase.ResetDependencies(displayVersion, packagesToUninstall);

                    RefreshPackageActionButtons();

                    PackageManagerWindowAnalytics.SendEvent("reset", displayVersion?.uniqueId);
                }
            }
        }

        private void ImportClick()
        {
            m_PackageDatabase.Import(package);
            RefreshDownloadStatesButtons();

            PackageManagerWindowAnalytics.SendEvent("import", package.uniqueId);
        }

        private void DownloadClick()
        {
            DownloadClickInternal();
        }

        private void DowngradeClick()
        {
            DownloadClickInternal("startDownloadDowngrade");
        }

        private void ReDownloadClick()
        {
            DownloadClickInternal("startReDownload");
        }

        private void DownloadClickInternal(string eventName = null)
        {
            var downloadInProgress = m_PackageDatabase.IsDownloadInProgress(displayVersion);
            if (!downloadInProgress && !m_Application.isInternetReachable)
                return;

            var canDownload = m_PackageDatabase.Download(package);
            RefreshDownloadStatesButtons();

            if (canDownload)
            {
                var operation = m_AssetStoreDownloadManager.GetDownloadOperation(displayVersion.packageUniqueId);
                downloadProgress.UpdateProgress(operation);

                if (string.IsNullOrEmpty(eventName))
                    eventName = package.state == PackageState.UpdateAvailable ? "startDownloadUpdate" : "startDownloadNew";
                PackageManagerWindowAnalytics.SendEvent(eventName, package.uniqueId);
            }
        }

        private void CancelClick()
        {
            m_PackageDatabase.AbortDownload(package);
            RefreshDownloadStatesButtons();

            PackageManagerWindowAnalytics.SendEvent("abortDownload", package.uniqueId);
        }

        private void PauseClick()
        {
            m_PackageDatabase.PauseDownload(package);
            RefreshDownloadStatesButtons();
            PackageManagerWindowAnalytics.SendEvent("pauseDownload", package.uniqueId);
        }

        private void ResumeClick()
        {
            if (!m_Application.isInternetReachable)
                return;

            m_PackageDatabase.ResumeDownload(package);
            RefreshDownloadStatesButtons();
            PackageManagerWindowAnalytics.SendEvent("resumeDownload", package.uniqueId);
        }

        private void UnlockClick()
        {
            m_PageManager.SetPackagesUserUnlockedState(new List<string> { package.uniqueId }, true);
            RefreshContent();
            PackageManagerWindowAnalytics.SendEvent("unlock", package.uniqueId);
        }

        private VisualElementCache cache { get; set; }

        private InProgressView inProgressView => cache.Get<InProgressView>("inProgressView");
        private PackageDetailsHeader header => cache.Get<PackageDetailsHeader>("detailsHeader");
        private PackageDetailsBody body => cache.Get<PackageDetailsBody>("detailsBody");

        internal Alert detailError { get { return cache.Get<Alert>("detailError"); } }
        private ScrollView detailScrollView { get { return cache.Get<ScrollView>("detailScrollView"); } }
        private VisualElement detail { get { return cache.Get<VisualElement>("detail"); } }

        private VisualElement customContainer { get { return cache.Get<VisualElement>("detailCustomContainer"); } }
        internal VisualElement extensionContainer { get { return cache.Get<VisualElement>("detailExtensionContainer"); } }

        internal VisualElement packageToolbarContainer { get { return cache.Get<VisualElement>("toolbarContainer"); } }
        internal VisualElement packageToolbarMainContainer { get { return cache.Get<VisualElement>("toolbarMainContainer"); } }
        private VisualElement packageToolbarLeftArea { get { return cache.Get<VisualElement>("leftItems"); } }
        internal VisualElement toolbarExtensions { get { return cache.Get<VisualElement>("extensionItems"); } }
        internal Button unlockButton { get { return cache.Get<Button>("unlock"); } }
        internal Button updateButton { get { return cache.Get<Button>("update"); } }
        internal Button removeButton { get { return cache.Get<Button>("remove"); } }
        internal DropdownButton resetButton => cache.Get<DropdownButton>("reset");
        internal Button importButton { get { return cache.Get<Button>("import"); } }
        internal Button downloadButton { get { return cache.Get<Button>("download"); } }
        internal DropdownButton downgradeButton { get { return cache.Get<DropdownButton>("downgrade"); } }
        internal Button redownloadButton { get { return cache.Get<Button>("redownload"); } }
        private Button cancelButton { get { return cache.Get<Button>("cancel"); } }
        internal Button pauseButton { get { return cache.Get<Button>("pause"); } }
        internal Button resumeButton { get { return cache.Get<Button>("resume"); } }
        private Button signInButton { get { return cache.Get<Button>("signIn"); } }
        private ProgressBar downloadProgress { get { return cache.Get<ProgressBar>("downloadProgress"); } }

        internal VisualElement packageToolbarErrorContainer { get { return cache.Get<VisualElement>("toolbarErrorContainer"); } }
        private Label errorMessage { get { return cache.Get<Label>("message"); } }
        private Label errorStatus { get { return cache.Get<Label>("state"); } }
        private Button okButton { get { return cache.Get<Button>("ok"); } }

        private FeatureDependencies featureDependencies => cache.Get<FeatureDependencies>("featureDependencies");
    }
}
