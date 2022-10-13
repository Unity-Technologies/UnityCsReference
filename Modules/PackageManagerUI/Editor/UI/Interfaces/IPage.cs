// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal struct ListUpdateArgs
    {
        public IPage page;
        public IEnumerable<IPackage> added;
        public IEnumerable<IPackage> updated;
        public IEnumerable<IPackage> removed;
        public bool reorder;
    }

    internal struct VisualStateChangeArgs
    {
        public IPage page;
        public IEnumerable<VisualState> visualStates;
    }

    internal struct PageSelectionChangeArgs
    {
        public IPage page;
        public PageSelection selection;
        public bool isExplicitUserSelection;
    }

    internal interface IPage
    {
        event Action<PageSelectionChangeArgs> onSelectionChanged;
        // triggered when the state of the UI item is updated (expanded, hidden, see all versions toggled)
        event Action<VisualStateChangeArgs> onVisualStateChange;
        // triggered when packages are added/updated or removed
        event Action<ListUpdateArgs> onListUpdate;
        event Action<IPage> onListRebuild;
        event Action<IPage> onSubPageChanged;
        event Action<PageFilters> onFiltersChange;
        PageFilters filters { get; }
        PackageFilterTab tab { get; }
        PageCapability capability { get; }

        IEnumerable<SubPage> subPages { get; }
        SubPage currentSubPage { get; set; }
        void AddSubPage(SubPage subPage);

        bool isActivePage { get; }
        // Called when a page got activated (when it became the current visible page)
        void OnActivated();
        // Called when a page got deactivated (when it went from the current page to the previous page)
        void OnDeactivated();

        // an ordered list of `packageUniqueIds`
        IVisualStateList visualStates { get; }

        string contentType { get; }

        void LoadMore(long numberOfPackages);

        // return true if filters are changed
        bool ClearFilters();
        // return true if filters are changed
        bool UpdateFilters(PageFilters filters);
        void UpdateSearchText(string searchText);
        PageSelection GetSelection();
        IEnumerable<VisualState> GetSelectedVisualStates();

        void OnPackagesChanged(PackagesChangeArgs args);

        void OnEnable();
        void OnDisable();

        void Load(string packageUniqueId);
        void LoadExtraItems(IEnumerable<IPackage> packages);

        bool SetNewSelection(IPackage package, IPackageVersion version = null, bool isExplicitUserSelection = false);
        bool SetNewSelection(PackageAndVersionIdPair packageAndVersionIdPair, bool isExplicitUserSelection = false);
        bool SetNewSelection(IEnumerable<PackageAndVersionIdPair> packageAndVersionIdPairs, bool isExplicitUserSelection = false);
        void RemoveSelection(IEnumerable<PackageAndVersionIdPair> toRemove, bool isExplicitUserSelection = false);
        bool AmendSelection(IEnumerable<PackageAndVersionIdPair> toAddOrUpdate, IEnumerable<PackageAndVersionIdPair> toRemove, bool isExplicitUserSelection = false);
        bool ToggleSelection(string packageUniqueId, bool isExplicitUserSelection = false);
        bool UpdateSelectionIfCurrentSelectionIsInvalid();
        void TriggerOnSelectionChanged(bool isExplicitUserSelection = false);
        bool IsGroupExpanded(string groupName);
        void SetGroupExpanded(string groupName, bool value);
        string GetGroupName(IPackage package);
        void SetPackagesUserUnlockedState(IEnumerable<string> packageUniqueIds, bool unlocked);
        void ResetUserUnlockedState();
        bool GetDefaultLockState(IPackage package);
    }
}
