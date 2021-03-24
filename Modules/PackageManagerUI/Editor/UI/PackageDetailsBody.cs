// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsBody : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageDetailsBody> {}

        private const string k_EmptyDescriptionClass = "empty";

        internal enum InfoBoxState
        {
            PreRelease,
            Experimental,
            ReleaseCandidate,
            ScopedRegistry
        }

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

        private string infoBoxUrl => $"https://docs.unity3d.com/{m_Application?.shortUnityVersion}";

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
            detailDesc.RegisterCallback<GeometryChangedEvent>(DescriptionGeometryChangeEvent);
            scopedRegistryInfoBox.Q<Button>().clickable.clicked += OnInfoBoxClickMore;
        }

        public void OnEnable()
        {
            m_PackageDatabase.onPackagesChanged += (added, removed, preUpdate, postUpdate) => RefreshDependencies();
            m_SettingsProxy.onEnablePackageDependenciesChanged += (value) => RefreshDependencies();
        }

        public void OnDisable()
        {
            detailsImages.OnDisable();
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
            RefreshRegistry();
            RefreshReleaseDetails();
            RefreshSizeAndSupportedUnityVersions();
            RefreshPurchasedDate();
            RefreshSourcePath();
        }

        private void RefreshDependencies()
        {
            dependencies.SetPackageVersion(m_Version);
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

        private void OnInfoBoxClickMore()
        {
            if (m_Version.HasTag(PackageTag.PreRelease))
                m_Application.OpenURL($"{infoBoxUrl}{k_InfoBoxReadMoreUrl[(int)InfoBoxState.PreRelease]}");
            else if (m_Version.HasTag(PackageTag.Experimental))
                m_Application.OpenURL($"{infoBoxUrl}{k_InfoBoxReadMoreUrl[(int)InfoBoxState.Experimental]}");
            else if (m_Version.HasTag(PackageTag.ReleaseCandidate))
                m_Application.OpenURL($"{infoBoxUrl}{k_InfoBoxReadMoreUrl[(int)InfoBoxState.ReleaseCandidate]}");
            else if (m_Package.Is(PackageType.ScopedRegistry))
                m_Application.OpenURL($"{infoBoxUrl}{k_InfoBoxReadMoreUrl[(int)InfoBoxState.ScopedRegistry]}");
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
            detailDesc.style.maxHeight = (int)detailDesc.MeasureTextSize("|", 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).y * 3 + 5;
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

            var minTextHeight = (int)detailDesc.MeasureTextSize("|", 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).y * 3 + 1;
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
            var hasDescription = !string.IsNullOrEmpty(m_Version.description);
            detailDesc.EnableInClassList(k_EmptyDescriptionClass, !hasDescription);
            detailDesc.style.maxHeight = int.MaxValue;
            detailDesc.SetValueWithoutNotify(hasDescription ? m_Version.description : L10n.Tr("There is no description for this package."));
            UIUtils.SetElementDisplay(detailDescMore, false);
            UIUtils.SetElementDisplay(detailDescLess, false);
            m_DescriptionExpanded = !m_Package.Is(PackageType.AssetStore);
        }

        private void RefreshRegistry()
        {
            var registry = m_Version.packageInfo?.registry;
            var showRegistry = registry != null;
            UIUtils.SetElementDisplay(detailRegistryContainer, showRegistry);
            if (showRegistry)
            {
                scopedRegistryInfoBox.text = k_InfoBoxReadMoreText[(int)InfoBoxState.ScopedRegistry];
                UIUtils.SetElementDisplay(scopedRegistryInfoBox, !registry.isDefault);
                detailRegistryName.text = registry.isDefault ? "Unity" : registry.name;
                detailRegistryName.tooltip = registry.url;
            }
            if (m_Version.HasTag(PackageTag.Experimental))
            {
                scopedRegistryInfoBox.text = k_InfoBoxReadMoreText[(int)InfoBoxState.Experimental];
                UIUtils.SetElementDisplay(scopedRegistryInfoBox, true);
            }
            else if (m_Version.HasTag(PackageTag.PreRelease))
            {
                scopedRegistryInfoBox.text = k_InfoBoxReadMoreText[(int)InfoBoxState.PreRelease];
                UIUtils.SetElementDisplay(scopedRegistryInfoBox, true);
            }
            else if (m_Version.HasTag(PackageTag.ReleaseCandidate))
            {
                scopedRegistryInfoBox.text = k_InfoBoxReadMoreText[(int)InfoBoxState.ReleaseCandidate];
                UIUtils.SetElementDisplay(scopedRegistryInfoBox, true);
            }
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
                    m_Version.publishedDate, m_Version.releaseNotes));

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

        private SelectableLabel detailDesc => cache.Get<SelectableLabel>("detailDesc");
        private Button detailDescMore => cache.Get<Button>("detailDescMore");
        private Button detailDescLess => cache.Get<Button>("detailDescLess");

        private VisualElement detailRegistryContainer => cache.Get<VisualElement>("detailRegistryContainer");
        private HelpBox scopedRegistryInfoBox => cache.Get<HelpBox>("scopedRegistryInfoBox");
        private Label detailRegistryName => cache.Get<Label>("detailRegistryName");

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
    }
}
