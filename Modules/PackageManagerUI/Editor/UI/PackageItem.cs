// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
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

        internal IEnumerable<PackageVersionItem> versionItems { get { return versionList.Children().Cast<PackageVersionItem>(); } }

        public PackageItem(IPackage package, VisualState state)
        {
            var root = Resources.GetTemplate("PackageItem.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            mainItem.OnLeftClick(SelectMainItem);
            seeAllVersionsLabel.OnLeftClick(SeeAllVersionsClick);
            arrowExpander.RegisterValueChangedCallback(ToggleExpansion);

            UpdateExpanderUI(false);

            SetPackage(package);
            UpdateVisualState(state);
        }

        public void BecomesVisible()
        {
            if (m_FetchingDetail || !(package is PlaceholderPackage) || !package.Is(PackageType.AssetStore))
                return;

            if (long.TryParse(package.uniqueId, out var productId))
            {
                m_FetchingDetail = true;
                AssetStoreClient.instance.FetchDetail(productId, () => { m_FetchingDetail = false; });
            }
        }

        public void UpdateVisualState(VisualState newVisualState)
        {
            var seeAllVersionsOld = visualState?.seeAllVersions ?? false;
            var selectedVersionIdOld = visualState?.selectedVersionId ?? string.Empty;

            visualState = newVisualState?.Clone() ?? visualState ?? new VisualState(package?.uniqueId, string.Empty);

            EnableInClassList("invisible", !visualState.visible);

            if (selectedVersion != null && visualState != null && selectedVersion != targetVersion)
                visualState.seeAllVersions = visualState.seeAllVersions || !package.versions.key.Contains(selectedVersion);

            var expansionChanged = UIUtils.IsElementVisible(versionsContainer) != visualState.expanded;
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
                    PageManager.instance.SetSelected(package, displayVersion);
            }

            nameLabel.text = displayVersion.displayName;
            versionLabel.text = displayVersion.versionString;

            var expandable = !package.Is(PackageType.BuiltIn);
            UIUtils.SetElementDisplay(arrowExpander, expandable);
            UIUtils.SetElementDisplay(versionLabel, expandable);
            UIUtils.SetElementDisplay(expanderHidden, !expandable);
            if (!expandable && UIUtils.IsElementVisible(versionsContainer))
                UpdateExpanderUI(false);

            var showVersionList = !displayVersion.HasTag(PackageTag.BuiltIn) && !string.IsNullOrEmpty(package.displayName);
            UIUtils.SetElementDisplay(versionList, showVersionList);

            tagContainer.Clear();
            var tagLabel = PackageTagLabel.CreateTagLabel(displayVersion);
            if (tagLabel != null)
                tagContainer.Add(tagLabel);

            RefreshState();
            RefreshVersions();
            RefreshSelection();
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
                    stateIcon.RemoveFromClassList(m_CurrentStateClass);
                if (!string.IsNullOrEmpty(stateClass))
                    stateIcon.AddToClassList(stateClass);
                m_CurrentStateClass = stateClass;

                stateIcon.tooltip = GetTooltipByState(state);
            }
            else
            {
                StartSpinner();
                spinner.tooltip = GetTooltipByProgress(package.progress);
            }
        }

        private void RefreshVersions()
        {
            if (!arrowExpander.value)
                return;

            versionList.Clear();

            var seeAllVersions = visualState?.seeAllVersions ?? false;

            var keyVersions = package.versions.key.ToList();
            var allVersions = package.versions.ToList();

            var versions = seeAllVersions ? allVersions : keyVersions;

            for (var i = versions.Count - 1; i >= 0; i--)
                versionList.Add(new PackageVersionItem(package, versions[i]));

            var seeAllVersionsLabelVisible = !seeAllVersions && allVersions.Count > keyVersions.Count;
            UIUtils.SetElementDisplay(seeAllVersionsLabel, seeAllVersionsLabelVisible);

            // Hack until ScrollList has a better way to do the same -- Vertical scroll bar is not yet visible
            var maxNumberOfItemBeforeScrollbar = 6;
            versionList.EnableInClassList("hasScrollBar", versions.Count > maxNumberOfItemBeforeScrollbar);
        }

        public void RefreshSelection()
        {
            var selectedVersion = this.selectedVersion;
            EnableInClassList(k_SelectedClassName, selectedVersion != null);
            mainItem.EnableInClassList(k_SelectedClassName, selectedVersion != null);
            foreach (var version in versionItems)
                version.EnableInClassList(k_SelectedClassName, selectedVersion == version.targetVersion);
        }

        public void SelectMainItem()
        {
            PageManager.instance.SetSelected(package, null, true);
        }

        private void ToggleExpansion(ChangeEvent<bool> evt)
        {
            SetExpanded(evt.newValue);
        }

        internal void SetExpanded(bool value)
        {
            if (!UIUtils.IsElementVisible(arrowExpander))
                return;

            // mark the package as expanded in the page manager,
            // the UI will be updated through the callback chain
            if (!value || string.IsNullOrEmpty(visualState.selectedVersionId))
                SelectMainItem();

            PageManager.instance.SetExpanded(package, value);
        }

        internal void UpdateExpanderUI(bool expanded)
        {
            mainItem.EnableInClassList(k_ExpandedClassName, expanded);
            arrowExpander.value = expanded;
            UIUtils.SetElementDisplay(versionsContainer, expanded);
        }

        private void SeeAllVersionsClick()
        {
            PageManager.instance.SetSeeAllVersions(package, true);
        }

        private void StartSpinner()
        {
            spinner.Start();
            UIUtils.SetElementDisplay(stateIcon, false);
        }

        private void StopSpinner()
        {
            spinner.Stop();
            UIUtils.SetElementDisplay(stateIcon, true);
        }

        private VisualElementCache cache { get; set; }

        private Label nameLabel { get { return cache.Get<Label>("packageName"); } }
        private Label seeAllVersionsLabel { get { return cache.Get<Label>("seeAllVersions"); } }
        private VisualElement tagContainer => cache.Get<VisualElement>("tagContainer");
        private VisualElement mainItem { get { return cache.Get<VisualElement>("mainItem"); } }
        private VisualElement stateIcon { get { return cache.Get<VisualElement>("stateIcon"); } }
        private Label versionLabel { get { return cache.Get<Label>("versionLabel"); } }
        private LoadingSpinner spinner { get { return cache.Get<LoadingSpinner>("packageSpinner"); } }
        private Toggle arrowExpander { get { return cache.Get<Toggle>("arrowExpander"); } }
        private Label expanderHidden { get { return cache.Get<Label>("expanderHidden"); } }
        private VisualElement versionsContainer { get { return cache.Get<VisualElement>("versionsContainer"); } }
        private ScrollView versionList { get { return cache.Get<ScrollView>("versionList"); } }

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
            "There are errors with this package. Please read the package details for further guidance."
        };

        public static string GetTooltipByState(PackageState state)
        {
            return ApplicationUtil.instance.GetTranslationForText(k_TooltipsByState[(int)state]);
        }

        private static readonly string[] k_TooltipsByProgress =
        {
            "",
            "Package refreshing in progress.",
            "Package downloading in progress.",
            "Package installing in progress.",
            "Package removing in progress."
        };

        public static string GetTooltipByProgress(PackageProgress progress)
        {
            return ApplicationUtil.instance.GetTranslationForText(k_TooltipsByProgress[(int)progress]);
        }
    }
}
