// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageManagerToolbar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageManagerToolbar> {}
        private readonly VisualElement root;

        public event Action<PackageFilter> OnFilterChange = delegate {};
        public event Action OnTogglePreviewChange = delegate {};
        public static event Action OnToggleDependenciesChange = delegate {};

        [SerializeField]
        private PackageFilter selectedFilter;

        public PackageManagerToolbar()
        {
            root = Resources.GetTemplate("PackageManagerToolbar.uxml");
            Add(root);
            root.StretchToParentSize();
            Cache = new VisualElementCache(root);

            AddButton.RegisterCallback<MouseDownEvent>(OnAddButtonMouseDown);
            FilterButton.RegisterCallback<MouseDownEvent>(OnFilterButtonMouseDown);
            AdvancedButton.RegisterCallback<MouseDownEvent>(OnAdvancedButtonMouseDown);
        }

        public void GrabFocus()
        {
            SearchToolbar.GrabFocus();
        }

        public new void SetEnabled(bool enable)
        {
            base.SetEnabled(enable);
            FilterButton.SetEnabled(enable);
            AdvancedButton.SetEnabled(enable);
            SearchToolbar.SetEnabled(enable);
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
                default:
                    return filter.ToString();
            }
        }

        public void SetFilter(object obj)
        {
            var previouSelectedFilter = selectedFilter;
            selectedFilter = (PackageFilter)obj;
            FilterButton.text = string.Format("{0} ▾", GetFilterDisplayName(selectedFilter));

            if (selectedFilter != previouSelectedFilter)
                OnFilterChange(selectedFilter);
        }

        private void OnAddButtonMouseDown(MouseDownEvent evt)
        {
            if (evt.propagationPhase != PropagationPhase.AtTarget)
                return;

            var menu = new GenericMenu();
            var addPackageFromDiskItem = new GUIContent("Add package from disk...");
            menu.AddItem(addPackageFromDiskItem, false, delegate
            {
                var path = EditorUtility.OpenFilePanelWithFilters("Select package on disk", "", new[] { "package.json file", "json" });
                if (!string.IsNullOrEmpty(path) && !Package.AddRemoveOperationInProgress)
                    Package.AddFromLocalDisk(path);
            });

            if (Unsupported.IsDeveloperMode())
            {
                var addPackageFromIdItem = new GUIContent("Add package from package ID...");
                menu.AddItem(addPackageFromIdItem, false, delegate { AddFromIdField.Show(true); });
            }

            var menuPosition = new Vector2(AddButton.layout.xMin, AddButton.layout.center.y);
            menuPosition = this.LocalToWorld(menuPosition);
            var menuRect = new Rect(menuPosition, Vector2.zero);
            menu.DropDown(menuRect);
        }

        private void OnFilterButtonMouseDown(MouseDownEvent evt)
        {
            if (evt.propagationPhase != PropagationPhase.AtTarget)
                return;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(GetFilterDisplayName(PackageFilter.All)),
                selectedFilter == PackageFilter.All,
                SetFilter, PackageFilter.All);
            menu.AddItem(new GUIContent(GetFilterDisplayName(PackageFilter.Local)),
                selectedFilter == PackageFilter.Local,
                SetFilter, PackageFilter.Local);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent(GetFilterDisplayName(PackageFilter.Modules)),
                selectedFilter == PackageFilter.Modules,
                SetFilter, PackageFilter.Modules);
            var menuPosition = new Vector2(FilterButton.layout.xMin, FilterButton.layout.center.y);
            menuPosition = this.LocalToWorld(menuPosition);
            var menuRect = new Rect(menuPosition, Vector2.zero);
            menu.DropDown(menuRect);
        }

        private void OnAdvancedButtonMouseDown(MouseDownEvent evt)
        {
            if (evt.propagationPhase != PropagationPhase.AtTarget)
                return;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Reset Packages to defaults"), false, () => EditorApplication.ExecuteMenuItem(ApplicationUtil.ResetPackagesMenuPath));
            menu.AddItem(new GUIContent("Show dependencies"), PackageManagerPrefs.ShowPackageDependencies, ToggleDependencies);
            menu.AddItem(new GUIContent("Show preview packages"), PackageManagerPrefs.ShowPreviewPackages, TogglePreviewPackages);

            var menuPosition = new Vector2(AdvancedButton.layout.xMax + 30, AdvancedButton.layout.center.y);
            menuPosition = this.LocalToWorld(menuPosition);
            var menuRect = new Rect(menuPosition, Vector2.zero);
            menu.DropDown(menuRect);
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

        private Label AddButton { get { return Cache.Get<Label>("toolbarAddButton"); }}
        private Label FilterButton { get { return Cache.Get<Label>("toolbarFilterButton"); } }
        private Label AdvancedButton { get { return Cache.Get<Label>("toolbarAdvancedButton"); } }
        internal PackageSearchToolbar SearchToolbar { get { return Cache.Get<PackageSearchToolbar>("toolbarSearch"); } }

        private PackageAddFromIdField _addFromIdField;
        private PackageAddFromIdField AddFromIdField { get { return _addFromIdField ?? (_addFromIdField = parent.Q<PackageAddFromIdField>("packageAddFromIdField")); } }
    }
}
