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

    internal interface IPage
    {
        event Action<PageSelection> onSelectionChanged;
        // triggered when the state of the UI item is updated (expanded, hidden, see all versions toggled)
        event Action<IEnumerable<VisualState>> onVisualStateChange;
        // triggered when packages are added/updated or removed
        event Action<ListUpdateArgs> onListUpdate;
        event Action<IPage> onListRebuild;
        event Action<IPage> onSubPageChanged;
        event Action<PageFilters> onFiltersChange;
        PageFilters filters { get; }
        PackageFilterTab tab { get; }
        PageCapability capability { get; }

        long numTotalItems { get; }
        long numCurrentItems { get; }

        IEnumerable<SubPage> subPages { get; }
        SubPage currentSubPage { get; set; }
        void AddSubPage(SubPage subPage);

        // an ordered list of `packageUniqueIds`
        IEnumerable<VisualState> visualStates { get; }
        bool isFullyLoaded { get; }

        string contentType { get; }

        VisualState GetVisualState(string packageUniqueId);
        void LoadMore(long numberOfPackages);

        // return true if filters are changed
        bool ClearFilters();
        // return true if filters are changed
        bool UpdateFilters(PageFilters filters);

        PageSelection GetSelection();
        IEnumerable<VisualState> GetSelectedVisualStates();

        void OnPackagesChanged(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> preUpdate, IEnumerable<IPackage> postUpdate);

        void Rebuild();
        void Load(IPackage package, IPackageVersion version = null);

        bool SetNewSelection(IEnumerable<PackageAndVersionIdPair> packageAndVersionIdPairs);
        bool AmendSelection(IEnumerable<PackageAndVersionIdPair> toAddOrUpdate, IEnumerable<PackageAndVersionIdPair> toRemove);
        bool ToggleSelection(string packageUniqueId);
        void TriggerOnSelectionChanged();

        void SetExpanded(string packageUniqueId, bool value);
        void SetExpanded(IPackage package, bool value);
        void SetSeeAllVersions(string packageUniqueId, bool value);
        void SetSeeAllVersions(IPackage package, bool value);
        bool IsGroupExpanded(string groupName);
        void SetGroupExpanded(string groupName, bool value);
        string GetGroupName(IPackage package);

        bool Contains(IPackage package);
        bool Contains(string packageUniqueId);
        void SetPackagesUserUnlockedState(IEnumerable<string> packageUniqueIds, bool unlocked);
        void ResetUserUnlockedState();
        bool GetDefaultLockState(IPackage package);
    }
}
