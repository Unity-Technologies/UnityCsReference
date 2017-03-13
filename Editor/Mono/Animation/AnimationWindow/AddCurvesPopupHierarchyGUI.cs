// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditorInternal
{
    internal class AddCurvesPopupHierarchyGUI : TreeViewGUI
    {
        public EditorWindow owner;
        public bool showPlusButton { get; set; }
        private GUIStyle plusButtonStyle = new GUIStyle("OL Plus");
        private GUIStyle plusButtonBackgroundStyle = new GUIStyle("Tag MenuItem");
        private const float plusButtonWidth = 17;

        public AddCurvesPopupHierarchyGUI(TreeViewController treeView, EditorWindow owner)
            : base(treeView, true)
        {
            this.owner = owner;
        }

        public override void OnRowGUI(Rect rowRect, TreeViewItem node, int row, bool selected, bool focused)
        {
            base.OnRowGUI(rowRect, node, row, selected, focused);

            // Is it propertynode. If not, then we don't need plusButton so quit here
            AddCurvesPopupPropertyNode hierarchyNode = node as AddCurvesPopupPropertyNode;
            if (hierarchyNode == null || hierarchyNode.curveBindings == null || hierarchyNode.curveBindings.Length == 0)
                return;

            Rect buttonRect = new Rect(rowRect.width - plusButtonWidth, rowRect.yMin, plusButtonWidth, plusButtonStyle.fixedHeight);

            // TODO Make a style for add curves popup
            // Draw background behind plus button to prevent text overlapping
            GUI.Box(buttonRect, GUIContent.none, plusButtonBackgroundStyle);

            // Check if the curve already exists and remove plus button
            if (GUI.Button(buttonRect, GUIContent.none, plusButtonStyle))
            {
                AddCurvesPopup.AddNewCurve(hierarchyNode);
                owner.Close();
            }
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
