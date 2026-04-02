// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal abstract class BasePage : IPage
    {
        public event Action<PageSelectionChangeArgs> onSelectionChanged = delegate {};
        public event Action<VisualStateChangeArgs> onVisualStateChange = delegate {};
        public event Action<ListUpdateArgs> onListUpdate = delegate {};
        public event Action<PageStateChangeArgs> onStageChanged = delegate {};
        public event Action onListRebuild = delegate {};
        public event Action<PageFiltersChangeArgs> onFiltersChanged = delegate {};
        public event Action onTrimmedSearchTextChanged = delegate {};

        [SerializeField]
        private PageSelection m_Selection = new();

        [SerializeField]
        private List<string> m_CollapsedGroups = new();

        [SerializeField]
        private string m_SearchText = string.Empty;

        public virtual bool visible => true;

        public string searchText
        {
            get => m_SearchText;
            set
            {
                value ??= string.Empty;
                if (m_SearchText == value)
                    return;

                m_SearchText = value;
                var newTrimmedSearchText = Regex.Replace(m_SearchText.Trim(' ', '\t'), @"[ ]{2,}", " ");
                if (newTrimmedSearchText == m_TrimmedSearchText)
                    return;

                m_TrimmedSearchText = newTrimmedSearchText;
                RefreshListOnSearchTextChange();
                onTrimmedSearchTextChanged?.Invoke();
            }
        }

        [SerializeField]
        private string m_TrimmedSearchText = string.Empty;
        public string trimmedSearchText => m_TrimmedSearchText;

        [SerializeField]
        private PageFilters m_Filters = new ();
        public IPageFilters filters => m_Filters;

        // Most of the time we know all the supported filters from the page content and the supported filters are always
        // in sync with the page content, so by default we do nothing for this async function
        public virtual void UpdateSupportedFiltersAsync() {}

        public virtual PageCapability capability => PageCapability.None;

        [SerializeField]
        private bool m_IsActive;
        public bool isActive => m_IsActive;

        public abstract string id { get; }
        public abstract string displayName { get; }
        public abstract Icon icon { get; }
        public abstract RefreshOptions refreshOptions { get; }
        public abstract IVisualStateList visualStates { get; }

        public virtual RegistryInfo scopedRegistry => null;

        public abstract void OnEnable();
        public abstract void OnDisable();

        public bool ClearFilters(bool resetSortOptionToDefault = false)
        {
            var newFilters = new PageFilters(filters);
            var changedTypes = newFilters.Clear();
            if (resetSortOptionToDefault)
                changedTypes |= newFilters.ResetSortOptionToDefault();
            if (changedTypes == PageFilters.ChangedTypes.None)
                return false;
            UpdateFiltersInternal(newFilters, changedTypes);
            return true;
        }

        private static PageFilters.ChangedTypes FindChangedTypes(IPageFilters first, IPageFilters second)
        {
            if (ReferenceEquals(first, second) || first == null || second == null)
                return PageFilters.ChangedTypes.None;
            var result = PageFilters.ChangedTypes.None;
            if (first.status != second.status)
                result |= PageFilters.ChangedTypes.Status;
            if (first.sortOption != second.sortOption)
                result |= PageFilters.ChangedTypes.SortOption;
            if (!first.categories.IsSequenceEqual(second.categories))
                result |= PageFilters.ChangedTypes.Categories;
            if (!first.labels.IsSequenceEqual(second.labels))
                result |= PageFilters.ChangedTypes.Labels;
            if (!first.packageUniqueIds.IsSequenceEqual(second.packageUniqueIds))
                result |= PageFilters.ChangedTypes.Packages;
            return result;
        }

        public bool UpdateFilters(IPageFilters newFilters)
        {
            // We only check changed filters but not supported changed filter because we don't support changing supported filters externally
            var changedTypes = FindChangedTypes(m_Filters, newFilters);
            if (changedTypes == PageFilters.ChangedTypes.None)
                return false;
            UpdateFiltersInternal(new PageFilters(newFilters), changedTypes);
            return true;
        }

        public bool UpdateSortOption(PageSortOption newSortOption)
        {
            if (filters.sortOption == newSortOption)
                return false;
            var newFilters = new PageFilters(filters);
            var changedTypes = newFilters.UpdateSortOption(newSortOption);
            UpdateFiltersInternal(newFilters, changedTypes);
            return true;
        }

        protected bool UpdateFilterStatus(PageFilterStatus newStatus)
        {
            if (filters.status == newStatus)
                return false;
            var newFilters = new PageFilters(filters);
            var changedTypes = newFilters.UpdateStatus(newStatus);
            UpdateFiltersInternal(newFilters, changedTypes);
            return true;
        }

        protected virtual void UpdateFiltersInternal(PageFilters newFilters, PageFilters.ChangedTypes changedTypes, bool triggerEvent = true)
        {
            var previousFilters = m_Filters;
            m_Filters = newFilters;
            if (triggerEvent)
                onFiltersChanged?.Invoke(new PageFiltersChangeArgs { page = this, previousFilters = previousFilters, filterTypesChanged = changedTypes });
        }

        private bool UpdateSupportedFilters(Func<PageFilters, PageFilters.ChangedTypes> changeFunction, bool triggerChangeEvent)
        {
            if (changeFunction == null)
                return false;
            var newFilters = new PageFilters(filters);
            var changedTypes = changeFunction(newFilters);
            if (changedTypes == PageFilters.ChangedTypes.None)
                return false;
            UpdateFiltersInternal(newFilters, changedTypes, triggerChangeEvent);
            return true;
        }

        protected bool UpdateSupportedSortOptions(IReadOnlyList<PageSortOption> newSortOptions, bool triggerChangeEvent)
            => UpdateSupportedFilters(f => f.UpdateSupportedSortOptions(newSortOptions), triggerChangeEvent);

        protected bool UpdateSupportedStatuses(IReadOnlyList<PageFilterStatus> newStatuses, bool triggerChangeEvent)
            => UpdateSupportedFilters(f => f.UpdateSupportedStatuses(newStatuses), triggerChangeEvent);

        protected bool UpdateSupportedCategories(IReadOnlyList<string> newCategories, bool triggerChangeEvent)
            => UpdateSupportedFilters(f => f.UpdateSupportedCategories(newCategories), triggerChangeEvent);

        protected bool UpdateSupportedLabels(IReadOnlyList<string> newLabels, bool triggerChangeEvent)
            => UpdateSupportedFilters(f => f.UpdateSupportedLabels(newLabels), triggerChangeEvent);

        protected bool UpdateSupportedPackages(IReadOnlyList<string> newPackageUniqueIds, bool triggerChangeEvent)
            => UpdateSupportedFilters(f => f.UpdateSupportedPackages(newPackageUniqueIds), triggerChangeEvent);

        public virtual void Activate()
        {
            m_IsActive = true;
        }

        public virtual void Deactivate()
        {
            m_IsActive = false;
            ResetStatesOnDeactivate();
        }

        protected abstract void RefreshListOnSearchTextChange();

        protected void TriggerOnListUpdate(IReadOnlyCollection<string> added = null, IReadOnlyCollection<string> updated = null, IReadOnlyCollection<string> removed = null)
        {
            added ??= Array.Empty<string>();
            updated ??= Array.Empty<string>();
            removed ??= Array.Empty<string>();
            if (added.Count > 0 || updated.Count > 0 || removed.Count > 0)
                onListUpdate?.Invoke(new ListUpdateArgs { page = this, added = added, updated = updated, removed = removed });
        }

        protected void TriggerOnStateChange()
        {
            onStageChanged?.Invoke(new PageStateChangeArgs{ page = this, visible = visible, icon = icon});
        }

        protected void TriggerListRebuild()
        {
            onListRebuild?.Invoke();
        }

        protected void TriggerOnVisualStateChange(IReadOnlyCollection<VisualState> changedVisualStates)
        {
            if (changedVisualStates?.Count > 0)
                onVisualStateChange?.Invoke(new VisualStateChangeArgs { page = this, changed = changedVisualStates });
        }

        public virtual void TriggerOnSelectionChanged(bool isDirectMouseSelection)
        {
            onSelectionChanged?.Invoke(new PageSelectionChangeArgs
            {
                page = this,
                selection = GetSelection(),
                isDirectMouseSelection = isDirectMouseSelection
            });
        }

        public PageSelection GetSelection() => m_Selection;

        public virtual bool SetNewSelection(string itemUniqueId, bool isDirectMouseSelection)
        {
            return SetNewSelection(new[] { itemUniqueId }, isDirectMouseSelection);
        }

        public virtual bool SetNewSelection(IEnumerable<string> itemUniqueIds, bool isDirectMouseSelection)
        {
            if (!m_Selection.SetNewSelection(itemUniqueIds) && !isDirectMouseSelection)
                return false;

            TriggerOnSelectionChanged(isDirectMouseSelection);
            return true;
        }

        public virtual void RemoveSelection(IEnumerable<string> itemUniqueIds, bool isDirectMouseSelection)
        {
            var previousFirstSelection = GetSelection().first;
            AmendSelection(Array.Empty<string>(), itemUniqueIds, isDirectMouseSelection);
            if (GetSelection().Count == 0)
                SetNewSelection(new[] { previousFirstSelection }, isDirectMouseSelection);
        }

        public virtual bool AmendSelection(IEnumerable<string> toAdd, IEnumerable<string> toRemove, bool isDirectMouseSelection)
        {
            if (!m_Selection.AmendSelection(toAdd, toRemove) && !isDirectMouseSelection)
                return false;

            TriggerOnSelectionChanged(isDirectMouseSelection);
            return true;
        }

        public virtual bool ToggleSelection(string itemUniqueId, bool isDirectMouseSelection)
        {
            if (!m_Selection.ToggleSelection(itemUniqueId) && !isDirectMouseSelection)
                return false;

            TriggerOnSelectionChanged(isDirectMouseSelection);
            return true;
        }

        public virtual bool UpdateSelectionIfCurrentSelectionIsInvalid()
        {
            var selection = GetSelection();

            var invalidSelectionsToRemove = new List<string>(selection.Filter(i => visualStates.Get(i) is not { visible: true }));
            if (selection.Count > 0 && invalidSelectionsToRemove.Count == 0)
                return false;

            var newSelectionToAdd = new List<string>();
            if (invalidSelectionsToRemove.Count == selection.Count)
            {
                var firstVisible = visualStates.FirstMatch(v => v.visible && !selection.Contains(v.itemUniqueId));
                if (firstVisible != null)
                    newSelectionToAdd.Add(firstVisible.itemUniqueId);
            }

            return AmendSelection(newSelectionToAdd, invalidSelectionsToRemove, false);
        }

        public bool IsGroupExpanded(string groupName)
        {
            return !m_CollapsedGroups.Contains(groupName);
        }

        public virtual void SetGroupExpanded(string groupName, bool value)
        {
            if (value)
                m_CollapsedGroups.RemoveAll(i => i == groupName);
            else
                m_CollapsedGroups.Add(groupName);
        }

        public void SetUserUnlockedState(IEnumerable<string> itemUniqueIds, bool unlocked)
        {
            var changedVisualStates = new HashSet<VisualState>();
            foreach (var uniqueId in itemUniqueIds)
            {
                var visualState = visualStates.Get(uniqueId);
                if (visualState == null || visualState.userUnlocked == unlocked)
                    continue;
                visualState.userUnlocked = unlocked;
                changedVisualStates.Add(visualState);
            }
            TriggerOnVisualStateChange(changedVisualStates);
        }

        public void ResetStatesOnDeactivate()
        {
            var unlockedVisualStates = new List<VisualState>(visualStates.Filter(v => v.userUnlocked));
            foreach (var visualState in unlockedVisualStates)
                visualState.userUnlocked = false;
            TriggerOnVisualStateChange(unlockedVisualStates);

            if (m_Selection.Count == 0)
                return;

            foreach (var group in m_Selection.SelectNonEmpty(s => visualStates.Get(s)?.groupName).EnumerateDistinct())
                SetGroupExpanded(group, true);
        }

        public virtual void LoadMore(long numberOfItems) {}
        public virtual void Load(string itemUniqueId) {}
        public virtual void LoadExtraItems(IEnumerable<string> itemUniqueIds) {}
    }
}
