// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditor.UIElements;
using UnityEditor.UIElements.Debugger;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Internal;
using static UnityEditor.EditorGUI;

namespace UnityEditor.UIElements
{
    internal class UIToolkitProfilerModuleDetailsView : ProfilerModuleViewController
    {
        const int k_MainThreadIndex = 0;

        static string GetPanelDisplayName(RawFrameDataView frameData, EntityId entityId)
        {
            if (entityId == EntityId.None)
                return L10n.Tr("(Unknown)");
            if (frameData != null && frameData.GetUnityObjectInfo(entityId, out var info))
            {
                if (!string.IsNullOrEmpty(info.name))
                    return info.name;
                if (info.nativeTypeIndex >= 0 && info.nativeTypeIndex < frameData.GetUnityObjectNativeTypeInfoCount() &&
                    frameData.GetUnityObjectNativeTypeInfo(info.nativeTypeIndex, out var typeInfo) && !string.IsNullOrEmpty(typeInfo.name))
                    return typeInfo.name;
            }
            // fallback to using the EditorUtility to get the object name, which can work for editor-only objects that don't show up in the frame data
            var obj = InternalEditorUtility.GetObjectFromEntityId(entityId);
            if (obj != null)
            {
                if (!string.IsNullOrEmpty(obj.name))
                    return obj.name;
                return obj.GetType().Name;
            }
            return EntityId.ToULong(entityId).ToString();
        }

        static void OpenFrameDebugger()
        {
            FrameDebuggerWindow.OpenWindow();
        }

        static void OpenUIToolkitDebugger()
        {
            UIElementsDebugger.OpenAndInspectWindow(null);
        }

        static void OpenUiToolkitProfilerMarkersDocumentation()
        {
            const string kManualRelativePath = "UIE-profiler-markers";
            var url = Help.FindHelpNamed(kManualRelativePath);
            if (!string.IsNullOrEmpty(url))
                Help.BrowseURL(url);
        }

        static void PingEntity(EntityId id)
        {
            var obj = InternalEditorUtility.GetObjectFromEntityId(id);
            if (obj != null)
            {
                if (obj is DockArea dockArea)
                    dockArea.actualView?.Focus();
                else if (obj is EditorWindow window)
                    window.Focus();
                else
                { 
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }
        }

        readonly ProfilerCounterDescriptor[] m_Counters;
        readonly string[] m_HierarchyMarkerNames;
        readonly string[] m_ColumnDisplayTitles;
        MultiColumnListView m_ListView;
        List<PanelDetailRow> m_Rows = new List<PanelDetailRow>();
        readonly List<int> m_ChildItemIdsScratch = new List<int>(64);
        readonly Stack<int> m_HierarchyWalkStack = new Stack<int>(256);
        IVisualElementScheduledItem m_ApplyListSourceScheduled;

        // Reusable collections for ReloadData; cleared each frame instead of re-allocated.
        readonly Dictionary<int, int> m_MarkerIdToCounterIndex = new Dictionary<int, int>();
        readonly Dictionary<(EntityId, int), float> m_PanelMarkerTimes = new Dictionary<(EntityId, int), float>();
        readonly List<EntityId> m_PanelOrder = new List<EntityId>();
        readonly HashSet<EntityId> m_PanelsSeen = new HashSet<EntityId>();

        internal struct PanelDetailRow
        {
            public EntityId panelEntityId;
            public string panelName;
            public float[] counterTimesMs;
            public float totalTimeMs;
        }

        public UIToolkitProfilerModuleDetailsView(ProfilerWindow profilerWindow, ProfilerCounterDescriptor[] counters, string[] hierarchyMarkerNames, string[] columnDisplayTitles)
            : base(profilerWindow)
        {
            UnityEngine.Debug.Assert(counters != null && hierarchyMarkerNames != null && columnDisplayTitles != null
                && counters.Length == hierarchyMarkerNames.Length && counters.Length == columnDisplayTitles.Length,
                "Profiler counters, hierarchy marker names, and column display titles must be parallel arrays of equal length.");
            m_Counters = counters;
            m_HierarchyMarkerNames = hierarchyMarkerNames;
            m_ColumnDisplayTitles = columnDisplayTitles;
        }

        static string BuildColumnHeaderTooltip(ProfilerCounterDescriptor counter, string hierarchyMarkerName)
        {
            var desc = counter.Description;
            if (!string.IsNullOrEmpty(desc))
                return string.Format(L10n.Tr("{0}\n\nProfiler marker: {1}"), desc, hierarchyMarkerName);
            return string.Format(L10n.Tr("Profiler marker: {0}"), hierarchyMarkerName);
        }

        /// <summary>
        /// Same structure as <see cref="UnityEngine.UIElements.Internal.MultiColumnHeaderColumn"/>.CreateDefaultHeaderContent.
        /// Required when using a custom <see cref="Column.bindHeader"/>; otherwise <c>makeHeader == null</c> causes the
        /// default implementation to replace <c>bindHeader</c> with DefaultBindHeaderContent.
        /// </summary>
        static VisualElement CreateDefaultProfilerDetailsColumnHeaderContent()
        {
            var defContent = new VisualElement() { pickingMode = PickingMode.Ignore };
            defContent.AddToClassList(MultiColumnHeaderColumn.defaultContentUssClassName);

            var icon = new Image() { name = MultiColumnHeaderColumn.iconElementName, pickingMode = PickingMode.Ignore };

            var title = new Label() { name = MultiColumnHeaderColumn.titleElementName, pickingMode = PickingMode.Ignore };
            title.AddToClassList(MultiColumnHeaderColumn.titleUssClassName);

            defContent.Add(icon);
            defContent.Add(title);

            return defContent;
        }

        /// <summary>
        /// Mirrors default multi-column header binding (<see cref="UnityEngine.UIElements.Internal.MultiColumnHeaderColumn"/>) and adds tooltips.
        /// </summary>
        static void BindProfilerDetailsColumnHeader(VisualElement headerContent, Column column, string tooltipText)
        {
            var title = headerContent.Q<Label>(MultiColumnHeaderColumn.titleElementName);
            var icon = headerContent.Q<Image>(MultiColumnHeaderColumn.iconElementName);

            headerContent.RemoveFromClassList(MultiColumnHeaderColumn.hasTitleUssClassName);

            if (title != null)
            {
                title.text = column.title;
                title.tooltip = tooltipText;
            }

            if (!string.IsNullOrEmpty(column.title))
                headerContent.AddToClassList(MultiColumnHeaderColumn.hasTitleUssClassName);

            if (icon != null)
            {
                if (column.icon.texture != null || column.icon.sprite != null || column.icon.vectorImage != null)
                {
                    icon.image = column.icon.texture;
                    icon.sprite = column.icon.sprite;
                    icon.vectorImage = column.icon.vectorImage;
                }
                else
                {
                    icon.image = null;
                    icon.sprite = null;
                    icon.vectorImage = null;
                }
            }

            headerContent.parent.tooltip = tooltipText;
        }

        protected override VisualElement CreateView()
        {
            var columns = new Columns { reorderable = true };
            columns.Add(new Column
            {
                name = "panel",
                title = L10n.Tr("Panel"),
                minWidth = 120,
                comparison = (a, b) => EntityId.ToULong(m_Rows[a].panelEntityId).CompareTo(EntityId.ToULong(m_Rows[b].panelEntityId)),
                bindCell = (e, row) => ((Label)e).text = row < m_Rows.Count ? m_Rows[row].panelName : ""
            });

            for (var i = 0; i < m_Counters.Length; i++)
            {
                var counterIndex = i;
                var headerTooltip = BuildColumnHeaderTooltip(m_Counters[i], m_HierarchyMarkerNames[i]);
                var column = new Column
                {
                    name = m_Counters[i].Name,
                    title = m_ColumnDisplayTitles[i],
                    minWidth = 80,
                    makeHeader = CreateDefaultProfilerDetailsColumnHeaderContent,
                    comparison = (a, b) =>
                    {
                        var ta = rowTimeMs(a, counterIndex);
                        var tb = rowTimeMs(b, counterIndex);
                        return ta.CompareTo(tb);
                    },
                    bindCell = (e, row) =>
                    {
                        var label = (Label)e;
                        if (row < m_Rows.Count && counterIndex < m_Rows[row].counterTimesMs.Length)
                            label.text = FormatTime(m_Rows[row].counterTimesMs[counterIndex]);
                        else
                            label.text = "—";
                    }
                };
                column.bindHeader = (ve) => BindProfilerDetailsColumnHeader(ve, column, headerTooltip);
                columns.Add(column);
            }

            columns.Add(new Column
            {
                name = "total",
                title = L10n.Tr("Total"),
                minWidth = 80,
                comparison = (a, b) => m_Rows[a].totalTimeMs.CompareTo(m_Rows[b].totalTimeMs),
                bindCell = (e, row) =>
                {
                    var label = (Label)e;
                    label.text = row < m_Rows.Count ? FormatTime(m_Rows[row].totalTimeMs) : "—";
                }
            });

            m_ListView = new MultiColumnListView(columns)
            {
                fixedItemHeight = 18,
                reorderable = true,
                sortingMode = ColumnSortingMode.Default,
                viewDataKey = "uitoolkit-profiler-details-listview",
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
            };

            m_ListView.makeNoneElement = () =>
            {
                var label = new Label(L10n.Tr("No data to show. Start profiling UI Toolkit content to see details."));
                label.style.flexGrow = 1;
                return label;
            };

            m_ListView.columnSortingChanged += OnColumnSortingChanged;
            m_ListView.itemsChosen += OnItemsChosen;

            ReloadData(ProfilerWindow.selectedFrameIndex);
            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarSpacer { flex = true });

            var openFrameDebuggerContent = EditorGUIUtility.TrTextContent("Open Frame Debugger", "Opens the Frame Debugger window to inspect draw calls.");
            toolbar.Add(new ToolbarButton(OpenFrameDebugger)
            {
                text = openFrameDebuggerContent.text,
                tooltip = openFrameDebuggerContent.tooltip,
            });
            var openUitkDebuggerContent = EditorGUIUtility.TrTextContent("Open UI Toolkit Debugger", "Opens the UI Toolkit Debugger window.");
            toolbar.Add(new ToolbarButton(OpenUIToolkitDebugger)
            {
                text = openUitkDebuggerContent.text,
                tooltip = openUitkDebuggerContent.tooltip,
            });
            var docHelpContent = EditorGUIUtility.TrTextContent("Documentation", "Opens the manual page for UI Toolkit profiler markers (Documentation/Manual/UIE-profiler-markers.html).");
            var docButton = new ToolbarButton(OpenUiToolkitProfilerMarkersDocumentation)
            {
                iconImage = Background.FromTexture2D(GUIContents.helpIcon.image as Texture2D),
                text = string.Empty,
                tooltip = docHelpContent.tooltip,
            };

            toolbar.Add(docButton);

            var root = new VisualElement { name = "uitoolkit-profiler-details-root" };
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1;
            root.Add(toolbar);
            root.Add(m_ListView);

            return root;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (m_ListView != null)
            {
                m_ListView.itemsChosen -= OnItemsChosen;
                m_ListView.columnSortingChanged -= OnColumnSortingChanged;
            }
            m_ApplyListSourceScheduled?.Pause();
            m_ApplyListSourceScheduled = null;
            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
            base.Dispose(disposing);
        }

        void OnColumnSortingChanged()
        {
            ScheduleApplyListViewSource();
        }

        void OnItemsChosen(IEnumerable<object> chosen)
        {
            foreach (var item in chosen)
            {
                if (item is PanelDetailRow row && row.panelEntityId != EntityId.None)
                {
                    PingEntity(row.panelEntityId);
                    return;
                }
            }
        }

        void OnSelectedFrameIndexChanged(long frame)
        {
            ReloadData(frame);
        }

        void ReloadData(long frameIndex)
        {
            m_Rows.Clear();

            if (frameIndex < 0)
            {
                ApplyListViewSource();
                return;
            }

            using (var hierarchyData = ProfilerDriver.GetHierarchyFrameDataView(
                (int)frameIndex, k_MainThreadIndex,
                HierarchyFrameDataView.ViewModes.Default,
                HierarchyFrameDataView.columnDontSort, false))
            using (var rawFrameData = ProfilerDriver.GetRawFrameDataView((int)frameIndex, k_MainThreadIndex))
            {
                if (!hierarchyData.valid)
                {
                    ApplyListViewSource();
                    return;
                }

                m_MarkerIdToCounterIndex.Clear();
                for (var i = 0; i < m_HierarchyMarkerNames.Length; i++)
                {
                    var markerId = hierarchyData.GetMarkerId(m_HierarchyMarkerNames[i]);
                    if (markerId != FrameDataView.invalidMarkerId)
                        m_MarkerIdToCounterIndex[markerId] = i;
                }

                m_PanelMarkerTimes.Clear();
                m_PanelOrder.Clear();
                m_PanelsSeen.Clear();

                CollectUpdaterSamplesFromHierarchy(hierarchyData, hierarchyData.GetRootItemID());

                BuildRows(rawFrameData);
            }

            ApplyListViewSource();
        }

        void ApplyListViewSource()
        {
            ScheduleApplyListViewSource();
        }

        /// <summary>
        /// Defer binding + <see cref="BaseVerticalCollectionView.Rebuild"/> to the next scheduler tick.
        /// Profiler frame changes can fire during layout (e.g. IMGUI toolbar); rebuilding the list then throws
        /// <c>Cannot modify VisualElement hierarchy during layout calculation</c>.
        /// </summary>
        void ScheduleApplyListViewSource()
        {
            if (m_ListView == null)
                return;

            if (m_ListView.panel == null)
            {
                m_ListView.itemsSource = m_Rows;
                m_ListView.Rebuild();
                return;
            }

            if (m_ApplyListSourceScheduled == null)
                m_ApplyListSourceScheduled = m_ListView.schedule.Execute(DeferredApplyListViewSource);
            else if (!m_ApplyListSourceScheduled.isActive)
                m_ApplyListSourceScheduled.Resume();
        }

        void DeferredApplyListViewSource()
        {
            if (m_ListView == null || m_ListView.panel == null)
                return;
            m_ListView.itemsSource = m_Rows;
            m_ListView.Rebuild();
        }

        void CollectUpdaterSamplesFromHierarchy(HierarchyFrameDataView hierarchyData, int rootItemId)
        {
            m_HierarchyWalkStack.Clear();
            m_HierarchyWalkStack.Push(rootItemId);
            while (m_HierarchyWalkStack.Count > 0)
            {
                var itemId = m_HierarchyWalkStack.Pop();
                var itemMarkerId = hierarchyData.GetItemMarkerID(itemId);
                if (m_MarkerIdToCounterIndex.TryGetValue(itemMarkerId, out var markerIndex))
                {
                    var entityId = hierarchyData.GetItemEntityId(itemId);
                    var key = (entityId, markerIndex);
                    var timeMs = hierarchyData.GetItemColumnDataAsFloat(itemId, HierarchyFrameDataView.columnTotalTime);
                    if (m_PanelMarkerTimes.TryGetValue(key, out var existing))
                        m_PanelMarkerTimes[key] = existing + timeMs;
                    else
                    {
                        m_PanelMarkerTimes[key] = timeMs;
                        if (m_PanelsSeen.Add(entityId))
                            m_PanelOrder.Add(entityId);
                    }
                }

                if (!hierarchyData.HasItemChildren(itemId))
                    continue;

                m_ChildItemIdsScratch.Clear();
                hierarchyData.GetItemChildren(itemId, m_ChildItemIdsScratch);
                for (var i = m_ChildItemIdsScratch.Count - 1; i >= 0; i--)
                    m_HierarchyWalkStack.Push(m_ChildItemIdsScratch[i]);
            }
        }

        float rowTimeMs(int row, int counterIndex)
        {
            if (row < 0 || row >= m_Rows.Count || counterIndex >= m_Rows[row].counterTimesMs.Length)
                return 0f;
            return m_Rows[row].counterTimesMs[counterIndex];
        }

        void BuildRows(RawFrameDataView rawFrameData)
        {
            foreach (var panelEntityId in m_PanelOrder)
            {
                var panelName = GetPanelDisplayName(rawFrameData, panelEntityId);
                var times = new float[m_Counters.Length];
                var total = 0f;
                for (var i = 0; i < m_Counters.Length; i++)
                {
                    m_PanelMarkerTimes.TryGetValue((panelEntityId, i), out times[i]);
                    total += times[i];
                }

                m_Rows.Add(new PanelDetailRow
                {
                    panelEntityId = panelEntityId,
                    panelName = panelName,
                    counterTimesMs = times,
                    totalTimeMs = total
                });
            }
        }

        static string FormatTime(float timeMs) => $"{timeMs:F2} ms";
    }
}
