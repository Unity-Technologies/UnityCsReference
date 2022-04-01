// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageTagLabel : Label
    {
        internal new class UxmlFactory : UxmlFactory<PackageTagLabel, UxmlTraits> {}
        internal new class UxmlTraits : TextElement.UxmlTraits {}

        private PackageTag m_Tag;
        public PackageTag tag => m_Tag;

        public void Refresh(IPackageVersion version, bool isVersionItem = false)
        {
            if (m_Tag != PackageTag.None)
                RemoveFromClassList(m_Tag.ToString());

            text = string.Empty;
            tooltip = string.Empty;
            m_Tag = PackageTag.None;

            if (version != null)
            {
                if (version.HasTag(PackageTag.Custom))
                {
                    text = L10n.Tr("Custom");
                    m_Tag = PackageTag.Custom;
                }
                else if (version.HasTag(PackageTag.PreRelease))
                {
                    text = L10n.Tr("Pre");
                    tooltip = L10n.Tr("Pre-release");
                    m_Tag = PackageTag.PreRelease;
                }
                else if (isVersionItem && version.HasTag(PackageTag.Release))
                {
                    text = L10n.Tr("R");
                    tooltip = L10n.Tr("Release");
                    m_Tag = PackageTag.Release;
                }
                else if (version.HasTag(PackageTag.Experimental))
                {
                    text = L10n.Tr("Exp");
                    tooltip = L10n.Tr("Experimental");
                    m_Tag = PackageTag.Experimental;
                }
                else if (version.HasTag(PackageTag.ReleaseCandidate))
                {
                    text = L10n.Tr("RC");
                    tooltip = L10n.Tr("Release Candidate");
                    m_Tag = PackageTag.ReleaseCandidate;
                }
            }

            if (m_Tag != PackageTag.None)
                AddToClassList(m_Tag.ToString());
        }

        public static PackageTagLabel CreateTagLabel(IPackageVersion version, bool isVersionItem = false)
        {
            var tagLabel = new PackageTagLabel();
            tagLabel.Refresh(version, isVersionItem);
            return tagLabel.m_Tag != PackageTag.None ? tagLabel : null;
        }
    }
}
