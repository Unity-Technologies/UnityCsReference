// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// Default implementation for <see cref="IItemLibraryAdapter"/>.
    /// </summary>
    class ItemLibraryAdapter : IItemLibraryAdapter
    {
        /// <summary>
        /// The tool name that will be given by default if not specified.
        /// </summary>
        public static string DefaultToolName => "UnknownItemLibraryTool";

        /// <summary>
        /// USS Class used on titles in the detail panel.
        /// </summary>
        public const string DetailsTitleClassName = "unity-label__item-details-title";

        /// <summary>
        /// USS Class used on sub-titles in the detail panel.
        /// </summary>
        public const string DetailsSubTitleClassName = "unity-label__item-details-subtitle";

        /// <summary>
        /// USS Class used on general text (not titles) in the detail panel.
        /// </summary>
        public const string DetailsTextClassName = "unity-label__item-details-text";

        /// <summary>
        /// Name to display when creating a library.
        /// </summary>
        public virtual string Title { get; }

        /// <summary>
        /// Unique human-readable name for the library created by this adapter.
        /// </summary>
        /// <remarks>Used to separate preferences between Libraries.</remarks>
        public virtual string LibraryName { get; }

        /// <summary>
        /// Comparison used to sort items with no search active (empty query).
        /// </summary>
        public Comparison<ItemLibraryItem> SortComparison { get; set; } = (x, y) => string.CompareOrdinal(x.FullName, y.FullName);

        /// <summary>
        /// If <c>true</c>, the ItemLibrary will have a toggleable Details panel.
        /// </summary>
        public virtual bool HasDetailsPanel => true;

        /// <summary>
        /// If <c>true</c>, enables support for multi-selection in the library.
        /// </summary>
        public virtual bool MultiSelectEnabled => false;

        /// <summary>
        /// Initial width ratio to use when splitting the main view and the details view.
        /// </summary>
        public virtual float InitialSplitterDetailRatio => 1.0f;

        /// <summary>
        /// Associates style names to category paths.
        /// </summary>
        /// <remarks>Allows UI to apply custom styles to certain categories.</remarks>
        public IReadOnlyDictionary<string, string> CategoryPathStyleNames { get; set; }

        /// <summary>
        /// Extra stylesheet(s) to load when displaying the library.
        /// </summary>
        /// <remarks>(FILENAME)_dark.uss and (FILENAME)_light.uss will be loaded as well if existing.</remarks>
        public string CustomStyleSheetPath { get; set; }

        /// <summary>
        /// Default label used to display the details title.
        /// </summary>
        public Label DetailsTitleLabel { get; protected set; }

        /// <summary>
        /// Default label used to display the details text.
        /// </summary>
        public Label DetailsTextLabel { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemLibraryAdapter"/> class.
        /// </summary>
        /// <param name="title">The name to display when creating a library.</param>
        /// <param name="toolName">The name for the tool using this adapter.</param>
        public ItemLibraryAdapter(string title, string toolName = null)
        {
            Title = title;
            LibraryName = string.IsNullOrEmpty(toolName)? DefaultToolName : toolName;
        }

        /// <summary>
        /// Creates a Title for the Details section
        /// </summary>
        /// <returns>A <see cref="Label"/> with uss class for a title in the details panel.</returns>
        protected static Label MakeDetailsTitleLabel(string text = null)
        {
            var titleLabel = new Label(text);
            titleLabel.AddToClassList(DetailsTitleClassName);
            return titleLabel;
        }

        /// <summary>
        /// Creates a sub-title for the Details section
        /// </summary>
        /// <returns>A <see cref="Label"/> with uss class for a sub-title in the details panel.</returns>
        protected static Label MakeDetailsSubTitleLabel(string text = null)
        {
            var titleLabel = new Label(text);
            titleLabel.AddToClassList(DetailsSubTitleClassName);
            return titleLabel;
        }

        /// <summary>
        /// Creates some Text label for the Details section
        /// </summary>
        /// <returns>A <see cref="Label"/> with uss class for a text in the details panel.</returns>
        protected static Label MakeDetailsTextLabel(string text = null)
        {
            var textLabel = new Label(text);
            textLabel.AddToClassList(DetailsTextClassName);
            return textLabel;
        }

        /// <summary>
        /// Called once when the details panel gets initialized during the library view creation (<see cref="ItemLibraryControl_Internal"/>).
        /// </summary>
        /// <param name="detailsPanel">The <see cref="VisualElement"/> used to display the details panel.</param>
        public virtual void InitDetailsPanel(VisualElement detailsPanel)
        {
            DetailsTitleLabel = MakeDetailsTitleLabel();
            detailsPanel.Add(DetailsTitleLabel);

            DetailsTextLabel = MakeDetailsTextLabel();
            if (DetailsTextLabel != null)
            {
                DetailsTextLabel.enableRichText = true;
                detailsPanel.Add(DetailsTextLabel);
            }
        }

        /// <summary>
        /// Callback to use when an item is selected but not yet validated in the library.
        /// </summary>
        /// <param name="items">List of items being selected. Can be empty.</param>
        public virtual void OnSelectionChanged(IEnumerable<ItemLibraryItem> items)
        {
        }

        /// <summary>
        /// Called when the details panel will be redrawn for a <see cref="ItemLibraryItem"/>.
        /// </summary>
        /// <param name="item">The item that will be displayed in the details view.</param>
        public virtual void UpdateDetailsPanel(ItemLibraryItem item)
        {
            if (DetailsTitleLabel != null)
                DetailsTitleLabel.text = item?.Name ?? "";
            if (DetailsTextLabel != null)
                DetailsTextLabel.text = item?.Help ?? "";
        }
    }
}
