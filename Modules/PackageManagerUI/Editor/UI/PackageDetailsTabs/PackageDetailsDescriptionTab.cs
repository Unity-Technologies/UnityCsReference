// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsDescriptionTab : PackageDetailsTabElement
    {
        public const string k_Id = "description";

        private const string k_EmptyDescriptionClass = "empty";
        private const int k_maxDescriptionCharacters = 10000;

        public override bool IsValid(IPackageVersion version)
        {
            return version?.package?.Is(PackageType.AssetStore) == false;
        }

        public PackageDetailsDescriptionTab(ResourceLoader resourceLoader)
        {
            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Description");
            var root = resourceLoader.GetTemplate("DetailsTabs/PackageDetailsDescriptionTab.uxml");
            Add(root);
            m_Cache = new VisualElementCache(root);
        }

        public override void Refresh(IPackageVersion version)
        {
            packagePlatformList.Refresh(version);
            RefreshDescription(version);
            RefreshSourcePath(version);
        }

        private void RefreshDescription(IPackageVersion version)
        {
            var hasDescription = !string.IsNullOrEmpty(version.description);
            var desc = hasDescription ? version.description : L10n.Tr("There is no description for this package.");
            if (desc.Length > k_maxDescriptionCharacters)
                desc = desc.Substring(0, k_maxDescriptionCharacters);
            detailDescription.EnableInClassList(k_EmptyDescriptionClass, !hasDescription);
            detailDescription.style.maxHeight = int.MaxValue;
            detailDescription.SetValueWithoutNotify(desc);
        }

        private void RefreshSourcePath(IPackageVersion version)
        {
            var sourcePath = (version as UpmPackageVersion)?.sourcePath;
            UIUtils.SetElementDisplay(detailSourcePathContainer, !string.IsNullOrEmpty(sourcePath));

            if (!string.IsNullOrEmpty(sourcePath))
                detailSourcePath.SetValueWithoutNotify(sourcePath.EscapeBackslashes());
        }

        private readonly VisualElementCache m_Cache;
        private PackagePlatformList packagePlatformList => m_Cache.Get<PackagePlatformList>("detailPlatformList");
        private SelectableLabel detailDescription => m_Cache.Get<SelectableLabel>("detailDescription");
        private VisualElement detailSourcePathContainer => m_Cache.Get<VisualElement>("detailSourcePathContainer");
        private SelectableLabel detailSourcePath => m_Cache.Get<SelectableLabel>("detailSourcePath");
    }
}
