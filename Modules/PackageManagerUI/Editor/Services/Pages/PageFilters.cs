// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class PageFilters : IEquatable<PageFilters>
    {
        public enum Status
        {
            None,
            Unlabeled,
            Downloaded,
            Imported,
            UpdateAvailable,
            Hidden,
            Deprecated,
            SubscriptionBased,
        }

        public Status status;
        public PageSortOption sortOption;

        private List<string> m_Categories = new();
        private List<string> m_Labels = new();

        public List<string> categories
        {
            get => m_Categories;
            set => m_Categories = value ?? new List<string>();
        }

        public List<string> labels
        {
            get => m_Labels;
            set => m_Labels = value ?? new List<string>();
        }


        public virtual bool isFilterSet => status != Status.None || categories.Any() || labels.Any();

        public PageFilters Clone()
        {
            return (PageFilters)MemberwiseClone();
        }

        public bool Equals(PageFilters other)
        {
            return other != null &&
                status == other.status &&
                sortOption == other.sortOption &&
                categories.Count == other.categories.Count && categories.SequenceEqual(other.categories) &&
                labels.Count == other.labels.Count && labels.SequenceEqual(other.labels);
        }
    }

    internal static class PageFiltersExtension
    {
        public static string GetDisplayName(this PageFilters.Status value)
        {
            return value switch
            {
                PageFilters.Status.Unlabeled => L10n.Tr("Unlabeled"),
                PageFilters.Status.Downloaded => L10n.Tr("Downloaded"),
                PageFilters.Status.Imported => L10n.Tr("Imported"),
                PageFilters.Status.UpdateAvailable => L10n.Tr("Update available"),
                PageFilters.Status.Hidden => L10n.Tr("Hidden"),
                PageFilters.Status.Deprecated => L10n.Tr("Deprecated"),
                PageFilters.Status.SubscriptionBased => L10n.Tr("Subscription based"),
                PageFilters.Status.None => string.Empty,
                _ => string.Empty
            };
        }
    }
}
