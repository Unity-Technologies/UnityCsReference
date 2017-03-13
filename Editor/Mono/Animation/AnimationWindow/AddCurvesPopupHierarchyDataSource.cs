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
    internal class AddCurvesPopupHierarchyDataSource : TreeViewDataSource
    {
        public static bool showEntireHierarchy { get; set; }

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
            if (AddCurvesPopup.selection == null)
                return;

            AnimationWindowSelectionItem[] selectionItems = AddCurvesPopup.selection.ToArray();
            if (selectionItems.Length > 1)
            {
                m_RootItem = new AddCurvesPopupObjectNode(null, "", "");
            }

            foreach (AnimationWindowSelectionItem item in selectionItems)
            {
                if (!item.canAddCurves)
                    continue;

                if (item.rootGameObject != null)
                {
                    AddGameObjectToHierarchy(item.rootGameObject, item, m_RootItem);
                }
                else if (item.scriptableObject != null)
                {
                    AddScriptableObjectToHierarchy(item.scriptableObject, item, m_RootItem);
                }
            }

            SetupRootNodeSettings();
            m_NeedRefreshRows = true;
        }

        private TreeViewItem AddGameObjectToHierarchy(GameObject gameObject, AnimationWindowSelectionItem selectionItem, TreeViewItem parent)
        {
            string path = AnimationUtility.CalculateTransformPath(gameObject.transform, selectionItem.rootGameObject.transform);
            TreeViewItem node = new AddCurvesPopupGameObjectNode(gameObject, parent, gameObject.name);
            List<TreeViewItem> childNodes = new List<TreeViewItem>();

            if (m_RootItem == null)
                m_RootItem = node;

            // Iterate over all animatable objects
            EditorCurveBinding[] allCurveBindings = AnimationUtility.GetAnimatableBindings(gameObject, selectionItem.rootGameObject);
            List<EditorCurveBinding> singleObjectBindings = new List<EditorCurveBinding>();
            for (int i = 0; i < allCurveBindings.Length; i++)
            {
                EditorCurveBinding curveBinding = allCurveBindings[i];

                singleObjectBindings.Add(curveBinding);

                // Don't create group for GameObject.m_IsActive. It looks messy
                if (curveBinding.propertyName == "m_IsActive")
                {
                    // Don't show for the root go
                    if (curveBinding.path != "")
                    {
                        TreeViewItem newNode = CreateNode(selectionItem, singleObjectBindings.ToArray(), node);
                        if (newNode != null)
                            childNodes.Add(newNode);
                        singleObjectBindings.Clear();
                    }
                    else
                    {
                        singleObjectBindings.Clear();
                    }
                }
                else
                {
                    // We expect allCurveBindings to come sorted by type

                    bool isLastItemOverall = (i == allCurveBindings.Length - 1);
                    bool isLastItemOnThisGroup = false;

                    if (!isLastItemOverall)
                        isLastItemOnThisGroup = (allCurveBindings[i + 1].type != curveBinding.type);

                    // Let's not add those that already have a existing curve.
                    if (AnimationWindowUtility.IsCurveCreated(selectionItem.animationClip, curveBinding))
                        singleObjectBindings.Remove(curveBinding);

                    // Remove animator enabled property which shouldn't be animated.
                    if (curveBinding.type == typeof(Animator) && curveBinding.propertyName == "m_Enabled")
                        singleObjectBindings.Remove(curveBinding);

                    if ((isLastItemOverall || isLastItemOnThisGroup) && singleObjectBindings.Count > 0)
                    {
                        childNodes.Add(AddAnimatableObjectToHierarchy(selectionItem, singleObjectBindings.ToArray(), node, path));
                        singleObjectBindings.Clear();
                    }
                }
            }

            if (showEntireHierarchy)
            {
                // Iterate over all child GOs
                for (int i = 0; i < gameObject.transform.childCount; i++)
                {
                    Transform childTransform = gameObject.transform.GetChild(i);
                    TreeViewItem childNode = AddGameObjectToHierarchy(childTransform.gameObject, selectionItem, node);
                    if (childNode != null)
                        childNodes.Add(childNode);
                }
            }

            TreeViewUtility.SetChildParentReferences(childNodes, node);
            return node;
        }

        private TreeViewItem AddScriptableObjectToHierarchy(ScriptableObject scriptableObject, AnimationWindowSelectionItem selectionItem, TreeViewItem parent)
        {
            EditorCurveBinding[] allCurveBindings = AnimationUtility.GetScriptableObjectAnimatableBindings(scriptableObject);
            EditorCurveBinding[] availableBindings = allCurveBindings.Where(c => !AnimationWindowUtility.IsCurveCreated(selectionItem.animationClip, c)).ToArray();

            TreeViewItem node = null;
            if (availableBindings.Length > 0)
                node = AddAnimatableObjectToHierarchy(selectionItem, availableBindings, parent, "");
            else
                node = new AddCurvesPopupObjectNode(parent, "", scriptableObject.name);

            if (m_RootItem == null)
                m_RootItem = node;

            return node;
        }

        static string GetClassName(AnimationWindowSelectionItem selectionItem, EditorCurveBinding binding)
        {
            if (selectionItem.rootGameObject != null)
            {
                Object target = AnimationUtility.GetAnimatedObject(selectionItem.rootGameObject, binding);
                if (target)
                    return ObjectNames.GetInspectorTitle(target);
            }

            return binding.type.Name;
        }

        static Texture2D GetIcon(AnimationWindowSelectionItem selectionItem, EditorCurveBinding binding)
        {
            if (selectionItem.rootGameObject != null)
            {
                return AssetPreview.GetMiniThumbnail(AnimationUtility.GetAnimatedObject(selectionItem.rootGameObject, binding));
            }
            else if (selectionItem.scriptableObject != null)
            {
                return AssetPreview.GetMiniThumbnail(selectionItem.scriptableObject);
            }

            return null;
        }

        private TreeViewItem AddAnimatableObjectToHierarchy(AnimationWindowSelectionItem selectionItem, EditorCurveBinding[] curveBindings, TreeViewItem parentNode, string path)
        {
            TreeViewItem node = new AddCurvesPopupObjectNode(parentNode, path, GetClassName(selectionItem, curveBindings[0]));
            node.icon = GetIcon(selectionItem, curveBindings[0]);

            List<TreeViewItem> childNodes = new List<TreeViewItem>();
            List<EditorCurveBinding> singlePropertyBindings = new List<EditorCurveBinding>();

            for (int i = 0; i < curveBindings.Length; i++)
            {
                EditorCurveBinding curveBinding = curveBindings[i];

                singlePropertyBindings.Add(curveBinding);

                // We expect curveBindings to come sorted by propertyname
                if (i == curveBindings.Length - 1 || AnimationWindowUtility.GetPropertyGroupName(curveBindings[i + 1].propertyName) != AnimationWindowUtility.GetPropertyGroupName(curveBinding.propertyName))
                {
                    TreeViewItem newNode = CreateNode(selectionItem, singlePropertyBindings.ToArray(), node);
                    if (newNode != null)
                        childNodes.Add(newNode);
                    singlePropertyBindings.Clear();
                }
            }

            childNodes.Sort();

            TreeViewUtility.SetChildParentReferences(childNodes, node);
            return node;
        }

        private TreeViewItem CreateNode(AnimationWindowSelectionItem selectionItem, EditorCurveBinding[] curveBindings, TreeViewItem parentNode)
        {
            var node = new AddCurvesPopupPropertyNode(parentNode, selectionItem, curveBindings);

            // For RectTransform.position we only want .z
            if (AnimationWindowUtility.IsRectTransformPosition(node.curveBindings[0]))
                node.curveBindings = new EditorCurveBinding[] {node.curveBindings[2]};

            node.icon = parentNode.icon;
            return node;
        }

        public void UpdateData()
        {
            m_TreeView.ReloadData();
        }
    }

    internal class AddCurvesPopupGameObjectNode : TreeViewItem
    {
        public AddCurvesPopupGameObjectNode(GameObject gameObject, TreeViewItem parent, string displayName)
            : base(gameObject.GetInstanceID(), parent != null ? parent.depth + 1 : -1, parent, displayName)
        {
        }
    }

    internal class AddCurvesPopupObjectNode : TreeViewItem
    {
        public AddCurvesPopupObjectNode(TreeViewItem parent, string path, string className)
            : base((path + className).GetHashCode(), parent != null ? parent.depth + 1 : -1, parent, className)
        {
        }
    }

    internal class AddCurvesPopupPropertyNode : TreeViewItem
    {
        public AnimationWindowSelectionItem selectionItem;
        public EditorCurveBinding[] curveBindings;

        public AddCurvesPopupPropertyNode(TreeViewItem parent, AnimationWindowSelectionItem selectionItem, EditorCurveBinding[] curveBindings)
            : base(curveBindings[0].GetHashCode(), parent.depth + 1, parent, AnimationWindowUtility.NicifyPropertyGroupName(curveBindings[0].type, AnimationWindowUtility.GetPropertyGroupName(curveBindings[0].propertyName)))
        {
            this.selectionItem = selectionItem;
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
