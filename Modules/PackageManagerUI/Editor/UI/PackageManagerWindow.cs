// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageManagerWindow : EditorWindow
    {
        internal PackageCollection Collection;
        private PackageSearchFilter SearchFilter;
        internal SelectionManager SelectionManager;

        private VisualElement root;

        [NonSerialized]
        private string PackageToSelectAfterLoad;

        internal static bool SkipFetchCacheForAllWindows;

        [SerializeField]
        private PackageFilter LastUsedPackageFilter;

        public void OnEnable()
        {
            var styleSheet = Resources.GetStyleSheet();
            rootVisualElement.styleSheets.Add(styleSheet);

            var collectionWasNull = Collection == null;
            if (Collection == null)
                Collection = new PackageCollection();

            if (SearchFilter == null)
                SearchFilter = new PackageSearchFilter();

            if (SelectionManager == null)
                SelectionManager = new SelectionManager();

            var windowResource = Resources.GetVisualTreeAsset("PackageManagerWindow.uxml");
            if (windowResource != null)
            {
                root = windowResource.CloneTree();
                rootVisualElement.Add(root);
                root.StretchToParentSize();

                Cache = new VisualElementCache(root);

                SelectionManager.SetCollection(Collection);
                Collection.OnFilterChanged += filter => SetupSelection();
                if (collectionWasNull)
                {
                    LastUsedPackageFilter = PackageManagerPrefs.LastUsedPackageFilter;
                }

                Collection.SetFilter(LastUsedPackageFilter);
                Collection.UpdatePackageCollection(true);

                SetupPackageDetails();
                SetupPackageList();
                SetupSearchToolbar();
                SetupToolbar();
                SetupStatusbar();
                SetupCollection();
                SetupSelection();

                // Disable filter while fetching first results
                if (!Collection.LatestListPackages.Any())
                    PackageManagerToolbar.SetEnabled(false);

                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    Collection.FetchListOfflineCache(true);
                    Collection.FetchListCache(collectionWasNull);
                    Collection.FetchSearchCache(collectionWasNull);
                }
                Collection.TriggerPackagesChanged();
            }
        }

        public void OnDisable()
        {
            PackageManagerPrefs.LastUsedPackageFilter = LastUsedPackageFilter;
        }

        private void OnDestroy()
        {
            foreach (var extension in PackageManagerExtensions.ToolbarExtensions)
                extension.OnWindowDestroy();
        }

        private void SetupCollection()
        {
            Collection.OnPackagesChanged += (filter, packages) =>
            {
                PackageList.SetPackages(filter, packages);
                SelectionManager.Selection.TriggerNewSelection();
                PackageManagerToolbar.OnPackagesChanged();
            };
            Collection.OnUpdateTimeChange += PackageStatusbar.SetUpdateTimeMessage;
            Collection.ListSignal.WhenOperation(PackageStatusbar.OnListOrSearchOperation);
            Collection.SearchSignal.WhenOperation(PackageStatusbar.OnListOrSearchOperation);
        }

        private void SetupStatusbar()
        {
            PackageStatusbar.OnCheckInternetReachability += OnCheckInternetReachability;
            PackageStatusbar.Setup(Collection);
        }

        private void SetupToolbar()
        {
            PackageManagerToolbar.OnFilterChange += OnFilterChange;
            PackageManagerToolbar.OnTogglePreviewChange += OnTogglePreviewChange;
            PackageManagerToolbar.SetFilter(Collection.Filter);
            PackageManagerToolbar.SetCollection(Collection);
        }

        private void SetupSearchToolbar()
        {
            PackageManagerToolbar.OnSearchChange += OnSearchChange;
            PackageManagerToolbar.SearchToolbar.SetValueWithoutNotify(SearchFilter.SearchText);
        }

        private void SetupPackageList()
        {
            PackageList.OnLoaded += OnPackagesLoaded;
            PackageList.OnFocusChange += OnListFocusChange;
            PackageList.SetSearchFilter(SearchFilter);
            Collection.OnLatestPackageInfoFetched += PackageList.OnLatestPackageInfoFetched;
        }

        private void SetupPackageDetails()
        {
            PackageDetails.OnCloseError += OnCloseError;
            PackageDetails.OnOperationError += OnOperationError;
            PackageDetails.SetCollection(Collection);
            Collection.OnLatestPackageInfoFetched += PackageDetails.OnLatestPackageInfoFetched;
        }

        private void SetupSelection()
        {
            PackageList.SetSelection(SelectionManager.Selection);
            PackageDetails.SetSelection(SelectionManager.Selection);
        }

        private void OnCloseError(Package package)
        {
            Collection.RemovePackageErrors(package);
            Collection.UpdatePackageCollection();
        }

        private void OnOperationError(Package package, Error error)
        {
            Collection.AddPackageError(package, error);
            Collection.UpdatePackageCollection();
        }

        private void OnTogglePreviewChange()
        {
            Collection.UpdatePackageCollection(true);
        }

        private void OnFilterChange(PackageFilter filter)
        {
            LastUsedPackageFilter = filter;
            Collection.SetFilter(filter);
        }

        private void OnCheckInternetReachability()
        {
            Collection.FetchSearchCache(true);
            Collection.FetchListCache(true);
        }

        private void OnListFocusChange()
        {
            PackageManagerToolbar.GrabFocus();
        }

        private void OnSearchChange(string searchText)
        {
            SearchFilter.SearchText = searchText;
            PackageList.SetSearchFilter(SearchFilter);
            PackageFiltering.FilterPackageList(PackageList);
        }

        private void OnPackagesLoaded()
        {
            PackageManagerToolbar.SetEnabled(true);
            SelectionManager.SetSelection(PackageToSelectAfterLoad);
            PackageManagerToolbar.SetFilter(Collection.Filter);
            PackageToSelectAfterLoad = null;
        }

        private VisualElementCache Cache { get; set; }

        private PackageList PackageList { get { return Cache.Get<PackageList>("packageList"); } }
        private PackageDetails PackageDetails { get { return Cache.Get<PackageDetails>("packageDetails"); } }
        private PackageManagerToolbar PackageManagerToolbar { get {return Cache.Get<PackageManagerToolbar>("topMenuToolbar");} }
        private PackageStatusBar PackageStatusbar { get {return Cache.Get<PackageStatusBar>("packageStatusBar");} }

        internal static void FetchListOfflineCacheForAllWindows()
        {
            if (SkipFetchCacheForAllWindows)
                return;

            var windows = UnityEngine.Resources.FindObjectsOfTypeAll<PackageManagerWindow>();
            if (windows == null || windows.Length <= 0)
                return;

            foreach (var window in windows)
            {
                if (window.Collection != null)
                    window.Collection.FetchListOfflineCache(true);
            }
        }

        [MenuItem("Window/Package Manager", priority = 1500)]
        internal static void ShowPackageManagerWindow(MenuCommand item)
        {
            SkipFetchCacheForAllWindows = false;
            var window = GetWindow<PackageManagerWindow>(false, "Packages", true);
            window.minSize = new Vector2(700, 250);
            if (item.context != null)
            {
                if (window.Collection != null && window.Collection.LatestListPackages.Any())
                {
                    window.SelectionManager.SetSelection(item.context.name);
                    window.PackageList.EnsureSelectionIsVisible();
                }
                else
                    window.PackageToSelectAfterLoad = item.context.name;
            }
            window.Show();
        }
    }
}
