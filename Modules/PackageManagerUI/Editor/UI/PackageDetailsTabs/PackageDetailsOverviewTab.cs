// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsOverviewTab : PackageDetailsTabElement
    {
        public const string k_Id = "overview";

        private PackageDetailsOverviewTabContent m_Content;

        public PackageDetailsOverviewTab(ResourceLoader resourceLoader)
        {
            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Overview");

            m_Content = new PackageDetailsOverviewTabContent(resourceLoader);
            Add(m_Content);
        }

        public override bool IsValid(IPackageVersion version)
        {
            return version != null && version.package.product != null && !version.HasTag(PackageTag.UpmFormat);
        }

        public override void Refresh(IPackageVersion version)
        {
            m_Content.Refresh(version);
        }
    }
}
