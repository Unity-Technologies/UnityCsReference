// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageReleaseTagLabel : PackageBaseTagLabel
    {
        private readonly IPackageDatabase m_PackageDatabase;
        public PackageReleaseTagLabel(IPackageDatabase packageDatabase)
        {
            m_PackageDatabase = packageDatabase;
            name = "tagRelease";
            text = L10n.Tr("Release");
        }

        private bool IsVisible(IPackageVersion version)
        {
            if (!version.HasTag(PackageTag.Release))
                return false;
            if (version.HasTag(PackageTag.Feature))
                return version.dependencies != null && version.dependencies.All(d => m_PackageDatabase.GetPackage(d.name)?.versions.recommended?.HasTag(PackageTag.Release) == true);
            return true;
        }

        public override void Refresh(IPackageVersion version)
        {
            var isVisible = IsVisible(version);
            UIUtils.SetElementDisplay(this, isVisible);
            EnableInClassList(PackageTag.Release.ToString(), isVisible);
        }
    }
}
