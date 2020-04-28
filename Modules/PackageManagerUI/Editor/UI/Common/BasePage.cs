// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal abstract class BasePage : IPage
    {
        public event Action<IPackageVersion> onSelectionChanged = delegate {};
        public event Action<IEnumerable<VisualState>> onVisualStateChange = delegate {};
        public event Action<IPage, IEnumerable<IPackage>, IEnumerable<IPackage>, bool> onListUpdate = delegate {};
        public event Action<IPage> onListRebuild = delegate {};

        // keep track of a list of selected items by remembering the uniqueIds
        [SerializeField]
        protected List<string> m_SelectedUniqueIds = new List<string>();

        [SerializeField]
        protected PackageFilterTab m_Tab;
        public PackageFilterTab tab => m_Tab;

        [SerializeField]
        protected PageFilters m_Filters;
        public PageFilters filters
        {
            get { return m_Filters; }
            set { if (value == null) m_Filters = new PageFilters(); else m_Filters = value; }
        }

        [SerializeField]
        protected PageCapability m_Capability;
        public PageCapability capability => m_Capability;

        public bool isFullyLoaded => numTotalItems <= numCurrentItems;

        public abstract long numTotalItems { get; }

        public abstract long numCurrentItems { get; }

        public abstract IEnumerable<VisualState> visualStates { get; }

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

        public void ClearFilters()
        {
            var filters = m_Filters?.Clone() ?? new PageFilters();
            filters.statuses = new List<string>();
            filters.categories = new List<string>();
            filters.labels = new List<string>();

            UpdateFilters(filters);
        }

        public abstract void UpdateFilters(PageFilters filters);

        public abstract void OnPackagesChanged(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> preUpdate, IEnumerable<IPackage> postUpdate);

        public abstract void Rebuild();

        protected void TriggerOnListUpdate(IEnumerable<IPackage> addOrUpdateList, IEnumerable<IPackage> removeList, bool reorder)
        {
            onListUpdate?.Invoke(this, addOrUpdateList, removeList, reorder);
        }

        protected void TriggerOnListRebuild()
        {
            onListRebuild?.Invoke(this);
        }

        protected void TriggerOnVisualStateChange(IEnumerable<VisualState> visualStates)
        {
            onVisualStateChange?.Invoke(visualStates);
        }

        protected void TriggerOnSelectionChanged(IPackageVersion version)
        {
            onSelectionChanged?.Invoke(version);
        }

        public abstract VisualState GetVisualState(string packageUniqueId);

        public IPackageVersion GetSelectedVersion()
        {
            IPackage package;
            IPackageVersion version;
            GetSelectedPackageAndVersion(out package, out version);
            return version;
        }

        public void GetSelectedPackageAndVersion(out IPackage package, out IPackageVersion version)
        {
            var selected = GetVisualState(m_SelectedUniqueIds.FirstOrDefault());
            m_PackageDatabase.GetPackageAndVersion(selected?.packageUniqueId, selected?.selectedVersionId, out package, out version);
        }

        public void SetSelected(IPackage package, IPackageVersion version = null)
        {
            SetSelected(package?.uniqueId, version?.uniqueId ?? package?.versions.primary?.uniqueId);
        }

        public void SetSelected(string packageUniqueId, string versionUniqueId)
        {
            var oldPackageUniqueId = m_SelectedUniqueIds.FirstOrDefault();
            var oldSelection = GetVisualState(oldPackageUniqueId);
            if (oldPackageUniqueId == packageUniqueId && oldSelection?.selectedVersionId == versionUniqueId)
                return;

            foreach (var uniqueId in m_SelectedUniqueIds)
            {
                var state = GetVisualState(uniqueId);
                if (state != null)
                    state.selectedVersionId = string.Empty;
            }
            m_SelectedUniqueIds.Clear();

            if (!string.IsNullOrEmpty(packageUniqueId) && !string.IsNullOrEmpty(versionUniqueId))
            {
                var selectedState = GetVisualState(packageUniqueId);
                if (selectedState != null)
                {
                    selectedState.selectedVersionId = versionUniqueId;
                    m_SelectedUniqueIds.Add(packageUniqueId);
                }
            }
            TriggerOnSelectionChanged(GetSelectedVersion());
            TriggerOnVisualStateChange(new[] { GetVisualState(oldPackageUniqueId), GetVisualState(packageUniqueId) }.Where(s => s != null));
        }

        public void SetExpanded(IPackage package, bool value)
        {
            SetExpanded(package?.uniqueId, value);
        }

        public abstract void SetExpanded(string packageUniqueId, bool value);

        public void SetSeeAllVersions(IPackage package, bool value)
        {
            SetSeeAllVersions(package?.uniqueId, value);
        }

        public abstract void SetSeeAllVersions(string packageUniqueId, bool value);

        public bool Contains(IPackage package)
        {
            return Contains(package?.uniqueId);
        }

        public abstract bool Contains(string packageUniqueId);

        public abstract void LoadMore(int numberOfPackages);

        public abstract void Load(IPackage package, IPackageVersion version = null);
    }
}
