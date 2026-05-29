// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    [UxmlElement]
    internal partial class PackageDetailsHeader : VisualElement
    {
        private IPackage m_Package;
        private IPackageVersion m_Version;

        private readonly IApplicationProxy m_Application;
        private readonly IPageManager m_PageManager;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPackageLinkFactory m_PackageLinkFactory;

        public PackageDetailsHeader() : this(
            ServicesContainer.instance.Resolve<IResourceLoader>(),
            ServicesContainer.instance.Resolve<IApplicationProxy>(),
            ServicesContainer.instance.Resolve<IPageManager>(),
            ServicesContainer.instance.Resolve<IPackageDatabase>(),
            ServicesContainer.instance.Resolve<IPackageLinkFactory>())
        {
        }

        public PackageDetailsHeader(
            IResourceLoader resourceLoader,
            IApplicationProxy application,
            IPageManager pageManager,
            IPackageDatabase packageDatabase,
            IPackageLinkFactory packageLinkFactory)
        {
            m_Application = application;
            m_PageManager = pageManager;
            m_PackageDatabase = packageDatabase;
            m_PackageLinkFactory = packageLinkFactory;

            var root = resourceLoader.GetTemplate("PackageDetailsHeader.uxml");
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
            helpBoxContainer.Add(new NonCompliantPackageHelpBox(m_Application));
            helpBoxContainer.Add(new DeprecatedVersionHelpBox(m_Application));
            helpBoxContainer.Add(new DeprecatedPackageHelpBox(m_Application));
            helpBoxContainer.Add(new DisabledPackageHelpBox(m_Application));
            helpBoxContainer.Add(new HiddenProductHelpBox(m_Application));
        }

        public void Refresh(IPackage package)
        {
            m_Package = package;
            m_Version = package.versions.primary;

            detailTitle.text = m_Version.displayName;
            detailsLinks.Refresh(m_Version);
            versionInfoIcon.Refresh(m_Package);
            packageAuthorLabel.Refresh(m_Version);

            RefreshDependency();
            RefreshFeatureSetElements();
            RefreshTags();
            RefreshHelpBoxes();
            RefreshVersionLabel();
            RefreshEntitlement();
        }

        private void RefreshFeatureSetElements(VisualState visualState = null)
        {
            var featureSets = new List<IPackageVersion>(m_PackageDatabase.EnumerateDirectReverseDependencies(m_Package.versions.installed, true));
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

        private void RefreshLockIcons(IReadOnlyList<IPackageVersion> featureSets, VisualState visualState = null)
        {
            var showLockedIcon = featureSets.Count > 0;
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
            if (!args.page.isActive)
                return;

            var visualState = args.changed.FirstMatch(vs => vs.itemUniqueId == m_Package?.uniqueId);
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

        private void RefreshUsedInFeatureSetMessage(IReadOnlyList<IPackageVersion> featureSets)
        {
            usedInFeatureSetMessageContainer.Clear();
            var showFeatureSetMessage = featureSets.Count > 0;
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

            usedInFeatureSetMessageContainer.Add(CreateLink(featureSets[0]));

            if (featureSets.Count > 1)
            {
                for (var i = 1; i < featureSets.Count - 1; i++)
                {
                    var comma = new Label(", ") { style = { marginLeft = 0, paddingLeft = 0 } };
                    usedInFeatureSetMessageContainer.Add(comma);
                    usedInFeatureSetMessageContainer.Add(CreateLink(featureSets[i]));
                }

                var and = new Label(L10n.Tr(" and ")) { style = { marginLeft = 0, paddingLeft = 0 } };
                usedInFeatureSetMessageContainer.Add(and);
                usedInFeatureSetMessageContainer.Add(CreateLink(featureSets[^1]));
                usedInFeatureSetMessageContainer.Add(new Label(L10n.Tr("features.")));
            }
            else
            {
                usedInFeatureSetMessageContainer.Add(new Label(L10n.Tr("feature.")));
            }
        }

        private void RefreshTags()
        {
            foreach (var tag in versionContainer.Children().FilterByType<PackageBaseTagLabel>())
                tag.Refresh(m_Version);
        }

        private void RefreshHelpBoxes()
        {
            foreach (var helpBox in helpBoxContainer.Children().FilterByType<PackageBaseHelpBox>())
                helpBox.Refresh(m_Version);
        }

        private void RefreshEntitlement()
        {
            var showEnterpriseLabel = m_Package.isEnterprise;
            UIUtils.SetElementDisplay(detailEnterpriseLabel, showEnterpriseLabel);
            if (!showEnterpriseLabel)
                return;
            detailEnterpriseLabel.text = "E";
            detailEnterpriseLabel.tooltip = L10n.Tr("This is an entitled package.");
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

        private VisualElementCache cache { get; }

        private SelectableLabel detailTitle => cache.Get<SelectableLabel>("detailTitle");
        private Label detailEnterpriseLabel => cache.Get<Label>("detailEnterpriseLabel");
        private SelectableLabel detailVersion => cache.Get<SelectableLabel>("detailVersion");
        private VersionInfoIcon versionInfoIcon => cache.Get<VersionInfoIcon>("versionInfoIcon");

        private PackageDetailsLinks detailsLinks => cache.Get<PackageDetailsLinks>("detailLinksContainer");

        private VisualElement versionContainer => cache.Get<VisualElement>("versionContainer");
        private VisualElement helpBoxContainer => cache.Get<VisualElement>("helpBoxContainer");

        private PackageAuthorLabel packageAuthorLabel => cache.Get<PackageAuthorLabel>("packageAuthorLabel");
        private VisualElement quickStartContainer => cache.Get<VisualElement>("quickStartContainer");
        private VisualElement usedInFeatureSetMessageContainer => cache.Get<VisualElement>("usedInFeatureSetMessageContainer");
        private VisualElement dependencyContainer => cache.Get<VisualElement>("dependencyContainer");
        private VisualElement lockedIcon => cache.Get<VisualElement>("lockedIcon");
    }
}
