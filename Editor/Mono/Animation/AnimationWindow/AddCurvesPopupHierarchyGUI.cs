// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;

namespace UnityEditorInternal
{
    internal class AddCurvesPopupHierarchyGUI : TreeViewGUI
    {
        public EditorWindow owner;
        public bool showPlusButton { get; set; }
        private GUIStyle buttonStyle = "IconButton";
        private GUIContent plusIcon = EditorGUIUtility.TrIconContent("Toolbar Plus");
        private GUIStyle plusButtonBackgroundStyle = "Tag MenuItem";
        private GUIContent addPropertiesContent = EditorGUIUtility.TrTextContent("Add Properties");
        private const float plusButtonWidth = 17;

        static Texture2D warningIcon = (Texture2D)EditorGUIUtility.LoadRequired("Icons/ShortcutManager/alertDialog.png");
        public static GUIStyle warningIconStyle;

        public AddCurvesPopupHierarchyGUI(TreeViewController treeView, EditorWindow owner)
            : base(treeView, true)
        {
            this.owner = owner;
            warningIconStyle = new GUIStyle();
            warningIconStyle.margin = new RectOffset(15, 15, 15, 15);
        }

        public override void OnRowGUI(Rect rowRect, TreeViewItem node, int row, bool selected, bool focused)
        {
            bool propertyPathMismatchWithHumanAvatar = false;
            AddCurvesPopupGameObjectNode addCurvesPopupNode = node as AddCurvesPopupGameObjectNode;
            if (addCurvesPopupNode != null)
            {
                propertyPathMismatchWithHumanAvatar = addCurvesPopupNode.propertyPathMismatchWithHumanAvatar;
            }

            using (new EditorGUI.DisabledScope(propertyPathMismatchWithHumanAvatar))
            {
                base.OnRowGUI(rowRect, node, row, selected, focused);
                DoAddCurveButton(rowRect, node);
                HandleContextMenu(rowRect, node);
            }

            if (propertyPathMismatchWithHumanAvatar)
            {
                Rect iconRect = new Rect(rowRect.width - plusButtonWidth, rowRect.yMin, plusButtonWidth, buttonStyle.fixedHeight);
                GUI.Label(iconRect, new GUIContent(warningIcon, "The Avatar definition does not match the property path. Please author using a hierarchy the Avatar was built with."), warningIconStyle);
            }
        }

        private void DoAddCurveButton(Rect rowRect, TreeViewItem node)
        {
            // Is it propertynode. If not, then we don't need plusButton so quit here
            AddCurvesPopupPropertyNode hierarchyNode = node as AddCurvesPopupPropertyNode;
            if (hierarchyNode == null || hierarchyNode.curveBindings == null || hierarchyNode.curveBindings.Length == 0)
                return;

            Rect buttonRect = new Rect(rowRect.width - plusButtonWidth, rowRect.yMin, plusButtonWidth, buttonStyle.fixedHeight);

            // TODO Make a style for add curves popup
            // Draw background behind plus button to prevent text overlapping
            GUI.Box(buttonRect, GUIContent.none, plusButtonBackgroundStyle);

            // Check if the curve already exists and remove plus button
            if (GUI.Button(buttonRect, plusIcon, buttonStyle))
            {
                AddCurvesPopup.AddNewCurve(hierarchyNode);

                // Hold shift key to add new curves and keep window opened.
                if (Event.current.shift)
                    m_TreeView.ReloadData();
                else
                    owner.Close();
            }
        }

        private void HandleContextMenu(Rect rowRect, TreeViewItem node)
        {
            if (Event.current.type != EventType.ContextClick)
                return;

            if (rowRect.Contains(Event.current.mousePosition))
            {
                // Add current node to selection
                var ids = new List<int>(m_TreeView.GetSelection());
                ids.Add(node.id);
                m_TreeView.SetSelection(ids.ToArray(), false, false);

                GenerateMenu().ShowAsContext();
                Event.current.Use();
            }
        }

        private GenericMenu GenerateMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(addPropertiesContent, false, AddPropertiesFromSelectedNodes);

            return menu;
        }

        private void AddPropertiesFromSelectedNodes()
        {
            int[] ids = m_TreeView.GetSelection();
            for (int i = 0; i < ids.Length; ++i)
            {
                var node = m_TreeView.FindItem(ids[i]);
                var propertyNode = node as AddCurvesPopupPropertyNode;

                if (propertyNode != null)
                {
                    AddCurvesPopup.AddNewCurve(propertyNode);
                }
                else if (node.hasChildren)
                {
                    foreach (var childNode in node.children)
                    {
                        var childPropertyNode = childNode as AddCurvesPopupPropertyNode;
                        if (childPropertyNode != null)
                        {
                            AddCurvesPopup.AddNewCurve(childPropertyNode);
                        }
                    }
                }
            }

            m_TreeView.ReloadData();
        }

        public float GetContentWidth()
        {
            IList<TreeViewItem> rows = m_TreeView.data.GetRows();
            List<TreeViewItem> allRows = new List<TreeViewItem>();
            allRows.AddRange(rows);

            for (int i = 0; i < allRows.Count; ++i)
            {
                var row = allRows[i];
                if (row.hasChildren)
                    allRows.AddRange(row.children);
            }

            float rowWidth = GetMaxWidth(allRows);
            return rowWidth + plusButtonWidth;
        }

        override protected void SyncFakeItem()
        {
            //base.SyncFakeItem();
        }

        override protected void RenameEnded()
        {
            //base.RenameEnded();
        }

        override protected bool IsRenaming(int id)
        {
            return false;
        }

        public override bool BeginRename(TreeViewItem item, float delay)
        {
            return false;
        }

        override protected Texture GetIconForItem(TreeViewItem item)
        {
            if (item != null)
                return item.icon;

            return null;
        }
    }
}
