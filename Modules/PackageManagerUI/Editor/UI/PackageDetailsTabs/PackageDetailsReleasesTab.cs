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

        public override bool IsValid(IPackageVersion version)
        {
            var package = version?.package;
            return package != null && package.Is(PackageType.AssetStore) && !package.Is(PackageType.Upm);
        }

        public PackageDetailsReleasesTab()
        {
            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Releases");

            m_ReleasesContainer = new VisualElement { name = "releasesContainer" };
            Add(m_ReleasesContainer);
        }

        public override void Refresh(IPackageVersion version)
        {
            m_ReleasesContainer.Clear();

            if (version is PlaceholderPackageVersion)
                return;

            if (version?.package?.firstPublishedDate != null)
            {
                var latest = version.package.versions.latest;
                m_ReleasesContainer.Add(new PackageReleaseDetailsItem(latest.versionString, latest.publishedDate, latest == version, latest.package.latestReleaseNotes));
                if (latest != version)
                    m_ReleasesContainer.Add(new PackageReleaseDetailsItem(version.versionString, version.publishedDate, true, version.localReleaseNotes));
                m_ReleasesContainer.Add(new PackageReleaseDetailsItem(L10n.Tr("Original"), version.package.firstPublishedDate));
            }
        }
    }
}
