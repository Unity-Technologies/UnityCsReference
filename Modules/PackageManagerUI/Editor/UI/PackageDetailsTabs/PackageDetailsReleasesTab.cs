// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsReleasesTab : PackageDetailsTabElement
    {
        public const string k_Id = "releases";

        private readonly VisualElement m_ReleasesContainer;

        protected override bool requiresUserSignIn => true;

        public override bool IsValid(IPackageVersion version)
        {
            return version != null && version.package.product != null && !version.HasTag(PackageTag.UpmFormat);
        }

        public PackageDetailsReleasesTab(IUnityConnectProxy unityConnect) : base(unityConnect)
        {
            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Releases");

            m_ReleasesContainer = new VisualElement { name = "releasesContainer" };
            m_ContentContainer.Add(m_ReleasesContainer);
        }

        protected override void RefreshContent(IPackageVersion version)
        {
            m_ReleasesContainer.Clear();

            if (version is PlaceholderPackageVersion)
                return;

            if (version?.package.product?.firstPublishedDate != null)
            {
                var latest = version.package.versions.latest;
                m_ReleasesContainer.Add(new PackageReleaseDetailsItem(latest.versionString, latest.publishedDate, latest == version, latest.package.product.latestReleaseNotes));
                if (latest != version)
                    m_ReleasesContainer.Add(new PackageReleaseDetailsItem(version.versionString, version.publishedDate, true, version.localReleaseNotes));
                m_ReleasesContainer.Add(new PackageReleaseDetailsItem(L10n.Tr("Original"), version.package.product.firstPublishedDate));
            }
        }
    }
}
