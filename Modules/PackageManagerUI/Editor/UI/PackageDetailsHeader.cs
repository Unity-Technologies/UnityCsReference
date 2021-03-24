// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsHeader : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageDetailsHeader> {}

        internal static readonly PackageTag[] k_VisibleTags =
        {
            PackageTag.Release,
            PackageTag.Custom,
            PackageTag.Local,
            PackageTag.Git,
            PackageTag.Deprecated,
            PackageTag.Disabled,
            PackageTag.PreRelease,
            PackageTag.Experimental,
            PackageTag.ReleaseCandidate
        };

        private ResourceLoader m_ResourceLoader;
        private ApplicationProxy m_Application;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_Application = container.Resolve<ApplicationProxy>();
        }

        private IPackage m_Package;
        private IPackageVersion m_Version;

        public PackageDetailsHeader()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageDetailsHeader.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            detailAuthorLink.clickable.clicked += AuthorClick;
        }

        public void Refresh(IPackage package, IPackageVersion version)
        {
            m_Package = package;
            m_Version = version;

            detailTitle.SetValueWithoutNotify(m_Version.displayName);
            detailsLinks.Refresh(m_Package, m_Version);

            RefreshAuthor();
            RefreshTags();
            RefreshVersionLabel();
            RefreshVersionInfoIcon();
        }

        private void RefreshAuthor()
        {
            var showAuthorContainer = !string.IsNullOrEmpty(m_Version?.author);
            UIUtils.SetElementDisplay(detailAuthorContainer, showAuthorContainer);
            if (showAuthorContainer)
            {
                if (!string.IsNullOrEmpty(m_Version.authorLink))
                {
                    UIUtils.SetElementDisplay(detailAuthorText, false);
                    UIUtils.SetElementDisplay(detailAuthorLink, true);
                    detailAuthorLink.text = m_Version.author;
                }
                else
                {
                    UIUtils.SetElementDisplay(detailAuthorText, true);
                    UIUtils.SetElementDisplay(detailAuthorLink, false);
                    detailAuthorText.SetValueWithoutNotify(m_Version.author);
                }
            }
        }

        private void RefreshTags()
        {
            foreach (var tag in k_VisibleTags)
                UIUtils.SetElementDisplay(GetTagLabel(tag.ToString()), m_Version.HasTag(tag));

            var scopedRegistryTagLabel = GetTagLabel("ScopedRegistry");
            if ((m_Version as UpmPackageVersion)?.isUnityPackage == false && !string.IsNullOrEmpty(m_Version.version?.Prerelease))
            {
                scopedRegistryTagLabel.tooltip = m_Version.version?.Prerelease;
                scopedRegistryTagLabel.text = m_Version.version?.Prerelease;
                UIUtils.SetElementDisplay(scopedRegistryTagLabel, true);
            }
            else
            {
                UIUtils.SetElementDisplay(scopedRegistryTagLabel, false);
            }
            UIUtils.SetElementDisplay(GetTagLabel(PackageType.AssetStore.ToString()), m_Package.Is(PackageType.AssetStore));
        }

        private void AuthorClick()
        {
            var authorLink = m_Version?.authorLink ?? string.Empty;
            if (!string.IsNullOrEmpty(authorLink))
                m_Application.OpenURL(authorLink);
        }

        public void RefreshEntitlement()
        {
            var showEntitlement = m_Package.hasEntitlements;
            UIUtils.SetElementDisplay(detailEntitlement, showEntitlement);
            detailEntitlement.text = showEntitlement ? "E" : string.Empty;
            detailEntitlement.tooltip = showEntitlement ? L10n.Tr("This is an Entitlement package.") : string.Empty;
        }

        private void RefreshVersionLabel()
        {
            var versionString = m_Version.versionString;
            var releaseDateString = m_Version.publishedDate?.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("en-US"));
            detailVersion.SetValueWithoutNotify(string.IsNullOrEmpty(releaseDateString)
                ? string.Format(L10n.Tr("Version {0}"), versionString)
                : string.Format(L10n.Tr("Version {0} - {1}"), versionString, releaseDateString));
            UIUtils.SetElementDisplay(detailVersion, !m_Package.Is(PackageType.BuiltIn) && !string.IsNullOrEmpty(versionString));
        }

        private void RefreshVersionInfoIcon()
        {
            var isInstalledVersionDifferentThanRequested = UpmPackageVersion.IsDifferentVersionThanRequested(m_Package?.versions.installed);
            UIUtils.SetElementDisplay(versionInfoIcon, isInstalledVersionDifferentThanRequested);

            if (!isInstalledVersionDifferentThanRequested)
                return;

            var installedVersionString = m_Package?.versions.installed.versionString;
            if (UpmPackageVersion.IsRequestedButOverriddenVersion(m_Package, m_Version))
                versionInfoIcon.tooltip = string.Format(
                    L10n.Tr("Unity installed version {0} because another package depends on it (version {0} overrides version {1})."),
                    installedVersionString, m_Version.versionString);
            else if (m_Version.isInstalled && UpmPackageVersion.IsDifferentVersionThanRequested(m_Version))
                versionInfoIcon.tooltip = L10n.Tr("At least one other package depends on this version of the package.");
            else
                versionInfoIcon.tooltip = string.Format(
                    L10n.Tr("At least one other package depends on version {0} of this package."), installedVersionString);
        }

        private VisualElementCache cache { get; set; }

        private SelectableLabel detailTitle => cache.Get<SelectableLabel>("detailTitle");
        private Label detailEntitlement => cache.Get<Label>("detailEntitlement");
        private SelectableLabel detailVersion => cache.Get<SelectableLabel>("detailVersion");
        private VisualElement versionInfoIcon => cache.Get<VisualElement>("versionInfoIcon");

        private VisualElement detailAuthorContainer => cache.Get<VisualElement>("detailAuthorContainer");
        private SelectableLabel detailAuthorText => cache.Get<SelectableLabel>("detailAuthorText");
        private Button detailAuthorLink => cache.Get<Button>("detailAuthorLink");

        private PackageDetailsLinks detailsLinks => cache.Get<PackageDetailsLinks>("detailLinksContainer");

        internal PackageTagLabel GetTagLabel(string tag) => cache.Get<PackageTagLabel>("tag" + tag);
    }
}
