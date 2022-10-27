// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// Provides ways to customize the searching interface.
    /// </summary>
    interface IItemLibraryAdapter
    {
        /// <summary>
        /// Name to display when creating a library.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Unique human-readable name for the library created by this adapter.
        /// </summary>
        /// <remarks>Used to separate preferences between Libraries.</remarks>
        string LibraryName { get; }

        /// <summary>
        /// Comparison used to sort items while no search is active (empty query).
        /// </summary>
        Comparison<ItemLibraryItem> SortComparison { get; }

        /// <summary>
        /// If <c>true</c>, the ItemLibrary will have a toggleable Details panel.
        /// </summary>
        bool HasDetailsPanel { get; }

        /// <summary>
        /// If <c>true</c>, enables support for multi-selection in the library.
        /// </summary>
        bool MultiSelectEnabled { get; }

        /// <summary>
        /// Initial width ratio to use when splitting the main view and the details view.
        /// </summary>
        float InitialSplitterDetailRatio { get; }

        /// <summary>
        /// Associates style names to category paths.
        /// </summary>
        /// <remarks>Allows UI to apply custom styles to certain categories.</remarks>
        IReadOnlyDictionary<string, string> CategoryPathStyleNames { get; }

        /// <summary>
        /// Extra stylesheet(s) to load when displaying the library.
        /// </summary>
        /// <remarks>(FILENAME)_dark.uss and (FILENAME)_light.uss will be loaded as well if existing.</remarks>
        string CustomStyleSheetPath { get; }

        /// <summary>
        /// Callback to use when an item is selected but not yet validated in the library.
        /// </summary>
        /// <param name="items">List of items being selected. Can be empty.</param>
        void OnSelectionChanged(IEnumerable<ItemLibraryItem> items);

        /// <summary>
        /// Called when the details panel will be redrawn for a <see cref="ItemLibraryItem"/>.
        /// </summary>
        /// <param name="item">The item that will be displayed in the details view.</param>
        void UpdateDetailsPanel(ItemLibraryItem item);

        /// <summary>
        /// Called once when the details panel gets initialized during the library view creation (<see cref="ItemLibraryControl_Internal"/>).
        /// </summary>
        /// <param name="detailsPanel">The <see cref="VisualElement"/> used to display the details panel.</param>
        void InitDetailsPanel(VisualElement detailsPanel);
    }
}
