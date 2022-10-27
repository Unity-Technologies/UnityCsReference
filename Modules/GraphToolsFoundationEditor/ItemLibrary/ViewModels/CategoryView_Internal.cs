// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// View model for a Category in the library Tree View.
    /// </summary>
    /// <remarks>Basic implementation of <see cref="ICategoryView_Internal"/>.</remarks>
    class CategoryView_Internal: ICategoryView_Internal
    {
        /// <summary>
        /// Name of the Item.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Parent of this item in the hierarchy.
        /// </summary>
        public ICategoryView_Internal Parent { get; }

        /// <summary>
        /// Custom name used to generate USS styles when creating UI for this item.
        /// </summary>
        public string StyleName { get; set; }

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
        public string Path => m_Path ??= this.GetPath();

        /// <summary>
        /// Help content to display about this item.
        /// </summary>
        public string Help { get; set; }

        /// <summary>
        /// Categories to display as children of this one.
        /// </summary>
        public IReadOnlyList<ICategoryView_Internal> SubCategories => m_SubCategories;

        /// <summary>
        /// Items to display as children of this view.
        /// </summary>
        public IReadOnlyList<IItemView_Internal> Items => m_Items;

        int m_Depth = -1;

        string m_Path;

        List<ICategoryView_Internal> m_SubCategories;

        List<IItemView_Internal> m_Items;

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryView_Internal"/> class.
        /// </summary>
        /// <param name="name">Name of the category.</param>
        /// <param name="parent">Category under which to display this category. Can be <c>null</c>.</param>
        public CategoryView_Internal(string name, ICategoryView_Internal parent = null)
        {
            Parent = parent;
            Name = name;

            m_SubCategories = new List<ICategoryView_Internal>();
            m_Items = new List<IItemView_Internal>();
        }

        /// <summary>
        /// Remove every non-category item under this category.
        /// </summary>
        public void ClearItems()
        {
            m_Items.Clear();
        }

        /// <summary>
        /// Add a <see cref="IItemView_Internal"/> as a child of this category.
        /// </summary>
        /// <param name="item">The item to add as a child.</param>
        public void AddItem(IItemView_Internal item)
        {
            m_Items.Add(item);
        }

        /// <summary>
        /// Add a <see cref="ICategoryView_Internal"/> as a child of this category.
        /// </summary>
        /// <param name="category">The category to add as a child.</param>
        public void AddSubCategory(ICategoryView_Internal category)
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
        /// Creates a <see cref="CategoryView_Internal"/> populated with view models for several <see cref="ItemLibraryItem"/>.
        /// </summary>
        /// <param name="items">The list of <see cref="ItemLibraryItem"/> to build the view model from.</param>
        /// <param name="viewMode">If set to <see cref="ResultsViewMode.Hierarchy"/>, builds category view models following items <see cref="ItemLibraryItem.CategoryPath"/>.
        ///     Otherwise, items are displayed one by one at the same level.</param>
        /// <param name="categoryPathStyleNames">Style names to apply to categories</param>
        /// <returns>A root <see cref="CategoryView_Internal"/> with view models representing each library items.</returns>
        public static CategoryView_Internal BuildViewModels(IEnumerable<ItemLibraryItem> items,
            ResultsViewMode viewMode, IReadOnlyDictionary<string, string> categoryPathStyleNames = null)
        {
            var rootCategory = new CategoryView_Internal("Root");
            foreach (var item in items)
            {
                ICategoryView_Internal parentCategory = null;
                if (viewMode == ResultsViewMode.Hierarchy && !string.IsNullOrEmpty(item.CategoryPath))
                    parentCategory = RetrieveOrCreatePath(item, rootCategory, categoryPathStyleNames);

                var itemView = new ItemView_Internal(parentCategory, item);

                if (parentCategory == null)
                    rootCategory.AddItem(itemView);
                else
                    parentCategory.AddItem(itemView);
            }

            return rootCategory;
        }

        static ICategoryView_Internal RetrieveOrCreatePath(ItemLibraryItem item,
            ICategoryView_Internal rootCategory, IReadOnlyDictionary<string, string> categoryPathStyleNames)
        {
            var pathParts = item.GetParentCategories();
            if (pathParts.Length == 0)
                return null;

            var potentialCategories = rootCategory.SubCategories;
            ICategoryView_Internal parentCategory = null;
            var i = 0;

            for (;i < pathParts.Length && potentialCategories.Count > 0; ++i)
            {
                var foundCat = potentialCategories.FirstOrDefault(c => c.Name == pathParts[i]);
                if (foundCat == null)
                    break;
                parentCategory = foundCat;
                potentialCategories = foundCat.SubCategories;
            }

            for (; i < pathParts.Length; i++)
            {
                var newCategory = new CategoryView_Internal(pathParts[i], parentCategory);
                string styleName = null;
                if (categoryPathStyleNames?.TryGetValue(newCategory.Path, out styleName) == true)
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
