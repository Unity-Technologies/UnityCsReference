// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using UnityEditor.IMGUI.Controls;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // TreeView Item for SketchUp mesh.
    class SketchUpNode : TreeViewItem
    {
        public SketchUpNodeInfo Info;
        public bool Enabled
        {
            get { return Info.enabled; }
            set
            {
                if (Info.enabled != value)
                {
                    if (value)
                    {
                        ToggleParent(value);
                    }
                    ToggleChildren(value);
                    Info.enabled = value;
                }
            }
        }

        public SketchUpNode(int id, int depth, TreeViewItem parent, string displayName, SketchUpNodeInfo info)
            : base(id, depth, parent, displayName)
        {
            this.Info = info;
            children = new List<TreeViewItem>();
        }

        // Toggle Parent Item.
        private void ToggleParent(bool toggle)
        {
            SketchUpNode parentItem = parent as SketchUpNode;
            if (parentItem != null)
            {
                parentItem.ToggleParent(toggle);
                parentItem.Info.enabled = toggle;
            }
        }

        // Enable Item
        private void ToggleChildren(bool toggle)
        {
            foreach (var child in children)
            {
                SketchUpNode item = child as SketchUpNode;
                item.Info.enabled = toggle;
                item.ToggleChildren(toggle);
            }
        }
    }

    // Treeview DataSource.
    class SketchUpDataSource : TreeViewDataSource
    {
        SketchUpNodeInfo[] m_Nodes;
        const int k_ProgressUpdateStep = 50;
        public SketchUpDataSource(TreeViewController treeView, SketchUpNodeInfo[] nodes)
            : base(treeView)
        {
            m_Nodes = nodes;
            FetchData();
        }

        public int[] FetchEnableNodes()
        {
            List<int> enable = new List<int>();
            InternalFetchEnableNodes(m_RootItem as SketchUpNode, enable);
            return enable.ToArray();
        }

        void InternalFetchEnableNodes(SketchUpNode node, List<int> enable)
        {
            if (node.Enabled)
            {
                enable.Add(node.Info.nodeIndex);
            }
            foreach (var childNode in node.children)
            {
                InternalFetchEnableNodes(childNode as SketchUpNode, enable);
            }
        }

        public override void FetchData()
        {
            // first item is the root. always.
            m_RootItem = new SketchUpNode(m_Nodes[0].nodeIndex, 0, null, m_Nodes[0].name, m_Nodes[0]);
            // We are relying on the assumption that
            // 1. the nodes are already sorted by nodeIndex
            // 2. Parent's nodeIndex < child's nodeIndex
            List<SketchUpNode> nodeList = new List<SketchUpNode>();
            nodeList.Add(m_RootItem as SketchUpNode);
            SetExpanded(m_RootItem, true);
            int previousNodeIndex = m_Nodes[0].nodeIndex;
            for (int i = 1; i < m_Nodes.Length; ++i)
            {
                SketchUpNodeInfo node = m_Nodes[i];
                if (node.parent < 0 || node.parent > nodeList.Count)
                    continue;

                if (node.parent >= i)
                {
                    Debug.LogError("Parent node index is greater than child node");
                    continue;
                }
                if (previousNodeIndex >= node.nodeIndex)
                {
                    Debug.LogError("Node array is not sorted by nodeIndex");
                    continue;
                }

                SketchUpNode parent = nodeList[node.parent];
                SketchUpNode suNode = new SketchUpNode(node.nodeIndex, parent.depth + 1, parent, node.name, node);
                parent.children.Add(suNode);
                SetExpanded(suNode, suNode.Info.enabled);
                nodeList.Add(suNode);

                if (i % k_ProgressUpdateStep == 0)
                {
                    EditorUtility.DisplayProgressBar("SketchUp Import", "Building Node Selection", (float)i / m_Nodes.Length);
                }
            }
            EditorUtility.ClearProgressBar();

            m_NeedRefreshRows = true;
        }

        override public bool CanBeParent(TreeViewItem item)
        {
            return item.hasChildren;
        }

        override public bool IsRenamingItemAllowed(TreeViewItem item)
        {
            return false;
        }
    }

    class SketchUpTreeViewGUI : TreeViewGUI
    {
        readonly Texture2D k_Root = EditorGUIUtility.FindTexture(typeof(DefaultAsset));
        readonly Texture2D k_Icon = EditorGUIUtility.FindTexture(typeof(Mesh));

        public SketchUpTreeViewGUI(TreeViewController treeView)
            : base(treeView)
        {
            k_BaseIndent = 20;
        }

        protected override Texture GetIconForItem(TreeViewItem item)
        {
            return (item.children != null && item.children.Count > 0) ? k_Root : k_Icon;
        }

        protected override void RenameEnded()
        {
        }

        protected override void SyncFakeItem()
        {
        }

        public override void OnRowGUI(Rect rowRect, TreeViewItem node, int row, bool selected, bool focused)
        {
            DoItemGUI(rowRect, row, node, selected, focused, false);
            SketchUpNode suNode = node as SketchUpNode;

            Rect toggleRect = new Rect(2, rowRect.y, rowRect.height, rowRect.height);

            suNode.Enabled = GUI.Toggle(toggleRect, suNode.Enabled, GUIContent.none, SketchUpImportDlg.Styles.styles.toggleStyle);
        }
    }

    internal class SketchUpImportDlg : EditorWindow
    {
        private TreeViewController m_TreeView;
        private SketchUpTreeViewGUI m_ImportGUI;
        private SketchUpDataSource m_DataSource;
        private int[] m_Selection;
        private WeakReference m_ModelEditor;

        const float kHeaderHeight = 25f;
        const float kBottomHeight = 28f;
        readonly Vector2 m_WindowMinSize = new Vector2(350, 350);

        TreeViewState m_TreeViewState;

        internal class Styles
        {
            public readonly float buttonWidth;
            public readonly GUIStyle headerStyle;
            public readonly GUIStyle toggleStyle;
            public readonly GUIStyle footerStyle;
            public readonly GUIContent okButton = EditorGUIUtility.TrTextContent("OK");
            public readonly GUIContent cancelButton = EditorGUIUtility.TrTextContent("Cancel");
            public readonly GUIContent nodesLabel = EditorGUIUtility.TrTextContent("Select the SketchUp nodes to import", "Nodes in the file hierarchy");
            public readonly GUIContent windowTitle = EditorGUIUtility.TrTextContent("SketchUp Node Selection Dialog", "SketchUp Node Selection Dialog");

            public Styles()
            {
                buttonWidth = 32f;
                headerStyle = new GUIStyle(EditorStyles.toolbar);
                headerStyle.fixedHeight = kHeaderHeight;
                headerStyle.padding.left = 4;
                headerStyle.alignment = TextAnchor.MiddleLeft;
                toggleStyle = new GUIStyle(EditorStyles.toggle);
                toggleStyle.padding.left = 8;
                toggleStyle.alignment = TextAnchor.MiddleCenter;
                footerStyle = new GUIStyle("toolbarBottom");
                footerStyle.padding.top = 4;
                footerStyle.fixedHeight = kBottomHeight;
            }

            static Styles s_Styles = null;

            public static Styles styles
            {
                get { return s_Styles ?? (s_Styles = new Styles()); }
            }
        }

        public void Init(SketchUpNodeInfo[] nodes, SketchUpImporterModelEditor suModelEditor)
        {
            titleContent = Styles.styles.windowTitle;
            minSize = m_WindowMinSize;
            position = new Rect(position.x, position.y, minSize.x, minSize.y);

            m_TreeViewState = new TreeViewState();

            m_TreeView = new TreeViewController(this, m_TreeViewState);

            m_ImportGUI = new SketchUpTreeViewGUI(m_TreeView);
            m_DataSource = new SketchUpDataSource(m_TreeView, nodes);
            m_TreeView.Init(position, m_DataSource, m_ImportGUI, null);

            m_TreeView.selectionChangedCallback += OnTreeSelectionChanged;

            m_ModelEditor = new WeakReference(suModelEditor);

            isModal = false;
        }

        bool isModal
        {
            get; set;
        }

        internal static void Launch(SketchUpNodeInfo[] nodes, SketchUpImporterModelEditor suModelEditor)
        {
            SketchUpImportDlg win = EditorWindow.GetWindowDontShow<SketchUpImportDlg>();
            win.Init(nodes, suModelEditor);
            win.ShowAuxWindow();
        }

        internal static int[] LaunchAsModal(SketchUpNodeInfo[] nodes)
        {
            SketchUpImportDlg win = EditorWindow.GetWindowDontShow<SketchUpImportDlg>();
            win.Init(nodes, null);
            win.isModal = true;
            win.ShowModal();
            return win.m_DataSource.FetchEnableNodes();
        }

        void HandleKeyboardEvents()
        {
            Event evt = Event.current;

            if (evt.type == EventType.KeyDown &&
                (evt.keyCode == KeyCode.Space ||
                 evt.keyCode == KeyCode.Return ||
                 evt.keyCode == KeyCode.KeypadEnter))
            {
                if (m_Selection != null && m_Selection.Length > 0)
                {
                    SketchUpNode node = m_TreeView.FindItem(m_Selection[0]) as SketchUpNode;
                    if (node != null && node != m_DataSource.root)
                    {
                        node.Enabled = !node.Enabled;
                        evt.Use();
                        Repaint();
                    }
                }
            }
        }

        public void OnTreeSelectionChanged(int[] selection)
        {
            m_Selection = selection;
        }

        void OnGUI()
        {
            Rect rect = new Rect(0, 0, position.width, position.height);
            int keyboardControl = GUIUtility.GetControlID(FocusType.Keyboard, rect);

            // Header
            Rect headerRect = new Rect(0, 0, position.width, kHeaderHeight);
            GUI.Label(headerRect, string.Empty, Styles.styles.headerStyle);
            GUI.Label(new Rect(10, 2, position.width, kHeaderHeight), Styles.styles.nodesLabel);

            Rect bottomRect = new Rect(rect.x, rect.yMax - kBottomHeight, rect.width, kBottomHeight);

            // Footer
            GUILayout.BeginArea(bottomRect);
            GUILayout.BeginHorizontal(Styles.styles.footerStyle);
            GUILayout.FlexibleSpace();
            bool closeWindow = false;
            if (isModal)
            {
                if (GUILayout.Button(Styles.styles.okButton))
                {
                    closeWindow = true;
                }
            }
            else
            {
                if (GUILayout.Button(Styles.styles.cancelButton))
                {
                    closeWindow = true;
                }
                else if (GUILayout.Button(Styles.styles.okButton))
                {
                    closeWindow = true;
                    if (m_ModelEditor.IsAlive)
                    {
                        SketchUpImporterModelEditor modelEditor = m_ModelEditor.Target as SketchUpImporterModelEditor;
                        modelEditor.SetSelectedNodes(m_DataSource.FetchEnableNodes());
                    }
                }
            }

            GUILayout.Space(10);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // TreeView
            rect.y = kHeaderHeight;
            rect.height -= headerRect.height + bottomRect.height;
            m_TreeView.OnEvent();
            m_TreeView.OnGUI(rect, keyboardControl);

            HandleKeyboardEvents();
            if (closeWindow)
            {
                Close();
            }
        }
    }
}
