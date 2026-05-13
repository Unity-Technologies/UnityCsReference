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

        const string k_DetailsSplitModeEditorPrefKey = "UIToolkitProfilerModule.DetailsSplitMode";
        const string k_PanelComponentRowNameLabelName = "uitoolkit-profiler-panel-component-name";
        const string k_PanelComponentRowPingButtonName = "uitoolkit-profiler-panel-component-ping";

        static class DetailsViewStrings
        {
            public static readonly string OpenFrameDebugger = L10n.Tr("Open Frame Debugger");
            public static readonly string OpenFrameDebuggerTooltip = L10n.Tr("Opens the Frame Debugger window to inspect draw calls.");
            public static readonly string OpenUiToolkitDebugger = L10n.Tr("Open UI Toolkit Debugger");
            public static readonly string OpenUiToolkitDebuggerTooltip = L10n.Tr("Opens the UI Toolkit Debugger window.");
            public static readonly string DocumentationTooltip = L10n.Tr("Opens the manual page for UI Toolkit profiler markers (Documentation/Manual/UIE-profiler-markers.html).");
            public static readonly string DetailViewModeTooltip = L10n.Tr("Show or hide UI Toolkit related data next to the panel timing list.");
            public static readonly string PanelComponentsPaneTitle = L10n.Tr("Panel components");
            public static readonly string PanelComponentsPaneTooltip = L10n.Tr("Side pane listing IPanelComponent objects from UI Toolkit frame metadata for the selected panel row.");
            public static readonly string StatusSelectPanelRow = L10n.Tr("Select a panel row on the left to list IPanelComponent objects stored in this frame's UI Toolkit metadata.");
            public static readonly string StatusNoMetadataEntry = L10n.Tr("No UI Toolkit panel metadata entry for this panel in the selected frame.");
            public static readonly string StatusNoPanelComponents = L10n.Tr("No panel components listed for this panel in metadata (expected for editor panels, or runtime panels with no IPanelComponent instances).");
            public static readonly string StatusListEmptyCouldNotLoadDetails = L10n.Tr("Panel components are present in frame metadata but details could not be loaded for this frame.");
            public static readonly string PingTooltip = L10n.Tr("Ping");
        }

        static void ApplyToolbarButtonTextAndTooltip(ToolbarButton button, string text, string tooltip)
        {
            button.text = text;
            button.tooltip = tooltip;
        }

        enum DetailsSplitMode
        {
            NoDetails = 0,
            RelatedData = 1,
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

        long m_LastFrameIndex = -1;
        readonly Dictionary<EntityId, List<EntityId>> m_PanelToPanelComponentEntityIds = new Dictionary<EntityId, List<EntityId>>();
        readonly Stack<List<EntityId>> m_PanelComponentIdListPool = new Stack<List<EntityId>>();
        readonly List<PanelComponentListEntry> m_PanelComponentListEntries = new List<PanelComponentListEntry>();
        readonly Stack<PanelComponentListEntry> m_PanelComponentListEntryPool = new Stack<PanelComponentListEntry>();

        TwoPaneSplitView m_MainRelatedSplitView;
        VisualElement m_RelatedDataColumn;
        ToolbarToggle m_DetailsToolbarToggle;
        DetailsSplitMode m_DetailsSplitMode = DetailsSplitMode.RelatedData;
        ListView m_PanelComponentsListView;
        Label m_PanelComponentsHeading;
        Label m_PanelComponentsListEmptyLabel;
        IVisualElementScheduledItem m_PanelComponentsPaneRefreshScheduleItem;

        sealed class PanelComponentListEntry
        {
            public EntityId entityId;
            public string displayName;
        }

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
                optional = false,
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
                    width = 120,
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
            m_ListView.selectedIndicesChanged += OnPanelRowSelectionChanged;

            m_PanelComponentsListView = new ListView
            {
                fixedItemHeight = 18,
                makeItem = CreatePanelComponentListRow,
                bindItem = BindPanelComponentListRow,
                makeNoneElement = CreatePanelComponentsListViewEmptyElement,
                selectionType = SelectionType.Single,
                style = { flexGrow = 1 },
                viewDataKey = "uitoolkit-profiler-panel-components-list",
            };
            m_PanelComponentsListView.itemsChosen += OnPanelComponentsItemsChosen;

            var panelComponentsPane = new VisualElement { name = "uitoolkit-profiler-panel-components-pane" };
            panelComponentsPane.style.flexDirection = FlexDirection.Column;
            panelComponentsPane.style.flexGrow = 1;
            m_PanelComponentsHeading = new Label(DetailsViewStrings.PanelComponentsPaneTitle)
            {
                tooltip = DetailsViewStrings.PanelComponentsPaneTooltip,
                focusable = false,
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    paddingLeft = 4,
                    paddingTop = 4,
                    paddingRight = 4,
                    paddingBottom = 4,
                    whiteSpace = WhiteSpace.Normal,
                },
            };
            panelComponentsPane.Add(m_PanelComponentsHeading);
            panelComponentsPane.Add(m_PanelComponentsListView);

            m_DetailsToolbarToggle = new ToolbarToggle
            {
                text = string.Empty,
                tooltip = DetailsViewStrings.DetailViewModeTooltip,
                viewDataKey = "uitoolkit-profiler-details-view-mode-toggle",
                style = { flexShrink = 0 },
            };
            m_DetailsToolbarToggle.Add(new Image { image = EditorGUIUtility.LoadIconRequired("UnityEditor.InspectorWindow"), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore });
            m_DetailsToolbarToggle.RegisterValueChangedCallback(evt =>
            {
                SetDetailsSplitMode(evt.newValue ? DetailsSplitMode.RelatedData : DetailsSplitMode.NoDetails);
            });

            var mainToolbar = new Toolbar();
            mainToolbar.Add(new ToolbarSpacer { flex = true });

            var openFrameDebuggerButton = new ToolbarButton(OpenFrameDebugger);
            ApplyToolbarButtonTextAndTooltip(openFrameDebuggerButton, DetailsViewStrings.OpenFrameDebugger, DetailsViewStrings.OpenFrameDebuggerTooltip);
            mainToolbar.Add(openFrameDebuggerButton);

            var openUiToolkitDebuggerButton = new ToolbarButton(OpenUIToolkitDebugger);
            ApplyToolbarButtonTextAndTooltip(openUiToolkitDebuggerButton, DetailsViewStrings.OpenUiToolkitDebugger, DetailsViewStrings.OpenUiToolkitDebuggerTooltip);
            mainToolbar.Add(openUiToolkitDebuggerButton);

            var docButton = new ToolbarButton(OpenUiToolkitProfilerMarkersDocumentation)
            {
                iconImage = Background.FromTexture2D(EditorGUIUtility.LoadIcon("_Help")),
                text = string.Empty,
            };
            docButton.tooltip = DetailsViewStrings.DocumentationTooltip;
            mainToolbar.Add(docButton);

            mainToolbar.Add(m_DetailsToolbarToggle);

            m_RelatedDataColumn = new VisualElement { name = "uitoolkit-profiler-related-data-column" };
            m_RelatedDataColumn.viewDataKey = "uitoolkit-profiler-related-data-column";
            m_RelatedDataColumn.style.flexDirection = FlexDirection.Column;
            m_RelatedDataColumn.style.flexGrow = 1;
            m_RelatedDataColumn.style.minWidth = 120;
            m_RelatedDataColumn.Add(panelComponentsPane);

            var leftColumn = new VisualElement { name = "uitoolkit-profiler-main-column" };
            leftColumn.style.flexDirection = FlexDirection.Column;
            leftColumn.style.flexGrow = 1;
            leftColumn.style.minWidth = 0;
            leftColumn.Add(m_ListView);

            m_MainRelatedSplitView = new TwoPaneSplitView(1, 280, TwoPaneSplitViewOrientation.Horizontal)
            {
                name = "uitoolkit-profiler-main-related-split",
                viewDataKey = "uitoolkit-profiler-main-related-split",
            };
            m_MainRelatedSplitView.Add(leftColumn);
            m_MainRelatedSplitView.Add(m_RelatedDataColumn);

            var root = new VisualElement { name = "uitoolkit-profiler-details-root" };
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1;
            root.Add(mainToolbar);
            root.Add(m_MainRelatedSplitView);

            var savedMode = (DetailsSplitMode)EditorPrefs.GetInt(k_DetailsSplitModeEditorPrefKey, (int)DetailsSplitMode.RelatedData);
            if (savedMode != DetailsSplitMode.NoDetails && savedMode != DetailsSplitMode.RelatedData)
                savedMode = DetailsSplitMode.RelatedData;
            SetDetailsSplitMode(savedMode, persistSelection: false);

            ReloadData(ProfilerWindow.selectedFrameIndex);
            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            root.schedule.Execute(() =>
            {
                if (m_DetailsSplitMode == DetailsSplitMode.NoDetails && m_MainRelatedSplitView != null)
                    m_MainRelatedSplitView.CollapseChild(1);
            });

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
                m_ListView.selectedIndicesChanged -= OnPanelRowSelectionChanged;
            }
            if (m_PanelComponentsListView != null)
                m_PanelComponentsListView.itemsChosen -= OnPanelComponentsItemsChosen;
            m_ApplyListSourceScheduled?.Pause();
            m_ApplyListSourceScheduled = null;
            m_PanelComponentsPaneRefreshScheduleItem?.Pause();
            m_PanelComponentsPaneRefreshScheduleItem = null;
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

        void OnPanelComponentsItemsChosen(IEnumerable<object> chosen)
        {
            foreach (var item in chosen)
            {
                if (item is PanelComponentListEntry entry && entry.entityId != EntityId.None)
                    PingEntity(entry.entityId);
                break;
            }
        }

        VisualElement CreatePanelComponentsListViewEmptyElement()
        {
            var label = new Label
            {
                focusable = false,
                style =
                {
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 4,
                    paddingBottom = 4,
                    whiteSpace = WhiteSpace.Normal,
                    flexGrow = 1,
                },
            };
            m_PanelComponentsListEmptyLabel = label;
            return label;
        }

        void SyncPanelComponentsListEmptyMessage(string message)
        {
            if (m_PanelComponentsListEmptyLabel != null)
                m_PanelComponentsListEmptyLabel.text = message;
        }

        VisualElement CreatePanelComponentListRow()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.flexGrow = 1;
            container.style.alignItems = Align.Center;
            container.style.minWidth = 0;

            var nameLabel = new Label { name = k_PanelComponentRowNameLabelName, focusable = false };
            nameLabel.style.flexGrow = 1;
            nameLabel.style.flexShrink = 1;
            nameLabel.style.minWidth = 0;
            nameLabel.style.overflow = Overflow.Hidden;
            nameLabel.style.textOverflow = TextOverflow.Ellipsis;

            var pingBtn = new UnityEngine.UIElements.Button { name = k_PanelComponentRowPingButtonName, text = string.Empty };
            pingBtn.style.backgroundImage = Background.FromTexture2D(EditorGUIUtility.LoadIconRequired("UIPackageResources/Images/pick_uielements.png"));
            pingBtn.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            pingBtn.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            pingBtn.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            pingBtn.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            pingBtn.style.alignItems = Align.FlexEnd;
            pingBtn.tooltip = DetailsViewStrings.PingTooltip;
            pingBtn.style.width = 22;
            pingBtn.style.minWidth = 22;
            pingBtn.style.maxWidth = 22;
            pingBtn.style.flexShrink = 0;
            pingBtn.clicked += () =>
            {
                if (container.userData is int rowIndex && rowIndex >= 0 && rowIndex < m_PanelComponentListEntries.Count)
                    m_PanelComponentsListView.SetSelection(rowIndex);
                if (pingBtn.userData is EntityId id && id != EntityId.None)
                    PingEntity(id);
            };

            container.Add(nameLabel);
            container.Add(pingBtn);
            return container;
        }

        void BindPanelComponentListRow(VisualElement element, int index)
        {
            var nameLabel = element.Q<Label>(k_PanelComponentRowNameLabelName);
            var pingBtn = element.Q<UnityEngine.UIElements.Button>(k_PanelComponentRowPingButtonName);
            if (nameLabel == null || pingBtn == null)
                return;

            if (index >= 0 && index < m_PanelComponentListEntries.Count)
            {
                var entry = m_PanelComponentListEntries[index];
                element.userData = index;
                nameLabel.text = entry.displayName;
                pingBtn.userData = entry.entityId;
                pingBtn.SetEnabled(entry.entityId != EntityId.None);
            }
            else
            {
                element.userData = -1;
                nameLabel.text = string.Empty;
                pingBtn.userData = null;
                pingBtn.SetEnabled(false);
            }
        }

        void OnPanelRowSelectionChanged(IEnumerable<int> selectedIndices)
        {
            ScheduleRefreshPanelComponentsPane();
        }

        void SetDetailsSplitMode(DetailsSplitMode mode, bool persistSelection = true)
        {
            if (m_MainRelatedSplitView == null || m_DetailsToolbarToggle == null)
                return;

            m_DetailsSplitMode = mode;
            if (persistSelection)
                EditorPrefs.SetInt(k_DetailsSplitModeEditorPrefKey, (int)mode);

            m_DetailsToolbarToggle.SetValueWithoutNotify(mode == DetailsSplitMode.RelatedData);

            if (mode == DetailsSplitMode.RelatedData)
            {
                m_MainRelatedSplitView.UnCollapse();
                ScheduleRefreshPanelComponentsPane();
            }
            else
                m_MainRelatedSplitView.CollapseChild(1);
        }

        void ReleaseAllPanelComponentEntityIdLists()
        {
            foreach (var list in m_PanelToPanelComponentEntityIds.Values)
            {
                list.Clear();
                m_PanelComponentIdListPool.Push(list);
            }
            m_PanelToPanelComponentEntityIds.Clear();
        }

        List<EntityId> RentPanelComponentIdList()
        {
            return m_PanelComponentIdListPool.Count > 0 ? m_PanelComponentIdListPool.Pop() : new List<EntityId>();
        }

        void LoadPanelComponentMetadata(RawFrameDataView rawFrameData)
        {
            ReleaseAllPanelComponentEntityIdLists();
            if (rawFrameData == null || !rawFrameData.valid)
                return;

            var guid = ProfilerUIToolkit.kProfilerMetadataGuid;
            var tag = ProfilerUIToolkit.kProfilerUIToolkitMetadataTagPanelEntries;
            var chunkCount = rawFrameData.GetFrameMetaDataCount(guid, tag);
            for (var chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
            {
                using (var ids = rawFrameData.GetFrameMetaData<EntityId>(guid, tag, chunkIndex))
                {
                    if (ids.Length < 1)
                        continue;
                    var panelEntityId = ids[0];
                    if (!m_PanelToPanelComponentEntityIds.TryGetValue(panelEntityId, out var list))
                    {
                        list = RentPanelComponentIdList();
                        m_PanelToPanelComponentEntityIds[panelEntityId] = list;
                    }
                    for (var j = 1; j < ids.Length; j++)
                    {
                        if (ids[j] != EntityId.None)
                            list.Add(ids[j]);
                    }
                }
            }
        }

        void ClearPanelComponentListEntriesReturningToPool()
        {
            for (var i = 0; i < m_PanelComponentListEntries.Count; i++)
                m_PanelComponentListEntryPool.Push(m_PanelComponentListEntries[i]);
            m_PanelComponentListEntries.Clear();
        }

        PanelComponentListEntry RentPanelComponentListEntry()
        {
            return m_PanelComponentListEntryPool.Count > 0 ? m_PanelComponentListEntryPool.Pop() : new PanelComponentListEntry();
        }

        void TrimExcessPanelComponentListEntries(int targetCount)
        {
            while (m_PanelComponentListEntries.Count > targetCount)
            {
                var lastIndex = m_PanelComponentListEntries.Count - 1;
                m_PanelComponentListEntryPool.Push(m_PanelComponentListEntries[lastIndex]);
                m_PanelComponentListEntries.RemoveAt(lastIndex);
            }
        }

        int GetFirstSelectedPanelRowIndex()
        {
            foreach (var index in m_ListView.selectedIndices)
            {
                if (index >= 0 && index < m_Rows.Count)
                    return index;
            }
            return -1;
        }

        void ScheduleRefreshPanelComponentsPane()
        {
            if (m_ListView == null)
                return;

            m_PanelComponentsPaneRefreshScheduleItem?.Pause();
            m_PanelComponentsPaneRefreshScheduleItem = m_ListView.schedule.Execute(() =>
            {
                m_PanelComponentsPaneRefreshScheduleItem = null;
                RefreshPanelComponentsPane();
            });
        }

        void SetPanelComponentsHeadingForSelection()
        {
            if (m_PanelComponentsHeading == null)
                return;

            var rowIndex = GetFirstSelectedPanelRowIndex();
            if (rowIndex < 0)
            {
                m_PanelComponentsHeading.text = DetailsViewStrings.PanelComponentsPaneTitle;
                return;
            }

            var panelName = m_Rows[rowIndex].panelName;
            var panelEntityId = m_Rows[rowIndex].panelEntityId;
            if (!m_PanelToPanelComponentEntityIds.TryGetValue(panelEntityId, out var componentIds) || componentIds.Count == 0)
            {
                m_PanelComponentsHeading.text = string.Format(L10n.Tr("{0}: {1}"), DetailsViewStrings.PanelComponentsPaneTitle, panelName);
                return;
            }

            var count = componentIds.Count;
            if (count == 1)
                m_PanelComponentsHeading.text = string.Format(L10n.Tr("{0} panel component using {1}"), count, panelName);
            else
                m_PanelComponentsHeading.text = string.Format(L10n.Tr("{0} panel components using {1}"), count, panelName);
        }

        void RefreshPanelComponentsPane()
        {
            if (m_PanelComponentsListView == null)
                return;

            if (m_DetailsSplitMode != DetailsSplitMode.RelatedData)
                return;

            var rowIndex = GetFirstSelectedPanelRowIndex();
            SetPanelComponentsHeadingForSelection();

            if (rowIndex < 0)
            {
                ClearPanelComponentListEntriesReturningToPool();
                m_PanelComponentsListView.itemsSource = m_PanelComponentListEntries;
                m_PanelComponentsListView.Rebuild();
                SyncPanelComponentsListEmptyMessage(DetailsViewStrings.StatusSelectPanelRow);
                return;
            }

            var panelEntityId = m_Rows[rowIndex].panelEntityId;
            if (!m_PanelToPanelComponentEntityIds.TryGetValue(panelEntityId, out var componentIds))
            {
                ClearPanelComponentListEntriesReturningToPool();
                m_PanelComponentsListView.itemsSource = m_PanelComponentListEntries;
                m_PanelComponentsListView.Rebuild();
                SyncPanelComponentsListEmptyMessage(DetailsViewStrings.StatusNoMetadataEntry);
                return;
            }

            if (componentIds.Count == 0)
            {
                ClearPanelComponentListEntriesReturningToPool();
                m_PanelComponentsListView.itemsSource = m_PanelComponentListEntries;
                m_PanelComponentsListView.Rebuild();
                SyncPanelComponentsListEmptyMessage(DetailsViewStrings.StatusNoPanelComponents);
                return;
            }

            var neededCount = componentIds.Count;
            TrimExcessPanelComponentListEntries(neededCount);
            if (m_LastFrameIndex >= 0)
            {
                using (var rawFrameData = ProfilerDriver.GetRawFrameDataView((int)m_LastFrameIndex, k_MainThreadIndex))
                {
                    if (rawFrameData.valid)
                    {
                        for (var i = 0; i < neededCount; i++)
                        {
                            PanelComponentListEntry entry;
                            if (i < m_PanelComponentListEntries.Count)
                                entry = m_PanelComponentListEntries[i];
                            else
                            {
                                entry = RentPanelComponentListEntry();
                                m_PanelComponentListEntries.Add(entry);
                            }

                            var componentId = componentIds[i];
                            entry.entityId = componentId;
                            entry.displayName = GetPanelDisplayName(rawFrameData, componentId);
                        }
                    }
                    else
                        ClearPanelComponentListEntriesReturningToPool();
                }
            }
            else
                ClearPanelComponentListEntriesReturningToPool();

            m_PanelComponentsListView.itemsSource = m_PanelComponentListEntries;
            m_PanelComponentsListView.Rebuild();
            if (m_PanelComponentListEntries.Count == 0)
                SyncPanelComponentsListEmptyMessage(DetailsViewStrings.StatusListEmptyCouldNotLoadDetails);
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
                m_LastFrameIndex = -1;
                ReleaseAllPanelComponentEntityIdLists();
                ApplyListViewSource();
                return;
            }

            using (var hierarchyData = ProfilerDriver.GetHierarchyFrameDataView(
                (int)frameIndex, k_MainThreadIndex,
                HierarchyFrameDataView.ViewModes.Default,
                HierarchyFrameDataView.columnDontSort, false))
            using (var rawFrameData = ProfilerDriver.GetRawFrameDataView((int)frameIndex, k_MainThreadIndex))
            {
                LoadPanelComponentMetadata(rawFrameData);

                if (!hierarchyData.valid)
                {
                    m_LastFrameIndex = frameIndex;
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

            m_LastFrameIndex = frameIndex;
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
            ScheduleRefreshPanelComponentsPane();
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
