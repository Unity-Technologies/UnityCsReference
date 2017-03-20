// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditorInternal
{
    internal class AddCurvesPopupHierarchy
    {
        private TreeViewController m_TreeView;
        private TreeViewState m_TreeViewState;
        private AddCurvesPopupHierarchyDataSource m_TreeViewDataSource;

        public void OnGUI(Rect position, EditorWindow owner)
        {
            InitIfNeeded(owner, position);
            m_TreeView.OnEvent();
            m_TreeView.OnGUI(position, GUIUtility.GetControlID(FocusType.Keyboard));
        }

        public void InitIfNeeded(EditorWindow owner, Rect rect)
        {
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();
            else
                return;

            m_TreeView = new TreeViewController(owner, m_TreeViewState);

            m_TreeView.deselectOnUnhandledMouseDown = true;

            m_TreeViewDataSource = new AddCurvesPopupHierarchyDataSource(m_TreeView);
            TreeViewGUI gui = new AddCurvesPopupHierarchyGUI(m_TreeView, owner);

            m_TreeView.Init(rect,
                m_TreeViewDataSource,
                gui,
                null
                );

            m_TreeViewDataSource.UpdateData();
        }

        internal virtual bool IsRenamingNodeAllowed(TreeViewItem node)
        {
            return false;
        }
    }
}
