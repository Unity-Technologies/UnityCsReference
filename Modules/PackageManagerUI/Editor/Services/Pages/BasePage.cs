// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
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
        public event Action<IPage> onListRebuild = delegate {};
        public event Action<PageFilters> onFiltersChange = delegate {};
        public event Action<string> onTrimmedSearchTextChanged = delegate {};
        public event Action<IPage> onSupportedStatusFiltersChanged = delegate {};

        [SerializeField]
        private PageSelection m_Selection = new();

        [SerializeField]
        private List<string> m_CollapsedGroups = new();

        [SerializeField]
        private string m_SearchText = string.Empty;
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
                onTrimmedSearchTextChanged?.Invoke(newTrimmedSearchText);
            }
        }

        [SerializeField]
        private string m_TrimmedSearchText = string.Empty;
        public string trimmedSearchText => m_TrimmedSearchText;

        [SerializeField]
        private PageFilters m_Filters;
        public PageFilters filters
        {
            get
            {
                m_Filters ??= new PageFilters { sortOption = supportedSortOptions?.FirstOrDefault() ?? PageSortOption.NameAsc };
                return m_Filters;
            }
            protected set => m_Filters = value;
        }

        public abstract PageCapability capability { get; }
        public abstract IEnumerable<PageFilters.Status> supportedStatusFilters { get; }
        public abstract IEnumerable<PageSortOption> supportedSortOptions { get; }

        [SerializeField]
        private bool m_IsActive;
        public bool isActivePage => m_IsActive;

        public abstract string id { get; }
        public abstract string displayName { get; }
        public abstract RefreshOptions refreshOptions { get; }
        public abstract IVisualStateList visualStates { get; }

        [NonSerialized]
        protected PackageDatabase m_PackageDatabase;
        public void ResolveDependencies(PackageDatabase packageDatabase)
        {
            m_PackageDatabase = packageDatabase;
        }

        protected BasePage(PackageDatabase packageDatabase)
        {
            ResolveDependencies(packageDatabase);
        }

        public virtual void OnEnable()
        {
            m_PackageDatabase.onPackageUniqueIdFinalize += OnPackageUniqueIdFinalize;
        }

        public virtual void OnDisable()
        {
            m_PackageDatabase.onPackageUniqueIdFinalize -= OnPackageUniqueIdFinalize;
        }

        public bool ClearFilters(bool resetSortOptionToDefault = false)
        {
            var newFilters = filters.Clone();
            newFilters.status = PageFilters.Status.None;
            newFilters.categories = new List<string>();
            newFilters.labels = new List<string>();
            if (resetSortOptionToDefault)
                newFilters.sortOption = supportedSortOptions.FirstOrDefault();
            return UpdateFilters(newFilters);
        }

        public virtual bool UpdateFilters(PageFilters newFilters)
        {
            if (filters.Equals(newFilters))
                return false;
            filters = newFilters?.Clone();
            onFiltersChange?.Invoke(filters);
            return true;
        }

        public abstract bool ShouldInclude(IPackage package);

        public virtual void OnPackagesChanged(PackagesChangeArgs args)
        {
            var addList = new List<IPackage>();
            var updateList = new List<IPackage>();
            var removeList = args.removed.Where(p => visualStates.Contains(p.uniqueId)).ToList();
            foreach (var package in args.added.Concat(args.updated))
            {
                if (ShouldInclude(package))
                {
                    if (visualStates.Contains(package.uniqueId))
                        updateList.Add(package);
                    else
                        addList.Add(package);
                }
                else if (visualStates.Contains(package.uniqueId))
                    removeList.Add(package);
            }

            if (addList.Any() || updateList.Any() || removeList.Any())
            {
                RebuildAndReorderVisualStates();

                TriggerOnListUpdate(addList, updateList, removeList);

                CheckEntitlementStatusAndTriggerEvents(addList, updateList, removeList);

                UpdateVisualStateVisibilityWithSearchText();
            }
        }

        private void OnPackageUniqueIdFinalize(string tempPackageUniqueId, string finalPackageUniqueId)
        {
            if (!GetSelection().Contains(tempPackageUniqueId))
                return;
            AmendSelection(new[] { new PackageAndVersionIdPair(finalPackageUniqueId) }, new[] { new PackageAndVersionIdPair(tempPackageUniqueId) });
        }

        public void UpdateVisualStateVisibilityWithSearchText()
        {
            var changedVisualStates = new List<VisualState>();
            foreach (var state in visualStates)
            {
                var package = m_PackageDatabase.GetPackage(state.packageUniqueId);
                var visible = package?.versions.primary.MatchesSearchText(trimmedSearchText) == true;
                if (state.visible != visible)
                {
                    state.visible = visible;
                    changedVisualStates.Add(state);
                }
            }

            if (changedVisualStates.Any())
                TriggerOnVisualStateChange(changedVisualStates);
        }

        public virtual void OnActivated()
        {
            m_IsActive = true;
        }

        public virtual void OnDeactivated()
        {
            m_IsActive = false;
        }

        protected abstract void RefreshListOnSearchTextChange();

        public abstract void RebuildAndReorderVisualStates();

        // Returns true if SupportedStatusFilter changed
        public virtual bool RefreshSupportedStatusFiltersOnEntitlementPackageChange() => false;

        protected virtual void TriggerOnListUpdate(IList<IPackage> added = null, IList<IPackage> updated = null, IList<IPackage> removed = null)
        {
            added ??= Array.Empty<IPackage>();
            updated ??= Array.Empty<IPackage>();
            removed ??= Array.Empty<IPackage>();
            var anyAddedOrUpdated = added.Any() || updated.Any();
            if (!anyAddedOrUpdated && !removed.Any())
                return;

            var reorder = (capability & PageCapability.SupportLocalReordering) != 0 && anyAddedOrUpdated;
            onListUpdate?.Invoke(new ListUpdateArgs
                { page = this, added = added, updated = updated, removed = removed, reorder = reorder });
        }

        protected void CheckEntitlementStatusAndTriggerEvents(IList<IPackage> added = null, IList<IPackage> updated = null, IList<IPackage> removed = null)
        {
            if ((capability & PageCapability.DynamicEntitlementStatus) == 0)
                return;

            added ??= Array.Empty<IPackage>();
            updated ??= Array.Empty<IPackage>();
            removed ??= Array.Empty<IPackage>();
            if (!added.Concat(updated).Concat(removed).Any(p => p.hasEntitlements))
                return;

            if (!RefreshSupportedStatusFiltersOnEntitlementPackageChange())
                return;

            // For now, this will only happens if the last entitlement package is removed and the Subscription Based filter was selected
            if (!supportedStatusFilters.Contains(filters.status) && filters.status != PageFilters.Status.None)
            {
                var newFilters = filters.Clone();
                newFilters.status = PageFilters.Status.None;
                UpdateFilters(newFilters);
            }
            TriggerSupportedFiltersChanged();
        }

        protected void TriggerListRebuild()
        {
            onListRebuild?.Invoke(this);
        }

        protected void TriggerSupportedFiltersChanged()
        {
            onSupportedStatusFiltersChanged?.Invoke(this);
        }

        protected void TriggerOnVisualStateChange(IEnumerable<VisualState> changedVisualStates)
        {
            onVisualStateChange?.Invoke(new VisualStateChangeArgs
            {
                page = this,
                visualStates = changedVisualStates
            });
        }

        public virtual void TriggerOnSelectionChanged(bool isExplicitUserSelection = false)
        {
            onSelectionChanged?.Invoke(new PageSelectionChangeArgs
            {
                page = this,
                selection = GetSelection(),
                isExplicitUserSelection = isExplicitUserSelection
            });
        }

        public PageSelection GetSelection() => m_Selection;

        public IEnumerable<VisualState> GetSelectedVisualStates()
        {
            return m_Selection.Select(s => visualStates.Get(s?.packageUniqueId)).Where(v => v != null);
        }

        public virtual bool SetNewSelection(IPackage package, IPackageVersion version = null, bool isExplicitUserSelection = false)
        {
            return SetNewSelection(new[] { new PackageAndVersionIdPair(package?.uniqueId, version?.uniqueId) }, isExplicitUserSelection);
        }

        public virtual bool SetNewSelection(PackageAndVersionIdPair packageAndVersionId, bool isExplicitUserSelection = false)
        {
            return SetNewSelection(new[] { packageAndVersionId }, isExplicitUserSelection);
        }

        public virtual bool SetNewSelection(IEnumerable<PackageAndVersionIdPair> packageAndVersionIds, bool isExplicitUserSelection = false)
        {
            if (!m_Selection.SetNewSelection(packageAndVersionIds))
                return false;

            TriggerOnSelectionChanged(isExplicitUserSelection);
            return true;
        }

        public virtual void RemoveSelection(IEnumerable<PackageAndVersionIdPair> toRemove, bool isExplicitUserSelection = false)
        {
            var previousFirstSelection = GetSelection().firstSelection;
            AmendSelection(Enumerable.Empty<PackageAndVersionIdPair>(), toRemove, isExplicitUserSelection);
            if (!GetSelection().Any())
                SetNewSelection(new[] { previousFirstSelection }, isExplicitUserSelection);
        }

        public virtual bool AmendSelection(IEnumerable<PackageAndVersionIdPair> toAddOrUpdate, IEnumerable<PackageAndVersionIdPair> toRemove, bool isExplicitUserSelection = false)
        {
            if (!m_Selection.AmendSelection(toAddOrUpdate, toRemove))
                return false;

            TriggerOnSelectionChanged(isExplicitUserSelection);
            return true;
        }

        public virtual bool ToggleSelection(string packageUniqueId, bool isExplicitUserSelection = false)
        {
            if (!m_Selection.ToggleSelection(packageUniqueId))
                return false;

            TriggerOnSelectionChanged(isExplicitUserSelection);
            return true;
        }

        public virtual bool UpdateSelectionIfCurrentSelectionIsInvalid()
        {
            var selection = GetSelection();

            var invalidSelectionsToRemove = new List<PackageAndVersionIdPair>();
            foreach (var item in selection)
            {
                m_PackageDatabase.GetPackageAndVersion(item, out var package, out var version);
                var visualState = visualStates.Get(item.packageUniqueId);
                if (package == null || visualState?.visible != true)
                    invalidSelectionsToRemove.Add(item);
            }

            if (selection.Count > 0 && invalidSelectionsToRemove.Count == 0)
                return false;

            var newSelectionToAdd = new List<PackageAndVersionIdPair>();
            if (invalidSelectionsToRemove.Count == selection.Count)
            {
                var firstVisible = visualStates.FirstOrDefault(v => v.visible && !selection.Contains(v.packageUniqueId));
                if (firstVisible != null)
                    newSelectionToAdd.Add(new PackageAndVersionIdPair(firstVisible.packageUniqueId));
            }

            return AmendSelection(newSelectionToAdd, invalidSelectionsToRemove);
        }

        public bool IsGroupExpanded(string groupName)
        {
            return !m_CollapsedGroups.Contains(groupName);
        }

        public void SetGroupExpanded(string groupName, bool value)
        {
            var groupExpanded = !m_CollapsedGroups.Contains(groupName);
            if (groupExpanded == value)
                return;
            if (value)
                m_CollapsedGroups.Remove(groupName);
            else
                m_CollapsedGroups.Add(groupName);
        }

        public virtual string GetGroupName(IPackage package) => string.Empty;

        public abstract void LoadMore(long numberOfPackages);
        public abstract void Load(string packageUniqueId);
        public abstract void LoadExtraItems(IEnumerable<IPackage> packages);

        public abstract void SetPackagesUserUnlockedState(IEnumerable<string> packageUniqueIds, bool unlocked);
        public abstract void ResetUserUnlockedState();
        public abstract bool GetDefaultLockState(IPackage package);
    }
}
