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
            versionContainer.Add(new PackageSimpleTagLabel(PackageTag.Tarball, L10n.Tr("Tarball")));
            versionContainer.Add(new PackageDeprecatedTagLabel());
            versionContainer.Add(new PackageSimpleTagLabel(PackageTag.Disabled, L10n.Tr("Disabled")));
            versionContainer.Add(new PackageSimpleTagLabel(PackageTag.Custom, L10n.Tr("Custom")));
            versionContainer.Add(new PackageSimpleTagLabel(PackageTag.PreRelease, L10n.Tr("Pre-Release")));
            versionContainer.Add(new PackageSimpleTagLabel(PackageTag.Experimental, L10n.Tr("Experimental")));
        }

        private void CreateHelpBoxes()
        {
            helpBoxContainer.Add(new PackageSignatureHelpBox(m_Application));
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

            detailTitle.text = m_Version.displayName;
            detailsLinks.Refresh(m_Version);
            versionInfoIcon.Refresh(m_Package);

            RefreshDependency();
            RefreshFeatureSetElements();
            RefreshTags();
            RefreshHelpBoxes();
            RefreshVersionLabel();
            RefreshRegistryAndAuthor();
            RefreshEntitlement();
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
            var showQuickStart = quickStartLink?.isVisible == true;
            UIUtils.SetElementDisplay(quickStartContainer, showQuickStart);
            if (showQuickStart)
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
            var showFeatureSetMessage = featureSetsCount > 0;
            UIUtils.SetElementDisplay(usedInFeatureSetMessageContainer, showFeatureSetMessage);

            if (!showFeatureSetMessage)
                return;

            // we don't want to see the dependency container when a package is installed as a feature dependency
            UIUtils.SetElementDisplay(dependencyContainer, false);

            var element = new VisualElement { name = "usedInFeatureSetIconAndMessageContainer" };
            var icon = new VisualElement { name = "featureSetIcon" };
            element.Add(icon);

            var message = new Label
            {
                name = "usedInFeatureSetMessageLabel",
                text = L10n.Tr("Installed as part of the ")
            };

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
            UIUtils.SetElementDisplay(versionContainer, showVersionLabel);
            if (!showVersionLabel)
                return;

            var releaseDateString = m_Version.publishedDate?.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("en-US"));
            detailVersion.text = string.IsNullOrEmpty(releaseDateString)
                ? versionString
                : string.Format(L10n.Tr("{0} · {1}"), versionString, releaseDateString);
        }

        private void AddRegistryAndAuthorLabel(string registryName, bool registryVerified, string authorName, PackageLink authorLink)
        {
            VisualElement registryElement = null;
            VisualElement authorElement = null;
            if (!string.IsNullOrEmpty(registryName))
            {
                var registryLabel = new Label(registryName) { classList = { "registryLabel" } };
                if (registryVerified)
                    registryLabel.AddToClassList("verified");
                registryElement = registryLabel;
            }

            if (authorLink is { isVisible: true })
                authorElement = new PackageLinkButton(m_Application, authorLink);
            else if (!string.IsNullOrEmpty(authorName))
                authorElement = new Label(authorName) { classList = { "authorLabel" } };

            registryAndAuthorContainer.Clear();
            if (registryElement == null && authorElement == null)
            {
                registryAndAuthorContainer.Add(new Label(L10n.Tr("Author unknown")){ classList = { "authorLabel" } });
                return;
            }

            if (registryElement != null)
            {
                registryAndAuthorContainer.Add(new Label(L10n.Tr("From")));
                registryAndAuthorContainer.Add(registryElement);
            }

            if (authorElement != null)
            {
                registryAndAuthorContainer.Add(new Label(registryElement == null ? L10n.Tr("By") : L10n.Tr("by")));
                registryAndAuthorContainer.Add(authorElement);
            }
        }

        private void RefreshRegistryAndAuthor()
        {
            var isFromUnity = m_Version.availableRegistry == RegistryType.UnityRegistry && !m_Version.HasTag(PackageTag.InstalledFromPath);
            var isFromAssetStore = m_Version.package.product != null && !m_Version.HasTag(PackageTag.InstalledFromPath);

            if (isFromAssetStore)
                AddRegistryAndAuthorLabel(L10n.Tr("Asset Store"), true, null, m_PackageLinkFactory.CreateAssetStoreAuthorLink(m_Version));
            else if (isFromUnity)
                AddRegistryAndAuthorLabel(L10n.Tr("Unity Registry"), true, L10n.Tr("Unity Technologies"), null);
            else
            {
                var packageInfo = m_UpmCache.GetBestMatchPackageInfo(m_Version.name, m_Version.package.product?.id ?? 0, m_Version.isInstalled, m_Version.versionString);
                // Null check for the package info is needed here because sometimes the PackageDetails would be refreshed mid-package generation (due to selection change),
                // and sometimes an installed package would exist in the PackageDatabase, but the corresponding installed package info has been removed mid-generation.
                // This won't cause any UI issues as the UI will be refreshed again after all packages are generated (and some packages removed).
                AddRegistryAndAuthorLabel(packageInfo?.registry?.name ?? string.Empty, false, m_Version?.author?.name ?? string.Empty, null);
            }
        }

        private VisualElementCache cache { get; }

        private SelectableLabel detailTitle => cache.Get<SelectableLabel>("detailTitle");
        private Label detailEntitlement => cache.Get<Label>("detailEntitlement");
        private SelectableLabel detailVersion => cache.Get<SelectableLabel>("detailVersion");
        private VersionInfoIcon versionInfoIcon => cache.Get<VersionInfoIcon>("versionInfoIcon");

        private PackageDetailsLinks detailsLinks => cache.Get<PackageDetailsLinks>("detailLinksContainer");

        private VisualElement versionContainer => cache.Get<VisualElement>("versionContainer");
        private VisualElement helpBoxContainer => cache.Get<VisualElement>("helpBoxContainer");

        private VisualElement registryAndAuthorContainer => cache.Get<VisualElement>("registryAndAuthorContainer");
        private VisualElement quickStartContainer => cache.Get<VisualElement>("quickStartContainer");
        private VisualElement usedInFeatureSetMessageContainer => cache.Get<VisualElement>("usedInFeatureSetMessageContainer");
        private VisualElement dependencyContainer => cache.Get<VisualElement>("dependencyContainer");
        private VisualElement lockedIcon => cache.Get<VisualElement>("lockedIcon");
    }
}
