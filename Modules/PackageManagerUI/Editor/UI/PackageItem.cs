// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageItem : VisualElement, ISelectableItem
    {
        // Note that the height here is only the height of the main item (i.e, version list is not expanded)
        internal const int k_MainItemHeight = 25;
        private const string k_SelectedClassName = "selected";
        private const string k_ExpandedClassName = "expanded";

        private string m_CurrentStateClass;

        public IPackage package { get; private set; }
        public VisualState visualState { get; private set; }

        public IPackageVersion targetVersion => package?.versions.primary;
        public VisualElement element => this;

        // Since the layout for Feature and Non-Feature are different, we want to keep track of the current layout
        // and only call BuildMainItem again when there's a layout change
        private bool? m_IsFeatureLayout = null;

        internal PackageGroup packageGroup { get; set; }

        private IPackageVersion selectedVersion
        {
            get
            {
                if (package == null || string.IsNullOrEmpty(visualState?.selectedVersionId))
                    return null;
                return package.versions.FirstOrDefault(v => v.uniqueId == visualState.selectedVersionId);
            }
        }

        internal IEnumerable<PackageVersionItem> versionItems => m_VersionList?.Children().Cast<PackageVersionItem>() ?? Enumerable.Empty<PackageVersionItem>();

        private PageManager m_PageManager;
        private PackageFiltering m_PackageFiltering;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private PackageDatabase m_PackageDatabase;
        private void ResolveDependencies(PageManager pageManager, PackageFiltering packageFiltering, PackageManagerProjectSettingsProxy settingsProxy, PackageDatabase packageDatabase)
        {
            m_PageManager = pageManager;
            m_PackageFiltering = packageFiltering;
            m_SettingsProxy = settingsProxy;
            m_PackageDatabase = packageDatabase;
        }

        public PackageItem(PageManager pageManager, PackageFiltering packageFiltering, PackageManagerProjectSettingsProxy settingsProxy, PackageDatabase packageDatabase)
        {
            ResolveDependencies(pageManager, packageFiltering, settingsProxy, packageDatabase);
        }

        public void SetPackageAndVisualState(IPackage package, VisualState state)
        {
            var isFeature = package?.Is(PackageType.Feature) == true;
            if (m_IsFeatureLayout != isFeature)
            {
                Clear();
                BuildMainItem(isFeature);
            }

            UpdateExpanderUI(false);
            SetPackage(package);
            UpdateVisualState(state);
        }

        private void BuildMainItem(bool isFeature)
        {
            m_IsFeatureLayout = isFeature;

            m_MainItem = new VisualElement {name = "mainItem"};
            m_MainItem.OnLeftClick(SelectMainItem);
            Add(m_MainItem);

            m_LeftContainer = new VisualElement {name = "leftContainer", classList = {"left"}};
            m_MainItem.Add(m_LeftContainer);

            m_ArrowExpander = new Toggle {name = "arrowExpander", classList = {"expander"}};
            m_ArrowExpander.RegisterValueChangedCallback(ToggleExpansion);
            m_LockedIcon = new Label { name = "lockedIcon" };
            m_LeftContainer.Add(m_ArrowExpander);
            m_LeftContainer.Add(m_LockedIcon);

            m_ExpanderHidden = new Label {name = "expanderHidden", classList = {"expanderHidden"}};
            m_LeftContainer.Add(m_ExpanderHidden);

            m_NameLabel = new Label {name = "packageName", classList = {"name"}};
            if (isFeature)
            {
                m_MainItem.AddToClassList("feature");
                m_NumPackagesInFeature = new Label() { name = "numPackages" };

                var leftMiddleContainer = new VisualElement() { name = "leftMiddleContainer" };
                leftMiddleContainer.Add(m_NameLabel);
                leftMiddleContainer.Add(m_NumPackagesInFeature);
                m_LeftContainer.Add(leftMiddleContainer);
            }
            else
            {
                m_LeftContainer.Add(m_NameLabel);
            }

            m_EntitlementLabel = new Label {name = "entitlementLabel"};
            UIUtils.SetElementDisplay(m_EntitlementLabel, false);
            m_LeftContainer.Add(m_EntitlementLabel);

            m_VersionLabel = new Label {name = "versionLabel", classList = {"version", "middle"}};
            m_MainItem.Add(m_VersionLabel);

            m_RightContainer = new VisualElement {name = "rightContainer", classList = {"right"}};
            m_MainItem.Add(m_RightContainer);

            m_TagContainer = new VisualElement {name = "tagContainer"};
            m_RightContainer.Add(m_TagContainer);

            m_Spinner = null;

            m_StateContainer = new VisualElement { name = "statesContainer" };
            m_MainItem.Add(m_StateContainer);

            m_StateIcon = new VisualElement { name = "stateIcon", classList = { "status" } };
            m_StateContainer.Add(m_StateIcon);

            if (isFeature)
            {
                m_InfoStateIcon = new VisualElement { name = "versionState" };
                m_StateContainer.Add(m_InfoStateIcon);
            }

            m_VersionsContainer = null;
        }

        private void BuildVersions()
        {
            m_VersionsContainer = new VisualElement {name = "versionsContainer"};
            Add(m_VersionsContainer);

            m_VersionList = new ScrollView {name = "versionList"};
            m_VersionsContainer.Add(m_VersionList);

            m_SeeAllVersionsLabel = new Label(L10n.Tr("See other versions")) {name = "seeAllVersions"};
            m_SeeAllVersionsLabel.OnLeftClick(SeeAllVersionsClick);
            m_VersionsContainer.Add(m_SeeAllVersionsLabel);
        }

        public void UpdateVisualState(VisualState newVisualState)
        {
            if (targetVersion == null)
                return;

            Refresh(newVisualState);
        }

        public void Refresh(VisualState newVisualState = null)
        {
            var previousVisualState = visualState?.Clone() ?? new VisualState(package?.uniqueId, string.Empty, false);
            visualState = newVisualState?.Clone() ?? visualState ?? new VisualState(package?.uniqueId, string.Empty, false);

            EnableInClassList("invisible", !visualState.visible);
            m_NameLabel.text = targetVersion?.displayName ?? string.Empty;
            m_VersionLabel.text = targetVersion.versionString ?? string.Empty;

            if (m_NumPackagesInFeature != null)
                m_NumPackagesInFeature.text = string.Format(L10n.Tr("{0} packages"), package.versions.primary?.dependencies?.Length ?? 0);

            var expandable = !package.Is(PackageType.BuiltIn | PackageType.Feature) && package.Is(PackageType.Upm) && m_PackageFiltering.currentFilterTab != PackageFilterTab.AssetStore;
            UIUtils.SetElementDisplay(m_ArrowExpander, expandable);
            UIUtils.SetElementDisplay(m_ExpanderHidden, !expandable);

            var showVersionLabel = !package.Is(PackageType.BuiltIn | PackageType.Feature);
            UIUtils.SetElementDisplay(m_VersionLabel, showVersionLabel);

            UIUtils.SetElementDisplay(m_LockedIcon, false);

            if (!expandable && UIUtils.IsElementVisible(m_VersionsContainer))
                UpdateExpanderUI(false);
            else
                UpdateExpanderUI(visualState.expanded);

            UpdateLockedUI(visualState.isLocked, expandable);

            var showVersionList = !targetVersion.HasTag(PackageTag.BuiltIn) && !string.IsNullOrEmpty(package.displayName);
            UIUtils.SetElementDisplay(m_VersionList, showVersionList);

            RefreshState();

            RefreshVersions();

            RefreshSelection();

            RefreshTags();
            RefreshEntitlement();
        }

        public void SetPackage(IPackage package)
        {
            this.package = package;
            name = package?.displayName ?? package?.uniqueId ?? string.Empty;
        }

        public void RefreshState()
        {
            var state = package?.state ?? PackageState.None;
            var progress = package?.progress ?? PackageProgress.None;
            if (state != PackageState.InProgress || progress == PackageProgress.None)
            {
                var stateClass = state != PackageState.None ? state.ToString().ToLower() : null;
                if (!string.IsNullOrEmpty(m_CurrentStateClass))
                    m_StateIcon.RemoveFromClassList(m_CurrentStateClass);
                if (!string.IsNullOrEmpty(stateClass))
                    m_StateIcon.AddToClassList(stateClass);
                m_CurrentStateClass = stateClass;

                m_StateIcon.tooltip = GetTooltipByState(state);
                StopSpinner();

                if (package.Is(PackageType.Feature) && state == PackageState.Installed)
                    GetState();
            }
            else
            {
                StartSpinner();
            }
        }

        private void GetState()
        {
            var featureState = FeatureState.None;
            foreach (var dependency in targetVersion.dependencies)
            {
                var packageVersion = m_PackageDatabase.GetPackageInFeatureVersion(dependency.name);
                if (packageVersion == null)
                    continue;

                var package = m_PackageDatabase.GetPackage(packageVersion);
                var installedVersion = package?.versions.installed;

                if (installedVersion == null)
                {
                    continue;
                }
                // User manually decide to install a different version
                else if ((installedVersion.isDirectDependency && package.versions.isNonLifecycleVersionInstalled) || installedVersion.HasTag(PackageTag.InDevelopment))
                {
                    featureState = FeatureState.Customized;
                    break;
                }
            }

            if (featureState == FeatureState.Customized)
            {
                m_InfoStateIcon.AddToClassList(featureState.ToString().ToLower());
                m_InfoStateIcon.tooltip = L10n.Tr("This feature has been manually customized");
            }
        }

        private void RefreshVersions()
        {
            if (!m_ArrowExpander.value)
                return;

            m_VersionList.Clear();

            var versions = package.versions.ToList();
            for (var i = versions.Count - 1; i >= 0; i--)
            {
                // even if package is not installed, we want to show the recommended label
                //  if there's more than one version shown
                var alwaysShowRecommendedLabel = versions.Count > 1;
                var isLatestVersion = i == versions.Count - 1;
                m_VersionList.Add(new PackageVersionItem(package, versions[i], alwaysShowRecommendedLabel, isLatestVersion));
            }

            var seeAllVersionsLabelVisible = package.versions.numUnloadedVersions > 0
                && (package.versions.Any(v => v.availableRegistry != RegistryType.UnityRegistry) || m_SettingsProxy.seeAllPackageVersions || package.versions.installed?.HasTag(PackageTag.Experimental) == true);
            UIUtils.SetElementDisplay(m_SeeAllVersionsLabel, seeAllVersionsLabelVisible);

            // Hack until ScrollList has a better way to do the same -- Vertical scroll bar is not yet visible
            var maxNumberOfItemBeforeScrollbar = 6;
            m_VersionList.EnableInClassList("hasScrollBar", versions.Count > maxNumberOfItemBeforeScrollbar);
        }

        public void RefreshSelection()
        {
            var enable = selectedVersion != null;
            EnableInClassList(k_SelectedClassName, enable);
            m_MainItem.EnableInClassList(k_SelectedClassName, enable);
            foreach (var versionItem in versionItems)
                versionItem.EnableInClassList(k_SelectedClassName, selectedVersion == versionItem.targetVersion);
        }

        private void RefreshTags()
        {
            m_TagContainer.Clear();
            var showTags = !package.Is(PackageType.AssetStore) && !package.Is(PackageType.BuiltIn);
            if (showTags)
            {
                var tagLabel = PackageTagLabel.CreateTagLabel(targetVersion);
                if (tagLabel != null)
                    m_TagContainer.Add(tagLabel);
            }
        }

        private void RefreshEntitlement()
        {
            var showEntitlement = package.hasEntitlements;
            UIUtils.SetElementDisplay(m_EntitlementLabel, showEntitlement);
            m_EntitlementLabel.text = showEntitlement ? "E" : string.Empty;
            m_EntitlementLabel.tooltip = showEntitlement ? L10n.Tr("This is an Entitlement package.") : string.Empty;
        }

        public void SelectMainItem()
        {
            m_PageManager.SetSelected(package, null, true);
        }

        private void ToggleExpansion(ChangeEvent<bool> evt)
        {
            SetExpanded(evt.newValue);
            if (!evt.newValue && m_PageManager.IsSeeAllVersions(package))
                m_PageManager.SetSeeAllVersions(package, false);
        }

        internal void SetExpanded(bool value)
        {
            if (!UIUtils.IsElementVisible(m_ArrowExpander))
                return;

            // mark the package as expanded in the page manager,
            // the UI will be updated through the callback chain
            if (!value || string.IsNullOrEmpty(visualState.selectedVersionId))
                SelectMainItem();

            m_PageManager.SetExpanded(package, value);
        }

        internal void UpdateLockedUI(bool showLock, bool expandable)
        {
            UIUtils.SetElementDisplay(m_ArrowExpander, !showLock && expandable);
            UIUtils.SetElementDisplay(m_LockedIcon, showLock);

            if (showLock && UIUtils.IsElementVisible(m_VersionsContainer))
                UIUtils.SetElementDisplay(m_VersionsContainer, false);
        }

        internal void UpdateExpanderUI(bool expanded)
        {
            m_MainItem.EnableInClassList(k_ExpandedClassName, expanded);
            m_ArrowExpander.SetValueWithoutNotify(expanded);

            if (expanded && m_VersionsContainer == null)
                BuildVersions();
            UIUtils.SetElementDisplay(m_VersionsContainer, expanded);
        }

        private void SeeAllVersionsClick()
        {
            m_PageManager.SetSeeAllVersions(package, true);
            PackageManagerWindowAnalytics.SendEvent("seeAllVersions", targetVersion?.uniqueId);
        }

        private void StartSpinner()
        {
            if (m_Spinner == null)
            {
                m_Spinner = new LoadingSpinner {name = "packageSpinner"};
                m_StateContainer.Insert(0, m_Spinner);
            }

            m_Spinner.Start();
            m_Spinner.tooltip = GetTooltipByProgress(package.progress);
            UIUtils.SetElementDisplay(m_StateIcon, false);
        }

        private void StopSpinner()
        {
            m_Spinner?.Stop();
            UIUtils.SetElementDisplay(m_StateIcon, true);
        }

        private Label m_NameLabel;
        private Label m_SeeAllVersionsLabel;
        private VisualElement m_TagContainer;
        private VisualElement m_MainItem;
        private VisualElement m_StateIcon;
        private VisualElement m_InfoStateIcon;
        private VisualElement m_StateContainer;
        private Label m_EntitlementLabel;
        private Label m_VersionLabel;
        private LoadingSpinner m_Spinner;
        private Toggle m_ArrowExpander;
        private Label m_LockedIcon;
        private Label m_ExpanderHidden;
        private VisualElement m_VersionsContainer;
        private ScrollView m_VersionList;
        private VisualElement m_LeftContainer;
        private VisualElement m_RightContainer;
        private Label m_NumPackagesInFeature;

        private static readonly string[] k_TooltipsByState =
        {
            "",
            L10n.Tr("This {0} is installed."),
            // Keep the error message for `installed` and `installedAsDependency` the same for now as requested by the designer
            L10n.Tr("This {0} is installed."),
            L10n.Tr("This {0} is available for download."),
            L10n.Tr("This {0} is available for import."),
            L10n.Tr("This {0} is in development."),
            L10n.Tr("A newer version of this {0} is available."),
            "",
            L10n.Tr("There are errors with this {0}. Please read the {0} details for further guidance."),
            L10n.Tr("There are warnings with this {0}. Please read the {0} details for further guidance.")
        };

        public string GetTooltipByState(PackageState state)
        {
            return string.Format(k_TooltipsByState[(int)state], package.GetDescriptor());
        }

        private static readonly string[] k_TooltipsByProgress =
        {
            "",
            L10n.Tr("{0} refreshing in progress."),
            L10n.Tr("{0} downloading in progress."),
            L10n.Tr("{0} pausing in progress."),
            L10n.Tr("{0} resuming in progress."),
            L10n.Tr("{0} installing in progress."),
            L10n.Tr("{0} resetting in progress."),
            L10n.Tr("{0} removing in progress.")
        };

        public string GetTooltipByProgress(PackageProgress progress)
        {
            return string.Format(k_TooltipsByProgress[(int)progress], package.GetDescriptor(true));
        }
    }
}
