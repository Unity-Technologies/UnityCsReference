// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsBody : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageDetailsBody> {}

        private const string k_EmptyDescriptionClass = "empty";

        private ResourceLoader m_ResourceLoader;
        private ApplicationProxy m_Application;
        private PackageDatabase m_PackageDatabase;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
        }

        internal bool descriptionExpanded => m_DescriptionExpanded;
        private bool m_DescriptionExpanded;

        private IPackage m_Package;
        private IPackageVersion m_Version;

        public PackageDetailsBody()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageDetailsBody.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            detailDescMore.clickable.clicked += DescMoreClick;
            detailDescLess.clickable.clicked += DescLessClick;
            productDesc.RegisterCallback<GeometryChangedEvent>(DescriptionGeometryChangeEvent);
        }

        public void OnEnable()
        {
            m_PackageDatabase.onPackagesChanged += RefreshDependencies;
            m_SettingsProxy.onEnablePackageDependenciesChanged += RefreshDependencies;
        }

        public void OnDisable()
        {
            detailsImages.OnDisable();

            m_PackageDatabase.onPackagesChanged -= RefreshDependencies;
            m_SettingsProxy.onEnablePackageDependenciesChanged -= RefreshDependencies;
        }

        public void Refresh(IPackage package, IPackageVersion version)
        {
            m_Package = package;
            m_Version = version;

            detailsImages.Refresh(m_Package);
            sampleList.SetPackageVersion(m_Version);
            UIUtils.SetElementDisplay(disabledInfoBox, m_Version.HasTag(PackageTag.Disabled));

            RefreshDependencies();
            RefreshLabels();
            RefreshDescription();
            RefreshReleaseDetails();
            RefreshSizeAndSupportedUnityVersions();
            RefreshPurchasedDate();
            RefreshSourcePath();
        }

        private void RefreshDependencies()
        {
            featureDependencies.SetPackageVersion(m_Version);
            dependencies.SetPackageVersion(m_Version);
        }

        private void RefreshDependencies(bool _)
        {
            RefreshDependencies();
        }

        private void RefreshDependencies(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> preUpdate, IEnumerable<IPackage> postUpdate)
        {
            RefreshDependencies();
        }

        private void RefreshLabels()
        {
            detailLabels.Clear();

            if (enabledSelf && m_Package?.labels != null)
            {
                var labels = string.Join(", ", m_Package.labels.ToArray());

                if (!string.IsNullOrEmpty(labels))
                {
                    var label = new SelectableLabel();
                    label.SetValueWithoutNotify(labels);
                    detailLabels.Add(label);
                }
            }

            var hasLabels = detailLabels.Children().Any();
            var isAssetStorePackage = m_Package is AssetStorePackage;

            if (!hasLabels && isAssetStorePackage)
                detailLabels.Add(new Label(L10n.Tr("(None)")));

            UIUtils.SetElementDisplay(detailLabelsContainer, hasLabels || isAssetStorePackage);
        }

        private void DescMoreClick()
        {
            productDesc.style.maxHeight = float.MaxValue;
            UIUtils.SetElementDisplay(detailDescMore, false);
            UIUtils.SetElementDisplay(detailDescLess, true);
            m_DescriptionExpanded = true;
        }

        private void DescLessClick()
        {
            productDesc.style.maxHeight = (int)productDesc.MeasureTextSize("|", 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).y * 3 + 5;
            UIUtils.SetElementDisplay(detailDescMore, true);
            UIUtils.SetElementDisplay(detailDescLess, false);
            m_DescriptionExpanded = false;
        }

        private void DescriptionGeometryChangeEvent(GeometryChangedEvent evt)
        {
            if (m_Package == null || !m_Package.Is(PackageType.AssetStore))
            {
                UIUtils.SetElementDisplay(detailDescMore, false);
                UIUtils.SetElementDisplay(detailDescLess, false);
                return;
            }

            var minTextHeight = (int)productDesc.MeasureTextSize("|", 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).y * 3 + 1;
            var textHeight = (int)productDesc.MeasureTextSize(productDesc.text, evt.newRect.width, MeasureMode.AtMost, float.MaxValue, MeasureMode.Undefined).y + 1;
            if (!m_DescriptionExpanded && textHeight > minTextHeight)
            {
                UIUtils.SetElementDisplay(detailDescMore, true);
                UIUtils.SetElementDisplay(detailDescLess, false);
                productDesc.style.maxHeight = minTextHeight + 4;
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
            var showProductDesc = !string.IsNullOrEmpty(m_Package.productDescription);
            var showPackageDesc = !string.IsNullOrEmpty(m_Version.description);
            var showTitles = showProductDesc && showPackageDesc;
            var showEmptyDescription = !showProductDesc && !showPackageDesc;

            productDesc.SetValueWithoutNotify(m_Package.productDescription ?? string.Empty);
            UIUtils.SetElementDisplay(productDescTitle, showTitles);
            UIUtils.SetElementDisplay(productDesc, showProductDesc || showEmptyDescription);

            packageDesc.SetValueWithoutNotify(m_Version.description ?? string.Empty);
            UIUtils.SetElementDisplay(packageDescTitle, showTitles);
            UIUtils.SetElementDisplay(packageDesc, showPackageDesc);

            if (showEmptyDescription)
                productDesc.SetValueWithoutNotify(L10n.Tr("There is no description for this package."));
            productDesc.EnableInClassList(k_EmptyDescriptionClass, showEmptyDescription);

            productDesc.style.maxHeight = int.MaxValue;
            UIUtils.SetElementDisplay(detailDescMore, false);
            UIUtils.SetElementDisplay(detailDescLess, false);
            m_DescriptionExpanded = !m_Package.Is(PackageType.AssetStore);
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
                detailReleaseDetails.Add(new PackageReleaseDetailsItem($"{m_Version.versionString}{(m_Version is AssetStorePackageVersion ? " (Current)" : string.Empty)}",
                    m_Version.publishedDate, m_Version.localReleaseNotes));

                if (m_Package.firstPublishedDate != null)
                    detailReleaseDetails.Add(new PackageReleaseDetailsItem("Original", m_Package.firstPublishedDate, string.Empty));
            }

            UIUtils.SetElementDisplay(detailReleaseDetailsContainer, detailReleaseDetails.Children().Any());
        }

        private void RefreshSizeAndSupportedUnityVersions()
        {
            var showSupportedUnityVersions = RefreshSupportedUnityVersions();
            var showSize = RefreshSizeInfo();
            UIUtils.SetElementDisplay(detailSizesAndSupportedVersionsContainer, showSize || showSupportedUnityVersions);
        }

        private bool RefreshSupportedUnityVersions()
        {
            var hasSupportedVersions = (m_Version.supportedVersions?.Any() == true);
            var supportedVersion = m_Version.supportedVersions?.FirstOrDefault();

            if (!hasSupportedVersions)
            {
                supportedVersion = m_Version.supportedVersion;
                hasSupportedVersions = supportedVersion != null;
            }

            UIUtils.SetElementDisplay(detailUnityVersionsContainer, hasSupportedVersions);
            if (hasSupportedVersions)
            {
                detailUnityVersions.SetValueWithoutNotify(string.Format(L10n.Tr("{0} or higher"), supportedVersion));
                var tooltip = supportedVersion.ToString();
                if (m_Version.supportedVersions != null && m_Version.supportedVersions.Any())
                {
                    var versions = m_Version.supportedVersions.Select(version => version.ToString()).ToArray();
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
            var showSizes = m_Version.sizes.Any();
            UIUtils.SetElementDisplay(detailSizesContainer, showSizes);
            detailSizes.Clear();

            var sizeInfo = m_Version.sizes.FirstOrDefault(info => info.supportedUnityVersion == m_Version.supportedVersion);
            if (sizeInfo == null)
                sizeInfo = m_Version.sizes.LastOrDefault();

            if (sizeInfo != null)
            {
                var label = new SelectableLabel();
                label.style.whiteSpace = WhiteSpace.Normal;
                label.SetValueWithoutNotify(string.Format(L10n.Tr("Size: {0} (Number of files: {1})"), UIUtils.ConvertToHumanReadableSize(sizeInfo.downloadSize), sizeInfo.assetCount));
                detailSizes.Add(label);
            }

            return showSizes;
        }

        private void RefreshSourcePath()
        {
            var sourcePath = (m_Version as UpmPackageVersion)?.sourcePath;
            UIUtils.SetElementDisplay(detailSourcePathContainer, !string.IsNullOrEmpty(sourcePath));

            if (!string.IsNullOrEmpty(sourcePath))
                detailSourcePath.SetValueWithoutNotify(sourcePath);
        }

        private VisualElementCache cache { get; set; }
        private Label productDescTitle => cache.Get<Label>("productDescTitle");
        private SelectableLabel productDesc => cache.Get<SelectableLabel>("detailDesc");
        private Label packageDescTitle => cache.Get<Label>("packageDescTitle");
        private SelectableLabel packageDesc => cache.Get<SelectableLabel>("packageDesc");
        private Button detailDescMore => cache.Get<Button>("detailDescMore");
        private Button detailDescLess => cache.Get<Button>("detailDescLess");

        private HelpBox disabledInfoBox => cache.Get<HelpBox>("disabledInfoBox");

        private VisualElement detailSourcePathContainer => cache.Get<VisualElement>("detailSourcePathContainer");
        private SelectableLabel detailSourcePath => cache.Get<SelectableLabel>("detailSourcePath");

        private VisualElement detailSizesAndSupportedVersionsContainer => cache.Get<VisualElement>("detailSizesAndSupportedVersionsContainer");
        private VisualElement detailUnityVersionsContainer => cache.Get<VisualElement>("detailUnityVersionsContainer");
        private SelectableLabel detailUnityVersions => cache.Get<SelectableLabel>("detailUnityVersions");
        private VisualElement detailSizesContainer => cache.Get<VisualElement>("detailSizesContainer");
        private VisualElement detailSizes => cache.Get<VisualElement>("detailSizes");

        private VisualElement detailPurchasedDateContainer => cache.Get<VisualElement>("detailPurchasedDateContainer");
        private SelectableLabel detailPurchasedDate => cache.Get<SelectableLabel>("detailPurchasedDate");

        private VisualElement detailReleaseDetailsContainer => cache.Get<VisualElement>("detailReleaseDetailsContainer");
        private VisualElement detailReleaseDetails => cache.Get<VisualElement>("detailReleaseDetails");

        private VisualElement detailLabelsContainer => cache.Get<VisualElement>("detailLabelsContainer");
        private VisualElement detailLabels => cache.Get<VisualElement>("detailLabels");

        private PackageSampleList sampleList => cache.Get<PackageSampleList>("detailSampleList");
        private PackageDependencies dependencies => cache.Get<PackageDependencies>("detailDependencies");
        private PackageDetailsImages detailsImages => cache.Get<PackageDetailsImages>("detailImagesContainer");

        private FeatureDependencies featureDependencies => cache.Get<FeatureDependencies>("featureDependencies");
    }
}
