// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsHeader : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageDetailsHeader> {}

        internal enum InfoBoxState
        {
            PreRelease,
            Experimental,
            ReleaseCandidate,
            ScopedRegistry
        }

        private string infoBoxUrl => $"https://docs.unity3d.com/{m_Application?.shortUnityVersion}";

        private static readonly string[] k_InfoBoxReadMoreUrl =
        {
            "/Documentation/Manual/pack-preview.html",
            "/Documentation/Manual/pack-exp.html",
            "/Documentation/Manual/pack-releasecandidate.html",
            "/Documentation/Manual/upm-scoped.html"
        };

        private static readonly string[] k_InfoBoxReadMoreText =
        {
            L10n.Tr("Pre-release packages are in the process of becoming stable and will be available as production-ready by the end of this LTS release. We recommend using these only for testing purposes and to give us direct feedback until then."),
            L10n.Tr("Experimental packages are new packages or experiments on mature packages in the early stages of development. Experimental packages are not supported by Unity."),
            L10n.Tr("Release Candidate (RC) versions of a package will transition to Released with the current editor release. RCs are supported by Unity"),
            L10n.Tr("This package is hosted on a Scoped Registry.")
        };

        private ResourceLoader m_ResourceLoader;
        private ApplicationProxy m_Application;
        private PageManager m_PageManager;
        private PackageDatabase m_PackageDatabase;
        private UpmCache m_UpmCache;

        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_PageManager = container.Resolve<PageManager>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_UpmCache = container.Resolve<UpmCache>();
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

            m_PageManager.onVisualStateChange += OnVisualStateChange;
            detailAuthorLink.clickable.clicked += AuthorClick;
            scopedRegistryInfoBox.Q<Button>().clickable.clicked += OnInfoBoxClickMore;
            quickStart.clickable.clicked += ViewQuickStartClick;
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
            versionContainer.Add(new PackageReleaseTagLabel(m_PackageDatabase));
            versionContainer.Add(new PackageSimpleTagLabel(PackageTag.ReleaseCandidate, L10n.Tr("Release Candidate")));
            versionContainer.Add(new PackageSimpleTagLabel(PackageTag.Experimental, L10n.Tr("Experimental")));
            versionContainer.Add(new PackageScopedRegistryTagLabel());
        }

        public void Refresh(IPackage package, IPackageVersion version)
        {
            m_Package = package;
            m_Version = version;

            detailTitle.SetValueWithoutNotify(m_Version.displayName);
            detailsLinks.Refresh(m_Package, m_Version);

            UIUtils.SetElementDisplay(disabledWarningBox, version.HasTag(PackageTag.Disabled));

            RefreshName();
            RefreshDependency();
            RefreshFeatureSetElements();
            RefreshAuthor();
            RefreshTags();
            RefreshVersionLabel();
            RefreshVersionInfoIcon();
            RefreshDetailRegistry();
            RefreshEntitlement();
            RefreshDeprecationHelpBox();
            RefreshEmbeddedFeatureSetWarningBox();
            RefreshHiddenAssetInfo();
        }

        private void RefreshName()
        {
            if (!string.IsNullOrEmpty(m_Version.name))
            {
                UIUtils.SetElementDisplay(detailName, true);
                detailName.SetValueWithoutNotify(m_Version.name);
            }
            else
            {
                UIUtils.SetElementDisplay(detailName, false);
            }
        }

        private void RefreshHiddenAssetInfo()
        {
            bool showHiddenInfoBox = m_Package?.product?.isHidden == true;
            UIUtils.SetElementDisplay(hiddenAssetInfoBoxContainer, showHiddenInfoBox);
        }

        private void RefreshFeatureSetElements(VisualState visualState = null)
        {
            var featureSets = m_PackageDatabase.GetFeaturesThatUseThisPackage(m_Package.versions.installed);
            RefreshUsedInFeatureSetMessage(featureSets);
            RefreshFeatureSetDependentVersionDifferentInfoBox(featureSets);
            RefreshLockIcons(featureSets, visualState);
            RefreshQuickStart();
        }

        private void RefreshQuickStart()
        {
            var featurePackageInfo = m_Version != null && m_Version.HasTag(PackageTag.Feature) ? m_UpmCache.GetBestMatchPackageInfo(m_Version.name, m_Version.isInstalled, m_Version.versionString) : null;
            var showQuickStartButton = !string.IsNullOrEmpty(UpmPackageDocs.GetQuickStartUrl(featurePackageInfo, m_UpmCache));
            if (showQuickStartButton)
            {
                quickStart.Clear();
                quickStart.Add(new VisualElement { classList = { "quickStartIcon" } });
                quickStart.Add(new TextElement { text = L10n.Tr("QuickStart"), classList = { "quickStartText" } });
            }
            UIUtils.SetElementDisplay(quickStart, showQuickStartButton);
        }

        private void ViewQuickStartClick()
        {
            var packageInfo = m_Version != null ? m_UpmCache.GetBestMatchPackageInfo(m_Version.name, m_Version.isInstalled, m_Version.versionString) : null;
            UpmPackageDocs.ViewUrl(UpmPackageDocs.GetQuickStartUrl(packageInfo, m_UpmCache), string.Empty, L10n.Tr("quick start documentation"), "viewQuickstart", m_Version, m_Package, m_Application);
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
                visualState ??= m_PageManager.GetPage().visualStates.Get(m_Package?.uniqueId);
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
            var featureSetLink = new Button(() => { PackageManagerWindow.OpenPackageManager(version.name); });
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

        private void RefreshFeatureSetDependentVersionDifferentInfoBox(IEnumerable<IPackageVersion> featureSets)
        {
            var featureSetsCount = featureSets?.Count() ?? 0;

            // if the installed version is the feature set version and the user is viewing a different version
            if (featureSetsCount > 0 && m_Package?.versions.installed != null && m_Package.versions.installed.version == m_Package.versions.lifecycleVersion?.version
                && m_Version != m_Package.versions.installed)
            {
                featureSetDependentVersionDifferentInfoBox.text = featureSetsCount > 1 ?
                    string.Format(L10n.Tr("This package is part of the {0} features, therefore we recommend keeping the version {1} installed. Changing to a different version may affect the features' performance."), string.Join(", ", featureSets.Select(f => f.displayName)), m_Package.versions.installed.versionString)
                    : string.Format(L10n.Tr("This package is part of the {0} feature, therefore we recommend keeping the version {1} installed. Changing to a different version may affect the feature's performance."), featureSets.FirstOrDefault().displayName, m_Package.versions.installed.versionString);
                UIUtils.SetElementDisplay(featureSetDependentVersionDifferentInfoBox, true);
            }
            else
            {
                UIUtils.SetElementDisplay(featureSetDependentVersionDifferentInfoBox, false);
            }
        }

        private void RefreshAuthor()
        {
            var publisherName = m_Package?.product?.publisherName;
            var publisherLink = m_Package?.product?.publisherLink;

            var showDetailAuthorLink = !string.IsNullOrEmpty(publisherName) && !string.IsNullOrEmpty(publisherLink);
            if (showDetailAuthorLink)
                detailAuthorLink.text = publisherName;

            UIUtils.SetElementDisplay(detailAuthorLink, showDetailAuthorLink);
        }

        private void RefreshTags()
        {
            foreach (var tag in versionContainer.Children().OfType<PackageBaseTagLabel>())
                tag.Refresh(m_Version);
        }

        private void AuthorClick()
        {
            var authorLink = m_Package?.product?.publisherLink ?? string.Empty;
            if (!string.IsNullOrEmpty(authorLink))
            {
                m_Application.OpenURL(authorLink);
                PackageManagerWindowAnalytics.SendEvent("viewAuthorLink", m_Version?.uniqueId);
            }
        }

        private void RefreshEntitlement()
        {
            var showEntitlement = m_Package.hasEntitlements;
            UIUtils.SetElementDisplay(detailEntitlement, showEntitlement);
            detailEntitlement.text = showEntitlement ? "E" : string.Empty;
            detailEntitlement.tooltip = showEntitlement ? L10n.Tr("This is an Entitlement package.") : string.Empty;
        }

        private void RefreshDeprecationHelpBox()
        {
            var versionDeprecationMessage = m_Package.versions.primary.deprecationMessage;
            var showVersionDeprecation = !string.IsNullOrEmpty(versionDeprecationMessage) && m_Version.isInstalled;
            var showPackageDeprecation = m_Package.isDeprecated;

            UIUtils.SetElementDisplay(deprecatedWarningInfoBox, showPackageDeprecation);
            if (showPackageDeprecation)
            {
                if (string.IsNullOrEmpty(m_Package.deprecationMessage))
                    deprecatedWarningInfoBox.text = L10n.Tr("This package is no longer supported.");
                else
                    deprecatedWarningInfoBox.text = L10n.Tr(m_Package.deprecationMessage);
            }

            UIUtils.SetElementDisplay(deprecatedErrorInfoBox, showVersionDeprecation);
            if (showVersionDeprecation)
                deprecatedErrorInfoBox.text = L10n.Tr("This installed version of the package is deprecated. ") + L10n.Tr(versionDeprecationMessage);
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
            if (installed.IsDifferentVersionThanRequested)
            {
                UIUtils.SetElementDisplay(versionInfoIcon, true);
                versionInfoIcon.tooltip = string.Format(L10n.Tr("Unity installed version {0} because another package depends on it (version {0} overrides version {1})."),
                    installedVersionString, m_Version.versionInManifest);
                return;
            }

            // In Lifecycle V2, if a Unity package doesn't have a lifecycle version (listed in the editor manifest),
            // then that package is not considered part of the Unity Editor "product" and we need to let users know.
            var unityVersionString = m_Application.unityVersion;
            if (!m_Package.versions.hasLifecycleVersion && m_Version.HasTag(PackageTag.Unity) && !m_Version.HasTag(PackageTag.BuiltIn))
            {
                UIUtils.SetElementDisplay(versionInfoIcon, true);
                versionInfoIcon.tooltip = string.Format(L10n.Tr("This package is not officially supported for Unity {0}."), unityVersionString);
                return;
            }

            // We want to let users know when they are using a version different than the recommended.
            // The recommended version is the resolvedLifecycleVersion or the resolvedLifecycleNextVersion.
            // However, we don't want to show the info icon if the version currently installed
            // is a higher patch version of the one in the editor manifest (still considered
            // recommended).
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

        private void RefreshDetailRegistry()
        {
            var packageInfo = m_UpmCache.GetBestMatchPackageInfo(m_Version.name, m_Version.isInstalled, m_Version.versionString);
            var registry = packageInfo?.registry;
            var showRegistry = registry != null && m_Version.package.product == null;
            UIUtils.SetElementDisplay(detailRegistry, showRegistry);

            if (showRegistry)
            {
                var isByUnity = packageInfo.versions.all.Length > 0 && registry.isDefault && !m_Version.HasTag(PackageTag.InstalledFromPath);

                var detailRegistryName = isByUnity ? "Unity Registry" : registry.name;
                var detailRegistryAuthor = isByUnity ? "Unity Technologies Inc." : m_Version?.author;
                detailRegistry.tooltip = isByUnity ? registry.url : string.Empty;
                detailRegistry.enableRichText = true;

                if (!string.IsNullOrEmpty(detailRegistryName) && !string.IsNullOrEmpty(detailRegistryAuthor))
                    detailRegistry.text = string.Format(L10n.Tr("From <b>{0}</b> by {1}"), detailRegistryName, detailRegistryAuthor);
                else if (!string.IsNullOrEmpty(detailRegistryName))
                    detailRegistry.text = string.Format(L10n.Tr("From <b>{0}</b>"), detailRegistryName);
                else if (!string.IsNullOrEmpty(detailRegistryAuthor))
                    detailRegistry.text = string.Format(L10n.Tr("By {0}"), detailRegistryAuthor);
                else
                    detailRegistry.text = L10n.Tr("Author unknown");
            }

            RefreshScopedRegistryInfoBox(showRegistry, registry);
        }

        private void RefreshScopedRegistryInfoBox(bool showRegistry, RegistryInfo registry)
        {
            UIUtils.SetElementDisplay(scopedRegistryInfoBox, showRegistry);

            // To be reworked in PAX-2547
            if (m_Version.HasTag(PackageTag.Experimental))
            {
                scopedRegistryInfoBox.text = k_InfoBoxReadMoreText[(int)InfoBoxState.Experimental];
                UIUtils.SetElementDisplay(scopedRegistryInfoBox, true);
            }
            else if (m_Version.HasTag(PackageTag.PreRelease))
            {
                scopedRegistryInfoBox.text = k_InfoBoxReadMoreText[(int)InfoBoxState.PreRelease];
                UIUtils.SetElementDisplay(scopedRegistryInfoBox, true);
            }
            else if (m_Version.HasTag(PackageTag.ReleaseCandidate))
            {
                scopedRegistryInfoBox.text = k_InfoBoxReadMoreText[(int)InfoBoxState.ReleaseCandidate];
                UIUtils.SetElementDisplay(scopedRegistryInfoBox, true);
            }
            else if (showRegistry)
            {
                scopedRegistryInfoBox.text = k_InfoBoxReadMoreText[(int)InfoBoxState.ScopedRegistry];
                UIUtils.SetElementDisplay(scopedRegistryInfoBox, !registry.isDefault);
            }
        }

        private void RefreshEmbeddedFeatureSetWarningBox()
        {
            UIUtils.SetElementDisplay(embeddedFeatureSetWarningBox, m_Version.HasTag(PackageTag.Feature) && m_Version.HasTag(PackageTag.Custom));
        }

        // To be reworked in PAX-2547
        private void OnInfoBoxClickMore()
        {
            if (m_Version.HasTag(PackageTag.Experimental))
                m_Application.OpenURL($"{infoBoxUrl}{k_InfoBoxReadMoreUrl[(int)InfoBoxState.Experimental]}");
            else if (m_Version.HasTag(PackageTag.PreRelease))
                m_Application.OpenURL($"{infoBoxUrl}{k_InfoBoxReadMoreUrl[(int)InfoBoxState.PreRelease]}");
            else if (m_Version.HasTag(PackageTag.ReleaseCandidate))
                m_Application.OpenURL($"{infoBoxUrl}{k_InfoBoxReadMoreUrl[(int)InfoBoxState.ReleaseCandidate]}");
            else
                m_Application.OpenURL($"{infoBoxUrl}{k_InfoBoxReadMoreUrl[(int)InfoBoxState.ScopedRegistry]}");
        }

        private VisualElementCache cache { get; set; }

        private SelectableLabel detailTitle => cache.Get<SelectableLabel>("detailTitle");
        private Label detailEntitlement => cache.Get<Label>("detailEntitlement");
        private SelectableLabel detailVersion => cache.Get<SelectableLabel>("detailVersion");
        private VisualElement versionInfoIcon => cache.Get<VisualElement>("versionInfoIcon");

        private SelectableLabel detailName => cache.Get<SelectableLabel>("detailName");

        private Button detailAuthorLink => cache.Get<Button>("detailAuthorLink");

        private PackageDetailsLinks detailsLinks => cache.Get<PackageDetailsLinks>("detailLinksContainer");

        private VisualElement versionContainer => cache.Get<VisualElement>("versionContainer");

        private Label detailRegistry => cache.Get<Label>("detailRegistry");
        private HelpBox scopedRegistryInfoBox => cache.Get<HelpBox>("scopedRegistryInfoBox");
        private HelpBox deprecatedWarningInfoBox => cache.Get<HelpBox>("deprecatedWarningInfoBox");
        private HelpBox deprecatedErrorInfoBox => cache.Get<HelpBox>("deprecatedErrorInfoBox");
        private Button quickStart => cache.Get<Button>("quickStart");

        private VisualElement usedInFeatureSetMessageContainer => cache.Get<VisualElement>("usedInFeatureSetMessageContainer");
        private VisualElement dependencyContainer => cache.Get<VisualElement>("dependencyContainer");
        private HelpBox featureSetDependentVersionDifferentInfoBox => cache.Get<HelpBox>("featureSetDependentVersionDifferentInfoBox");
        private VisualElement lockedIcon => cache.Get<VisualElement>("lockedIcon");
        private HelpBox embeddedFeatureSetWarningBox => cache.Get<HelpBox>("embeddedFeatureSetWarningBox");
        private VisualElement hiddenAssetInfoBoxContainer => cache.Get<VisualElement>("hiddenAssetInfoBoxContainer");
        private HelpBox disabledWarningBox => cache.Get<HelpBox>("disabledWarningBox");
    }
}
