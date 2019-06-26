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

        public static readonly int k_ListItemSpacing = 16 + 4 + 4;
        public static readonly int k_ListItemMaxHeight = 150;

        private string m_CurrentStateClass;
        public IPackage package { get; private set; }
        internal IEnumerable<PackageVersionItem> versionItems { get { return versionList.Children().Cast<PackageVersionItem>(); } }
        private IEnumerable<IPackageVersion> additionalVersions { get { return package.versions.Where(p => p != package.primaryVersion); } }

        public string displayName
        {
            get { return nameLabel.text; }
            set { nameLabel.text = value; }
        }

        public PackageItem() : this(null)
        {
        }

        public PackageItem(IPackage package)
        {
            var root = Resources.GetTemplate("PackageItem.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            SetPackage(package);

            itemLabel.RegisterCallback<MouseDownEvent>(e => SelectMainItem());
            seeAllVersionsLabel.RegisterCallback<MouseDownEvent>(e => SeeAllVersionsClick());

            expander.RegisterCallback<MouseDownEvent>(e => SetExpand(!expander.expanded));

            UIUtils.SetElementDisplay(itemVersions, expander.expanded);
            StopSpinner();
        }

        public void SelectMainItem()
        {
            SelectionManager.instance.SetSelected(package);
        }

        private void SetExpandInternal(bool value)
        {
            expander.expanded = value;
            UIUtils.SetElementDisplay(itemVersions, expander.expanded);
            SelectionManager.instance.SetExpanded(package, value);
            if (!value)
                SelectionManager.instance.SetSeeAllVersions(package, false);
        }

        public void SetExpand(bool value)
        {
            SetExpandInternal(value);
            if (value)
                RefreshVersions();
        }

        internal void SetPackage(IPackage package)
        {
            var displayVersion = package?.primaryVersion;
            if (displayVersion == null)
                return;

            var oldDisplayVersion = this.package?.primaryVersion;
            this.package = package;

            if (oldDisplayVersion?.uniqueId != displayVersion.uniqueId && SelectionManager.instance.IsSelected(package, oldDisplayVersion))
                SelectionManager.instance.SetSelected(package, displayVersion);

            expander.expanded = SelectionManager.instance.IsExpanded(package);

            displayName = displayVersion.displayName;
            versionLabel.text = GetStandardizedLabel(displayVersion, true);

            string stateClass = null;
            if (package.installedVersion != null)
            {
                if (displayVersion.HasTag(PackageTag.InDevelopment))
                    stateClass = "development";
                else if (package.state == PackageState.Outdated && package.recommendedVersion != package.installedVersion)
                    stateClass = GetIconStateId(PackageState.Outdated);
                else
                    stateClass = "installed";
            }
            // Error state should be last as it should supersede other states
            if (package.errors.Any())
                stateClass = GetIconStateId(PackageState.Error);
            stateClass = stateClass ?? GetIconStateId(package.state);

            stateLabel.RemoveFromClassList(m_CurrentStateClass);
            stateLabel.AddToClassList(stateClass);

            var isBuiltIn = displayVersion.HasTag(PackageTag.BuiltIn);
            UIUtils.SetElementDisplay(expander, !isBuiltIn);
            UIUtils.SetElementDisplay(expanderHidden, isBuiltIn);
            UIUtils.SetElementDisplay(versionList, !isBuiltIn);
            UIUtils.SetElementDisplay(versionLabel, !isBuiltIn);

            m_CurrentStateClass = stateClass;
            if (package.state != PackageState.InProgress && spinner.started)
                StopSpinner();

            RefreshVersions();
            RefreshSelection();
        }

        private void RefreshVersions()
        {
            if (package == null)
                return;

            var additionalVersions = this.additionalVersions.ToList();

            if (package.keyVersions.Where(v => v != package.primaryVersion).Any(version => SelectionManager.instance.IsSelected(package, version)))
                SetExpandInternal(true);
            else if (additionalVersions.Any(version => SelectionManager.instance.IsSelected(package, version)))
            {
                SelectionManager.instance.SetSeeAllVersions(package, true);
                SetExpandInternal(true);
            }

            if (!expander.expanded)
                return;

            versionList.Clear();

            var seeAllVersionsClicked = SelectionManager.instance.IsSeeAllVersions(package);
            var showToolbar = !seeAllVersionsClicked;

            var versions = seeAllVersionsClicked ? additionalVersions : package.keyVersions;
            versions = versions.Where(p => p != package.primaryVersion).Reverse();

            foreach (var version in versions)
            {
                var versionLabel = new PackageVersionItem(package, version);
                versionList.Add(versionLabel);
            }

            var hasAnyVersion = additionalVersions.Any() || versions.Any();
            UIUtils.SetElementDisplay(noVersionsLabel, !hasAnyVersion);
            UIUtils.SetElementDisplay(seeAllVersionsLabel, hasAnyVersion);
            UIUtils.SetElementDisplay(versionToolbar, showToolbar);

            // TODO: Hard-code until scrollView can size to its content
            // Note: ListItemMaxHeight is used because there is an issue with VisualElement where at construction time,
            //          styling is not yet applied and max height returns 0 even though the stylesheet has it at 150.
            var maxHeight = Math.Max(versionList.style.maxHeight.value.value, k_ListItemMaxHeight);
            versionList.style.minHeight = Math.Min(versions.Count() * k_ListItemSpacing, maxHeight);

            // Hack until ScrollList has a better way to do the same -- Vertical scroll bar is not yet visible
            var maxNumberOfItemBeforeScrollbar = 7;
            versionList.EnableClass("hasScrollBar", versions.Count() > maxNumberOfItemBeforeScrollbar);
        }

        public void RefreshSelection()
        {
            itemLabel.EnableClass(UIUtils.k_SelectedClassName, SelectionManager.instance.IsSelected(package));
        }

        public IPackageVersion targetVersion { get { return package.primaryVersion; } }
        public VisualElement element { get { return this; } }

        private void SeeAllVersionsClick()
        {
            SelectionManager.instance.SetSeeAllVersions(package, true);
            RefreshVersions();
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
        private Label noVersionsLabel { get { return cache.Get<Label>("noVersions"); } }
        private VisualElement versionToolbar { get { return cache.Get<VisualElement>("versionsToolbar"); } }
        private VisualElement itemLabel { get { return cache.Get<VisualElement>("itemLabel"); } }
        private LoadingSpinner spinner { get { return cache.Get<LoadingSpinner>("packageSpinner"); } }
        private ArrowToggle expander { get { return cache.Get<ArrowToggle>("expander"); } }
        private Label expanderHidden { get { return cache.Get<Label>("expanderHidden"); } }
        private VisualElement itemVersions { get { return cache.Get<VisualElement>("itemVersions"); } }
        private ScrollView versionList { get { return cache.Get<ScrollView>("versionList"); } }

        public static string GetIconStateId(PackageState state)
        {
            return state.ToString().ToLower();
        }

        public static string GetStandardizedLabel(IPackageVersion version, bool simplified = false)
        {
            if (version == null)
                return string.Empty;

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
