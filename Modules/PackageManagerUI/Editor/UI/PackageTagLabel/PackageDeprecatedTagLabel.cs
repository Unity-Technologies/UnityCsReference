// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDeprecatedTagLabel : PackageBaseTagLabel
    {
        public PackageDeprecatedTagLabel()
        {
            name = "tagDeprecated";
            text = L10n.Tr("Deprecated");
        }

        public override void Refresh(IPackageVersion version)
        {
            var showDeprecatedVersion = version.HasTag(PackageTag.Deprecated);
            var showDeprecatedPackage = version.package.isDeprecated;
            var visible = showDeprecatedVersion || showDeprecatedPackage;
            UIUtils.SetElementDisplay(this, visible);

            if (!visible)
                return;

            EnableInClassList("DeprecatedVersion", showDeprecatedVersion);
            EnableInClassList("DeprecatedPackage", showDeprecatedPackage);
        }
    }
}
