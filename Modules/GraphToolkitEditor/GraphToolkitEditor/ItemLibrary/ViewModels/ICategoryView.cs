// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.GraphToolkit.ItemLibrary.Editor
{
    /// <summary>
    /// View model for ItemLibrary Categories.
    /// </summary>
    interface ICategoryView : ITreeItemView
    {
        /// <summary>
        /// Categories to display as children of this one.
        /// </summary>
        public IReadOnlyList<ICategoryView> SubCategories { get; }

        /// <summary>
        /// Items to display as children of this view.
        /// </summary>
        public IReadOnlyList<IItemView> Items { get; }

        /// <summary>
        /// Add a <see cref="IItemView"/> as a child of this category.
        /// </summary>
        /// <param name="item">The item to add as a child.</param>
        public void AddItem(IItemView item);

        /// <summary>
        /// Remove every non-category item under this category.
        /// </summary>
        public void ClearItems();

        /// <summary>
        /// Add a <see cref="ICategoryView"/> as a child of this category.
        /// </summary>
        /// <param name="category">The category to add as a child.</param>
        public void AddSubCategory(ICategoryView category);

        /// <summary>
        /// Remove every subcategory under this category.
        /// </summary>
        public void ClearSubCategories();

        /// <summary>
        /// Check if this category is part of an other <see cref="ICategoryView"/>.
        /// </summary>
        /// <param name="category">The other <see cref="ICategoryView"/>.</param>
        /// <returns>True if it is part of the other <see cref="ICategoryView"/>, false otherwise.</returns>
        public bool IsInCategory(ICategoryView category);
    }
}
