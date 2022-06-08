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

        private ResourceLoader m_ResourceLoader;
        private PackageManagerPrefs m_PackageManagerPrefs;

        private VisualElement m_FoldoutContainer;
        private Toggle m_OverviewToggle;
        private PackageDetailsOverviewTabContent m_OverviewContent;

        public override bool IsValid(IPackageVersion version)
        {
            return version?.package?.Is(PackageType.Upm) == true;
        }

        public PackageDetailsDescriptionTab(ResourceLoader resourceLoader, PackageManagerPrefs packageManagerPrefs)
        {
            m_ResourceLoader = resourceLoader;
            m_PackageManagerPrefs = packageManagerPrefs;

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
            RefreshOverviewFoldout(version);
        }

        private void RefreshDescription(IPackageVersion version)
        {
            var hasVersionDescription = !string.IsNullOrEmpty(version.description);
            var desc = hasVersionDescription ? version.description : L10n.Tr("There is no description for this package.");
            if (desc.Length > k_maxDescriptionCharacters)
                desc = desc.Substring(0, k_maxDescriptionCharacters);
            detailDescription.EnableInClassList(k_EmptyDescriptionClass, !hasVersionDescription);
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

        private void RefreshOverviewFoldout(IPackageVersion version)
        {
            var showFoldout = version.package.Is(PackageType.AssetStore);
            if (showFoldout && m_OverviewContent == null)
            {
                m_FoldoutContainer = new VisualElement();

                m_OverviewToggle = new Toggle(L10n.Tr("Overview"));
                m_OverviewToggle.name = "overviewToggle";
                m_OverviewToggle.AddToClassList("expander");
                m_OverviewToggle.RegisterValueChangedCallback(OnOverviewToggled);
                m_OverviewToggle.SetValueWithoutNotify(m_PackageManagerPrefs.overviewFoldoutExpanded);
                m_FoldoutContainer.Add(m_OverviewToggle);

                m_OverviewContent = new PackageDetailsOverviewTabContent(m_ResourceLoader);
                m_OverviewContent.name = "overviewFoldout";
                m_FoldoutContainer.Add(m_OverviewContent);

                Add(m_FoldoutContainer);
            }
            if (m_OverviewContent == null)
                return;

            UIUtils.SetElementDisplay(m_FoldoutContainer, showFoldout);
            UIUtils.SetElementDisplay(m_OverviewContent, m_PackageManagerPrefs.overviewFoldoutExpanded);
            m_OverviewContent.Refresh(version);
        }

        private void OnOverviewToggled(ChangeEvent<bool> evt)
        {
            var expanded = evt.newValue;
            m_PackageManagerPrefs.overviewFoldoutExpanded = expanded;
            UIUtils.SetElementDisplay(m_OverviewContent, expanded);
        }

        private readonly VisualElementCache m_Cache;
        private PackagePlatformList packagePlatformList => m_Cache.Get<PackagePlatformList>("detailPlatformList");
        private SelectableLabel detailDescription => m_Cache.Get<SelectableLabel>("detailDescription");
        private VisualElement detailSourcePathContainer => m_Cache.Get<VisualElement>("detailSourcePathContainer");
        private SelectableLabel detailSourcePath => m_Cache.Get<SelectableLabel>("detailSourcePath");
    }
}
