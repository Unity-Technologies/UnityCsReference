// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageTagLabel : Label
    {
        internal new class UxmlFactory : UxmlFactory<PackageTagLabel, UxmlTraits> {}
        internal new class UxmlTraits : TextElement.UxmlTraits {}

        private PackageTagLabel(string text, string tooltipText, PackageTag tag = PackageTag.None)
            : base(text)
        {
            AddToClassList(tag.ToString());
            tooltip = tooltipText;
        }

        public PackageTagLabel()
        {
        }

        public static PackageTagLabel CreateTagLabel(IPackageVersion version, bool isVersionItem = false)
        {
            // check with version.packageInfo.UnityLifecycle
            if (version != null)
            {
                if (version.HasTag(PackageTag.Custom))
                    return new PackageTagLabel(L10n.Tr("Custom"), L10n.Tr("Custom"), PackageTag.Custom);
                if (version.HasTag(PackageTag.PreRelease))
                    return new PackageTagLabel(L10n.Tr("Pre"), L10n.Tr("Pre-release"), PackageTag.PreRelease);
                if (isVersionItem && version.HasTag(PackageTag.Release))
                    return new PackageTagLabel(L10n.Tr("R"), L10n.Tr("Release"), PackageTag.Release);
                if (version.HasTag(PackageTag.Experimental))
                    return new PackageTagLabel(L10n.Tr("Exp"), L10n.Tr("Experimental"), PackageTag.Experimental);
                if (isVersionItem && version.HasTag(PackageTag.ReleaseCandidate))
                    return new PackageTagLabel(L10n.Tr("RC"), L10n.Tr("Release Candidate"), PackageTag.ReleaseCandidate);
            }
            return null;
        }
    }
}
