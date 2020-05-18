// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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

        private IPackage m_Package;
        private IPackageVersion m_Version;
        private IPackage package
        {
            get { return m_Package; }
            set { m_Package = value; }
        }

        private IPackageVersion displayVersion
        {
            get { return m_Version ?? m_Package?.versions.primary; }
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

        private string previewInfoReadMoreUrl => $"https://docs.unity3d.com/{ApplicationUtil.instance.shortUnityVersion}/Documentation/Manual/pack-preview.html";

        internal enum PackageAction
        {
            Add,
            Remove,
            Update,
            Enable,
            Disable,
            UpToDate,
            Current,
            Local,
            Git,
            Embedded,
            Download,
            Upgrade,
            Import
        }

        internal static readonly string[] k_PackageActionVerbs = { "Install", "Remove", "Update to", "Enable", "Disable", "Up to date", "Current", "Local", "Git", "Embedded", "Download", "Update", "Import" };
        private static readonly string[] k_PackageActionInProgressVerbs = { "Installing", "Removing", "Updating to", "Enabling", "Disabling", "Up to date", "Current", "Local", "Git", "Embedded", "Cancel", "Cancel", "Import" };
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

        public PackageDetails()
        {
            var root = Resources.GetTemplate("PackageDetails.uxml");
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
            downloadButton.clickable.clicked += DownloadOrCancelClick;
            detailDescMore.clickable.clicked += DescMoreClick;
            detailDescLess.clickable.clicked += DescLessClick;

            detailDesc.RegisterCallback<GeometryChangedEvent>(DescriptionGeometryChangeEvent);
            detailImages?.RegisterCallback<GeometryChangedEvent>(ImagesGeometryChangeEvent);

            previewInfoBox.Q<Button>().clickable.clicked += () => ApplicationUtil.instance.OpenURL(previewInfoReadMoreUrl);

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
            return AssetDatabase.LoadMainAssetAtPath(Path.Combine(assetPath, "package.json"));
        }

        public void OnEnable()
        {
            detailImagesWidthsWhenImagesRemoved = new Stack<float>();
            ApplicationUtil.instance.onFinishCompiling += RefreshPackageActionButtons;

            PackageDatabase.instance.onPackagesChanged += (added, removed, preUpdate, postUpdate) => OnPackagesUpdated(postUpdate);
            PackageDatabase.instance.onPackagesChanged += (added, removed, preUpdate, postUpdate) => RefreshDependencies();

            PackageDatabase.instance.onPackageProgressUpdate += OnPackageProgressUpdate;

            AssetStoreDownloadManager.instance.onDownloadProgress += UpdateDownloadProgressBar;
            AssetStoreDownloadManager.instance.onDownloadFinalized += StopDownloadProgressBar;

            PageManager.instance.onSelectionChanged += OnSelectionChanged;

            PackageManagerPrefs.instance.onShowDependenciesChanged += (value) => RefreshDependencies();

            // manually call the callback function once on initialization to refresh the UI
            OnSelectionChanged(PageManager.instance.GetSelectedVersion());
        }

        public void OnDisable()
        {
            ApplicationUtil.instance.onFinishCompiling -= RefreshPackageActionButtons;

            PackageDatabase.instance.onPackageProgressUpdate -= OnPackageProgressUpdate;

            AssetStoreDownloadManager.instance.onDownloadProgress -= UpdateDownloadProgressBar;
            AssetStoreDownloadManager.instance.onDownloadFinalized -= StopDownloadProgressBar;

            PageManager.instance.onSelectionChanged -= OnSelectionChanged;

            ClearSupportingImages();
        }

        private void UpdateDownloadProgressBar(IOperation operation)
        {
            if (displayVersion?.packageUniqueId != operation.packageUniqueId)
                return;
            downloadProgress.UpdateProgress(operation);
        }

        private void StopDownloadProgressBar(IOperation operation)
        {
            if (displayVersion?.packageUniqueId != operation.packageUniqueId)
                return;

            var downloadOperation = operation as AssetStoreDownloadOperation;
            if (downloadOperation.state == DownloadState.Error || downloadOperation.state == DownloadState.Aborted)
                RefreshErrorDisplay();
            downloadProgress.UpdateProgress(operation);
            RefreshImportAndDownloadButtons();
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
                SetPackage(PackageDatabase.instance.GetPackage(version), version);
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

                var versionString = displayVersion.version?.ToString() ?? displayVersion.versionString;
                var releaseDateString = displayVersion.publishedDate?.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("en-US"));
                if (string.IsNullOrEmpty(releaseDateString))
                    detailVersion.SetValueWithoutNotify(string.Format(ApplicationUtil.instance.GetTranslationForText("Version {0}"), versionString));
                else
                    detailVersion.SetValueWithoutNotify(string.Format(ApplicationUtil.instance.GetTranslationForText("Version {0} - {1}"), versionString, releaseDateString));
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
                RefreshImportAndDownloadButtons();

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
            detailDesc.SetValueWithoutNotify(hasDescription ? displayVersion.description : ApplicationUtil.instance.GetTranslationForText("There is no description for this package."));
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

            if (enabledSelf && m_Package?.labels != null)
            {
                var labels = string.Join(", ", m_Package.labels.ToArray());

                if (!string.IsNullOrEmpty(labels))
                {
                    var textField = new TextField();
                    textField.SetValueWithoutNotify(labels);
                    textField.isReadOnly = true;
                    detailLabels.Add(textField);
                }
            }

            var hasLabels = detailLabels.Children().Any();
            var isAssetStorePackage = m_Package is AssetStorePackage;

            if (!hasLabels && isAssetStorePackage)
                detailLabels.Add(new Label(ApplicationUtil.instance.GetTranslationForText("(None)")));

            UIUtils.SetElementDisplay(detailLabelsContainer, hasLabels || isAssetStorePackage);
        }

        private void RefreshPurchasedDate()
        {
            if (enabledSelf)
            {
                detailPurchasedDate.SetValueWithoutNotify(m_Package?.purchasedTime?.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("en-US")) ?? string.Empty);
            }
            UIUtils.SetElementDisplay(detailPurchasedDateContainer, !string.IsNullOrEmpty(detailPurchasedDate.text));
        }

        private void RefreshReleaseDetails()
        {
            detailReleaseDetails.Clear();

            // If the package details is not enabled, don't update the date yet as we are fetching new information
            if (enabledSelf && m_Package.firstPublishedDate != null)
            {
                detailReleaseDetails.Add(new PackageReleaseDetailsItem($"{displayVersion.versionString} (Current)", displayVersion.publishedDate, displayVersion.releaseNotes));

                detailReleaseDetails.Add(new PackageReleaseDetailsItem("Original", m_Package.firstPublishedDate, string.Empty));
            }

            UIUtils.SetElementDisplay(detailReleaseDetailsContainer, detailReleaseDetails.Children().Any());
        }

        private void RefreshLinks()
        {
            detailLinksContainer.Clear();
            // add links from the package
            foreach (var link in package.links)
            {
                AddToLinks(new Button(() => { ApplicationUtil.instance.OpenURL(link.url); })
                {
                    text = link.name,
                    tooltip = link.url,
                    classList = { "unity-button", "link" }
                });
            }

            // add links related to the upm version
            if (UpmPackageDocs.HasDocs(displayVersion))
                AddToLinks(new Button(ViewDocClick) { text = ApplicationUtil.instance.GetTranslationForText("View documentation"), classList = { "unity-button", "link" } });

            if (UpmPackageDocs.HasChangelog(displayVersion))
                AddToLinks(new Button(ViewChangelogClick) { text = ApplicationUtil.instance.GetTranslationForText("View changelog"), classList = { "unity-button", "link" } });

            if (UpmPackageDocs.HasLicenses(displayVersion))
                AddToLinks(new Button(ViewLicensesClick) { text = ApplicationUtil.instance.GetTranslationForText("View licenses"), classList = { "unity-button", "link" } });

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
                detailUnityVersions.SetValueWithoutNotify(string.Format(ApplicationUtil.instance.GetTranslationForText("{0} or higher"), supportedVersion));
                var tooltip = supportedVersion.ToString();
                if (displayVersion.supportedVersions != null && displayVersion.supportedVersions.Any())
                {
                    var versions = displayVersion.supportedVersions.Select(version => version.ToString()).ToArray();
                    tooltip = versions.Length == 1 ? versions[0] :
                        string.Format(ApplicationUtil.instance.GetTranslationForText("{0} and {1} to improve compatibility with the range of these versions of Unity"), string.Join(", ", versions, 0, versions.Length - 1), versions[versions.Length - 1]);
                }
                detailUnityVersions.tooltip = string.Format(ApplicationUtil.instance.GetTranslationForText("Package has been submitted using Unity {0}"), tooltip);
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
                textField.SetValueWithoutNotify(string.Format(ApplicationUtil.instance.GetTranslationForText("Size: {0} (Number of files: {1})"), UIUtils.ConvertToHumanReadableSize(sizeInfo.downloadSize), sizeInfo.assetCount));
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
                    image.OnLeftClick(() => { ApplicationUtil.instance.OpenURL(packageImage.url); });
                    image.style.backgroundImage = s_LoadingTexture;
                    detailImages.Add(image);

                    AssetStoreDownloadManager.instance.DownloadImageAsync(id, packageImage.thumbnailUrl, (retId, texture) =>
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
            ApplicationUtil.instance.OpenURL((package as AssetStorePackage).assetStoreLink);
        }

        public void SetPackage(IPackage package, IPackageVersion version = null)
        {
            this.package = package;
            displayVersion = version ?? package?.versions.primary;

            if (version?.isFullyFetched == false)
                PackageDatabase.instance.FetchExtraInfo(version);

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
                detailError.onCloseError = () => PackageDatabase.instance.ClearPackageErrors(package);
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
            var visibleFlag = installed?.HasTag(PackageTag.VersionLocked) != true && displayVersion != null && installable;
            if (visibleFlag)
            {
                var installInProgress = PackageDatabase.instance.IsInstallInProgress(displayVersion);
                var enableButton = installed != targetVersion && !installInProgress &&
                    !PackageDatabase.instance.isInstallOrUninstallInProgress && !ApplicationUtil.instance.isCompiling;

                SemVersion? versionToUpdateTo = null;
                var action = displayVersion.HasTag(PackageTag.BuiltIn) ? PackageAction.Enable : PackageAction.Add;
                if (installed != null)
                {
                    if (installed == targetVersion)
                        action = targetVersion == package.versions.recommended ? PackageAction.UpToDate : PackageAction.Current;
                    else
                    {
                        action = PackageAction.Update;
                        versionToUpdateTo = targetVersion.version;
                    }
                }
                updateButton.SetEnabled(enableButton);
                updateButton.text = GetButtonText(action, installInProgress, versionToUpdateTo);
            }
            UIUtils.SetElementDisplay(updateButton, visibleFlag);
        }

        private void RefreshRemoveButton()
        {
            var visibleFlag = displayVersion?.HasTag(PackageTag.Removable) ?? false;
            if (visibleFlag)
            {
                var installed = package?.versions.installed;
                var action = displayVersion.HasTag(PackageTag.BuiltIn) ? PackageAction.Disable : PackageAction.Remove;
                var removeInProgress = PackageDatabase.instance.IsUninstallInProgress(package);

                var enableButton = !ApplicationUtil.instance.isCompiling && !removeInProgress
                    && !PackageDatabase.instance.isInstallOrUninstallInProgress && installed == displayVersion;

                removeButton.SetEnabled(enableButton);
                removeButton.text = GetButtonText(action, removeInProgress);
            }
            UIUtils.SetElementDisplay(removeButton, visibleFlag);
        }

        private void RefreshImportAndDownloadButtons()
        {
            if (displayVersion == null)
                return;

            var enableButton = !ApplicationUtil.instance.isCompiling;
            var operation = AssetStoreDownloadManager.instance.GetDownloadOperation(displayVersion.packageUniqueId);
            var downloadInProgress = operation?.isInProgress ?? false;

            var downloadable = displayVersion.HasTag(PackageTag.Downloadable);
            UIUtils.SetElementDisplay(downloadButton, downloadable);
            if (downloadable)
            {
                var state = package.state;
                downloadButton.text = GetButtonText(state == PackageState.UpdateAvailable ? PackageAction.Upgrade : PackageAction.Download, downloadInProgress);

                var enableDownloadButton = !displayVersion.isAvailableOnDisk || state == PackageState.InProgress || state == PackageState.UpdateAvailable;
                downloadButton.SetEnabled(enableButton && enableDownloadButton);

                downloadProgress.UpdateProgress(operation);
            }
            else
            {
                UIUtils.SetElementDisplay(downloadProgress, false);
            }


            var importable = displayVersion.HasTag(PackageTag.Importable);
            UIUtils.SetElementDisplay(importButton, importable);
            if (importable)
            {
                importButton.text = GetButtonText(PackageAction.Import);
                importButton.SetEnabled(enableButton && displayVersion.isAvailableOnDisk);
            }
        }

        private string GetButtonText(PackageAction action, bool inProgress = false, SemVersion? version = null)
        {
            var actionText = inProgress ? ApplicationUtil.instance.GetTranslationForText(k_PackageActionInProgressVerbs[(int)action]) : ApplicationUtil.instance.GetTranslationForText(k_PackageActionVerbs[(int)action]);
            return version == null ? actionText : $"{actionText} {version}";
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
                ApplicationUtil.instance.OpenURL(authorLink);
        }

        private void UpdateClick()
        {
            // dissuade users from updating by showing a warning message
            if (package.versions.installed != null && !package.versions.installed.isDirectDependency && package.versions.installed != targetVersion)
            {
                var message = ApplicationUtil.instance.GetTranslationForText("This version of the package is being used by other packages. Upgrading a different version might break your project. Are you sure you want to continue?");
                if (!EditorUtility.DisplayDialog(ApplicationUtil.instance.GetTranslationForText("Unity Package Manager"), message, ApplicationUtil.instance.GetTranslationForText("Yes"), ApplicationUtil.instance.GetTranslationForText("No")))
                    return;
            }

            detailError.ClearError();
            PackageDatabase.instance.Install(targetVersion);
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
            var prefix = string.Format(ApplicationUtil.instance.GetTranslationForText("This {0} is a dependency of the following "), packageType);
            var message = string.Format("{0}{1}:\n\n", prefix,  dependentPackages.Any() ? "packages" : "built-in packages");

            if (dependentPackages.Any())
                message += GetPackageDashList(dependentPackages, maxListCount);
            if (dependentPackages.Any() && dependentModules.Any())
                message += ApplicationUtil.instance.GetTranslationForText("\n\nand the following built-in packages:\n\n");
            if (dependentModules.Any())
                message += GetPackageDashList(dependentModules, maxListCount);

            if (roots.Count() > maxListCount)
                message += ApplicationUtil.instance.GetTranslationForText("\n\n   ... and more (see console for details) ...");

            var actionType = version.HasTag(PackageTag.BuiltIn) ? "disable" : "remove";
            message += string.Format(ApplicationUtil.instance.GetTranslationForText("\n\nYou will need to remove or disable them before being able to {0} this {1}."), actionType, packageType);

            return message;
        }

        private void RemoveClick()
        {
            var roots = PackageDatabase.instance.GetReverseDependencies(displayVersion)?.Where(p => p.isDirectDependency && p.isInstalled).ToList();
            // Only show this message on a package if it is installed by dependency only. This allows it to still be removed from the installed list.
            var showDialog = (roots?.Any() ?? false) && !(!displayVersion.HasTag(PackageTag.BuiltIn) && displayVersion.isDirectDependency);
            if (showDialog)
            {
                if (roots.Count > MaxDependentList)
                    Debug.Log(GetDependentMessage(displayVersion, roots, int.MaxValue));

                var message = GetDependentMessage(displayVersion, roots);
                var title = displayVersion.HasTag(PackageTag.BuiltIn) ? ApplicationUtil.instance.GetTranslationForText("Cannot disable built-in package") : ApplicationUtil.instance.GetTranslationForText("Cannot remove dependent package");
                EditorUtility.DisplayDialog(title, message, ApplicationUtil.instance.GetTranslationForText("Ok"));

                return;
            }

            if (displayVersion.HasTag(PackageTag.InDevelopment))
            {
                if (!EditorUtility.DisplayDialog(ApplicationUtil.instance.GetTranslationForText("Unity Package Manager"), ApplicationUtil.instance.GetTranslationForText("You will lose all your changes (if any) if you delete a package in development. Are you sure?"), ApplicationUtil.instance.GetTranslationForText("Yes"), ApplicationUtil.instance.GetTranslationForText("No")))
                    return;

                detailError.ClearError();
                PackageDatabase.instance.RemoveEmbedded(package);
                RefreshPackageActionButtons();

                PackageManagerWindowAnalytics.SendEvent("removeEmbedded", displayVersion.uniqueId);
                return;
            }

            var result = 0;
            if (displayVersion.HasTag(PackageTag.BuiltIn))
            {
                if (!PackageManagerPrefs.instance.skipDisableConfirmation)
                {
                    result = EditorUtility.DisplayDialogComplex(ApplicationUtil.instance.GetTranslationForText("Disable Built-In Package"),
                        ApplicationUtil.instance.GetTranslationForText("Are you sure you want to disable this built-in package?"),
                        ApplicationUtil.instance.GetTranslationForText("Disable"), ApplicationUtil.instance.GetTranslationForText("Cancel"), ApplicationUtil.instance.GetTranslationForText("Never ask"));
                }
            }
            else
            {
                if (!PackageManagerPrefs.instance.skipRemoveConfirmation)
                {
                    result = EditorUtility.DisplayDialogComplex(ApplicationUtil.instance.GetTranslationForText("Removing Package"),
                        ApplicationUtil.instance.GetTranslationForText("Are you sure you want to remove this package?"),
                        ApplicationUtil.instance.GetTranslationForText("Remove"), ApplicationUtil.instance.GetTranslationForText("Cancel"), ApplicationUtil.instance.GetTranslationForText("Never ask"));
                }
            }

            // Cancel
            if (result == 1)
                return;

            // Do not ask again
            if (result == 2)
            {
                if (displayVersion.HasTag(PackageTag.BuiltIn))
                    PackageManagerPrefs.instance.skipDisableConfirmation = true;
                else
                    PackageManagerPrefs.instance.skipRemoveConfirmation = true;
            }

            // Remove
            detailError.ClearError();
            PackageDatabase.instance.Uninstall(package);
            RefreshPackageActionButtons();

            PackageManagerWindowAnalytics.SendEvent("uninstall", displayVersion?.uniqueId);
        }

        private static void ViewOfflineUrl(IPackageVersion version, Func<IPackageVersion, bool, string> getUrl, string messageOnNotFound)
        {
            if (!version.isAvailableOnDisk)
            {
                EditorUtility.DisplayDialog(ApplicationUtil.instance.GetTranslationForText("Unity Package Manager"), ApplicationUtil.instance.GetTranslationForText("This package is not available offline."), ApplicationUtil.instance.GetTranslationForText("Ok"));
                return;
            }
            var offlineUrl = getUrl(version, true);
            if (!string.IsNullOrEmpty(offlineUrl))
                ApplicationUtil.instance.OpenURL(offlineUrl);
            else
                EditorUtility.DisplayDialog(ApplicationUtil.instance.GetTranslationForText("Unity Package Manager"), messageOnNotFound, ApplicationUtil.instance.GetTranslationForText("Ok"));
        }

        private static void ViewUrl(IPackageVersion version, Func<IPackageVersion, bool, string> getUrl, string messageOnNotFound)
        {
            if (ApplicationUtil.instance.isInternetReachable)
            {
                var onlineUrl = getUrl(version, false);
                var request = UnityWebRequest.Head(onlineUrl);
                var operation = request.SendWebRequest();
                operation.completed += (op) =>
                {
                    if (request.responseCode != 404)
                    {
                        ApplicationUtil.instance.OpenURL(onlineUrl);
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
            ViewUrl(displayVersion, UpmPackageDocs.GetDocumentationUrl, ApplicationUtil.instance.GetTranslationForText("This package does not contain offline documentation."));
        }

        private void ViewChangelogClick()
        {
            ViewUrl(displayVersion, UpmPackageDocs.GetChangelogUrl, ApplicationUtil.instance.GetTranslationForText("This package does not contain offline changelog."));
        }

        private void ViewLicensesClick()
        {
            ViewUrl(displayVersion, UpmPackageDocs.GetLicensesUrl, ApplicationUtil.instance.GetTranslationForText("This package does not contain offline licenses."));
        }

        private void ImportClick()
        {
            PackageDatabase.instance.Import(package);
            RefreshImportAndDownloadButtons();

            PackageManagerWindowAnalytics.SendEvent("import", package.uniqueId);
        }

        private void DownloadOrCancelClick()
        {
            var downloadInProgress = PackageDatabase.instance.IsDownloadInProgress(displayVersion);
            if (!downloadInProgress && !ApplicationUtil.instance.isInternetReachable)
            {
                detailError.SetError(new UIError(UIErrorCode.NetworkError, ApplicationUtil.instance.GetTranslationForText("No internet connection")));
                return;
            }

            detailError.ClearError();
            if (downloadInProgress)
                PackageDatabase.instance.AbortDownload(package);
            else
                PackageDatabase.instance.Download(package);

            RefreshImportAndDownloadButtons();

            var eventName = downloadInProgress ? "abortDownload" : "startDownload";
            PackageManagerWindowAnalytics.SendEvent(eventName, package.uniqueId);
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
        internal PackageTagLabel GetTagLabel(string tag) { return cache.Get<PackageTagLabel>("tag" + tag); }
    }
}
