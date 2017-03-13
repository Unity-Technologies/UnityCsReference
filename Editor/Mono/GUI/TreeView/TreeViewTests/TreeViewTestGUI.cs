// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;


namespace UnityEditor.TreeViewExamples
{
    internal class FooTreeViewItem : TreeViewItem
    {
        public BackendData.Foo foo { get; private set; }
        public FooTreeViewItem(int id, int depth, TreeViewItem parent, string displayName, BackendData.Foo foo)
            : base(id, depth, parent, displayName)
        {
            this.foo = foo;
        }
    }


    class TestGUI : TreeViewGUI
    {
        private Texture2D m_FolderIcon = EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName);
        private Texture2D m_Icon = EditorGUIUtility.FindTexture("boo Script Icon");
        private GUIStyle m_LabelStyle;
        private GUIStyle m_LabelStyleRightAlign;

        public TestGUI(TreeViewController treeView)
            : base(treeView)
        {
        }

        protected override Texture GetIconForItem(TreeViewItem item)
        {
            return (item.hasChildren) ? m_FolderIcon : m_Icon;
        }

        protected override void RenameEnded()
        {
        }

        protected override void SyncFakeItem()
        {
        }

        float[] columnWidths { get { return ((TreeViewStateWithColumns)m_TreeView.state).columnWidths; } }

        protected override void OnContentGUI(Rect rect, int row, TreeViewItem item, string label, bool selected, bool focused, bool useBoldFont, bool isPinging)
        {
            if (Event.current.rawType != EventType.Repaint)
                return;

            if (m_LabelStyle == null)
            {
                m_LabelStyle = new GUIStyle(Styles.lineStyle);
                m_LabelStyle.padding.left = m_LabelStyle.padding.right = 6;

                m_LabelStyleRightAlign = new GUIStyle(Styles.lineStyle);
                m_LabelStyleRightAlign.padding.right = m_LabelStyleRightAlign.padding.left = 6;
                m_LabelStyleRightAlign.alignment = TextAnchor.MiddleRight;
            }

            if (selected)
                Styles.selectionStyle.Draw(rect, false, false, true, focused);

            // If pinging just render main label and icon (not columns)
            if (isPinging || columnWidths == null || columnWidths.Length == 0)
            {
                base.OnContentGUI(rect, row, item, label, selected, focused, useBoldFont, isPinging);
                return;
            }

            Rect columnRect = rect;
            for (int i = 0; i < columnWidths.Length; ++i)
            {
                columnRect.width = columnWidths[i];
                if (i == 0)
                    base.OnContentGUI(columnRect, row, item, label, selected, focused, useBoldFont, isPinging);
                else
                    GUI.Label(columnRect, "Zksdf SDFS DFASDF ", (i % 2 == 0) ? m_LabelStyle : m_LabelStyleRightAlign);
                columnRect.x += columnRect.width;
            }
        }
    }
} // UnityEditor
