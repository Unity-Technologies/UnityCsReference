// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.U2D.PhysicsCore2D.Profiler.UI
{
    [UxmlElement]
    partial class PhysicsCore2DModuleView : VisualElement
    {
        const string k_UXML = "PhysicsCore2D/Profiler/PhysicsCore2DModuleView/PhysicsCore2DModuleView.uxml";
        const string k_CollapsedKeysPref = "PhysicsCore2D.Profiler.CollapsedRows";
        const string k_ExpandedKeysPref = "PhysicsCore2D.Profiler.ExpandedRows";
        const char k_KeySeparator = '/';
        const string k_WorldKeyPrefix = "World:";
        const string k_MarkerKeyPrefix = "Marker:";
        enum Grouping { PerWorld, AllWorlds, ProfilerTiming }

        MultiColumnTreeView m_WorldTreeView;
        Label m_NoDataLabel;
        VisualElement m_ContentContainer;
        DropdownField m_GroupingDropdown;
        List<TreeViewItemData<TreeNodeData>> m_Data = new();
        Dictionary<int, string> m_IdToKey = new();
        // Timing rows a reader has opened or closed against their default, by path. Both world views share
        // these, since a row under one world is the same timing as the row of that name in the combined view.
        // Only these are stored between sessions: the paths come from a fixed set of timings, so they are
        // stable and few.
        HashSet<string> m_CollapsedKeys = LoadRowKeys(k_CollapsedKeysPref);
        HashSet<string> m_ExpandedKeys = LoadRowKeys(k_ExpandedKeysPref);

        // Worlds and marker groups a reader has opened, which is not stored between sessions. A world's name
        // is its own and a project can produce any number of them, and a marker group comes from whatever the
        // module registers, so neither belongs in a preference that grows without bound. Both start closed,
        // so remembering them for the session is enough to survive scrubbing the timeline.
        HashSet<string> m_ExpandedSessionKeys = new();

        // The state each key was left in by the last rebuild. Every world's row for a timing shares that
        // timing's key, so this is what tells a row the reader has since toggled from one that was not touched.
        Dictionary<string, bool> m_AppliedKeyState = new();
        PhysicsCore2DFrameData[] m_FrameData;
        byte[] m_WorldNames;
        float[] m_ProfileMarkers;
        int m_NextId;

        public PhysicsCore2DModuleView()
        {
            VisualTreeAsset visualTree = EditorGUIUtility.Load(k_UXML) as VisualTreeAsset;
            visualTree.CloneTree(this);

            m_WorldTreeView = this.Q<MultiColumnTreeView>("WorldTreeView");
            m_NoDataLabel = this.Q<Label>("noDataLabel");
            m_ContentContainer = this.Q<VisualElement>("contentContainer");
            m_GroupingDropdown = this.Q<DropdownField>("groupingDropdown");

            m_GroupingDropdown.choices = new List<string> { "Physics World", "Physics World (Combined)", "Physics Profiler Timing" };
            m_GroupingDropdown.index = 0;
            m_GroupingDropdown.RegisterValueChangedCallback(_ => Rebuild());

            SetupTree();
            ShowTree();
        }

        void SetupTree()
        {
            if (EditorGUIUtility.isProSkin)
                m_WorldTreeView.AddToClassList("dark");
            else
                m_WorldTreeView.AddToClassList("light");

            m_WorldTreeView.sortingMode = ColumnSortingMode.Custom;
            m_WorldTreeView.columnSortingChanged += OnColumnSortingChanged;
            m_WorldTreeView.itemsChosen += OnItemsChosen;

            foreach (var column in m_WorldTreeView.columns)
            {
                column.makeCell = () =>
                {
                    var label = new Label();
                    label.AddToClassList("cell-label");
                    return label;
                };

                column.bindCell = (element, index) =>
                {
                    var label = element as Label;
                    var data = m_WorldTreeView.GetItemDataForIndex<TreeNodeData>(index);
                    if (data == null)
                        return;

                    label.text = column.name == "Name" ? data.name : data.value;
                };

                column.comparison = (a, b) =>
                {
                    var dataA = m_WorldTreeView.GetItemDataForIndex<TreeNodeData>(a);
                    var dataB = m_WorldTreeView.GetItemDataForIndex<TreeNodeData>(b);
                    if (dataA == null || dataB == null)
                        return 0;
                    return column.name == "Name"
                        ? string.Compare(dataA.name, dataB.name, StringComparison.Ordinal)
                        : dataA.timeMs.CompareTo(dataB.timeMs);
                };
            }
        }

        // Rebuilding from the frame data restores the natural order when sorting is cleared, which re-sorting the already-sorted items cannot.
        void OnColumnSortingChanged() => Rebuild();

        // Double-clicking a row, or pressing Return on it, opens or closes it, so a branch can be worked
        // without aiming at its arrow. A row with nothing beneath it has nothing to toggle.
        void OnItemsChosen(IEnumerable<object> chosenItems)
        {
            // The rows arrive as a view over the live selection, and opening one rebuilds the rows underneath
            // it, so the ids are taken first and the tree is left alone until the reading is done.
            var chosenIds = new List<int>();
            foreach (var chosenItem in chosenItems)
            {
                if (chosenItem is TreeNodeData node && m_IdToKey.ContainsKey(node.id))
                    chosenIds.Add(node.id);
            }

            foreach (var chosenId in chosenIds)
            {
                if (m_WorldTreeView.IsExpanded(chosenId))
                    m_WorldTreeView.CollapseItem(chosenId);
                else
                    m_WorldTreeView.ExpandItem(chosenId);
            }
        }

        void ApplySorting(List<TreeViewItemData<TreeNodeData>> items)
        {
            // With no sorted column every comparison ties, and an unstable sort would still shuffle the build order, so leave the items untouched.
            if (!HasSortedColumns())
                return;

            SortItems(items);
        }

        bool HasSortedColumns()
        {
            if (m_WorldTreeView.sortedColumns == null)
                return false;

            foreach (var _ in m_WorldTreeView.sortedColumns)
                return true;

            return false;
        }

        void SortItems(List<TreeViewItemData<TreeNodeData>> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.children != null)
                {
                    var children = new List<TreeViewItemData<TreeNodeData>>(item.children);
                    SortItems(children);
                    items[i] = new TreeViewItemData<TreeNodeData>(item.id, item.data, children);
                }
            }

            items.Sort(CompareItems);
        }

        int CompareItems(TreeViewItemData<TreeNodeData> a, TreeViewItemData<TreeNodeData> b)
        {
            foreach (var sort in m_WorldTreeView.sortedColumns)
            {
                int result = sort.column.name == "Name"
                    ? string.Compare(a.data.name, b.data.name, StringComparison.Ordinal)
                    : a.data.timeMs.CompareTo(b.data.timeMs);
                if (result != 0)
                    return sort.direction == SortDirection.Ascending ? result : -result;
            }
            return 0;
        }

        public void SetData(PhysicsCore2DFrameData[] frameData, byte[] worldNames, float[] profileMarkers)
        {
            m_FrameData = frameData;
            m_WorldNames = worldNames;
            if (m_FrameData != null)
                Array.Sort(m_FrameData, (a, b) => string.Compare(WorldName(a), WorldName(b), StringComparison.Ordinal));
            m_ProfileMarkers = (profileMarkers != null && profileMarkers.Length == PhysicsCore2DProfilerMarkers.markerNames.Length)
                ? profileMarkers : null;
            Rebuild();
        }

        void Rebuild()
        {
            var grouping = m_GroupingDropdown.index switch
            {
                0 => Grouping.PerWorld,
                1 => Grouping.AllWorlds,
                _ => Grouping.ProfilerTiming,
            };
            // Whatever a reader opened or closed is remembered against the row's own default, so the tree
            // returns to the shape it was left in rather than reverting on the next frame change.
            var stateChanged = false;

            foreach (var (id, key) in m_IdToKey)
            {
                var expanded = m_WorldTreeView.IsExpanded(id);

                // A row still as the last rebuild left it was not touched, so it says nothing about its key.
                // Otherwise an untouched row in one world would undo the toggle of the same row in another.
                if (m_AppliedKeyState.TryGetValue(key, out var applied) && expanded == applied)
                    continue;

                if (IsSessionKey(key))
                {
                    if (expanded)
                        m_ExpandedSessionKeys.Add(key);
                    else
                        m_ExpandedSessionKeys.Remove(key);

                    continue;
                }

                if (expanded == DefaultExpanded(key))
                {
                    stateChanged |= m_ExpandedKeys.Remove(key);
                    stateChanged |= m_CollapsedKeys.Remove(key);
                    continue;
                }

                stateChanged |= expanded ? m_ExpandedKeys.Add(key) : m_CollapsedKeys.Add(key);
                stateChanged |= expanded ? m_CollapsedKeys.Remove(key) : m_ExpandedKeys.Remove(key);
            }

            if (stateChanged)
                SaveRowState();

            m_NextId = 0;
            m_Data.Clear();
            m_IdToKey.Clear();

            if (grouping == Grouping.ProfilerTiming)
                BuildProfilerTiming();
            else if (m_FrameData != null)
            {
                if (grouping == Grouping.PerWorld)
                    BuildPerWorld();
                else
                    BuildAllWorlds();
            }

            // Scrubbing the timeline rebuilds the rows, which loses the scroll position with them, so a row
            // being watched scrolls out of view on every frame change. The scroll offset itself is not
            // reachable from here, so the selected row is scrolled back into view instead.
            var selectedId = FirstSelectedId();

            ApplySorting(m_Data);
            m_WorldTreeView.SetRootItems(m_Data);
            m_WorldTreeView.Rebuild();
            m_AppliedKeyState.Clear();

            foreach (var (id, key) in m_IdToKey)
            {
                var expand = IsSessionKey(key)
                    ? m_ExpandedSessionKeys.Contains(key)
                    : m_ExpandedKeys.Contains(key) || (DefaultExpanded(key) && !m_CollapsedKeys.Contains(key));

                m_AppliedKeyState[key] = expand;

                if (expand)
                    m_WorldTreeView.ExpandItem(id, false);
                else
                    m_WorldTreeView.CollapseItem(id, false);
            }

            if (selectedId.HasValue)
                m_WorldTreeView.ScrollToItemById(selectedId.Value);

            ShowTree();
        }

        // A row whose state is kept for this session only, rather than stored. A timing's path is fixed and
        // worth storing; a world's name and a marker group's name are neither fixed nor bounded.
        static bool IsSessionKey(string key)
        {
            return key.StartsWith(k_WorldKeyPrefix, StringComparison.Ordinal)
                || key.StartsWith(k_MarkerKeyPrefix, StringComparison.Ordinal);
        }

        // Whether a timing row starts open. The step's own row does, so its parts are the first thing read.
        // Anything deeper starts closed, since a reader after the solver's individual stages goes looking.
        static bool DefaultExpanded(string key)
        {
            return key.IndexOf(k_KeySeparator, 1) < 0;
        }

        // The state the tree was left in last time the profiler was used, so it opens the same way again.
        static HashSet<string> LoadRowKeys(string preference)
        {
            var stored = EditorPrefs.GetString(preference, string.Empty);
            if (stored.Length == 0)
                return new HashSet<string>();

            return new HashSet<string>(stored.Split('\n'));
        }

        void SaveRowState()
        {
            EditorPrefs.SetString(k_CollapsedKeysPref, string.Join("\n", m_CollapsedKeys));
            EditorPrefs.SetString(k_ExpandedKeysPref, string.Join("\n", m_ExpandedKeys));
        }

        // The id of the selected row, or none when nothing is selected. Ids are handed out afresh on each
        // rebuild, but in the same order, so the same row keeps its id while the tree's shape holds.
        int? FirstSelectedId()
        {
            foreach (var selectedId in m_WorldTreeView.selectedIds)
                return selectedId;

            return null;
        }

        // The world's name as it stood when the frame was captured, decoded from that frame's name blob.
        // Names are captured rather than looked up now because the world may since have been renamed or
        // destroyed, and a capture from a player refers to worlds this process knows nothing about.
        string WorldName(PhysicsCore2DFrameData frameData)
        {
            if (m_WorldNames == null || frameData.worldNameLength == 0)
                return "World";

            // Guards a truncated or mismatched blob rather than throwing while a row is being built.
            if ((long)frameData.worldNameOffset + frameData.worldNameLength > m_WorldNames.Length)
                return "World";

            return Encoding.UTF8.GetString(m_WorldNames, (int)frameData.worldNameOffset, (int)frameData.worldNameLength);
        }

        // Allocate the id for an expandable node and record its expansion key, which is the node's path within the tree so it survives a rebuild.
        // Leaf rows have nothing to expand, so they take a plain id without a key.
        int RegisterNode(string expansionKey)
        {
            int id = m_NextId++;
            m_IdToKey[id] = expansionKey;
            return id;
        }

        // A world's row, with everything that world spent its time on beneath it.
        void BuildPerWorld()
        {
            for (int i = 0; i < m_FrameData.Length; i++)
            {
                var worldData = new PhysicsWorldTreeData(m_FrameData[i], WorldName(m_FrameData[i]));
                int worldId = RegisterNode($"{k_WorldKeyPrefix}{worldData.name}");
                var worldNode = TreeNodeData.CreateWorldNode(worldId, worldData);

                // The step and everything under it, built exactly as the combined view builds it, so the two
                // views are the same tree below their root and a row's key means the same thing in both.
                var timingItems = new List<TreeViewItemData<TreeNodeData>>
                {
                    BuildTimingItem(worldData.timing, string.Empty)
                };

                m_Data.Add(new TreeViewItemData<TreeNodeData>(worldId, worldNode, timingItems));
            }
        }

        // The same rows the per-world view shows beneath a world, with every world's time added together.
        // The two views differ only in whether a world level sits on top.
        void BuildAllWorlds()
        {
            var worldTimings = new List<PhysicsTimingNode>();
            for (int i = 0; i < m_FrameData.Length; i++)
                worldTimings.Add(new PhysicsWorldTreeData(m_FrameData[i], WorldName(m_FrameData[i])).timing);

            if (worldTimings.Count == 0)
                return;

            m_Data.Add(BuildTimingItem(PhysicsTimingNode.Merge(worldTimings), string.Empty));
        }

        // One timing row and everything beneath it. The expansion key is the row's path through the tree, so
        // an opened row stays open as the timeline is scrubbed.
        TreeViewItemData<TreeNodeData> BuildTimingItem(PhysicsTimingNode timing, string parentKey)
        {
            var key = $"{parentKey}{k_KeySeparator}{timing.name}";

            // A row with nothing beneath it has nothing to expand, so it takes a plain id without a key.
            int id = timing.children.Count > 0 ? RegisterNode(key) : m_NextId++;
            var node = TreeNodeData.CreateTimingNode(id, timing);

            if (timing.children.Count == 0)
                return new TreeViewItemData<TreeNodeData>(id, node);

            var childItems = new List<TreeViewItemData<TreeNodeData>>(timing.children.Count);
            foreach (var child in timing.children)
                childItems.Add(BuildTimingItem(child, key));

            return new TreeViewItemData<TreeNodeData>(id, node, childItems);
        }

        void BuildProfilerTiming()
        {
            if (m_ProfileMarkers == null)
                return;

            var markerNames = PhysicsCore2DProfilerMarkers.markerNames;
            var markerGroups = new SortedDictionary<string, List<int>>(StringComparer.Ordinal);

            for (int i = 0; i < markerNames.Length && i < m_ProfileMarkers.Length; ++i)
            {
                var groupName = MarkerGroupName(markerNames[i]);
                if (!markerGroups.TryGetValue(groupName, out var markerIndices))
                {
                    markerIndices = new List<int>();
                    markerGroups[groupName] = markerIndices;
                }

                markerIndices.Add(i);
            }

            foreach (var markerGroup in markerGroups)
            {
                float groupTotal = 0f;
                foreach (var markerIndex in markerGroup.Value)
                    groupTotal += m_ProfileMarkers[markerIndex];

                int groupId = RegisterNode($"{k_MarkerKeyPrefix}{markerGroup.Key}");
                var groupNode = new TreeNodeData(groupId, TreeNodeType.Module,
                    markerGroup.Key, $"{groupTotal:F3}ms", groupTotal);

                var markerItems = new List<TreeViewItemData<TreeNodeData>>();
                foreach (var markerIndex in markerGroup.Value)
                {
                    float time = m_ProfileMarkers[markerIndex];

                    int markerId = m_NextId++;
                    var markerNode = new TreeNodeData(markerId, TreeNodeType.TimingEntry,
                        markerNames[markerIndex], $"{time:F3}ms", time);
                    markerItems.Add(new TreeViewItemData<TreeNodeData>(markerId, markerNode));
                }

                m_Data.Add(new TreeViewItemData<TreeNodeData>(groupId, groupNode, markerItems));
            }
        }

        // The group a marker belongs to, taken from its own name: "PhysicsCore2D.PhysicsShape.CreateCircle"
        // groups under "PhysicsShape". Deriving it this way means a marker added or removed natively needs
        // nothing changed here. A name carrying no type after the module's own prefix has nothing to group
        // under, so it goes to "Miscellaneous".
        static string MarkerGroupName(string markerName)
        {
            const string markerNamePrefix = "PhysicsCore2D.";

            var start = markerName.StartsWith(markerNamePrefix, StringComparison.Ordinal) ? markerNamePrefix.Length : 0;
            var end = markerName.IndexOf('.', start);

            return end < 0 ? "Miscellaneous" : markerName.Substring(start, end - start);
        }

        void ShowTree()
        {
            bool hasData = m_Data.Count > 0;
            m_ContentContainer.style.display = hasData ? DisplayStyle.Flex : DisplayStyle.None;
            m_NoDataLabel.style.display = hasData ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
