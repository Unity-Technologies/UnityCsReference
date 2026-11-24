// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace UnityEditor.Accessibility
{
    /// <summary>
    /// View that displays an accessibility hierarchy and properties of its nodes.
    /// </summary>
    internal class AccessibilityHierarchyViewer : VisualElement
    {
        private static readonly string s_UssClassName = "hierarchy-viewer";
        private static readonly string s_NoSelectionUssClassName = s_UssClassName + "--no-selection";

        /// Controls

        // Hierarchy view
        private AccessibilityHierarchyTreeView m_HierarchyView;

        // Inspector
        private readonly VisualElement m_InspectorContainer;

        private AccessibilityHierarchyViewModel m_HierarchyModel;

        private IVisualElementScheduledItem m_RebuildScheduledItem;

        /// <summary>
        /// The accessibility hierarchy view model
        /// </summary>
        public AccessibilityHierarchyViewModel hierarchyModel
        {
            get => m_HierarchyModel;
            set
            {
                if (m_HierarchyModel == value)
                    return;

                if (m_HierarchyModel != null)
                {
                    m_HierarchyModel.modelReset -= RebuildHierarchy;
                }

                m_HierarchyModel = value;

                if (m_HierarchyModel != null)
                {
                    m_HierarchyModel.modelReset += RebuildHierarchy;
                }

                RebuildHierarchy();
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public AccessibilityHierarchyViewer()
        {
            var asset = EditorGUIUtility.Load("Accessibility/HierarchyViewer.uxml") as VisualTreeAsset;
            var themeUssFilePath = $"Accessibility/HierarchyViewer{(EditorGUIUtility.isProSkin ? "Dark" : "Light")}.uss";
            var themeUss = EditorGUIUtility.Load(themeUssFilePath) as StyleSheet;

            asset.CloneTree(this);
            styleSheets.Add(themeUss);

            AddToClassList(s_UssClassName);

            m_HierarchyView = this.Q<AccessibilityHierarchyTreeView>("hierarchyView");

            // Inspector

            m_InspectorContainer = this.Q("inspectorContainer");

            this.Q<Toggle>("isActiveField").SetEnabled(false);
            this.Q<Toggle>("allowsDirectInteractionField").SetEnabled(false);
            // Make all input fields of the frame field readonly
            this.Q<RectField>("frameField").Query<FloatField>().ForEach(frameSubField => frameSubField.isReadOnly = true);

            m_HierarchyView.treeView.selectionChanged += (_) => OnSelectionChanged();
            OnSelectionChanged();
        }

        private void RebuildHierarchy()
        {
            if (m_HierarchyView == null)
                return;

            if (m_RebuildScheduledItem == null)
            {
                m_RebuildScheduledItem = schedule.Execute(HandleRebuildHierarchy);
            }
            else
            {
                m_RebuildScheduledItem.Resume();
            }
        }

        private void HandleRebuildHierarchy()
        {
            m_HierarchyView.SetRootItems(hierarchyModel != null ? new List<TreeViewItemData<AccessibilityViewModelNode>>() { RebuildHierarchy(hierarchyModel.root)} : null);
        }

        private static TreeViewItemData<AccessibilityViewModelNode> RebuildHierarchy(AccessibilityViewModelNode modelNode)
        {
            var childList = new List<TreeViewItemData<AccessibilityViewModelNode>>(modelNode.childNodeCount);

            for (var i = 0; i < modelNode.childNodeCount; ++i)
            {
                childList.Add(RebuildHierarchy(modelNode.GetChildNode(i)));
            }
            return new TreeViewItemData<AccessibilityViewModelNode>(modelNode.id, modelNode, childList);
        }

        private void RefreshInspector(AccessibilityViewModelNode node)
        {
            m_InspectorContainer.dataSource = node;
        }

        void OnSelectionChanged()
        {
            bool hasSelection = false;

            if (m_HierarchyView.treeView.selectedItem != null)
            {
                var node = (AccessibilityViewModelNode) m_HierarchyView.treeView.selectedItem;

                if (!node.isRoot)
                {
                    RefreshInspector(node);
                    hasSelection = true;
                }
            }
            else
            {
                RefreshInspector(default);
            }

            EnableInClassList(s_NoSelectionUssClassName, !hasSelection);
        }
    }
}
