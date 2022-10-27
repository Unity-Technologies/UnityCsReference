// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// View model for ItemLibrary Categories.
    /// </summary>
    interface ICategoryView_Internal : ITreeItemView_Internal
    {
        /// <summary>
        /// Categories to display as children of this one.
        /// </summary>
        public IReadOnlyList<ICategoryView_Internal> SubCategories { get; }

        /// <summary>
        /// Items to display as children of this view.
        /// </summary>
        public IReadOnlyList<IItemView_Internal> Items { get; }

        /// <summary>
        /// Add a <see cref="IItemView_Internal"/> as a child of this category.
        /// </summary>
        /// <param name="item">The item to add as a child.</param>
        public void AddItem(IItemView_Internal item);

        /// <summary>
        /// Remove every non-category item under this category.
        /// </summary>
        public void ClearItems();

        /// <summary>
        /// Add a <see cref="ICategoryView_Internal"/> as a child of this category.
        /// </summary>
        /// <param name="category">The category to add as a child.</param>
        public void AddSubCategory(ICategoryView_Internal category);

        /// <summary>
        /// Remove every subcategory under this category.
        /// </summary>
        public void ClearSubCategories();
    }
}
