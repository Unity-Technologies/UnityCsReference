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
        private IPackageLinkFactory m_PackageLinkFactory;

        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<IResourceLoader>();
            m_Application = container.Resolve<IApplicationProxy>();
            m_PageManager = container.Resolve<IPageManager>();
            m_PackageDatabase = container.Resolve<IPackageDatabase>();
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
            versionContainer.Add(new PackageDeprecatedTagLabel());
            versionContainer.Add(new PackageSimpleTagLabel(PackageTag.Disabled, L10n.Tr("Disabled")));
        }

        private void CreateHelpBoxes()
        {
            helpBoxContainer.Add(new PackageSignatureHelpBox(m_Application));
            helpBoxContainer.Add(new NonCompliantPackageHelpBox());
            helpBoxContainer.Add(new DeprecatedVersionHelpBox());
            helpBoxContainer.Add(new DeprecatedPackageHelpBox());
            helpBoxContainer.Add(new DisabledPackageHelpBox());
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
            RefreshAuthor();
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
#pragma warning disable RS0031 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var showLockedIcon = featureSets?.Any() == true;
#pragma warning restore RS0031
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

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var visualState = args.visualStates.FirstOrDefault(vs => vs.packageUniqueId == m_Package?.uniqueId);
#pragma warning restore RS0030
            if (visualState != null)
                RefreshFeatureSetElements(visualState);
        }

        private static Button CreateLink(IPackageVersion version)
        {
            var featureSetLink = new Button(() => { PackageManagerWindow.OpenAndSelectPackage(version.name); });
            featureSetLink.AddToClassList("link", "featureSetLink");
            featureSetLink.text = version.displayName;
            return featureSetLink;
        }

        private void RefreshUsedInFeatureSetMessage(IEnumerable<IPackageVersion> featureSets)
        {
            usedInFeatureSetMessageContainer.Clear();
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var featureSetsCount = featureSets?.Count() ?? 0;
#pragma warning restore RS0030
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
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            usedInFeatureSetMessageContainer.Add(CreateLink(featureSets.FirstOrDefault()));
#pragma warning restore RS0030

            if (featureSetsCount > 2)
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var remaining = featureSets.Skip(1);
#pragma warning restore RS0030
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                remaining.Take(featureSetsCount - 2).Aggregate(usedInFeatureSetMessageContainer, (current, next) =>
#pragma warning restore RS0030
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
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                usedInFeatureSetMessageContainer.Add(CreateLink(featureSets.LastOrDefault()));
#pragma warning restore RS0030
                usedInFeatureSetMessageContainer.Add(new Label(L10n.Tr("features.")));
            }
            else
            {
                usedInFeatureSetMessageContainer.Add(new Label(L10n.Tr("feature.")));
            }
        }

        private void RefreshTags()
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var tag in versionContainer.Children().OfType<PackageBaseTagLabel>())
#pragma warning restore RS0030
                tag.Refresh(m_Version);
        }

        private void RefreshHelpBoxes()
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var helpBox in helpBoxContainer.Children().OfType<PackageBaseHelpBox>())
#pragma warning restore RS0030
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

        private void AddAuthorLabel(string authorName, PackageLink authorLink)
        {
            VisualElement authorElement = null;
            if (authorLink is { isVisible: true })
                authorElement = new PackageLinkButton(m_Application, authorLink);
            else if (!string.IsNullOrEmpty(authorName))
                authorElement = new Label(authorName) { classList = { "authorLabel" } };

            authorContainer.Clear();
            if (authorElement == null)
            {
                authorContainer.Add(new Label(L10n.Tr("Author unknown")){ classList = { "authorLabel" } });
                return;
            }

            authorContainer.Add(new Label(L10n.Tr("By")));
            authorContainer.Add(authorElement);
        }

        private void RefreshAuthor()
        {
            if (m_Version.isFromAssetStore)
                AddAuthorLabel(null, m_PackageLinkFactory.CreateAssetStoreAuthorLink(m_Version));
            else if (m_Version.isFromUnity)
                AddAuthorLabel(L10n.Tr("Unity Technologies"), null);
            else
                AddAuthorLabel(m_Version?.author?.name ?? string.Empty, null);
        }

        private VisualElementCache cache { get; }

        private SelectableLabel detailTitle => cache.Get<SelectableLabel>("detailTitle");
        private Label detailEntitlement => cache.Get<Label>("detailEntitlement");
        private SelectableLabel detailVersion => cache.Get<SelectableLabel>("detailVersion");
        private VersionInfoIcon versionInfoIcon => cache.Get<VersionInfoIcon>("versionInfoIcon");

        private PackageDetailsLinks detailsLinks => cache.Get<PackageDetailsLinks>("detailLinksContainer");

        private VisualElement versionContainer => cache.Get<VisualElement>("versionContainer");
        private VisualElement helpBoxContainer => cache.Get<VisualElement>("helpBoxContainer");

        private VisualElement authorContainer => cache.Get<VisualElement>("authorContainer");
        private VisualElement quickStartContainer => cache.Get<VisualElement>("quickStartContainer");
        private VisualElement usedInFeatureSetMessageContainer => cache.Get<VisualElement>("usedInFeatureSetMessageContainer");
        private VisualElement dependencyContainer => cache.Get<VisualElement>("dependencyContainer");
        private VisualElement lockedIcon => cache.Get<VisualElement>("lockedIcon");
    }
}
