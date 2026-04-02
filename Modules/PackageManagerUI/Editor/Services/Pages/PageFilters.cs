// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class PageFiltersChangedTypeExtension
    {
        public static bool AnySupportedFiltersChanged(this PageFilters.ChangedTypes value) =>
            (value & (PageFilters.ChangedTypes.SupportedStatuses | PageFilters.ChangedTypes.SupportedSortOptions | PageFilters.ChangedTypes.SupportedCategories | PageFilters.ChangedTypes.SupportedLabels | PageFilters.ChangedTypes.SupportedPackages)) != 0;
        public static bool AnyFilterValuesChanged(this PageFilters.ChangedTypes value) =>
            (value & (PageFilters.ChangedTypes.Status | PageFilters.ChangedTypes.SortOption | PageFilters.ChangedTypes.Categories | PageFilters.ChangedTypes.Labels | PageFilters.ChangedTypes.Packages)) != 0;
    }

    internal interface IPageFilters
    {
        public PageFilterStatus status { get; }
        public IReadOnlyList<PageFilterStatus> supportedStatuses { get; }

        public PageSortOption sortOption { get; }
        public IReadOnlyList<PageSortOption> supportedSortOptions { get; }

        public IReadOnlyList<string> categories { get; }
        public IReadOnlyList<string> supportedCategories { get; }

        public IReadOnlyList<string> labels { get; }
        public IReadOnlyList<string> supportedLabels { get; }

        public IReadOnlyList<string> packageUniqueIds { get; }
        public IReadOnlyList<string> supportedPackageUniqueIds { get; }

        public bool anySupportedFilters { get; }
        public bool isFilterSet { get; }

        public bool Equals(IPageFilters other);
    }

    [Serializable]
    internal class PageFilters : IPageFilters, IEquatable<IPageFilters>
    {
        [Flags]
        internal enum ChangedTypes : uint
        {
            None                    = 0,

            Status                  = 1 << 0,
            SortOption              = 1 << 1,
            Categories              = 1 << 2,
            Labels                  = 1 << 3,
            Packages                = 1 << 4,

            SupportedStatuses       = 1 << 5,
            SupportedSortOptions    = 1 << 6,
            SupportedCategories     = 1 << 7,
            SupportedLabels         = 1 << 8,
            SupportedPackages       = 1 << 9,
        }

        [Serializable]
        internal class StringFilters
        {
            public List<string> supported = new ();
            public List<string> selected = new ();

            public StringFilters(IEnumerable<string> supported, IEnumerable<string> selected)
            {
                this.supported.AddRange(supported ?? Array.Empty<string>());
                this.selected.AddRange(selected ?? Array.Empty<string>());
            }

            // Both the selected items and supported items list should be very small, so it is not worth it to create a HashSet for faster look up
            public bool RemoveUnsupportedSelections()
            {
                var removedAny = false;
                for (var i = selected.Count - 1; i >= 0; i--)
                    if (!supported.Contains(selected[i]))
                    {
                        selected.RemoveAt(i);
                        removedAny = true;
                    }
                return removedAny;
            }
        }

        [SerializeField]
        private StringFilters m_Categories;
        public IReadOnlyList<string> categories => m_Categories.selected;
        public IReadOnlyList<string> supportedCategories => m_Categories.supported;

        public ChangedTypes UpdateCategories(IReadOnlyList<string> newCategories)
            => UpdateList(m_Categories.selected, newCategories) ? ChangedTypes.Categories : ChangedTypes.None;

        public ChangedTypes UpdateSupportedCategories(IReadOnlyList<string> newSupportedCategories)
        {
            if (!UpdateList(m_Categories.supported, newSupportedCategories))
                return ChangedTypes.None;
            return m_Categories.RemoveUnsupportedSelections() ? ChangedTypes.SupportedCategories | ChangedTypes.Categories : ChangedTypes.SupportedCategories;
        }

        public bool IsCategorySelected(string category) => m_Categories.selected.Contains(category);

        [SerializeField]
        protected StringFilters m_Labels;
        public IReadOnlyList<string> labels => m_Labels.selected;
        public IReadOnlyList<string> supportedLabels => m_Labels.supported;

        public ChangedTypes UpdateLabels(IReadOnlyList<string> newLabels)
        {
            if (!UpdateList(m_Labels.selected, newLabels))
                return ChangedTypes.None;
            var changedTypes = ChangedTypes.Labels;
            if (m_Labels.selected.Count > 0 && m_Status == PageFilterStatus.Unlabeled)
            {
                m_Status = PageFilterStatus.None;
                changedTypes |= ChangedTypes.Status;
            }
            return changedTypes;
        }

        public ChangedTypes UpdateSupportedLabels(IReadOnlyList<string> newSupportedLabels)
        {
            if (!UpdateList(m_Labels.supported, newSupportedLabels))
                return ChangedTypes.None;
            return m_Labels.RemoveUnsupportedSelections() ? ChangedTypes.SupportedLabels | ChangedTypes.Labels : ChangedTypes.SupportedLabels;
        }

        public bool IsLabelSelected(string label) => m_Labels.selected.Contains(label);

        [SerializeField]
        protected StringFilters m_PackageUniqueIds;
        public IReadOnlyList<string> packageUniqueIds => m_PackageUniqueIds.selected;
        public IReadOnlyList<string> supportedPackageUniqueIds => m_PackageUniqueIds.supported;

        public ChangedTypes UpdatePackages(IReadOnlyList<string> newPackageUniqueIds)
            => UpdateList(m_PackageUniqueIds.selected, newPackageUniqueIds) ? ChangedTypes.Packages : ChangedTypes.None;

        public ChangedTypes UpdateSupportedPackages(IReadOnlyList<string> newSupportedPackageUniqueIds)
        {
            if (!UpdateList(m_PackageUniqueIds.supported, newSupportedPackageUniqueIds))
                return ChangedTypes.None;
            return m_PackageUniqueIds.RemoveUnsupportedSelections() ? ChangedTypes.SupportedPackages | ChangedTypes.Packages : ChangedTypes.SupportedPackages;
        }

        public bool IsPackageSelected(string packageUniqueId) => m_PackageUniqueIds.selected.Contains(packageUniqueId);

        [SerializeField]
        private List<PageFilterStatus> m_SupportedStatuses;
        public IReadOnlyList<PageFilterStatus> supportedStatuses => m_SupportedStatuses;

        public ChangedTypes UpdateSupportedStatuses(IReadOnlyList<PageFilterStatus> newStatuses)
        {
            if (!UpdateList(m_SupportedStatuses, newStatuses))
                return ChangedTypes.None;
            if (status != PageFilterStatus.None && !m_SupportedStatuses.Contains(status))
                return UpdateStatus(PageFilterStatus.None) | ChangedTypes.SupportedStatuses;
            return ChangedTypes.SupportedStatuses;
        }

        [SerializeField]
        protected PageFilterStatus m_Status;
        public PageFilterStatus status => m_Status;

        public ChangedTypes UpdateStatus(PageFilterStatus newStatus)
        {
            if (m_Status == newStatus)
                return ChangedTypes.None;
            m_Status = newStatus;
            var result = ChangedTypes.Status;
            if (newStatus == PageFilterStatus.Unlabeled && m_Labels.selected.Count > 0)
            {
                m_Labels.selected.Clear();
                result |= ChangedTypes.Labels;
            }
            return result;
        }

        [SerializeField]
        private List<PageSortOption> m_SupportedSortOptions;
        public IReadOnlyList<PageSortOption> supportedSortOptions => m_SupportedSortOptions;

        public ChangedTypes UpdateSupportedSortOptions(IReadOnlyList<PageSortOption> newSortOptions)
        {
            var oldSortOption = sortOption;
            if (!UpdateList(m_SupportedSortOptions, newSortOptions))
                return ChangedTypes.None;
            if (oldSortOption != sortOption)
            {
                // Since selected sort option is index based, changing the list of options could've changed the value
                // so we attempt to restore the old selection if possible here
                UpdateSortOption(oldSortOption);
            }
            return oldSortOption != sortOption ? ChangedTypes.SupportedSortOptions | ChangedTypes.SortOption : ChangedTypes.SupportedSortOptions;
        }

        [SerializeField]
        private int m_SortOptionIndex;
        public PageSortOption sortOption => m_SortOptionIndex >= 0 && m_SortOptionIndex < supportedSortOptions?.Count ? supportedSortOptions[m_SortOptionIndex] : default;

        public ChangedTypes UpdateSortOption(PageSortOption newSortOption)
        {
            var oldSortOption = sortOption;
            m_SortOptionIndex =m_SupportedSortOptions?.Count > 0 ? Math.Max(0, m_SupportedSortOptions.IndexOf(newSortOption)) : 0;
            return oldSortOption != sortOption ?  ChangedTypes.SortOption : ChangedTypes.None;
        }

        public ChangedTypes ResetSortOptionToDefault()
        {
            var oldSortOption = sortOption;
            m_SortOptionIndex = 0;
            return oldSortOption != sortOption ?  ChangedTypes.SortOption : ChangedTypes.None;
        }

        // We don't count sorting options here because there's always a default sorting option even no sorting options are available
        public bool anySupportedFilters => supportedStatuses.Count > 0 || supportedCategories.Count > 0 || supportedLabels.Count > 0 || supportedPackageUniqueIds.Count > 0;
        public virtual bool isFilterSet => status != PageFilterStatus.None || categories.Count > 0 || labels.Count > 0 || packageUniqueIds.Count > 0;

        private static bool UpdateList<T>(List<T> originalList, IReadOnlyList<T> newList)
        {
            newList ??= Array.Empty<T>();
            if (newList.IsSequenceEqual(originalList))
                return false;
            originalList.Clear();
            originalList.AddRange(newList);
            return true;
        }

        public PageFilters() : this(null) {}

        public PageFilters(IPageFilters other)
        {
            m_SupportedStatuses = new List<PageFilterStatus>(other?.supportedStatuses ?? Array.Empty<PageFilterStatus>());
            m_Status = other?.status ?? PageFilterStatus.None;

            m_SupportedSortOptions = new List<PageSortOption>(other?.supportedSortOptions ?? Array.Empty<PageSortOption>());
            if (other != null)
                UpdateSortOption(other.sortOption);
            else
                m_SortOptionIndex = 0;

            m_Categories = new StringFilters(other?.supportedCategories, other?.categories);
            m_Labels = new StringFilters(other?.supportedLabels, other?.labels);
            m_PackageUniqueIds = new StringFilters(other?.supportedPackageUniqueIds, other?.packageUniqueIds);
        }

        public ChangedTypes Clear()
        {
            var result = ChangedTypes.None;
            result |= UpdateStatus(PageFilterStatus.None);
            result |= UpdateCategories(Array.Empty<string>());
            result |= UpdateLabels(Array.Empty<string>());
            result |= UpdatePackages(Array.Empty<string>());
            return result;
        }

        public bool Equals(IPageFilters other)
        {
            if (ReferenceEquals(this, other))
                return true;
            return other != null &&
                status == other.status &&
                sortOption == other.sortOption &&
                categories.IsSequenceEqual(other.categories) &&
                labels.IsSequenceEqual(other.labels) &&
                packageUniqueIds.IsSequenceEqual(other.packageUniqueIds);
        }
    }
}
