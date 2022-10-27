// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    class AddCurvesPopupHierarchyDataSource : TreeViewDataSource
    {
        struct TreeViewBuilder
        {
            struct KeyComparer : IComparer<Key>
            {
                static readonly Type s_GameObjectType = typeof(GameObject);
                static readonly Type s_TransformType = typeof(Transform);

                public int Compare(Key x, Key y)
                {
                    var result = String.Compare(x.path, y.path, StringComparison.Ordinal);
                    if (result == 0 && x.type != y.type)
                    {
                        // Make sure GameObject properties appear first, then Transform.
                        if (x.type == s_GameObjectType)
                            return -1;
                        if (y.type == s_GameObjectType)
                            return 1;
                        if (x.type == typeof(Transform))
                            return -1;
                        if (y.type == typeof(Transform))
                            return 1;

                        return String.Compare(x.type.Name, y.type.Name, StringComparison.Ordinal);
                    }

                    return result;
                }
            }

            struct Key
            {
                public string path;
                public Type type;
            }

            SortedDictionary<Key, List<EditorCurveBinding>> m_AccumulatedBindings;
            AnimationWindowState m_State;

            public TreeViewBuilder(AnimationWindowState state)
            {
                m_AccumulatedBindings = new SortedDictionary<Key, List<EditorCurveBinding>>(new KeyComparer());
                m_State = state;
            }

            public void Add(EditorCurveBinding binding)
            {
                var key = new Key { path = binding.path, type = binding.type };

                if (m_AccumulatedBindings.TryGetValue(key, out var bindings))
                    bindings.Add(binding);
                else
                    m_AccumulatedBindings[key] = new List<EditorCurveBinding>(new [] {binding});
            }

            public TreeViewItem CreateTreeView()
            {
                TreeViewItem rootNode;

                // Bindings of a single Component/ScriptableObject, skip the group node.
                if (m_AccumulatedBindings.Count == 1)
                {
                    rootNode = AddAnimatableObjectToHierarchy(m_AccumulatedBindings.First().Value, null, "");
                }
                else
                {
                    var groupNodes = new Dictionary<string, TreeViewItem>();
                    var childNodes = new Dictionary<TreeViewItem, List<TreeViewItem>>();
                    var inheritedNodeWeights = new Dictionary<TreeViewItem, int>();

                    rootNode = new AddCurvesPopupObjectNode(null, string.Empty, string.Empty);

                    TreeViewItem groupNode = rootNode;

                    groupNodes.Add(string.Empty, (rootNode));
                    childNodes.Add(groupNode, new List<TreeViewItem>());
                    inheritedNodeWeights.Add(groupNode, 0);

                    string currentPath = string.Empty;
                    foreach (var kvp in m_AccumulatedBindings)
                    {
                        if (!currentPath.Equals(kvp.Key.path))
                        {
                            TreeViewItem parentNode = rootNode;
                            var parentPath = GetParentPath(kvp.Key.path);

                            while (parentPath != null)
                            {
                                if (groupNodes.TryGetValue(parentPath, out var node))
                                {
                                    parentNode = node;
                                    break;
                                }

                                parentPath = GetParentPath(parentPath);
                            }

                            groupNode = new AddCurvesPopupObjectNode(parentNode, kvp.Key.path, "", GetObjectName(kvp.Key.path));
                            groupNodes.Add(kvp.Key.path, groupNode);
                            childNodes.Add(groupNode, new List<TreeViewItem>());
                            inheritedNodeWeights.Add(groupNode, 0);

                            childNodes[parentNode].Add(groupNode);

                            currentPath = kvp.Key.path;
                        }

                        var bindings = kvp.Value;

                        for (int i = bindings.Count - 1; i >= 0; --i)
                        {
                            // Let's not add those that already have a existing curve.
                            if (AnimationWindowUtility.IsCurveCreated(m_State.activeAnimationClip, bindings[i]))
                                bindings.RemoveAt(i);
                            // Remove animator enabled property which shouldn't be animated.
                            else if (bindings[i].type == typeof(Animator) && bindings[i].propertyName == "m_Enabled")
                                bindings.RemoveAt(i);
                        }

                        if (bindings.Count > 0)
                        {
                            // Builtin GameObject attributes.
                            if (kvp.Key.type == typeof(GameObject))
                            {
                                // Don't show for the root go
                                if (!string.IsNullOrEmpty(kvp.Key.path))
                                {
                                    TreeViewItem newNode = CreateNode(kvp.Value.ToArray(), groupNode, null);
                                    if (newNode != null)
                                        childNodes[groupNode].Add(newNode);
                                }
                            }
                            else
                            {
                                childNodes[groupNode].Add(AddAnimatableObjectToHierarchy(bindings, groupNode, kvp.Key.path));

                                var parentGroupNode = groupNode;
                                while (parentGroupNode != null)
                                {
                                    inheritedNodeWeights[parentGroupNode] += bindings.Count;
                                    parentGroupNode = parentGroupNode.parent;
                                }
                            }
                        }
                    }

                    // Remove empty leaves from tree view.
                    foreach (var kvp in inheritedNodeWeights)
                    {
                        // Remove Leaves nodes without properties.
                        if (inheritedNodeWeights[kvp.Key] == 0 && kvp.Key.parent != null)
                        {
                            childNodes[kvp.Key.parent].Remove(kvp.Key);
                            kvp.Key.parent = null;
                        }
                    }

                    // Set child parent references.
                    foreach (var kvp in childNodes)
                    {
                        TreeViewUtility.SetChildParentReferences(kvp.Value, kvp.Key);
                    }
                }

                m_AccumulatedBindings.Clear();

                return rootNode;
            }

            private string GetParentPath(string path)
            {
                if (String.IsNullOrEmpty(path))
                    return null;

                int index = path.LastIndexOf('/');
                if (index == -1)
                    return string.Empty;

                return path.Substring(0, index);
            }

            private string GetObjectName(string path)
            {
                if (String.IsNullOrEmpty(path))
                    return null;

                int index = path.LastIndexOf('/');
                if (index == -1)
                    return path;

                return path.Substring(index + 1);
            }

            private string GetClassName(EditorCurveBinding binding)
            {
                if (m_State.activeRootGameObject != null)
                {
                    Object target = AnimationUtility.GetAnimatedObject(m_State.activeRootGameObject, binding);
                    if (target != null)
                        return ObjectNames.GetInspectorTitle(target);
                }

                return binding.type.Name;
            }

            private Texture2D GetIcon(EditorCurveBinding binding)
            {
                return AssetPreview.GetMiniTypeThumbnail(binding.type);
            }

            private TreeViewItem AddAnimatableObjectToHierarchy(List<EditorCurveBinding> curveBindings, TreeViewItem parentNode, string path)
            {
                TreeViewItem node = new AddCurvesPopupObjectNode(parentNode, path, GetClassName(curveBindings[0]));
                node.icon = GetIcon(curveBindings[0]);

                List<TreeViewItem> childNodes = new List<TreeViewItem>();
                List<EditorCurveBinding> singlePropertyBindings = new List<EditorCurveBinding>();
                SerializedObject so = null;

                for (int i = 0; i < curveBindings.Count; i++)
                {
                    EditorCurveBinding curveBinding = curveBindings[i];
                    if (m_State.activeRootGameObject && curveBinding.isSerializeReferenceCurve)
                    {
                        var animatedObject = AnimationUtility.GetAnimatedObject(m_State.activeRootGameObject, curveBinding);
                        if (animatedObject != null && (so == null || so.targetObject != animatedObject))
                            so = new SerializedObject(animatedObject);
                    }

                    singlePropertyBindings.Add(curveBinding);

                    // We expect curveBindings to come sorted by propertyname
                    if (i == curveBindings.Count - 1 || AnimationWindowUtility.GetPropertyGroupName(curveBindings[i + 1].propertyName) != AnimationWindowUtility.GetPropertyGroupName(curveBinding.propertyName))
                    {
                        TreeViewItem newNode = CreateNode(singlePropertyBindings.ToArray(), node, so);
                        if (newNode != null)
                            childNodes.Add(newNode);
                        singlePropertyBindings.Clear();
                    }
                }

                childNodes.Sort();

                TreeViewUtility.SetChildParentReferences(childNodes, node);
                return node;
            }

            private TreeViewItem CreateNode(EditorCurveBinding[] curveBindings, TreeViewItem parentNode, SerializedObject so)
            {
                var node = new AddCurvesPopupPropertyNode(parentNode, curveBindings, AnimationWindowUtility.GetNicePropertyGroupDisplayName(curveBindings[0], so));

                // For RectTransform.position we only want .z
                if (AnimationWindowUtility.IsRectTransformPosition(node.curveBindings[0]))
                    node.curveBindings = new EditorCurveBinding[] {node.curveBindings[2]};

                node.icon = parentNode.icon;
                return node;
            }
        }

        public AddCurvesPopupHierarchyDataSource(TreeViewController treeView)
            : base(treeView)
        {
            showRootItem = false;
            rootIsCollapsable = false;
        }

        private void SetupRootNodeSettings()
        {
            showRootItem = false;
            SetExpanded(root, true);
        }

        public override void FetchData()
        {
            m_RootItem = null;
            if (AddCurvesPopup.s_State.selection.canAddCurves)
            {
                var state = AddCurvesPopup.s_State;
                AddBindingsToHierarchy(state.controlInterface.GetAnimatableBindings());
            }

            SetupRootNodeSettings();
            m_NeedRefreshRows = true;
        }

        private void AddBindingsToHierarchy(EditorCurveBinding[] bindings)
        {
            if (bindings == null || bindings.Length == 0)
            {
                m_RootItem = new AddCurvesPopupObjectNode(null, "", "");
                return;
            }

            var builder = new TreeViewBuilder(AddCurvesPopup.s_State);
            for (int i = 0; i < bindings.Length; i++)
            {
                builder.Add(bindings[i]);
            }

            m_RootItem = builder.CreateTreeView();
        }

        public void UpdateData()
        {
            m_TreeView.ReloadData();
        }
    }

    class AddCurvesPopupObjectNode : TreeViewItem
    {
        public AddCurvesPopupObjectNode(TreeViewItem parent, string path, string className, string displayName = null)
            : base((path + className).GetHashCode(), parent != null ? parent.depth + 1 : -1, parent, displayName ?? className)
        {
        }
    }

    class AddCurvesPopupPropertyNode : TreeViewItem
    {
        public EditorCurveBinding[] curveBindings;

        public AddCurvesPopupPropertyNode(TreeViewItem parent, EditorCurveBinding[] curveBindings, string displayName)
            : base(curveBindings[0].GetHashCode(), parent.depth + 1, parent, displayName)
        {
            this.curveBindings = curveBindings;
        }

        public override int CompareTo(TreeViewItem other)
        {
            AddCurvesPopupPropertyNode otherNode = other as AddCurvesPopupPropertyNode;
            if (otherNode != null)
            {
                if (displayName.Contains("Rotation") && otherNode.displayName.Contains("Position"))
                    return 1;
                if (displayName.Contains("Position") && otherNode.displayName.Contains("Rotation"))
                    return -1;
            }
            return base.CompareTo(other);
        }
    }
}
