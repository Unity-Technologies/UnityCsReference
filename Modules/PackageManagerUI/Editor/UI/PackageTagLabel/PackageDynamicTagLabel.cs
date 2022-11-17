// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDynamicTagLabel : PackageBaseTagLabel
    {
        private PackageTag m_Tag;
        private bool m_IsVersionItem;
        public PackageDynamicTagLabel(bool isVersionItem = false)
        {
            m_IsVersionItem = isVersionItem;
            m_Tag = PackageTag.None;
            UIUtils.SetElementDisplay(this, false);
        }

        private void UpdateTag(PackageTag newTag)
        {
            var oldTag = m_Tag;
            m_Tag = newTag;
            if (oldTag == newTag)
                return;

            if (oldTag != PackageTag.None)
                RemoveFromClassList(oldTag.ToString());

            if (newTag != PackageTag.None)
                AddToClassList(newTag.ToString());

            UIUtils.SetElementDisplay(this, newTag != PackageTag.None);

            switch (newTag)
            {
                case PackageTag.Custom:
                    text = L10n.Tr("Custom");
                    tooltip = string.Empty;
                    break;
                case PackageTag.Deprecated:
                    text = L10n.Tr("D");
                    tooltip = L10n.Tr("Deprecated");
                    break;
                case PackageTag.PreRelease:
                    text = L10n.Tr("Pre");
                    tooltip = L10n.Tr("Pre-release");
                    break;
                case PackageTag.Release:
                    text = L10n.Tr("R");
                    tooltip = L10n.Tr("Release");
                    break;
                case PackageTag.Experimental:
                    text = L10n.Tr("Exp");
                    tooltip = L10n.Tr("Experimental");
                    break;
                case PackageTag.ReleaseCandidate:
                    text = L10n.Tr("RC");
                    tooltip = L10n.Tr("Release Candidate");
                    break;
                case PackageTag.None:
                default:
                    text = string.Empty;
                    tooltip = string.Empty;
                    break;
            }
        }

        public override void Refresh(IPackageVersion version)
        {
            if (version == null || version.HasTag(PackageTag.BuiltIn | PackageTag.LegacyFormat))
                UpdateTag(PackageTag.None);
            else if (version.HasTag(PackageTag.Custom))
                UpdateTag(PackageTag.Custom);
            else if (version.HasTag(PackageTag.Deprecated))
                // We don't want to see the Deprecated tag in the packageItem, but we also want to hide any other tags
                UpdateTag(m_IsVersionItem ? PackageTag.Deprecated : PackageTag.None);
            else if (version.HasTag(PackageTag.PreRelease))
                UpdateTag(PackageTag.PreRelease);
            else if (m_IsVersionItem && version.HasTag(PackageTag.Release))
                UpdateTag(PackageTag.Release);
            else if (version.HasTag(PackageTag.Experimental))
                UpdateTag(PackageTag.Experimental);
            else if (version.HasTag(PackageTag.ReleaseCandidate))
                UpdateTag(PackageTag.ReleaseCandidate);
            else
                UpdateTag(PackageTag.None);
        }
    }
}
