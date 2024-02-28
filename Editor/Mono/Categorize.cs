// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Categorization;
using Unity.Collections;

namespace UnityEditor.Categorization
{
    /* Sorting API that rely on DisplayCategory and DisplaySubCategory */

    internal interface ICategorizable
    {
        Type type { get; }
    }
    
    interface IOrdering
    {
        string name { get; }
        int order { get; }
    }

    //Need to be a class to prevent loop definition with leaf that can cause issue when loading type through reflection
    internal class Category<T> : IEnumerable<T>, IOrdering
        where T : ICategorizable, IOrdering
    {
        public List<T> content { get; private set; }
        public string name { get; private set; }
        public int order { get; private set; }
        public Type type => content.Count > 0 ? content[0].type : null;
        public T this[int i] => content[i];
        public int count => content.Count;

        public Category(string name, int order)
        {
            content = new();
            this.name = name;
            this.order = order;
        }

        public void Add(T newElement, IComparer<T> comparer)
            => content.AddSorted(newElement, comparer);

        public IEnumerator<T> GetEnumerator() => content.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => content.GetEnumerator();
    }

    internal struct LeafElement<T> : IOrdering, ICategorizable
        where T : ICategorizable
    {
        public T data { get; private set; }
        public static implicit operator T(LeafElement<T> leaftElement)
            => leaftElement.data;

        public string name { get; private set; }
        public int order { get; private set; }
        public Category<LeafElement<T>> parent { get; private set; }
        public Type type => typeof(T);

        public LeafElement(T data, string name, int order, Category<LeafElement<T>> parent)
        {
            this.data = data;
            this.name = name;
            this.order = order;
            this.parent = parent;
        }
    }

    internal static class CategorizeHelper
    {
        struct OrderingComparer<T> : IComparer<T>
            where T : IOrdering
        {
            public int Compare(T a, T b)
            {
                var order = a.order.CompareTo(b.order);
                if (order != 0)
                    return order;
                return a.name.CompareTo(b.name);
            }
        }

        internal static List<Category<LeafElement<T>>> SortByCategory<T>(this List<T> list)
            where T : ICategorizable
        {
            var categories = new Dictionary<string, Category<LeafElement<T>>>();
            var result = new List<Category<LeafElement<T>>>();
            var comparerLeaf = new OrderingComparer<LeafElement<T>>();
            var comparerCategory = new OrderingComparer<Category<LeafElement<T>>>();

            foreach(var entry in list)
            {
                var type = entry.type;
                CategoryInfoAttribute categoryInfo = type.GetCustomAttribute<CategoryInfoAttribute>();
                ElementInfoAttribute displayInfo = type.GetCustomAttribute<ElementInfoAttribute>();
                int categoryOrder = categoryInfo?.Order ?? int.MaxValue;
                int inCategoryOrder = displayInfo?.Order ?? int.MaxValue;
                string categoryName = categoryInfo?.Name
                    // Keep compatibility with previous used attribute for 23.3LTS
                    ?? type.GetCustomAttribute<System.ComponentModel.CategoryAttribute>()?.Category;
                string name = displayInfo?.Name ?? ObjectNames.NicifyVariableName(type.Name);
                categoryName ??= name;

                Category<LeafElement<T>> category;
                if (!categories.TryGetValue(categoryName, out category))
                {
                    category = new Category<LeafElement<T>>(categoryName, categoryOrder);
                    categories[categoryName] = category;
                    result.AddSorted(category, comparerCategory);
                }

                category.Add(new LeafElement<T>(entry, name, inCategoryOrder, category), comparerLeaf);
            }

            return result;
        }
    }
}
