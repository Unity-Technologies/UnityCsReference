// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.GraphToolkit.ItemLibrary.Editor
{
    /// <summary>
    /// View model for a Category in the library Tree View.
    /// </summary>
    /// <remarks>Basic implementation of <see cref="ICategoryView"/>.</remarks>
    class CategoryView : ICategoryView
    {
        /// <summary>
        /// Name of the Item.
        /// </summary>
        public virtual string Name { get; }

        /// <summary>
        /// Parent of this item in the hierarchy.
        /// </summary>
        public virtual ICategoryView Parent { get; }

        /// <summary>
        /// Custom name used to generate USS styles when creating UI for this item.
        /// </summary>
        public virtual string StyleName { get; set; }

        /// <summary>
        /// Custom path for the item's icon. If set, it takes precedence over the style defined by <see cref="StyleName"/>.
        /// </summary>
        public virtual string IconPath { get; set; }

        /// <summary>
        /// Depth of this item in the hierarchy.
        /// </summary>
        public int Depth
        {
            get
            {
                if (m_Depth == -1)
                {
                    m_Depth = this.GetDepth();
                }
                return m_Depth;
            }
        }

        /// <summary>
        /// Path in the hierarchy of items.
        /// </summary>
        /// <returns>The path.</returns>
        public virtual string GetPath() => m_Path ??= this.GetPathFromParent();

        /// <summary>
        /// Help content to display about this item.
        /// </summary>
        public virtual string Help { get; set; }

        /// <summary>
        /// Categories to display as children of this one.
        /// </summary>
        public IReadOnlyList<ICategoryView> SubCategories => m_SubCategories;

        /// <summary>
        /// Items to display as children of this view.
        /// </summary>
        public IReadOnlyList<IItemView> Items => m_Items;

        int m_Depth = -1;

        string m_Path;

        List<ICategoryView> m_SubCategories;

        List<IItemView> m_Items;

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryView"/> class.
        /// </summary>
        /// <param name="name">Name of the category.</param>
        /// <param name="parent">Category under which to display this category. Can be <c>null</c>.</param>
        public CategoryView(string name, ICategoryView parent = null)
        {
            Parent = parent;
            Name = name;

            m_SubCategories = new List<ICategoryView>();
            m_Items = new List<IItemView>();
        }

        /// <summary>
        /// Remove every non-category item under this category.
        /// </summary>
        public void ClearItems()
        {
            m_Items.Clear();
        }

        /// <summary>
        /// Add a <see cref="IItemView"/> as a child of this category.
        /// </summary>
        /// <param name="item">The item to add as a child.</param>
        public void AddItem(IItemView item)
        {
            m_Items.Add(item);
        }

        /// <summary>
        /// Add a <see cref="ICategoryView"/> as a child of this category.
        /// </summary>
        /// <param name="category">The category to add as a child.</param>
        public void AddSubCategory(ICategoryView category)
        {
            m_SubCategories.Add(category);
        }

        /// <summary>
        /// Remove every subcategory under this category.
        /// </summary>
        public void ClearSubCategories()
        {
            m_SubCategories.Clear();
        }

        /// <summary>
        /// Creates a <see cref="CategoryView"/> populated with view models for several <see cref="ItemLibraryItem"/>.
        /// </summary>
        /// <param name="items">The list of <see cref="ItemLibraryItem"/> to build the view model from.</param>
        /// <param name="viewMode">If set to <see cref="ResultsViewMode.Hierarchy"/>, builds category view models following items <see cref="ItemLibraryItem.CategoryPath"/>.
        ///     Otherwise, items are displayed one by one at the same level.</param>
        /// <param name="categoryPathStyleNames">Style names to apply to categories</param>
        /// <returns>A root <see cref="CategoryView"/> with view models representing each library items.</returns>
        public static CategoryView BuildViewModels(IEnumerable<ItemLibraryItem> items,
            ResultsViewMode viewMode, IReadOnlyDictionary<string, string> categoryPathStyleNames = null)
        {
            var rootCategory = new CategoryView("Root");
            foreach (var item in items)
            {
                ICategoryView parentCategory = null;
                if (viewMode == ResultsViewMode.Hierarchy && !string.IsNullOrEmpty(item.CategoryPath))
                    parentCategory = RetrieveOrCreatePath(item, rootCategory, categoryPathStyleNames);

                var itemView = new ItemView(parentCategory, item);

                if (parentCategory == null)
                    rootCategory.AddItem(itemView);
                else
                    parentCategory.AddItem(itemView);
            }

            return rootCategory;
        }

        /// <summary>
        /// Check if this category is part of an other <see cref="ICategoryView"/>.
        /// </summary>
        /// <param name="category">The other <see cref="ICategoryView"/>.</param>
        /// <returns>True if it is part of the other <see cref="ICategoryView"/>, false otherwise.</returns>
        public bool IsInCategory(ICategoryView category)
        {
            var parent = Parent;
            while (parent != null)
            {
                if (parent == category)
                    return true;
                parent = parent.Parent;
            }

            return false;
        }

        static ICategoryView RetrieveOrCreatePath(ItemLibraryItem item,
            ICategoryView rootCategory, IReadOnlyDictionary<string, string> categoryPathStyleNames)
        {
            var pathParts = item.GetParentCategories();
            if (pathParts.Length == 0)
                return null;

            var potentialCategories = rootCategory.SubCategories;
            ICategoryView parentCategory = null;
            var i = 0;

            for (; i < pathParts.Length && potentialCategories.Count > 0; ++i)
            {
                ICategoryView foundCat = null;
                for (int j = 0; j < potentialCategories.Count; ++j)
                {
                    if (potentialCategories[j].Name == pathParts[i])
                    {
                        foundCat = potentialCategories[j];
                        break;
                    }
                }

                if (foundCat == null)
                    break;

                parentCategory = foundCat;
                potentialCategories = foundCat.SubCategories;
            }

            for (; i < pathParts.Length; i++)
            {
                var newCategory = new CategoryView(pathParts[i], parentCategory);
                string styleName = null;
                if (categoryPathStyleNames?.TryGetValue(newCategory.GetPathFromParent(), out styleName) == true)
                    newCategory.StyleName = styleName;
                parentCategory?.AddSubCategory(newCategory);
                parentCategory = newCategory;
                if (i == 0)
                    rootCategory.AddSubCategory(parentCategory);
            }
            return parentCategory;
        }
    }
}
