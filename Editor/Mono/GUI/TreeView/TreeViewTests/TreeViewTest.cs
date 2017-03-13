// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Profiling;


namespace UnityEditor.TreeViewExamples
{
    internal class TreeViewStateWithColumns : TreeViewState
    {
        [SerializeField]
        public float[] columnWidths;
    }

    internal class TreeViewTest
    {
        private BackendData m_BackendData;
        private TreeViewController m_TreeView;
        private EditorWindow m_EditorWindow;
        private bool m_Lazy;
        private TreeViewColumnHeader m_ColumnHeader;
        private GUIStyle m_HeaderStyle;
        private GUIStyle m_HeaderStyleRightAligned;

        public int GetNumItemsInData()
        {
            return m_BackendData.IDCounter;
        }

        public int GetNumItemsInTree()
        {
            var data = m_TreeView.data as LazyTestDataSource;
            if (data != null)
                return data.itemCounter;

            var data2 = m_TreeView.data as TestDataSource;
            if (data2 != null)
                return data2.itemCounter;

            return -1;
        }

        public TreeViewTest(EditorWindow editorWindow, bool lazy)
        {
            m_EditorWindow = editorWindow;
            m_Lazy = lazy;
        }

        public void Init(Rect rect, BackendData backendData)
        {
            if (m_TreeView != null)
                return;

            m_BackendData = backendData;

            var state = new TreeViewStateWithColumns();
            state.columnWidths = new float[] {250f, 90f, 93f, 98f, 74f, 78f};

            m_TreeView = new TreeViewController(m_EditorWindow, state);
            ITreeViewGUI gui = new TestGUI(m_TreeView);
            ITreeViewDragging dragging = new TestDragging(m_TreeView, m_BackendData);
            ITreeViewDataSource dataSource;
            if (m_Lazy) dataSource = new LazyTestDataSource(m_TreeView, m_BackendData);
            else        dataSource = new TestDataSource(m_TreeView, m_BackendData);
            m_TreeView.Init(rect, dataSource, gui, dragging);


            m_ColumnHeader = new TreeViewColumnHeader();
            m_ColumnHeader.columnWidths = state.columnWidths;
            m_ColumnHeader.minColumnWidth = 30f;
            m_ColumnHeader.columnRenderer += OnColumnRenderer;
        }

        void OnColumnRenderer(int column, Rect rect)
        {
            if (m_HeaderStyle == null)
            {
                m_HeaderStyle = new GUIStyle(EditorStyles.toolbarButton);
                m_HeaderStyle.padding.left = 4;
                m_HeaderStyle.alignment = TextAnchor.MiddleLeft;

                m_HeaderStyleRightAligned = new GUIStyle(EditorStyles.toolbarButton);
                m_HeaderStyleRightAligned.padding.right = 4;
                m_HeaderStyleRightAligned.alignment = TextAnchor.MiddleRight;
            }

            string[] headers = new[] { "Name", "Date Modified", "Size", "Kind", "Author", "Platform", "Faster", "Slower" };
            GUI.Label(rect, headers[column], (column % 2 == 0) ? m_HeaderStyle : m_HeaderStyleRightAligned);
        }

        public void OnGUI(Rect rect)
        {
            int keyboardControl = GUIUtility.GetControlID(FocusType.Keyboard, rect);

            const float kHeaderHeight = 17f;
            const float kBottomHeight = 20f;
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, kHeaderHeight);
            Rect bottomRect = new Rect(rect.x, rect.yMax - kBottomHeight, rect.width, kBottomHeight);

            // Header
            GUI.Label(headerRect, "", EditorStyles.toolbar);
            m_ColumnHeader.OnGUI(headerRect);

            Profiler.BeginSample("TREEVIEW");

            // TreeView
            rect.y += headerRect.height;
            rect.height -= headerRect.height + bottomRect.height;
            m_TreeView.OnEvent();
            m_TreeView.OnGUI(rect, keyboardControl);

            Profiler.EndSample();

            // BottomBar
            GUILayout.BeginArea(bottomRect, GetHeader(), EditorStyles.helpBox);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            m_BackendData.m_RecursiveFindParentsBelow = GUILayout.Toggle(m_BackendData.m_RecursiveFindParentsBelow, GUIContent.Temp("Recursive"));
            if (GUILayout.Button("Ping", EditorStyles.miniButton))
            {
                int id = GetNumItemsInData() / 2;
                m_TreeView.Frame(id, true, true);
                m_TreeView.SetSelection(new[] {id}, false);
            }
            if (GUILayout.Button("Frame", EditorStyles.miniButton))
            {
                int id = GetNumItemsInData() / 10;
                m_TreeView.Frame(id, true, false);
                m_TreeView.SetSelection(new[] { id }, false);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private string GetHeader()
        {
            return (m_Lazy ? "LAZY: " : "FULL: ") + "GUI items: " + GetNumItemsInTree() + "  (data items: " + GetNumItemsInData() + ")";
        }
    }
} // UnityEditor
