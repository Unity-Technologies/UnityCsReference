// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    internal class ExposeTransformEditor
    {
        private static class Styles
        {
            // TreeView column
            public static GUIContent TransformName = EditorGUIUtility.TrTextContent("Node Name");
            public static GUIContent EnableName = EditorGUIUtility.TrTextContent("Use", "Maintain Alt/Option key to enable or disable all children");
        }


        // TreeView
        private string[] m_TransformPaths;
        private SerializedProperty m_ExtraExposedTransformPaths;
        List<string> m_ExtraExposedTransformPathsList;
        TreeViewState m_TreeViewState;
        MultiColumnHeaderState m_ViewHeaderState;
        private OptimizeGameObjectTreeView m_ExposeTransformEditor;

        public void OnEnable(string[] transformPaths, SerializedObject serializedObject)
        {
            m_TransformPaths = transformPaths;
            m_ExtraExposedTransformPaths = serializedObject.FindProperty("m_ExtraExposedTransformPaths");

            SetupOptimizeGameObjectTreeView();

            ResetExposedTransformList();
        }

        public void ResetExposedTransformList()
        {
            int exposedNodeCount = m_ExtraExposedTransformPaths.arraySize;
            m_ExtraExposedTransformPathsList = new List<string>(exposedNodeCount);
            if (exposedNodeCount > 0)
            {
                var nodeIterator = m_ExtraExposedTransformPaths.GetArrayElementAtIndex(0);
                for (int i = 0; i < exposedNodeCount; ++i)
                {
                    m_ExtraExposedTransformPathsList.Add(nodeIterator.stringValue);
                    nodeIterator.Next(false);
                }
            }
        }

        public void OnGUI()
        {
            var listRect = GUILayoutUtility.GetRect(10, m_ExposeTransformEditor.totalHeightIncludingSearchBarAndBottomBar, GUILayout.ExpandWidth(true));
            m_ExposeTransformEditor.OnGUI(listRect);
        }

        void SetupOptimizeGameObjectTreeView()
        {
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = Styles.EnableName,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = false,
                    width = 31f, minWidth = 31f, maxWidth = 31f,
                    autoResize = true, allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = Styles.TransformName,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    autoResize = true, allowToggleVisibility = false,
                }
            };
            var newHeader = new MultiColumnHeaderState(columns);
            if (m_ViewHeaderState != null)
            {
                MultiColumnHeaderState.OverwriteSerializedFields(m_ViewHeaderState, newHeader);
            }
            m_ViewHeaderState = newHeader;
            var multiColumnHeader = new MultiColumnHeader(m_ViewHeaderState);
            multiColumnHeader.ResizeToFit();
            m_ExposeTransformEditor = new OptimizeGameObjectTreeView(m_TreeViewState, multiColumnHeader, FillNodeInfos);
            if (m_ExposeTransformEditor.searchString == null)
                m_ExposeTransformEditor.searchString = string.Empty;
        }

        SerializedNodeInfo FillNodeInfos()
        {
            var rootNode = new SerializedNodeInfo() { depth = -1, displayName = "", id = 0, children = new List<TreeViewItem>(0) };
            if (m_TransformPaths == null || m_TransformPaths.Length < 1)
            {
                return rootNode;
            }

            var nodesCount = m_TransformPaths.Length;
            var nodeInfos = new List<SerializedNodeInfo>(nodesCount - 1);

            // skip the first index as it is the empty root of the gameObject
            for (int i = 1; i < nodesCount; i++)
            {
                var newNode = new SerializedNodeInfo();
                newNode.id = i;
                newNode.path = m_TransformPaths[i];
                newNode.getNodeState = GetNodeState;
                newNode.setNodeState = SetNodeState;

                newNode.depth = newNode.path.Count(f => f == '/') + 1;

                int lastIndex = newNode.path.LastIndexOf('/');
                lastIndex = lastIndex == -1 ? 0 : lastIndex + 1;
                newNode.displayName = newNode.path.Substring(lastIndex);

                nodeInfos.Add(newNode);
            }

            TreeViewUtility.SetChildParentReferences(nodeInfos.Cast<TreeViewItem>().ToList(), rootNode);
            return rootNode;
        }

        private bool GetNodeState(string nodePath)
        {
            return m_ExtraExposedTransformPathsList.Contains(nodePath);
        }

        private void SetNodeState(string nodePath, bool state)
        {
            if (GetNodeState(nodePath) != state)
            {
                if (state)
                {
                    m_ExtraExposedTransformPaths.InsertArrayElementAtIndex(m_ExtraExposedTransformPaths.arraySize);
                    m_ExtraExposedTransformPaths.GetArrayElementAtIndex(m_ExtraExposedTransformPaths.arraySize - 1).stringValue = nodePath;
                    m_ExtraExposedTransformPathsList.Add(nodePath);
                }
                else
                {
                    var index = m_ExtraExposedTransformPathsList.IndexOf(nodePath);
                    m_ExtraExposedTransformPaths.DeleteArrayElementAtIndex(index);
                    m_ExtraExposedTransformPathsList.RemoveAt(index);
                }
            }
        }
    }

    internal class OptimizeGameObjectTreeView : ToggleTreeView<SerializedNodeInfo>
    {
        static class Styles
        {
            public static GUIContent iconToolbarPlusMore = EditorGUIUtility.TrIconContent("Toolbar Plus More", "Choose to add to list");
            public static GUIContent iconToolbarMinus = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove selection from list");
            public static GUIStyle footerBackground = "RL Footer";
            public static GUIStyle preButton = "RL FooterButton";
        }

        public OptimizeGameObjectTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader, Func<SerializedNodeInfo> rebuildRoot)
            : base(state, multiColumnHeader, rebuildRoot)
        {
        }
    }

    internal class SerializedNodeInfo : ToggleTreeViewItem
    {
        public string path;
        public Func<string, bool> getNodeState;
        public Action<string, bool> setNodeState;

        public override bool nodeState
        {
            get { return getNodeState != null && getNodeState(path); }

            set
            {
                setNodeState?.Invoke(path, value);
            }
        }
    }
}
