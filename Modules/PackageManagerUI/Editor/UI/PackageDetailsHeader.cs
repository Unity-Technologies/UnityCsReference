// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsHeader : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new PackageDetailsHeader();
        }

        private IResourceLoader m_ResourceLoader;
        private IApplicationProxy m_Application;
        private IPageManager m_PageManager;
        private IPackageDatabase m_PackageDatabase;
        private IUpmCache m_UpmCache;
        private IPackageLinkFactory m_PackageLinkFactory;

        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<IResourceLoader>();
            m_Application = container.Resolve<IApplicationProxy>();
            m_PageManager = container.Resolve<IPageManager>();
            m_PackageDatabase = container.Resolve<IPackageDatabase>();
            m_UpmCache = container.Resolve<IUpmCache>();
            m_PackageLinkFactory = container.Resolve<IPackageLinkFactory>();
        }

        private IPackage m_Package;
        private IPackageVersion m_Version;

        public PackageDetailsHeader()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageDetailsHeader.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            CreateTags();
            CreateHelpBoxes();

            m_PageManager.onVisualStateChange += OnVisualStateChange;
        }

        private void CreateTags()
        {
            versionContainer.Add(new PackageSimpleTagLabel(PackageTag.Git, L10n.Tr("Git")));
            versionContainer.Add(new PackageSimpleTagLabel(PackageTag.Local, L10n.Tr("Local")));
            versionContainer.Add(new PackageAssetStoreTagLabel());
            versionContainer.Add(new PackageDeprecatedTagLabel());
            versionContainer.Add(new PackageSimpleTagLabel(PackageTag.Disabled, L10n.Tr("Disabled")));
            versionContainer.Add(new PackageSimpleTagLabel(PackageTag.Custom, L10n.Tr("Custom")));
            versionContainer.Add(new PackageSimpleTagLabel(PackageTag.PreRelease, L10n.Tr("Pre-Release")));
            versionContainer.Add(new PackageSimpleTagLabel(PackageTag.Experimental, L10n.Tr("Experimental")));
        }

        private void CreateHelpBoxes()
        {
            helpBoxContainer.Add(new NonCompliantPackageHelpBox());
            helpBoxContainer.Add(new DeprecatedVersionHelpBox());
            helpBoxContainer.Add(new DeprecatedPackageHelpBox());
            helpBoxContainer.Add(new DisabledPackageHelpBox());
            helpBoxContainer.Add(new ScopedRegistryHelpBox(m_Application));
            helpBoxContainer.Add(new VersionTagHelpBox(m_Application));
            helpBoxContainer.Add(new HiddenProductHelpBox());
        }

        public void Refresh(IPackage package)
        {
            m_Package = package;
            m_Version = package.versions.primary;

            detailTitle.SetValueWithoutNotify(m_Version.displayName);
            detailsLinks.Refresh(m_Version);

            RefreshName();
            RefreshDependency();
            RefreshFeatureSetElements();
            RefreshTags();
            RefreshHelpBoxes();
            RefreshVersionLabel();
            RefreshVersionInfoIcon();
            RefreshRegistryAndAuthor();
            RefreshEntitlement();
        }

        private void RefreshName()
        {
            // We use package.name instead of version.name because `version.name` would be empty for a PlaceholderPackageVersion
            detailName.SetValueWithoutNotify(m_Package.name);
            UIUtils.SetElementDisplay(detailName, !string.IsNullOrEmpty(m_Package.name));
        }

        private void RefreshFeatureSetElements(VisualState visualState = null)
        {
            var featureSets = m_PackageDatabase.GetFeaturesThatUseThisPackage(m_Package.versions.installed);
            RefreshUsedInFeatureSetMessage(featureSets);
            RefreshLockIcons(featureSets, visualState);
            RefreshQuickStart();
        }

        private void RefreshQuickStart()
        {
            quickStartContainer.Clear();

            var quickStartLink = m_PackageLinkFactory.CreateUpmQuickStartLink(m_Version);

            if (quickStartLink?.isVisible == true)
                quickStartContainer.Add(new PackageQuickStartButton(m_Application, quickStartLink));
        }

        private void RefreshDependency()
        {
            UIUtils.SetElementDisplay(dependencyContainer, m_Version.isInstalled && !m_Version.isDirectDependency && !m_Version.HasTag(PackageTag.Feature));
        }

        private void RefreshLockIcons(IEnumerable<IPackageVersion> featureSets, VisualState visualState = null)
        {
            var showLockedIcon = featureSets?.Any() == true;
            if (showLockedIcon)
            {
                visualState ??= m_PageManager.activePage.visualStates.Get(m_Package?.uniqueId);
                if (visualState?.isLocked == true)
                {
                    lockedIcon.RemoveFromClassList("unlocked");
                    lockedIcon.AddToClassList("locked");
                    lockedIcon.tooltip = L10n.Tr("This package is locked because it's part of a feature set. Click unlock button to be able to make changes");
                }
                else
                {
                    lockedIcon.AddToClassList("unlocked");
                    lockedIcon.RemoveFromClassList("locked");
                    lockedIcon.tooltip = L10n.Tr("This package is unlocked. You can now change its version.");
                }
            }
            UIUtils.SetElementDisplay(lockedIcon, showLockedIcon);
        }

        private void OnVisualStateChange(VisualStateChangeArgs args)
        {
            if (!args.page.isActivePage)
                return;

            var visualState = args.visualStates.FirstOrDefault(vs => vs.packageUniqueId == m_Package?.uniqueId);
            if (visualState != null)
                RefreshFeatureSetElements(visualState);
        }

        private static Button CreateLink(IPackageVersion version)
        {
            var featureSetLink = new Button(() => { PackageManagerWindow.OpenAndSelectPackage(version.name); });
            featureSetLink.AddClasses("link featureSetLink");
            featureSetLink.text = version.displayName;
            return featureSetLink;
        }

        internal void RefreshUsedInFeatureSetMessage(IEnumerable<IPackageVersion> featureSets)
        {
            usedInFeatureSetMessageContainer.Clear();
            var featureSetsCount = featureSets?.Count() ?? 0;

            if (featureSetsCount > 0)
            {
                // we don't want to see the dependency container when a package is installed as a feature dependency
                UIUtils.SetElementDisplay(dependencyContainer, false);

                var element = new VisualElement { name = "usedInFeatureSetIconAndMessageContainer" };
                var icon = new VisualElement { name = "featureSetIcon" };
                element.Add(icon);

                var message = new Label {name = "usedInFeatureSetMessageLabel"};
                message.text = string.Format(L10n.Tr("is installed as part of the "), m_Version.GetDescriptor());

                element.Add(message);
                usedInFeatureSetMessageContainer.Add(element);
                usedInFeatureSetMessageContainer.Add(CreateLink(featureSets.FirstOrDefault()));

                if (featureSetsCount > 2)
                {
                    var remaining = featureSets.Skip(1);
                    remaining.Take(featureSetsCount - 2).Aggregate(usedInFeatureSetMessageContainer, (current, next) =>
                    {
                        var comma = new Label(", ");
                        comma.style.marginLeft = 0;
                        comma.style.paddingLeft = 0;

                        current.Add(comma);
                        current.Add(CreateLink(next));
                        return current;
                    });
                }
                if (featureSetsCount > 1)
                {
                    var and = new Label(L10n.Tr(" and "));
                    and.style.marginLeft = 0;
                    and.style.paddingLeft = 0;

                    usedInFeatureSetMessageContainer.Add(and);
                    usedInFeatureSetMessageContainer.Add(CreateLink(featureSets.LastOrDefault()));
                    usedInFeatureSetMessageContainer.Add(new Label(L10n.Tr("features.")));
                }
                else
                {
                    usedInFeatureSetMessageContainer.Add(new Label(L10n.Tr("feature.")));
                }
            }
        }

        private void RefreshTags()
        {
            foreach (var tag in versionContainer.Children().OfType<PackageBaseTagLabel>())
                tag.Refresh(m_Version);
        }

        private void RefreshHelpBoxes()
        {
            foreach (var helpBox in helpBoxContainer.Children().OfType<PackageBaseHelpBox>())
                helpBox.Refresh(m_Version);
        }

        private void RefreshEntitlement()
        {
            var showEntitlement = m_Package.hasEntitlements;
            UIUtils.SetElementDisplay(detailEntitlement, showEntitlement);
            detailEntitlement.text = showEntitlement ? "E" : string.Empty;
            detailEntitlement.tooltip = showEntitlement ? L10n.Tr("This is an Entitlement package.") : string.Empty;
        }

        private void RefreshVersionLabel()
        {
            var versionString = m_Version.versionString;
            var showVersionLabel = !m_Version.HasTag(PackageTag.BuiltIn) && !m_Version.HasTag(PackageTag.Feature) && !string.IsNullOrEmpty(versionString);
            UIUtils.SetElementDisplay(detailVersion, showVersionLabel);
            if (!showVersionLabel)
                return;

            var releaseDateString = m_Version.publishedDate?.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("en-US"));
            detailVersion.SetValueWithoutNotify(string.IsNullOrEmpty(releaseDateString)
                ? versionString
                : string.Format(L10n.Tr("{0} · {1}"), versionString, releaseDateString));
        }

        private void RefreshVersionInfoIcon()
        {
            var installed = m_Package?.versions?.installed;
            if (installed == null || m_Version == null)
            {
                UIUtils.SetElementDisplay(versionInfoIcon, false);
                return;
            }
            var installedVersionString = installed.versionString;
            if (installed.IsDifferentVersionThanRequested && !installed.isInvalidSemVerInManifest)
            {
                UIUtils.SetElementDisplay(versionInfoIcon, true);
                versionInfoIcon.tooltip = string.Format(L10n.Tr("Unity installed version {0} because another package depends on it (version {0} overrides version {1})."),
                    installedVersionString, m_Version.versionInManifest);
                return;
            }

            // If a Unity package doesn't have a recommended version (decided by versions set in the editor manifest or remote manifest override),
            // then that package is not considered part of the Unity Editor "product" and we need to let users know.
            var unityVersionString = m_Application.unityVersion;
            if (m_Version.HasTag(PackageTag.Unity) && !m_Version.HasTag(PackageTag.BuiltIn) && m_Package.versions.recommended == null)
            {
                UIUtils.SetElementDisplay(versionInfoIcon, true);
                versionInfoIcon.tooltip = string.Format(L10n.Tr("This package is not officially supported for Unity {0}."), unityVersionString);
                return;
            }

            // We want to let users know when they are using a version different than the recommended.
            // However, we don't want to show the info icon if the version currently installed
            // is a higher patch version of the one in the editor manifest (still considered recommended).
            var recommended = m_Package.versions.recommended;
            if (m_Version.isInstalled
                && m_Package.state != PackageState.InstalledAsDependency
                && m_Version.HasTag(PackageTag.Unity)
                && recommended != null
                && installed.version?.IsEqualOrPatchOf(recommended.version) != true)
            {
                UIUtils.SetElementDisplay(versionInfoIcon, true);
                versionInfoIcon.tooltip = string.Format(L10n.Tr("This version is not the recommended for Unity {0}. The recommended version is {1}."),
                    unityVersionString, recommended.versionString);
                return;
            }

            UIUtils.SetElementDisplay(versionInfoIcon, false);
        }

        private bool TryShowAuthorLink()
        {
            authorContainer.Clear();
            var authorLink = m_PackageLinkFactory.CreateAuthorLink(m_Version);
            if (authorLink is not { isVisible: true })
                return false;
            authorContainer.Add(new PackageLinkButton(m_Application, authorLink));
            return true;
        }

        private void RefreshRegistryAndAuthor()
        {
            var showRegistryAndAuthorLabel = !TryShowAuthorLink();
            UIUtils.SetElementDisplay(registryAndAuthorLabel, showRegistryAndAuthorLabel);
            if (!showRegistryAndAuthorLabel)
                return;

            var isByUnity = m_Version.availableRegistry == RegistryType.UnityRegistry && !m_Version.HasTag(PackageTag.InstalledFromPath);
            var author = isByUnity ? L10n.Tr("Unity Technologies Inc.") : m_Version?.author;
            registryAndAuthorLabel.tooltip = string.Empty;

            if (m_Version is { availableRegistry: RegistryType.UnityRegistry or RegistryType.MyRegistries })
            {
                var packageInfo = m_UpmCache.GetBestMatchPackageInfo(m_Version.name, m_Version.isInstalled, m_Version.versionString);
                // Null check for the package info is needed here because sometimes the PackageDetails would be refreshed mid-package generation (due to selection change),
                // and sometimes an installed package would exist in the PackageDatabase, but the corresponding installed package info has been removed mid-generation.
                // This won't cause any UI issues as the UI will be refreshed again after all packages are generated (and some packages removed).
                var registryName = isByUnity ? L10n.Tr("Unity Registry") : packageInfo?.registry?.name ?? string.Empty;
                if (isByUnity)
                    registryAndAuthorLabel.tooltip = packageInfo?.registry?.url ?? string.Empty;
                if (!string.IsNullOrEmpty(registryName))
                {
                    registryAndAuthorLabel.text = string.IsNullOrEmpty(author)
                        ? string.Format(L10n.Tr("From <b>{0}</b>"), registryName)
                        : string.Format(L10n.Tr("From <b>{0}</b> by {1}"), registryName, author);
                    return;
                }
            }
            registryAndAuthorLabel.text = string.IsNullOrEmpty(author) ? L10n.Tr("Author unknown") : string.Format(L10n.Tr("By {0}"), author);
        }

        private VisualElementCache cache { get; }

        private SelectableLabel detailTitle => cache.Get<SelectableLabel>("detailTitle");
        private Label detailEntitlement => cache.Get<Label>("detailEntitlement");
        private SelectableLabel detailVersion => cache.Get<SelectableLabel>("detailVersion");
        private VisualElement versionInfoIcon => cache.Get<VisualElement>("versionInfoIcon");

        private SelectableLabel detailName => cache.Get<SelectableLabel>("detailName");

        private VisualElement authorContainer => cache.Get<VisualElement>("detailAuthorContainer");

        private PackageDetailsLinks detailsLinks => cache.Get<PackageDetailsLinks>("detailLinksContainer");

        private VisualElement versionContainer => cache.Get<VisualElement>("versionContainer");
        private VisualElement helpBoxContainer => cache.Get<VisualElement>("helpBoxContainer");

        private Label registryAndAuthorLabel => cache.Get<Label>("registryAndAuthorLabel");
        private VisualElement quickStartContainer => cache.Get<VisualElement>("quickStartContainer");
        private VisualElement usedInFeatureSetMessageContainer => cache.Get<VisualElement>("usedInFeatureSetMessageContainer");
        private VisualElement dependencyContainer => cache.Get<VisualElement>("dependencyContainer");
        private VisualElement lockedIcon => cache.Get<VisualElement>("lockedIcon");
    }
}
