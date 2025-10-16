// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor.ContextualMenuItems
{
    /// <summary>
    /// Holds information about a contextual menu item.
    /// </summary>
    readonly struct ContextualMenuItem : IEquatable<ContextualMenuItem>
    {
        /// <summary>
        /// The category of the item within the menu.
        /// </summary>
        public ContextualMenuCategory Category { get; }

        /// <summary>
        /// The name of the item.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The index of the item within its category. If it's -1 or out of bounds, it is placed at the end.
        /// </summary>
        public int IndexInCategory { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextualMenuItem"/> class.
        /// </summary>
        /// <param name="category">The category of the item within the menu.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="indexInCategory">The index of the item within its category. If it's -1 or out of bounds, it is placed at the end.</param>
        public ContextualMenuItem(ContextualMenuCategory category, string name, int indexInCategory = -1)
        {
            Category = category;
            Name = name;
            IndexInCategory = indexInCategory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextualMenuItem"/> class using an existing <see cref="ContextualMenuItem"/>.
        /// </summary>
        /// <param name="menuItem">The existing <see cref="ContextualMenuItem"/>.</param>
        /// <param name="indexInCategory">The index of the item within its category. If it's -1 or out of bounds, it is placed at the end.</param>
        public ContextualMenuItem(ContextualMenuItem menuItem, int indexInCategory = -1)
        {
            Category = menuItem.Category;
            Name = menuItem.Name;
            IndexInCategory = indexInCategory;
        }

        public bool Equals(ContextualMenuItem other)
        {
            return Category == other.Category && Name == other.Name && IndexInCategory == other.IndexInCategory;
        }

        public override bool Equals(object obj)
        {
            return obj is ContextualMenuItem other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)Category, Name, IndexInCategory);
        }
    }
}
