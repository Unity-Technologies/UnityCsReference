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

        public enum ColumnIndices
        {
            ObjectName,
            AssetName,
            Volume,
            Audibility,
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
                    case ColumnIndices.Audibility: res = a.info.audibility.CompareTo(b.info.audibility); break;
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
                    case ColumnIndices.Duration: res = a.info.duration.CompareTo(b.info.duration); break;
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
            return string.Format("{0:0.00} dB", 20.0f * Mathf.Log10(vol));
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
                case ColumnIndices.Audibility: return isGroup ? "" : FormatDb(info.info.audibility);
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
                case ColumnIndices.DistanceToListener: return isGroup ? "" : !is3D ? "N/A" : (info.info.distanceToListener >= 1000.0f) ? string.Format("{0:0.00} km", info.info.distanceToListener * 0.001f) : string.Format("{0:0.00} m", info.info.distanceToListener);
                case ColumnIndices.MinDist: return isGroup ? "" : !is3D ? "N/A" : (info.info.minDist >= 1000.0f) ? string.Format("{0:0.00} km", info.info.minDist * 0.001f) : string.Format("{0:0.00} m", info.info.minDist);
                case ColumnIndices.MaxDist: return isGroup ? "" : !is3D ? "N/A" : (info.info.maxDist >= 1000.0f) ? string.Format("{0:0.00} km", info.info.maxDist * 0.001f) : string.Format("{0:0.00} m", info.info.maxDist);
                case ColumnIndices.Time: return isGroup ? "" : string.Format("{0:0.00} s", info.info.time);
                case ColumnIndices.Duration: return isGroup ? "" : string.Format("{0:0.00} s", info.info.duration);
                case ColumnIndices.Frequency: return isGroup ? string.Format("{0:0.00} x", info.info.frequency) : (info.info.frequency >= 1000.0f) ? string.Format("{0:0.00} kHz", info.info.frequency * 0.001f) : string.Format("{0:0.00} Hz", info.info.frequency);
            }
            return "Unknown";
        }

        public static int GetLastColumnIndex()
        {
            return Unsupported.IsDeveloperBuild() ? ((int)ColumnIndices._LastColumn - 1) : (int)ColumnIndices.Duration;
        }
    }

    internal class AudioProfilerGroupViewBackend
    {
        public List<AudioProfilerGroupInfoWrapper> items { get; private set; }

        public delegate void DataUpdateDelegate();
        public DataUpdateDelegate OnUpdate;
        public AudioProfilerGroupTreeViewState m_TreeViewState;

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
            items.Sort(new AudioProfilerGroupInfoHelper.AudioProfilerGroupInfoComparer((AudioProfilerGroupInfoHelper.ColumnIndices)m_TreeViewState.selectedColumn, (AudioProfilerGroupInfoHelper.ColumnIndices)m_TreeViewState.prevSelectedColumn, m_TreeViewState.sortByDescendingOrder));
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
            {
                m_HeaderStyle = new GUIStyle("OL title");
            }

            m_HeaderStyle.alignment = TextAnchor.MiddleLeft;

            if (m_TreeView != null)
                return;

            m_Backend = backend;

            // Default widths
            if (m_TreeViewState.columnWidths == null || m_TreeViewState.columnWidths.Length == 0)
            {
                int numCols = AudioProfilerGroupInfoHelper.GetLastColumnIndex() + 1;
                m_TreeViewState.columnWidths = new float[numCols];
                for (int n = 2; n < numCols; n++)
                    m_TreeViewState.columnWidths[n] = (n == 2 || n == 3 || (n >= 11 && n <= 16)) ? 75 : 60;
                m_TreeViewState.columnWidths[0] = 140;
                m_TreeViewState.columnWidths[1] = 140;
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

        public void OnGUI(Rect rect, bool allowSorting)
        {
            int keyboardControl = GUIUtility.GetControlID(FocusType.Keyboard, rect);

            Rect headerRect = new Rect(rect.x, rect.y, rect.width, m_HeaderStyle.fixedHeight);

            // Header
            GUI.Label(headerRect, "", m_HeaderStyle);
            m_ColumnHeader.OnGUI(headerRect, allowSorting, m_HeaderStyle);

            // TreeView
            rect.y += headerRect.height;
            rect.height -= headerRect.height;
            m_TreeView.OnEvent();
            m_TreeView.OnGUI(rect, keyboardControl);
        }

        internal class AudioProfilerGroupTreeViewItem : TreeViewItem
        {
            public AudioProfilerGroupInfoWrapper info { get; set; }

            public AudioProfilerGroupTreeViewItem(int id, int depth, TreeViewItem parent, string displayName, AudioProfilerGroupInfoWrapper info)
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
                int numChildren = 0;
                foreach (var s in items)
                    if (parentId == (s.addToRoot ? 0 : s.info.parentId))
                        numChildren++;
                if (numChildren > 0)
                {
                    parentNode.children = new List<TreeViewItem>(numChildren);

                    foreach (var s in items)
                    {
                        if (parentId == (s.addToRoot ? 0 : s.info.parentId))
                        {
                            var childNode = new AudioProfilerGroupTreeViewItem(s.info.uniqueId, s.addToRoot ? 1 : depth, parentNode, s.objectName, s);
                            parentNode.children.Add(childNode);
                            FillTreeItems(childNode, depth + 1, s.info.uniqueId, items);
                        }
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

            string[] headers = new[] { "Object", "Asset", "Volume", "Audibility", "Plays", "3D", "Paused", "Muted", "Virtual", "OneShot", "Looped", "Distance", "MinDist", "MaxDist", "Time", "Duration", "Frequency", "Stream", "Compressed", "NonBlocking", "User", "Memory", "MemoryPoint" };

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
                        title += m_TreeViewState.sortByDescendingOrder ? " \u25BC" : " \u25B2";

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
            public AudioProfilerDSPNode(AudioProfilerDSPNode firstTarget, AudioProfilerDSPInfo info, int x, int y, int level)
            {
                this.firstTarget = firstTarget;
                this.info = info;
                this.x = x;
                this.y = y;
                this.level = level;
                this.maxY = y;
                audible = (info.flags & AUDIOPROFILER_DSPFLAGS_ACTIVE) != 0 && (info.flags & AUDIOPROFILER_DSPFLAGS_BYPASS) == 0;
                if (firstTarget != null)
                    audible &= firstTarget.audible;
            }

            public AudioProfilerDSPNode firstTarget;
            public AudioProfilerDSPInfo info;
            public int x;
            public int y;
            public int level;
            public int maxY;
#pragma warning disable 649
            public int targetPort;
            public bool audible;
        }

        private class AudioProfilerDSPWire
        {
            public AudioProfilerDSPWire(AudioProfilerDSPNode source, AudioProfilerDSPNode target, AudioProfilerDSPInfo info)
            {
                this.source = source;
                this.target = target;
                this.info = info;
                this.targetPort = target.targetPort;
            }

            public AudioProfilerDSPNode source;
            public AudioProfilerDSPNode target;
            public AudioProfilerDSPInfo info;
            public int targetPort;
        }

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

            if (name == null)
            {
                EditorGUI.DrawRect(s, col);
            }
            else
            {
                GUI.color = col;
                GUI.Button(s, name);
            }
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

        public void OnGUI(Rect clippingRect, ProfilerProperty property, bool showInactiveDSPChains, bool highlightAudibleDSPChains, ref float zoomFactor, ref Vector2 scrollPos)
        {
            if (Event.current.type == EventType.ScrollWheel && clippingRect.Contains(Event.current.mousePosition) && Event.current.shift)
            {
                float zoomStep = 1.05f;
                var oldZoomFactor = zoomFactor;
                zoomFactor *= (Event.current.delta.y > 0) ? zoomStep : (1.0f / zoomStep);
                scrollPos += (Event.current.mousePosition - scrollPos) * (zoomFactor - oldZoomFactor);
                Event.current.Use();
                return;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            int cw = 64, ch = 16, w = 140, h = 30, xspacing = cw + 10, yspacing = 5;
            var nodes = new Dictionary<int, AudioProfilerDSPNode>();
            var wires = new List<AudioProfilerDSPWire>();
            var dspInfo = property.GetAudioProfilerDSPInfo();
            if (dspInfo == null)
                return;

            // Figure out where to place all this stuff and how much screen estate the branches need.
            bool isRootNode = true;
            foreach (var info in dspInfo)
            {
                if (!showInactiveDSPChains && (info.flags & AUDIOPROFILER_DSPFLAGS_ACTIVE) == 0)
                    continue;

                if (!nodes.ContainsKey(info.id))
                {
                    var firstTarget = nodes.ContainsKey(info.target) ? nodes[info.target] : null;
                    if (firstTarget != null)
                    {
                        nodes[info.id] = new AudioProfilerDSPNode(firstTarget, info, firstTarget.x + w + xspacing, firstTarget.maxY, firstTarget.level + 1);
                        firstTarget.maxY += h + yspacing;
                        var p = firstTarget;
                        while (p != null)
                        {
                            p.maxY = Mathf.Max(p.maxY, firstTarget.maxY);
                            p = p.firstTarget;
                        }
                    }
                    else if (isRootNode)
                    {
                        isRootNode = false;
                        nodes[info.id] = new AudioProfilerDSPNode(firstTarget, info, 10 + (w / 2), 10 + (h / 2), 1);
                    }
                    if (firstTarget != null)
                        wires.Add(new AudioProfilerDSPWire(nodes[info.id], firstTarget, info));
                }
                else
                {
                    // Same node, but containing an additional target connection (i.e. for reverb mix connections)
                    wires.Add(new AudioProfilerDSPWire(nodes[info.id], nodes[info.target], info));
                }
            }

            // Center node relative to its child branch
            foreach (var _node in nodes)
            {
                var node = _node.Value;
                node.y += ((node.maxY == node.y) ? (h + yspacing) : (node.maxY - node.y)) / 2;
            }

            // Let's draw some wires!
            foreach (var wire in wires)
            {
                float portSpacing = 4;
                var source = wire.source;
                var target = wire.target;
                var info = wire.info;
                var p1 = new Vector3((source.x - w * 0.5f) * zoomFactor, source.y * zoomFactor, 0.0f);
                var p2 = new Vector3((target.x + w * 0.5f) * zoomFactor, (target.y + wire.targetPort * portSpacing) * zoomFactor, 0.0f);
                var c1 = GetOutCode(p1, clippingRect);
                var c2 = GetOutCode(p2, clippingRect);
                if ((c1 & c2) == 0)
                {
                    float thickness = 3.0f;
                    Handles.color = new Color(info.weight, 0.0f, 0.0f, (!highlightAudibleDSPChains || source.audible) ? 1.0f : 0.4f);
                    Handles.DrawAAPolyLine(thickness, 2, new Vector3[] { p1, p2 });
                }
            }

            // Draw connection weights on top of wires
            foreach (var wire in wires)
            {
                var source = wire.source;
                var target = wire.target;
                var info = wire.info;
                if (info.weight != 1.0f)
                {
                    int cx = source.x - ((xspacing + w) / 2);
                    int cy = (target != null) ? (int)(target.y + ((cx - target.x - w * 0.5f) * (float)(source.y - target.y) / (float)(source.x - target.x - w))) : target.y;
                    DrawRectClipped(
                        new Rect(cx - (cw / 2), cy - (ch / 2), cw, ch),
                        new Color(1.0f, 0.3f, 0.2f, (!highlightAudibleDSPChains || source.audible) ? 1.0f : 0.4f),
                        string.Format("{0:0.00}%", 100.0f * info.weight),
                        clippingRect,
                        zoomFactor);
                }
            }

            // Now draw the nodes
            foreach (var _node in nodes)
            {
                var node = _node.Value;
                var info = node.info;
                if (!nodes.ContainsKey(info.target) || node.firstTarget != nodes[info.target])
                    continue;
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
                name = name.Replace("FMOD Channel", "Channel");
                name = name.Replace("FMOD WaveTable Unit", "Wavetable");
                name = name.Replace("FMOD Resampler Unit", "Resampler");
                name = name.Replace("FMOD Channel DSPHead Unit", "Channel DSP");
                name = name.Replace("FMOD Channel DSPHead Unit", "Channel DSP");
                name += string.Format(" ({0:0.00}%)", cpuLoad);
                DrawRectClipped(new Rect(node.x - w * 0.5f, node.y - h * 0.5f, w, h), color, name, clippingRect, zoomFactor);
                if (node.audible)
                {
                    if (info.numLevels >= 1)
                    {
                        float p = (h - 6) * Mathf.Clamp(info.level1, 0.0f, 1.0f);
                        DrawRectClipped(new Rect(node.x - w * 0.5f + 3, node.y - h * 0.5f + 3, 4, h - 6), Color.black, null, clippingRect, zoomFactor);
                        DrawRectClipped(new Rect(node.x - w * 0.5f + 3, node.y - h * 0.5f - 3 + h - p, 4, p), Color.red, null, clippingRect, zoomFactor);
                    }
                    if (info.numLevels >= 2)
                    {
                        float p = (h - 6) * Mathf.Clamp(info.level2, 0.0f, 1.0f);
                        DrawRectClipped(new Rect(node.x - w * 0.5f + 8, node.y - h * 0.5f + 3, 4, h - 6), Color.black, null, clippingRect, zoomFactor);
                        DrawRectClipped(new Rect(node.x - w * 0.5f + 8, node.y - h * 0.5f - 3 + h - p, 4, p), Color.red, null, clippingRect, zoomFactor);
                    }
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
            {
                m_HeaderStyle = new GUIStyle("OL title");
            }

            m_HeaderStyle.alignment = TextAnchor.MiddleLeft;

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

            string[] headers = new[] { "Asset", "Load State", "Internal Load State", "Age", "Disposed", "Num Voices" };

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
                        title += m_TreeViewState.sortByDescendingOrder ? " \u25BC" : " \u25B2";

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
