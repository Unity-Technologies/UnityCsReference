// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsOverviewTab : PackageDetailsTabElement
    {
        public const string k_Id = "overview";

        private readonly PackageDetailsOverviewTabContent m_Content;

        protected override bool requiresUserSignIn => true;

        public PackageDetailsOverviewTab(IUnityConnectProxy unityConnect, IResourceLoader resourceLoader) : base(unityConnect)
        {
            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Overview");

            m_Content = new PackageDetailsOverviewTabContent(resourceLoader);
            m_ContentContainer.Add(m_Content);
        }

        public override bool IsValid(IPackageVersion version)
        {
            return version != null && version.package.product != null && !version.HasTag(PackageTag.UpmFormat);
        }

        protected override void RefreshContent(IPackageVersion version)
        {
            m_Content.Refresh(version);
        }
    }
}
