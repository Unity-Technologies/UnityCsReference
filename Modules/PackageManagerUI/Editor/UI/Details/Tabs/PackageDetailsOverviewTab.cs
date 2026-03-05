// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsOverviewTab : PackageDetailsTabElement
    {
        private const string k_Id = "overview";

        private const string k_EmptyDescriptionClass = "empty";
        private const int k_MaxDescriptionCharacters = 10000;

        protected override bool requiresUserSignIn => true;

        public PackageDetailsOverviewTab(IUnityConnectProxy unityConnect, IResourceLoader resourceLoader, IUpmCache upmCache) : base(unityConnect)
        {
            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Overview");

            name = "packageOverviewContent";
            var root = resourceLoader.GetTemplate("DetailsTabs/PackageDetailsOverviewTab.uxml");
            m_ContentContainer.Add(root);
            m_Cache = new VisualElementCache(root);

            detailInformationCardsContainer.Add(new SourceInfoCard(upmCache));
            detailInformationCardsContainer.Add(new OriginalUnityVersionInfoCard());
            detailInformationCardsContainer.Add(new PurchaseDateInfoCard());
            detailInformationCardsContainer.Add(new PackageSizeInfoCard());
        }

        public override bool IsValid(IPackageVersion version)
        {
            return version?.package.product != null;
        }

        protected override void RefreshContent(IPackageVersion version)
        {
            RefreshInformationCards(version);
            RefreshLabels(version);
            RefreshDescription(version);
        }

        private void RefreshInformationCards(IPackageVersion version)
        {
            foreach (var child in detailInformationCardsContainer.Children())
            {
                if (child is PackageInformationCard card)
                    card.Refresh(version);
            }
        }

        private void RefreshLabels(IPackageVersion version)
        {
            var labels = version?.package.product?.labels;
            var hasLabels = labels?.Count > 0;
            if (hasLabels)
                assignedLabelList.Refresh(labels.Count > 1 ? L10n.Tr("Assigned Labels") : L10n.Tr("Assigned Label"), labels);
            UIUtils.SetElementDisplay(assignedLabelList, hasLabels);
        }

        private void RefreshDescription(IPackageVersion version)
        {
            var productDescription = version.package.product?.description;
            var hasProductDescription = !string.IsNullOrEmpty(productDescription);
            var desc = hasProductDescription ? productDescription : L10n.Tr("There is no description for this package.");
            if (desc.Length > k_MaxDescriptionCharacters)
                desc = desc.Substring(0, k_MaxDescriptionCharacters);
            detailDescription.EnableInClassList(k_EmptyDescriptionClass, !hasProductDescription);
            detailDescription.text = desc;
        }

        private readonly VisualElementCache m_Cache;

        private VisualElement detailInformationCardsContainer  => m_Cache.Get<VisualElement>("detailInformationCardsContainer");
        private TagLabelList assignedLabelList => m_Cache.Get<TagLabelList>("assignedLabelList");
        private SelectableLabel detailDescription => m_Cache.Get<SelectableLabel>("detailDescription");
    }
}
