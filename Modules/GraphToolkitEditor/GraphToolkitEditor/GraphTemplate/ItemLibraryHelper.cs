// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.GraphToolkit.ItemLibrary.Editor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A helper for displaying the Item Library with the appropriate items.
    /// </summary>
    /// <remarks>
    /// ItemLibraryHelper is a utility class designed for use with a <see cref="RootView"/> that implements <see cref="IHasItemLibrary"/>. It provides essential functionality
    /// for retrieving the correct <see cref="IItemLibraryAdapter"/>, <see cref="IItemDatabaseProvider"/>, and <see cref="ILibraryFilterProvider"/>. This ensures that the item library
    /// is properly configured and operates as expected within the <see cref="RootView"/>. Additionally, it enables item styling by linking the appropriate stylesheet path.
    /// </remarks>
    [UnityRestricted]
    internal class ItemLibraryHelper
    {
        /// <summary>
        /// The graph model to which this helper is associated.
        /// </summary>
        public GraphModel GraphModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemLibraryHelper"/> class.
        /// </summary>
        public ItemLibraryHelper(GraphModel graphModel)
        {
            GraphModel = graphModel;
        }

        static readonly IReadOnlyDictionary<string, string> k_NoCategoryStyle = new Dictionary<string, string>();

        protected IItemDatabaseProvider m_DatabaseProvider;

        /// <summary>
        /// The style names of the category paths in the item library.
        /// </summary>
        public virtual IReadOnlyDictionary<string, string> CategoryPathStyleNames => k_NoCategoryStyle;

        /// <summary>
        /// Extra stylesheet(s) to load when displaying the item library.
        /// </summary>
        /// <remarks>(FILENAME)_dark.uss and (FILENAME)_light.uss will be loaded as well if existing.</remarks>
        public virtual string CustomItemLibraryStylesheetPath => null;

        /// <summary>
        /// Retrieves the <see cref="ILibraryFilterProvider"/> associated with the item library.
        /// </summary>
        /// <returns>The <see cref="ILibraryFilterProvider"/>.</returns>
        [CanBeNull]
        public virtual ILibraryFilterProvider GetLibraryFilterProvider()
        {
            return null;
        }

        /// <summary>
        /// Gets the <see cref="ItemLibraryAdapter"/> used to search for elements.
        /// </summary>
        /// <param name="title">The title to display when searching.</param>
        /// <param name="toolName">The name of the tool requesting the item library, for display purposes.</param>
        /// <param name="contextPortModel">The ports used for the search, if any.</param>
        /// <returns>The <see cref="ItemLibraryAdapter"/> used to search for elements.</returns>
        [CanBeNull]
        public virtual IItemLibraryAdapter GetItemLibraryAdapter(string title, string toolName, IEnumerable<PortModel> contextPortModel = null)
        {
            var adapter = new GraphNodeLibraryAdapter(GraphModel, title, toolName);
            adapter.CategoryPathStyleNames = CategoryPathStyleNames;
            adapter.CustomStyleSheetPath = CustomItemLibraryStylesheetPath;
            return adapter;
        }

        /// <summary>
        /// Retrieves the database provider for the graph associated with this helper.
        /// </summary>
        /// <returns>The <see cref="IItemDatabaseProvider"/>.</returns>
        /// <remarks>
        /// 'GetItemDatabaseProvider' retrieves the <see cref="IItemDatabaseProvider"/> associated with the graph that this helper is linked to.
        /// The database provider is responsible for managing and supplying item data to the item library, which ensures that the correct set of items is available for use.
        /// This method is useful when working with dynamic or configurable item libraries where the item database may vary based on the graph context.
        /// </remarks>
        public virtual IItemDatabaseProvider GetItemDatabaseProvider()
        {
            return m_DatabaseProvider ??= new DefaultDatabaseProvider(GraphModel);
        }
    }
}
