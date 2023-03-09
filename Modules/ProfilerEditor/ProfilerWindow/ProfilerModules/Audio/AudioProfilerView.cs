// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    internal class AudioProfilerGroupInfoWrapper
    {
        public AudioProfilerGroupInfo info;
        public string assetName;
        public string objectName;
        public string parentName;
        public bool addToRoot;

        public AudioProfilerGroupInfoWrapper(AudioProfilerGroupInfo info, string assetName, string objectName, bool addToRoot)
        {
            this.info = info;
            this.assetName = assetName;
            this.objectName = objectName;
            this.addToRoot = addToRoot;
        }
    }

    internal class AudioProfilerGroupInfoHelper
    {
        public const int AUDIOPROFILER_FLAGS_3D              = 0x00000001;
        public const int AUDIOPROFILER_FLAGS_ISSPATIAL       = 0x00000002;
        public const int AUDIOPROFILER_FLAGS_PAUSED          = 0x00000004;
        public const int AUDIOPROFILER_FLAGS_MUTED           = 0x00000008;
        public const int AUDIOPROFILER_FLAGS_VIRTUAL         = 0x00000010;
        public const int AUDIOPROFILER_FLAGS_ONESHOT         = 0x00000020;
        public const int AUDIOPROFILER_FLAGS_GROUP           = 0x00000040;
        public const int AUDIOPROFILER_FLAGS_STREAM          = 0x00000080;
        public const int AUDIOPROFILER_FLAGS_COMPRESSED      = 0x00000100;
        public const int AUDIOPROFILER_FLAGS_LOOPED          = 0x00000200;
        public const int AUDIOPROFILER_FLAGS_OPENMEMORY      = 0x00000400;
        public const int AUDIOPROFILER_FLAGS_OPENMEMORYPOINT = 0x00000800;
        public const int AUDIOPROFILER_FLAGS_OPENUSER        = 0x00001000;
        public const int AUDIOPROFILER_FLAGS_NONBLOCKING     = 0x00002000;
        public const int AUDIOPROFILER_FLAGS_PRIORITY_SHIFT  = 23;

        public enum ColumnIndices
        {
            ObjectName,
            AssetName,
            Volume,
            VULevel,
            Audibility,
            Group,
            Priority,
            PlayCount,
            Is3D,
            IsPaused,
            IsMuted,
            IsVirtual,
            IsOneShot,
            IsLooped,
            DistanceToListener,
            MinDist,
            MaxDist,
            Time,
            Duration,
            Frequency,
            IsStream,
            IsCompressed,
            IsNonBlocking,
            IsOpenUser,
            IsOpenMemory,
            IsOpenMemoryPoint,
            _LastColumn
        }

        public class AudioProfilerGroupInfoComparer : IComparer<AudioProfilerGroupInfoWrapper>
        {
            public ColumnIndices primarySortKey;
            public ColumnIndices secondarySortKey;
            public bool sortByDescendingOrder;

            public AudioProfilerGroupInfoComparer(ColumnIndices primarySortKey, ColumnIndices secondarySortKey, bool sortByDescendingOrder)
            {
                this.primarySortKey = primarySortKey;
                this.secondarySortKey = secondarySortKey;
                this.sortByDescendingOrder = sortByDescendingOrder;
            }

            private int CompareInternal(AudioProfilerGroupInfoWrapper a, AudioProfilerGroupInfoWrapper b, ColumnIndices key)
            {
                int res = 0;
                switch (key)
                {
                    case ColumnIndices.ObjectName: res = a.objectName.CompareTo(b.objectName); break;
                    case ColumnIndices.AssetName: res = a.assetName.CompareTo(b.assetName); break;
                    case ColumnIndices.Volume: res = a.info.volume.CompareTo(b.info.volume); break;
                    case ColumnIndices.VULevel: res = a.info.maxRMSLevelOrDuration.CompareTo(b.info.maxRMSLevelOrDuration); break;
                    case ColumnIndices.Audibility: res = a.info.audibility.CompareTo(b.info.audibility); break;
                    case ColumnIndices.Group: res = a.parentName.CompareTo(b.parentName); break;
                    case ColumnIndices.Priority:
                    {
                        int priority1 = (a.info.flags >> AUDIOPROFILER_FLAGS_PRIORITY_SHIFT) & 255;
                        int priority2 = (b.info.flags >> AUDIOPROFILER_FLAGS_PRIORITY_SHIFT) & 255;
                        res = priority1.CompareTo(priority2);
                        break;
                    }
                    case ColumnIndices.PlayCount: res = a.info.playCount.CompareTo(b.info.playCount); break;
                    case ColumnIndices.Is3D: res = (a.info.flags & AUDIOPROFILER_FLAGS_3D).CompareTo(b.info.flags & AUDIOPROFILER_FLAGS_3D) + (a.info.flags & AUDIOPROFILER_FLAGS_ISSPATIAL).CompareTo(b.info.flags & AUDIOPROFILER_FLAGS_ISSPATIAL) * 2; break;
                    case ColumnIndices.IsPaused: res = (a.info.flags & AUDIOPROFILER_FLAGS_PAUSED).CompareTo(b.info.flags & AUDIOPROFILER_FLAGS_PAUSED); break;
                    case ColumnIndices.IsMuted: res = (a.info.flags & AUDIOPROFILER_FLAGS_MUTED).CompareTo(b.info.flags & AUDIOPROFILER_FLAGS_MUTED); break;
                    case ColumnIndices.IsVirtual: res = (a.info.flags & AUDIOPROFILER_FLAGS_VIRTUAL).CompareTo(b.info.flags & AUDIOPROFILER_FLAGS_VIRTUAL); break;
                    case ColumnIndices.IsOneShot: res = (a.info.flags & AUDIOPROFILER_FLAGS_ONESHOT).CompareTo(b.info.flags & AUDIOPROFILER_FLAGS_ONESHOT); break;
                    case ColumnIndices.IsStream: res = (a.info.flags & AUDIOPROFILER_FLAGS_STREAM).CompareTo(b.info.flags & AUDIOPROFILER_FLAGS_STREAM); break;
                    case ColumnIndices.IsCompressed: res = (a.info.flags & AUDIOPROFILER_FLAGS_COMPRESSED).CompareTo(b.info.flags & AUDIOPROFILER_FLAGS_COMPRESSED); break;
                    case ColumnIndices.IsLooped: res = (a.info.flags & AUDIOPROFILER_FLAGS_LOOPED).CompareTo(b.info.flags & AUDIOPROFILER_FLAGS_LOOPED); break;
                    case ColumnIndices.IsOpenMemory: res = (a.info.flags & AUDIOPROFILER_FLAGS_OPENMEMORY).CompareTo(b.info.flags & AUDIOPROFILER_FLAGS_OPENMEMORY); break;
                    case ColumnIndices.IsOpenMemoryPoint: res = (a.info.flags & AUDIOPROFILER_FLAGS_OPENMEMORYPOINT).CompareTo(b.info.flags & AUDIOPROFILER_FLAGS_OPENMEMORYPOINT); break;
                    case ColumnIndices.IsOpenUser: res = (a.info.flags & AUDIOPROFILER_FLAGS_OPENUSER).CompareTo(b.info.flags & AUDIOPROFILER_FLAGS_OPENUSER); break;
                    case ColumnIndices.IsNonBlocking: res = (a.info.flags & AUDIOPROFILER_FLAGS_NONBLOCKING).CompareTo(b.info.flags & AUDIOPROFILER_FLAGS_NONBLOCKING); break;
                    case ColumnIndices.DistanceToListener: res = a.info.distanceToListener.CompareTo(b.info.distanceToListener); break;
                    case ColumnIndices.MinDist: res = a.info.minDist.CompareTo(b.info.minDist); break;
                    case ColumnIndices.MaxDist: res = a.info.maxDist.CompareTo(b.info.maxDist); break;
                    case ColumnIndices.Time: res = a.info.time.CompareTo(b.info.time); break;
                    case ColumnIndices.Duration: res = a.info.maxRMSLevelOrDuration.CompareTo(b.info.maxRMSLevelOrDuration); break;
                    case ColumnIndices.Frequency: res = a.info.frequency.CompareTo(b.info.frequency); break;
                }
                return (sortByDescendingOrder) ? -res : res;
            }

            public int Compare(AudioProfilerGroupInfoWrapper a, AudioProfilerGroupInfoWrapper b)
            {
                int res = CompareInternal(a, b, primarySortKey);
                return (res == 0) ? CompareInternal(a, b, secondarySortKey) : res;
            }
        }

        private static string FormatDb(float vol)
        {
            if (vol == 0.0f)
                return "-\u221E dB";
            return UnityString.Format("{0:F1} dB", 20.0f * Mathf.Log10(vol));
        }

        public static string GetColumnString(AudioProfilerGroupInfoWrapper info, ColumnIndices index)
        {
            bool is3D = (info.info.flags & AUDIOPROFILER_FLAGS_3D) != 0;
            bool isGroup = (info.info.flags & AUDIOPROFILER_FLAGS_GROUP) != 0;
            switch (index)
            {
                case ColumnIndices.ObjectName: return info.objectName;
                case ColumnIndices.AssetName: return info.assetName;
                case ColumnIndices.Volume: return FormatDb(info.info.volume);
                case ColumnIndices.VULevel: return (!isGroup || info.info.maxRMSLevelOrDuration <= 0.0f) ? "" : FormatDb(Mathf.Sqrt(info.info.maxRMSLevelOrDuration));
                case ColumnIndices.Audibility: return FormatDb(info.info.audibility);
                case ColumnIndices.Group: return info.parentName;
                case ColumnIndices.Priority:
                {
                    int priority = (info.info.flags >> AUDIOPROFILER_FLAGS_PRIORITY_SHIFT) & 255;
                    return isGroup ? "" : priority.ToString();
                }
                case ColumnIndices.PlayCount: return isGroup ? "" : info.info.playCount.ToString();
                case ColumnIndices.Is3D: return isGroup ? "" : is3D ? ((info.info.flags & AUDIOPROFILER_FLAGS_ISSPATIAL) != 0 ? "Spatial" : "YES") : "NO";
                case ColumnIndices.IsPaused: return isGroup ? "" : (info.info.flags & AUDIOPROFILER_FLAGS_PAUSED) != 0 ? "YES" : "NO";
                case ColumnIndices.IsMuted: return isGroup ? "" : (info.info.flags & AUDIOPROFILER_FLAGS_MUTED) != 0 ? "YES" : "NO";
                case ColumnIndices.IsVirtual: return isGroup ? "" : (info.info.flags & AUDIOPROFILER_FLAGS_VIRTUAL) != 0 ? "YES" : "NO";
                case ColumnIndices.IsOneShot: return isGroup ? "" : (info.info.flags & AUDIOPROFILER_FLAGS_ONESHOT) != 0 ? "YES" : "NO";
                case ColumnIndices.IsStream: return isGroup ? "" : (info.info.flags & AUDIOPROFILER_FLAGS_STREAM) != 0 ? "YES" : "NO";
                case ColumnIndices.IsCompressed: return isGroup ? "" : (info.info.flags & AUDIOPROFILER_FLAGS_COMPRESSED) != 0 ? "YES" : "NO";
                case ColumnIndices.IsLooped: return isGroup ? "" : (info.info.flags & AUDIOPROFILER_FLAGS_LOOPED) != 0 ? "YES" : "NO";
                case ColumnIndices.IsOpenMemory: return isGroup ? "" : (info.info.flags & AUDIOPROFILER_FLAGS_OPENMEMORY) != 0 ? "YES" : "NO";
                case ColumnIndices.IsOpenMemoryPoint: return isGroup ? "" : (info.info.flags & AUDIOPROFILER_FLAGS_OPENMEMORYPOINT) != 0 ? "YES" : "NO";
                case ColumnIndices.IsOpenUser: return isGroup ? "" : (info.info.flags & AUDIOPROFILER_FLAGS_OPENUSER) != 0 ? "YES" : "NO";
                case ColumnIndices.IsNonBlocking: return isGroup ? "" : (info.info.flags & AUDIOPROFILER_FLAGS_NONBLOCKING) != 0 ? "YES" : "NO";
                case ColumnIndices.DistanceToListener: return isGroup ? "" : !is3D ? "N/A" : (info.info.distanceToListener >= 1000.0f) ? UnityString.Format("{0:0.00} km", info.info.distanceToListener * 0.001f) : UnityString.Format("{0:0.00} m", info.info.distanceToListener);
                case ColumnIndices.MinDist: return isGroup ? "" : !is3D ? "N/A" : (info.info.minDist >= 1000.0f) ? UnityString.Format("{0:0.00} km", info.info.minDist * 0.001f) : UnityString.Format("{0:0.00} m", info.info.minDist);
                case ColumnIndices.MaxDist: return isGroup ? "" : !is3D ? "N/A" : (info.info.maxDist >= 1000.0f) ? UnityString.Format("{0:0.00} km", info.info.maxDist * 0.001f) : UnityString.Format("{0:0.00} m", info.info.maxDist);
                case ColumnIndices.Time: return isGroup ? "" : UnityString.Format("{0:0.00} s", info.info.time);
                case ColumnIndices.Duration: return isGroup ? "" : UnityString.Format("{0:0.00} s", info.info.maxRMSLevelOrDuration);
                case ColumnIndices.Frequency: return isGroup ? UnityString.Format("{0:0.00} x", info.info.frequency) : (info.info.frequency >= 1000.0f) ? UnityString.Format("{0:0.00} kHz", info.info.frequency * 0.001f) : UnityString.Format("{0:0.00} Hz", info.info.frequency);
            }
            return "Unknown";
        }

        public static int GetLastColumnIndex()
        {
            return Unsupported.IsDeveloperMode() ? ((int)ColumnIndices._LastColumn - 1) : (int)ColumnIndices.Duration;
        }
    }

    internal class AudioProfilerGroupViewBackend
    {
        public List<AudioProfilerGroupInfoWrapper> items { get; private set; }

        public delegate void DataUpdateDelegate();
        public DataUpdateDelegate OnUpdate;
        public AudioProfilerGroupTreeViewState m_TreeViewState;
        public ProfilerAudioView m_ViewType = ProfilerAudioView.Channels;

        public AudioProfilerGroupViewBackend(AudioProfilerGroupTreeViewState state)
        {
            m_TreeViewState = state;
            items = new List<AudioProfilerGroupInfoWrapper>();
        }

        public void SetData(List<AudioProfilerGroupInfoWrapper> data)
        {
            items = data;
            UpdateSorting();
        }

        public void UpdateSorting()
        {
            if (m_ViewType == ProfilerAudioView.Channels)
                items.Sort(new AudioProfilerGroupInfoHelper.AudioProfilerGroupInfoComparer((AudioProfilerGroupInfoHelper.ColumnIndices)m_TreeViewState.selectedColumn, (AudioProfilerGroupInfoHelper.ColumnIndices)m_TreeViewState.prevSelectedColumn, m_TreeViewState.sortByDescendingOrder));
            else
                items.Sort(new AudioProfilerGroupInfoHelper.AudioProfilerGroupInfoComparer(0, 0, false));

            if (OnUpdate != null)
                OnUpdate();
        }
    }

    internal class AudioProfilerGroupTreeViewState : TreeViewState
    {
        [SerializeField]
        public int selectedColumn = (int)AudioProfilerGroupInfoHelper.ColumnIndices.Audibility;

        [SerializeField]
        public int prevSelectedColumn = (int)AudioProfilerGroupInfoHelper.ColumnIndices.Is3D;

        [SerializeField]
        public bool sortByDescendingOrder = true;

        [SerializeField]
        public float[] columnWidths;

        public void SetSelectedColumn(int index)
        {
            if (index != selectedColumn)
                prevSelectedColumn = selectedColumn;
            else
                sortByDescendingOrder = !sortByDescendingOrder;
            selectedColumn = index;
        }
    }

    internal class AudioProfilerGroupView
    {
        private TreeViewController m_TreeView;
        private AudioProfilerGroupTreeViewState m_TreeViewState;
        private EditorWindow m_EditorWindow;
        private AudioProfilerGroupViewColumnHeader m_ColumnHeader;
        private AudioProfilerGroupViewBackend m_Backend;
        private GUIStyle m_HeaderStyle;

        public int GetNumItemsInData()
        {
            return m_Backend.items.Count;
        }

        public AudioProfilerGroupView(EditorWindow editorWindow, AudioProfilerGroupTreeViewState state)
        {
            m_EditorWindow = editorWindow;
            m_TreeViewState = state;
        }

        public void Init(Rect rect, AudioProfilerGroupViewBackend backend)
        {
            if (m_HeaderStyle == null)
                m_HeaderStyle = "OL title";

            if (m_TreeView != null)
                return;

            m_Backend = backend;

            // Default widths
            if (m_TreeViewState.columnWidths == null || m_TreeViewState.columnWidths.Length == 0)
            {
                int numCols = AudioProfilerGroupInfoHelper.GetLastColumnIndex() + 1;
                m_TreeViewState.columnWidths = new float[numCols];
                for (int n = 2; n < numCols; n++)
                    m_TreeViewState.columnWidths[n] = (n >= 14 && n <= 19) ? 85 : 60;
                m_TreeViewState.columnWidths[0] = 140;
                m_TreeViewState.columnWidths[1] = 140;
                m_TreeViewState.columnWidths[2] = 75;
                m_TreeViewState.columnWidths[3] = 75;
                m_TreeViewState.columnWidths[5] = 100;
            }

            m_TreeView = new TreeViewController(m_EditorWindow, m_TreeViewState);

            ITreeViewGUI gui = new AudioProfilerGroupViewGUI(m_TreeView);
            //ITreeViewDragging dragging = new TestDragging(m_TreeView);
            ITreeViewDataSource dataSource = new AudioProfilerDataSource(m_TreeView, m_Backend);
            m_TreeView.Init(rect, dataSource, gui, null);

            m_ColumnHeader = new AudioProfilerGroupViewColumnHeader(m_TreeViewState, m_Backend);
            m_ColumnHeader.columnWidths = m_TreeViewState.columnWidths;
            m_ColumnHeader.minColumnWidth = 30f;

            m_TreeView.selectionChangedCallback += OnTreeSelectionChanged;
        }

        private int delayedPingObject;

        private void PingObjectDelayed()
        {
            EditorGUIUtility.PingObject(delayedPingObject);
        }

        public void OnTreeSelectionChanged(int[] selection)
        {
            if (selection.Length == 1)
            {
                var node = m_TreeView.FindItem(selection[0]);
                var audioNode = node as AudioProfilerGroupTreeViewItem;
                if (audioNode != null)
                {
                    EditorGUIUtility.PingObject(audioNode.info.info.assetInstanceId);
                    delayedPingObject = audioNode.info.info.objectInstanceId;
                    EditorApplication.CallDelayed(PingObjectDelayed, 1.0f);
                }
            }
        }

        public void OnGUI(Rect rect, ProfilerAudioView viewType)
        {
            if (viewType != m_Backend.m_ViewType)
            {
                m_Backend.m_ViewType = viewType;
                m_Backend.UpdateSorting();
            }

            int keyboardControl = GUIUtility.GetControlID(FocusType.Keyboard, rect);

            Rect headerRect = new Rect(rect.x, rect.y, rect.width, m_HeaderStyle.fixedHeight);

            // Header
            GUI.Label(headerRect, "", m_HeaderStyle);
            m_ColumnHeader.OnGUI(headerRect, viewType == ProfilerAudioView.Channels, m_HeaderStyle);

            // TreeView
            rect.y += headerRect.height;
            rect.height -= headerRect.height;
            m_TreeView.OnEvent();
            m_TreeView.OnGUI(rect, keyboardControl);
        }

        internal class AudioProfilerGroupTreeViewItem : TreeViewItem
        {
            public AudioProfilerGroupInfoWrapper info { get; set; }

            public AudioProfilerGroupTreeViewItem(int id, int depth, AudioProfilerGroupTreeViewItem parent, string displayName, AudioProfilerGroupInfoWrapper info)
                : base(id, depth, parent, displayName)
            {
                this.info = info;
            }
        }

        internal class AudioProfilerDataSource : TreeViewDataSource
        {
            private AudioProfilerGroupViewBackend m_Backend;

            public AudioProfilerDataSource(TreeViewController treeView, AudioProfilerGroupViewBackend backend)
                : base(treeView)
            {
                m_Backend = backend;
                m_Backend.OnUpdate = FetchData;
                showRootItem = false;
                rootIsCollapsable = false;
                FetchData();
            }

            private void FillTreeItems(AudioProfilerGroupTreeViewItem parentNode, int depth, int parentId, List<AudioProfilerGroupInfoWrapper> items)
            {
                foreach (var s in items)
                {
                    if (parentId == (s.addToRoot ? 0 : s.info.parentId))
                    {
                        if (parentNode.children == null)
                            parentNode.children = new List<TreeViewItem>();
                        var childNode = new AudioProfilerGroupTreeViewItem(s.info.uniqueId, s.addToRoot ? 1 : depth, parentNode, s.objectName, s);
                        parentNode.children.Add(childNode);
                        FillTreeItems(childNode, depth + 1, s.info.uniqueId, items);
                    }
                }
            }

            public override void FetchData()
            {
                var root = new AudioProfilerGroupTreeViewItem(1, 0, null, "ROOT", new AudioProfilerGroupInfoWrapper(new AudioProfilerGroupInfo(), "ROOT", "ROOT", false));
                FillTreeItems(root, 1, 0, m_Backend.items);
                m_RootItem = root;
                //SetExpanded (m_RootItem, true);
                SetExpandedWithChildren(m_RootItem, true);
                m_NeedRefreshRows = true;
            }

            public override bool CanBeParent(TreeViewItem item)
            {
                return item.hasChildren;
            }

            public override bool IsRenamingItemAllowed(TreeViewItem item)
            {
                return false;
            }
        }

        internal class AudioProfilerGroupViewColumnHeader
        {
            public float[] columnWidths { get; set; }
            public float minColumnWidth { get; set; }
            public float dragWidth { get; set; }
            private AudioProfilerGroupTreeViewState m_TreeViewState;
            private AudioProfilerGroupViewBackend m_Backend;

            string[] headers = new[] { "Object", "Asset", "Volume", "VU Level", "Audibility", "Group", "Priority", "Plays", "3D", "Paused", "Muted", "Virtual", "OneShot", "Looped", "Distance", "MinDist", "MaxDist", "Time", "Duration", "Frequency", "Stream", "Compressed", "NonBlocking", "User", "Memory", "MemoryPoint" };

            public AudioProfilerGroupViewColumnHeader(AudioProfilerGroupTreeViewState state, AudioProfilerGroupViewBackend backend)
            {
                m_TreeViewState = state;
                m_Backend = backend;
                minColumnWidth = 10;
                dragWidth = 6f;
            }

            public void OnGUI(Rect rect, bool allowSorting, GUIStyle headerStyle)
            {
                GUI.BeginClip(rect);
                const float dragAreaWidth = 3f;
                float columnPos = -m_TreeViewState.scrollPos.x;
                int lastColumnIndex = AudioProfilerGroupInfoHelper.GetLastColumnIndex();
                for (int i = 0; i <= lastColumnIndex; ++i)
                {
                    Rect columnRect = new Rect(columnPos, 0, columnWidths[i], rect.height - 1);
                    columnPos += columnWidths[i];
                    Rect dragRect = new Rect(columnPos - dragWidth / 2, 0, dragAreaWidth, rect.height);
                    float deltaX = EditorGUI.MouseDeltaReader(dragRect, true).x;
                    if (deltaX != 0f)
                    {
                        columnWidths[i] += deltaX;
                        columnWidths[i] = Mathf.Max(columnWidths[i], minColumnWidth);
                    }

                    string title = headers[i];
                    if (allowSorting && i == m_TreeViewState.selectedColumn)
                        title = (m_TreeViewState.sortByDescendingOrder ? "\u2191" : "\u2193") + title;

                    GUI.Box(columnRect, title, headerStyle);

                    if (allowSorting && Event.current.type == EventType.MouseDown && columnRect.Contains(Event.current.mousePosition))
                    {
                        m_TreeViewState.SetSelectedColumn(i);
                        m_Backend.UpdateSorting();
                    }

                    if (Event.current.type == EventType.Repaint)
                        EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.SplitResizeLeftRight);
                }
                GUI.EndClip();
            }
        }

        internal class AudioProfilerGroupViewGUI : TreeViewGUI
        {
            public AudioProfilerGroupViewGUI(TreeViewController treeView)
                : base(treeView)
            {
                k_IconWidth = 0;
            }

            protected override Texture GetIconForItem(TreeViewItem item)
            {
                return null;
            }

            protected override void RenameEnded()
            {
            }

            protected override void SyncFakeItem()
            {
            }

            override public Vector2 GetTotalSize()
            {
                Vector2 size = base.GetTotalSize();
                size.x = 0;
                foreach (var c in columnWidths)
                    size.x += c;
                return size;
            }

            protected override void OnContentGUI(Rect rect, int row, TreeViewItem item, string label, bool selected, bool focused, bool useBoldFont, bool isPinging)
            {
                if (Event.current.type != EventType.Repaint)
                    return;

                GUIStyle lineStyle = useBoldFont ? Styles.lineBoldStyle : Styles.lineStyle;
                var orgAlignment = lineStyle.alignment;

                lineStyle.alignment = TextAnchor.MiddleLeft;
                lineStyle.padding.left = 0;

                int margin = 2;
                base.OnContentGUI(new Rect(rect.x, rect.y, columnWidths[0] - margin, rect.height), row, item, label, selected, focused, useBoldFont, isPinging);

                rect.x += columnWidths[0] + margin;
                var profilerItem = item as AudioProfilerGroupTreeViewItem;
                for (int i = 1; i < columnWidths.Length; i++)
                {
                    rect.width = columnWidths[i] - 2 * margin;
                    lineStyle.Draw(rect, AudioProfilerGroupInfoHelper.GetColumnString(profilerItem.info, (AudioProfilerGroupInfoHelper.ColumnIndices)i), false, false, selected, focused);
                    Handles.color = Color.black;
                    Handles.DrawLine(new Vector3(rect.x - margin + 1, rect.y, 0), new Vector3(rect.x - margin + 1, rect.y + rect.height, 0));
                    rect.x += columnWidths[i];
                    lineStyle.alignment = TextAnchor.MiddleRight;
                }

                lineStyle.alignment = orgAlignment;
            }

            float[] columnWidths { get { return ((AudioProfilerGroupTreeViewState)m_TreeView.state).columnWidths; } }
        }
    }

    internal class AudioProfilerDSPView
    {
        const int AUDIOPROFILER_DSPFLAGS_ACTIVE = 1;
        const int AUDIOPROFILER_DSPFLAGS_BYPASS = 2;

        private class AudioProfilerDSPNode
        {
            public AudioProfilerDSPNode(AudioProfilerDSPInfo info)
            {
                this.info = info;
                audible = (info.flags & AUDIOPROFILER_DSPFLAGS_ACTIVE) != 0 && (info.flags & AUDIOPROFILER_DSPFLAGS_BYPASS) == 0;
            }

            public AudioProfilerDSPInfo info;
            public List<AudioProfilerDSPNode> parents;
            public List<AudioProfilerDSPNode> children;
            public int x;
            public int y;
            public int w;
            public int h;
            public int level;
#pragma warning disable 649
            public bool audible;
        }

        private class AudioProfilerDSPWire
        {
            public AudioProfilerDSPWire(AudioProfilerDSPNode source, AudioProfilerDSPNode target, AudioProfilerDSPInfo info)
            {
                this.source = source;
                this.target = target;
                this.info = info;
            }

            public AudioProfilerDSPNode source;
            public AudioProfilerDSPNode target;
            public AudioProfilerDSPInfo info;
        }

        GUIStyle m_FontStyle;

        private void DrawRectClipped(Rect r, Color col, string name, Rect c, float zoomFactor)
        {
            Rect s = new Rect(r.x * zoomFactor, r.y * zoomFactor, r.width * zoomFactor, r.height * zoomFactor);
            float sx1 = s.x, sx2 = s.x + s.width;
            float sy1 = s.y, sy2 = s.y + s.height;
            float cx1 = c.x, cx2 = c.x + c.width;
            float cy1 = c.y, cy2 = c.y + c.height;
            float ax = Mathf.Max(sx1, cx1);
            float ay = Mathf.Max(sy1, cy1);
            float bx = Mathf.Min(sx2, cx2);
            float by = Mathf.Min(sy2, cy2);
            if (ax >= bx || ay >= by)
                return;

            GUI.color = col;
            GUI.Button(s, name, m_FontStyle);
        }

        // Cohen-Sutherland out code
        private static int GetOutCode(Vector3 p, Rect c)
        {
            int code = 0;
            if (p.x < c.x) code |= 1;
            if (p.x > c.x + c.width) code |= 2;
            if (p.y < c.y) code |= 4;
            if (p.y > c.y + c.height) code |= 8;
            return code;
        }

        public void DrawArrow(Color color, float arrowThickness, float lineThickness, float shorten, Vector3 start, Vector3 end)
        {
            var length = (end - start).magnitude;
            if (length < 0.001f)
                return;

            var forward = (end - start) / length;
            var right = Vector3.Cross(Vector3.forward, forward).normalized;

            var width = arrowThickness * 0.5f;
            var height = arrowThickness * 0.7f;

            start += forward * shorten;
            end -= forward * shorten;

            var arrowHead = new Vector3[3]
            {
                end,
                end - forward * height + right * width,
                end - forward * height - right * width
            };

            var arrowLine = new Vector3[2]
            {
                start,
                end - forward * height
            };

            Handles.color = color;
            Handles.DrawAAPolyLine(lineThickness, 2, arrowLine);
            Handles.DrawAAConvexPolygon(arrowHead);
        }

        const int connectionWidth = 40;
        const int connectionHeight = 10;
        const int nodeWidth = 120;
        const int nodeHeight = 60;
        const int nodeMargin = 50;

        int nodeSpacingX;
        int nodeSpacingY;

        bool horizontalLayout = false;

        bool UpdateLevels(AudioProfilerDSPNode node, int level)
        {
            foreach (var parent in node.parents)
                if (parent.level != -1 &&
                    level <= parent.level)
                    level = parent.level + 1;

            bool changed = false;
            if (node.level == -1 ||
                node.level != level)
            {
                changed = true;
                node.level = level;
            }

            foreach (var child in node.children)
                changed |= UpdateLevels(child, level + 1);

            return changed;
        }

        void UpdatePositions(AudioProfilerDSPNode node, int p)
        {
            if (((horizontalLayout) ? node.y : node.x) != -1)
                return;

            var s = (horizontalLayout) ? nodeHeight : nodeWidth;

            if (horizontalLayout)
                node.y = p;
            else
                node.x = p;

            foreach (var child in node.children)
            {
                UpdatePositions(child, p);

                var size = (horizontalLayout) ? child.h : child.w;
                s += size;
                p += size;
            }

            if (horizontalLayout)
            {
                if (node.x == -1)
                {
                    node.x = node.level * (nodeWidth + nodeSpacingX);
                    node.h = (s > nodeHeight) ? (s - nodeHeight) : (nodeHeight + nodeSpacingY);
                }
            }
            else
            {
                if (node.y == -1)
                {
                    node.y = node.level * (nodeHeight + nodeSpacingY);
                    node.w = (s > nodeWidth) ? (s - nodeWidth) : (nodeWidth + nodeSpacingX);
                }
            }
        }

        void DoDSPNodeLayout(List<AudioProfilerDSPNode> nodes, List<AudioProfilerDSPWire> wires, ProfilerProperty property)
        {
            // Add space for wire weights that typically fall between two nodes
            nodeSpacingX = 10;
            nodeSpacingY = 10;
            if (horizontalLayout)
                nodeSpacingX += connectionWidth;
            else
                nodeSpacingY += connectionHeight;

            foreach (var node in nodes)
            {
                node.level = -1;
                node.x = -1;
                node.y = -1;
                node.w = nodeWidth;
                node.h = nodeHeight;
                node.parents = new List<AudioProfilerDSPNode>();
                node.children = new List<AudioProfilerDSPNode>();
            }

            foreach (var wire in wires)
            {
                wire.target.children.Add(wire.source);
                wire.source.parents.Add(wire.target);
            }

            for (int iter = 0; iter < 10; iter++)
            {
                var changed = false;
                foreach (var node in nodes)
                    changed |= UpdateLevels(node, 0);
                if (!changed)
                    break;
            }

            foreach (var node in nodes)
            {
                var start = 0;
                if (horizontalLayout)
                {
                    foreach (var t in nodes)
                        if (node.y != -1 && node.y + node.h > start)
                            start = node.y + node.h;
                    start += nodeSpacingY;
                }
                else
                {
                    foreach (var t in nodes)
                        if (node.x != -1 && node.x + node.w > start)
                            start = node.x + node.w;
                    start += nodeSpacingX;
                }

                UpdatePositions(node, start);
            }

            var x = 0;
            var y = 0;
            foreach (var node in nodes)
            {
                if (horizontalLayout)
                {
                    node.y += (node.h - nodeHeight) / 2;
                    node.h = nodeHeight;
                }
                else
                {
                    node.x += (node.w - nodeWidth) / 2;
                    node.w = nodeWidth;
                }

                x = Math.Min(x, node.x);
                y = Math.Min(y, node.y);
            }

            foreach (var node in nodes)
            {
                node.x += nodeMargin - x;
                node.y += nodeMargin - y;
            }
        }

        public void OnGUI(Rect clippingRect, ProfilerProperty property, bool showInactiveDSPChains, bool highlightAudibleDSPChains, bool _horizontalLayout, ref float zoomFactor, ref Vector2 scrollPos, ref Vector2 virtualSize)
        {
            horizontalLayout = _horizontalLayout;

            if (Event.current.type == EventType.ScrollWheel && clippingRect.Contains(Event.current.mousePosition) && (Event.current.shift || Event.current.command))
            {
                // x = internal unscaled position
                // x_screen = x * zoomFactor - scrollPosX
                // mousePosX = scrollPosX  + relativeMousePosX
                //             scrollPosX1 + relativeMousePosX = x * zoomFactor1
                //             scrollPosX2 + relativeMousePosX = x * zoomFactor2
                //                           relativeMousePosX = x * zoomFactor1 - scrollPosX1
                //                                             = x * zoomFactor2 - scrollPosX2
                // x = mousePosX / zoomFactor1
                // scrollPosX2 = scrollPosX1 - x0 * (zoomFactor1 - zoomFactor2)
                //             = scrollPosX1 - (zoomFactor1 - zoomFactor2) * mousePosX / zoomFactor1
                //             = scrollPosX1 - (1 - zoomFactor2 / zoomFactor1) * mousePosX
                float zoomStep = 1.025f;
                var delta = Event.current.mousePosition - clippingRect.min;
                var oldZoomFactor = zoomFactor;
                zoomFactor *= (Event.current.delta.y < 0) ? zoomStep : (1.0f / zoomStep);
                zoomFactor = Mathf.Clamp(zoomFactor, 0.25f, 4.0f);
                scrollPos -= (1.0f - zoomFactor / oldZoomFactor) * Event.current.mousePosition;
                m_FontStyle = null;
                Event.current.Use();
                return;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            var nodeDictionary = new Dictionary<int, AudioProfilerDSPNode>();
            var nodes = new List<AudioProfilerDSPNode>();
            var wires = new List<AudioProfilerDSPWire>();
            var dspInfo = property.GetAudioProfilerDSPInfo();
            if (dspInfo == null || dspInfo.Length == 0)
                return;

            if (m_FontStyle == null)
            {
                m_FontStyle = new GUIStyle(GUI.skin.button);
                m_FontStyle.fontSize = (int)(zoomFactor * 7.0f);
                m_FontStyle.wordWrap = true;
            }

            foreach (var info in dspInfo)
            {
                if (!showInactiveDSPChains && (info.flags & AUDIOPROFILER_DSPFLAGS_ACTIVE) == 0)
                    continue;

                if (!nodeDictionary.ContainsKey(info.id))
                {
                    var node = new AudioProfilerDSPNode(info);
                    nodeDictionary[info.id] = node;
                    nodes.Add(node);
                }

                if (info.target != 0)
                    wires.Add(new AudioProfilerDSPWire(nodeDictionary[info.id], nodeDictionary[info.target], info));
            }

            DoDSPNodeLayout(nodes, wires, property);

            virtualSize.x = 0;
            virtualSize.y = 0;

            foreach (var node in nodes)
            {
                virtualSize.x = Mathf.Max(virtualSize.x, (node.x + nodeWidth) * zoomFactor);
                virtualSize.y = Mathf.Max(virtualSize.y, (node.y + nodeHeight) * zoomFactor);
            }

            virtualSize.x += nodeMargin * zoomFactor;
            virtualSize.y += nodeMargin * zoomFactor;

            // Now draw the nodes
            foreach (var node in nodes)
            {
                var info = node.info;
                var name = property.GetAudioProfilerNameByOffset(info.nameOffset);

                float cpuLoad = 0.01f * info.cpuLoad; // Result is in 0..100 range
                float cpuColoring = 0.1f; // Color 10% CPU load red

                bool active = (info.flags & AUDIOPROFILER_DSPFLAGS_ACTIVE) != 0;
                bool bypass = (info.flags & AUDIOPROFILER_DSPFLAGS_BYPASS) != 0;

                var color = new Color(
                    (!active || bypass) ? 0.5f : Mathf.Clamp(2.0f * cpuColoring * cpuLoad, 0.0f, 1.0f),
                    (!active || bypass) ? 0.5f : Mathf.Clamp(2.0f - 2.0f * cpuColoring * cpuLoad, 0.0f, 1.0f),
                    bypass ? 1.0f : active ? 0.0f : 0.5f,
                    (!highlightAudibleDSPChains || node.audible) ? 1.0f : 0.4f);

                name = name.Replace("ChannelGroup", "Group");
                name = name.Replace("FMOD Channel DSPHead Unit", "Channel DSP Head");
                name = name.Replace("FMOD Channel", "Channel");
                name = name.Replace("FMOD WaveTable Unit", "Wavetable");
                name = name.Replace("FMOD Resampler Unit", "Resampler");

                float vuWidth = 4.0f;
                float vuAlpha = 0.5f;

                float nodeX = node.x;
                float nodeW = nodeWidth;

                if (info.numLevels >= 1)
                {
                    float p = nodeHeight * Mathf.Clamp(info.level1, 0.0f, 1.0f);
                    DrawRectClipped(new Rect(nodeX, node.y, vuWidth, nodeHeight), new Color(0.0f, 0.0f, 0.0f, vuAlpha), null, clippingRect, zoomFactor);
                    DrawRectClipped(new Rect(nodeX, node.y + nodeHeight - p, vuWidth, p), new Color(1.0f, 0.5f, 0.0f, 1.0f), null, clippingRect, zoomFactor);
                    nodeX += vuWidth;
                    nodeW -= vuWidth;
                }

                if (info.numLevels >= 2)
                {
                    float p = nodeHeight * Mathf.Clamp(info.level2, 0.0f, 1.0f);
                    DrawRectClipped(new Rect(nodeX, node.y, vuWidth, nodeHeight), new Color(0.0f, 0.0f, 0.0f, vuAlpha), null, clippingRect, zoomFactor);
                    DrawRectClipped(new Rect(nodeX, node.y + nodeHeight - p, vuWidth, p), new Color(1.0f, 0.5f, 0.0f, 1.0f), null, clippingRect, zoomFactor);
                    nodeX += vuWidth;
                    nodeW -= vuWidth;
                }

                var s = name;
                if ((node.info.flags & AUDIOPROFILER_DSPFLAGS_ACTIVE) == 0)
                    s += " [OFF]";
                if ((node.info.flags & AUDIOPROFILER_DSPFLAGS_BYPASS) != 0)
                    s += " [BYP]";
                if (name == "Send")
                    s += UnityString.Format(" ({0:0.00} dB)", 20.0f * Math.Log10(node.info.level1));
                s += "\n";
                if (name == "VUFader" || name == "EmbeddedFader")
                    s += UnityString.Format("Vol: {0:0.00} dB\nVU: {1:0.00} dB\n", 20.0f * Math.Log10(node.info.level1), 20.0f * Math.Log10(node.info.level2));
                s += UnityString.Format("Rel. audibility: {0:0.00} dB\nAbs. audibility: {1:0.00} dB\nAud. order:{2}\n", 20.0f * Math.Log10(node.info.relativeAudibility), 20.0f * Math.Log10(node.info.absoluteAudibility), node.info.audibilityVisitOrder);

                DrawRectClipped(new Rect(nodeX, node.y, nodeW, nodeHeight), color, s, clippingRect, zoomFactor);
            }

            // Let's draw some wires!
            foreach (var wire in wires)
            {
                float portSpacing = 4;
                var source = wire.source;
                var target = wire.target;
                var info = wire.info;
                Vector3 p1, p2;
                if (horizontalLayout)
                {
                    p1 = new Vector3(source.x * zoomFactor, (source.y + nodeHeight * 0.5f) * zoomFactor, 0.0f);
                    p2 = new Vector3((target.x + nodeWidth) * zoomFactor, (target.y + nodeHeight * 0.5f + (wire.info.targetPort - (wire.target.children.Count - 1) * 0.5f) * portSpacing) * zoomFactor, 0.0f);
                }
                else
                {
                    p1 = new Vector3((source.x + nodeWidth * 0.5f) * zoomFactor, source.y * zoomFactor, 0.0f);
                    p2 = new Vector3((target.x + nodeWidth * 0.5f + (wire.info.targetPort - (wire.target.children.Count - 1) * 0.5f) * portSpacing) * zoomFactor, (target.y + nodeHeight) * zoomFactor, 0.0f);
                }
                var c1 = GetOutCode(p1, clippingRect);
                var c2 = GetOutCode(p2, clippingRect);
                if ((c1 & c2) == 0)
                {
                    var alpha = (!highlightAudibleDSPChains || source.audible) ? 0.7f : 0.3f;
                    DrawArrow(new Color(0.0f, 0.0f, 0.0f, alpha), 7.0f * zoomFactor, 3.0f * zoomFactor, 0.0f, p1, p2);
                    DrawArrow(new Color(1.0f, info.weight, 0.0f, alpha), 5.0f * zoomFactor, 2.0f * zoomFactor, zoomFactor, p1, p2);
                }
            }

            // Draw connection weights on top
            foreach (var wire in wires)
            {
                var source = wire.source;
                var target = wire.target;
                var info = wire.info;
                if (info.weight != 1.0f && target != null)
                {
                    var cx = (source.x + target.x + nodeWidth) * 0.5f;
                    var cy = (source.y + target.y + nodeHeight) * 0.5f;
                    DrawRectClipped(
                        new Rect(cx - connectionWidth * 0.5f, cy - connectionHeight * 0.5f, connectionWidth, connectionHeight),
                        new Color(1.0f, info.weight, 0.0f, (!highlightAudibleDSPChains || source.audible) ? 0.7f : 0.3f),
                        UnityString.Format($"{(int)(100.0f * info.weight)}%"),
                        clippingRect,
                        zoomFactor);
                }
            }
        }
    }

    internal class AudioProfilerClipInfoWrapper
    {
        public AudioProfilerClipInfo info;
        public string assetName;

        public AudioProfilerClipInfoWrapper(AudioProfilerClipInfo info, string assetName)
        {
            this.info = info;
            this.assetName = assetName;
        }
    }

    internal class AudioProfilerClipInfoHelper
    {
        public enum ColumnIndices
        {
            AssetName,
            LoadState,
            InternalLoadState,
            Age,
            Disposed,
            NumChannelInstances,
            NumClones,
            RefCount,
            InstancePtr,
            _LastColumn
        }

        public class AudioProfilerClipInfoComparer : IComparer<AudioProfilerClipInfoWrapper>
        {
            public ColumnIndices primarySortKey;
            public ColumnIndices secondarySortKey;
            public bool sortByDescendingOrder;

            public AudioProfilerClipInfoComparer(ColumnIndices primarySortKey, ColumnIndices secondarySortKey, bool sortByDescendingOrder)
            {
                this.primarySortKey = primarySortKey;
                this.secondarySortKey = secondarySortKey;
                this.sortByDescendingOrder = sortByDescendingOrder;
            }

            private int CompareInternal(AudioProfilerClipInfoWrapper a, AudioProfilerClipInfoWrapper b, ColumnIndices key)
            {
                int res = 0;
                switch (key)
                {
                    case ColumnIndices.AssetName: res = a.assetName.CompareTo(b.assetName); break;
                    case ColumnIndices.LoadState: res = a.info.loadState.CompareTo(b.info.loadState); break;
                    case ColumnIndices.InternalLoadState: res = a.info.internalLoadState.CompareTo(b.info.internalLoadState); break;
                    case ColumnIndices.Age: res = a.info.age.CompareTo(b.info.age); break;
                    case ColumnIndices.Disposed: res = a.info.disposed.CompareTo(b.info.disposed); break;
                    case ColumnIndices.NumChannelInstances: res = a.info.numChannelInstances.CompareTo(b.info.numChannelInstances); break;
                    case ColumnIndices.NumClones: res = a.info.numClones.CompareTo(b.info.numClones); break;
                    case ColumnIndices.RefCount: res = a.info.refCount.CompareTo(b.info.refCount); break;
                    case ColumnIndices.InstancePtr: res = a.info.instancePtr.CompareTo(b.info.instancePtr); break;
                }
                return (sortByDescendingOrder) ? -res : res;
            }

            public int Compare(AudioProfilerClipInfoWrapper a, AudioProfilerClipInfoWrapper b)
            {
                int res = CompareInternal(a, b, primarySortKey);
                return (res == 0) ? CompareInternal(a, b, secondarySortKey) : res;
            }
        }

        static string[] m_LoadStateNames = new string[] { "Unloaded", "Loading Base", "Loading Sub", "Loaded", "Failed" };
        static string[] m_InternalLoadStateNames = new string[] { "Pending", "Loaded", "Failed" };

        public static string GetColumnString(AudioProfilerClipInfoWrapper info, ColumnIndices index)
        {
            switch (index)
            {
                case ColumnIndices.AssetName: return info.assetName;
                case ColumnIndices.LoadState: return m_LoadStateNames[info.info.loadState];
                case ColumnIndices.InternalLoadState: return m_InternalLoadStateNames[info.info.internalLoadState];
                case ColumnIndices.Age: return info.info.age.ToString();
                case ColumnIndices.Disposed: return (info.info.disposed != 0) ? "YES" : "NO";
                case ColumnIndices.NumChannelInstances: return info.info.numChannelInstances.ToString();
                case ColumnIndices.NumClones: return info.info.numClones.ToString();
                case ColumnIndices.RefCount: return info.info.refCount.ToString();
                case ColumnIndices.InstancePtr: return "0x" + info.info.instancePtr.ToString("X");
            }
            return "Unknown";
        }

        public static int GetLastColumnIndex()
        {
            return (int)ColumnIndices._LastColumn - 1;
        }
    }

    internal class AudioProfilerClipViewBackend
    {
        public List<AudioProfilerClipInfoWrapper> items { get; private set; }

        public delegate void DataUpdateDelegate();
        public DataUpdateDelegate OnUpdate;
        public AudioProfilerClipTreeViewState m_TreeViewState;

        public AudioProfilerClipViewBackend(AudioProfilerClipTreeViewState state)
        {
            m_TreeViewState = state;
            items = new List<AudioProfilerClipInfoWrapper>();
        }

        public void SetData(List<AudioProfilerClipInfoWrapper> data)
        {
            items = data;
            UpdateSorting();
        }

        public void UpdateSorting()
        {
            items.Sort(new AudioProfilerClipInfoHelper.AudioProfilerClipInfoComparer((AudioProfilerClipInfoHelper.ColumnIndices)m_TreeViewState.selectedColumn, (AudioProfilerClipInfoHelper.ColumnIndices)m_TreeViewState.prevSelectedColumn, m_TreeViewState.sortByDescendingOrder));
            if (OnUpdate != null)
                OnUpdate();
        }
    }

    internal class AudioProfilerClipTreeViewState : TreeViewState
    {
        [SerializeField]
        public int selectedColumn = (int)AudioProfilerClipInfoHelper.ColumnIndices.InternalLoadState;

        [SerializeField]
        public int prevSelectedColumn = (int)AudioProfilerClipInfoHelper.ColumnIndices.LoadState;

        [SerializeField]
        public bool sortByDescendingOrder = true;

        [SerializeField]
        public float[] columnWidths;

        public void SetSelectedColumn(int index)
        {
            if (index != selectedColumn)
                prevSelectedColumn = selectedColumn;
            else
                sortByDescendingOrder = !sortByDescendingOrder;
            selectedColumn = index;
        }
    }

    internal class AudioProfilerClipView
    {
        private TreeViewController m_TreeView;
        private AudioProfilerClipTreeViewState m_TreeViewState;
        private EditorWindow m_EditorWindow;
        private AudioProfilerClipViewColumnHeader m_ColumnHeader;
        private AudioProfilerClipViewBackend m_Backend;
        private GUIStyle m_HeaderStyle;

        public int GetNumItemsInData()
        {
            return m_Backend.items.Count;
        }

        public AudioProfilerClipView(EditorWindow editorWindow, AudioProfilerClipTreeViewState state)
        {
            m_EditorWindow = editorWindow;
            m_TreeViewState = state;
        }

        public void Init(Rect rect, AudioProfilerClipViewBackend backend)
        {
            if (m_HeaderStyle == null)
                m_HeaderStyle = "OL title";

            if (m_TreeView != null)
                return;

            m_Backend = backend;

            // Default widths
            if (m_TreeViewState.columnWidths == null || m_TreeViewState.columnWidths.Length == 0)
            {
                int numCols = AudioProfilerClipInfoHelper.GetLastColumnIndex() + 1;
                m_TreeViewState.columnWidths = new float[numCols];
                for (int n = 0; n < numCols; n++)
                    m_TreeViewState.columnWidths[n] = (n == 0) ? 300 : (n == 2) ? 110 : 80;
            }

            m_TreeView = new TreeViewController(m_EditorWindow, m_TreeViewState);

            ITreeViewGUI gui = new AudioProfilerClipViewGUI(m_TreeView);
            //ITreeViewDragging dragging = new TestDragging(m_TreeView);
            ITreeViewDataSource dataSource = new AudioProfilerDataSource(m_TreeView, m_Backend);
            m_TreeView.Init(rect, dataSource, gui, null);

            m_ColumnHeader = new AudioProfilerClipViewColumnHeader(m_TreeViewState, m_Backend);
            m_ColumnHeader.columnWidths = m_TreeViewState.columnWidths;
            m_ColumnHeader.minColumnWidth = 30f;

            m_TreeView.selectionChangedCallback += OnTreeSelectionChanged;
        }

        public void OnTreeSelectionChanged(int[] selection)
        {
            if (selection.Length == 1)
            {
                var node = m_TreeView.FindItem(selection[0]);
                var audioNode = node as AudioProfilerClipTreeViewItem;
                if (audioNode != null)
                {
                    EditorGUIUtility.PingObject(audioNode.info.info.assetInstanceId);
                }
            }
        }

        public void OnGUI(Rect rect)
        {
            int keyboardControl = GUIUtility.GetControlID(FocusType.Keyboard, rect);

            Rect headerRect = new Rect(rect.x, rect.y, rect.width, m_HeaderStyle.fixedHeight);

            // Header
            GUI.Label(headerRect, "", m_HeaderStyle);
            m_ColumnHeader.OnGUI(headerRect, true, m_HeaderStyle);

            // TreeView
            rect.y += headerRect.height;
            rect.height -= headerRect.height;
            m_TreeView.OnEvent();
            m_TreeView.OnGUI(rect, keyboardControl);
        }

        internal class AudioProfilerClipTreeViewItem : TreeViewItem
        {
            public AudioProfilerClipInfoWrapper info { get; set; }

            public AudioProfilerClipTreeViewItem(int id, int depth, TreeViewItem parent, string displayName, AudioProfilerClipInfoWrapper info)
                : base(id, depth, parent, displayName)
            {
                this.info = info;
            }
        }

        internal class AudioProfilerDataSource : TreeViewDataSource
        {
            private AudioProfilerClipViewBackend m_Backend;

            public AudioProfilerDataSource(TreeViewController treeView, AudioProfilerClipViewBackend backend)
                : base(treeView)
            {
                m_Backend = backend;
                m_Backend.OnUpdate = FetchData;
                showRootItem = false;
                rootIsCollapsable = false;
                FetchData();
            }

            private void FillTreeItems(AudioProfilerClipTreeViewItem parentNode, int depth, int parentId, List<AudioProfilerClipInfoWrapper> items)
            {
                parentNode.children = new List<TreeViewItem>(items.Count);

                int uniqueId = 1;
                foreach (var s in items)
                {
                    var childNode = new AudioProfilerClipTreeViewItem(++uniqueId, 1, parentNode, s.assetName, s);
                    parentNode.children.Add(childNode);
                }
            }

            public override void FetchData()
            {
                var root = new AudioProfilerClipTreeViewItem(1, 0, null, "ROOT", new AudioProfilerClipInfoWrapper(new AudioProfilerClipInfo(), "ROOT"));
                FillTreeItems(root, 1, 0, m_Backend.items);
                m_RootItem = root;
                //SetExpanded (m_RootItem, true);
                SetExpandedWithChildren(m_RootItem, true);
                m_NeedRefreshRows = true;
            }

            public override bool CanBeParent(TreeViewItem item)
            {
                return item.hasChildren;
            }

            public override bool IsRenamingItemAllowed(TreeViewItem item)
            {
                return false;
            }
        }

        internal class AudioProfilerClipViewColumnHeader
        {
            public float[] columnWidths { get; set; }
            public float minColumnWidth { get; set; }
            public float dragWidth { get; set; }
            private AudioProfilerClipTreeViewState m_TreeViewState;
            private AudioProfilerClipViewBackend m_Backend;

            string[] headers = new[] { "Asset", "Load State", "Internal Load State", "Age", "Disposed", "Num Voices", "Num Clones", "Ref Count", "Instance Ptr" };

            public AudioProfilerClipViewColumnHeader(AudioProfilerClipTreeViewState state, AudioProfilerClipViewBackend backend)
            {
                m_TreeViewState = state;
                m_Backend = backend;
                minColumnWidth = 10;
                dragWidth = 6f;
            }

            public void OnGUI(Rect rect, bool allowSorting, GUIStyle headerStyle)
            {
                GUI.BeginClip(rect);
                const float dragAreaWidth = 3f;
                float columnPos = -m_TreeViewState.scrollPos.x;
                int lastColumnIndex = AudioProfilerClipInfoHelper.GetLastColumnIndex();
                for (int i = 0; i <= lastColumnIndex; ++i)
                {
                    Rect columnRect = new Rect(columnPos, 0, columnWidths[i], rect.height - 1);
                    columnPos += columnWidths[i];
                    Rect dragRect = new Rect(columnPos - dragWidth / 2, 0, dragAreaWidth, rect.height);
                    float deltaX = EditorGUI.MouseDeltaReader(dragRect, true).x;
                    if (deltaX != 0f)
                    {
                        columnWidths[i] += deltaX;
                        columnWidths[i] = Mathf.Max(columnWidths[i], minColumnWidth);
                    }

                    string title = headers[i];
                    if (allowSorting && i == m_TreeViewState.selectedColumn)
                        title = (m_TreeViewState.sortByDescendingOrder ? "\u2191" : "\u2193") + title;

                    GUI.Box(columnRect, title, headerStyle);

                    if (allowSorting && Event.current.type == EventType.MouseDown && columnRect.Contains(Event.current.mousePosition))
                    {
                        m_TreeViewState.SetSelectedColumn(i);
                        m_Backend.UpdateSorting();
                    }

                    if (Event.current.type == EventType.Repaint)
                        EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.SplitResizeLeftRight);
                }
                GUI.EndClip();
            }
        }

        internal class AudioProfilerClipViewGUI : TreeViewGUI
        {
            public AudioProfilerClipViewGUI(TreeViewController treeView)
                : base(treeView)
            {
                k_IconWidth = 0;
            }

            protected override Texture GetIconForItem(TreeViewItem item)
            {
                return null;
            }

            protected override void RenameEnded()
            {
            }

            protected override void SyncFakeItem()
            {
            }

            override public Vector2 GetTotalSize()
            {
                Vector2 size = base.GetTotalSize();
                size.x = 0;
                foreach (var c in columnWidths)
                    size.x += c;
                return size;
            }

            protected override void OnContentGUI(Rect rect, int row, TreeViewItem item, string label, bool selected, bool focused, bool useBoldFont, bool isPinging)
            {
                if (Event.current.type != EventType.Repaint)
                    return;

                GUIStyle lineStyle = useBoldFont ? Styles.lineBoldStyle : Styles.lineStyle;
                lineStyle.alignment = TextAnchor.MiddleLeft;
                lineStyle.padding.left = 0;

                int margin = 2;
                base.OnContentGUI(new Rect(rect.x, rect.y, columnWidths[0] - margin, rect.height), row, item, label, selected, focused, useBoldFont, isPinging);

                rect.x += columnWidths[0] + margin;
                var profilerItem = item as AudioProfilerClipTreeViewItem;
                for (int i = 1; i < columnWidths.Length; i++)
                {
                    rect.width = columnWidths[i] - 2 * margin;
                    lineStyle.alignment = TextAnchor.MiddleRight;
                    lineStyle.Draw(rect, AudioProfilerClipInfoHelper.GetColumnString(profilerItem.info, (AudioProfilerClipInfoHelper.ColumnIndices)i), false, false, selected, focused);
                    Handles.color = Color.black;
                    Handles.DrawLine(new Vector3(rect.x - margin + 1, rect.y, 0), new Vector3(rect.x - margin + 1, rect.y + rect.height, 0));
                    rect.x += columnWidths[i];
                }

                lineStyle.alignment = TextAnchor.MiddleLeft;
            }

            float[] columnWidths { get { return ((AudioProfilerClipTreeViewState)m_TreeView.state).columnWidths; } }
        }
    }
}
