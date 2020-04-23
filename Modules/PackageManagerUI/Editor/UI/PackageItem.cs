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
        internal const float k_ElementHeight = 25.0f;

        private string m_CurrentStateClass;

        public IPackage package { get; private set; }
        private VisualState visualState { get; set; }

        public IPackageVersion targetVersion { get { return package?.versions.primary; } }
        public VisualElement element { get { return this; } }

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

        private ResourceLoader m_ResourceLoader;
        private PageManager m_PageManager;
        private void ResolveDependencies(ResourceLoader resourceLoader, PageManager pageManager)
        {
            m_ResourceLoader = resourceLoader;
            m_PageManager = pageManager;
        }

        public PackageItem(ResourceLoader resourceLoader, PageManager pageManager, IPackage package)
        {
            ResolveDependencies(resourceLoader, pageManager);

            var root = m_ResourceLoader.GetTemplate("PackageItem.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            itemLabel.OnLeftClick(SelectMainItem);
            seeAllVersionsLabel.OnLeftClick(SeeAllVersionsClick);
            arrowExpander.RegisterValueChangedCallback(ToggleExpansion);
            nameLabel.ShowTextTooltipOnSizeChange();

            UpdateExpanderUI(false);

            SetPackage(package);
            UpdateVisualState();
        }

        public void UpdateVisualState(VisualState newVisualState = null)
        {
            var seeAllVersionsOld = visualState?.seeAllVersions ?? false;
            var selectedVersionIdOld = visualState?.selectedVersionId ?? string.Empty;

            visualState = newVisualState?.Clone() ?? visualState ?? new VisualState(package?.uniqueId);

            if (UIUtils.IsElementVisible(this) != visualState.visible)
                UIUtils.SetElementDisplay(this, visualState.visible);

            if (selectedVersion != null && visualState != null && selectedVersion != targetVersion)
            {
                var keyVersions = package.versions.key.Where(p => p != package.versions.primary);
                var allVersions = package.versions.Where(p => p != package.versions.primary);

                visualState.expanded = visualState.expanded || allVersions.Contains(selectedVersion);
                visualState.seeAllVersions = visualState.seeAllVersions || !keyVersions.Contains(selectedVersion);
            }

            var expansionChanged = UIUtils.IsElementVisible(versionsContainer) != visualState.expanded;
            if (expansionChanged)
                UpdateExpanderUI(visualState.expanded);

            if (expansionChanged || seeAllVersionsOld != visualState.seeAllVersions)
                RefreshVersions();

            if (selectedVersionIdOld != visualState.selectedVersionId)
                RefreshSelection();
        }

        internal void SetPackage(IPackage package)
        {
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

            nameLabel.text = displayVersion.displayName;

            var expandable = package.versions.Skip(1).Any();
            UIUtils.SetElementDisplay(arrowExpander, expandable);
            UIUtils.SetElementDisplay(expanderHidden, !expandable);
            if (!expandable && UIUtils.IsElementVisible(versionsContainer))
                UpdateExpanderUI(false);

            var showVersionLabel = !displayVersion.HasTag(PackageTag.BuiltIn) && !string.IsNullOrEmpty(package.displayName);
            UIUtils.SetElementDisplay(versionList, showVersionLabel);
            UIUtils.SetElementDisplay(versionLabel, showVersionLabel);
            if (showVersionLabel)
            {
                var text = GetVersionText(displayVersion, true);
                versionLabel.text = text == "0.0.0" ? string.Empty : text;
            }

            UpdateStatusIcon();
            RefreshVersions();
            RefreshSelection();
        }

        public void UpdateStatusIcon()
        {
            var state = package?.state ?? PackageState.None;
            var progress = package?.progress ?? PackageProgress.None;
            if (state != PackageState.InProgress || progress == PackageProgress.None)
            {
                StopSpinner();

                var stateClass = state != PackageState.None ? state.ToString().ToLower() : null;
                if (!string.IsNullOrEmpty(m_CurrentStateClass))
                    stateLabel.RemoveFromClassList(m_CurrentStateClass);
                if (!string.IsNullOrEmpty(stateClass))
                    stateLabel.AddToClassList(stateClass);
                m_CurrentStateClass = stateClass;

                stateLabel.tooltip = GetTooltipByState(state);
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

            var additionalKeyVersions = package.versions.key.Where(p => p != package.versions.primary).ToList();
            var additionalVersions = package.versions.Where(p => p != package.versions.primary).ToList();

            var versions = seeAllVersions ? additionalVersions : additionalKeyVersions;

            for (var i = versions.Count - 1; i >= 0; i--)
                versionList.Add(new PackageVersionItem(package, versions[i]));

            var showToolbar = !seeAllVersions && additionalVersions.Count > additionalKeyVersions.Count;
            UIUtils.SetElementDisplay(versionToolbar, showToolbar);

            FixVersionListStyle(versions);
        }

        // TODO: Hard-code until scrollView can size to its content
        // Note: ListItemMaxHeight is used because there is an issue with VisualElement where at construction time,
        //          styling is not yet applied and max height returns 0 even though the stylesheet has it at 150.
        private void FixVersionListStyle(List<IPackageVersion> versions)
        {
            const int listItemSpacing = 16 + 4 + 4;
            const int listItemMaxHeight = 150;

            var maxHeight = Math.Max(versionList.style.maxHeight.value.value, listItemMaxHeight);
            versionList.style.minHeight = Math.Min(versions.Count * listItemSpacing, maxHeight);

            // Hack until ScrollList has a better way to do the same -- Vertical scroll bar is not yet visible
            var maxNumberOfItemBeforeScrollbar = 6;
            versionList.EnableClass("hasScrollBar", versions.Count > maxNumberOfItemBeforeScrollbar);
        }

        public void RefreshSelection()
        {
            var selectedVersion = this.selectedVersion;
            itemLabel.EnableClass(UIUtils.k_SelectedClassName, selectedVersion == targetVersion);
            foreach (var version in versionItems)
                version.EnableClass(UIUtils.k_SelectedClassName, selectedVersion == version.targetVersion);
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
            // mark the package as expanded in the page manager,
            // the UI will be updated through the callback chain

            if (!value || string.IsNullOrEmpty(visualState.selectedVersionId))
                SelectMainItem();

            m_PageManager.SetExpanded(package, value);
        }

        internal void UpdateExpanderUI(bool expanded)
        {
            arrowExpander.value = expanded;
            UIUtils.SetElementDisplay(versionsContainer, expanded);
        }

        private void SeeAllVersionsClick()
        {
            m_PageManager.SetSeeAllVersions(package, true);
        }

        private void StartSpinner()
        {
            spinner.Start();
            UIUtils.SetElementDisplay(stateLabel, false);
        }

        private void StopSpinner()
        {
            spinner.Stop();
            UIUtils.SetElementDisplay(stateLabel, true);
        }

        public IEnumerable<ISelectableItem> GetSelectableItems()
        {
            yield return this;
            if (arrowExpander.value)
                foreach (var version in versionItems)
                    yield return version;
        }

        private VisualElementCache cache { get; set; }

        private Label nameLabel { get { return cache.Get<Label>("packageName"); } }
        private Label stateLabel { get { return cache.Get<Label>("packageState"); } }
        private Label versionLabel { get { return cache.Get<Label>("packageVersion"); } }
        private Label seeAllVersionsLabel { get { return cache.Get<Label>("seeAllVersions"); } }
        private VisualElement versionToolbar { get { return cache.Get<VisualElement>("versionsToolbar"); } }
        private VisualElement itemLabel { get { return cache.Get<VisualElement>("itemLabel"); } }
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
            "This package is available for import.",
            "This package is in development.",
            "A newer version of this package is available.",
            "",
            "There are errors with this package. Please read the package details for further guidance."
        };

        public string GetTooltipByState(PackageState state)
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

        public string GetTooltipByProgress(PackageProgress progress)
        {
            return L10n.Tr(k_TooltipsByProgress[(int)progress]);
        }

        public static string GetVersionText(IPackageVersion version, bool simplified = false)
        {
            if (version?.version == null)
                return version?.versionString;

            var label = version.version?.StripTag();
            if (!simplified)
            {
                if (version.HasTag(PackageTag.Local))
                    label = string.Format(L10n.Tr("local - {0}"), label);
                if (version.HasTag(PackageTag.Git))
                    label = string.Format(L10n.Tr("git - {0}"), label);
                if (version.HasTag(PackageTag.Verified))
                    label = string.Format(L10n.Tr("verified - {0}"), label);
                if (version.isInstalled)
                    label = string.Format(L10n.Tr("current - {0}"), label);
            }
            if (version.HasTag(PackageTag.Preview))
            {
                var previewLabel = string.IsNullOrEmpty(version.version?.Prerelease) ? L10n.Tr("preview") : version.version?.Prerelease;
                label = $"{previewLabel} - {label}";
            }
            return label;
        }
    }
}
