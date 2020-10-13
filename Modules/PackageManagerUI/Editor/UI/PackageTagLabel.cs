// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageTagLabel : Label
    {
        internal new class UxmlFactory : UxmlFactory<PackageTagLabel, UxmlTraits> {}
        internal new class UxmlTraits : TextElement.UxmlTraits {}

        private PackageTagLabel(string text, PackageTag tag = PackageTag.None)
            : base(text)
        {
            AddToClassList(tag.ToString());
        }

        public PackageTagLabel()
        {
        }

        public static PackageTagLabel CreateTagLabel(IPackageVersion version, bool isVersionItem = false)
        {
            if (version == null)
                return null;
            if (version.HasTag(PackageTag.Custom))
                return new PackageTagLabel(L10n.Tr("Custom"), PackageTag.Custom);
            if (version.HasTag(PackageTag.Preview))
                return new PackageTagLabel(L10n.Tr("Preview"), PackageTag.Preview);
            if (isVersionItem && version.HasTag(PackageTag.Verified))
                return new PackageTagLabel(L10n.Tr("Verified"), PackageTag.Verified);
            return null;
        }
    }
}
