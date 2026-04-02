// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageMultiSelectItem : MultiSelectItemBase<IPackage>
    {
        public PackageMultiSelectItem(IPackage package, string rightInfoText = "") : base(package)
        {
            m_RightInfoLabel.text = rightInfoText;
            var version = m_Item?.versions.primary;
            m_TypeIcon.EnableClassToggle("featureIcon", "packageIcon", version?.HasTag(PackageTag.Feature) == true);

            if (version?.HasTag(PackageTag.Feature | PackageTag.LegacyFormat | PackageTag.BuiltIn) == false)
            {
                m_VersionLabel = new Label { name = "versionLabel" };
                m_VersionLabel.text = version.versionString;
                m_LeftContainer.Add(m_VersionLabel);
            }

            m_NameLabel.text = version?.displayName;

            if (m_Item != null && m_Item.progress != PackageProgress.None)
                StartSpinner();
        }
    }
}
