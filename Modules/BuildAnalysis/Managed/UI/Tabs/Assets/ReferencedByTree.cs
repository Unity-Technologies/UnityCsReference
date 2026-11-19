// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    internal enum ReferencedByRowKind : byte
    {
        Asset,
        PathGroup,
        More,
        Placeholder,
        Stopped,
        Hidden,
    }

    /// <summary>
    /// One row of the Referenced By tree, with everything bindItem shows already resolved. Rows are
    /// addressed by synthetic tree item ids: the same asset legitimately appears in several branches.
    /// </summary>
    internal sealed class ReferencedByRow
    {
        public ReferencedByRowKind Kind;
        public int AssetId = -1;

        /// <summary>The display name, or the group and paging captions.</summary>
        public string Label = string.Empty;

        /// <summary>"(loadable)", "(circular)" or "(unreferenced)"; null for a plain row.</summary>
        public string Suffix;

        public string Tooltip;

        /// <summary>What the label shows: the name with the suffix styled in.</summary>
        public string Text = string.Empty;

        public Texture Icon;
        public RouteGroup Group;
        public ReferencedByExpansion Expansion;

        /// <summary>For a More row, the row whose remaining entries it appends.</summary>
        public int ParentItemId = -1;

        /// <summary>For a More row, the Hidden note above it; the two come and go together.</summary>
        public int HiddenItemId = -1;

        /// <summary>A group row showing its routes in place of its summary.</summary>
        public bool RoutesOpen;
    }

    /// <summary>
    /// The Referenced By body: the inspected asset as the root row, its direct parents beneath it,
    /// and on expanding a parent the routes up to the build roots, grouped by where they end. A
    /// parent's children materialize on first expand from one bounded walk and are added in place;
    /// the tree is never rebuilt, so expansion and selection survive. A group row's box swaps its
    /// children between the terminal summary and the routes themselves, read from that same walk.
    /// </summary>
    internal sealed class ReferencedByTree : VisualElement
    {
        internal const int k_EntriesPerPage = 10;
        internal const int k_RoutesPerPage = 25;

        const string k_OpenGlyphClass = "inspector__ref-group-glyph--open";
        const string k_PathsNoun = " paths";
        const string k_LoadableSuffix = "(loadable)";
        const string k_CycleSuffix = "(circular)";
        const string k_UnreferencedSuffix = "(unreferenced)";
        const string k_CycleTooltipPrefix = "Already on this path: ";
        const string k_UnreferencedTooltip = "Nothing in the build references this asset, and it isn't a build root.";

        readonly TreeView m_Tree;
        readonly LockedGroupController m_Controller;
        readonly ZebraEmptyBody m_EmptyBody;
        readonly ReferenceFilter m_Filter = new ReferenceFilter();
        readonly ToolbarSearchField m_SearchField;
        readonly ToolbarMenu m_ViewMenu;

        // The direct parents' item ids in display order, and the subtrees the filter has taken out
        // of the control: the data as it was, and which rows in it were open.
        readonly List<int> m_ParentItems = new List<int>();
        readonly Dictionary<int, DetachedParent> m_Detached = new Dictionary<int, DetachedParent>();
        readonly List<int> m_ToRestore = new List<int>();
        readonly List<int> m_Expanded = new List<int>();
        IVisualElementScheduledItem m_SearchDebounce;

        // Parent item id → the placeholder child that gives it an arrow until it is expanded.
        readonly Dictionary<int, int> m_Pending = new Dictionary<int, int>();

        // Parent item id → its entries, for the More row to continue from.
        readonly Dictionary<int, Page> m_Pages = new Dictionary<int, Page>();

        // Group item id → both things the group can show.
        readonly Dictionary<int, GroupState> m_Groups = new Dictionary<int, GroupState>();
        readonly List<int> m_Chain = new List<int>();
        readonly List<int> m_Nodes = new List<int>();

        // Rows added in the current batch that open once the control has taken them in.
        readonly List<int> m_ToExpand = new List<int>();

        ReferencedByWalker m_Walker;
        IAssetReferenceProvider m_References;
        BuildAnalysisAsset[] m_Assets = Array.Empty<BuildAnalysisAsset>();
        int m_InspectedAssetId = -1;
        int m_NextItemId;

        public ReferencedByTree()
        {
            AddToClassList("inspector__ref-list");
            AddToClassList("inspector__ref-tree");

            Add(ReferenceListToolbar.Make(out m_SearchField, out m_ViewMenu));
            m_SearchField.tooltip = "Search referencing asset name";
            m_SearchField.RegisterValueChangedCallback(evt =>
            {
                var newText = evt.newValue ?? string.Empty;
                m_SearchDebounce?.Pause();
                m_SearchDebounce = schedule.Execute(() => SetSearchText(newText)).StartingIn(ReferenceListToolbar.k_SearchDebounceMs);
            });

            m_Tree = new TreeView
            {
                fixedItemHeight = 20,
                selectionType = SelectionType.Single,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
            };
            m_Controller = new LockedGroupController();
            m_Tree.SetViewController(m_Controller);
            m_Tree.makeItem = MakeRow;
            m_Tree.bindItem = BindRow;
            m_Tree.itemExpandedChanged += OnItemExpandedChanged;
            m_Tree.itemsChosen += OnItemsChosen;
            m_Tree.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
            m_Tree.AddToClassList("inspector__references-list");
            Add(m_Tree);

            m_EmptyBody = new ZebraEmptyBody(m_Tree);
            Add(m_EmptyBody);
        }

        /// <summary>The bounds handed to every expansion. Tests lower them to reach the truncated states.</summary>
        internal WalkLimits Limits { get; set; } = WalkLimits.Default;

        /// <summary>Where a row's "Select in Asset Table" goes; the section wires it to its owner.</summary>
        internal Action<int> SelectInTable { get; set; }

        internal int RootItemId { get; private set; } = -1;

        public void Bind(ReferencedByWalker walker, IAssetReferenceProvider references, int assetId, BuildAnalysisAsset[] assets)
        {
            if (walker == null)
                throw new ArgumentNullException(nameof(walker));
            if (references == null)
                throw new ArgumentNullException(nameof(references));
            if (assets == null)
                throw new ArgumentNullException(nameof(assets));

            // The same asset again, from a regenerated graph with the same data: what is on screen
            // is still right, so the rows and everything the user did to them stay.
            var sameAsset = assetId == m_InspectedAssetId && RootItemId >= 0
                && SameData(m_Walker, m_References, m_Assets, walker, references, assets);
            m_Walker = walker;
            m_References = references;
            m_Assets = assets;
            if (sameAsset)
            {
                m_EmptyBody.Refresh();
                return;
            }

            m_InspectedAssetId = assetId;
            Reset();

            var parents = SortedParents(assetId);
            var children = new List<TreeViewItemData<ReferencedByRow>>(parents.Count);
            var parentPaths = new List<string>(parents.Count);
            foreach (var parent in parents)
            {
                var itemId = NextItemId();
                List<TreeViewItemData<ReferencedByRow>> placeholder = null;
                if (walker.HasAncestry(parent.Id))
                {
                    var placeholderId = NextItemId();
                    m_Pending.Add(itemId, placeholderId);
                    placeholder = new List<TreeViewItemData<ReferencedByRow>>(1)
                    {
                        new TreeViewItemData<ReferencedByRow>(placeholderId, new ReferencedByRow { Kind = ReferencedByRowKind.Placeholder }),
                    };
                }
                children.Add(new TreeViewItemData<ReferencedByRow>(itemId, AssetRow(parent.Id), placeholder));
                m_ParentItems.Add(itemId);
                parentPaths.Add(parent.Path);
            }
            m_Filter.SetItems(parentPaths);
            ResetControls();

            // SetRootItems replaces the data without refreshing the rows, and ExpandItem refreshes
            // only when there is something to expand: an asset with no referrers must refresh itself,
            // or the previous asset's rows stay on screen.
            RootItemId = NextItemId();
            m_Tree.SetRootItems(new List<TreeViewItemData<ReferencedByRow>>(1)
            {
                new TreeViewItemData<ReferencedByRow>(RootItemId, AssetRow(assetId), children),
            });
            m_Tree.ExpandItem(RootItemId, expandAllChildren: false, refresh: false);
            m_Tree.RefreshItems();
            m_EmptyBody.Refresh();
        }

        // Everything the rows were read from: referrers, the loadable mark, the roots and the paths.
        static bool SameData(
            ReferencedByWalker oldWalker, IAssetReferenceProvider oldReferences, BuildAnalysisAsset[] oldAssets,
            ReferencedByWalker walker, IAssetReferenceProvider references, BuildAnalysisAsset[] assets)
        {
            if (oldWalker == null || oldReferences == null)
                return false;
            if (oldWalker == walker && oldReferences == references && oldAssets == assets)
                return true;
            if (oldReferences.NodeCount != references.NodeCount || oldAssets.Length != assets.Length)
                return false;

            for (var id = 0; id < references.NodeCount; id++)
            {
                if (!oldReferences.GetReferencedBy(id).SequenceEqual(references.GetReferencedBy(id))
                    || oldReferences.IsLoadable(id) != references.IsLoadable(id)
                    || oldWalker.IsRoot(id) != walker.IsRoot(id))
                    return false;
            }
            for (var id = 0; id < assets.Length; id++)
            {
                if (oldAssets[id].Path != assets[id].Path)
                    return false;
            }
            return true;
        }

        public void ClearRows()
        {
            m_InspectedAssetId = -1;
            Reset();
            m_Filter.SetItems(Array.Empty<string>());
            ResetControls();
            m_Tree.SetRootItems(new List<TreeViewItemData<ReferencedByRow>>());
            m_Tree.RefreshItems();
            m_EmptyBody.Refresh();
        }

        // The zebra reads live geometry, so it needs a nudge when the tree is display-toggled back on.
        public void RefreshZebra() => m_EmptyBody.Refresh();

        internal ReferencedByRow RowOf(int itemId) => m_Tree.GetItemDataForId<ReferencedByRow>(itemId);
        internal IEnumerable<int> ChildrenOf(int itemId) => m_Tree.viewController.GetChildrenIds(itemId);
        internal bool HasChildren(int itemId) => m_Tree.viewController.HasChildren(itemId);
        internal bool IsExpanded(int itemId) => m_Tree.IsExpanded(itemId);
        internal void Expand(int itemId) => m_Tree.ExpandItem(itemId);
        internal void Collapse(int itemId) => m_Tree.CollapseItem(itemId);
        internal void ExpandAll(int itemId) => m_Tree.ExpandItem(itemId, expandAllChildren: true);
        internal void CollapseAll(int itemId) => m_Tree.CollapseItem(itemId, collapseAllChildren: true);
        internal int VisibleRowCount => m_Tree.itemsSource?.Count ?? 0;
        internal void Select(int itemId) => m_Tree.SetSelectionById(itemId);

        internal int SelectedItemId
        {
            get
            {
                foreach (var id in m_Tree.selectedIds)
                {
                    return id;
                }
                return -1;
            }
        }

        /// <summary>Replaces a Load Next row and its note with the next page of entries or routes.</summary>
        internal void ShowMore(int moreItemId)
        {
            var more = RowOf(moreItemId);
            if (more == null || more.Kind != ReferencedByRowKind.More)
                return;

            var parent = RowOf(more.ParentItemId);
            if (parent.Kind == ReferencedByRowKind.PathGroup)
            {
                if (!m_Groups.TryGetValue(more.ParentItemId, out var state))
                    return;
                AppendRoutePage(more.ParentItemId, state);
            }
            else
            {
                if (!m_Pages.TryGetValue(more.ParentItemId, out var page))
                    return;
                RemoveTrailer(moreItemId);
                AppendPage(more.ParentItemId, parent.AssetId, page);
            }
            CommitAppend();
        }

        // The two trailer rows leave together; selection goes only if it was on one of them.
        void RemoveTrailer(int trailerItemId)
        {
            var selected = SelectedItemId;
            var trailer = RowOf(trailerItemId);
            if (trailer != null && trailer.HiddenItemId >= 0)
            {
                if (selected == trailer.HiddenItemId)
                    m_Tree.ClearSelection();
                m_Tree.TryRemoveItem(trailer.HiddenItemId, rebuildTree: false);
            }
            if (selected == trailerItemId)
                m_Tree.ClearSelection();
            m_Tree.TryRemoveItem(trailerItemId, rebuildTree: false);
        }

        /// <summary>Swaps a group row's children between its summary and its routes.</summary>
        internal void ToggleGroup(int groupItemId)
        {
            var group = RowOf(groupItemId);
            if (group == null || group.Kind != ReferencedByRowKind.PathGroup || !m_Groups.TryGetValue(groupItemId, out var state))
                return;

            group.RoutesOpen = !group.RoutesOpen;
            if (IsBeneath(SelectedItemId, groupItemId))
                m_Tree.ClearSelection();

            var children = new List<int>(ChildrenOf(groupItemId));
            foreach (var child in children)
            {
                m_Tree.TryRemoveItem(child, rebuildTree: false);
            }
            state.TrailerId = -1;

            if (!group.RoutesOpen)
            {
                AddRecords(state.Summary);
                m_ToExpand.Add(state.Summary[0].ItemId);
            }
            else if (state.Routes.Count == 0)
            {
                AppendRoutePage(groupItemId, state);
            }
            else
            {
                AddRecords(state.Routes);
                foreach (var record in state.Routes)
                {
                    if (record.ParentItemId == groupItemId)
                        m_ToExpand.Add(record.ItemId);
                }
                AddTrailer(groupItemId, state);
            }
            CommitAppend();
        }

        // The suffix rides inside the name label so a tight row ellipsises them together, as the
        // design shows ("ScriptableObj01.asset (loada…)"). noparse keeps a '<' in a name literal.
        internal static string DisplayText(string name, string suffix) =>
            suffix == null ? name : "<noparse>" + name + "</noparse> <i><alpha=#99>" + suffix + "</i>";

        internal static string GroupLabel(int routeCount, bool lowerBound)
        {
            if (lowerBound)
                return routeCount + "+ Paths";
            return routeCount == 1 ? "1 Path" : routeCount + " Paths";
        }

        // The control keeps its selection by id across a root change, and ids start over here.
        void Reset()
        {
            m_Tree.ClearSelection();
            m_Pending.Clear();
            m_Pages.Clear();
            m_Groups.Clear();
            m_ToExpand.Clear();
            m_ParentItems.Clear();
            m_Detached.Clear();
            m_ToRestore.Clear();
            m_NextItemId = 0;
            RootItemId = -1;
        }

        internal string SearchText => m_Filter.SearchText;

        /// <summary>
        /// Narrows the direct parents to the names containing the text. A parent that leaves takes
        /// its subtree out of the control and brings it back as it was, open rows included, so a
        /// search never costs an expansion.
        /// </summary>
        internal void SetSearchText(string value)
        {
            if (m_Filter.SetSearchText(value) && RootItemId >= 0)
                ApplyFilter();
        }

        /// <summary>Narrows the direct parents to one extension ("All" for every one); combines with the search.</summary>
        internal void SetView(string value)
        {
            if (!m_Filter.SetView(value))
                return;
            ReferenceListToolbar.ShowFilterState(m_ViewMenu, m_Filter);
            if (RootItemId >= 0)
                ApplyFilter();
        }

        void ResetControls()
        {
            m_SearchDebounce?.Pause();
            m_SearchField.SetValueWithoutNotify(string.Empty);
            SyncFilterMenu();
        }

        void SyncFilterMenu() => ReferenceListToolbar.SyncFilter(m_ViewMenu, m_Filter, SetView);

        // A parent that stops matching comes out where it is: a removal keeps the others in order.
        // A parent that matches again can only be appended cheaply (an insert at an index re-sorts
        // the root's children and repacks the whole hierarchy), so from the first one that returns,
        // the parents still in the control behind it come out too and everything goes back in order.
        void ApplyFilter()
        {
            var selected = SelectedItemId;
            var selectedParent = DirectParentOf(selected);
            var firstReturning = -1;
            var changed = false;
            for (var i = m_ParentItems.Count - 1; i >= 0; i--)
            {
                var parentId = m_ParentItems[i];
                if (m_Detached.TryGetValue(parentId, out var entry))
                {
                    if (m_Filter.Matches(i, entry.Data.data.Label))
                        firstReturning = i;
                }
                else if (!m_Filter.Matches(i, RowOf(parentId).Label))
                {
                    Detach(parentId);
                    changed = true;
                }
            }
            if (firstReturning >= 0)
            {
                for (var i = m_ParentItems.Count - 1; i > firstReturning; i--)
                {
                    if (!m_Detached.ContainsKey(m_ParentItems[i]))
                        Detach(m_ParentItems[i]);
                }
                for (var i = firstReturning; i < m_ParentItems.Count; i++)
                {
                    var parentId = m_ParentItems[i];
                    var entry = m_Detached[parentId];
                    if (!m_Filter.Matches(i, entry.Data.data.Label))
                        continue;
                    m_Tree.AddItem(entry.Data, RootItemId, rebuildTree: false);
                    if (entry.Expanded != null)
                        m_ToRestore.AddRange(entry.Expanded);
                    m_Detached.Remove(parentId);
                }
                changed = true;
            }
            if (!changed)
                return;

            // A row that came out and went back in under the same id keeps its selection.
            if (selected >= 0 && m_Detached.ContainsKey(selectedParent))
                m_Tree.ClearSelection();

            // Same two-step as CommitAppend: the control takes the rows in, then opens them.
            m_Tree.RefreshItems();
            foreach (var id in m_ToRestore)
                m_Tree.ExpandItem(id, expandAllChildren: false, refresh: false);
            m_ToRestore.Clear();
            m_Tree.RefreshItems();
            m_EmptyBody.Refresh();
        }

        // Takes a direct parent's subtree out of the control, remembering what was open in it.
        void Detach(int parentId)
        {
            m_Expanded.Clear();
            CollectExpanded(parentId, m_Expanded);
            m_Detached.Add(parentId, new DetachedParent(m_Controller.GetTreeViewItemDataForId(parentId),
                m_Expanded.Count > 0 ? m_Expanded.ToArray() : null));
            m_Tree.TryRemoveItem(parentId, rebuildTree: false);
        }

        // The direct parent the row sits under; -1 for the root row, a missing row, or none.
        int DirectParentOf(int itemId)
        {
            if (itemId < 0 || itemId == RootItemId)
                return -1;
            var id = itemId;
            while (true)
            {
                var parent = m_Tree.viewController.GetParentId(id);
                if (parent < 0)
                    return -1;
                if (parent == RootItemId)
                    return id;
                id = parent;
            }
        }

        void CollectExpanded(int itemId, List<int> expanded)
        {
            if (!m_Tree.viewController.HasChildren(itemId))
                return;
            if (m_Tree.IsExpanded(itemId))
                expanded.Add(itemId);
            foreach (var child in m_Tree.viewController.GetChildrenIds(itemId))
                CollectExpanded(child, expanded);
        }

        int NextItemId() => m_NextItemId++;

        void OnItemExpandedChanged(TreeViewExpansionChangedArgs args)
        {
            if (args.isAppliedToAllChildren)
                UndoRecursiveFlags(args);
            if (args.id < 0 || !args.isExpanded || !m_Pending.TryGetValue(args.id, out var placeholderId))
                return;
            m_Pending.Remove(args.id);

            // Only direct parents expand lazily, so the path climbed from is always two long.
            var parent = RowOf(args.id);
            var expansion = m_Walker.Expand(stackalloc[] { m_InspectedAssetId, parent.AssetId }, Limits);
            var page = new Page(expansion, BuildEntries(expansion), NoGroupShowsTheStop(expansion, parent.AssetId));
            m_Pages[args.id] = page;

            m_Tree.TryRemoveItem(placeholderId, rebuildTree: false);
            AppendPage(args.id, parent.AssetId, page);
            CommitAppend();
        }

        // Alt+click and Alt+arrow set or clear the flag on a whole subtree without asking the
        // controller row by row, and raise the event for the clicked row only. A pending parent
        // the user did not click closes again rather than showing open over its placeholder; a
        // group row, drawn without an arrow, must stay open or its box would act on hidden rows.
        void UndoRecursiveFlags(TreeViewExpansionChangedArgs args)
        {
            var changed = false;
            if (args.isExpanded)
            {
                // Pending parents sit right under the root, so only its recursive expand (or the
                // whole tree's, id -1) reaches them; CommitAppend's own recursive expands never do.
                if (args.id >= 0 && args.id != RootItemId)
                    return;
                foreach (var parentId in m_Pending.Keys)
                {
                    if (!m_Tree.IsExpanded(parentId))
                        continue;
                    m_Tree.CollapseItem(parentId, collapseAllChildren: false, refresh: false);
                    changed = true;
                }
            }
            else
            {
                foreach (var groupId in m_Groups.Keys)
                {
                    if (args.id >= 0 && groupId != args.id && !IsBeneath(groupId, args.id))
                        continue;
                    if (!m_Tree.viewController.HasChildren(groupId))
                        continue;
                    if (!m_Tree.IsExpanded(groupId))
                    {
                        m_Tree.ExpandItem(groupId, expandAllChildren: false, refresh: false);
                        changed = true;
                    }
                    // The summary (or the routes) closed with the group; a group shows them open.
                    foreach (var child in ChildrenOf(groupId))
                    {
                        if (m_Tree.IsExpanded(child) || !m_Tree.viewController.HasChildren(child))
                            continue;
                        m_Tree.ExpandItem(child, expandAllChildren: true, refresh: false);
                        changed = true;
                    }
                }
            }
            if (changed)
                m_Tree.RefreshItems();
        }

        // Added rows reach the control's flattened hierarchy on a refresh, and only then can they
        // be expanded: the first refresh takes them in, the second shows them open.
        void CommitAppend()
        {
            m_Tree.RefreshItems();
            foreach (var id in m_ToExpand)
                m_Tree.ExpandItem(id, expandAllChildren: true, refresh: false);
            m_ToExpand.Clear();
            m_Tree.RefreshItems();
            m_EmptyBody.Refresh();
        }

        List<AssetKey> SortedParents(int assetId)
        {
            var referrers = m_References.GetReferencedBy(assetId);
            var parents = new List<AssetKey>(referrers.Length);
            for (var i = 0; i < referrers.Length; i++)
            {
                var id = referrers[i];
                if ((uint)id < (uint)m_Assets.Length)
                    parents.Add(new AssetKey(id, PathOf(id)));
            }
            parents.Sort();
            return parents;
        }

        // Most routes first; the root's name then path break ties, then root+1's, since a build
        // with one root would otherwise order equal counts by asset id. The names are resolved once
        // per group, not once per comparison.
        List<Entry> BuildEntries(ReferencedByExpansion expansion)
        {
            var groups = expansion.Groups;
            var keyed = new (RouteGroup group, AssetKey root, AssetKey rootPlusOne)[groups.Length];
            for (var i = 0; i < groups.Length; i++)
            {
                var group = groups[i];
                keyed[i] = (group, new AssetKey(group.Root, PathOf(group.Root)), new AssetKey(group.RootPlusOne, PathOf(group.RootPlusOne)));
            }
            Array.Sort(keyed, (x, y) =>
            {
                var byCount = y.group.RouteCount.CompareTo(x.group.RouteCount);
                if (byCount != 0)
                    return byCount;
                var byRoot = x.root.CompareTo(y.root);
                return byRoot != 0 ? byRoot : x.rootPlusOne.CompareTo(y.rootPlusOne);
            });

            var entries = new List<Entry>(groups.Length + expansion.CycleLeaves.Length + expansion.DeadEndLeaves.Length);
            foreach (var entry in keyed)
                entries.Add(new Entry(entry.group, -1));
            foreach (var leaf in expansion.CycleLeaves)
                entries.Add(new Entry(null, leaf));
            foreach (var leaf in expansion.DeadEndLeaves)
                entries.Add(new Entry(null, leaf));
            return entries;
        }

        void AppendPage(int parentItemId, int expandedAssetId, Page page)
        {
            var end = Math.Min(page.Cursor + k_EntriesPerPage, page.Entries.Count);
            for (var i = page.Cursor; i < end; i++)
            {
                var entry = page.Entries[i];
                if (entry.Group != null)
                    AddGroup(parentItemId, expandedAssetId, entry.Group, page.Expansion);
                else
                    AddChain(parentItemId, expandedAssetId, entry.Leaf, page.Expansion);
            }
            page.Cursor = end;

            if (page.Remaining > 0)
                AddHiddenAndMore(parentItemId, page.Remaining, k_EntriesPerPage, string.Empty);
            else if (page.StopUnmarked)
            {
                var stopped = new TreeViewItemData<ReferencedByRow>(NextItemId(), StoppedBeforeRootRow(page.Expansion));
                m_Tree.AddItem(stopped, parentItemId, rebuildTree: false);
            }
        }

        // "N hidden" above a "Load Next N" button, the shape the 1 October review asked for.
        int AddHiddenAndMore(int parentItemId, int remaining, int pageSize, string noun)
        {
            var hiddenId = NextItemId();
            m_Tree.AddItem(new TreeViewItemData<ReferencedByRow>(hiddenId, HiddenRow(remaining, noun)), parentItemId, rebuildTree: false);
            var moreId = NextItemId();
            var more = MoreRow(parentItemId, Math.Min(pageSize, remaining));
            more.HiddenItemId = hiddenId;
            m_Tree.AddItem(new TreeViewItemData<ReferencedByRow>(moreId, more), parentItemId, rebuildTree: false);
            return moreId;
        }

        void AddGroup(int parentItemId, int expandedAssetId, RouteGroup group, ReferencedByExpansion expansion)
        {
            // A root referencing the expanded row directly: root+1 is that row, so only the root shows.
            if (group.RootPlusOne == expandedAssetId)
            {
                m_Tree.AddItem(new TreeViewItemData<ReferencedByRow>(NextItemId(), AssetRow(group.Root)), parentItemId, rebuildTree: false);
                return;
            }

            // Nothing to collapse when the one route is direct and the walk saw everything. A count
            // that is only a lower bound keeps its group row so the '+' is visible.
            var direct = expansion.Complete && group.RouteCount == 1 && expansion.DepthOf(group.Leaves[0]) == 2;
            var rootPlusOneId = NextItemId();
            if (direct)
            {
                AddRecord(new RowRecord(rootPlusOneId, parentItemId, AssetRow(group.RootPlusOne)));
                AddRecord(new RowRecord(NextItemId(), rootPlusOneId, AssetRow(group.Root)));
                m_ToExpand.Add(rootPlusOneId);
                return;
            }

            var groupId = NextItemId();
            m_Tree.AddItem(new TreeViewItemData<ReferencedByRow>(groupId, GroupRow(group, expansion)), parentItemId, rebuildTree: false);
            var state = new GroupState(group, expansion);
            state.Summary.Add(new RowRecord(rootPlusOneId, groupId, AssetRow(group.RootPlusOne)));
            state.Summary.Add(new RowRecord(NextItemId(), rootPlusOneId, AssetRow(group.Root)));
            m_Groups.Add(groupId, state);
            AddRecords(state.Summary);
            m_ToExpand.Add(groupId);
        }

        // The routes are the walk's own tree filtered to this group's leaves: rows sharing a prefix
        // share a walk node and merge without comparing assets. Everything beneath a node created
        // here was created in the same batch, so expanding each chain's first new node opens exactly
        // the new rows.
        void AppendRoutePage(int groupItemId, GroupState state)
        {
            if (state.TrailerId >= 0)
            {
                RemoveTrailer(state.TrailerId);
                state.TrailerId = -1;
            }

            var leaves = state.Group.Leaves;
            var end = Math.Min(state.Cursor + k_RoutesPerPage, leaves.Length);
            for (var i = state.Cursor; i < end; i++)
            {
                m_Nodes.Clear();
                for (var node = leaves[i]; node >= 0; node = state.Expansion.ParentOf(node))
                {
                    m_Nodes.Add(node);
                }
                m_Nodes.Reverse();

                var parentItemId = groupItemId;
                var firstNew = -1;
                foreach (var node in m_Nodes)
                {
                    if (!state.NodeItems.TryGetValue(node, out var itemId))
                    {
                        itemId = NextItemId();
                        var record = new RowRecord(itemId, parentItemId, AssetRow(state.Expansion.AssetIdAt(node)));
                        state.Routes.Add(record);
                        state.NodeItems.Add(node, itemId);
                        AddRecord(record);
                        if (firstNew < 0)
                            firstNew = itemId;
                    }
                    parentItemId = itemId;
                }
                if (firstNew >= 0)
                    m_ToExpand.Add(firstNew);
            }
            state.Cursor = end;
            AddTrailer(groupItemId, state);
        }

        // After the last page, a count that is only a lower bound says so where the routes end.
        void AddTrailer(int groupItemId, GroupState state)
        {
            var remaining = state.Group.Leaves.Length - state.Cursor;
            if (remaining > 0)
            {
                state.TrailerId = AddHiddenAndMore(groupItemId, remaining, k_RoutesPerPage, k_PathsNoun);
                return;
            }
            if (!state.Group.Capped && state.Expansion.Complete)
                return;

            state.TrailerId = NextItemId();
            m_Tree.AddItem(new TreeViewItemData<ReferencedByRow>(state.TrailerId, StoppedRow(state.Group.RouteCount)), groupItemId, rebuildTree: false);
        }

        void AddRecord(in RowRecord record) =>
            m_Tree.AddItem(new TreeViewItemData<ReferencedByRow>(record.ItemId, record.Row), record.ParentItemId, rebuildTree: false);

        void AddRecords(List<RowRecord> records)
        {
            foreach (var record in records)
            {
                AddRecord(record);
            }
        }

        bool IsBeneath(int itemId, int ancestorItemId)
        {
            if (itemId < 0)
                return false;
            for (var id = m_Tree.viewController.GetParentId(itemId); id >= 0; id = m_Tree.viewController.GetParentId(id))
            {
                if (id == ancestorItemId)
                    return true;
            }
            return false;
        }

        void AddChain(int parentItemId, int expandedAssetId, int leaf, ReferencedByExpansion expansion)
        {
            expansion.GetChain(leaf, m_Chain);
            var last = m_Chain.Count - 1;
            var leafRow = expansion.KindOf(leaf) == RouteNodeKind.Cycle
                ? AssetRow(m_Chain[last], k_CycleSuffix, CycleTooltip(expandedAssetId))
                : AssetRow(m_Chain[last], k_UnreferencedSuffix, k_UnreferencedTooltip);

            var item = new TreeViewItemData<ReferencedByRow>(NextItemId(), leafRow);
            for (var i = last - 1; i >= 0; i--)
            {
                item = new TreeViewItemData<ReferencedByRow>(NextItemId(), AssetRow(m_Chain[i]),
                    new List<TreeViewItemData<ReferencedByRow>>(1) { item });
            }

            m_Tree.AddItem(item, parentItemId, rebuildTree: false);
            m_ToExpand.Add(item.id);
        }

        string CycleTooltip(int expandedAssetId)
        {
            var text = new StringBuilder(k_CycleTooltipPrefix);
            text.Append(NameOf(PathOf(m_InspectedAssetId)));
            text.Append(" → ").Append(NameOf(PathOf(expandedAssetId)));
            foreach (var id in m_Chain)
                text.Append(" → ").Append(NameOf(PathOf(id)));
            return text.ToString();
        }

        ReferencedByRow AssetRow(int assetId, string suffix = null, string tooltip = null)
        {
            var path = PathOf(assetId);
            var name = NameOf(path);
            suffix ??= m_References.IsLoadable(assetId) ? k_LoadableSuffix : null;
            return new ReferencedByRow
            {
                Kind = ReferencedByRowKind.Asset,
                AssetId = assetId,
                Label = name,
                Suffix = suffix,
                Tooltip = tooltip ?? path,
                Text = DisplayText(name, suffix),
                Icon = IconUtility.GetAssetIcon(path),
            };
        }

        static ReferencedByRow GroupRow(RouteGroup group, ReferencedByExpansion expansion)
        {
            var label = GroupLabel(group.RouteCount, group.Capped || !expansion.Complete);
            return new ReferencedByRow
            {
                Kind = ReferencedByRowKind.PathGroup,
                Label = label,
                Text = label,
                Group = group,
                Expansion = expansion,
            };
        }

        static ReferencedByRow MoreRow(int parentItemId, int count)
        {
            var label = "Load Next " + count;
            return new ReferencedByRow
            {
                Kind = ReferencedByRowKind.More,
                Label = label,
                Text = label,
                ParentItemId = parentItemId,
            };
        }

        static ReferencedByRow HiddenRow(int remaining, string noun) =>
            NoteRow(ReferencedByRowKind.Hidden, remaining + noun + " hidden");

        static ReferencedByRow StoppedRow(int routeCount) =>
            NoteRow(ReferencedByRowKind.Stopped, "Stopped after " + routeCount + " paths");

        // An incomplete walk shows as a '+' on its group rows; a root referencing the expanded row
        // directly renders as a plain row instead, so with only those (or nothing) the stop is unmarked.
        static bool NoGroupShowsTheStop(ReferencedByExpansion expansion, int expandedAssetId)
        {
            if (expansion.Complete)
                return false;
            foreach (var group in expansion.Groups)
            {
                if (group.RootPlusOne != expandedAssetId)
                    return false;
            }
            return true;
        }

        // Says which limit cut the walk short: the depth when a route ran past it, else the visit budget.
        static ReferencedByRow StoppedBeforeRootRow(ReferencedByExpansion expansion) =>
            NoteRow(ReferencedByRowKind.Stopped, expansion.TruncatedLeaves.Length > 0
                ? "Stopped after " + expansion.ReachedDepth + " levels"
                : "Stopped after " + expansion.VisitCount + " assets");

        static ReferencedByRow NoteRow(ReferencedByRowKind kind, string label)
        {
            return new ReferencedByRow
            {
                Kind = kind,
                Label = label,
                Text = "<i><alpha=#99>" + label + "</i>",
            };
        }

        string PathOf(int assetId) =>
            (uint)assetId < (uint)m_Assets.Length ? m_Assets[assetId].Path ?? string.Empty : string.Empty;

        static string NameOf(string path) => string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileName(path);

        VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.AddToClassList("inspector__ref-tree-row");

            var glyph = new VisualElement();
            glyph.AddToClassList("inspector__ref-group-glyph");
            var box = new VisualElement();
            box.AddToClassList("inspector__ref-group-glyph-box");
            var horizontal = new VisualElement();
            horizontal.AddToClassList("inspector__ref-group-glyph-bar-h");
            var vertical = new VisualElement();
            vertical.AddToClassList("inspector__ref-group-glyph-bar-v");
            box.Add(horizontal);
            box.Add(vertical);
            glyph.Add(box);
            glyph.RegisterCallback<PointerDownEvent>(StopPointer);
            glyph.RegisterCallback<PointerUpEvent>(StopPointer);
            glyph.RegisterCallback<ClickEvent>(OnGlyphClicked);
            row.Add(glyph);

            row.Add(AssetReferenceRow.Make(withSize: false, id => SelectInTable?.Invoke(id)));

            // A real button: its own click handling keeps the pointer from reaching the tree's
            // selection, so paging leaves the selected row where it was.
            var load = new Button();
            load.AddToClassList("inspector__ref-tree-load");
            load.style.display = DisplayStyle.None;
            load.clicked += () =>
            {
                if (row.userData is int itemId)
                    ShowMore(itemId);
            };
            row.Add(load);
            return row;
        }

        void BindRow(VisualElement element, int index)
        {
            var row = m_Tree.GetItemDataForIndex<ReferencedByRow>(index);
            var isAsset = row.Kind == ReferencedByRowKind.Asset;
            var isGroup = row.Kind == ReferencedByRowKind.PathGroup;
            var isMore = row.Kind == ReferencedByRowKind.More;

            element[0].style.display = isGroup ? DisplayStyle.Flex : DisplayStyle.None;
            element[0].EnableInClassList(k_OpenGlyphClass, isGroup && row.RoutesOpen);
            var assetRow = element[1];
            assetRow.style.display = isMore ? DisplayStyle.None : DisplayStyle.Flex;
            assetRow[0].style.display = isAsset ? DisplayStyle.Flex : DisplayStyle.None;
            // The path comes from the id, not the tooltip: a circular leaf's tooltip is its chain.
            AssetReferenceRow.Bind(assetRow, row.Icon, row.Text, row.Tooltip, size: null,
                isAsset ? PathOf(row.AssetId) : null, isAsset ? row.AssetId : -1);
            var load = (Button)element[2];
            load.style.display = isMore ? DisplayStyle.Flex : DisplayStyle.None;
            load.text = isMore ? row.Label : string.Empty;
            element.userData = m_Tree.GetIdForIndex(index);

            // The control has just shown the arrow because the row has children. A group row is
            // permanently expanded and draws the box over that slot instead. Hidden, not removed:
            // the row's indent is measured from the arrow, and collapses with it.
            if (isGroup)
            {
                var toggle = element.parent?.parent?.Q(className: BaseTreeView.itemToggleUssClassName);
                if (toggle != null)
                    toggle.visible = false;
            }
        }

        // Enter (and a double-click) on the selected row: the group row's box and the Load Next
        // button are the two controls in the tree, and this is their keyboard.
        void OnItemsChosen(IEnumerable<object> chosen)
        {
            var itemId = SelectedItemId;
            var row = RowOf(itemId);
            if (row == null)
                return;
            if (row.Kind == ReferencedByRowKind.PathGroup)
                ToggleGroup(itemId);
            else if (row.Kind == ReferencedByRowKind.More)
                ShowMore(itemId);
        }

        // Left on a group row would ask the control to collapse it, which the controller refuses
        // without the control noticing, so the key does nothing. Move to the parent instead, as
        // Left does on any other row that cannot collapse.
        void OnNavigationMove(NavigationMoveEvent evt)
        {
            if (evt.direction != NavigationMoveEvent.Direction.Left)
                return;
            var itemId = SelectedItemId;
            var row = RowOf(itemId);
            if (row == null || row.Kind != ReferencedByRowKind.PathGroup)
                return;

            var parentId = m_Tree.viewController.GetParentId(itemId);
            if (parentId < 0)
                return;
            m_Tree.SetSelectionById(parentId);
            m_Tree.ScrollToItemById(parentId);
            evt.StopPropagation();
            m_Tree.focusController?.IgnoreEvent(evt);
        }

        // The box is a control in the row, not part of its click target: the pointer never reaches
        // the tree's selection handling, while the click itself is still delivered.
        static void StopPointer(EventBase evt) => evt.StopPropagation();

        void OnGlyphClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not VisualElement glyph || glyph.parent?.userData is not int itemId)
                return;
            ToggleGroup(itemId);
            evt.StopPropagation();
        }

        readonly struct Entry
        {
            public readonly RouteGroup Group;
            public readonly int Leaf;

            public Entry(RouteGroup group, int leaf)
            {
                Group = group;
                Leaf = leaf;
            }
        }

        // The order every asset list in the tree uses: name, then path, then id. The name is
        // resolved once here, not on every comparison.
        readonly struct AssetKey : IComparable<AssetKey>
        {
            public readonly int Id;
            public readonly string Path;
            readonly string m_Name;

            public AssetKey(int id, string path)
            {
                Id = id;
                Path = path;
                m_Name = NameOf(path);
            }

            public int CompareTo(AssetKey other)
            {
                var byName = string.Compare(m_Name, other.m_Name, StringComparison.OrdinalIgnoreCase);
                if (byName != 0)
                    return byName;
                var byPath = string.Compare(Path, other.Path, StringComparison.Ordinal);
                return byPath != 0 ? byPath : Id.CompareTo(other.Id);
            }
        }

        // A direct parent's subtree while the filter keeps it out of the control.
        readonly struct DetachedParent
        {
            public readonly TreeViewItemData<ReferencedByRow> Data;

            /// <summary>The rows that were open in it, or null.</summary>
            public readonly int[] Expanded;

            public DetachedParent(TreeViewItemData<ReferencedByRow> data, int[] expanded)
            {
                Data = data;
                Expanded = expanded;
            }
        }

        sealed class Page
        {
            public readonly ReferencedByExpansion Expansion;
            public readonly List<Entry> Entries;

            /// <summary>The walk was cut short and no row says so; a note closes the last page.</summary>
            public readonly bool StopUnmarked;
            public int Cursor;

            public Page(ReferencedByExpansion expansion, List<Entry> entries, bool stopUnmarked)
            {
                Expansion = expansion;
                Entries = entries;
                StopUnmarked = stopUnmarked;
            }

            public int Remaining => Entries.Count - Cursor;
        }

        readonly struct RowRecord
        {
            public readonly int ItemId;
            public readonly int ParentItemId;
            public readonly ReferencedByRow Row;

            public RowRecord(int itemId, int parentItemId, ReferencedByRow row)
            {
                ItemId = itemId;
                ParentItemId = parentItemId;
                Row = row;
            }
        }

        // Both things a group row can show, as flat rows in parent-first order so a flip replays
        // them under the same item ids. Routes accumulate page by page.
        sealed class GroupState
        {
            public readonly RouteGroup Group;
            public readonly ReferencedByExpansion Expansion;
            public readonly List<RowRecord> Summary = new List<RowRecord>(2);
            public readonly List<RowRecord> Routes = new List<RowRecord>();

            // Walk node → item id: the prefix merge.
            public readonly Dictionary<int, int> NodeItems = new Dictionary<int, int>();
            public int Cursor;
            public int TrailerId = -1;

            public GroupState(RouteGroup group, ReferencedByExpansion expansion)
            {
                Group = group;
                Expansion = expansion;
            }
        }

        // A group row is created expanded and stays that way: its summary must remain visible, and
        // only the box changes what sits beneath it.
        sealed class LockedGroupController : DefaultTreeViewController<ReferencedByRow>
        {
            public override bool CanChangeExpandedState(int id)
            {
                var row = GetDataForId(id);
                return row == null || row.Kind != ReferencedByRowKind.PathGroup || !IsExpanded(id);
            }
        }
    }
}
