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
        internal const float k_MainItemHeight = 25.0f;
        private const string k_SelectedClassName = "selected";
        private const string k_ExpandedClassName = "expanded";

        private string m_CurrentStateClass;

        public IPackage package { get; private set; }
        public VisualState visualState { get; private set; }

        public IPackageVersion targetVersion => package?.versions.primary;
        public VisualElement element => this;

        private bool m_VisibleInScrollView;
        public bool visibleInScrollView
        {
            get => m_VisibleInScrollView;
            set
            {
                if (value == m_VisibleInScrollView)
                    return;

                m_VisibleInScrollView = value;
                RefreshState();
                RefreshSpinner();
                if (m_VisibleInScrollView)
                {
                    Refresh();
                    IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint | VersionChangeType.Transform);
                }
            }
        }

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
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private void ResolveDependencies(PageManager pageManager, PackageManagerProjectSettingsProxy settingsProxy)
        {
            m_PageManager = pageManager;
            m_SettingsProxy = settingsProxy;
        }

        public PackageItem(PageManager pageManager, PackageManagerProjectSettingsProxy settingsProxy, IPackage package, VisualState state)
        {
            ResolveDependencies(pageManager, settingsProxy);

            BuildMainItem();

            UpdateExpanderUI(false);

            SetPackage(package);
            UpdateVisualState(state);
        }

        private void BuildMainItem()
        {
            m_MainItem = new VisualElement {name = "mainItem"};
            m_MainItem.OnLeftClick(SelectMainItem);
            Add(m_MainItem);

            m_LeftContainer = new VisualElement {name = "leftContainer", classList = {"left"}};
            m_MainItem.Add(m_LeftContainer);

            m_ArrowExpander = new Toggle {name = "arrowExpander", classList = {"expander"}};
            m_ArrowExpander.RegisterValueChangedCallback(ToggleExpansion);
            m_LeftContainer.Add(m_ArrowExpander);

            m_ExpanderHidden = new Label {name = "expanderHidden", classList = {"expanderHidden"}};
            m_LeftContainer.Add(m_ExpanderHidden);

            m_NameLabel = new Label {name = "packageName", classList = {"name"}};
            m_LeftContainer.Add(m_NameLabel);

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

            m_StateIcon = new VisualElement {name = "stateIcon", classList = {"status"}};
            m_RightContainer.Add(m_StateIcon);

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
            var previousVisualState = visualState?.Clone() ?? new VisualState(package?.uniqueId, string.Empty);
            visualState = newVisualState?.Clone() ?? visualState ?? new VisualState(package?.uniqueId, string.Empty);

            EnableInClassList("invisible", !visualState.visible);
            m_NameLabel.text = targetVersion?.displayName ?? string.Empty;
            m_VersionLabel.text = targetVersion.versionString ?? string.Empty;

            var expandable = !package.Is(PackageType.BuiltIn);
            UIUtils.SetElementDisplay(m_ArrowExpander, expandable);
            UIUtils.SetElementDisplay(m_VersionLabel, expandable);
            UIUtils.SetElementDisplay(m_ExpanderHidden, !expandable);

            if (!expandable && UIUtils.IsElementVisible(m_VersionsContainer))
                UpdateExpanderUI(false);
            else
                UpdateExpanderUI(visualState.expanded);

            var showVersionList = !targetVersion.HasTag(PackageTag.BuiltIn) && !string.IsNullOrEmpty(package.displayName);
            UIUtils.SetElementDisplay(m_VersionList, showVersionList);

            var version = selectedVersion;
            if (version != null && version != targetVersion)
                visualState.seeAllVersions = visualState.seeAllVersions || !package.versions.key.Contains(version);

            RefreshState();

            var expansionChanged = previousVisualState.expanded != visualState.expanded;
            var seeAllVersionsChanged = previousVisualState.seeAllVersions != visualState.seeAllVersions;
            var needRefreshVersions = showVersionList && (expansionChanged || seeAllVersionsChanged);
            if (needRefreshVersions)
                RefreshVersions();

            var selectedVersionIdOld = previousVisualState.selectedVersionId ?? string.Empty;
            if (needRefreshVersions || selectedVersionIdOld != visualState.selectedVersionId)
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
            }
            else
            {
                StartSpinner();
            }
        }

        private void RefreshSpinner()
        {
            if (!m_VisibleInScrollView)
            {
                StopSpinner();
                return;
            }

            var state = package?.state ?? PackageState.None;
            var progress = package?.progress ?? PackageProgress.None;
            if (state != PackageState.InProgress || progress == PackageProgress.None)
                StopSpinner();
            else
                StartSpinner();
        }

        private void RefreshVersions()
        {
            if (!m_ArrowExpander.value)
                return;

            m_VersionList.Clear();

            var seeAllVersions = visualState?.seeAllVersions ?? false;
            var keyVersions = package.versions.key.ToList();
            var allVersions = package.versions.ToList();

            var versions = seeAllVersions ? allVersions : keyVersions;

            for (var i = versions.Count - 1; i >= 0; i--)
            {
                // even if package is not installed, we want to show the recommended label
                //  if there's more than one version shown
                var alwaysShowRecommendedLabel = versions.Count > 1;
                var isLatestVersion = i == versions.Count - 1;
                m_VersionList.Add(new PackageVersionItem(package, versions[i], alwaysShowRecommendedLabel, isLatestVersion));
            }

            var seeAllVersionsLabelVisible = !seeAllVersions && allVersions.Count > keyVersions.Count
                && (package.Is(PackageType.ScopedRegistry) || m_SettingsProxy.seeAllPackageVersions || package.versions.installed?.HasTag(PackageTag.Experimental) == true);
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
                m_RightContainer.Add(m_Spinner);
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
        private Label m_EntitlementLabel;
        private Label m_VersionLabel;
        private LoadingSpinner m_Spinner;
        private Toggle m_ArrowExpander;
        private Label m_ExpanderHidden;
        private VisualElement m_VersionsContainer;
        private ScrollView m_VersionList;
        private VisualElement m_LeftContainer;
        private VisualElement m_RightContainer;

        private static readonly string[] k_TooltipsByState =
        {
            "",
            "This package is installed.",
            // Keep the error message for `installed` and `installedAsDependency` the same for now as requested by the designer
            "This package is installed.",
            "This package is available for download.",
            "This package is available for import.",
            "This package is in development.",
            "A newer version of this package is available.",
            "",
            "There are errors with this package. Please read the package details for further guidance.",
            "There are warnings with this package. Please read the package details for further guidance."
        };

        public static string GetTooltipByState(PackageState state)
        {
            return L10n.Tr(k_TooltipsByState[(int)state]);
        }

        private static readonly string[] k_TooltipsByProgress =
        {
            "",
            "Package refreshing in progress.",
            "Package downloading in progress.",
            "Package pausing in progress.",
            "Package resuming in progress.",
            "Package installing in progress.",
            "Package removing in progress."
        };

        public static string GetTooltipByProgress(PackageProgress progress)
        {
            return L10n.Tr(k_TooltipsByProgress[(int)progress]);
        }
    }
}
