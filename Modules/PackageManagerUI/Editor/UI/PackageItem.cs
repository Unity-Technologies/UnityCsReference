// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageItem : VisualElement, IPackageSelection
    {
        internal new class UxmlFactory : UxmlFactory<PackageItem> {}

        public static int ListItemSpacing = 16 + 4 + 4;
        public static int ListItemMaxHeight = 150;

        private Selection Selection;
        private readonly VisualElement root;
        private string currentStateClass;
        public Package package { get; private set; }
        private IEnumerable<PackageVersionLabel> Versions { get { return VersionList.Children().Cast<PackageVersionLabel>(); } }
        private ElementSelection SelectionManager { get; set; }
        private IEnumerable<PackageInfo> AdditionalVersions { get { return package.Versions.Where(p => p != package.VersionToDisplay); } }

        private bool IsExpanded
        {
            get { return Selection.IsExpanded(package.VersionToDisplay); }
        }

        public PackageItem() : this(null, null)
        {
        }

        public PackageItem(Package package, Selection selection)
        {
            root = Resources.GetTemplate("PackageItem.uxml");
            Add(root);
            Cache = new VisualElementCache(root);

            Selection = selection;
            SetItem(package);

            SelectionManager = new ElementSelection(this, Selection);

            ItemLabel.RegisterCallback<MouseDownEvent>(e => SelectMainItem());
            SeeAllVersions.RegisterCallback<MouseDownEvent>(e => SeeAllVersionsClick());
            VersionList.contentContainer.AddToClassList("fix-scroll-view");

            Expander.RegisterCallback<MouseDownEvent>(e => ExpandToggle());

            UIUtils.SetElementDisplay(ItemVersions, Expander.Expanded);
            StopSpinner();
        }

        public void SelectMainItem()
        {
            Selection.SetSelection(package.VersionToDisplay);
        }

        private void SetExpandInternal(bool value)
        {
            Expander.Expanded = value;
            UIUtils.SetElementDisplay(ItemVersions, Expander.Expanded);
            Selection.SetExpanded(package.VersionToDisplay, value);
        }

        public void SetExpand(bool value)
        {
            SetExpandInternal(value);
            if (Expander.Expanded)
            {
                SetSeeAllVersions(false);
                RefreshVersions();
            }
        }

        private void ExpandToggle()
        {
            SetExpand(!Expander.Expanded);
        }

        private void SetSelected(bool value)
        {
            if (value)
                ItemLabel.AddToClassList(ApplicationUtil.SelectedClassName);
            else
                ItemLabel.RemoveFromClassList(ApplicationUtil.SelectedClassName);

            Spinner.InvertColor = value;
        }

        private void SetItem(Package package)
        {
            var displayPackage = package != null ? package.VersionToDisplay : null;
            if (displayPackage == null)
                return;

            this.package = package;
            OnPackageChanged();
            this.package.AddSignal.WhenOperation(OnPackageAdd);
            this.package.RemoveSignal.WhenOperation(OnPackageRemove);
        }

        private void OnPackageRemove(IRemoveOperation operation)
        {
            operation.OnOperationError += error => StopSpinner();
            OnPackageUpdate();
        }

        private void OnPackageAdd(IAddOperation operation)
        {
            operation.OnOperationError += error => StopSpinner();
            OnPackageUpdate();
        }

        internal void SetDisplayName(string displayName)
        {
            NameLabel.text = displayName;
        }

        private void OnPackageChanged()
        {
            var displayPackage = package != null ? package.VersionToDisplay : null;
            if (displayPackage == null)
                return;

            Expander.Expanded = Selection.IsExpanded(package.VersionToDisplay);

            SetDisplayName(displayPackage.DisplayName);
            VersionLabel.text = displayPackage.StandardizedLabel(false);

            var stateClass = GetIconStateId(displayPackage);
            if (displayPackage.State == PackageState.Outdated && package.LatestUpdate == package.Current)
                stateClass = GetIconStateId(PackageState.UpToDate);
            if (package.Error != null)
                stateClass = GetIconStateId(PackageState.Error);
            if (stateClass ==  GetIconStateId(PackageState.UpToDate) && package.Current != null)
                stateClass = "installed";

            StateLabel.RemoveFromClassList(currentStateClass);
            StateLabel.AddToClassList(stateClass);

            UIUtils.SetElementDisplay(Expander, !displayPackage.IsBuiltIn);
            UIUtils.SetElementDisplay(ExpanderHidden, displayPackage.IsBuiltIn);
            UIUtils.SetElementDisplay(VersionList, !displayPackage.IsBuiltIn);
            UIUtils.SetElementDisplay(VersionLabel, !displayPackage.IsBuiltIn);

            RefreshVersions();

            currentStateClass = stateClass;
            if (displayPackage.State != PackageState.InProgress && Spinner.Started)
                StopSpinner();
        }

        private void RefreshVersions()
        {
            if (package == null)
                return;

            var additionalVersions = AdditionalVersions.ToList();

            if (package.KeyVersions.Where(v => v != package.VersionToDisplay).Any(version => Selection.IsSelected(version)))
                SetExpandInternal(true);
            else if (AdditionalVersions.Any(version => Selection.IsSelected(version)))
            {
                SetSeeAllVersions(true);
                SetExpandInternal(true);
            }

            if (!Expander.Expanded)
                return;

            VersionList.Clear();

            var seeAllVersionsClicked = Selection.IsSeeAllVersions(package.VersionToDisplay);
            var showToolbar = !seeAllVersionsClicked;

            var versions = seeAllVersionsClicked ? additionalVersions : package.KeyVersions;
            versions = versions.Where(p => p != package.VersionToDisplay).Reverse();

            foreach (var version in versions)
            {
                var versionLabel = new PackageVersionLabel(version, Selection);
                VersionList.Add(versionLabel);
            }

            var hasAnyVersion = additionalVersions.Any() || versions.Any();
            UIUtils.SetElementDisplay(NoVersions, !hasAnyVersion);
            UIUtils.SetElementDisplay(SeeAllVersions, hasAnyVersion);
            UIUtils.SetElementDisplay(VersionToolbar, showToolbar);

            // TODO: Hard-code until scrollView can size to its content
            // Note: ListItemMaxHeight is used because there is an issue with VisualElement where at construction time,
            //          styling is not yet applied and max height returns 0 even though the stylesheet has it at 150.
            var maxHeight = Math.Max(VersionList.style.maxHeight.value.value, ListItemMaxHeight);
            VersionList.style.minHeight = Math.Min(versions.Count() * ListItemSpacing, maxHeight);

            // Hack until ScrollList has a better way to do the same -- Vertical scroll bar is not yet visible
            var maxNumberOfItemBeforeScrollbar = 7;
            VersionList.EnableClass("hasScrollBar", versions.Count() > maxNumberOfItemBeforeScrollbar);
        }

        private bool IsKeyVersion(PackageInfo packageInfo)
        {
            return package.KeyVersions.Any(p => p == packageInfo);
        }

        public void RefreshSelection()
        {
            SetSelected(Selection.IsSelected(package.VersionToDisplay));
        }

        public PackageInfo TargetVersion { get { return package.VersionToDisplay; } }
        public VisualElement Element { get { return this; } }

        private void SeeAllVersionsClick()
        {
            SetSeeAllVersions(true);
            RefreshVersions();
        }

        private void SetSeeAllVersions(bool value)
        {
            Selection.SetSeeAllVersions(package.VersionToDisplay, value);
        }

        private void OnPackageUpdate()
        {
            StartSpinner();
        }

        private void StartSpinner()
        {
            UIUtils.SetElementDisplay(SpinnerContainer, true);
            Spinner.Start();
            StateLabel.AddToClassList("no-icon");
            UIUtils.SetElementDisplay(StateLabel, false);
        }

        private void StopSpinner()
        {
            Spinner.Stop();
            StateLabel.RemoveFromClassList("no-icon");
            UIUtils.SetElementDisplay(StateLabel, true);
            UIUtils.SetElementDisplay(SpinnerContainer, false);
        }

        public IEnumerable<IPackageSelection> GetSelectionList()
        {
            yield return this;
            if (Expander.Expanded)
                foreach (var version in Versions)
                    yield return version;
        }

        private VisualElementCache Cache { get; set; }

        public Label NameLabel { get { return Cache.Get<Label>("packageName"); } }
        private Label StateLabel { get { return Cache.Get<Label>("packageState"); } }
        private Label VersionLabel { get { return Cache.Get<Label>("packageVersion"); } }
        private Label SeeAllVersions { get { return Cache.Get<Label>("seeAllVersions"); } }
        private Label NoVersions { get { return Cache.Get<Label>("noVersions"); } }
        private VisualElement VersionToolbar { get { return Cache.Get<VisualElement>("versionsToolbar"); } }
        private VisualElement PackageContainer { get { return Cache.Get<VisualElement>("packageContainer"); } }
        private VisualElement ItemLabel { get { return Cache.Get<VisualElement>("itemLabel"); } }
        private LoadingSpinner Spinner { get { return Cache.Get<LoadingSpinner>("packageSpinner"); } }
        private VisualElement SpinnerContainer { get { return Cache.Get<VisualElement>("loadingSpinnerContainer"); } }
        private ArrowToggle Expander { get { return Cache.Get<ArrowToggle>("expander"); } }
        private Label ExpanderHidden { get { return Cache.Get<Label>("expanderHidden"); } }
        private VisualElement ItemVersions { get { return Cache.Get<VisualElement>("itemVersions"); } }
        private ScrollView VersionList { get { return Cache.Get<ScrollView>("versionList"); } }

        public static string GetIconStateId(PackageInfo packageInfo)
        {
            if (packageInfo == null)
                return "";

            return GetIconStateId(packageInfo.State);
        }

        public static string GetIconStateId(PackageState state)
        {
            return state.ToString().ToLower();
        }
    }
}
