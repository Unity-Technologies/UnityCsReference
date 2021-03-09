// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
        public VisualState visualState { get; set; }

        public IPackageVersion targetVersion { get { return package?.versions.primary; } }
        public VisualElement element { get { return this; } }

        [NonSerialized]
        private bool m_FetchingDetail;

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

            BuildUI();

            UpdateExpanderUI(false);

            SetPackage(package);
            UpdateVisualState(state);
        }

        private void BuildUI()
        {
            m_MainItem = new VisualElement {name = "mainItem"};
            m_MainItem.OnLeftClick(SelectMainItem);
            Add(m_MainItem);

            // Main Item follows the `left, middle, right` layout.
            var leftContainer = new VisualElement {name = "leftContainer", classList = {"left"}};
            m_VersionLabel = new Label { name = "versionLabel", classList = { "version", "middle" } };
            var rightContainer = new VisualElement { name = "rightContainer", classList = { "right" } };

            m_MainItem.Add(leftContainer);
            m_MainItem.Add(m_VersionLabel);
            m_MainItem.Add(rightContainer);

            // Left container children
            m_ArrowExpander = new Toggle {name = "arrowExpander", classList = {"expander"}};
            m_ArrowExpander.RegisterValueChangedCallback(ToggleExpansion);
            leftContainer.Add(m_ArrowExpander);

            m_ExpanderHidden = new Label {name = "expanderHidden", classList = {"expanderHidden"}};
            leftContainer.Add(m_ExpanderHidden);

            m_NameLabel = new Label {name = "packageName", classList = {"name"}};
            leftContainer.Add(m_NameLabel);

            m_EntitlementLabel = new Label {name = "entitlementLabel"};
            UIUtils.SetElementDisplay(m_EntitlementLabel, false);
            leftContainer.Add(m_EntitlementLabel);

            // Right contains children
            m_TagContainer = new VisualElement {name = "tagContainer"};
            rightContainer.Add(m_TagContainer);

            m_Spinner = new LoadingSpinner {name = "packageSpinner"};
            rightContainer.Add(m_Spinner);

            m_StateIcon = new VisualElement {name = "stateIcon", classList = {"status"}};
            rightContainer.Add(m_StateIcon);

            // Versions dropdown
            m_VersionsContainer = new VisualElement {name = "versionsContainer"};
            Add(m_VersionsContainer);

            m_VersionList = new ScrollView {name = "versionList"};
            m_VersionsContainer.Add(m_VersionList);

            m_SeeAllVersionsLabel = new Label(L10n.Tr("See other versions")) {name = "seeAllVersions"};
            m_SeeAllVersionsLabel.OnLeftClick(SeeAllVersionsClick);
            m_VersionsContainer.Add(m_SeeAllVersionsLabel);
        }

        public void BecomesVisible()
        {
            if (m_FetchingDetail || !(package is PlaceholderPackage) || !package.Is(PackageType.AssetStore))
                return;

            m_FetchingDetail = true;
            m_PageManager.FetchDetail(package, () => { m_FetchingDetail = false; });
        }

        public void UpdateVisualState(VisualState newVisualState)
        {
            var seeAllVersionsOld = visualState?.seeAllVersions ?? false;
            var selectedVersionIdOld = visualState?.selectedVersionId ?? string.Empty;

            visualState = newVisualState?.Clone() ?? visualState ?? new VisualState(package?.uniqueId, string.Empty);

            EnableInClassList("invisible", !visualState.visible);

            if (selectedVersion != null && visualState != null && selectedVersion != targetVersion)
                visualState.seeAllVersions = visualState.seeAllVersions || !package.versions.key.Contains(selectedVersion);

            var expansionChanged = UIUtils.IsElementVisible(m_VersionsContainer) != visualState.expanded;
            if (expansionChanged)
                UpdateExpanderUI(visualState.expanded);

            var needRefreshVersions = expansionChanged || seeAllVersionsOld != visualState.seeAllVersions;
            if (needRefreshVersions)
                RefreshVersions();

            if (needRefreshVersions || selectedVersionIdOld != visualState.selectedVersionId)
                RefreshSelection();
        }

        internal void SetPackage(IPackage package)
        {
            m_FetchingDetail = false;

            var displayVersion = package?.versions.primary;
            if (displayVersion == null)
                return;

            // changing the package assigned to an item is not supported
            if (this.package != null && this.package.uniqueId != package.uniqueId)
                return;

            var oldDisplayVersion = this.package?.versions.primary;
            this.package = package;

            // if the package gets updated while it's selected, we need to do some special handling
            if (!string.IsNullOrEmpty(visualState?.selectedVersionId))
            {
                // if the primary version was selected but there is a new primary version
                // select the new primary version to keep the main item selected
                if (visualState.selectedVersionId == oldDisplayVersion?.uniqueId && oldDisplayVersion?.uniqueId != displayVersion.uniqueId)
                    m_PageManager.SetSelected(package, displayVersion);
            }

            m_NameLabel.text = displayVersion.displayName;
            m_NameLabel.ShowTextTooltipOnSizeChange();
            m_VersionLabel.text = displayVersion.versionString;
            m_VersionLabel.ShowTextTooltipOnSizeChange();

            var expandable = !package.Is(PackageType.BuiltIn);
            UIUtils.SetElementDisplay(m_ArrowExpander, expandable);
            UIUtils.SetElementDisplay(m_VersionLabel, expandable);
            UIUtils.SetElementDisplay(m_ExpanderHidden, !expandable);
            if (!expandable && UIUtils.IsElementVisible(m_VersionsContainer))
                UpdateExpanderUI(false);

            var showVersionList = !displayVersion.HasTag(PackageTag.BuiltIn) && !string.IsNullOrEmpty(package.displayName);
            UIUtils.SetElementDisplay(m_VersionList, showVersionList);

            m_TagContainer.Clear();
            var tagLabel = PackageTagLabel.CreateTagLabel(displayVersion);
            if (tagLabel != null)
                m_TagContainer.Add(tagLabel);

            RefreshState();
            RefreshVersions();
            RefreshSelection();
            RefreshEntitlement();
        }

        public void RefreshState()
        {
            var state = package?.state ?? PackageState.None;
            var progress = package?.progress ?? PackageProgress.None;
            if (state != PackageState.InProgress || progress == PackageProgress.None)
            {
                StopSpinner();

                var stateClass = state != PackageState.None ? state.ToString().ToLower() : null;
                if (!string.IsNullOrEmpty(m_CurrentStateClass))
                    m_StateIcon.RemoveFromClassList(m_CurrentStateClass);
                if (!string.IsNullOrEmpty(stateClass))
                    m_StateIcon.AddToClassList(stateClass);
                m_CurrentStateClass = stateClass;

                m_StateIcon.tooltip = GetTooltipByState(state);
            }
            else
            {
                StartSpinner();
                m_Spinner.tooltip = GetTooltipByProgress(package.progress);
            }
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
            var selectedVersion = this.selectedVersion;
            EnableInClassList(k_SelectedClassName, selectedVersion != null);
            m_MainItem.EnableInClassList(k_SelectedClassName, selectedVersion != null);
            foreach (var version in versionItems)
                version.EnableInClassList(k_SelectedClassName, selectedVersion == version.targetVersion);
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
            m_ArrowExpander.value = expanded;
            UIUtils.SetElementDisplay(m_VersionsContainer, expanded);
        }

        private void SeeAllVersionsClick()
        {
            m_PageManager.SetSeeAllVersions(package, true);
            PackageManagerWindowAnalytics.SendEvent("seeAllVersions", targetVersion?.uniqueId);
        }

        private void StartSpinner()
        {
            m_Spinner.Start();
            UIUtils.SetElementDisplay(m_StateIcon, false);
        }

        private void StopSpinner()
        {
            m_Spinner.Stop();
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
