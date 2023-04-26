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
        public event Action<PageSelectionChangeArgs> onSelectionChanged = delegate { };
        public event Action<VisualStateChangeArgs> onVisualStateChange = delegate { };
        public event Action<ListUpdateArgs> onListUpdate = delegate { };
        public event Action<IPage> onListRebuild = delegate { };
        public event Action<IPage> onSubPageChanged = delegate { };
        public event Action<PageFilters> onFiltersChange = delegate { };

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

        public bool isActivePage => m_PackageManagerPrefs.currentFilterTab == tab;

        public abstract IVisualStateList visualStates { get; }
        public abstract IEnumerable<SubPage> subPages { get; }
        public abstract SubPage currentSubPage { get; set; }

        public abstract string contentType { get; set; }

        [NonSerialized]
        protected PackageDatabase m_PackageDatabase;
        [NonSerialized]
        protected PackageManagerPrefs m_PackageManagerPrefs;
        public void ResolveDependencies(PackageDatabase packageDatabase, PackageManagerPrefs packageManagerPrefs)
        {
            m_PackageDatabase = packageDatabase;
            m_PackageManagerPrefs = packageManagerPrefs;
        }

        protected BasePage(PackageDatabase packageDatabase, PackageManagerPrefs packageManagerPrefs, PackageFilterTab tab, PageCapability capability)
        {
            ResolveDependencies(packageDatabase, packageManagerPrefs);

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

        public virtual void OnEnable()
        {
            m_PackageDatabase.onPackageUniqueIdFinalize += OnPackageUniqueIdFinalize;
        }

        public virtual void OnDisable()
        {
            m_PackageDatabase.onPackageUniqueIdFinalize += OnPackageUniqueIdFinalize;
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

        public virtual void OnPackagesChanged(PackagesChangeArgs args)
        {
            var addList = new List<IPackage>();
            var updateList = new List<IPackage>();
            var removeList = args.removed.Where(p => visualStates.Contains(p.uniqueId)).ToList();
            foreach (var package in args.added.Concat(args.updated))
            {
                if (package.IsInTab(tab))
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

                UpdateVisualStateVisbilityWithSearchText();
            }
        }

        private void OnPackageUniqueIdFinalize(string tempPackageUniqueId, string finalPackageUniqueId)
        {
            if (!GetSelection().Contains(tempPackageUniqueId))
                return;
            AmendSelection(new[] { new PackageAndVersionIdPair(finalPackageUniqueId) }, new[] { new PackageAndVersionIdPair(tempPackageUniqueId) });
        }

        public void UpdateVisualStateVisbilityWithSearchText()
        {
            var changedVisualStates = new List<VisualState>();
            foreach (var state in visualStates)
            {
                var package = m_PackageDatabase.GetPackage(state.packageUniqueId);
                var visible = package?.versions.primary.MatchesSearchText(m_PackageManagerPrefs.trimmedSearchText) == true;
                if (state.visible != visible)
                {
                    state.visible = visible;
                    changedVisualStates.Add(state);
                }
            }

            if (changedVisualStates.Any())
                TriggerOnVisualStateChange(changedVisualStates);
        }

        public abstract void OnActivated();
        public abstract void OnDeactivated();

        public abstract void UpdateSearchText(string searchText);

        public abstract void RebuildAndReorderVisualStates();

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

        protected void TriggerListRebuild()
        {
            onListRebuild?.Invoke(this);
        }

        protected void TriggerOnVisualStateChange(IEnumerable<VisualState> visualStates)
        {
            onVisualStateChange?.Invoke(new VisualStateChangeArgs
            {
                page = this,
                visualStates = visualStates
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

        public void TriggerOnSubPageChanged()
        {
            onSubPageChanged?.Invoke(this);
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
            if (!m_Selection.SetNewSelection(packageAndVersionIds) && !isExplicitUserSelection)
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
            if (!m_Selection.AmendSelection(toAddOrUpdate, toRemove) && !isExplicitUserSelection)
                return false;

            TriggerOnSelectionChanged(isExplicitUserSelection);
            return true;
        }

        public virtual bool ToggleSelection(string packageUniqueId, bool isExplicitUserSelection = false)
        {
            if (!m_Selection.ToggleSelection(packageUniqueId) && !isExplicitUserSelection)
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

        public virtual string GetGroupName(IPackage package)
        {
            return GetDefaultGroupName(tab, package);
        }

        public static string GetDefaultGroupName(PackageFilterTab tab, IPackage package)
        {
            if (package.product != null)
                return PageManager.k_AssetStorePackageGroupName;

            var version = package.versions.primary;
            if (version.HasTag(PackageTag.BuiltIn))
                return string.Empty;

            if (version.HasTag(PackageTag.Unity))
                return tab == PackageFilterTab.UnityRegistry ? string.Empty : PageManager.k_UnityPackageGroupName;

            return string.IsNullOrEmpty(version?.author) ? PageManager.k_OtherPackageGroupName : version.author;
        }

        public abstract void LoadMore(long numberOfPackages);
        public abstract void Load(string packageUniqueId);
        public abstract void LoadExtraItems(IEnumerable<IPackage> packages);

        public abstract void SetPackagesUserUnlockedState(IEnumerable<string> packageUniqueIds, bool unlocked);
        public abstract void ResetUserUnlockedState();
        public abstract bool GetDefaultLockState(IPackage package);
        public abstract void AddSubPage(SubPage subPage);
    }
}
