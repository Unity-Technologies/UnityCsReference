// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PageFilters : IEquatable<PageFilters>
    {
        private string m_SearchText = "";
        private List<string> m_statuses = new List<string>();
        private List<string> m_categories = new List<string>();
        private List<string> m_labels = new List<string>();
        private string m_orderBy = "";

        public string searchText
        {
            get { return m_SearchText; }
            set { if (value == null) m_SearchText = ""; else m_SearchText = value; }
        }

        public List<string> statuses
        {
            get { return m_statuses; }
            set { if (value == null) m_statuses = new List<string>(); else m_statuses = value;}
        }

        public List<string> categories
        {
            get { return m_categories; }
            set { if (value == null) m_categories = new List<string>(); else m_categories = value; }
        }

        public List<string> labels
        {
            get { return m_labels; }
            set { if (value == null) m_labels = new List<string>(); else m_labels = value; }
        }

        public string orderBy
        {
            get { return m_orderBy; }
            set { if (value == null) m_orderBy = ""; else m_orderBy = value; }
        }

        public bool isReverseOrder;

        public bool isFilterSet => !string.IsNullOrEmpty(searchText) || statuses.Any() || categories.Any() || labels.Any();
        public bool isOrderSet => !string.IsNullOrEmpty(orderBy) || isReverseOrder;

        public PageFilters Clone()
        {
            return (PageFilters)MemberwiseClone();
        }

        public bool Equals(PageFilters other)
        {
            return other != null &&
                searchText == other.searchText &&
                orderBy == other.orderBy &&
                isReverseOrder == other.isReverseOrder &&
                statuses.Count == other.statuses.Count && statuses.SequenceEqual(other.statuses) &&
                categories.Count == other.categories.Count && categories.SequenceEqual(other.categories) &&
                labels.Count == other.labels.Count && labels.SequenceEqual(other.labels);
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(searchText))
                stringBuilder.Append($"searchText={searchText},");
            if (!string.IsNullOrEmpty(orderBy))
            {
                stringBuilder.Append($"orderBy={orderBy},");
                stringBuilder.Append($"isReverseOrder={isReverseOrder}");
            }

            if (statuses.Any())
                stringBuilder.Append($"statuses=[{string.Join(",", statuses.ToArray())}],");
            if (categories.Any())
                stringBuilder.Append($"categories=[{string.Join(",", categories.ToArray())}],");
            if (labels.Any())
                stringBuilder.Append($"labels=[{string.Join(",", labels.ToArray())}],");

            var text = stringBuilder.ToString();
            if (!string.IsNullOrEmpty(text))
                text = text.Remove(text.Length - 1);
            return text;
        }
    }
}
