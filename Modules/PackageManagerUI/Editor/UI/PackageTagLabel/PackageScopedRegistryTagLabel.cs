// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageScopedRegistryTagLabel : PackageBaseTagLabel
    {
        public PackageScopedRegistryTagLabel()
        {
            name = "tagScopedRegistry";
        }

        private bool IsVisible(IPackageVersion version)
        {
            return (version as UpmPackageVersion)?.HasTag(PackageTag.Unity) == false && !string.IsNullOrEmpty(version.version?.Prerelease);
        }

        public override void Refresh(IPackageVersion version)
        {
            var visible = IsVisible(version);
            if (visible)
            {
                tooltip = version.version?.Prerelease;
                text = version.version?.Prerelease;
            }
            UIUtils.SetElementDisplay(this, visible);
        }
    }
}
