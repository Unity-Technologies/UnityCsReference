// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsDetailsTab : PackageDetailsTabElement
    {
        public const string k_Id = "description";

        private const string k_EmptyDescriptionClass = "empty";
        private const int k_MaxDescriptionCharacters = 10000;

        public override bool IsValid(IPackageVersion version)
        {
            return version?.HasTag(PackageTag.UpmFormat) == true;
        }

        private readonly IApplicationProxy m_ApplicationProxy;
        private readonly IUpmCache m_UpmCache;
        public PackageDetailsDetailsTab(IUnityConnectProxy unityConnect, IResourceLoader resourceLoader, IApplicationProxy applicationProxy, IUpmCache upmCache) : base(unityConnect)
        {
            m_ApplicationProxy = applicationProxy;
            m_UpmCache = upmCache;

            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Details");
            var root = resourceLoader.GetTemplate("DetailsTabs/PackageDetailsDetailsTab.uxml");
            m_ContentContainer.Add(root);
            m_Cache = new VisualElementCache(root);
            AddInformationCards();
        }

        private void AddInformationCards()
        {
            detailInformationCardsContainer.Add(new TechnicalNameInfoCard(m_ApplicationProxy));
            detailInformationCardsContainer.Add(new SourceInfoCard(m_UpmCache));
            detailInformationCardsContainer.Add(new SignatureInfoCard());
            detailInformationCardsContainer.Add(new MinimumEditorVersionInfoCard());
            detailInformationCardsContainer.Add(new PackageStateInfoCard(m_ApplicationProxy));
            detailInformationCardsContainer.Add(new InstalledFromInfoCard(m_ApplicationProxy));
        }

        protected override void RefreshContent(IPackageVersion version)
        {
            RefreshInformationCards(version);
            packagePlatformList.Refresh(version);
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

        private void RefreshDescription(IPackageVersion version)
        {
            var hasVersionDescription = !string.IsNullOrEmpty(version.description);
            var desc = hasVersionDescription ? version.description : L10n.Tr("There is no description for this package.");
            if (desc.Length > k_MaxDescriptionCharacters)
                desc = desc.Substring(0, k_MaxDescriptionCharacters);
            detailDescription.EnableInClassList(k_EmptyDescriptionClass, !hasVersionDescription);
            detailDescription.text = desc;
        }

        private readonly VisualElementCache m_Cache;
        private VisualElement detailInformationCardsContainer => m_Cache.Get<VisualElement>("detailInformationCardsContainer");
        private PackagePlatformList packagePlatformList => m_Cache.Get<PackagePlatformList>("detailPlatformList");
        private SelectableLabel detailDescription => m_Cache.Get<SelectableLabel>("detailDescription");
    }
}
