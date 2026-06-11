// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// <see cref="MultiColumnTreeView"/> that renders a "totals" cell inside each column header
    /// (directly beneath the column title). Wrapping the per-column <see cref="Column.makeHeader"/>
    /// piggybacks on the column header element, so the totals row inherits column width, the
    /// header's horizontal-scroll translate, and the header band's pinned (non-vertical-scroll)
    /// behaviour for free.
    /// </summary>
    /// <remarks>
    /// Use <see cref="SetTotalCell"/> to write a value into the totals cell of a column (looked
    /// up by <see cref="Column.name"/>). Values are cached and re-applied on header rebuilds
    /// (visibility flips, template refreshes) so they survive transient teardown of the labels.
    /// </remarks>
    internal sealed class MultiColumnTreeViewWithTotal : MultiColumnTreeView
    {
        struct DefaultColumnState
        {
            public Column column;
            public Length width;
            public bool visible;
        }

        struct DefaultSortState
        {
            public string columnName;
            public int columnIndex;
            public SortDirection direction;
        }

        readonly ColumnTotalsBinding m_Totals;
        readonly List<DefaultColumnState> m_DefaultColumns = new();
        readonly List<DefaultSortState> m_DefaultSort = new();

        // Chain through a private ctor so the wrappers are installed on every column BEFORE the
        // base ctor runs. MultiColumnTreeView's base ctor sets `this.columns = columns`, which
        // synchronously creates the controller, the header, and the per-column
        // MultiColumnHeaderColumn — each of which invokes Column.makeHeader on construction.
        // If we installed the wrappers after base(columns), the initial header layout would use
        // the original (unwrapped) makeHeader and miss the totals row until the next rebuild.
        public MultiColumnTreeViewWithTotal(Columns columns)
            : this(columns, new ColumnTotalsBinding(columns))
        {
        }

        MultiColumnTreeViewWithTotal(Columns columns, ColumnTotalsBinding totals)
            : base(columns)
        {
            m_Totals = totals;
            // Snapshot the layout as supplied by the caller — width, visibility, display order, and
            // any sort descriptions present at construction. This becomes the target state for
            // "Reset Layout" in the column header context menu. Captured before view-data
            // restoration runs so user-saved tweaks don't pollute the "default" baseline.
            foreach (var col in columns.displayList)
                m_DefaultColumns.Add(new DefaultColumnState { column = col, width = col.width, visible = col.visible });
            foreach (var desc in sortColumnDescriptions)
                m_DefaultSort.Add(new DefaultSortState { columnName = desc.columnName, columnIndex = desc.columnIndex, direction = desc.direction });

            headerContextMenuPopulateEvent += OnHeaderContextMenuPopulate;
        }

        public void SetTotalCell(string columnName, string text, string tooltip = null)
            => m_Totals.SetCell(columnName, text, tooltip);

        public void ClearAllTotals() => m_Totals.ClearAll();

        void OnHeaderContextMenuPopulate(ContextualMenuPopulateEvent evt, Column _)
        {
            // Insert the "Reset Columns" action at index 1, right after the built-in "Resize To Fit" action.
            evt.menu.InsertAction(1, L10n.Tr("Reset Columns"), _ => ResetColumnsToDefault());
        }

        void ResetColumnsToDefault()
        {
            // Restore each column's width, visibility, and display position to the captured
            // snapshot. The reorder is index-by-index: at iteration i, the column that should sit
            // at displayIndex i is moved there from wherever it currently is. Setting `width`
            // resets `desiredWidth` to NaN so the header recomputes the layout on next pass.
            for (var i = 0; i < m_DefaultColumns.Count; i++)
            {
                var state = m_DefaultColumns[i];
                state.column.visible = state.visible;
                state.column.width = state.width;

                var currentIndex = state.column.displayIndex;
                if (currentIndex >= 0 && currentIndex != i)
                    columns.ReorderDisplay(currentIndex, i);
            }

            sortColumnDescriptions.Clear();
            foreach (var s in m_DefaultSort)
            {
                var desc = !string.IsNullOrEmpty(s.columnName)
                    ? new SortColumnDescription(s.columnName, s.direction)
                    : new SortColumnDescription(s.columnIndex, s.direction);
                sortColumnDescriptions.Add(desc);
            }
        }
    }

    /// <summary>
    /// Attaches a totals <see cref="Label"/> to each column header in a multi-column view by
    /// wrapping the host's <see cref="Column.makeHeader"/> / <see cref="Column.bindHeader"/>
    /// callbacks. State for cells is keyed by <see cref="Column.name"/> and cached so values
    /// survive header rebuilds.
    /// </summary>
    sealed class ColumnTotalsBinding
    {
        const string k_TotalsCellName = "uitoolkit-profiler-totals-cell";
        // Styled by UIToolkitProfiler.uss (attached at split-view scope by PanelComponentsPaneController).
        const string k_TotalsCellClass = "uitoolkit-profiler__totals-cell";

        readonly Dictionary<string, Label> m_LabelByColumnName = new(StringComparer.Ordinal);
        readonly Dictionary<string, CellState> m_StateByColumnName = new(StringComparer.Ordinal);

        struct CellState
        {
            public string text;
            public string tooltip;
        }

        public ColumnTotalsBinding(Columns columns)
        {
            // Snapshot install — columns appended to the collection after construction won't get a
            // totals cell. Callers build the full column set before constructing the view, so this
            // is safe in current usage.
            foreach (var col in columns)
                InstallColumn(col);
        }

        void InstallColumn(Column col)
        {
            var originalMake = col.makeHeader;
            var originalBind = col.bindHeader;
            var originalUnbind = col.unbindHeader;
            var originalDestroy = col.destroyHeader;

            col.makeHeader = () =>
            {
                // The framework feeds whatever we return here into MultiColumnHeaderColumn.content
                // and tags it with the __content class. Wrapping the host's original element in a
                // column-direction stack puts the totals label below the title row while keeping
                // the inner element addressable by the original bindHeader's name lookups.
                var inner = originalMake != null
                    ? originalMake()
                    : UIToolkitProfilerToolbarHelpers.CreateDefaultColumnHeaderContent();

                // Initial "0" so the totals row is always visible even for columns whose total is
                // never set (or before the first ReloadData) — bindHeader re-stamps the cached
                // value afterwards if SetCell has been called.
                var totalsLabel = new Label { name = k_TotalsCellName, text = "0" };
                totalsLabel.AddToClassList(k_TotalsCellClass);
                totalsLabel.pickingMode = PickingMode.Ignore;

                var stack = new VisualElement { pickingMode = PickingMode.Ignore };
                stack.style.flexDirection = FlexDirection.Column;
                stack.style.flexGrow = 1;
                stack.style.flexShrink = 1;
                stack.Add(inner);
                stack.Add(totalsLabel);

                m_LabelByColumnName[col.name] = totalsLabel;
                return stack;
            };

            col.bindHeader = ve =>
            {
                if (ve.childCount == 0)
                    return;
                // ve is our outer stack. Forward the bind to the inner element so the framework's
                // class-list / icon / title plumbing (and any host-supplied tooltip wiring) lands
                // on the same element it would have without our wrap.
                var inner = ve[0];
                if (originalBind != null)
                    originalBind(inner);
                else
                    UIToolkitProfilerToolbarHelpers.BindColumnHeaderWithTooltip(inner, col, null);

                // bindHeader can run after a SetCell for a column whose header is being rebuilt
                // (visibility flip, template refresh) — re-stamp the cached value.
                ApplyState(col.name);
            };

            col.unbindHeader = ve =>
            {
                if (originalUnbind != null && ve.childCount > 0)
                    originalUnbind(ve[0]);
            };

            col.destroyHeader = ve =>
            {
                if (originalDestroy != null && ve.childCount > 0)
                    originalDestroy(ve[0]);
                // Only untrack if the dict still points at the label inside the destroyed stack.
                // MultiColumnHeaderColumn.content's setter evaluates the new makeHeader (which
                // registers the NEW label) BEFORE destroying the old content. Removing
                // unconditionally would drop the just-registered NEW label on every template
                // refresh.
                if (m_LabelByColumnName.TryGetValue(col.name, out var tracked) && ve.Q<Label>(k_TotalsCellName) == tracked)
                    m_LabelByColumnName.Remove(col.name);
            };
        }

        public void SetCell(string columnName, string text, string tooltip)
        {
            // Frame scrubbing repeatedly stamps identical totals (e.g. "0" on every empty frame);
            // skip the dictionary write and label/tooltip assignments when nothing changed.
            if (m_StateByColumnName.TryGetValue(columnName, out var existing)
                && existing.text == text && existing.tooltip == tooltip)
                return;
            m_StateByColumnName[columnName] = new CellState { text = text, tooltip = tooltip };
            ApplyState(columnName);
        }

        public void ClearAll()
        {
            m_StateByColumnName.Clear();
            foreach (var label in m_LabelByColumnName.Values)
            {
                label.text = string.Empty;
                label.tooltip = string.Empty;
            }
        }

        void ApplyState(string columnName)
        {
            if (!m_StateByColumnName.TryGetValue(columnName, out var state))
                return;
            if (!m_LabelByColumnName.TryGetValue(columnName, out var label))
                return;
            label.text = state.text ?? string.Empty;
            label.tooltip = state.tooltip ?? string.Empty;
        }
    }
}
