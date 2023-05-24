// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageUpmChangelogLink : PackageLink
    {
        public PackageUpmChangelogLink(IPackageVersion version) : base(version)
        {
        }

        public override bool isVisible => version != null && version.HasTag(PackageTag.UpmFormat) && !version.HasTag(PackageTag.Feature | PackageTag.BuiltIn);
        public override bool isEnabled => !isEmpty;
        public override ContextMenuAction[] contextMenuActions => new ContextMenuAction[] { ContextMenuAction.OpenInBrowser, ContextMenuAction.OpenLocally };

        public override string tooltip
        {
            get
            {
                if (isEnabled)
                    return L10n.Tr("Right click to see viewing options: in browser or local.");
                else if (version?.isInstalled != true && version?.package.product != null)
                    return L10n.Tr("Install to view changelog");
                else
                    return L10n.Tr("Changelog unavailable");
            }
        }
    }
}
