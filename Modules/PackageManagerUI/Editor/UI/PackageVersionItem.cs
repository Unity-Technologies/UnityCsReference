// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageVersionItem : VisualElement, ISelectableItem
    {
        public IPackage package { get; set; }
        public IPackageVersion version { get; set; }

        private ResourceLoader m_ResourceLoader;
        private PageManager m_PageManager;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_PageManager = container.Resolve<PageManager>();
        }

        public PackageVersionItem(IPackage package, IPackageVersion version, bool alwaysShowRecommendedLabel, bool isLatestVersion)
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageVersionItem.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            this.package = package;
            this.version = version;
            RefreshLabel(alwaysShowRecommendedLabel, isLatestVersion);
            this.OnLeftClick(() => m_PageManager.SetSelected(package, version, true));
        }

        public IPackageVersion targetVersion { get { return version; } }
        public VisualElement element { get { return this; } }

        private void RefreshLabel(bool alwaysShowRecommendedLabel, bool isLatestVersion)
        {
            versionLabel.text = version.version?.ToString() ?? version.versionString;
            versionLabel.ShowTextTooltipOnSizeChange();

            var primary = package.versions.primary;
            var recommended = package.versions.recommended;
            var versionInManifest = primary?.packageInfo?.projectDependenciesEntry;
            var stateText = string.Empty;

            if (version == primary)
            {
                if (version.isInstalled)
                    stateText = L10n.Tr(version.isDirectDependency ? "Currently installed" : "Installed as dependency");
                else if (version.HasTag(PackageTag.Downloadable) && version.isAvailableOnDisk)
                    stateText = L10n.Tr("Currently downloaded");
                // with keyVersions being potentially larger now, we want to
                //  show 'Recommended' text whether package is installed or not
                //  if more than one version is in keyVersions
                else if (version == recommended && alwaysShowRecommendedLabel)
                    stateText = L10n.Tr("Recommended");
            }
            else if (versionInManifest == version.versionString)
                stateText = L10n.Tr("Requested but overridden");
            else if (version == recommended)
                stateText = L10n.Tr("Recommended");
            else if (primary.isInstalled && isLatestVersion)
                stateText = L10n.Tr("Latest update");

            stateLabel.text = stateText;

            var tagLabel = PackageTagLabel.CreateTagLabel(version, true);
            if (tagLabel != null)
                stateContainer.Add(tagLabel);
        }

        private VisualElementCache cache { get; set; }
        private Label stateLabel { get { return cache.Get<Label>("stateLabel"); } }
        private Label versionLabel { get { return cache.Get<Label>("versionLabel"); } }
        private VisualElement stateContainer { get { return cache.Get<VisualElement>("stateContainer"); } }
    }
}
