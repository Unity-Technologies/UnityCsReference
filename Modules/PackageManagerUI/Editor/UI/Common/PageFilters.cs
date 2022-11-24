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
        internal const string k_UnlabeledStatus = "Unlabeled";
        internal const string k_DownloadedStatus = "Downloaded";
        internal const string k_ImportedStatus = "Imported";
        internal const string k_UpdateAvailableStatus = "Update available";
        internal const string k_SubscriptionBasedStatus = "Subscription based";

        private string m_Status = "";
        private string m_OrderBy = "";

        private List<string> m_Categories = new List<string>();
        private List<string> m_Labels = new List<string>();

        public string status
        {
            get => m_Status;
            set => m_Status = value ?? string.Empty;
        }

        public bool downloadedOnly => k_DownloadedStatus.Equals(status, StringComparison.OrdinalIgnoreCase);
        public bool importedOnly => k_ImportedStatus.Equals(status, StringComparison.OrdinalIgnoreCase);
        public bool updateAvailableOnly => k_UpdateAvailableStatus.Equals(status, StringComparison.OrdinalIgnoreCase);
        public bool subscriptionBasedOnly => k_SubscriptionBasedStatus.Equals(status, StringComparison.OrdinalIgnoreCase);

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

        public string orderBy
        {
            get => m_OrderBy;
            set => m_OrderBy = value ?? string.Empty;
        }

        public bool isReverseOrder;

        public virtual bool isFilterSet => !string.IsNullOrEmpty(status) || categories.Any() || labels.Any();

        public PageFilters Clone()
        {
            return (PageFilters)MemberwiseClone();
        }

        public bool Equals(PageFilters other)
        {
            return other != null &&
                status == other.status &&
                orderBy == other.orderBy &&
                isReverseOrder == other.isReverseOrder &&
                categories.Count == other.categories.Count && categories.SequenceEqual(other.categories) &&
                labels.Count == other.labels.Count && labels.SequenceEqual(other.labels);
        }
    }
}
