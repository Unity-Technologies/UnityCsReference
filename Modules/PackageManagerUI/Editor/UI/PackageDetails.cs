// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetails : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageDetails> {}

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

        internal enum PackageAction
        {
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
            Import
        }

        internal static readonly string[] k_PackageActionVerbs =
        {
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
            L10n.Tr("Import")
        };

        private static readonly string[] k_PackageActionInProgressVerbs =
        {
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
            L10n.Tr("Import")
        };

        internal static readonly string[] k_PackageActionTooltips =
        {
            L10n.Tr("Click to install this package into your project."),
            L10n.Tr("Click to remove this package from your project."),
            L10n.Tr("Click to update this package to the specified version."),
            L10n.Tr("Enable the use of this package in your project."),
            L10n.Tr("Disable the use of this package in your project."),
            L10n.Tr("Click to download this package for later use."),
            L10n.Tr("Click to download the latest version of this package."),
            L10n.Tr("Click to pause the download of this package."),
            L10n.Tr("Click to resume the download of this package."),
            L10n.Tr("Click to cancel the download of this package."),
            L10n.Tr("Click to import assets from the package into your project.")
        };

        private ResourceLoader m_ResourceLoader;
        private ExtensionManager m_ExtensionManager;
        private ApplicationProxy m_Application;
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
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

            updateButton.clickable.clicked += UpdateClick;
            removeButton.clickable.clicked += RemoveClick;
            importButton.clickable.clicked += ImportClick;
            downloadButton.clickable.clicked += DownloadClick;
            cancelButton.clickable.clicked += CancelClick;
            pauseButton.clickable.clicked += PauseClick;
            resumeButton.clickable.clicked += ResumeClick;

            okButton.clickable.clicked += ClearError;

            signInButton.clickable.clicked += m_UnityConnectProxy.ShowLogin;

            detailScrollView.verticalScroller.valueChanged += OnDetailScroll;

            errorMessage.ShowTextTooltipOnSizeChange();

            RefreshContent();
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

            m_PackageDatabase.onPackagesChanged += (added, removed, preUpdate, postUpdate) => OnPackagesUpdated(postUpdate);

            m_PackageDatabase.onPackageProgressUpdate += OnPackageProgressUpdate;

            m_AssetStoreDownloadManager.onDownloadProgress += UpdateDownloadProgressBar;
            m_AssetStoreDownloadManager.onDownloadFinalized += StopDownloadProgressBar;
            m_AssetStoreDownloadManager.onDownloadPaused += PauseDownloadProgressBar;

            m_PageManager.onSelectionChanged += OnSelectionChanged;

            m_UnityConnectProxy.onUserLoginStateChange += OnUserLoginStateChange;

            RefreshUI(m_PageManager.GetSelectedVersion());
        }

        public void OnDisable()
        {
            body.OnDisable();

            m_Application.onFinishCompiling -= RefreshPackageActionButtons;
            m_Application.onInternetReachabilityChange -= OnInternetReachabilityChange;

            m_PackageDatabase.onPackageProgressUpdate -= OnPackageProgressUpdate;

            m_AssetStoreDownloadManager.onDownloadProgress -= UpdateDownloadProgressBar;
            m_AssetStoreDownloadManager.onDownloadFinalized -= StopDownloadProgressBar;
            m_AssetStoreDownloadManager.onDownloadPaused -= PauseDownloadProgressBar;

            m_PageManager.onSelectionChanged -= OnSelectionChanged;

            m_UnityConnectProxy.onUserLoginStateChange -= OnUserLoginStateChange;
        }

        private void OnInternetReachabilityChange(bool value)
        {
            RefreshButtonStatusAndTooltip(resumeButton, PackageAction.Resume,
                new ButtonDisableCondition(!value, L10n.Tr("You need to restore your network connection to perform this action.")));

            RefreshButtonStatusAndTooltip(downloadButton, PackageAction.Download,
                new ButtonDisableCondition(!value, L10n.Tr("You need to restore your network connection to perform this action.")));

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
            var packageInfo = version?.packageInfo;
            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var extension in PackageManagerExtensions.Extensions)
                    extension.OnPackageSelectionChange(packageInfo);
            });

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
                header.Refresh(package, displayVersion);
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

        internal void OnPackagesUpdated(IEnumerable<IPackage> updatedPackages)
        {
            var packageUniqudId = package?.uniqueId;
            if (string.IsNullOrEmpty(packageUniqudId) || !updatedPackages.Any())
                return;

            var updatedPackage = updatedPackages.FirstOrDefault(p => p.uniqueId == packageUniqudId);
            if (updatedPackage != null)
            {
                var updatedVersion = displayVersion == null ? null : updatedPackage.versions.FirstOrDefault(v => v.uniqueId == displayVersion.uniqueId);
                SetPackage(updatedPackage, updatedVersion);

                if (updatedVersion?.isFullyFetched ?? false)
                    SetEnabled(true);
            }
        }

        private void RefreshErrorDisplay()
        {
            var error = displayVersion?.errors?.FirstOrDefault(e => !e.HasAttribute(UIError.Attribute.IsClearable)) ?? package?.errors?.FirstOrDefault(e => !e.HasAttribute(UIError.Attribute.IsClearable));
            var operationError = displayVersion?.errors?.FirstOrDefault(e  => e.HasAttribute(UIError.Attribute.IsClearable)) ?? package?.errors?.FirstOrDefault(e => e.HasAttribute(UIError.Attribute.IsClearable));

            if (error == null && operationError == null)
            {
                ClearError();
                detailError.ClearError();
                return;
            }

            if (error == null)
                detailError.ClearError();
            else
                detailError.SetError(error);

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
            RefreshAddButton();
            RefreshRemoveButton();
            RefreshToolbarExtensionsStatusAndTooltip();
        }

        private void RefreshAddButton()
        {
            var installed = package?.versions.installed;
            var targetVersion = this.targetVersion;
            var installable = targetVersion?.HasTag(PackageTag.Installable) ?? false;
            var visibleFlag = installed?.HasTag(PackageTag.VersionLocked) != true && displayVersion != null && installable && installed != targetVersion && !isRequestedButOverriddenVersion;
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
            UIUtils.SetElementDisplay(updateButton, visibleFlag);
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
                    L10n.Tr("You cannot remove this package because at least one other installed package depends on it. See dependencies for more details."));
                RefreshButtonStatusAndTooltip(removeButton, action, disableIfUsedByOthers, disableIfInstallOrUninstallInProgress, disableIfCompiling);
            }
            UIUtils.SetElementDisplay(removeButton, visibleFlag);
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
                UIUtils.SetElementDisplay(cancelButton, false);
                UIUtils.SetElementDisplay(pauseButton, false);
                UIUtils.SetElementDisplay(resumeButton, false);
                UIUtils.SetElementDisplay(downloadProgress, false);
                UIUtils.SetElementDisplay(importButton, false);
            }
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

        private static void RefreshButtonStatusAndTooltip(Button button, PackageAction action, params ButtonDisableCondition[] disableConditions)
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

        private static void RefreshButtonStatusAndTooltip(Button button, params ButtonDisableCondition[] disableConditions)
        {
            foreach (var condition in disableConditions)
            {
                if (condition.value)
                {
                    button.SetEnabled(false);
                    button.parent.tooltip = condition.tooltip;
                    return;
                }
            }
            button.SetEnabled(true);
            button.parent.tooltip = string.Empty;
        }

        internal static string GetButtonText(PackageAction action, bool inProgress = false, SemVersion? version = null)
        {
            var actionText = inProgress ? k_PackageActionInProgressVerbs[(int)action] : k_PackageActionVerbs[(int)action];
            return version == null ? actionText : $"{actionText} {version}";
        }

        internal static string GetButtonTooltip(PackageAction action)
        {
            return k_PackageActionTooltips[(int)action];
        }

        private void UpdateClick()
        {
            // dissuade users from updating by showing a warning message
            var installedVersion = package.versions.installed;
            var targetVersion = this.targetVersion;
            if (installedVersion != null && !installedVersion.isDirectDependency && installedVersion != targetVersion)
            {
                var message = L10n.Tr("This version of the package is being used by other packages. Upgrading a different version might break your project. Are you sure you want to continue?");
                if (!EditorUtility.DisplayDialog(L10n.Tr("Unity Package Manager"), message, L10n.Tr("Yes"), L10n.Tr("No")))
                    return;
            }

            m_PackageDatabase.Install(targetVersion);
            RefreshPackageActionButtons();

            var installType = installedVersion == null ? "New" : "Update";
            var installRecommended = package.versions.recommended == targetVersion ? "Recommended" : "NonRecommended";
            var eventName = $"install{installType}{installRecommended}";
            PackageManagerWindowAnalytics.SendEvent(eventName, targetVersion?.uniqueId);
        }

        private void RemoveClick()
        {
            if (displayVersion.HasTag(PackageTag.Custom))
            {
                if (!EditorUtility.DisplayDialog(L10n.Tr("Unity Package Manager"), L10n.Tr("You will lose all your changes (if any) if you delete a package in development. Are you sure?"), L10n.Tr("Yes"), L10n.Tr("No")))
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
                    result = EditorUtility.DisplayDialogComplex(L10n.Tr("Disable Built-In Package"),
                        L10n.Tr("Are you sure you want to disable this built-in package?"),
                        L10n.Tr("Disable"), L10n.Tr("Cancel"), L10n.Tr("Never ask"));
                }
            }
            else
            {
                if (!m_PackageManagerPrefs.skipRemoveConfirmation)
                {
                    result = EditorUtility.DisplayDialogComplex(L10n.Tr("Removing Package"),
                        L10n.Tr("Are you sure you want to remove this package?"),
                        L10n.Tr("Remove"), L10n.Tr("Cancel"), L10n.Tr("Never ask"));
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

            // Remove
            m_PackageDatabase.Uninstall(package);
            RefreshPackageActionButtons();

            PackageManagerWindowAnalytics.SendEvent("uninstall", displayVersion?.uniqueId);
        }

        private void ImportClick()
        {
            m_PackageDatabase.Import(package);
            RefreshDownloadStatesButtons();

            PackageManagerWindowAnalytics.SendEvent("import", package.uniqueId);
        }

        private void DownloadClick()
        {
            var downloadInProgress = m_PackageDatabase.IsDownloadInProgress(displayVersion);
            if (!downloadInProgress && !m_Application.isInternetReachable)
                return;

            var isUpdate = package.state == PackageState.UpdateAvailable;

            m_PackageDatabase.Download(package);
            RefreshDownloadStatesButtons();

            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(displayVersion.packageUniqueId);
            downloadProgress.UpdateProgress(operation);

            var eventName = isUpdate ? "startDownloadUpdate" : "startDownloadNew";
            PackageManagerWindowAnalytics.SendEvent(eventName, package.uniqueId);
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
        internal Button updateButton { get { return cache.Get<Button>("update"); } }
        internal Button removeButton { get { return cache.Get<Button>("remove"); } }
        internal Button importButton { get { return cache.Get<Button>("import"); } }
        internal Button downloadButton { get { return cache.Get<Button>("download"); } }
        private Button cancelButton { get { return cache.Get<Button>("cancel"); } }
        internal Button pauseButton { get { return cache.Get<Button>("pause"); } }
        internal Button resumeButton { get { return cache.Get<Button>("resume"); } }
        private Button signInButton { get { return cache.Get<Button>("signIn"); } }
        private ProgressBar downloadProgress { get { return cache.Get<ProgressBar>("downloadProgress"); } }

        internal VisualElement packageToolbarErrorContainer { get { return cache.Get<VisualElement>("toolbarErrorContainer"); } }
        private Label errorMessage { get { return cache.Get<Label>("message"); } }
        private Label errorStatus { get { return cache.Get<Label>("state"); } }
        private Button okButton { get { return cache.Get<Button>("ok"); } }
    }
}
