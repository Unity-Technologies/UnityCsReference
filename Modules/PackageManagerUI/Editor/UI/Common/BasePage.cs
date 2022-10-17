// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal abstract class BasePage : IPage
    {
        public event Action<PageSelection> onSelectionChanged = delegate { };
        public event Action<VisualStateChangeArgs> onVisualStateChange = delegate {};
        public event Action<ListUpdateArgs> onListUpdate = delegate {};
        public event Action<IPage> onListRebuild = delegate {};
        public event Action<IPage> onSubPageChanged = delegate {};
        public event Action<PageFilters> onFiltersChange = delegate {};

        [SerializeField]
        private PageSelection m_Selection = new PageSelection();

        [SerializeField]
        private List<string> m_CollapsedGroups = new List<string>();

        [SerializeField]
        protected PackageFilterTab m_Tab;
        public PackageFilterTab tab => m_Tab;

        [SerializeField]
        protected PageFilters m_Filters;
        public PageFilters filters => m_Filters;

        [SerializeField]
        protected PageCapability m_Capability;
        public PageCapability capability => m_Capability;

        public bool isFullyLoaded => numTotalItems <= numCurrentItems;

        public abstract long numTotalItems { get; }

        public abstract long numCurrentItems { get; }

        public abstract IEnumerable<VisualState> visualStates { get; }
        public abstract IEnumerable<SubPage> subPages { get; }
        public abstract SubPage currentSubPage { get; set; }

        public abstract string contentType { get; set; }

        [NonSerialized]
        protected PackageDatabase m_PackageDatabase;
        protected void ResolveDependencies(PackageDatabase packageDatabase)
        {
            m_PackageDatabase = packageDatabase;
        }

        protected BasePage(PackageDatabase packageDatabase, PackageFilterTab tab, PageCapability capability)
        {
            ResolveDependencies(packageDatabase);

            m_Tab = tab;
            m_Capability = capability;
            if (m_Filters == null)
            {
                var defaultOrdering = m_Capability?.orderingValues?.FirstOrDefault();
                m_Filters = new PageFilters
                {
                    orderBy = defaultOrdering?.orderBy,
                    isReverseOrder = false
                };
            }
        }

        public bool ClearFilters()
        {
            var filters = m_Filters?.Clone() ?? new PageFilters();
            filters.status = string.Empty;
            filters.categories = new List<string>();
            filters.labels = new List<string>();

            return UpdateFilters(filters);
        }

        public virtual bool UpdateFilters(PageFilters filters)
        {
            if ((m_Filters == null && filters == null) || (m_Filters?.Equals(filters) ?? false))
                return false;

            m_Filters = filters?.Clone();
            onFiltersChange?.Invoke(m_Filters);
            return true;
        }

        public abstract void OnPackagesChanged(PackagesChangeArgs args);

        public abstract void Rebuild();

        protected void TriggerOnListUpdate(IEnumerable<IPackage> added = null, IEnumerable<IPackage> updated = null, IEnumerable<IPackage> removed = null)
        {
            added ??= Enumerable.Empty<IPackage>();
            updated ??= Enumerable.Empty<IPackage>();
            removed ??= Enumerable.Empty<IPackage>();
            var anyAddedOrUpdated = added.Any() || updated.Any();
            if (!anyAddedOrUpdated && !removed.Any())
                return;

            var reorder = capability.supportLocalReordering && anyAddedOrUpdated;
            onListUpdate?.Invoke(new ListUpdateArgs { page = this, added = added, updated = updated, removed = removed, reorder = reorder });
        }

        protected void TriggerOnListRebuild()
        {
            onListRebuild?.Invoke(this);
        }

        public virtual void SetPackagesUserUnlockedState(IEnumerable<string> packageUniqueIds, bool unlocked)
        {
            // do nothing, only simple page needs implementation right now
        }

        public virtual void ResetUserUnlockedState()
        {
            // do nothing, only simple page needs implementation right now
        }

        public virtual bool GetDefaultLockState(IPackage package)
        {
            return false;
        }

        protected void TriggerOnVisualStateChange(IEnumerable<VisualState> visualStates)
        {
            onVisualStateChange?.Invoke(new VisualStateChangeArgs
            {
                page = this,
                visualStates = visualStates
            });
        }

        public virtual void TriggerOnSelectionChanged()
        {
            onSelectionChanged?.Invoke(GetSelection());
        }

        public void TriggerOnSubPageChanged()
        {
            onSubPageChanged?.Invoke(this);
        }

        public abstract VisualState GetVisualState(string packageUniqueId);

        private VisualState GetVisualState(PackageAndVersionIdPair packageAndVersionId)
        {
            return GetVisualState(packageAndVersionId?.packageUniqueId);
        }

        public PageSelection GetSelection() => m_Selection;

        public IEnumerable<VisualState> GetSelectedVisualStates()
        {
            return m_Selection.Select(s => GetVisualState(s)).Where(v => v != null);
        }

        public virtual bool SetNewSelection(IEnumerable<PackageAndVersionIdPair> packageAndVersionIds)
        {
            var numOldSelections = m_Selection.Count;
            if (!m_Selection.SetNewSelection(packageAndVersionIds))
                return false;

            TriggerOnSelectionChanged();
            return true;
        }

        public virtual bool AmendSelection(IEnumerable<PackageAndVersionIdPair> toAddOrUpdate, IEnumerable<PackageAndVersionIdPair> toRemove)
        {
            var numOldSelections = m_Selection.Count;
            if (!m_Selection.AmendSelection(toAddOrUpdate, toRemove))
                return false;

            TriggerOnSelectionChanged();
            return true;
        }

        public virtual bool ToggleSelection(string packageUniqueId)
        {
            var numOldSelections = m_Selection.Count;
            if (!m_Selection.ToggleSelection(packageUniqueId))
                return false;

            TriggerOnSelectionChanged();
            return true;
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

        public bool Contains(IPackage package)
        {
            return Contains(package?.uniqueId);
        }

        public virtual string GetGroupName(IPackage package)
        {
            return GetDefaultGroupName(tab, package);
        }

        public static string GetDefaultGroupName(PackageFilterTab tab, IPackage package)
        {
            if (package.Is(PackageType.BuiltIn))
                return string.Empty;

            if (package.Is(PackageType.AssetStore))
                return PageManager.k_AssetStorePackageGroupName;

            if (package.Is(PackageType.Unity))
                return tab == PackageFilterTab.UnityRegistry ? string.Empty : PageManager.k_UnityPackageGroupName;

            return string.IsNullOrEmpty(package.versions.primary?.author) ?
                PageManager.k_OtherPackageGroupName :
                package.versions.primary.author;
        }

        public abstract bool Contains(string packageUniqueId);

        public abstract void LoadMore(long numberOfPackages);

        public abstract void Load(IPackage package, IPackageVersion version = null);

        public abstract void LoadExtraItems(IEnumerable<IPackage> packages);

        public abstract void AddSubPage(SubPage subPage);
    }
}
