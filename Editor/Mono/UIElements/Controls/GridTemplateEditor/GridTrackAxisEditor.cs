// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    // CSS Grid. How a track row repeats. None = a plain single track; the others
    // wrap the row's sizing as repeat(auto-fill|auto-fit|<count>, …).
    internal enum GridTemplateRepeatMode { None, AutoFill, AutoFit, Count }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class GridTrackAxisEditor : VisualElement
    {
        class Entry
        {
            public GridTrackSize track = GridTrackSize.Fraction(1);
            public GridTemplateRepeatMode mode = GridTemplateRepeatMode.None;
            public int count = 2;
        }

        readonly List<Entry> m_Entries = new();
        readonly ListView m_ListView;
        VisualElement m_AffordanceSlot;
        Label m_Title;
        IntegerField m_Count;
        bool m_Notify = true;
        // True when the property is unset: one default row stays visible for editing but the emitted value
        // is empty (so the inline style is cleared). Any edit/add promotes it to a real track.
        bool m_Placeholder;

        public event Action<List<GridTrackSize>> changed;

        // Slot at the left of the header where the owner mounts this axis' property affordance indicator.
        // Public (on a VisibleToOtherModules internal class) so the authoring/builder wrappers can reach it.
        public VisualElement affordanceSlot => m_AffordanceSlot;

        // The header title (e.g. "Template Columns"); settable so one control can serve every axis field.
        public string title { get => m_Title.text; set => m_Title.text = value; }

        // When false, rows omit the repeat selector (implicit tracks can't use repeat() per the spec). Set
        // before rows are built (rows are created lazily on first bind, after UXML attributes are applied).
        public bool allowRepeat { get; set; } = true;

        public GridTrackAxisEditor(string title)
        {
            AddToClassList(ussClassName);

            // Header: [affordance slot][title]. The slot is where the owner mounts the property's
            // override/affordance indicator.
            var header = new VisualElement();
            header.AddToClassList(headerUssClassName);
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 2;

            m_AffordanceSlot = new VisualElement();
            m_AffordanceSlot.AddToClassList(affordanceSlotUssClassName);
            m_AffordanceSlot.style.flexShrink = 0;
            header.Add(m_AffordanceSlot);

            m_Title = new Label(title);
            m_Title.AddToClassList(titleUssClassName);
            m_Title.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_Title.style.flexGrow = 1;
            header.Add(m_Title);

            // Track count: type a number to create that many rows at once. 0 clears the property but keeps
            // one row visible for editing.
            m_Count = new IntegerField { isDelayed = true };
            m_Count.AddToClassList(countUssClassName);
            m_Count.style.width = 44;
            m_Count.style.flexShrink = 0;
            m_Count.tooltip = "Number of tracks. 0 clears the property (one row stays for editing).";
            m_Count.RegisterValueChangedCallback(evt => SetCount(evt.newValue));
            header.Add(m_Count);

            Add(header);

            m_ListView = new ListView
            {
                itemsSource = m_Entries,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                makeItem = () => new Row(this),
                bindItem = (el, i) => ((Row)el).Bind(m_Entries[i], i),
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showAddRemoveFooter = true,
                allowAdd = true,
                allowRemove = true,
                showBoundCollectionSize = false,
                showFoldoutHeader = false,
                selectionType = SelectionType.Single,
            };
            m_ListView.AddToClassList(listUssClassName);
            // Grows to its content up to this height, then scrolls internally; the inspector scrolls overall.
            m_ListView.style.maxHeight = 200;
            m_ListView.onAdd += _ => AddDefaultTrack();
            m_ListView.onRemove += _ =>
            {
                if (m_Placeholder)
                    return; // already unset; nothing to remove
                int idx = m_ListView.selectedIndex >= 0 ? m_ListView.selectedIndex : m_Entries.Count - 1;
                if (idx < 0 || idx >= m_Entries.Count)
                    return;
                m_Entries.RemoveAt(idx);
                if (m_Entries.Count == 0)
                {
                    m_Placeholder = true; // keep one row visible; the value clears
                    m_Entries.Add(new Entry());
                }
                RefreshView();
                Notify();
            };
            m_ListView.itemIndexChanged += (_, _) => Notify(); // drag-reorder mutates m_Entries in place
            Add(m_ListView);

            // The list may be populated (SetTracks) before it is attached; rebuild on attach so it reflects
            // the current entries instead of showing the "List is empty" placeholder.
            RegisterCallback<AttachToPanelEvent>(_ => RefreshView());

            // Start unset: one editable row is shown but the value is empty until the user edits/adds.
            m_Placeholder = true;
            m_Entries.Add(new Entry());
            UpdateCountField();
        }

        public List<GridTrackSize> GetTracks() => ToList();

        // Test-friendly mutators (mirror the +/- footer and the per-row repeat selector), so add/remove
        // logic can be exercised without simulating clicks in a panel.
        internal int entryCount => m_Entries.Count;

        // The footer + button. From unset, the first add yields two tracks (the placeholder row becomes
        // the first); a single-track grid is rarely the goal.
        internal void AddDefaultTrack()
        {
            if (m_Placeholder)
                m_Placeholder = false;
            m_Entries.Add(new Entry());
            RefreshView();
            Notify();
        }

        internal void AddTrack(GridTrackSize track)
        {
            ClearPlaceholder();
            m_Entries.Add(new Entry { track = track, mode = GridTemplateRepeatMode.None });
            RefreshView();
            Notify();
        }

        internal void AddRepeat(GridTemplateRepeatMode mode, int count, GridTrackSize pattern)
        {
            ClearPlaceholder();
            m_Entries.Add(new Entry { track = pattern, mode = mode, count = count });
            RefreshView();
            Notify();
        }

        // Set the number of tracks in one step (the count field). 0 clears the property but keeps one row.
        internal void SetCount(int n)
        {
            if (n < 0) n = 0;
            if (n == 0)
            {
                m_Placeholder = true;
                m_Entries.Clear();
                m_Entries.Add(new Entry());
            }
            else
            {
                m_Placeholder = false;
                while (m_Entries.Count > n) m_Entries.RemoveAt(m_Entries.Count - 1);
                while (m_Entries.Count < n) m_Entries.Add(new Entry());
            }
            RefreshView();
            Notify();
        }

        internal void RemoveEntryAt(int index)
        {
            if (index < 0 || index >= m_Entries.Count)
                return;
            m_Entries.RemoveAt(index);
            RefreshView();
            Notify();
        }

        public void SetTracks(List<GridTrackSize> tracks)
        {
            m_Notify = false;
            try
            {
                if (tracks == null || tracks.Count == 0)
                {
                    // Unset: keep a single editable row instead of the "List is empty" state.
                    m_Placeholder = true;
                    m_Entries.Clear();
                    m_Entries.Add(new Entry());
                }
                else
                {
                    m_Placeholder = false;
                    Reconstruct(tracks);
                }
                RefreshView();
            }
            finally
            {
                m_Notify = true;
            }
        }

        void RefreshView()
        {
            // Rebuild (not RefreshItems) so the empty-state placeholder is re-evaluated against the current
            // entry count. Only valid once attached; on attach RefreshView runs again to reflect the data.
            if (m_ListView.panel != null)
                m_ListView.Rebuild();
            UpdateCountField();
        }

        void UpdateCountField() => m_Count?.SetValueWithoutNotify(m_Placeholder ? 0 : m_Entries.Count);

        // The single placeholder row becomes real content: drop it so the added track(s) aren't doubled.
        void ClearPlaceholder()
        {
            if (!m_Placeholder) return;
            m_Placeholder = false;
            m_Entries.Clear();
        }

        // A row edit turns the unset placeholder into a real single-track value.
        void PromotePlaceholder()
        {
            if (!m_Placeholder) return;
            m_Placeholder = false;
            UpdateCountField();
        }

        List<GridTrackSize> ToList()
        {
            var list = new List<GridTrackSize>();
            if (m_Placeholder)
                return list; // unset -> empty value, clears the inline style
            foreach (var e in m_Entries)
            {
                switch (e.mode)
                {
                    case GridTemplateRepeatMode.AutoFill:
                        list.Add(GridTrackSize.RepeatAutoFill(e.track));
                        break;
                    case GridTemplateRepeatMode.AutoFit:
                        list.Add(GridTrackSize.RepeatAutoFit(e.track));
                        break;
                    case GridTemplateRepeatMode.Count:
                        for (int i = 0; i < Math.Max(1, e.count); ++i)
                            list.Add(e.track);
                        break;
                    default:
                        list.Add(e.track);
                        break;
                }
            }
            return list;
        }

        void Reconstruct(List<GridTrackSize> tracks)
        {
            m_Entries.Clear();
            if (tracks == null)
                return;

            // Each track is its own row; auto-fill / auto-fit map to a single repeat row. Identical
            // consecutive tracks are NOT auto-merged into a repeat(count) row (that is created explicitly
            // via a row's repeat selector), so separately added identical tracks stay individually editable.
            foreach (var t in tracks)
            {
                if (t.isAutoFill)
                    m_Entries.Add(new Entry { mode = GridTemplateRepeatMode.AutoFill, track = PatternFromRepeat(t) });
                else if (t.isAutoFit)
                    m_Entries.Add(new Entry { mode = GridTemplateRepeatMode.AutoFit, track = PatternFromRepeat(t) });
                else
                    m_Entries.Add(new Entry { mode = GridTemplateRepeatMode.None, track = t });
            }
        }

        // A repeat(auto-fill|auto-fit, …) pattern is stored in the track's own min/max fields.
        static GridTrackSize PatternFromRepeat(GridTrackSize t)
        {
            bool minmax = t.minValue != t.maxValue || t.minUnit != t.maxUnit;
            if (minmax)
                return GridTrackSize.Minmax(FromUnit(t.minValue, t.minUnit), FromUnit(t.maxValue, t.maxUnit));
            return FromUnit(t.maxValue, t.maxUnit);
        }

        static GridTrackSize FromUnit(float v, GridTrackSizeUnit u) => u switch
        {
            GridTrackSizeUnit.Pixel => GridTrackSize.Pixels(v),
            GridTrackSizeUnit.Percent => GridTrackSize.Percent(v),
            GridTrackSizeUnit.Fraction => GridTrackSize.Fraction(v),
            GridTrackSizeUnit.MinContent => GridTrackSize.MinContent(),
            GridTrackSizeUnit.MaxContent => GridTrackSize.MaxContent(),
            _ => GridTrackSize.Auto()
        };

        void Notify()
        {
            if (m_Notify)
                changed?.Invoke(ToList());
        }

        // One ListView row: [ sizing field ][ repeat selector ][ count ]. The count only shows for "repeat".
        class Row : VisualElement
        {
            readonly GridTrackAxisEditor m_Owner;
            readonly GridTrackSizeField m_Size;
            readonly GridRepeatField m_Repeat;
            int m_Index = -1;
            bool m_Binding;

            public Row(GridTrackAxisEditor owner)
            {
                m_Owner = owner;
                style.flexDirection = FlexDirection.Row;
                style.alignItems = Align.FlexStart; // the sizing field grows taller for minmax/fit-content

                m_Size = new GridTrackSizeField();
                m_Size.style.flexGrow = 1;
                m_Size.changed += t =>
                {
                    if (m_Binding || m_Index < 0 || m_Index >= m_Owner.m_Entries.Count) return;
                    m_Owner.m_Entries[m_Index].track = t;
                    m_Owner.PromotePlaceholder();
                    m_Owner.Notify();
                };
                Add(m_Size);

                // Implicit tracks (grid-auto-columns/rows) can't use repeat() per the spec, so the repeat
                // selector is omitted when the axis disallows it.
                if (m_Owner.allowRepeat)
                {
                    m_Repeat = new GridRepeatField();
                    m_Repeat.style.width = 116;
                    m_Repeat.style.flexShrink = 0;
                    m_Repeat.style.marginLeft = 4;
                    m_Repeat.changed += () =>
                    {
                        if (m_Binding || m_Index < 0 || m_Index >= m_Owner.m_Entries.Count) return;
                        var entry = m_Owner.m_Entries[m_Index];
                        entry.mode = m_Repeat.mode;
                        entry.count = m_Repeat.count;
                        m_Owner.PromotePlaceholder();
                        m_Owner.Notify();
                    };
                    Add(m_Repeat);
                }
            }

            public void Bind(Entry e, int index)
            {
                m_Binding = true;
                try
                {
                    m_Index = index;
                    m_Size.SetValueWithoutNotify(e.track);
                    m_Repeat?.SetValueWithoutNotify(e.mode, e.count);
                }
                finally
                {
                    m_Binding = false;
                }
            }
        }

        public static readonly string ussClassName = "grid-track-axis-editor";
        public static readonly string headerUssClassName = ussClassName + "__header";
        public static readonly string affordanceSlotUssClassName = ussClassName + "__affordance-slot";
        public static readonly string titleUssClassName = ussClassName + "__title";
        public static readonly string countUssClassName = ussClassName + "__count";
        public static readonly string listUssClassName = ussClassName + "__list";
    }
}
