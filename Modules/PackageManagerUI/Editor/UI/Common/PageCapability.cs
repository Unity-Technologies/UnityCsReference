// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class PageCapability
    {
        public enum Order
        {
            Ascending,
            Descending
        }

        [Serializable]
        public class Ordering
        {
            public string displayName;
            public string orderBy;
            public Order order;

            public Ordering(string displayName, string orderBy, Order order)
            {
                this.displayName = displayName;
                this.orderBy = orderBy;
                this.order = order;
            }
        }

        public bool requireUserLoggedIn;
        public bool requireNetwork;
        public bool supportFilters;
        public bool supportLocalReordering;
        public Ordering[] orderingValues;
    }
}
