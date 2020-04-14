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
    internal class SimplePage : BasePage
    {
        [SerializeField]
        private VisualStateList m_VisualStateList = new VisualStateList();

        public override long numTotalItems => m_VisualStateList.numTotalItems;

        public override long numCurrentItems => m_VisualStateList.numItems;

        public override IEnumerable<VisualState> visualStates => m_VisualStateList;

        [NonSerialized]
        private PackageFiltering m_PackageFiltering;
        public void ResolveDependencies(PackageDatabase packageDatabase, PackageFiltering packageFiltering)
        {
            ResolveDependencies(packageDatabase);
            m_PackageFiltering = packageFiltering;
        }

        public SimplePage(PackageDatabase packageDatabase, PackageFiltering packageFiltering, PackageFilterTab tab, PageCapability capability) : base(packageDatabase, tab, capability)
        {
            ResolveDependencies(packageDatabase, packageFiltering);
        }

        public override void UpdateFilters(PageFilters filters)
        {
            if (this.filters.Equals(filters))
                return;

            m_Filters = filters.Clone();
            Rebuild();
        }

        public override void OnPackagesChanged(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> preUpdate, IEnumerable<IPackage> postUpdate)
        {
            var addOrUpdateList = new List<IPackage>();
            var removeList = removed.Where(p => Contains(p)).ToList();
            foreach (var package in added.Concat(postUpdate))
            {
                if (m_PackageFiltering.FilterByCurrentTab(package))
                    addOrUpdateList.Add(package);
                else if (Contains(package))
                    removeList.Add(package);
            }

            if (addOrUpdateList.Any() || removeList.Any())
            {
                RebuildOrderedVisualStates();

                TriggerOnListUpdate(addOrUpdateList, removeList, addOrUpdateList.Any());

                RefreshVisualStates();
            }
        }

        public override void Rebuild()
        {
            RebuildOrderedVisualStates();
            TriggerOnListRebuild();
            RefreshVisualStates();
        }

        private void RebuildOrderedVisualStates()
        {
            var packages = m_PackageDatabase.allPackages
                .Where(p => m_PackageFiltering.FilterByCurrentTab(p));

            IOrderedEnumerable<IPackage> orderedPackages;
            if (m_Filters.orderBy == "name")
                orderedPackages = packages.OrderBy(p => p.name);
            else if (m_Filters.orderBy == "publishedDate")
                orderedPackages = packages.OrderBy(p => p.versions.primary?.publishedDate ?? new DateTime(1, 1, 1));
            else // displayName
                orderedPackages = packages.OrderBy(p => p.versions.primary?.displayName ?? p.name);

            var orderedPackageIds = m_Filters?.isReverseOrder ?? false ?
                orderedPackages.Reverse().Select(p => p.uniqueId) :
                orderedPackages.Select(p => p.uniqueId);
            m_VisualStateList.Rebuild(orderedPackageIds);
        }

        private void RefreshVisualStates()
        {
            var changedVisualStates = new List<VisualState>();
            foreach (var state in m_VisualStateList)
            {
                var package = m_PackageDatabase.GetPackage(state.packageUniqueId);
                var visible = m_PackageFiltering.FilterByCurrentSearchText(package);
                if (state.visible != visible)
                {
                    state.visible = visible;
                    changedVisualStates.Add(state);
                }
            }

            if (changedVisualStates.Any())
                TriggerOnVisualStateChange(changedVisualStates);
        }

        public override VisualState GetVisualState(string packageUniqueId)
        {
            return m_VisualStateList.GetVisualState(packageUniqueId);
        }

        public override void SetExpanded(string packageUniqueId, bool value)
        {
            if (m_VisualStateList.SetExpanded(packageUniqueId, value))
                TriggerOnVisualStateChange(new[] { m_VisualStateList.GetVisualState(packageUniqueId) });
        }

        public override void SetSeeAllVersions(string packageUniqueId, bool value)
        {
            if (m_VisualStateList.SetSeeAllVersions(packageUniqueId, value))
                TriggerOnVisualStateChange(new[] { m_VisualStateList.GetVisualState(packageUniqueId) });
        }

        public override bool Contains(string packageUniqueId)
        {
            return m_VisualStateList.Contains(packageUniqueId);
        }

        public override void LoadMore(int numberOfPackages)
        {
            // do nothing, as for a single page we have a complete/known list and there's no more to load
        }

        public override void Load(IPackage package, IPackageVersion version = null)
        {
            // do nothing, as for a single page we have a complete/known list and there's no more to load
        }
    }
}
