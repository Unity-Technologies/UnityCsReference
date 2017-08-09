// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor.IMGUI.Controls
{
    internal abstract class TreeViewGUI : ITreeViewGUI
    {
        protected TreeViewController m_TreeView;
        protected PingData m_Ping = new PingData();
        protected Rect m_DraggingInsertionMarkerRect;
        protected bool m_UseHorizontalScroll;

        // Icon overlay
        public float iconLeftPadding { get; set; }
        public float iconRightPadding { get; set; }
        public float iconTotalPadding { get { return iconLeftPadding + iconRightPadding; } }
        public System.Action<TreeViewItem, Rect> iconOverlayGUI { get; set; } // Rect includes iconLeftPadding and iconRightPadding
        public System.Action<TreeViewItem, Rect> labelOverlayGUI { get; set; }

        private bool m_AnimateScrollBarOnExpandCollapse = true;

        // Layout
        public float k_LineHeight = 16f;
        public float k_BaseIndent = 2f;
        public float k_IndentWidth = 14f;
        public float k_IconWidth = 16f;
        public float k_SpaceBetweenIconAndText = 2f;
        public float k_TopRowMargin = 0f;
        public float k_BottomRowMargin = 0f;
        public float indentWidth { get { return k_IndentWidth + iconTotalPadding; } }
        public float k_HalfDropBetweenHeight = 4f;
        public float customFoldoutYOffset = 0f;
        public float extraInsertionMarkerIndent = 0f;
        public float extraSpaceBeforeIconAndLabel { get; set; }

        public float halfDropBetweenHeight { get { return k_HalfDropBetweenHeight; } }

        public virtual float topRowMargin { get { return k_TopRowMargin; } }
        public virtual float bottomRowMargin { get { return k_BottomRowMargin; } }

        // Styles
        internal static class Styles
        {
            public static GUIStyle foldout = "IN Foldout";
            public static GUIStyle insertion = new GUIStyle("PR Insertion");
            public static GUIStyle ping = new GUIStyle("PR Ping");
            public static GUIStyle toolbarButton = "ToolbarButton";
            public static GUIStyle lineStyle = new GUIStyle("PR Label");
            public static GUIStyle lineBoldStyle;
            public static GUIStyle selectionStyle = new GUIStyle("PR Label");
            public static GUIContent content = new GUIContent(EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName));

            static Styles()
            {
                // We want to render selection separately from text and icon, so clear background textures

                // TODO: Fix in new style setup
                var transparent = lineStyle.hover.background;
                lineStyle.onNormal.background = transparent;
                lineStyle.onActive.background = transparent;
                lineStyle.onFocused.background = transparent;
                lineStyle.alignment = TextAnchor.UpperLeft;     // We use UpperLeft to use same alignment as labels and controls so it is consistent when used in multi column treeviews
                lineStyle.padding.top = 2;                      // Use a value that centers the text when the rect height is EditorGUIUtility.singleLineHeight
                lineStyle.fixedHeight = 0;                      // Ensure drop marker rendering fits to entire rect

                lineBoldStyle = new GUIStyle(lineStyle);
                lineBoldStyle.font = EditorStyles.boldLabel.font;
                lineBoldStyle.fontStyle = EditorStyles.boldLabel.fontStyle;
                ping.padding.left = 16;
                ping.padding.right = 16;
                ping.fixedHeight = 16; // needed because otherwise height becomes the largest mip map size in the icon

                selectionStyle.fixedHeight = 0;
                selectionStyle.border = new RectOffset();

                insertion.overflow = new RectOffset(4, 0, 0, 0);
            }

            public static float foldoutWidth
            {
                get { return foldout.fixedWidth; }
            }
        }

        public TreeViewGUI(TreeViewController treeView)
        {
            m_TreeView = treeView;
        }

        public TreeViewGUI(TreeViewController treeView, bool useHorizontalScroll)
        {
            m_TreeView = treeView;
            m_UseHorizontalScroll = useHorizontalScroll;
        }

        virtual public void OnInitialize()
        {
        }

        protected virtual Texture GetIconForItem(TreeViewItem item)
        {
            return item.icon;
        }

        // ------------------
        // Size section

        // Calc correct width if horizontal scrollbar is wanted return new Vector2(1, height)
        virtual public Vector2 GetTotalSize()
        {
            // Width is 1 to prevent showing horizontal scrollbar
            float width = 1f;
            if (m_UseHorizontalScroll)
            {
                var rows = m_TreeView.data.GetRows();
                width = GetMaxWidth(rows);
            }

            // Height
            float height = m_TreeView.data.rowCount * k_LineHeight + topRowMargin + bottomRowMargin;


            if (m_AnimateScrollBarOnExpandCollapse && m_TreeView.expansionAnimator.isAnimating)
            {
                height -= m_TreeView.expansionAnimator.deltaHeight;
            }

            return new Vector2(width, height);
        }

        protected float GetMaxWidth(IList<TreeViewItem> rows)
        {
            float maxWidth = 1f;

            foreach (TreeViewItem item in rows)
            {
                float width = 0f;

                width += GetContentIndent(item);

                if (item.icon != null)
                    width += k_IconWidth;

                float minNameWidth, maxNameWidth;
                Styles.lineStyle.CalcMinMaxWidth(GUIContent.Temp(item.displayName), out minNameWidth, out maxNameWidth);
                width += maxNameWidth;

                // Add some padding to the back
                width += k_BaseIndent;

                if (width > maxWidth)
                    maxWidth = width;
            }

            return maxWidth;
        }

        virtual public int GetNumRowsOnPageUpDown(TreeViewItem fromItem, bool pageUp, float heightOfTreeView)
        {
            return (int)Mathf.Floor(heightOfTreeView / k_LineHeight);
        }

        // Should return the row index of the first and last row thats fits in the pixel rect defined by top and height
        virtual public void GetFirstAndLastRowVisible(out int firstRowVisible, out int lastRowVisible)
        {
            if (m_TreeView.data.rowCount == 0)
            {
                firstRowVisible = lastRowVisible = -1;
                return;
            }

            float topPixel = m_TreeView.state.scrollPos.y;
            float heightInPixels = m_TreeView.GetTotalRect().height;
            firstRowVisible = (int)Mathf.Floor((topPixel - topRowMargin) / k_LineHeight);
            lastRowVisible = firstRowVisible + (int)Mathf.Ceil(heightInPixels / k_LineHeight);

            firstRowVisible = Mathf.Max(firstRowVisible, 0);
            lastRowVisible = Mathf.Min(lastRowVisible, m_TreeView.data.rowCount - 1);

            // Validate
            if (firstRowVisible >= m_TreeView.data.rowCount && firstRowVisible > 0)
            {
                // Reset scroll if it was invalid, this can be the case if scroll y value was serialized and loading new tree data
                m_TreeView.state.scrollPos.y = 0f;
                GetFirstAndLastRowVisible(out firstRowVisible, out lastRowVisible);
            }
        }

        // ---------------------
        // OnGUI section

        virtual public void BeginRowGUI()
        {
            // Reset
            m_DraggingInsertionMarkerRect.x = -1;

            SyncFakeItem(); // After domain reload we ensure to reconstruct new Item state

            // Input for rename overlay (repainted in EndRowGUI to ensure rendered on top)
            if (Event.current.type != EventType.Repaint)
                DoRenameOverlay();
        }

        virtual public void EndRowGUI()
        {
            // Draw row marker when dragging
            if (m_DraggingInsertionMarkerRect.x >= 0 && Event.current.type == EventType.Repaint)
            {
                Styles.insertion.Draw(m_DraggingInsertionMarkerRect, false, false, false, false);
            }
            // Render rename overlay last (input is handled in BeginRowGUI)
            if (Event.current.type == EventType.Repaint)
                DoRenameOverlay();

            // Ping a Item
            HandlePing();
        }

        virtual public void OnRowGUI(Rect rowRect, TreeViewItem item, int row, bool selected, bool focused)
        {
            DoItemGUI(rowRect, row, item, selected, focused, false);
        }

        protected virtual void DrawItemBackground(Rect rect, int row, TreeViewItem item, bool selected, bool focused)
        {
            // override for custom rendering of background behind selection and drop effect rendering
        }

        public virtual Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
        {
            float offset = GetContentIndent(item) + extraSpaceBeforeIconAndLabel;

            if (GetIconForItem(item) != null)
                offset += k_SpaceBetweenIconAndText + k_IconWidth + iconTotalPadding;

            // By default we top align the rename rect to follow the label style, foldout and controls alignment
            return new Rect(rowRect.x + offset, rowRect.y, rowRect.width - offset, EditorGUIUtility.singleLineHeight);
        }

        virtual protected void DoItemGUI(Rect rect, int row, TreeViewItem item, bool selected, bool focused, bool useBoldFont)
        {
            EditorGUIUtility.SetIconSize(new Vector2(k_IconWidth, k_IconWidth)); // If not set we see icons scaling down if text is being cropped

            float indent = GetFoldoutIndent(item);

            int itemControlID = TreeViewController.GetItemControlID(item);

            bool isDropTarget = false;
            if (m_TreeView.dragging != null)
                isDropTarget = m_TreeView.dragging.GetDropTargetControlID() == itemControlID && m_TreeView.data.CanBeParent(item);
            bool isRenamingThisItem = IsRenaming(item.id);
            bool showFoldout = m_TreeView.data.IsExpandable(item);

            // Adjust edit field if needed (on repaint since on layout rect.width is invalid when using GUILayout)
            if (isRenamingThisItem && Event.current.type == EventType.Repaint)
            {
                GetRenameOverlay().editFieldRect = GetRenameRect(rect, row, item);
            }

            string label = item.displayName;
            if (isRenamingThisItem)
            {
                selected = false;
                label = "";
            }

            if (Event.current.type == EventType.Repaint)
            {
                // Draw background (can be overridden)
                DrawItemBackground(rect, row, item, selected, focused);

                // Draw selection
                if (selected)
                    Styles.selectionStyle.Draw(rect, false, false, true, focused);

                // Draw drop marker
                if (isDropTarget)
                    Styles.lineStyle.Draw(rect, GUIContent.none, true, true, false, false);

                // Show insertion marker below this item (rendered end of rows)
                if (m_TreeView.dragging != null && m_TreeView.dragging.GetRowMarkerControlID() == itemControlID)
                {
                    float yPos = (m_TreeView.dragging.drawRowMarkerAbove ? rect.y : rect.yMax) - Styles.insertion.fixedHeight * 0.5f;
                    m_DraggingInsertionMarkerRect = new Rect(rect.x + indent + extraInsertionMarkerIndent + Styles.foldoutWidth + Styles.lineStyle.margin.left, yPos, rect.width - indent, rect.height);
                }
            }

            // Do row content (icon, label, controls etc)
            OnContentGUI(rect, row, item, label, selected, focused, useBoldFont, false);

            // Do foldout
            if (showFoldout)
            {
                DoFoldout(rect, item, row);
            }

            EditorGUIUtility.SetIconSize(Vector2.zero);
        }

        float GetTopPixelOfRow(int row)
        {
            return row * k_LineHeight + topRowMargin;
        }

        public virtual Rect GetRowRect(int row, float rowWidth)
        {
            return new Rect(0, GetTopPixelOfRow(row), rowWidth, k_LineHeight);
        }

        public virtual Rect GetRectForFraming(int row)
        {
            return GetRowRect(row, 1); // We ignore width by default when framing (only y scroll is affected)
        }

        float GetFoldoutYPosition(float rectY)
        {
            // By default the arrow is aligned to the top to match text rendering
            return rectY + customFoldoutYOffset;
        }

        protected virtual Rect DoFoldout(Rect rect, TreeViewItem item, int row)
        {
            float indent = GetFoldoutIndent(item);
            Rect foldoutRect = new Rect(rect.x + indent, GetFoldoutYPosition(rect.y), Styles.foldoutWidth, EditorGUIUtility.singleLineHeight);
            FoldoutButton(foldoutRect, item, row, Styles.foldout);
            return foldoutRect;
        }

        protected virtual void FoldoutButton(Rect foldoutRect, TreeViewItem item, int row, GUIStyle foldoutStyle)
        {
            var expansionAnimator = m_TreeView.expansionAnimator;

            bool newExpandedValue;
            EditorGUI.BeginChangeCheck();
            {
                bool expandedState = expansionAnimator.IsAnimating(item.id) ? expansionAnimator.isExpanding : m_TreeView.data.IsExpanded(item);
                newExpandedValue = GUI.Toggle(foldoutRect, expandedState, GUIContent.none, foldoutStyle);
            }
            if (EditorGUI.EndChangeCheck())
            {
                m_TreeView.UserInputChangedExpandedState(item, row, newExpandedValue);
            }
        }

        protected virtual void OnContentGUI(Rect rect, int row, TreeViewItem item, string label, bool selected, bool focused, bool useBoldFont, bool isPinging)
        {
            if (Event.current.rawType != EventType.Repaint)
                return;

            if (!isPinging)
            {
                // The rect is assumed indented and sized after the content when pinging
                float indent = GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
                rect.xMin += indent;
            }

            GUIStyle lineStyle = useBoldFont ? Styles.lineBoldStyle : Styles.lineStyle;

            // Draw icon
            Rect iconRect = rect;
            iconRect.width = k_IconWidth;
            iconRect.x += iconLeftPadding;

            Texture icon = GetIconForItem(item);
            if (icon != null)
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

            if (iconOverlayGUI != null)
            {
                Rect iconOverlayRect = rect;
                iconOverlayRect.width = k_IconWidth + iconTotalPadding;
                iconOverlayGUI(item, iconOverlayRect);
            }

            // Draw text
            lineStyle.padding.left = 0;

            if (icon != null)
                rect.xMin += k_IconWidth + iconTotalPadding + k_SpaceBetweenIconAndText;
            lineStyle.Draw(rect, label, false, false, selected, focused);

            if (labelOverlayGUI != null)
            {
                labelOverlayGUI(item, rect);
            }
        }

        // Ping Item
        // -------------

        virtual public void BeginPingItem(TreeViewItem item, float topPixelOfRow, float availableWidth)
        {
            if (item == null)
                return;

            // Setup ping
            if (topPixelOfRow >= 0f)
            {
                m_Ping.m_TimeStart = Time.realtimeSinceStartup;
                m_Ping.m_PingStyle = Styles.ping;

                GUIContent cont = GUIContent.Temp(item.displayName);
                Vector2 contentSize = m_Ping.m_PingStyle.CalcSize(cont);

                m_Ping.m_ContentRect = new Rect(GetContentIndent(item) + extraSpaceBeforeIconAndLabel,
                        topPixelOfRow,
                        k_IconWidth + k_SpaceBetweenIconAndText + contentSize.x + iconTotalPadding,
                        contentSize.y);
                m_Ping.m_AvailableWidth = availableWidth;

                int row = m_TreeView.data.GetRow(item.id);

                bool useBoldFont = item.displayName.Equals("Assets");
                m_Ping.m_ContentDraw = (Rect r) =>
                    {
                        // get Item parameters from closure
                        OnContentGUI(r, row, item, item.displayName, false, false, useBoldFont, true);
                    };

                m_TreeView.Repaint();
            }
        }

        virtual public void EndPingItem()
        {
            m_Ping.m_TimeStart = -1f;
        }

        void HandlePing()
        {
            m_Ping.HandlePing();

            if (m_Ping.isPinging)
                m_TreeView.Repaint();
        }

        //-------------------
        // Rename section

        protected RenameOverlay GetRenameOverlay()
        {
            return m_TreeView.state.renameOverlay;
        }

        virtual protected bool IsRenaming(int id)
        {
            return GetRenameOverlay().IsRenaming() && GetRenameOverlay().userData == id && !GetRenameOverlay().isWaitingForDelay;
        }

        virtual public bool BeginRename(TreeViewItem item, float delay)
        {
            return GetRenameOverlay().BeginRename(item.displayName, item.id, delay);
        }

        virtual public void EndRename()
        {
            // We give keyboard focus back to our tree view because the rename utility stole it (now we give it back)
            if (GetRenameOverlay().HasKeyboardFocus())
                m_TreeView.GrabKeyboardFocus();

            RenameEnded();
            ClearRenameAndNewItemState(); // Ensure clearing if RenameEnden is overrided
        }

        virtual protected void RenameEnded() {}

        virtual public void DoRenameOverlay()
        {
            if (GetRenameOverlay().IsRenaming())
                if (!GetRenameOverlay().OnGUI())
                    EndRename();
        }

        virtual protected void SyncFakeItem() {}


        virtual protected void ClearRenameAndNewItemState()
        {
            m_TreeView.data.RemoveFakeItem();
            GetRenameOverlay().Clear();
        }

        virtual public float GetFoldoutIndent(TreeViewItem item)
        {
            // Ignore depth when showing search results
            if (m_TreeView.isSearching)
                return k_BaseIndent;

            return k_BaseIndent + item.depth * indentWidth;
        }

        virtual public float GetContentIndent(TreeViewItem item)
        {
            return GetFoldoutIndent(item) + Styles.foldoutWidth + Styles.lineStyle.margin.left;
        }
    }
}
