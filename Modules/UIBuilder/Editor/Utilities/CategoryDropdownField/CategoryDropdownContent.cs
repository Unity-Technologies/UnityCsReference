// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.UI.Builder
{
    struct CategoryDropdownContent
    {
        public enum ItemType
        {
            Category,
            Separator,
            Item
        }

        public struct Category
        {
            public string name;
        }

        public struct ValueItem
        {
            public string value;
            public string displayName;
            public string categoryName;
        }

        internal struct Item
        {
            public string value;
            public string displayName;
            public string categoryName;
            public ItemType itemType;
        }

        List<Item> m_Items;

        internal List<Item> Items
        {
            get { return m_Items ??= new List<Item>(); }
        }

        public void AppendCategory(Category category)
        {
            Items.Add(new Item
            {
                itemType = ItemType.Category,
                displayName = category.name,
                categoryName = category.name
            });
        }

        public void AppendValue(ValueItem value)
        {
            Items.Add(new Item
            {
                itemType = ItemType.Item,
                displayName = value.displayName,
                categoryName = value.categoryName,
                value = value.value
            });
        }

        public void AppendSeparator()
        {
            Items.Add(new Item
            {
                itemType = ItemType.Separator
            });
        }

        public void AppendContent(CategoryDropdownContent content)
        {
            foreach (var item in content.Items)
            {
                Items.Add(new Item
                {
                    value = item.value,
                    categoryName = item.categoryName,
                    itemType = item.itemType,
                    displayName = item.displayName
                });
            }
        }
    }
}
