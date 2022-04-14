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
            return version?.package?.Is(PackageType.AssetStore) == true;
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

            if (version?.package?.firstPublishedDate != null)
            {
                var latest = version.package.versions.latest;
                if (latest != version)
                    m_ReleasesContainer.Add(new PackageReleaseDetailsItem(latest.versionString, latest.publishedDate, false, latest.releaseNotes));

                m_ReleasesContainer.Add(new PackageReleaseDetailsItem(version.versionString, version.publishedDate, version is AssetStorePackageVersion, version.releaseNotes));
                m_ReleasesContainer.Add(new PackageReleaseDetailsItem(L10n.Tr("Original"), version.package.firstPublishedDate));
            }
        }
    }
}
