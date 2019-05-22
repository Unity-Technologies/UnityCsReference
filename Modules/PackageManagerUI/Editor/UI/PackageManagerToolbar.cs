// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageManagerToolbar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageManagerToolbar> {}
        private PackageCollection Collection;

        public event Action<PackageFilter> OnFilterChange = delegate {};
        public event Action OnTogglePreviewChange = delegate {};
        public static event Action OnToggleDependenciesChange = delegate {};

        [SerializeField]
        private PackageFilter selectedFilter;

        public event Action<string> OnSearchChange = delegate {};

        public PackageManagerToolbar()
        {
            var root = Resources.GetTemplate("PackageManagerToolbar.uxml");
            Add(root);
            root.StretchToParentSize();
            Cache = new VisualElementCache(root);

            SetupAddMenu();
            SetupFilterMenu();
            SetupAdvancedMenu();
            SetupSearchToolbar();
        }

        private void SetupSearchToolbar()
        {
            SearchToolbar.RegisterValueChangedCallback(OnSearchTextChanged);
        }

        private void OnSearchTextChanged(ChangeEvent<string> evt)
        {
            OnSearchChange(evt.newValue);
        }

        public void GrabFocus()
        {
            SearchToolbar.Focus();
        }

        public void OnPackagesChanged()
        {
            SetupFilterMenu();
        }

        private static string GetFilterDisplayName(PackageFilter filter)
        {
            switch (filter)
            {
                case PackageFilter.All:
                    return "All packages";
                case PackageFilter.Local:
                    return "In Project";
                case PackageFilter.Modules:
                    return "Built-in packages";
                case PackageFilter.InDevelopment:
                    return "In Development";
                default:
                    return filter.ToString();
            }
        }

        public void SetCollection(PackageCollection collection)
        {
            Collection = collection;
        }

        public void SetFilter(object obj)
        {
            var previouSelectedFilter = selectedFilter;
            selectedFilter = (PackageFilter)obj;
            FilterMenu.text = GetFilterDisplayName(selectedFilter);

            if (selectedFilter != previouSelectedFilter)
                OnFilterChange(selectedFilter);
        }

        private void SetupAddMenu()
        {
            AddMenu.menu.AppendAction("Add package from disk...", a =>
            {
                var path = EditorUtility.OpenFilePanelWithFilters("Select package on disk", "", new[] { "package.json file", "json" });
                if (!string.IsNullOrEmpty(path) && !Package.AddRemoveOperationInProgress)
                    Package.AddFromLocalDisk(path);
            }, a => DropdownMenuAction.Status.Normal);

            AddMenu.menu.AppendAction("Add package from git URL...", a =>
            {
                PackageAddFromUrlField.Show(parent);
            }, a => DropdownMenuAction.Status.Normal);
        }

        private void SetupFilterMenu()
        {
            FilterMenu.menu.MenuItems().Clear();

            FilterMenu.menu.AppendAction(GetFilterDisplayName(PackageFilter.All), a =>
            {
                SetFilter(PackageFilter.All);
            }, a => selectedFilter == PackageFilter.All ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            FilterMenu.menu.AppendAction(GetFilterDisplayName(PackageFilter.Local), a =>
            {
                SetFilter(PackageFilter.Local);
            }, a => selectedFilter == PackageFilter.Local ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            FilterMenu.menu.AppendSeparator();
            FilterMenu.menu.AppendAction(GetFilterDisplayName(PackageFilter.Modules), a =>
            {
                SetFilter(PackageFilter.Modules);
            }, a => selectedFilter == PackageFilter.Modules ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            if (Collection != null && Collection.AnyPackageInDevelopment)
            {
                FilterMenu.menu.AppendSeparator();
                FilterMenu.menu.AppendAction(GetFilterDisplayName(PackageFilter.InDevelopment), a =>
                {
                    SetFilter(PackageFilter.InDevelopment);
                }, a => selectedFilter == PackageFilter.InDevelopment ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
        }

        private void SetupAdvancedMenu()
        {
            AdvancedMenu.menu.AppendAction("Reset Packages to defaults", a =>
            {
                EditorApplication.ExecuteMenuItem(ApplicationUtil.ResetPackagesMenuPath);
            }, a => DropdownMenuAction.Status.Normal);

            AdvancedMenu.menu.AppendAction("Show dependencies", a =>
            {
                ToggleDependencies();
            }, a => PackageManagerPrefs.ShowPackageDependencies ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            AdvancedMenu.menu.AppendAction("Show preview packages", a =>
            {
                TogglePreviewPackages();
            }, a => PackageManagerPrefs.ShowPreviewPackages ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }

        private void ToggleDependencies()
        {
            PackageManagerPrefs.ShowPackageDependencies = !PackageManagerPrefs.ShowPackageDependencies;
            OnToggleDependenciesChange();
        }

        private void TogglePreviewPackages()
        {
            var showPreviewPackages = PackageManagerPrefs.ShowPreviewPackages;
            if (!showPreviewPackages && PackageManagerPrefs.ShowPreviewPackagesWarning)
            {
                const string message = "Preview packages are not verified with Unity, may be unstable, and are unsupported in production. Are you sure you want to show preview packages?";
                if (!EditorUtility.DisplayDialog("Unity Package Manager", message, "Yes", "No"))
                    return;
                PackageManagerPrefs.ShowPreviewPackagesWarning = false;
            }
            PackageManagerPrefs.ShowPreviewPackages = !showPreviewPackages;
            OnTogglePreviewChange();
        }

        private VisualElementCache Cache { get; set; }

        private ToolbarMenu AddMenu { get { return Cache.Get<ToolbarMenu>("toolbarAddMenu"); }}
        private ToolbarMenu FilterMenu { get { return Cache.Get<ToolbarMenu>("toolbarFilterMenu"); } }
        private ToolbarMenu AdvancedMenu { get { return Cache.Get<ToolbarMenu>("toolbarAdvancedMenu"); } }
        internal ToolbarSearchField SearchToolbar { get { return Cache.Get<ToolbarSearchField>("toolbarSearch"); } }
    }
}
