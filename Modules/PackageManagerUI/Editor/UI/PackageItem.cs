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
        internal new class UxmlFactory : UxmlFactory<PackageItem> {}

        private string m_CurrentStateClass;

        public IPackage package { get; private set; }
        private VisualState visualState { get; set; }

        public IPackageVersion targetVersion { get { return package?.primaryVersion; } }
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

        // the item is only expandable when there are more than one versions
        private bool expandable { get { return package?.versions.Skip(1).Any() ?? false; } }

        public PackageItem() : this(null)
        {
        }

        public PackageItem(IPackage package)
        {
            var root = Resources.GetTemplate("PackageItem.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            itemLabel.OnLeftClick(SelectMainItem);
            seeAllVersionsLabel.OnLeftClick(SeeAllVersionsClick);
            expander.OnLeftClick(ToggleExpansion);
            nameLabel.RegisterCallback<GeometryChangedEvent>(OnSizeChange);

            UpdateExpanderUI(false);

            SetPackage(package);
            UpdateVisualState();
        }

        private void OnSizeChange(GeometryChangedEvent evt)
        {
            if (evt.newRect.width == evt.oldRect.width)
                return;

            var target = evt.target as TextElement;
            if (target == null)
                return;

            var size = target.MeasureTextSize(target.text, float.MaxValue, MeasureMode.AtMost, evt.newRect.height, MeasureMode.Undefined);
            var width = evt.newRect.width - target.resolvedStyle.paddingRight;
            target.tooltip = width < size.x ? target.text : string.Empty;
        }

        public void UpdateVisualState(VisualState newVisualState = null)
        {
            var seeAllVersionsOld = visualState?.seeAllVersions ?? false;
            var selectedVersionIdOld = visualState?.selectedVersionId ?? string.Empty;

            visualState = newVisualState?.Clone() ?? visualState ?? new VisualState(package?.uniqueId);

            if (UIUtils.IsElementVisible(this) != visualState.visible)
                UIUtils.SetElementDisplay(this, visualState.visible);

            var expansionChanged = expander.expanded != visualState.expanded;
            if (expansionChanged)
                UpdateExpanderUI(visualState.expanded);

            if (expansionChanged || seeAllVersionsOld != visualState.seeAllVersions)
                RefreshVersions();

            if (selectedVersionIdOld != visualState.selectedVersionId)
                RefreshSelection();
        }

        internal void SetPackage(IPackage package)
        {
            var displayVersion = package?.primaryVersion;
            if (displayVersion == null)
                return;

            // changing the package assigned to an item is not supported
            if (this.package != null && this.package.uniqueId != package.uniqueId)
                return;

            var oldDisplayVersion = this.package?.primaryVersion;
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

            var expandable = package.versions.Skip(1).Any();
            UIUtils.SetElementDisplay(expander, expandable);
            UIUtils.SetElementDisplay(expanderHidden, !expandable);
            if (!expandable && expander.expanded)
                UpdateExpanderUI(false);

            var showVersionLabel = !displayVersion.HasTag(PackageTag.BuiltIn) && !string.IsNullOrEmpty(package.displayName);
            UIUtils.SetElementDisplay(versionList, showVersionLabel);
            UIUtils.SetElementDisplay(versionLabel, showVersionLabel);
            if (showVersionLabel)
                versionLabel.text = GetVersionText(displayVersion, true);

            UpdateStatusIcon();
            RefreshVersions();
            RefreshSelection();
        }

        private void UpdateStatusIcon()
        {
            var state = package?.GetState() ?? PackageState.UpToDate;

            var stateClass = state != PackageState.UpToDate ? state.ToString().ToLower() : null;
            if (!string.IsNullOrEmpty(m_CurrentStateClass))
                stateLabel.RemoveFromClassList(m_CurrentStateClass);
            if (!string.IsNullOrEmpty(stateClass))
                stateLabel.AddToClassList(stateClass);
            m_CurrentStateClass = stateClass;

            if (state != PackageState.InProgress)
                StopSpinner();
            else if (state == PackageState.InProgress)
                StartSpinner();
        }

        private void RefreshVersions()
        {
            if (!expander.expanded)
                return;

            versionList.Clear();

            var seeAllVersions = visualState?.seeAllVersions ?? false;

            var additionalKeyVersions = package.keyVersions.Where(p => p != package.primaryVersion).ToList();
            var additionalVersions = package.versions.Where(p => p != package.primaryVersion).ToList();

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
            PageManager.instance.SetSelected(package);
        }

        private void ToggleExpansion()
        {
            SetExpanded(!expander.expanded);
        }

        internal void SetExpanded(bool value)
        {
            // mark the package as expanded in the page manager,
            // the UI will be updated through the callback chain
            PageManager.instance.SetExpanded(package, value);
        }

        internal void UpdateExpanderUI(bool expanded)
        {
            expander.expanded = expanded;
            UIUtils.SetElementDisplay(versionsContainer, expanded);
        }

        private void SeeAllVersionsClick()
        {
            PageManager.instance.SetSeeAllVersions(package, true);
        }

        internal void StartSpinner()
        {
            spinner.Start();
            UIUtils.SetElementDisplay(stateLabel, false);
        }

        internal void StopSpinner()
        {
            spinner.Stop();
            UIUtils.SetElementDisplay(stateLabel, true);
        }

        public IEnumerable<ISelectableItem> GetSelectableItems()
        {
            yield return this;
            if (expander.expanded)
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
        private ArrowToggle expander { get { return cache.Get<ArrowToggle>("expander"); } }
        private Label expanderHidden { get { return cache.Get<Label>("expanderHidden"); } }
        private VisualElement versionsContainer { get { return cache.Get<VisualElement>("versionsContainer"); } }
        private ScrollView versionList { get { return cache.Get<ScrollView>("versionList"); } }

        public static string GetVersionText(IPackageVersion version, bool simplified = false)
        {
            if (version?.version == null || version?.version?.ToString() == "0.0.0")
                return version?.versionString;

            var label = version.version.StripTag();
            if (!simplified)
            {
                if (version.HasTag(PackageTag.Local))
                    label = "local - " + label;
                if (version.HasTag(PackageTag.Git))
                    label = "git - " + label;
                if (version.HasTag(PackageTag.Verified))
                    label = "verified - " + label;
                if (version.isInstalled)
                    label = "current - " + label;
            }
            if (version.HasTag(PackageTag.Preview))
            {
                var previewLabel = string.IsNullOrEmpty(version.version.Prerelease) ? "preview" : version.version.Prerelease;
                label = $"{previewLabel} - {label}";
            }
            return label;
        }
    }
}
