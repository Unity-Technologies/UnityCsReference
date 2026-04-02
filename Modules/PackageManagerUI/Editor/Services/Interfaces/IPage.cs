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
        public IReadOnlyCollection<string> added;
        public IReadOnlyCollection<string> updated;
        public IReadOnlyCollection<string> removed;
    }

    internal struct VisualStateChangeArgs
    {
        public IPage page;
        public IReadOnlyCollection<VisualState> changed;
    }

    internal struct PageSelectionChangeArgs
    {
        public IPage page;
        public PageSelection selection;
        public bool isDirectMouseSelection;
    }

    internal struct PageStateChangeArgs
    {
        public IPage page;
        public bool visible;
        public Icon icon;
    }

    internal struct PageFiltersChangeArgs
    {
        public IPage page;
        public IPageFilters previousFilters;
        public PageFilters.ChangedTypes filterTypesChanged;
    }

    internal interface IPage
    {
        event Action<PageSelectionChangeArgs> onSelectionChanged;
        // triggered when the state of the UI item is updated (expanded, hidden, see all versions toggled)
        event Action<VisualStateChangeArgs> onVisualStateChange;
        // triggered when items are added/updated or removed
        event Action onListRebuild;
        event Action<ListUpdateArgs> onListUpdate;
        event Action<PageStateChangeArgs> onStageChanged;
        event Action<PageFiltersChangeArgs> onFiltersChanged;
        event Action onTrimmedSearchTextChanged;

        string id { get; }
        string displayName { get; }
        Icon icon { get; }

        bool visible { get; }

        string searchText { get; set; }
        string trimmedSearchText { get; }

        IPageFilters filters { get; }

        void UpdateSupportedFiltersAsync();

        PageCapability capability { get; }

        RefreshOptions refreshOptions { get; }

        RegistryInfo scopedRegistry { get; }

        bool isActive { get; }
        // Call to activate a page (when it became the current visible page)
        void Activate();
        // Call to deactivate a page (when it went from the current page to the previous page)
        void Deactivate();

        // an ordered list of `itemUniqueIds`
        IVisualStateList visualStates { get; }

        // return true if filters are changed
        bool ClearFilters(bool resetSortOptionToDefault = false);
        // return true if filters are changed
        bool UpdateFilters(IPageFilters newFilters);
        // return true if sort options are changed
        bool UpdateSortOption(PageSortOption newSortOption);
        PageSelection GetSelection();

        void OnEnable();
        void OnDisable();

        void LoadMore(long numberOfItems);
        void Load(string itemUniqueId);
        void LoadExtraItems(IEnumerable<string> itemUniqueIds);

        bool SetNewSelection(string itemUniqueId, bool isDirectMouseSelection);
        bool SetNewSelection(IEnumerable<string> itemUniqueIds, bool isDirectMouseSelection);
        void RemoveSelection(IEnumerable<string> itemUniqueIds, bool isDirectMouseSelection);
        bool ToggleSelection(string itemUniqueId, bool isDirectMouseSelection);
        bool UpdateSelectionIfCurrentSelectionIsInvalid();
        void TriggerOnSelectionChanged(bool isDirectMouseSelection);
        bool IsGroupExpanded(string groupName);
        void SetGroupExpanded(string groupName, bool value);
        void SetUserUnlockedState(IEnumerable<string> itemUniqueIds, bool unlocked);

        void ResetStatesOnDeactivate();
    }

    internal interface IPage<in T> : IPage
    {
        bool ShouldInclude(T item);
    }
}
