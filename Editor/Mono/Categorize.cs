// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Categorization;

namespace UnityEditor.Categorization
{
    public interface ICategorizable
    {
        Type type { get; }
    }
    
    public interface IOrdering
    {
        string name { get; }
        int order { get; }
    }

    //Need to be a class to prevent loop definition with leaf that can cause issue when loading type through reflection
    public class Node<T> : IOrdering, ICategorizable
        where T : ICategorizable
    {
        public string name { get; }
        public string description { get; }
        public int order { get; }
        public string helpUrl { get; internal set; }

        public Node<T> parent { get; internal set; }
        public Type type => data != null ? data.type : (children.Count > 0 ? children[0].type : null);

        public T data { get; }
        public List<Node<T>> children { get; }

        public Node(InfoAttribute info, T data = default)
        {
            this.name = info.Name;
            this.order = info.Order;
            this.description = info.Description;
            this.data = data;
            this.children = new ();
        }
    }

    public static class CategorizationExtensions
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

        public static List<Node<T>> SortByCategory<T>(this List<T> list)
            where T : ICategorizable
        {
            var categories = new Dictionary<string, Node<T>>();
            var result = new List<Node<T>>();

            var cmp = new OrderingComparer<Node<T>>();

            foreach (var entry in list)
            {
                var type = entry.type;

                // Fetch attribute data
                var catInfo = type.GetCustomAttribute<CategoryInfoAttribute>() ?? new CategoryInfoAttribute();
                var elemInfo = type.GetCustomAttribute<ElementInfoAttribute>() ?? new ElementInfoAttribute();
                var helpURLAttribute = type.GetCustomAttribute<HelpURLAttribute>();

                // Element info
                elemInfo.Name = ObjectNames.NicifyVariableName(string.IsNullOrEmpty(elemInfo.Name) ? type.Name : elemInfo.Name);
                elemInfo.Description = !string.IsNullOrEmpty(elemInfo.Description)
                    ? elemInfo.Description
                    : type.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;

                // Try to find a proper category
                catInfo.Name = string.IsNullOrEmpty(catInfo.Name)
                    ? (type.GetCustomAttribute<System.ComponentModel.CategoryAttribute>()?.Category ?? elemInfo.Name)
                    : catInfo.Name;

                // Find or create the category node
                if (!categories.TryGetValue(catInfo.Name, out Node<T> categoryNode))
                {
                    catInfo.Name = catInfo.Name;
                    categoryNode = new Node<T>(catInfo)
                    {
                        parent = null,
                        helpUrl = helpURLAttribute?.URL
                    };
                    categories[catInfo.Name] = categoryNode;
                    result.AddSorted(categoryNode, cmp);
                }

                // Create the leaf node
                var leaf = new Node<T>(elemInfo, entry)
                {
                    parent = categoryNode,
                    helpUrl = helpURLAttribute?.URL
                };
                categoryNode.children.AddSorted(leaf, cmp);
            }

            return result;
        }
    }
}
