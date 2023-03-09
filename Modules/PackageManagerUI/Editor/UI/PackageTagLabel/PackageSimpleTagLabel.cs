// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageSimpleTagLabel : PackageBaseTagLabel
    {
        private PackageTag m_Tag;
        public PackageSimpleTagLabel(PackageTag tag, string text)
        {
            m_Tag = tag;
            name = "tag" + tag;
            this.text = text;
        }

        private bool IsVisible(IPackageVersion version)
        {
            // When a package is deprecated, we want to hide all other tags
            return version != null && version.HasTag(m_Tag) && !version.package.isDeprecated && !version.HasTag(PackageTag.Deprecated);
        }

        public override void Refresh(IPackageVersion version)
        {
            var isVisible = IsVisible(version);
            UIUtils.SetElementDisplay(this, isVisible);
            EnableInClassList(m_Tag.ToString(), isVisible);
        }
    }
}
