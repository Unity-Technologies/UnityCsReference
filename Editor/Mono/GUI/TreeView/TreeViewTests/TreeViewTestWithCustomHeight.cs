// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEngine;


namespace UnityEditor.TreeViewExamples
{
    internal class TreeViewTestWithCustomHeight
    {
        private BackendData m_BackendData;
        private TreeViewController m_TreeView;

        public TreeViewTestWithCustomHeight(EditorWindow editorWindow, BackendData backendData, Rect rect)
        {
            m_BackendData = backendData;

            var state = new TreeViewState();

            m_TreeView = new TreeViewController(editorWindow, state);
            var gui = new TestGUICustomItemHeights(m_TreeView);
            var dragging = new TestDragging(m_TreeView, m_BackendData);
            var dataSource = new TestDataSource(m_TreeView, m_BackendData);
            dataSource.onVisibleRowsChanged += gui.CalculateRowRects;
            m_TreeView.Init(rect, dataSource, gui, dragging);
            dataSource.SetExpanded(dataSource.root, true);
        }

        public void OnGUI(Rect rect)
        {
            int keyboardControl = GUIUtility.GetControlID(FocusType.Keyboard, rect);
            m_TreeView.OnGUI(rect, keyboardControl);
        }
    }
} // UnityEditor
