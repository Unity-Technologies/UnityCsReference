// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Math = System.Math;
using IndexOutOfRangeException = System.IndexOutOfRangeException;


namespace UnityEditor
{
    internal partial class ObjectListArea
    {
        /* Abstract base class for each group of assets (e.g. Local, AssetStore) used in the ObjectListArea
         */
        abstract class Group
        {
            readonly protected float kGroupSeparatorHeight = EditorStyles.toolbar.fixedHeight;
            protected string m_GroupSeparatorTitle;

            protected static int[] s_Empty;
            public ObjectListArea m_Owner;
            public VerticalGrid m_Grid = new VerticalGrid();
            public float m_Height;

            public float Height { get { return m_Height; } }
            abstract public int ItemCount { get; }
            abstract public bool ListMode { get; set; }
            abstract public bool NeedsRepaint { get; protected set; }

            public bool Visible = true; // Visibility toggled in GUI
            public int ItemsAvailable = 0; // Calculated from total asset count
            public int ItemsWantedShown = 0; // Rows requested to be displayed
            protected bool m_Collapsable = true;
            public double m_LastClickedDrawTime = 0;

            public Group(ObjectListArea owner, string groupTitle)
            {
                m_GroupSeparatorTitle = groupTitle;
                if (s_Empty == null)
                    s_Empty = new int[0];
                m_Owner = owner;
                Visible = visiblePreference;
            }

            public bool visiblePreference
            {
                get
                {
                    if (string.IsNullOrEmpty(m_GroupSeparatorTitle))
                        return true;
                    return EditorPrefs.GetBool(m_GroupSeparatorTitle, true);
                }
                set
                {
                    if (string.IsNullOrEmpty(m_GroupSeparatorTitle))
                        return;
                    EditorPrefs.SetBool(m_GroupSeparatorTitle, value);
                }
            }


            // Called before repaints in order to prepare internal assets for rendering
            abstract public void UpdateAssets();

            // Called when height of this group should be recalculated
            abstract public void UpdateHeight();

            abstract protected void DrawInternal(int itemIdx, int endItem, float yOffset);

            // Called when the filter has changed
            abstract public void UpdateFilter(HierarchyType hierarchyType, SearchFilter searchFilter, bool showFoldersFirst);

            protected virtual float GetHeaderHeight()
            {
                return kGroupSeparatorHeight;
            }

            protected virtual void HandleUnusedDragEvents(float yOffset) {}

            int FirstVisibleRow(float yOffset, Vector2 scrollPos)
            {
                if (!Visible)
                    return -1;

                // Skip rows that is outside the offset rect
                float yRelOffset = scrollPos.y - (yOffset + GetHeaderHeight());

                int invisibleRows = 0;
                if (yRelOffset > 0f)
                {
                    // Initial rows hidden
                    float itemHeight = m_Grid.itemSize.y + m_Grid.verticalSpacing;
                    invisibleRows = (int)Mathf.Max(0, Mathf.Floor(yRelOffset / itemHeight));
                }
                return invisibleRows;
            }

            bool IsInView(float yOffset, Vector2 scrollPos, float scrollViewHeight)
            {
                if ((scrollPos.y + scrollViewHeight) < yOffset)
                    return false; // after visible area

                if ((yOffset + Height) < scrollPos.y)
                    return false; // before visible area

                return true;
            }

            // Main draw method of a group that is called from outside
            public void Draw(float yOffset, Vector2 scrollPos, ref int rowsInUse)
            {
                NeedsRepaint = false;

                // We need to always draw the header as it uses controlIDs (and we cannot cull gui elements using controlID)
                bool isRepaint = Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout;

                if (!isRepaint)
                    DrawHeader(yOffset, m_Collapsable); // logic here, draw on top below

                if (!IsInView(yOffset, scrollPos, m_Owner.m_VisibleRect.height))
                    return;

                int invisibleRows = FirstVisibleRow(yOffset, scrollPos);
                int beginItem = invisibleRows * m_Grid.columns;
                int totalItemCount = ItemCount;
                if (beginItem >= 0 && beginItem < totalItemCount)
                {
                    int itemIdx = beginItem;
                    // Limit by items avail and max items to show
                    // (plus an extra row to allow half a row in top and bottom at the same time)
                    int endItem = Math.Min(totalItemCount, m_Grid.rows * m_Grid.columns);

                    // Also limit to what can possible be in view in order to limit draws
                    float itemHeight = m_Grid.itemSize.y + m_Grid.verticalSpacing;
                    int rowsInVisibleRect = (int)Math.Ceiling(m_Owner.m_VisibleRect.height / itemHeight);

                    //When a row is hidden behind the header, it is still counted as visible, therefore to avoid
                    //weird popping in and out for the icons, we make sure that a new row will be rendered even if one
                    //is considered visible, even though it cannot be seen in the window
                    rowsInVisibleRect += 1;

                    int rowsNotInUse = rowsInVisibleRect - rowsInUse;
                    if (rowsNotInUse < 0)
                        rowsNotInUse = 0;

                    rowsInUse = Math.Min(rowsInVisibleRect, Mathf.CeilToInt((endItem - beginItem) / (float)m_Grid.columns));

                    endItem = rowsNotInUse * m_Grid.columns + beginItem;
                    if (endItem > totalItemCount)
                        endItem = totalItemCount;

                    DrawInternal(itemIdx, endItem, yOffset);
                }

                if (isRepaint)
                    DrawHeader(yOffset, m_Collapsable);

                // Always handle drag events in the case where we have no items we still want to be able to drag into the group.
                HandleUnusedDragEvents(yOffset);
            }

            protected void DrawObjectIcon(Rect position, Texture icon)
            {
                if (icon == null)
                    return;

                int size = icon.width;

                FilterMode temp = icon.filterMode;
                icon.filterMode = FilterMode.Point;
                GUI.DrawTexture(new Rect(position.x + ((int)position.width - size) / 2, position.y + ((int)position.height - size) / 2, size, size), icon, ScaleMode.ScaleToFit);
                icon.filterMode = temp;
            }

            protected void DrawDropShadowOverlay(Rect position, bool selected, bool isDropTarget, bool isRenaming)
            {
                // Draw dropshadow overlay
                float fraction = position.width / 128f;
                Rect dropShadowRect = new Rect(position.x - 4 * fraction, position.y - 2 * fraction, position.width + 8 * fraction, position.height + 12 * fraction - 0.5f);
                s_Styles.iconDropShadow.Draw(dropShadowRect, GUIContent.none, false, false, selected || isDropTarget, m_Owner.HasFocus() || isRenaming || isDropTarget);
            }

            protected void DrawHeaderBackground(Rect rect, bool firstHeader)
            {
                if (Event.current.type != EventType.Repaint)
                    return;

                // Draw the group bar background
                GUI.Label(rect, GUIContent.none, firstHeader ? s_Styles.groupHeaderTop : s_Styles.groupHeaderMiddle);
            }

            protected float GetHeaderYPosInScrollArea(float yOffset)
            {
                float y = yOffset;
                float yScrollPos = m_Owner.m_State.m_ScrollPosition.y;
                if (yScrollPos > yOffset)
                {
                    y = Mathf.Min(yScrollPos, yOffset + Height - kGroupSeparatorHeight);
                }
                return y;
            }

            virtual protected void DrawHeader(float yOffset, bool collapsable)
            {
                const int foldoutSpacing = 3;
                Rect rect = new Rect(0, GetHeaderYPosInScrollArea(yOffset), m_Owner.GetVisibleWidth(), kGroupSeparatorHeight - 1);

                DrawHeaderBackground(rect, yOffset == 0);

                // Draw the group toggle
                rect.x += 7;
                if (collapsable)
                {
                    bool oldVisible = Visible;
                    Visible = GUI.Toggle(rect, Visible, GUIContent.none, s_Styles.groupFoldout);
                    if (oldVisible ^ Visible)
                        visiblePreference = Visible;
                }

                // Draw title
                GUIStyle textStyle = s_Styles.groupHeaderLabel;
                if (collapsable)
                    rect.x += s_Styles.groupFoldout.fixedWidth + foldoutSpacing;
                rect.y += 1;
                if (!string.IsNullOrEmpty(m_GroupSeparatorTitle))
                    GUI.Label(rect, m_GroupSeparatorTitle, textStyle);

                if (s_Debug)
                {
                    Rect r2 = rect;
                    r2.x += 120;
                    GUI.Label(r2, AssetStorePreviewManager.StatsString());
                }

                rect.y -= 1;

                // Only draw counts if we have room for it
                if (m_Owner.GetVisibleWidth() > 150)
                    DrawItemCount(rect);
            }

            protected void DrawItemCount(Rect rect)
            {
                // Draw item count in group
                const float rightMargin = 4f;

                string label = ItemsAvailable.ToString() + " Total";
                Vector2 labelDims = s_Styles.groupHeaderLabelCount.CalcSize(new GUIContent(label));
                if (labelDims.x < rect.width)
                    rect.x = m_Owner.GetVisibleWidth() - labelDims.x - rightMargin; // right align if room
                rect.width = labelDims.x;
                rect.y += 2; // better y pos for minilabel
                GUI.Label(rect, label, s_Styles.groupHeaderLabelCount);
            }

            Object[] GetSelectedReferences()
            {
                return Selection.objects;
            }

            static string[] GetMainSelectedPaths()
            {
                List<string> paths = new List<string>();
                foreach (int instanceID in Selection.instanceIDs)
                {
                    if (AssetDatabase.IsMainAsset(instanceID))
                    {
                        string path = AssetDatabase.GetAssetPath(instanceID);
                        paths.Add(path);
                    }
                }

                return paths.ToArray();
            }
        }
    }
}  // namespace UnityEditor
