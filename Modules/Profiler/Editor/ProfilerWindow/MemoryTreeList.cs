// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    internal class MemoryTreeList
    {
        internal class Styles
        {
            public GUIStyle background = "OL Box";
            public GUIStyle header = "OL title";
            public GUIStyle entryEven = "OL EntryBackEven";
            public GUIStyle entryOdd = "OL EntryBackOdd";
            public GUIStyle numberLabel = "OL Label";
            public GUIStyle foldout = "IN foldout";
        }

        private static Styles m_Styles;

        protected static Styles styles
        {
            get { return m_Styles ?? (m_Styles = new Styles()); }
        }

        private bool m_RequiresRefresh;
        public bool RequiresRefresh
        {
            get { return m_RequiresRefresh; }
            set { m_RequiresRefresh = value; }
        }

        const float kIndentPx = 16;
        const float kBaseIndent = 4;
        protected const float kSmallMargin = 4;
        protected const float kRowHeight = 16;

        protected const float kNameColumnSize = 300;
        protected const float kColumnSize = 70;

        protected const float kFoldoutSize = 14;

        public MemoryElementSelection m_MemorySelection;
        protected MemoryElement m_Root = null;

        protected EditorWindow m_EditorWindow;
        protected SplitterState m_Splitter;
        protected MemoryTreeList m_DetailView;
        protected int m_ControlID;
        protected Vector2 m_ScrollPosition;
        protected float m_SelectionOffset;
        protected float m_VisibleHeight;

        public MemoryTreeList(EditorWindow editorWindow, MemoryTreeList detailview)
        {
            m_MemorySelection = new MemoryElementSelection();
            m_EditorWindow = editorWindow;
            m_DetailView = detailview;
            m_ControlID = GUIUtility.GetPermanentControlID();
            SetupSplitter();
        }

        protected virtual void SetupSplitter()
        {
            float[] splitterRelativeSizes = new float[1];
            int[] splitterMinWidths = new int[1];

            splitterRelativeSizes[0] = kNameColumnSize;
            splitterMinWidths[0] = 100;

            m_Splitter = new SplitterState(splitterRelativeSizes, splitterMinWidths, null);
        }

        public void OnGUI()
        {
            GUILayout.BeginVertical();

            SplitterGUILayout.BeginHorizontalSplit(m_Splitter, EditorStyles.toolbar);
            DrawHeader();
            SplitterGUILayout.EndHorizontalSplit();

            if (m_Root == null)
            {
                GUILayout.EndVertical();
                return;
            }

            HandleKeyboard();

            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition, styles.background);
            int row = 0;

            foreach (MemoryElement memoryElement in m_Root.children)
            {
                DrawItem(memoryElement, ref row, 1);
                row++;
            }

            GUILayoutUtility.GetRect(0f, row * kRowHeight, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
                m_VisibleHeight = GUIClip.visibleRect.height;

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        private static float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        private bool FindNamedChild(string name, List<MemoryElement> list, out MemoryElement outChild)
        {
            foreach (var child in list)
            {
                if (child.name == name)
                {
                    outChild = child;
                    return true;
                }
            }
            outChild = null;
            return false;
        }

        private void RestoreViewState(MemoryElement oldRoot, MemoryElement newRoot)
        {
            foreach (MemoryElement memoryElement in newRoot.children)
            {
                memoryElement.ExpandChildren();
                if (memoryElement.ChildCount() == 0)
                    continue;

                MemoryElement child = null;
                if (FindNamedChild(memoryElement.name, oldRoot.children, out child))
                {
                    memoryElement.expanded = child.expanded;

                    if (memoryElement.expanded)
                    {
                        RestoreViewState(child, memoryElement);
                    }
                }
            }
        }

        public void SetRoot(MemoryElement root)
        {
            MemoryElement    oldRoot = m_Root;

            m_Root = root;
            if (m_Root != null)
                m_Root.ExpandChildren();
            if (m_DetailView != null)
                m_DetailView.SetRoot(null);

            // Attempt to restore the old state of things by walking the old tree
            if (oldRoot != null && m_Root != null)
                RestoreViewState(oldRoot, m_Root);
        }

        public MemoryElement GetRoot()
        {
            return m_Root;
        }

        protected static void DrawBackground(int row, bool selected)
        {
            var currentRect = GenerateRect(row);

            var background = (row % 2 == 0 ? styles.entryEven : styles.entryOdd);
            if (Event.current.type == EventType.Repaint)
                background.Draw(currentRect, GUIContent.none, false, false, selected, false);
        }

        protected virtual void DrawHeader()
        {
            GUILayout.Label("Referenced By:", styles.header);
        }

        protected static Rect GenerateRect(int row)
        {
            var rect = new Rect(1, kRowHeight * row, GUIClip.visibleRect.width, kRowHeight);
            return rect;
        }

        protected virtual void DrawData(Rect rect, MemoryElement memoryElement, int indent, int row, bool selected)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            string displayName = memoryElement.name + "(" + memoryElement.memoryInfo.className + ")";
            styles.numberLabel.Draw(rect, displayName, false, false, false, selected);
        }

        protected void DrawRecursiveData(MemoryElement element, ref int row, int indent)
        {
            if (element.ChildCount() == 0)
                return;

            element.ExpandChildren();
            foreach (MemoryElement elem in element.children)
            {
                row++;
                DrawItem(elem, ref row, indent);
            }
        }

        protected virtual void DrawItem(MemoryElement memoryElement, ref int row, int indent)
        {
            bool isSelected = m_MemorySelection.isSelected(memoryElement);
            DrawBackground(row, isSelected);

            Rect rect = GenerateRect(row);

            rect.x = kBaseIndent + indent * kIndentPx - kFoldoutSize;
            Rect toggleRect = rect;
            toggleRect.width = kFoldoutSize;
            if (memoryElement.ChildCount() > 0)
                memoryElement.expanded = GUI.Toggle(toggleRect, memoryElement.expanded, GUIContent.none, styles.foldout);

            rect.x += kFoldoutSize;

            if (isSelected)
                m_SelectionOffset = row * kRowHeight;

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                RowClicked(Event.current, memoryElement);
            }

            DrawData(rect, memoryElement, indent, row, isSelected);

            if (memoryElement.expanded)
                DrawRecursiveData(memoryElement, ref row, indent + 1);
        }

        protected void RowClicked(Event evt, MemoryElement memoryElement)
        {
            m_MemorySelection.SetSelection(memoryElement);
            GUIUtility.keyboardControl = m_ControlID;

            if (evt.clickCount == 2 && memoryElement.memoryInfo != null && memoryElement.memoryInfo.instanceId != 0)
            {
                Selection.instanceIDs = new int[0];
                Selection.activeInstanceID = memoryElement.memoryInfo.instanceId;
            }
            evt.Use();
            if (memoryElement.memoryInfo != null)
            {
                EditorGUIUtility.PingObject(memoryElement.memoryInfo.instanceId);
            }

            if (m_DetailView != null)
                m_DetailView.SetRoot(memoryElement.memoryInfo == null ? null : new MemoryElement(memoryElement.memoryInfo, false));

            m_EditorWindow.Repaint();
        }

        protected void HandleKeyboard()
        {
            Event evt = Event.current;
            if (evt.GetTypeForControl(m_ControlID) != EventType.KeyDown ||
                m_ControlID != GUIUtility.keyboardControl)
                return;

            if (m_MemorySelection.Selected == null)
                return;

            int count;

            switch (evt.keyCode)
            {
                case KeyCode.UpArrow:
                    m_MemorySelection.MoveUp();
                    break;
                case KeyCode.DownArrow:
                    m_MemorySelection.MoveDown();
                    break;
                case KeyCode.Home:
                    m_MemorySelection.MoveFirst();
                    break;
                case KeyCode.End:
                    m_MemorySelection.MoveLast();
                    break;
                case KeyCode.LeftArrow:
                    if (m_MemorySelection.Selected.expanded)
                        m_MemorySelection.Selected.expanded = false;
                    else
                        m_MemorySelection.MoveParent();
                    break;
                case KeyCode.RightArrow:
                    if (m_MemorySelection.Selected.ChildCount() > 0)
                        m_MemorySelection.Selected.expanded = true;
                    break;
                case KeyCode.PageUp:
                    count = Mathf.RoundToInt(m_VisibleHeight / kRowHeight);
                    for (int i = 0; i < count; i++)
                        m_MemorySelection.MoveUp();
                    break;
                case KeyCode.PageDown:
                    count = Mathf.RoundToInt(m_VisibleHeight / kRowHeight);
                    for (int i = 0; i < count; i++)
                        m_MemorySelection.MoveDown();
                    break;
                case KeyCode.Return:
                    if (m_MemorySelection.Selected.memoryInfo != null)
                    {
                        Selection.instanceIDs = new int[0];
                        Selection.activeInstanceID = m_MemorySelection.Selected.memoryInfo.instanceId;
                    }
                    break;
                default:
                    return;
            }
            RowClicked(evt, m_MemorySelection.Selected);
            EnsureVisible();
            m_EditorWindow.Repaint();
        }

        private void RecursiveFindSelected(MemoryElement element, ref int row)
        {
            if (m_MemorySelection.isSelected(element))
                m_SelectionOffset = row * kRowHeight;
            row++;

            if (!element.expanded || element.ChildCount() == 0)
                return;

            element.ExpandChildren();

            foreach (MemoryElement elem in element.children)
                RecursiveFindSelected(elem, ref row);
        }

        protected void EnsureVisible()
        {
            int row = 0;
            RecursiveFindSelected(m_Root, ref row);
            m_ScrollPosition.y = Clamp(m_ScrollPosition.y, m_SelectionOffset - m_VisibleHeight, m_SelectionOffset - kRowHeight);
        }
    }

    class MemoryTreeListClickable : MemoryTreeList
    {
        public MemoryTreeListClickable(EditorWindow editorWindow, MemoryTreeList detailview)
            : base(editorWindow, detailview)
        {
        }

        protected override void SetupSplitter()
        {
            float[] splitterRelativeSizes = new float[3];
            int[] splitterMinWidths = new int[3];

            splitterRelativeSizes[0] = kNameColumnSize;
            splitterMinWidths[0] = 100;
            splitterRelativeSizes[1] = kColumnSize;
            splitterMinWidths[1] = 50;
            splitterRelativeSizes[2] = kColumnSize;
            splitterMinWidths[2] = 50;

            m_Splitter = new SplitterState(splitterRelativeSizes, splitterMinWidths, null);
        }

        protected override void DrawHeader()
        {
            GUILayout.Label("Name", styles.header);
            GUILayout.Label("Memory", styles.header);
            GUILayout.Label("Ref count", styles.header);
        }

        protected override void DrawData(Rect rect, MemoryElement memoryElement, int indent, int row, bool selected)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            string displayName = memoryElement.name;
            if (memoryElement.ChildCount() > 0 && indent < 3)
                displayName += " (" + memoryElement.AccumulatedChildCount().ToString() + ")";

            int currentColumn = 0;
            rect.xMax = m_Splitter.realSizes[currentColumn];
            styles.numberLabel.Draw(rect, displayName, false, false, false, selected);
            rect.x = rect.xMax;
            rect.width = m_Splitter.realSizes[++currentColumn] - kSmallMargin;
            styles.numberLabel.Draw(rect, EditorUtility.FormatBytes(memoryElement.totalMemory), false, false, false, selected);
            rect.x += m_Splitter.realSizes[currentColumn++];
            rect.width = m_Splitter.realSizes[currentColumn] - kSmallMargin;

            if (memoryElement.ReferenceCount() > 0)
                styles.numberLabel.Draw(rect, memoryElement.ReferenceCount().ToString(), false, false, false, selected);
            else if (selected)
                styles.numberLabel.Draw(rect, "", false, false, false, selected);
        }
    }
}
