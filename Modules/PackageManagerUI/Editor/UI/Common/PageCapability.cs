// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Text;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class PageCapability
    {
        public enum Order
        {
            None,
            Ascending,
            Descending
        }

        [Serializable]
        public class Ordering
        {
            public string displayName;
            public string orderBy;
            public Order order;

            public Ordering()
            {
                displayName = "-";
                orderBy = string.Empty;
                order = Order.Ascending;
            }

            public Ordering(string displayName, string orderBy, Order order = Order.None)
            {
                this.displayName = displayName;
                this.orderBy = orderBy;
                this.order = order;
            }
        }

        [Serializable]
        public class ConditionalOrdering : Ordering
        {
            public Func<bool> condition;

            public ConditionalOrdering(Func<bool> condition, string displayName, string orderBy, Order order = Order.None) : base(displayName, orderBy, order)
            {
                this.condition = condition;
            }
        }

        public bool requireUserLoggedIn;
        public bool requireNetwork;
        public bool supportFilters;
        public Ordering[] orderingValues;
        public ConditionalOrdering[] conditionalOrderingValues;

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"requireUserLoggedIn={requireUserLoggedIn},");
            stringBuilder.Append($"requireNetwork={requireNetwork},");
            stringBuilder.Append($"supportFilters={supportFilters},");
            if (orderingValues != null)
                stringBuilder.Append($"orderingValues=[{string.Join(",", orderingValues.Select(o => $"{o.orderBy} ({o.order})").ToArray())}],");
            if (conditionalOrderingValues != null)
                stringBuilder.Append($"conditionalOrderingValues=[{string.Join(",", conditionalOrderingValues.Select(o => $"{o.condition} {o.orderBy} ({o.order})").ToArray())}],");

            var text = stringBuilder.ToString();
            if (!string.IsNullOrEmpty(text))
                text = text.Remove(text.Length - 1);
            return text;
        }
    }
}
