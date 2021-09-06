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
            "/Documentation/Manual/pack-prerelease.html",
            "/Documentation/Manual/pack-experimental.html",
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
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_PageManager = container.Resolve<PageManager>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
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
            scopedRegistryInfoBox.Q<Button>().clickable.clicked += OnInfoBoxClickMore;
        }

        public void Refresh(IPackage package, IPackageVersion version)
        {
            m_Package = package;
            m_Version = version;

            detailTitle.SetValueWithoutNotify(m_Version.displayName);
            detailsLinks.Refresh(m_Package, m_Version);

            RefreshFeatureSetElements();
            RefreshAuthor();
            RefreshTags();
            RefreshVersionLabel();
            RefreshVersionInfoIcon();
            RefreshRegistry();
            RefreshEntitlement();
            RefreshEmbeddedFeatureSetWarningBox();
            RefreshHiddenAssetInfo();
        }

        private void RefreshHiddenAssetInfo()
        {
            bool showHiddenInfoBox = m_Package is AssetStorePackage && (m_Package as AssetStorePackage).isHidden;
            UIUtils.SetElementDisplay(hiddenAssetInfoBoxContainer, showHiddenInfoBox);
        }

        private void RefreshFeatureSetElements()
        {
            var featureSets = m_PackageDatabase.GetFeatureDependents(m_Package.versions.installed);
            RefreshUsedInFeatureSetMessage(featureSets);
            RefreshFeatureSetDependentVersionDifferentInfoBox(featureSets);
            RefreshLockIcons(featureSets);
        }

        private void RefreshLockIcons(IEnumerable<IPackageVersion> featureSets)
        {
            if (featureSets?.Any() == true)
            {
                var visualState = m_PageManager.GetVisualState(m_Package);
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
                    lockedIcon.tooltip = string.Empty;
                }
                UIUtils.SetElementDisplay(lockedIcon, true);
            }
            else
                UIUtils.SetElementDisplay(lockedIcon, false);
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
                var element = new VisualElement {name = "usedInFeatureSetIconAndMessageContainer"};
                var icon = new VisualElement {name = "featureSetIcon"};
                element.Add(icon);

                var message = new Label {name = "usedInFeatureSetMessageLabel"};
                message.text = string.Format(L10n.Tr("This {0} is installed as part of "), m_Package.GetDescriptor());

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
                    usedInFeatureSetMessageContainer.Add(new Label(L10n.Tr(" Features.")));
                }
                else
                {
                    usedInFeatureSetMessageContainer.Add(new Label(L10n.Tr(" Feature.")));
                }
            }
        }

        private void RefreshFeatureSetDependentVersionDifferentInfoBox(IEnumerable<IPackageVersion> featureSets)
        {
            var featureSetsCount = featureSets?.Count() ?? 0;

            // if the installed version is the Feature Set version and the user is viewing a different version
            if (featureSetsCount > 0 && m_Package?.versions.installed != null && m_Package.versions.installed.version == m_Package.versions.lifecycleVersion?.version
                && m_Version != m_Package.versions.installed)
            {
                featureSetDependentVersionDifferentInfoBox.text = featureSetsCount > 1 ?
                    string.Format(L10n.Tr("This Package is part of the {0} Features, therefore we recommend keeping the version {1} installed. Changing to a different version may affect the Features' performance."), string.Join(", ", featureSets.Select(f => f.displayName)), m_Package.versions.installed.versionString)
                    : string.Format(L10n.Tr("This Package is part of the {0} Feature, therefore we recommend keeping the version {1} installed. Changing to a different version may affect the Feature's performance."), featureSets.FirstOrDefault().displayName, m_Package.versions.installed.versionString);
                UIUtils.SetElementDisplay(featureSetDependentVersionDifferentInfoBox, true);
            }
            else
            {
                UIUtils.SetElementDisplay(featureSetDependentVersionDifferentInfoBox, false);
            }
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
            var showVersionLabel = !m_Package.Is(PackageType.BuiltIn) && !m_Package.Is(PackageType.Feature) && !string.IsNullOrEmpty(versionString);
            UIUtils.SetElementDisplay(detailVersion, showVersionLabel);
            if (!showVersionLabel)
                return;

            var releaseDateString = m_Version.publishedDate?.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("en-US"));
            detailVersion.SetValueWithoutNotify(string.IsNullOrEmpty(releaseDateString)
                ? string.Format(L10n.Tr("Version {0}"), versionString)
                : string.Format(L10n.Tr("Version {0} - {1}"), versionString, releaseDateString));
        }

        private void RefreshVersionInfoIcon()
        {
            var installed = m_Package?.versions?.installed;
            if (installed == null)
            {
                UIUtils.SetElementDisplay(versionInfoIcon, false);
                return;
            }

            var installedVersionString = installed.versionString;
            if (UpmPackageVersion.IsDifferentVersionThanRequested(installed))
            {
                UIUtils.SetElementDisplay(versionInfoIcon, true);

                if (UpmPackageVersion.IsRequestedButOverriddenVersion(m_Package, m_Version))
                    versionInfoIcon.tooltip = string.Format(L10n.Tr("Unity installed version {0} because another package depends on it (version {0} overrides version {1})."),
                        installedVersionString, m_Version.versionString);
                else if (m_Version.isInstalled)
                    versionInfoIcon.tooltip = L10n.Tr("At least one other package depends on this version of the package.");
                else
                    versionInfoIcon.tooltip = string.Format(L10n.Tr("At least one other package depends on version {0} of this package."), installedVersionString);
                return;
            }

            if (m_Version?.isInstalled == true &&  m_Package?.state != PackageState.InstalledAsDependency
                                               && installed.version?.IsEqualOrPatchOf(m_Package?.versions?.recommended.version) != true)
            {
                UIUtils.SetElementDisplay(versionInfoIcon, true);
                versionInfoIcon.tooltip = string.Format(L10n.Tr("The installed version {0} is not verified for your Unity version. The recommended one is {1}."), installedVersionString, m_Package?.versions?.recommended.versionString);
                return;
            }

            UIUtils.SetElementDisplay(versionInfoIcon, false);
        }

        private void RefreshRegistry()
        {
            var registry = m_Version.packageInfo?.registry;
            var showRegistry = registry != null;
            UIUtils.SetElementDisplay(detailRegistryContainer, showRegistry);
            if (showRegistry)
            {
                scopedRegistryInfoBox.text = k_InfoBoxReadMoreText[(int)InfoBoxState.ScopedRegistry];
                UIUtils.SetElementDisplay(scopedRegistryInfoBox, !registry.isDefault);
                if (!m_Version.packageInfo.versions.all.Any())
                {
                    detailRegistryName.text = L10n.Tr("Unknown");
                    detailRegistryName.tooltip = string.Empty;
                }
                else
                {
                    detailRegistryName.text = registry.isDefault ? "Unity" : registry.name;
                    detailRegistryName.tooltip = registry.url;
                }
            }
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
        }

        private void RefreshEmbeddedFeatureSetWarningBox()
        {
            UIUtils.SetElementDisplay(embeddedFeatureSetWarningBox, m_Package.Is(PackageType.Feature) && m_Version.HasTag(PackageTag.Custom));
        }

        private void OnInfoBoxClickMore()
        {
            if (m_Version.HasTag(PackageTag.PreRelease))
                m_Application.OpenURL($"{infoBoxUrl}{k_InfoBoxReadMoreUrl[(int)InfoBoxState.PreRelease]}");
            else if (m_Version.HasTag(PackageTag.Experimental))
                m_Application.OpenURL($"{infoBoxUrl}{k_InfoBoxReadMoreUrl[(int)InfoBoxState.Experimental]}");
            else if (m_Version.HasTag(PackageTag.ReleaseCandidate))
                m_Application.OpenURL($"{infoBoxUrl}{k_InfoBoxReadMoreUrl[(int)InfoBoxState.ReleaseCandidate]}");
            else if (m_Package.Is(PackageType.ScopedRegistry))
                m_Application.OpenURL($"{infoBoxUrl}{k_InfoBoxReadMoreUrl[(int)InfoBoxState.ScopedRegistry]}");
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

        private VisualElement detailRegistryContainer => cache.Get<VisualElement>("detailRegistryContainer");
        private HelpBox scopedRegistryInfoBox => cache.Get<HelpBox>("scopedRegistryInfoBox");
        private Label detailRegistryName => cache.Get<Label>("detailRegistryName");

        private VisualElement usedInFeatureSetMessageContainer => cache.Get<VisualElement>("usedInFeatureSetMessageContainer");
        private HelpBox featureSetDependentVersionDifferentInfoBox => cache.Get<HelpBox>("featureSetDependentVersionDifferentInfoBox");
        private VisualElement lockedIcon => cache.Get<VisualElement>("lockedIcon");
        private HelpBox embeddedFeatureSetWarningBox => cache.Get<HelpBox>("embeddedFeatureSetWarningBox");
        private VisualElement hiddenAssetInfoBoxContainer => cache.Get<VisualElement>("hiddenAssetInfoBoxContainer");
    }
}
