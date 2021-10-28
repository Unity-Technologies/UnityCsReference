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
        internal const string k_UpdateAvailableStatus = "Update available";
        internal const string k_SubscriptionBasedStatus = "Subscription based";

        private string m_SearchText = "";
        private string m_Status = "";
        private string m_OrderBy = "";

        private List<string> m_Categories = new List<string>();
        private List<string> m_Labels = new List<string>();

        public string searchText
        {
            get { return m_SearchText; }
            set { m_SearchText = value ?? string.Empty; }
        }

        public string status
        {
            get { return m_Status; }
            set { m_Status = value ?? string.Empty; }
        }

        public bool downloadedOnly => k_DownloadedStatus.Equals(status, StringComparison.OrdinalIgnoreCase);
        public bool updateAvailableOnly => k_UpdateAvailableStatus.Equals(status, StringComparison.OrdinalIgnoreCase);
        public bool subscriptionBasedOnly => k_SubscriptionBasedStatus.Equals(status, StringComparison.OrdinalIgnoreCase);

        public List<string> categories
        {
            get { return m_Categories; }
            set { if (value == null) m_Categories = new List<string>(); else m_Categories = value; }
        }

        public List<string> labels
        {
            get { return m_Labels; }
            set { if (value == null) m_Labels = new List<string>(); else m_Labels = value; }
        }

        public string orderBy
        {
            get { return m_OrderBy; }
            set { if (value == null) m_OrderBy = ""; else m_OrderBy = value; }
        }

        public bool isReverseOrder;

        public bool isFilterSet => !string.IsNullOrEmpty(searchText) || !string.IsNullOrEmpty(status) || categories.Any() || labels.Any();
        public bool isOrderSet => !string.IsNullOrEmpty(orderBy) || isReverseOrder;

        public PageFilters Clone()
        {
            return (PageFilters)MemberwiseClone();
        }

        public bool Equals(PageFilters other)
        {
            return other != null &&
                searchText == other.searchText &&
                status == other.status &&
                orderBy == other.orderBy &&
                isReverseOrder == other.isReverseOrder &&
                categories.Count == other.categories.Count && categories.SequenceEqual(other.categories) &&
                labels.Count == other.labels.Count && labels.SequenceEqual(other.labels);
        }
    }
}
