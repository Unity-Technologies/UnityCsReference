// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsOverviewTab : PackageDetailsTabElement
    {
        private const string k_Id = "overview";

        private const string k_EmptyDescriptionClass = "empty";
        private const int k_maxDescriptionCharacters = 10000;

        protected override bool requiresUserSignIn => true;

        public PackageDetailsOverviewTab(IUnityConnectProxy unityConnect, IResourceLoader resourceLoader) : base(unityConnect)
        {
            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Overview");

            name = "packageOverviewContent";
            var root = resourceLoader.GetTemplate("DetailsTabs/PackageDetailsOverviewTab.uxml");
            m_ContentContainer.Add(root);
            m_Cache = new VisualElementCache(root);
        }

        public override bool IsValid(IPackageVersion version)
        {
            return version != null && version.package.product != null;
        }

        protected override void RefreshContent(IPackageVersion version)
        {
            RefreshLabels(version);
            RefreshSupportedUnityVersions(version);
            RefreshSizeInfo(version);
            RefreshPurchasedDate(version);
            RefreshDescription(version);
        }

        private void RefreshLabels(IPackageVersion version)
        {
            var labels = version?.package.product?.labels;
            var hasLabels = labels?.Count > 0;
            if (hasLabels)
                assignedLabelList.Refresh(labels.Count > 1 ? L10n.Tr("Assigned Labels") : L10n.Tr("Assigned Label"), labels);
            UIUtils.SetElementDisplay(assignedLabelList, hasLabels);
        }

        private void RefreshSupportedUnityVersions(IPackageVersion version)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var supportedVersion = version.supportedVersions?.Count > 0 ? version.supportedVersions.FirstOrDefault() : version.supportedVersion;
#pragma warning restore RS0030
            var hasSupportedVersions = supportedVersion != null;
            if (hasSupportedVersions)
            {
                detailUnityVersions.text = string.Format(L10n.Tr("{0} or higher"), supportedVersion);

                var tooltip = supportedVersion.ToString();
                if (version.supportedVersions?.Count > 0)
                {
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var versions = version.supportedVersions.Select(version => version.ToString()).ToArray();
#pragma warning restore RS0030
                    tooltip = versions.Length == 1 ? versions[0] :
                        string.Format(L10n.Tr("{0} and {1} to improve compatibility with the range of these versions of Unity"), string.Join(", ", versions, 0, versions.Length - 1), versions[versions.Length - 1]);
                }
                detailUnityVersions.tooltip = string.Format(L10n.Tr("Package has been submitted using Unity {0}"), tooltip);
            }
            UIUtils.SetElementDisplay(detailUnityVersionsContainer, hasSupportedVersions);
        }

        private void RefreshSizeInfo(IPackageVersion version)
        {
            var showSizes = version.sizes.Count > 0;
            if (showSizes)
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var sizeInfo = version.sizes.FirstOrDefault(info => info.supportedUnityVersion == version.supportedVersion) ?? version.sizes.Last();
#pragma warning restore RS0030
                detailSizes.text = string.Format(L10n.Tr("Size: {0} (Number of files: {1})"), UIUtils.ConvertToHumanReadableSize(sizeInfo.downloadSize), sizeInfo.assetCount);
            }
            UIUtils.SetElementDisplay(detailSizesContainer, showSizes);
        }

        private void RefreshPurchasedDate(IPackageVersion version)
        {
            detailPurchasedDate.text = version.package.product?.purchasedTime?.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("en-US")) ?? string.Empty;
            UIUtils.SetElementDisplay(detailPurchasedDateContainer, !string.IsNullOrEmpty(detailPurchasedDate.text));
        }

        private void RefreshDescription(IPackageVersion version)
        {
            var productDescription = version.package.product?.description;
            var hasProductDescription = !string.IsNullOrEmpty(productDescription);
            var desc = hasProductDescription ? productDescription : L10n.Tr("There is no description for this package.");
            if (desc.Length > k_maxDescriptionCharacters)
                desc = desc.Substring(0, k_maxDescriptionCharacters);
            detailDescription.EnableInClassList(k_EmptyDescriptionClass, !hasProductDescription);
            detailDescription.text = desc;
        }

        private readonly VisualElementCache m_Cache;

        private TagLabelList assignedLabelList => m_Cache.Get<TagLabelList>("assignedLabelList");
        private VisualElement detailUnityVersionsContainer => m_Cache.Get<VisualElement>("detailUnityVersionsContainer");
        private SelectableLabel detailUnityVersions => m_Cache.Get<SelectableLabel>("detailUnityVersions");
        private VisualElement detailSizesContainer => m_Cache.Get<VisualElement>("detailSizesContainer");
        private SelectableLabel detailSizes => m_Cache.Get<SelectableLabel>("detailSizes");
        private VisualElement detailPurchasedDateContainer => m_Cache.Get<VisualElement>("detailPurchasedDateContainer");
        private SelectableLabel detailPurchasedDate => m_Cache.Get<SelectableLabel>("detailPurchasedDate");
        private SelectableLabel detailDescription => m_Cache.Get<SelectableLabel>("detailDescription");
    }
}
