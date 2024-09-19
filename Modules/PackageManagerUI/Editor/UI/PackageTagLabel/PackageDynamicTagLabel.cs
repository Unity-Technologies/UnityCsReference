// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDynamicTagLabel : PackageBaseTagLabel
    {
        public const string k_DisableEllipsisClass = "disable-ellipsis";

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
                case PackageTag.Local:
                    text = L10n.Tr("Local");
                    tooltip = string.Empty;
                    break;
                case PackageTag.Git:
                    text = L10n.Tr("Git");
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
                case PackageTag.Experimental:
                    text = L10n.Tr("Exp");
                    tooltip = L10n.Tr("Experimental");
                    break;
                case PackageTag.None:
                default:
                    text = string.Empty;
                    tooltip = string.Empty;
                    break;
            }

            // Sometimes the UI Element layout engine would calculate the size to be 1px less than it should be, causing
            // the tag to show up as just ellipsis in some cases. We are adding the special handling here so that short
            // tags never show ellipsis
            EnableInClassList(k_DisableEllipsisClass, text.Length <= 3);
        }

        public override void Refresh(IPackageVersion version)
        {
            if (version == null || version.HasTag(PackageTag.BuiltIn | PackageTag.LegacyFormat))
                UpdateTag(PackageTag.None);
            else if (version.HasTag(PackageTag.Custom))
                UpdateTag(PackageTag.Custom);
            else if (version.HasTag(PackageTag.Local))
                UpdateTag(PackageTag.Local);
            else if (version.HasTag(PackageTag.Git))
                UpdateTag(PackageTag.Git);
            else if (version.HasTag(PackageTag.Deprecated))
                // We don't want to see the Deprecated tag in the packageItem, but we also want to hide any other tags
                UpdateTag(m_IsVersionItem ? PackageTag.Deprecated : PackageTag.None);
            else if (version.HasTag(PackageTag.PreRelease))
                UpdateTag(PackageTag.PreRelease);
            else if (version.HasTag(PackageTag.Experimental))
                UpdateTag(PackageTag.Experimental);
            else
                UpdateTag(PackageTag.None);
        }
    }
}
