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
        event Action<PageFilters> onFiltersChange;
        event Action<string> onTrimmedSearchTextChanged;
        event Action<IPage> onSupportedStatusFiltersChanged;

        string id { get; }
        string displayName { get; }
        Icon icon { get; }

        string searchText { get; set; }
        string trimmedSearchText { get; }
        PageFilters filters { get; }
        PageCapability capability { get; }

        IEnumerable<PageFilters.Status> supportedStatusFilters { get; }
        IEnumerable<PageSortOption> supportedSortOptions { get; }

        RefreshOptions refreshOptions { get; }

        RegistryInfo scopedRegistry { get; }

        bool ShouldInclude(IPackage package);

        bool isActivePage { get; }
        // Called when a page got activated (when it became the current visible page)
        void OnActivated();
        // Called when a page got deactivated (when it went from the current page to the previous page)
        void OnDeactivated();

        // an ordered list of `packageUniqueIds`
        IVisualStateList visualStates { get; }

        // return true if filters are changed
        bool ClearFilters(bool resetSortOptionToDefault = false);
        // return true if filters are changed
        bool UpdateFilters(PageFilters newFilters);
        PageSelection GetSelection();

        void OnPackagesChanged(PackagesChangeArgs args);

        void OnEnable();
        void OnDisable();

        void LoadMore(long numberOfPackages);
        void Load(string packageUniqueId);
        void LoadExtraItems(IEnumerable<IPackage> packages);

        bool SetNewSelection(IPackage package, bool isExplicitUserSelection = false);
        bool SetNewSelection(IEnumerable<string> packageUniqueIds, bool isExplicitUserSelection = false);
        void RemoveSelection(IEnumerable<string> packageUniqueIds, bool isExplicitUserSelection = false);
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
