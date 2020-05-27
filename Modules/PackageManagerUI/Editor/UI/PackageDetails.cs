// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.PackageManager.UI
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
                return isInstalledVersion ? package.versions.recommended : displayVersion;
            }
        }

        private const string k_EmptyDescriptionClass = "empty";

        private string previewInfoReadMoreUrl => $"https://docs.unity3d.com/{m_Application?.shortUnityVersion}/Documentation/Manual/pack-preview.html";

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
            PackageTag.Verified,
            PackageTag.InDevelopment,
            PackageTag.Local,
            PackageTag.Git,
            PackageTag.Preview,
            PackageTag.Deprecated
        };

        private static Texture2D s_LoadingTexture;

        // We limit the number of entries shown so as to not overload the dialog.
        // The relevance of results beyond this limit is also questionable.
        private const int MaxDependentList = 10;

        // Keep track of the width breakpoints at which images were hidden so we know
        //  when to add them back in
        private Stack<float> detailImagesWidthsWhenImagesRemoved;

        private bool m_DescriptionExpanded;

        private ResourceLoader m_ResourceLoader;
        private ApplicationProxy m_Application;
        private AssetDatabaseProxy m_AssetDatabase;
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private AssetStoreCache m_AssetStoreCache;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private IOProxy m_IOProxy;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_AssetDatabase = container.Resolve<AssetDatabaseProxy>();
            m_AssetStoreDownloadManager = container.Resolve<AssetStoreDownloadManager>();
            m_AssetStoreCache = container.Resolve<AssetStoreCache>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_PageManager = container.Resolve<PageManager>();
            m_IOProxy = container.Resolve<IOProxy>();
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

            detailDesc.RegisterCallback<GeometryChangedEvent>(DescriptionGeometryChangeEvent);
            detailImages?.RegisterCallback<GeometryChangedEvent>(ImagesGeometryChangeEvent);

            previewInfoBox.Q<Button>().clickable.clicked += () => m_Application.OpenURL(previewInfoReadMoreUrl);

            root.Query<TextField>().ForEach(t =>
            {
                t.isReadOnly = true;
            });

            RefreshContent();
        }

        public UnityEngine.Object GetDisplayPackageManifestAsset()
        {
            var assetPath = displayVersion?.packageInfo?.assetPath;
            if (string.IsNullOrEmpty(assetPath))
                return null;
            return m_AssetDatabase.LoadMainAssetAtPath(Path.Combine(assetPath, "package.json"));
        }

        public void OnEnable()
        {
            detailImagesWidthsWhenImagesRemoved = new Stack<float>();
            m_Application.onFinishCompiling += RefreshPackageActionButtons;

            m_PackageDatabase.onPackagesChanged += (added, removed, preUpdate, postUpdate) => OnPackagesUpdated(postUpdate);
            m_PackageDatabase.onPackagesChanged += (added, removed, preUpdate, postUpdate) => RefreshDependencies();

            m_PackageDatabase.onPackageProgressUpdate += OnPackageProgressUpdate;

            m_AssetStoreDownloadManager.onDownloadProgress += UpdateDownloadProgressBar;
            m_AssetStoreDownloadManager.onDownloadFinalized += StopDownloadProgressBar;
            m_AssetStoreDownloadManager.onDownloadPaused += PauseDownloadProgressBar;

            m_PageManager.onSelectionChanged += OnSelectionChanged;

            m_PackageManagerPrefs.onShowDependenciesChanged += (value) => RefreshDependencies();

            // manually call the callback function once on initialization to refresh the UI
            OnSelectionChanged(m_PageManager.GetSelectedVersion());
        }

        public void OnDisable()
        {
            m_Application.onFinishCompiling -= RefreshPackageActionButtons;

            m_PackageDatabase.onPackageProgressUpdate -= OnPackageProgressUpdate;

            m_AssetStoreDownloadManager.onDownloadProgress -= UpdateDownloadProgressBar;
            m_AssetStoreDownloadManager.onDownloadFinalized -= StopDownloadProgressBar;
            m_AssetStoreDownloadManager.onDownloadPaused -= PauseDownloadProgressBar;

            m_PageManager.onSelectionChanged -= OnSelectionChanged;

            ClearSupportingImages();
        }

        private void UpdateDownloadProgressBar(IOperation operation)
        {
            if (!m_Application.isInternetReachable)
            {
                detailError.SetError(new UIError(UIErrorCode.NetworkError, L10n.Tr("No internet connection.")));
                m_PackageDatabase.AbortDownload(package);
                RefreshDownloadStatesButtons();
                return;
            }

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
            UIUtils.SetElementDisplay(detailContainer, visible);
            UIUtils.SetElementDisplay(packageToolbarContainer, visible);
            UIUtils.SetElementDisplay(packageToolbarLeftArea, visible);
        }

        internal void OnSelectionChanged(IPackageVersion version)
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

        void RefreshExtensions(IPackageVersion version)
        {
            var packageInfo = version?.packageInfo;
            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var extension in PackageManagerExtensions.Extensions)
                    extension.OnPackageSelectionChange(packageInfo);

                foreach (var extension in PackageManagerExtensions.ToolbarExtensions)
                    extension.OnPackageSelectionChange(version, packageToolbarContainer);
            });
        }

        private void RefreshContent()
        {
            detailScrollView.scrollOffset = new Vector2(0, 0);

            var detailVisible = package != null && displayVersion != null;
            var detailEnabled = displayVersion == null || displayVersion.isFullyFetched;
            if (!detailVisible)
            {
                UIUtils.SetElementDisplay(customContainer, false);
                RefreshExtensions(null);
            }
            else
            {
                detailTitle.SetValueWithoutNotify(displayVersion.displayName);

                RefreshLinks();

                RefreshDescription();

                var versionString = displayVersion.versionString;
                var releaseDateString = displayVersion.publishedDate?.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("en-US"));
                if (string.IsNullOrEmpty(releaseDateString))
                    detailVersion.SetValueWithoutNotify(string.Format(L10n.Tr("Version {0}"), versionString));
                else
                    detailVersion.SetValueWithoutNotify(string.Format(L10n.Tr("Version {0} - {1}"), versionString, releaseDateString));
                UIUtils.SetElementDisplay(detailVersion, !package.Is(PackageType.BuiltIn) && !string.IsNullOrEmpty(versionString));

                UIUtils.SetElementDisplay(previewInfoBox, displayVersion.HasTag(PackageTag.Preview));

                foreach (var tag in k_VisibleTags)
                    UIUtils.SetElementDisplay(GetTagLabel(tag.ToString()), displayVersion.HasTag(tag));
                UIUtils.SetElementDisplay(GetTagLabel(PackageType.AssetStore.ToString()), package.Is(PackageType.AssetStore));

                sampleList.SetPackageVersion(displayVersion);

                RefreshAuthor();

                RefreshReleaseDetails();

                UIUtils.SetElementDisplay(customContainer, true);
                RefreshExtensions(displayVersion);

                RefreshDependencies();

                RefreshSizeAndSupportedUnityVersions();

                RefreshSupportingImages();

                RefreshPackageActionButtons();
                RefreshImportButtons();
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
            var minTextHeight = (int)TextElement.MeasureVisualElementTextSize(detailDesc, "|", 0, MeasureMode.Undefined, 0, MeasureMode.Undefined, detailDesc.textHandle).y*3 + 1;
            var textHeight = (int)TextElement.MeasureVisualElementTextSize(detailDesc, detailDesc.text, evt.newRect.width, MeasureMode.AtMost, float.MaxValue, MeasureMode.Undefined, detailDesc.textHandle).y + 1;
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
                UIUtils.SetElementDisplay(detailDescLess, true);
            }
        }

        private void ImagesGeometryChangeEvent(GeometryChangedEvent evt)
        {
            // hide or show the last image depending on whether it fits on the screen
            var images = detailImages.Children();
            var visibleImages = images.Where(elem => UIUtils.IsElementVisible(elem));

            var firstInvisibleImage = images.FirstOrDefault(elem => !UIUtils.IsElementVisible(elem));
            var visibleImagesWidth = visibleImages.Sum(elem => elem.rect.width);
            var lastVisibleImage = visibleImages.LastOrDefault();

            var widthWhenLastImageRemoved = detailImagesWidthsWhenImagesRemoved.Any() ? detailImagesWidthsWhenImagesRemoved.Peek() : float.MaxValue;

            // if the container approximately doubles in height, that indicates the last image was wrapped around to another row
            if (lastVisibleImage != null && (evt.newRect.height >= 2 * lastVisibleImage.rect.height || visibleImagesWidth >= evt.newRect.width))
            {
                UIUtils.SetElementDisplay(lastVisibleImage, false);
                detailImagesWidthsWhenImagesRemoved.Push(evt.newRect.width);
            }
            else if (firstInvisibleImage != null && evt.newRect.width > widthWhenLastImageRemoved)
            {
                UIUtils.SetElementDisplay(firstInvisibleImage, true);
                detailImagesWidthsWhenImagesRemoved.Pop();
            }
        }

        private void RefreshDescription()
        {
            var hasDescription = !string.IsNullOrEmpty(displayVersion.description);
            detailDesc.EnableClass(k_EmptyDescriptionClass, !hasDescription);
            detailDesc.style.maxHeight = int.MaxValue;
            detailDesc.SetValueWithoutNotify(hasDescription ? displayVersion.description : L10n.Tr("There is no description for this package."));
            UIUtils.SetElementDisplay(detailDescMore, false);
            UIUtils.SetElementDisplay(detailDescLess, false);
            m_DescriptionExpanded = false;
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

        private void RefreshLabels()
        {
            detailLabels.Clear();

            if (enabledSelf && package?.labels != null)
            {
                var labels = string.Join(", ", package.labels.ToArray());

                if (!string.IsNullOrEmpty(labels))
                {
                    var textField = new TextField();
                    textField.SetValueWithoutNotify(labels);
                    textField.isReadOnly = true;
                    detailLabels.Add(textField);
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

        private void RefreshLinks()
        {
            detailLinksContainer.Clear();
            // add links from the package
            foreach (var link in package.links)
            {
                AddToLinks(new Button(() => { m_Application.OpenURL(link.url); })
                {
                    text = link.name,
                    tooltip = link.url,
                    classList = { "unity-button", "link" }
                });
            }

            // add links related to the upm version
            if (UpmPackageDocs.HasDocs(displayVersion))
                AddToLinks(new Button(ViewDocClick) { text = L10n.Tr("View documentation"), classList = { "unity-button", "link" } });

            if (UpmPackageDocs.HasChangelog(displayVersion))
                AddToLinks(new Button(ViewChangelogClick) { text = L10n.Tr("View changelog"), classList = { "unity-button", "link" } });

            if (UpmPackageDocs.HasLicenses(displayVersion))
                AddToLinks(new Button(ViewLicensesClick) { text = L10n.Tr("View licenses"), classList = { "unity-button", "link" } });

            UIUtils.SetElementDisplay(detailLinksContainer, detailLinksContainer.childCount != 0);
        }

        private void AddToLinks(VisualElement item)
        {
            // Add a seperator between links to make them less crowded together
            if (detailLinksContainer.childCount > 0)
                detailLinksContainer.Add(new Label("·") { classList = { "interpunct" } });
            detailLinksContainer.Add(item);
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
                var textField = new TextField();
                textField.style.whiteSpace = WhiteSpace.Normal;
                textField.SetValueWithoutNotify(string.Format(L10n.Tr("Size: {0} (Number of files: {1})"), UIUtils.ConvertToHumanReadableSize(sizeInfo.downloadSize), sizeInfo.assetCount));
                textField.isReadOnly = true;
                detailSizes.Add(textField);
            }

            return showSizes;
        }

        private void ClearSupportingImages()
        {
            foreach (var elt in detailImages.Children())
            {
                if (elt is Label &&
                    elt.style.backgroundImage.value.texture != null &&
                    elt.style.backgroundImage.value.texture != s_LoadingTexture)
                {
                    UnityEngine.Object.DestroyImmediate(elt.style.backgroundImage.value.texture);
                }
            }
            detailImages.Clear();
            detailImagesMoreLink.clicked -= OnMoreImagesClicked;
        }

        private void RefreshSupportingImages()
        {
            UIUtils.SetElementDisplay(detailImagesContainer, package.images.Any());
            ClearSupportingImages();

            if (s_LoadingTexture == null)
                s_LoadingTexture = (Texture2D)EditorGUIUtility.LoadRequired("Icons/UnityLogo.png");

            long id;
            if (long.TryParse(package.uniqueId, out id))
            {
                foreach (var packageImage in package.images)
                {
                    var image = new Label { classList = { "image" } };
                    image.OnLeftClick(() => { m_Application.OpenURL(packageImage.url); });
                    image.style.backgroundImage = s_LoadingTexture;
                    detailImages.Add(image);

                    m_AssetStoreCache.DownloadImageAsync(id, packageImage.thumbnailUrl, (retId, texture) =>
                    {
                        if (retId.ToString() == package?.uniqueId)
                        {
                            texture.hideFlags = HideFlags.HideAndDontSave;
                            image.style.backgroundImage = texture;
                        }
                    });
                }
            }

            detailImagesMoreLink.clicked += OnMoreImagesClicked;
        }

        private void OnMoreImagesClicked()
        {
            m_Application.OpenURL((package as AssetStorePackage).assetStoreLink);
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
            var error = displayVersion?.errors?.FirstOrDefault() ?? package?.errors?.FirstOrDefault();
            if (error == null)
                detailError.ClearError();
            else
            {
                detailError.SetError(error);
                detailError.onCloseError = () => m_PackageDatabase.ClearPackageErrors(package);
            }
        }

        internal void OnPackageProgressUpdate(IPackage package)
        {
            RefreshPackageActionButtons();
        }

        internal void RefreshPackageActionButtons()
        {
            RefreshAddButton();
            RefreshRemoveButton();
        }

        private void RefreshAddButton()
        {
            var installed = package?.versions.installed;
            var targetVersion = this.targetVersion;
            var installable = targetVersion?.HasTag(PackageTag.Installable) ?? false;
            var visibleFlag = installed?.HasTag(PackageTag.VersionLocked) != true && displayVersion != null && installable && installed != targetVersion;
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

                RefreshButtonStatusAndTooltip(updateButton, action, disableIfInstallOrUninstallInProgress, disableIfCompiling);
            }
            UIUtils.SetElementDisplay(updateButton, visibleFlag);
        }

        private void RefreshRemoveButton()
        {
            var installed = package?.versions.installed;
            var removable = displayVersion?.HasTag(PackageTag.Removable) ?? false;
            var visibleFlag = installed != null && installed == displayVersion && removable;
            if (visibleFlag)
            {
                var action = displayVersion.HasTag(PackageTag.BuiltIn) ? PackageAction.Disable : PackageAction.Remove;
                var currentPackageRemoveInProgress = m_PackageDatabase.IsUninstallInProgress(package);
                removeButton.text = GetButtonText(action, currentPackageRemoveInProgress);

                RefreshButtonStatusAndTooltip(removeButton, action, disableIfInstallOrUninstallInProgress, disableIfCompiling);
            }
            UIUtils.SetElementDisplay(removeButton, visibleFlag);
        }

        private void RefreshDownloadStatesButtons()
        {
            var downloadable = displayVersion?.HasTag(PackageTag.Downloadable) ?? false;
            if (downloadable)
            {
                var operation = m_AssetStoreDownloadManager.GetDownloadOperation(displayVersion.packageUniqueId);
                var isPaused = operation?.state == DownloadState.Paused;
                var isPausing = operation?.state == DownloadState.Pausing;
                var isInPause = operation?.isInPause == true;
                var isInProgress = operation?.isInProgress == true;
                var isDownloadRequested = operation?.state == DownloadState.DownloadRequested;
                var isResumeRequested = operation?.state == DownloadState.ResumeRequested;
                var state = package.state;

                var showDownloadButton = operation == null || (!isInProgress && !isResumeRequested && !isInPause);
                UIUtils.SetElementDisplay(downloadButton, showDownloadButton);
                if (showDownloadButton)
                {
                    var action = state == PackageState.UpdateAvailable ? PackageAction.Upgrade : PackageAction.Download;
                    downloadButton.text = GetButtonText(action, isInProgress);

                    var alreadyDownloaded = displayVersion.isAvailableOnDisk && state != PackageState.InProgress && state != PackageState.UpdateAvailable;
                    RefreshButtonStatusAndTooltip(downloadButton, action,
                        new ButtonDisableCondition(alreadyDownloaded, L10n.Tr("This package has already been downloaded to disk.")),
                        new ButtonDisableCondition(isDownloadRequested, L10n.Tr("The download request has been sent. Please wait for the download to start.")),
                        disableIfCompiling);
                }

                var showPauseButton = isInProgress || isPausing;
                UIUtils.SetElementDisplay(pauseButton, showPauseButton);
                if (showPauseButton)
                {
                    RefreshButtonStatusAndTooltip(pauseButton, PackageAction.Pause,
                        new ButtonDisableCondition(isPausing, L10n.Tr("The pause request has been sent. Please wait for the download to pause.")),
                        disableIfCompiling);
                }

                var showResumeButton = isPaused || isResumeRequested;
                UIUtils.SetElementDisplay(resumeButton, showResumeButton);
                if (showResumeButton)
                {
                    RefreshButtonStatusAndTooltip(resumeButton, PackageAction.Resume,
                        new ButtonDisableCondition(isResumeRequested, L10n.Tr("The resume request has been sent. Please wait for the download to resume.")),
                        disableIfCompiling);
                }

                var showCancelButton = !showDownloadButton && (showPauseButton || showResumeButton);
                UIUtils.SetElementDisplay(cancelButton, showCancelButton);
                if (showCancelButton)
                {
                    RefreshButtonStatusAndTooltip(cancelButton, PackageAction.Cancel,
                        new ButtonDisableCondition(isResumeRequested, L10n.Tr("A resume request has been sent for this download. You cannot cancel this download until it is resumed.")),
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
            }
        }

        private void RefreshImportButtons()
        {
            var importable = displayVersion?.HasTag(PackageTag.Importable) ?? false;
            UIUtils.SetElementDisplay(importButton, importable);
            if (!importable)
                return;

            importButton.text = GetButtonText(PackageAction.Import);

            RefreshButtonStatusAndTooltip(importButton, PackageAction.Import,
                new ButtonDisableCondition(!displayVersion.isAvailableOnDisk, L10n.Tr("You need to download the package before you can import assets into your project.")),
                disableIfCompiling);
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
            detailDesc.style.maxHeight = (int)TextElement.MeasureVisualElementTextSize(detailDesc, "|", 0, MeasureMode.Undefined, 0, MeasureMode.Undefined, detailDesc.textHandle).y*3 + 5;
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
            if (package.versions.installed != null && !package.versions.installed.isDirectDependency && package.versions.installed != targetVersion)
            {
                var message = L10n.Tr("This version of the package is being used by other packages. Upgrading a different version might break your project. Are you sure you want to continue?");
                if (!EditorUtility.DisplayDialog(L10n.Tr("Unity Package Manager"), message, L10n.Tr("Yes"), L10n.Tr("No")))
                    return;
            }

            detailError.ClearError();
            m_PackageDatabase.Install(targetVersion);
            RefreshPackageActionButtons();

            var eventName = package.versions.installed == null ? "installNew" : "installUpdate";
            PackageManagerWindowAnalytics.SendEvent(eventName, displayVersion?.uniqueId);
        }

        /// <summary>
        /// Get a line-separated bullet list of package names
        /// </summary>
        private string GetPackageDashList(IEnumerable<IPackageVersion> versions, int maxListCount = MaxDependentList)
        {
            var shortListed = versions.Take(maxListCount);
            return " - " + string.Join("\n - ", shortListed.Select(p => p.displayName).ToArray());
        }

        /// <summary>
        /// Get the full dependent message string
        /// </summary>
        private string GetDependentMessage(IPackageVersion version, IEnumerable<IPackageVersion> roots, int maxListCount = MaxDependentList)
        {
            var dependentPackages = roots.Where(p => !p.HasTag(PackageTag.BuiltIn)).ToList();
            var dependentModules = roots.Where(p => p.HasTag(PackageTag.BuiltIn)).ToList();

            var packageType = version.HasTag(PackageTag.BuiltIn) ? "built-in package" : "package";
            var prefix = string.Format(L10n.Tr("This {0} is a dependency of the following "), packageType);
            var message = string.Format("{0}{1}:\n\n", prefix,  dependentPackages.Any() ? "packages" : "built-in packages");

            if (dependentPackages.Any())
                message += GetPackageDashList(dependentPackages, maxListCount);
            if (dependentPackages.Any() && dependentModules.Any())
                message += L10n.Tr("\n\nand the following built-in packages:\n\n");
            if (dependentModules.Any())
                message += GetPackageDashList(dependentModules, maxListCount);

            if (roots.Count() > maxListCount)
                message += L10n.Tr("\n\n   ... and more (see console for details) ...");

            var actionType = version.HasTag(PackageTag.BuiltIn) ? "disable" : "remove";
            message += string.Format(L10n.Tr("\n\nYou will need to remove or disable them before being able to {0} this {1}."), actionType, packageType);

            return message;
        }

        private void RemoveClick()
        {
            var roots = m_PackageDatabase.GetReverseDependencies(displayVersion)?.Where(p => p.isDirectDependency && p.isInstalled).ToList();
            // Only show this message on a package if it is installed by dependency only. This allows it to still be removed from the installed list.
            var showDialog = (roots?.Any() ?? false) && !(!displayVersion.HasTag(PackageTag.BuiltIn) && displayVersion.isDirectDependency);
            if (showDialog)
            {
                if (roots.Count > MaxDependentList)
                    Debug.Log(GetDependentMessage(displayVersion, roots, int.MaxValue));

                var message = GetDependentMessage(displayVersion, roots);
                var title = displayVersion.HasTag(PackageTag.BuiltIn) ? L10n.Tr("Cannot disable built-in package") : L10n.Tr("Cannot remove dependent package");
                EditorUtility.DisplayDialog(title, message, L10n.Tr("Ok"));

                return;
            }

            if (displayVersion.HasTag(PackageTag.InDevelopment))
            {
                if (!EditorUtility.DisplayDialog(L10n.Tr("Unity Package Manager"), L10n.Tr("You will lose all your changes (if any) if you delete a package in development. Are you sure?"), L10n.Tr("Yes"), L10n.Tr("No")))
                    return;

                detailError.ClearError();
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
            detailError.ClearError();
            m_PackageDatabase.Uninstall(package);
            RefreshPackageActionButtons();

            PackageManagerWindowAnalytics.SendEvent("uninstall", displayVersion?.uniqueId);
        }

        private void ViewOfflineUrl(IPackageVersion version, Func<IOProxy, IPackageVersion, bool, string> getUrl, string messageOnNotFound)
        {
            if (!version.isAvailableOnDisk)
            {
                EditorUtility.DisplayDialog(L10n.Tr("Unity Package Manager"), L10n.Tr("This package is not available offline."), L10n.Tr("Ok"));
                return;
            }
            var offlineUrl = getUrl(m_IOProxy, version, true);
            if (!string.IsNullOrEmpty(offlineUrl))
                m_Application.OpenURL(offlineUrl);
            else
                EditorUtility.DisplayDialog(L10n.Tr("Unity Package Manager"), messageOnNotFound, L10n.Tr("Ok"));
        }

        private void ViewUrl(IPackageVersion version, Func<IOProxy, IPackageVersion, bool, string> getUrl, string messageOnNotFound)
        {
            if (m_Application.isInternetReachable)
            {
                var onlineUrl = getUrl(m_IOProxy, version, false);
                var request = UnityWebRequest.Head(onlineUrl);
                var operation = request.SendWebRequest();
                operation.completed += (op) =>
                {
                    if (request.responseCode != 404)
                    {
                        m_Application.OpenURL(onlineUrl);
                    }
                    else
                    {
                        ViewOfflineUrl(version, getUrl, messageOnNotFound);
                    }
                };
            }
            else
            {
                ViewOfflineUrl(version, getUrl, messageOnNotFound);
            }
        }

        private void ViewDocClick()
        {
            ViewUrl(displayVersion, UpmPackageDocs.GetDocumentationUrl, L10n.Tr("This package does not contain offline documentation."));
        }

        private void ViewChangelogClick()
        {
            ViewUrl(displayVersion, UpmPackageDocs.GetChangelogUrl, L10n.Tr("This package does not contain offline changelog."));
        }

        private void ViewLicensesClick()
        {
            ViewUrl(displayVersion, UpmPackageDocs.GetLicensesUrl, L10n.Tr("This package does not contain offline licenses."));
        }

        private void ImportClick()
        {
            m_PackageDatabase.Import(package);
            RefreshImportButtons();

            PackageManagerWindowAnalytics.SendEvent("import", package.uniqueId);
        }

        private void DownloadClick()
        {
            var downloadInProgress = m_PackageDatabase.IsDownloadInProgress(displayVersion);
            if (!downloadInProgress && !m_Application.isInternetReachable)
            {
                detailError.SetError(new UIError(UIErrorCode.NetworkError, L10n.Tr("No internet connection.")));
                return;
            }

            detailError.ClearError();

            m_PackageDatabase.Download(package);
            RefreshDownloadStatesButtons();

            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(displayVersion.packageUniqueId);
            downloadProgress.UpdateProgress(operation);


            PackageManagerWindowAnalytics.SendEvent("startDownload", package.uniqueId);
        }

        private void CancelClick()
        {
            var downloadInProgress = m_PackageDatabase.IsDownloadInProgress(displayVersion);
            if (!downloadInProgress && !m_Application.isInternetReachable)
            {
                detailError.SetError(new UIError(UIErrorCode.NetworkError, L10n.Tr("No internet connection.")));
                m_PackageDatabase.AbortDownload(package);
                return;
            }

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
            {
                detailError.SetError(new UIError(UIErrorCode.NetworkError, L10n.Tr("No internet connection.")));
                return;
            }

            m_PackageDatabase.ResumeDownload(package);
            RefreshDownloadStatesButtons();
            PackageManagerWindowAnalytics.SendEvent("resumeDownload", package.uniqueId);
        }

        private VisualElementCache cache { get; set; }

        private TextField detailDesc { get { return cache.Get<TextField>("detailDesc"); } }
        private Button detailDescMore { get { return cache.Get<Button>("detailDescMore"); } }
        private Button detailDescLess { get { return cache.Get<Button>("detailDescLess"); } }
        private VisualElement detailLinksContainer => cache.Get<VisualElement>("detailLinksContainer");
        internal Alert detailError { get { return cache.Get<Alert>("detailError"); } }
        private ScrollView detailScrollView { get { return cache.Get<ScrollView>("detailScrollView"); } }
        private VisualElement detailContainer { get { return cache.Get<VisualElement>("detail"); } }
        private TextField detailTitle { get { return cache.Get<TextField>("detailTitle"); } }
        private TextField detailVersion { get { return cache.Get<TextField>("detailVersion"); } }
        private HelpBox previewInfoBox { get { return cache.Get<HelpBox>("previewInfoBox"); } }
        private VisualElement detailPurchasedDateContainer { get { return cache.Get<VisualElement>("detailPurchasedDateContainer"); } }
        private TextField detailPurchasedDate { get { return cache.Get<TextField>("detailPurchasedDate"); } }
        private VisualElement detailAuthorContainer { get { return cache.Get<VisualElement>("detailAuthorContainer"); } }
        private TextField detailAuthorText { get { return cache.Get<TextField>("detailAuthorText"); } }
        private Button detailAuthorLink { get { return cache.Get<Button>("detailAuthorLink"); } }
        private VisualElement customContainer { get { return cache.Get<VisualElement>("detailCustomContainer"); } }
        private PackageSampleList sampleList { get { return cache.Get<PackageSampleList>("detailSampleList"); } }
        private PackageDependencies dependencies { get { return cache.Get<PackageDependencies>("detailDependencies"); } }
        internal VisualElement packageToolbarContainer { get { return cache.Get<VisualElement>("toolbarContainer"); } }
        private VisualElement packageToolbarLeftArea { get { return cache.Get<VisualElement>("leftItems"); } }
        internal Button updateButton { get { return cache.Get<Button>("update"); } }
        internal Button removeButton { get { return cache.Get<Button>("remove"); } }
        private Button importButton { get { return cache.Get<Button>("import"); } }
        private Button downloadButton { get { return cache.Get<Button>("download"); } }
        private Button cancelButton { get { return cache.Get<Button>("cancel"); } }
        internal Button pauseButton { get { return cache.Get<Button>("pause"); } }
        internal Button resumeButton { get { return cache.Get<Button>("resume"); } }
        private ProgressBar downloadProgress { get { return cache.Get<ProgressBar>("downloadProgress"); } }
        private VisualElement detailSizesAndSupportedVersionsContainer { get { return cache.Get<VisualElement>("detailSizesAndSupportedVersionsContainer"); } }
        private VisualElement detailUnityVersionsContainer { get { return cache.Get<VisualElement>("detailUnityVersionsContainer"); } }
        private TextField detailUnityVersions { get { return cache.Get<TextField>("detailUnityVersions"); } }
        private VisualElement detailSizesContainer { get { return cache.Get<VisualElement>("detailSizesContainer"); } }
        private VisualElement detailSizes { get { return cache.Get<VisualElement>("detailSizes"); } }
        private VisualElement detailImagesContainer { get { return cache.Get<VisualElement>("detailImagesContainer"); } }
        private VisualElement detailImages { get { return cache.Get<VisualElement>("detailImages"); } }
        private VisualElement detailReleaseDetailsContainer { get { return cache.Get<VisualElement>("detailReleaseDetailsContainer"); } }
        private VisualElement detailReleaseDetails { get { return cache.Get<VisualElement>("detailReleaseDetails"); } }
        private Button detailImagesMoreLink { get { return cache.Get<Button>("detailImagesMoreLink"); } }
        private VisualElement detailLabelsContainer { get { return cache.Get<VisualElement>("detailLabelsContainer"); } }
        private VisualElement detailLabels { get { return cache.Get<VisualElement>("detailLabels"); } }
        private VisualElement detailSourcePathContainer { get { return cache.Get<VisualElement>("detailSourcePathContainer"); } }
        private TextField detailSourcePath { get { return cache.Get<TextField>("detailSourcePath"); } }
        internal PackageTagLabel GetTagLabel(string tag) { return cache.Get<PackageTagLabel>("tag" + tag); }
    }
}
