// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor.PackageManager.UI.AssetStore;
using UnityEditorInternal;
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
            get { return m_Version ?? m_Package?.primaryVersion; }
            set { m_Version = value; }
        }

        private IPackageVersion targetVersion
        {
            get
            {
                var isInstalledVersion = displayVersion?.isInstalled ?? false;
                return isInstalledVersion ? package.recommendedVersion : displayVersion;
            }
        }

        private static readonly string k_EmptyDescriptionClass = "empty";

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

            SetContentVisibility(false);
            SetUpdateVisibility(false);
            removeButton.visible = false;
            importButton.visible = false;
            downloadButton.visible = false;

            detailAuthorLink.clickable.clicked += AuthorClick;
            updateButton.clickable.clicked += UpdateClick;
            removeButton.clickable.clicked += RemoveClick;
            importButton.clickable.clicked += ImportClick;
            downloadButton.clickable.clicked += DownloadOrCancelClick;
            editButton.clickable.clicked += EditPackageManifestClick;
            detailDescMore.clickable.clicked += DescMoreClick;
            detailDescLess.clickable.clicked += DescLessClick;
            detailWarningLink.clickable.clicked += WarningLinkClick;

            detailDesc.RegisterCallback<GeometryChangedEvent>(DescriptionGeometryChangeEvent);

            GetTagLabel(PackageTag.Verified.ToString()).text = ApplicationUtil.instance.shortUnityVersion + " verified";
        }

        private void OnEditorSelectionChanged()
        {
            var manifestAsset = GetDisplayPackageManifestAsset();
            if (manifestAsset == null)
                return;

            var onlyContainsCurrentPackageManifest = Selection.count == 1 && Selection.Contains(manifestAsset);
            editButton.SetEnabled(!onlyContainsCurrentPackageManifest);
        }

        public UnityEngine.Object GetDisplayPackageManifestAsset()
        {
            var assetPath = displayVersion?.packageInfo?.assetPath;
            if (string.IsNullOrEmpty(assetPath))
                return null;
            return AssetDatabase.LoadMainAssetAtPath(Path.Combine(assetPath, "package.json"));
        }

        private void EditPackageManifestClick()
        {
            var manifestAsset = GetDisplayPackageManifestAsset();
            if (manifestAsset == null)
                Debug.LogWarning("Could not find package.json asset for package: " + displayVersion.displayName);
            else
                Selection.activeObject = manifestAsset;
        }

        public void OnEnable()
        {
            ApplicationUtil.instance.onFinishCompiling += RefreshPackageActionButtons;

            PackageDatabase.instance.onPackagesChanged += (added, removed, preUpdate, postUpdate) => OnPackagesUpdated(postUpdate);
            PackageDatabase.instance.onPackagesChanged += (added, removed, preUpdate, postUpdate) => RefreshDependencies();

            PackageDatabase.instance.onPackageOperationStart += OnOperationStartOrFinish;
            PackageDatabase.instance.onPackageOperationFinish += OnOperationStartOrFinish;

            PackageDatabase.instance.onDownloadProgress += OnDownloadProgress;

            PageManager.instance.onPageRebuild += page => OnSelectionChanged(PageManager.instance.GetSelectedVersion());
            PageManager.instance.onSelectionChanged += OnSelectionChanged;

            PackageManagerPrefs.instance.onShowDependenciesChanged += (value) => RefreshDependencies();

            // manually call the callback function once on initialization to refresh the UI
            OnSelectionChanged(PageManager.instance.GetSelectedVersion());

            Selection.selectionChanged += OnEditorSelectionChanged;
        }

        public void OnDisable()
        {
            ApplicationUtil.instance.onFinishCompiling -= RefreshPackageActionButtons;

            PackageDatabase.instance.onPackageOperationStart -= OnOperationStartOrFinish;
            PackageDatabase.instance.onPackageOperationFinish -= OnOperationStartOrFinish;

            PackageDatabase.instance.onDownloadProgress -= OnDownloadProgress;

            PageManager.instance.onSelectionChanged -= OnSelectionChanged;

            Selection.selectionChanged -= OnEditorSelectionChanged;

            ClearSupportingImages();
        }

        private void OnDownloadProgress(IPackage package, DownloadProgress progress)
        {
            if (displayVersion?.packageUniqueId == package.uniqueId)
            {
                if (progress.state == DownloadProgress.State.Error || progress.state == DownloadProgress.State.Aborted)
                {
                    downloadProgress.Hide();
                    RefreshErrorDisplay();
                }
                else
                {
                    downloadProgress.SetProgress(progress.total == 0 ? 0 : progress.current / (float)progress.total);
                }
                RefreshImportAndDownloadButtons();
            }
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
            dependencies.SetDependencies(displayVersion?.dependencies);
        }

        private void SetUpdateVisibility(bool value)
        {
            UIUtils.SetElementDisplay(updateButton, value);
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

        private void SetDisplayVersion(IPackageVersion version)
        {
            displayVersion = version;
            detailScrollView.scrollOffset = new Vector2(0, 0);

            var detailVisible = package != null && displayVersion != null;
            if (!detailVisible)
            {
                UIUtils.SetElementDisplay(customContainer, false);
                RefreshExtensions(null);
            }
            else
            {
                var isBuiltIn = package.Is(PackageType.BuiltIn);

                SetUpdateVisibility(true);

                detailTitle.text = displayVersion.displayName;

                UIUtils.SetElementDisplay(detailNameContainer, !string.IsNullOrEmpty(package.name));
                detailName.text = package.name;

                RefreshLinks();

                RefreshDescription();

                RefreshCategories();

                var versionString = displayVersion.version?.ToString() ?? displayVersion.versionString;
                var releaseDateString = displayVersion.publishedDate?.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("en-US"));
                if (string.IsNullOrEmpty(releaseDateString))
                    detailVersion.text = string.Format(L10n.Tr("Version {0}"), versionString);
                else
                    detailVersion.text = string.Format(L10n.Tr("Version {0} - {1}"), versionString, releaseDateString);
                UIUtils.SetElementDisplay(detailVersion, !package.Is(PackageType.BuiltIn) && !string.IsNullOrEmpty(versionString));

                foreach (var tag in k_VisibleTags)
                    UIUtils.SetElementDisplay(GetTagLabel(tag.ToString()), displayVersion.HasTag(tag));
                UIUtils.SetElementDisplay(GetTagLabel(PackageType.AssetStore.ToString()), package.Is(PackageType.AssetStore));

                UIUtils.SetElementDisplay(editButton, displayVersion.isInstalled && !isBuiltIn);

                sampleList.SetPackageVersion(displayVersion);

                RefreshAuthor();

                RefreshRegistry();

                RefreshPublishedDate();

                UIUtils.SetElementDisplay(detailVersion, !isBuiltIn);

                UIUtils.SetElementDisplay(customContainer, true);
                RefreshExtensions(displayVersion);

                RefreshDependencies();

                RefreshSupportedUnityVersions();

                RefreshSizeInfo();

                RefreshSupportingImages();

                RefreshPackageActionButtons();
                RefreshImportAndDownloadButtons();
            }

            OnEditorSelectionChanged();

            // Set visibility
            SetContentVisibility(detailVisible);
            RefreshErrorDisplay();
        }

        private void DescriptionGeometryChangeEvent(GeometryChangedEvent evt)
        {
            // only hide long description when there are images to be displayed
            if (!(package?.images.Any() ?? false))
                return;

            var minTextHeight = (int)detailDesc.MeasureTextSize("|", 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).y*3 + 1;
            var textHeight = (int)detailDesc.MeasureTextSize(detailDesc.text, evt.newRect.width, MeasureMode.AtMost, float.MaxValue, MeasureMode.Undefined).y + 1;
            if (!m_DescriptionExpanded && textHeight > minTextHeight)
            {
                UIUtils.SetElementDisplay(detailDescMore, true);
                UIUtils.SetElementDisplay(detailDescLess, false);
                detailDesc.style.maxHeight = minTextHeight;
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

        private void RefreshDescription()
        {
            var hasDescription = !string.IsNullOrEmpty(displayVersion.description);
            detailDesc.EnableInClassList(k_EmptyDescriptionClass, !hasDescription);
            detailDesc.style.maxHeight = int.MaxValue;
            detailDesc.text = hasDescription ? displayVersion.description : "There is no description for this package.";
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
                    detailAuthorText.text = displayVersion.author;
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
                UIUtils.SetElementDisplay(detailRegistryWarning, !registry.isDefault);
                detailRegistryName.text = registry.isDefault ? "Unity" : registry.name;
                detailRegistryName.tooltip = registry.url;
            }
        }

        private void RefreshPublishedDate()
        {
            // If the package details is not enabled, don't update the date yet as we are fetching new information
            if (enabledSelf)
            {
                var dt = displayVersion.publishedDate ?? DateTime.Now;
                detailDate.text = displayVersion.publishedDate != null ? dt.ToString("MMMM dd,  yyyy", CultureInfo.CreateSpecificCulture("en-US")) : string.Empty;
            }

            UIUtils.SetElementDisplay(detailDateContainer, !string.IsNullOrEmpty(detailDate.text));
        }

        private void RefreshCategories()
        {
            var categoryLinks = displayVersion?.categoryLinks;
            UIUtils.SetElementDisplay(detailCategories, categoryLinks != null);

            detailCategories.Clear();
            if (categoryLinks != null)
            {
                foreach (var item in categoryLinks)
                {
                    var category = item.Key;
                    var url = item.Value;
                    detailCategories.Add(new Button(() => { ApplicationUtil.instance.OpenURL(url); })
                        { text = category, classList = { "category", "unity-button", "link" } });
                }
            }
        }

        private void RefreshLinks()
        {
            detailLinks.Clear();
            // add links from the package
            foreach (var link in package.links)
            {
                detailLinks.Add(new Button(() => { ApplicationUtil.instance.OpenURL(link.url); })
                {
                    text = link.name,
                    tooltip = link.url,
                    classList = { "unity-button", "link" }
                });
            }

            // add links related to the upm version
            if (UpmPackageDocs.HasDocs(displayVersion))
                detailLinks.Add(new Button(ViewDocClick) { text = "View documentation", classList = { "unity-button", "link" } });

            if (UpmPackageDocs.HasChangelog(displayVersion))
                detailLinks.Add(new Button(ViewChangelogClick) { text = "View changelog", classList = { "unity-button", "link" } });

            if (UpmPackageDocs.HasLicenses(displayVersion))
                detailLinks.Add(new Button(ViewLicensesClick) { text = "View licenses", classList = { "unity-button", "link" } });

            UIUtils.SetElementDisplay(detailLinksContainer, detailLinks.childCount != 0);
        }

        private void RefreshSupportedUnityVersions()
        {
            var supportedVersion = displayVersion.supportedVersions?.FirstOrDefault();
            if (supportedVersion == null)
                supportedVersion = displayVersion.supportedVersion;

            UIUtils.SetElementDisplay(detailUnityVersionsContainer, supportedVersion != null);
            if (supportedVersion != null)
            {
                detailUnityVersions.text = $"{supportedVersion} or higher";
                var tooltip = supportedVersion.ToString();
                if (displayVersion.supportedVersions != null && displayVersion.supportedVersions.Any())
                {
                    var versions = displayVersion.supportedVersions.Select(version => version.ToString()).ToArray();
                    tooltip = versions.Length == 1 ? versions[0] :
                        $"{string.Join(", ", versions, 0, versions.Length - 1)} and {versions[versions.Length - 1]} to improve compatibility with the range of these versions of Unity";
                }
                detailUnityVersions.tooltip = $"Package has been submitted using Unity {tooltip}.";
            }
            else
            {
                detailUnityVersions.text = string.Empty;
                detailUnityVersions.tooltip = string.Empty;
            }
        }

        private void RefreshSizeInfo()
        {
            UIUtils.SetElementDisplay(detailSizesContainer, displayVersion.sizes.Any());
            detailSizes.Clear();

            var sizeInfo = displayVersion.sizes.FirstOrDefault(info => info.supportedUnityVersion == displayVersion.supportedVersion);
            if (sizeInfo == null)
                sizeInfo = displayVersion.sizes.LastOrDefault();

            if (sizeInfo != null)
                detailSizes.Add(new Label($"Size: {UIUtils.convertToHumanReadableSize(sizeInfo.downloadSize)} (Number of files: {sizeInfo.assetCount})"));
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
                    var image = new Label {classList = {"image"}};

                    if (packageImage.type == PackageImage.ImageType.Youtube || packageImage.type == PackageImage.ImageType.Sketchfab)
                    {
                        image.AddToClassList("url");
                        image.OnLeftClick(() => { ApplicationUtil.instance.OpenURL(packageImage.url); });
                    }

                    image.style.backgroundImage = s_LoadingTexture;
                    detailImages.Add(image);

                    AssetStoreDownloadOperation.instance.DownloadImageAsync(id, packageImage.thumbnailUrl, (retId, texture) =>
                    {
                        if (retId.ToString() == package?.uniqueId)
                        {
                            texture.hideFlags = HideFlags.HideAndDontSave;
                            image.style.backgroundImage = texture;
                        }
                    });
                }
            }
        }

        public void SetPackage(IPackage package, IPackageVersion version = null)
        {
            version = version ?? package?.primaryVersion;
            this.package = package;

            SetEnabled(true);

            if (version != null && !version.isFullyFetched)
            {
                SetEnabled(false);
                PackageDatabase.instance.FetchExtraInfo(version);
            }

            SetDisplayVersion(version);
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

        internal void OnOperationStartOrFinish(IPackage package)
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
            var installed = package?.installedVersion;
            var targetVersion = this.targetVersion;
            var installable = targetVersion?.HasTag(PackageTag.Installable) ?? false;
            var visibleFlag = !(installed?.HasTag(PackageTag.VersionLocked) ?? false) && displayVersion != null && installable;
            if (visibleFlag)
            {
                var installInProgress = PackageDatabase.instance.IsInstallInProgress(displayVersion);
                var enableButton = installed != targetVersion && !installInProgress &&
                    !PackageDatabase.instance.isInstallOrUninstallInProgress && !ApplicationUtil.instance.isCompiling;

                SemVersion versionToUpdateTo = null;
                var action = displayVersion.HasTag(PackageTag.BuiltIn) ? PackageAction.Enable : PackageAction.Add;
                if (installed != null)
                {
                    if (installed == targetVersion)
                        action = targetVersion == package.recommendedVersion ? PackageAction.UpToDate : PackageAction.Current;
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
                var installed = package?.installedVersion;
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
            var downloadInProgress = false;

            var downloadable = displayVersion.HasTag(PackageTag.Downloadable);
            UIUtils.SetElementDisplay(downloadButton, downloadable);
            if (downloadable && downloadInProgress)
                downloadProgress.Show();
            else
                downloadProgress.Hide();
            if (downloadable)
            {
                var progress = PackageDatabase.instance.GetDownloadProgress(displayVersion);
                var state = package.GetState();
                downloadInProgress = progress != null && (progress.state == DownloadProgress.State.InProgress || progress.state == DownloadProgress.State.Started);
                downloadButton.text = GetButtonText(state == PackageState.Outdated ? PackageAction.Upgrade : PackageAction.Download, downloadInProgress);

                var enableDownloadButton = !displayVersion.isAvailableOnDisk || state == PackageState.InProgress || state == PackageState.Outdated;
                downloadButton.SetEnabled(enableButton && enableDownloadButton);

                if (downloadInProgress)
                    downloadProgress.SetProgress(progress.total == 0 ? 0 : progress.current / (float)progress.total);
            }

            var importable = displayVersion.HasTag(PackageTag.Importable);
            UIUtils.SetElementDisplay(importButton, importable);
            if (importable)
            {
                importButton.text = GetButtonText(PackageAction.Import);
                importButton.SetEnabled(enableButton && displayVersion.isAvailableOnDisk);
            }
        }

        private string GetButtonText(PackageAction action, bool inProgress = false, SemVersion version = null)
        {
            var actionText = inProgress ? k_PackageActionInProgressVerbs[(int)action] : k_PackageActionVerbs[(int)action];
            return version == null ? $"{actionText}" : $"{actionText} {version}";
        }

        private static void WarningLinkClick()
        {
            var unityVersionParts = Application.unityVersion.Split('.');
            Application.OpenURL($"https://docs.unity3d.com/{unityVersionParts[0]}.{unityVersionParts[1]}/Documentation/Manual/upm-scoped.html");
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
            detailDesc.style.maxHeight = (int)detailDesc.MeasureTextSize("|", 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).y*3 + 1;
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
            detailError.ClearError();
            PackageDatabase.instance.Install(targetVersion);
            RefreshPackageActionButtons();

            var eventName = package.installedVersion == null ? "installNew" : "installUpdate";
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
            var prefix = $"This {packageType} is a dependency of the following ";
            var message = string.Format("{0}{1}:\n\n", prefix,  dependentPackages.Any() ? "packages" : "built-in packages");

            if (dependentPackages.Any())
                message += GetPackageDashList(dependentPackages, maxListCount);
            if (dependentPackages.Any() && dependentModules.Any())
                message += "\n\nand the following built-in packages:\n\n";
            if (dependentModules.Any())
                message += GetPackageDashList(dependentModules, maxListCount);

            if (roots.Count() > maxListCount)
                message += "\n\n   ... and more (see console for details) ...";

            var actionType = version.HasTag(PackageTag.BuiltIn) ? "disable" : "remove";
            message += $"\n\nYou will need to remove or disable them before being able to {actionType} this {packageType}.";

            return message;
        }

        private void RemoveClick()
        {
            var roots = PackageDatabase.instance.GetDependentVersions(displayVersion).Where(p => p.isDirectDependency && p.isInstalled).ToList();
            // Only show this message on a package if it is installed by dependency only. This allows it to still be removed from the installed list.
            var showDialog = roots.Any() && !(!displayVersion.HasTag(PackageTag.BuiltIn) && displayVersion.isDirectDependency);
            if (showDialog)
            {
                if (roots.Count > MaxDependentList)
                    Debug.Log(GetDependentMessage(displayVersion, roots, int.MaxValue));

                var message = GetDependentMessage(displayVersion, roots);
                var title = displayVersion.HasTag(PackageTag.BuiltIn) ? "Cannot disable built-in package" : "Cannot remove dependent package";
                EditorUtility.DisplayDialog(title, message, "Ok");

                return;
            }

            if (displayVersion.HasTag(PackageTag.InDevelopment))
            {
                if (!EditorUtility.DisplayDialog("Unity Package Manager", "You will loose all your changes (if any) if you delete a package in development. Are you sure?", "Yes", "No"))
                    return;

                detailError.ClearError();
                PackageDatabase.instance.RemoveEmbedded(package);
                RefreshPackageActionButtons();

                PackageManagerWindowAnalytics.SendEvent("removeEmbedded", displayVersion.uniqueId);

                EditorApplication.delayCall += () =>
                {
                    PackageFilterTab? newFilterTab = null;
                    if (PackageFiltering.instance.currentFilterTab == PackageFilterTab.InDevelopment)
                    {
                        var hasOtherInDevelopment = PackageDatabase.instance.allPackages.Any(p =>
                        {
                            var installed = p.installedVersion;
                            return installed != null && installed.HasTag(PackageTag.InDevelopment) && p.uniqueId != package.uniqueId;
                        });
                        newFilterTab = hasOtherInDevelopment ? PackageFilterTab.InDevelopment : PackageFilterTab.Local;
                    }
                    PackageManagerWindow.SelectPackageAndFilter(displayVersion.uniqueId, newFilterTab, true);
                };
                return;
            }

            var result = 0;
            if (displayVersion.HasTag(PackageTag.BuiltIn))
            {
                if (!PackageManagerPrefs.instance.skipDisableConfirmation)
                {
                    result = EditorUtility.DisplayDialogComplex("Disable Built-In Package",
                        "Are you sure you want to disable this built-in package?",
                        "Disable", "Cancel", "Never ask");
                }
            }
            else
            {
                if (!PackageManagerPrefs.instance.skipRemoveConfirmation)
                {
                    result = EditorUtility.DisplayDialogComplex("Removing Package",
                        "Are you sure you want to remove this package?",
                        "Remove", "Cancel", "Never ask");
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
                EditorUtility.DisplayDialog("Unity Package Manager", "This package is not available offline.", "Ok");
                return;
            }
            var offlineUrl = getUrl(version, true);
            if (!string.IsNullOrEmpty(offlineUrl))
                ApplicationUtil.instance.OpenURL(offlineUrl);
            else
                EditorUtility.DisplayDialog("Unity Package Manager", messageOnNotFound, "Ok");
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
            ViewUrl(displayVersion, UpmPackageDocs.GetDocumentationUrl, "This package does not contain offline documentation.");
        }

        private void ViewChangelogClick()
        {
            ViewUrl(displayVersion, UpmPackageDocs.GetChangelogUrl, "This package does not contain offline changelog.");
        }

        private void ViewLicensesClick()
        {
            ViewUrl(displayVersion, UpmPackageDocs.GetLicensesUrl, "This package does not contain offline licenses.");
        }

        private void ImportClick()
        {
            PackageDatabase.instance.Import(package);
            RefreshImportAndDownloadButtons();

            PackageManagerWindowAnalytics.SendEvent("import", package.uniqueId);
        }

        private void DownloadOrCancelClick()
        {
            if (!ApplicationUtil.instance.isInternetReachable)
            {
                detailError.SetError(new Error(NativeErrorCode.Unknown, "No internet connection"));
                return;
            }

            detailError.ClearError();
            var downloadInProgress = PackageDatabase.instance.IsDownloadInProgress(displayVersion);
            if (downloadInProgress)
                PackageDatabase.instance.AbortDownload(package);
            else
                PackageDatabase.instance.Download(package);

            RefreshImportAndDownloadButtons();

            var eventName = downloadInProgress ? "abortDownload" : "startDownload";
            PackageManagerWindowAnalytics.SendEvent(eventName, package.uniqueId);
        }

        private VisualElementCache cache { get; set; }

        private VisualElement detailDescContainer { get { return cache.Get<VisualElement>("detailDescContainer"); } }
        private VisualElement detailNameContainer { get { return cache.Get<VisualElement>("detailNameContainer"); } }
        private Label detailName { get { return cache.Get<Label>("detailName"); } }
        private Label detailDesc { get { return cache.Get<Label>("detailDesc"); } }
        private Button detailDescMore { get { return cache.Get<Button>("detailDescMore"); } }
        private Button detailDescLess { get { return cache.Get<Button>("detailDescLess"); } }
        private VisualElement detailLinksContainer { get { return cache.Get<VisualElement>("detailLinksContainer"); } }
        private VisualElement detailLinks { get { return cache.Get<VisualElement>("detailLinks"); } }
        internal Alert detailError { get { return cache.Get<Alert>("detailError"); } }
        private ScrollView detailScrollView { get { return cache.Get<ScrollView>("detailScrollView"); } }
        private VisualElement detailContainer { get { return cache.Get<VisualElement>("detail"); } }
        private Label detailTitle { get { return cache.Get<Label>("detailTitle"); } }
        private Label detailVersion { get { return cache.Get<Label>("detailVersion"); } }
        private VisualElement detailDateContainer { get { return cache.Get<VisualElement>("detailDateContainer"); } }
        private Label detailDate { get { return cache.Get<Label>("detailDate"); } }
        private VisualElement detailAuthorContainer { get { return cache.Get<VisualElement>("detailAuthorContainer"); } }
        private Label detailAuthorText { get { return cache.Get<Label>("detailAuthorText"); } }
        private Button detailAuthorLink { get { return cache.Get<Button>("detailAuthorLink"); } }
        private VisualElement detailRegistryContainer { get { return cache.Get<VisualElement>("detailRegistryContainer"); } }
        private VisualElement detailRegistryWarning { get { return cache.Get<VisualElement>("detailRegistryWarning"); } }
        private Button detailWarningLink { get { return cache.Get<Button>("detailWarningLink"); } }
        private Label detailRegistryName { get { return cache.Get<Label>("detailRegistryName"); } }
        private VisualElement customContainer { get { return cache.Get<VisualElement>("detailCustomContainer"); } }
        private PackageSampleList sampleList { get { return cache.Get<PackageSampleList>("detailSampleList"); } }
        private PackageDependencies dependencies { get {return cache.Get<PackageDependencies>("detailDependencies");} }
        internal VisualElement packageToolbarContainer { get {return cache.Get<VisualElement>("toolbarContainer");} }
        private VisualElement packageToolbarLeftArea { get {return cache.Get<VisualElement>("leftItems");} }
        internal Button updateButton { get { return cache.Get<Button>("update"); } }
        internal Button removeButton { get { return cache.Get<Button>("remove"); } }
        private Button importButton { get { return cache.Get<Button>("import"); } }
        private Button downloadButton { get { return cache.Get<Button>("download"); } }
        private Button editButton { get { return cache.Get<Button>("editButton"); } }
        private ProgressBar downloadProgress { get { return cache.Get<ProgressBar>("downloadProgress"); } }
        private VisualElement detailCategories { get { return cache.Get<VisualElement>("detailCategories"); } }
        private VisualElement detailUnityVersionsContainer { get { return cache.Get<VisualElement>("detailUnityVersionsContainer"); } }
        private Label detailUnityVersions { get { return cache.Get<Label>("detailUnityVersions"); } }
        private VisualElement detailSizesContainer { get { return cache.Get<VisualElement>("detailSizesContainer"); } }
        private VisualElement detailSizes { get { return cache.Get<VisualElement>("detailSizes"); } }
        private VisualElement detailImagesContainer { get { return cache.Get<VisualElement>("detailImagesContainer"); } }
        private VisualElement detailImages { get { return cache.Get<VisualElement>("detailImages"); } }
        internal Label GetTagLabel(string tag) { return cache.Get<Label>("tag" + tag); }
    }
}
