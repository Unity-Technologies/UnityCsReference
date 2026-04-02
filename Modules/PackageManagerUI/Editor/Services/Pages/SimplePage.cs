// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal abstract class SimplePage : BasePage
    {
        [SerializeField]
        protected VisualStateList m_VisualStateList = new();
        public override IVisualStateList visualStates => m_VisualStateList;

        protected override void UpdateFiltersInternal(PageFilters newFilters, PageFilters.ChangedTypes changedTypes, bool triggerEvent = true)
        {
            base.UpdateFiltersInternal(newFilters, changedTypes, triggerEvent);
            if (!changedTypes.AnyFilterValuesChanged())
                return;

            if (changedTypes.HasFlag(PageFilters.ChangedTypes.SortOption))
                Rebuild(true);
            else
                ApplyFiltersAndSearchText(true);
        }

        protected override void RefreshListOnSearchTextChange()
        {
            ApplyFiltersAndSearchText(true);
        }

        protected void Rebuild(bool triggerEvent)
        {
            RebuildVisualStateList();
            if (triggerEvent)
                TriggerListRebuild();
            ApplyFiltersAndSearchText(triggerEvent);
        }

        protected void IncrementalListUpdate(IReadOnlyCollection<string> added = null, IReadOnlyCollection<string> updated = null, IReadOnlyCollection<string> removed = null)
        {
            added ??= Array.Empty<string>();
            updated ??= Array.Empty<string>();
            removed ??= Array.Empty<string>();
            if (added.Count == 0 & updated.Count == 0 && removed.Count == 0)
                return;

            // Note that even for incremental update we still just rebuild the list for simplicity because the grouping and ordering of an item might have changed
            RebuildVisualStateList();
            TriggerOnListUpdate(added, updated, removed);
            ApplyFiltersAndSearchText(true);
        }

        private void ApplyFiltersAndSearchText(bool triggerEvent)
        {
            var changedVisualStates = new List<VisualState>();
            foreach (var state in visualStates)
            {
                var newVisibility = MatchesSearchTextAndFilter(state.itemUniqueId);
                if (state.visible == newVisibility)
                    continue;
                state.visible = newVisibility;
                changedVisualStates.Add(state);
            }

            if (triggerEvent && changedVisualStates.Count > 0)
                TriggerOnVisualStateChange(changedVisualStates);
        }

        protected abstract bool MatchesSearchTextAndFilter(string itemUniqueId);

        protected abstract void RebuildVisualStateList();

        public override void Activate()
        {
            Rebuild(false);
            base.Activate();
        }

        protected virtual int CompareGroupName(string x, string y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
    }
}
