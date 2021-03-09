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

namespace UnityEditor.PackageManager.UI
{
    internal abstract class PackageDetails : VisualElement
    {
        internal abstract IAlert detailError { get; }
        internal abstract void RefreshPackageActionButtons();
    }
}

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetails : UI.PackageDetails
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

        private bool isRequestedButOverriddenVersion =>
            !string.IsNullOrEmpty(displayVersion?.versionString) &&
            displayVersion.versionString == package?.versions.primary.packageInfo?.projectDependenciesEntry;

        private const string k_EmptyDescriptionClass = "empty";

        internal enum InfoBoxState
        {
            preRelease,
            experimental,
            releaseCandidate,
            scopedRegistry
        }

        private string InfoBoxUrl => $"https://docs.unity3d.com/{m_Application?.shortUnityVersion}";

        private static readonly string[] k_InfoBoxReadMoreUrl =
        {
            "/Documentation/Manual/pack-prerelease.html",
            "/Documentation/Manual/pack-experimental.html",
            "/Documentation/Manual/pack-releasecandidate.html",
            "/Documentation/Manual/upm-scoped.html"
        };

        private static readonly string[] k_InfoBoxReadMoreText =
        {
            L10n.Tr("Pre-release packages are in the process of becoming stable and will be available as production-ready by the end of this LTS release. We recommend using these only for testing purposes and to give us direct feedback until then."),
            L10n.Tr("Experimental packages are new packages or experiments on mature packages in the early stages of development. Experimental packages are not supported by Unity."),
            L10n.Tr("Release Candidate (RC) versions of a package will transition to Released with the current editor release. RCs are supported by Unity"),
            L10n.Tr("This package is hosted on a Scoped Registry.")
        };

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

        internal static readonly PackageTag[] k_VisibleTags =
        {
            PackageTag.Release,
            PackageTag.Custom,
            PackageTag.Local,
            PackageTag.Git,
            PackageTag.Deprecated,
            PackageTag.Disabled,
            PackageTag.PreRelease,
            PackageTag.Experimental,
            PackageTag.ReleaseCandidate
        };

        internal bool descriptionExpanded => m_DescriptionExpanded;
        private bool m_DescriptionExpanded;

        private ResourceLoader m_ResourceLoader;
        private ExtensionManager m_ExtensionManager;
        private ApplicationProxy m_Application;
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private UnityConnectProxy m_UnityConnectProxy;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_ExtensionManager = container.Resolve<ExtensionManager>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_AssetStoreDownloadManager = container.Resolve<AssetStoreDownloadManager>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
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

            detailAuthorLink.clickable.clicked += AuthorClick;
            updateButton.clickable.clicked += UpdateClick;
            removeButton.clickable.clicked += RemoveClick;
            importButton.clickable.clicked += ImportClick;
            downloadButton.clickable.clicked += DownloadClick;
            cancelButton.clickable.clicked += CancelClick;
            pauseButton.clickable.clicked += PauseClick;
            resumeButton.clickable.clicked += ResumeClick;
            detailDescMore.clickable.clicked += DescMoreClick;
            detailDescLess.clickable.clicked += DescLessClick;
            okButton.clickable.clicked += ClearError;

            signInButton.clickable.clicked += m_UnityConnectProxy.ShowLogin;

            detailDesc.RegisterCallback<GeometryChangedEvent>(DescriptionGeometryChangeEvent);

            scopedRegistryInfoBox.Q<Button>().clickable.clicked += OnInfoBoxClickMore;
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
            m_Application.onFinishCompiling += RefreshPackageActionButtons;
            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;

            m_PackageDatabase.onPackagesChanged += (added, removed, preUpdate, postUpdate) => OnPackagesUpdated(postUpdate);
            m_PackageDatabase.onPackagesChanged += (added, removed, preUpdate, postUpdate) => RefreshDependencies();

            m_PackageDatabase.onPackageProgressUpdate += OnPackageProgressUpdate;

            m_AssetStoreDownloadManager.onDownloadProgress += UpdateDownloadProgressBar;
            m_AssetStoreDownloadManager.onDownloadFinalized += StopDownloadProgressBar;
            m_AssetStoreDownloadManager.onDownloadPaused += PauseDownloadProgressBar;

            m_PageManager.onSelectionChanged += OnSelectionChanged;

            m_SettingsProxy.onEnablePackageDependenciesChanged += (value) => RefreshDependencies();

            m_UnityConnectProxy.onUserLoginStateChange += OnUserLoginStateChange;

            RefreshUI(m_PageManager.GetSelectedVersion());
        }

        public void OnDisable()
        {
            detailsImages.OnDisable();

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

        private void OnInfoBoxClickMore()
        {
            if (displayVersion.HasTag(PackageTag.PreRelease))
                m_Application.OpenURL($"{InfoBoxUrl}{k_InfoBoxReadMoreUrl[(int)InfoBoxState.preRelease]}");
            else if (displayVersion.HasTag(PackageTag.Experimental))
                m_Application.OpenURL($"{InfoBoxUrl}{k_InfoBoxReadMoreUrl[(int)InfoBoxState.experimental]}");
            else if (displayVersion.HasTag(PackageTag.ReleaseCandidate))
                m_Application.OpenURL($"{InfoBoxUrl}{k_InfoBoxReadMoreUrl[(int)InfoBoxState.releaseCandidate]}");
            else if (package.Is(PackageType.ScopedRegistry))
                m_Application.OpenURL($"{InfoBoxUrl}{k_InfoBoxReadMoreUrl[(int)InfoBoxState.scopedRegistry]}");
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

        private void RefreshDependencies()
        {
            dependencies.SetPackageVersion(displayVersion);
        }

        void RefreshExtensions(IPackage package, IPackageVersion version)
        {
            var packageInfo = version?.packageInfo;
            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var extension in PackageManagerExtensions.Extensions)
                    extension.OnPackageSelectionChange(packageInfo);

                foreach (var extension in PackageManagerExtensions.ToolbarExtensions)
                    extension.OnPackageSelectionChange(version, packageToolbarContainer);
            });

            m_ExtensionManager.SendPackageSelectionChangedEvent(package, version);
        }

        private static bool IsDifferentVersionThanRequested(IPackageVersion packageVersion)
        {
            return !string.IsNullOrEmpty(packageVersion?.packageInfo?.projectDependenciesEntry) &&
                !packageVersion.HasTag(PackageTag.Git | PackageTag.Local | PackageTag.Custom) &&
                packageVersion.packageInfo.projectDependenciesEntry != packageVersion.versionString;
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
                detailTitle.SetValueWithoutNotify(displayVersion.displayName);

                RefreshEntitlement();

                detailsLinks.Refresh(package, displayVersion);

                RefreshDescription();

                var versionString = displayVersion.versionString;
                var releaseDateString = displayVersion.publishedDate?.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("en-US"));
                detailVersion.SetValueWithoutNotify(string.IsNullOrEmpty(releaseDateString)
                    ? string.Format(L10n.Tr("Version {0}"), versionString)
                    : string.Format(L10n.Tr("Version {0} - {1}"), versionString, releaseDateString));
                UIUtils.SetElementDisplay(detailVersion, !package.Is(PackageType.BuiltIn) && !string.IsNullOrEmpty(versionString));

                RefreshVersionInfoIcon();

                UIUtils.SetElementDisplay(disabledInfoBox, displayVersion.HasTag(PackageTag.Disabled));

                foreach (var tag in k_VisibleTags)
                    UIUtils.SetElementDisplay(GetTagLabel(tag.ToString()), displayVersion.HasTag(tag));

                var scopedRegistryTagLabel = GetTagLabel("ScopedRegistry");
                if ((displayVersion as UpmPackageVersion)?.isUnityPackage == false && !string.IsNullOrEmpty(displayVersion.version?.Prerelease))
                {
                    scopedRegistryTagLabel.tooltip = displayVersion.version?.Prerelease;
                    scopedRegistryTagLabel.text = displayVersion.version?.Prerelease;
                    UIUtils.SetElementDisplay(scopedRegistryTagLabel, true);
                }
                else
                {
                    UIUtils.SetElementDisplay(scopedRegistryTagLabel, false);
                }

                UIUtils.SetElementDisplay(GetTagLabel(PackageType.AssetStore.ToString()), package.Is(PackageType.AssetStore));

                sampleList.SetPackageVersion(displayVersion);

                RefreshAuthor();
                RefreshRegistry();
                RefreshReleaseDetails();

                UIUtils.SetElementDisplay(customContainer, true);
                UIUtils.SetElementDisplay(extensionContainer, true);
                RefreshExtensions(package, displayVersion);

                RefreshDependencies();

                RefreshSizeAndSupportedUnityVersions();

                detailsImages.Refresh(package);

                RefreshPackageActionButtons();
                RefreshSourcePath();

                // Here Do logic for Download/Cancel/Pause/Resume
                RefreshDownloadStatesButtons();

                RefreshPurchasedDate();
                RefreshLabels();
            }

            // Set visibility
            SetContentVisibility(detailVisible);
            SetEnabled(detailEnabled);
            RefreshErrorDisplay();
        }

        private void DescriptionGeometryChangeEvent(GeometryChangedEvent evt)
        {
            if (package == null || !package.Is(PackageType.AssetStore))
            {
                UIUtils.SetElementDisplay(detailDescMore, false);
                UIUtils.SetElementDisplay(detailDescLess, false);
                return;
            }

            var minTextHeight = (int)detailDesc.MeasureTextSize("|", 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).y*3 + 1;
            var textHeight = (int)detailDesc.MeasureTextSize(detailDesc.text, evt.newRect.width, MeasureMode.AtMost, float.MaxValue, MeasureMode.Undefined).y + 1;
            if (!m_DescriptionExpanded && textHeight > minTextHeight)
            {
                UIUtils.SetElementDisplay(detailDescMore, true);
                UIUtils.SetElementDisplay(detailDescLess, false);
                detailDesc.style.maxHeight = minTextHeight + 4;
                return;
            }

            if (evt.newRect.width > evt.oldRect.width && textHeight <= minTextHeight)
            {
                UIUtils.SetElementDisplay(detailDescMore, false);
                UIUtils.SetElementDisplay(detailDescLess, false);
            }
            else if (m_DescriptionExpanded && evt.newRect.width < evt.oldRect.width && textHeight > minTextHeight)
            {
                UIUtils.SetElementDisplay(detailDescMore, false);
                UIUtils.SetElementDisplay(detailDescLess, true);
            }
        }

        private void RefreshDescription()
        {
            var hasDescription = !string.IsNullOrEmpty(displayVersion.description);
            detailDesc.EnableInClassList(k_EmptyDescriptionClass, !hasDescription);
            detailDesc.style.maxHeight = int.MaxValue;
            detailDesc.SetValueWithoutNotify(hasDescription ? displayVersion.description : L10n.Tr("There is no description for this package."));
            UIUtils.SetElementDisplay(detailDescMore, false);
            UIUtils.SetElementDisplay(detailDescLess, false);
            m_DescriptionExpanded = !package.Is(PackageType.AssetStore);
        }

        private void RefreshAuthor()
        {
            UIUtils.SetElementDisplay(detailAuthorContainer, !string.IsNullOrEmpty(displayVersion.author));
            if (!string.IsNullOrEmpty(displayVersion.author))
            {
                if (!string.IsNullOrEmpty(displayVersion.authorLink))
                {
                    UIUtils.SetElementDisplay(detailAuthorText, false);
                    UIUtils.SetElementDisplay(detailAuthorLink, true);
                    detailAuthorLink.text = displayVersion.author;
                }
                else
                {
                    UIUtils.SetElementDisplay(detailAuthorText, true);
                    UIUtils.SetElementDisplay(detailAuthorLink, false);
                    detailAuthorText.SetValueWithoutNotify(displayVersion.author);
                }
            }
        }

        private void RefreshRegistry()
        {
            var registry = displayVersion.packageInfo?.registry;
            var showRegistry = registry != null;
            UIUtils.SetElementDisplay(detailRegistryContainer, showRegistry);
            if (showRegistry)
            {
                scopedRegistryInfoBox.text = k_InfoBoxReadMoreText[(int)InfoBoxState.scopedRegistry];
                UIUtils.SetElementDisplay(scopedRegistryInfoBox, !registry.isDefault);
                detailRegistryName.text = registry.isDefault ? "Unity" : registry.name;
                detailRegistryName.tooltip = registry.url;
            }
            if (displayVersion.HasTag(PackageTag.Experimental))
            {
                scopedRegistryInfoBox.text = k_InfoBoxReadMoreText[(int)InfoBoxState.experimental];
                UIUtils.SetElementDisplay(scopedRegistryInfoBox, true);
            }
            else if (displayVersion.HasTag(PackageTag.PreRelease))
            {
                scopedRegistryInfoBox.text = k_InfoBoxReadMoreText[(int)InfoBoxState.preRelease];
                UIUtils.SetElementDisplay(scopedRegistryInfoBox, true);
            }
            else if (displayVersion.HasTag(PackageTag.ReleaseCandidate))
            {
                scopedRegistryInfoBox.text = k_InfoBoxReadMoreText[(int)InfoBoxState.releaseCandidate];
                UIUtils.SetElementDisplay(scopedRegistryInfoBox, true);
            }
        }

        private void RefreshLabels()
        {
            detailLabels.Clear();

            if (enabledSelf && package?.labels != null)
            {
                var labels = string.Join(", ", package.labels.ToArray());

                if (!string.IsNullOrEmpty(labels))
                {
                    var label = new SelectableLabel();
                    label.SetValueWithoutNotify(labels);
                    detailLabels.Add(label);
                }
            }

            var hasLabels = detailLabels.Children().Any();
            var isAssetStorePackage = package is AssetStorePackage;

            if (!hasLabels && isAssetStorePackage)
                detailLabels.Add(new Label(L10n.Tr("(None)")));

            UIUtils.SetElementDisplay(detailLabelsContainer, hasLabels || isAssetStorePackage);
        }

        private void RefreshPurchasedDate()
        {
            if (enabledSelf)
            {
                detailPurchasedDate.SetValueWithoutNotify(package?.purchasedTime?.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("en-US")) ?? string.Empty);
            }
            UIUtils.SetElementDisplay(detailPurchasedDateContainer, !string.IsNullOrEmpty(detailPurchasedDate.text));
        }

        private void RefreshReleaseDetails()
        {
            detailReleaseDetails.Clear();

            // If the package details is not enabled, don't update the date yet as we are fetching new information
            if (enabledSelf && package.firstPublishedDate != null)
            {
                detailReleaseDetails.Add(new PackageReleaseDetailsItem($"{displayVersion.versionString}{(displayVersion is AssetStorePackageVersion ? " (Current)" : string.Empty)}",
                    displayVersion.publishedDate, displayVersion.releaseNotes));

                if (package.firstPublishedDate != null)
                    detailReleaseDetails.Add(new PackageReleaseDetailsItem("Original", package.firstPublishedDate, string.Empty));
            }

            UIUtils.SetElementDisplay(detailReleaseDetailsContainer, detailReleaseDetails.Children().Any());
        }

        private void RefreshVersionInfoIcon()
        {
            var isInstalledVersionDifferentThanRequested = IsDifferentVersionThanRequested(package?.versions.installed);
            UIUtils.SetElementDisplay(versionInfoIcon, isInstalledVersionDifferentThanRequested);

            if (!isInstalledVersionDifferentThanRequested)
                return;

            var installedVersionString = package?.versions.installed.versionString;
            if (isRequestedButOverriddenVersion)
                versionInfoIcon.tooltip = string.Format(
                    L10n.Tr("Unity installed version {0} because another package depends on it (version {0} overrides version {1})."),
                    installedVersionString, displayVersion.versionString);
            else if (displayVersion.isInstalled && IsDifferentVersionThanRequested(displayVersion))
                versionInfoIcon.tooltip = L10n.Tr("At least one other package depends on this version of the package.");
            else
                versionInfoIcon.tooltip = string.Format(
                    L10n.Tr("At least one other package depends on version {0} of this package."), installedVersionString);
        }

        private void RefreshEntitlement()
        {
            var showEntitlement = package.hasEntitlements;
            UIUtils.SetElementDisplay(detailEntitlement, showEntitlement);
            detailEntitlement.text = showEntitlement ? "E" : string.Empty;
            detailEntitlement.tooltip = showEntitlement ? L10n.Tr("This is an Entitlement package.") : string.Empty;

            var hasEntitlementsError = package.hasEntitlementsError;
            detailContainer.SetEnabled(!hasEntitlementsError);
            UIUtils.SetElementDisplay(signInButton, hasEntitlementsError && !m_UnityConnectProxy.isUserLoggedIn);
            RefreshButtonStatusAndTooltip(signInButton, disableIfNoNetwork);
        }

        private void RefreshSizeAndSupportedUnityVersions()
        {
            var showSupportedUnityVersions = RefreshSupportedUnityVersions();
            var showSize = RefreshSizeInfo();
            UIUtils.SetElementDisplay(detailSizesAndSupportedVersionsContainer, showSize || showSupportedUnityVersions);
        }

        private bool RefreshSupportedUnityVersions()
        {
            var hasSupportedVersions = (displayVersion.supportedVersions?.Any() == true);
            var supportedVersion = displayVersion.supportedVersions?.FirstOrDefault();

            if (!hasSupportedVersions)
            {
                supportedVersion = displayVersion.supportedVersion;
                hasSupportedVersions = supportedVersion != null;
            }

            UIUtils.SetElementDisplay(detailUnityVersionsContainer, hasSupportedVersions);
            if (hasSupportedVersions)
            {
                detailUnityVersions.SetValueWithoutNotify(string.Format(L10n.Tr("{0} or higher"), supportedVersion));
                var tooltip = supportedVersion.ToString();
                if (displayVersion.supportedVersions != null && displayVersion.supportedVersions.Any())
                {
                    var versions = displayVersion.supportedVersions.Select(version => version.ToString()).ToArray();
                    tooltip = versions.Length == 1 ? versions[0] :
                        string.Format(L10n.Tr("{0} and {1} to improve compatibility with the range of these versions of Unity"), string.Join(", ", versions, 0, versions.Length - 1), versions[versions.Length - 1]);
                }
                detailUnityVersions.tooltip = string.Format(L10n.Tr("Package has been submitted using Unity {0}"), tooltip);
            }
            else
            {
                detailUnityVersions.SetValueWithoutNotify(string.Empty);
                detailUnityVersions.tooltip = string.Empty;
            }

            return hasSupportedVersions;
        }

        private bool RefreshSizeInfo()
        {
            var showSizes = displayVersion.sizes.Any();
            UIUtils.SetElementDisplay(detailSizesContainer, showSizes);
            detailSizes.Clear();

            var sizeInfo = displayVersion.sizes.FirstOrDefault(info => info.supportedUnityVersion == displayVersion.supportedVersion);
            if (sizeInfo == null)
                sizeInfo = displayVersion.sizes.LastOrDefault();

            if (sizeInfo != null)
            {
                var label = new SelectableLabel();
                label.style.whiteSpace = WhiteSpace.Normal;
                label.SetValueWithoutNotify(string.Format(L10n.Tr("Size: {0} (Number of files: {1})"), UIUtils.ConvertToHumanReadableSize(sizeInfo.downloadSize), sizeInfo.assetCount));
                detailSizes.Add(label);
            }

            return showSizes;
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

        internal override void RefreshPackageActionButtons()
        {
            RefreshAddButton();
            RefreshRemoveButton();
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

                var isInstalledAsDependency = installed == displayVersion && (!displayVersion.isDirectDependency || IsDifferentVersionThanRequested(displayVersion));
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

        private void RefreshSourcePath()
        {
            var sourcePath = (displayVersion as UpmPackageVersion)?.sourcePath;
            UIUtils.SetElementDisplay(detailSourcePathContainer, !string.IsNullOrEmpty(sourcePath));

            if (!string.IsNullOrEmpty(sourcePath))
                detailSourcePath.SetValueWithoutNotify(sourcePath);
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

        private void DescMoreClick()
        {
            detailDesc.style.maxHeight = float.MaxValue;
            UIUtils.SetElementDisplay(detailDescMore, false);
            UIUtils.SetElementDisplay(detailDescLess, true);
            m_DescriptionExpanded = true;
        }

        private void DescLessClick()
        {
            detailDesc.style.maxHeight = (int)detailDesc.MeasureTextSize("|", 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).y*3 + 5;
            UIUtils.SetElementDisplay(detailDescMore, true);
            UIUtils.SetElementDisplay(detailDescLess, false);
            m_DescriptionExpanded = false;
        }

        private void AuthorClick()
        {
            var authorLink = displayVersion?.authorLink ?? string.Empty;
            if (!string.IsNullOrEmpty(authorLink))
                m_Application.OpenURL(authorLink);
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

            m_PackageDatabase.Download(package);
            RefreshDownloadStatesButtons();

            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(displayVersion.packageUniqueId);
            downloadProgress.UpdateProgress(operation);


            PackageManagerWindowAnalytics.SendEvent("startDownload", package.uniqueId);
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
        private SelectableLabel detailDesc { get { return cache.Get<SelectableLabel>("detailDesc"); } }
        private Button detailDescMore { get { return cache.Get<Button>("detailDescMore"); } }
        private Button detailDescLess { get { return cache.Get<Button>("detailDescLess"); } }
        internal override IAlert detailError { get { return cache.Get<Alert>("detailError"); } }
        private ScrollView detailScrollView { get { return cache.Get<ScrollView>("detailScrollView"); } }
        private VisualElement detail { get { return cache.Get<VisualElement>("detail"); } }
        private SelectableLabel detailTitle { get { return cache.Get<SelectableLabel>("detailTitle"); } }
        private Label detailEntitlement { get { return cache.Get<Label>("detailEntitlement"); } }
        private SelectableLabel detailVersion { get { return cache.Get<SelectableLabel>("detailVersion"); } }
        private VisualElement versionInfoIcon => cache.Get<VisualElement>("versionInfoIcon");
        private HelpBox disabledInfoBox { get { return cache.Get<HelpBox>("disabledInfoBox"); } }
        private VisualElement detailPurchasedDateContainer { get { return cache.Get<VisualElement>("detailPurchasedDateContainer"); } }
        private SelectableLabel detailPurchasedDate { get { return cache.Get<SelectableLabel>("detailPurchasedDate"); } }
        private VisualElement detailAuthorContainer { get { return cache.Get<VisualElement>("detailAuthorContainer"); } }
        private SelectableLabel detailAuthorText { get { return cache.Get<SelectableLabel>("detailAuthorText"); } }
        private Button detailAuthorLink { get { return cache.Get<Button>("detailAuthorLink"); } }
        private VisualElement detailRegistryContainer { get { return cache.Get<VisualElement>("detailRegistryContainer"); } }
        private HelpBox scopedRegistryInfoBox { get { return cache.Get<HelpBox>("scopedRegistryInfoBox"); } }
        private Label detailRegistryName { get { return cache.Get<Label>("detailRegistryName"); } }
        private VisualElement customContainer { get { return cache.Get<VisualElement>("detailCustomContainer"); } }
        internal VisualElement extensionContainer { get { return cache.Get<VisualElement>("detailExtensionContainer"); } }
        private PackageSampleList sampleList { get { return cache.Get<PackageSampleList>("detailSampleList"); } }
        private PackageDependencies dependencies { get { return cache.Get<PackageDependencies>("detailDependencies"); } }
        internal VisualElement packageToolbarContainer { get { return cache.Get<VisualElement>("toolbarContainer"); } }
        internal VisualElement packageToolbarMainContainer { get { return cache.Get<VisualElement>("toolbarMainContainer"); } }
        private VisualElement packageToolbarLeftArea { get { return cache.Get<VisualElement>("leftItems"); } }
        internal Button updateButton { get { return cache.Get<Button>("update"); } }
        internal Button removeButton { get { return cache.Get<Button>("remove"); } }
        internal Button importButton { get { return cache.Get<Button>("import"); } }
        internal Button downloadButton { get { return cache.Get<Button>("download"); } }
        private Button cancelButton { get { return cache.Get<Button>("cancel"); } }
        internal Button pauseButton { get { return cache.Get<Button>("pause"); } }
        internal Button resumeButton { get { return cache.Get<Button>("resume"); } }
        private Button signInButton { get { return cache.Get<Button>("signIn"); } }
        private ProgressBar downloadProgress { get { return cache.Get<ProgressBar>("downloadProgress"); } }
        private VisualElement detailSizesAndSupportedVersionsContainer { get { return cache.Get<VisualElement>("detailSizesAndSupportedVersionsContainer"); } }
        private VisualElement detailUnityVersionsContainer { get { return cache.Get<VisualElement>("detailUnityVersionsContainer"); } }
        private SelectableLabel detailUnityVersions { get { return cache.Get<SelectableLabel>("detailUnityVersions"); } }
        private VisualElement detailSizesContainer { get { return cache.Get<VisualElement>("detailSizesContainer"); } }
        private VisualElement detailSizes { get { return cache.Get<VisualElement>("detailSizes"); } }
        private VisualElement detailReleaseDetailsContainer { get { return cache.Get<VisualElement>("detailReleaseDetailsContainer"); } }
        private VisualElement detailReleaseDetails { get { return cache.Get<VisualElement>("detailReleaseDetails"); } }
        private VisualElement detailLabelsContainer { get { return cache.Get<VisualElement>("detailLabelsContainer"); } }
        private VisualElement detailLabels { get { return cache.Get<VisualElement>("detailLabels"); } }
        private VisualElement detailSourcePathContainer { get { return cache.Get<VisualElement>("detailSourcePathContainer"); } }
        private SelectableLabel detailSourcePath { get { return cache.Get<SelectableLabel>("detailSourcePath"); } }
        internal PackageTagLabel GetTagLabel(string tag) { return cache.Get<PackageTagLabel>("tag" + tag); }
        internal VisualElement packageToolbarErrorContainer { get { return cache.Get<VisualElement>("toolbarErrorContainer"); } }
        private Label errorMessage { get { return cache.Get<Label>("message"); } }
        private Label errorStatus { get { return cache.Get<Label>("state"); } }
        private Button okButton { get { return cache.Get<Button>("ok"); } }

        private VisualElement detailContainer { get { return cache.Get<VisualElement>("detailContainer"); } }

        private PackageDetailsLinks detailsLinks => cache.Get<PackageDetailsLinks>("detailLinksContainer");
        private PackageDetailsImages detailsImages => cache.Get<PackageDetailsImages>("detailImagesContainer");
    }
}
