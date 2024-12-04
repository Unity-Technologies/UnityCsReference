// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using TreeView = UnityEngine.UIElements.TreeView;

namespace UnityEditor.Accessibility
{
    class AccessibilityHierarchyTreeViewItem : VisualElement
    {
        public static readonly string s_UssClassName = AccessibilityHierarchyTreeView.s_UssClassName + "__item";
        public static readonly string s_RootUssClassName = s_UssClassName + "--root";
        public static readonly string s_InactiveUssClassName = s_UssClassName + "--inactive";
        public static readonly string s_IdTextUssClassName = s_UssClassName + "__id";
        public static readonly string s_LabelTextUssClassName = s_UssClassName + "__label";
        public static readonly string s_RoleTextUssClassName = s_UssClassName + "__role";

        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(isRoot), "is-root"),
                    new (nameof(id), "id"),
                    new (nameof(label), "label"),
                    new (nameof(role), "role"),
                    new (nameof(isActive), "is-active"),
                });
            }

            #pragma warning disable 649
            [SerializeField] bool isRoot;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags isRoot_UxmlAttributeFlags;
            [SerializeField] int id;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags id_UxmlAttributeFlags;
            [SerializeField] string label;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags label_UxmlAttributeFlags;
            [SerializeField] AccessibilityRole role;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags role_UxmlAttributeFlags;
            [SerializeField] bool isActive;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags isActive_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new AccessibilityHierarchyTreeViewItem();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (AccessibilityHierarchyTreeViewItem) obj;
                if (ShouldWriteAttributeValue(isRoot_UxmlAttributeFlags))
                    e.isRoot = isRoot;
                if (ShouldWriteAttributeValue(isActive_UxmlAttributeFlags))
                    e.isActive = isActive;
                if (ShouldWriteAttributeValue(id_UxmlAttributeFlags))
                    e.id = id;
                if (ShouldWriteAttributeValue(label_UxmlAttributeFlags))
                    e.label = label;
                if (ShouldWriteAttributeValue(role_UxmlAttributeFlags))
                    e.role = role;
            }
        }

        private readonly SearchableLabel m_IdText;
        private readonly SearchableLabel m_LabelText;
        private readonly SearchableLabel m_RoleText;
        private int m_Id;
        private bool m_IsRoot;
        private bool m_IsActive;
        private string m_Label;
        private AccessibilityRole m_Role;

        [CreateProperty]
        public bool isRoot
        {
            get => m_IsRoot;
            set
            {
                if (m_IsRoot == value)
                    return;
                m_IsRoot = value;
                UpdateTexts();
            }
        }

        [CreateProperty]
        public int id
        {
            get => m_Id;
            set
            {
                if (m_Id == value)
                    return;
                m_Id = value;
                UpdateTexts();
            }
        }

        [CreateProperty]
        public string label
        {
            get => m_Label;
            set
            {
                if (m_Label == value)
                    return;
                m_Label = value;
                UpdateTexts();
            }
        }

        [CreateProperty]
        public AccessibilityRole role
        {
            get => m_Role;
            set
            {
                if (m_Role == value)
                    return;
                m_Role = value;
                UpdateTexts();
            }
        }

        [CreateProperty]
        public bool isActive
        {
            get => m_IsActive;
            set
            {
                if (m_IsActive == value)
                    return;
                m_IsActive = value;
                UpdateTexts();
            }
        }

        public AccessibilityHierarchyTreeViewItem()
        {
            AddToClassList(s_UssClassName);
            m_IdText = new SearchableLabel();
            m_IdText.AddToClassList(s_IdTextUssClassName);
            m_LabelText = new SearchableLabel();
            m_LabelText.AddToClassList(s_LabelTextUssClassName);
            m_RoleText = new SearchableLabel();
            m_RoleText.AddToClassList(s_RoleTextUssClassName);
            Add(m_IdText);
            Add(m_LabelText);
            Add(m_RoleText);
            UpdateTexts();
        }

        public static string GetDisplayRoleText(AccessibilityRole role)
        {
            return role.ToString() + (role == AccessibilityRole.None ? L10n.Tr(" (Role)") : null);
        }

        public static string GetDisplayLabelText(bool active, string label)
        {
            return "\"" + ReplaceNewLines(label) + "\"" + (!active ? L10n.Tr(" (Inactive)") : null);
        }

        private void UpdateTexts()
        {
            m_IdText.text = isRoot ? null : id.ToString();
            m_LabelText.text = isRoot ? label : GetDisplayLabelText(isActive, label);
            m_RoleText.text = isRoot ? null : GetDisplayRoleText(role);
            EnableInClassList(s_InactiveUssClassName, !isActive);
        }

        /// <summary>
        /// Helper method used to replace all '\n' characters (new line) by "\n" string in the specified multiline text in order to display it in a single-line label.
        /// </summary>
        /// <param name="text">The source text to transform</param>
        /// <returns>Returns the source text with all '\n' characters replaced by "\n".</returns>
        private static string ReplaceNewLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return text.Replace("\n", "\\n");
        }
    }

    /// <summary>
    /// Tree view that displays an accessibility hierarchy.
    /// </summary>
    class AccessibilityHierarchyTreeView : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new AccessibilityHierarchyTreeView();
        }

        internal static readonly string s_UssClassName = "hierarchy-tree-view";

        private TreeView m_TreeView;
        private TreeViewSearchBar m_SearchBar;
        public TreeView treeView => m_TreeView;

        /// <summary>
        /// The selected node.
        /// </summary>
        public AccessibilityViewModelNode selectedNode => (m_TreeView.selectedItem != null) ? (AccessibilityViewModelNode) m_TreeView.selectedItem : default;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AccessibilityHierarchyTreeView()
        {
            var asset = EditorGUIUtility.Load("Accessibility/HierarchyTreeView.uxml") as VisualTreeAsset;

            asset.CloneTree(this);

            AddToClassList(s_UssClassName);

            m_TreeView = this.Q<TreeView>("treeView");
            m_SearchBar = this.Q<TreeViewSearchBar>("searchBar");
            m_SearchBar.treeView = m_TreeView;
            // Ensure that all items (including recycled items) have a non null data source by default to avoid binding errors.
            m_TreeView.dataSource ??= default(AccessibilityViewModelNode);
            m_TreeView.bindItem = (element, index) =>
            {
                var id = m_TreeView.GetIdForIndex(index);
                var node = m_TreeView.GetItemDataForId<AccessibilityViewModelNode>(id);

                // Cannot do element.dataSource = null as there seems to be an issue with Binding not updating the inherited data source properly.
                element.Q<AccessibilityHierarchyTreeViewItem>().dataSource = node;

                if (node.isRoot)
                {
                    element.parent.parent.AddToClassList(AccessibilityHierarchyTreeViewItem.s_RootUssClassName);
                    element.Query<SearchableLabel>().ForEach((label) => label.ClearHighlight());
                }
                else
                {
                    element.parent.parent.RemoveFromClassList(AccessibilityHierarchyTreeViewItem.s_RootUssClassName);

                    schedule.Execute(() =>
                    {
                        // Apply the current search query to the labels.
                        element.Query<SearchableLabel>().ForEach((label) => label.HighlightText(m_SearchBar.currentQuery));
                    }).ExecuteLater(200); // Delay to give time for the label.text to update first
                }
            };
        }

        /// <summary>
        /// Sets the root nodes
        /// </summary>
        /// <param name="rootItems"></param>
        /// <returns></returns>
        public void SetRootItems(List<TreeViewItemData<AccessibilityViewModelNode>> rootItems)
        {
            m_TreeView.ClearSelection();
            m_TreeView.SetRootItems(rootItems);
            m_TreeView.Rebuild();
            m_TreeView.ExpandAll();
            m_SearchBar.PerformSearch();
        }
    }
}
