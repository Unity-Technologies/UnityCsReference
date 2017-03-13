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
        class AssetStoreGroup : Group
        {
            public const int kDefaultRowsShown = 3;
            public const int kDefaultRowsShownListMode = 10;
            const int kMoreButtonOffset = 3;
            const int kMoreRowsAdded = 10;
            const int kMoreRowsAddedListMode = 75;
            const int kMaxQueryItems = 1000; // Max items allowed to query for by backend

            GUIContent m_Content = new GUIContent();
            List<AssetStoreAsset> m_Assets;
            string m_Name;
            bool m_ListMode;
            Vector3 m_ShowMoreDims;

            public string Name { get { return m_Name; } }
            public List<AssetStoreAsset> Assets { get { return m_Assets; } set { m_Assets = value; } }
            public override int ItemCount { get { return Math.Min(m_Assets.Count, ItemsWantedShown); } }
            public override bool ListMode
            {
                get { return m_ListMode; }
                set { m_ListMode = value; }
            }

            public bool NeedItems
            {
                get
                {
                    // Clamp to 1000 items since that is what the backend supports
                    int wantedItems = Math.Min(kMaxQueryItems, ItemsWantedShown);
                    int gotItems = Assets.Count;
                    return (ItemsAvailable >= wantedItems && gotItems < wantedItems) ||
                        (ItemsAvailable < wantedItems && gotItems < ItemsAvailable);
                }
            }

            public override bool NeedsRepaint { get; protected set; }

            public AssetStoreGroup(ObjectListArea owner, string groupTitle, string groupName) : base(owner, groupTitle)
            {
                m_Assets = new List<AssetStoreAsset>();
                m_Name = groupName;
                m_ListMode = false;
                m_ShowMoreDims = EditorStyles.miniButton.CalcSize(new GUIContent("Show more"));
                m_Owner.UpdateGroupSizes(this);
                ItemsWantedShown = kDefaultRowsShown * m_Grid.columns;
            }

            public override void UpdateAssets()
            {
            }

            protected override void DrawInternal(int itemIdx, int endItem, float yOffset)
            {
                int endContainerItem = m_Assets.Count;
                int startItemIdx = itemIdx;

                // The seperator bar is drawn before all items
                yOffset += kGroupSeparatorHeight;

                bool isRepaintEvent = Event.current.type == EventType.Repaint;

                Rect r;

                if (ListMode)
                {
                    // Draw row
                    //position.width = Mathf.Max (position.width, 500); // For some reason the icon is clipped if the rect cannot contain both the text and icon so make sure its large
                    for (; itemIdx < endItem && itemIdx < endContainerItem; itemIdx++)
                    {
                        r = m_Grid.CalcRect(itemIdx, yOffset);
                        // Mouse event handling
                        int clicks = HandleMouse(r);
                        if (clicks != 0)
                            m_Owner.SetSelection(m_Assets[itemIdx], clicks == 2);

                        if (isRepaintEvent)
                        {
                            bool selected = !AssetStoreAssetSelection.Empty && AssetStoreAssetSelection.ContainsAsset(m_Assets[itemIdx].id);
                            DrawLabel(r, m_Assets[itemIdx], selected);
                        }
                    }
                }
                else
                {
                    // First loop over assets to draw icons and the loop again to
                    // draw labels. Two loops in order to get rid of too many
                    // expensive setShader calls.
                    for (; itemIdx < endItem && itemIdx < endContainerItem; itemIdx++)
                    {
                        r = m_Grid.CalcRect(itemIdx, yOffset);

                        // Label has selection visuals now (keep below)
                        // bool selected = !AssetStoreAssetSelection.Empty && AssetStoreAssetSelection.ContainsAsset (m_Assets[itemIdx].id);
                        // bool selected = false;

                        // Mouse event handling
                        int clicks = HandleMouse(r);
                        if (clicks != 0)
                            m_Owner.SetSelection(m_Assets[itemIdx], clicks == 2);

                        if (isRepaintEvent)
                        {
                            Rect position = new Rect(r.x, r.y, r.width, r.height - s_Styles.resultsGridLabel.fixedHeight);
                            DrawIcon(position, m_Assets[itemIdx]);
                        }
                    }
                    itemIdx = startItemIdx;

                    // Labels
                    if (isRepaintEvent)
                    {
                        for (; itemIdx < endItem && itemIdx < endContainerItem; itemIdx++)
                        {
                            r = m_Grid.CalcRect(itemIdx, yOffset);
                            bool selected = !AssetStoreAssetSelection.Empty && AssetStoreAssetSelection.ContainsAsset(m_Assets[itemIdx].id);
                            DrawLabel(r, m_Assets[itemIdx], selected);
                        }
                    }
                }

                // "Show more" button if more asset can by found in asset store
                if (ItemsAvailable <= (m_Grid.rows * m_Grid.columns))
                    return; // no more items to fetch from A$ server

                r = new Rect(m_Owner.GetVisibleWidth() - m_ShowMoreDims.x - 6,
                        yOffset +  m_Grid.height + kMoreButtonOffset,
                        m_ShowMoreDims.x,
                        m_ShowMoreDims.y);

                if (ItemsAvailable > (m_Grid.rows * m_Grid.columns) &&
                    ItemsAvailable >= Assets.Count &&
                    Assets.Count < kMaxQueryItems)
                {
                    Event evt = Event.current;
                    switch (evt.type)
                    {
                        case EventType.MouseDown:
                            if (evt.button == 0 && r.Contains(evt.mousePosition))
                            {
                                if (ListMode)
                                {
                                    ItemsWantedShown += kMoreRowsAddedListMode;
                                }
                                else
                                {
                                    // Try to make sure that we have full rows of preview
                                    // after the update. Partly filled rows can occur when resizing the
                                    // listarea or when we reached the limit of results from server.
                                    int itemsNeededToFillLastRow = m_Grid.columns - (ItemCount % m_Grid.columns);
                                    itemsNeededToFillLastRow = itemsNeededToFillLastRow % m_Grid.columns;
                                    ItemsWantedShown += (kMoreRowsAdded * m_Grid.columns) + itemsNeededToFillLastRow;
                                }
                                if (NeedItems) // Check that we need to fetch it since we might already have it prefetched
                                    m_Owner.QueryAssetStore();
                                evt.Use();
                            }
                            break;
                        case EventType.Repaint:
                        {
                            EditorStyles.miniButton.Draw(r,  "More", false, false, false, false);
                        }
                        break;
                    }
                }
            }

            AssetStorePreviewManager.CachedAssetStoreImage GetIconForAssetStoreAsset(AssetStoreAsset assetStoreResource)
            {
                if (!string.IsNullOrEmpty(assetStoreResource.staticPreviewURL))
                {
                    m_Owner.LastScrollTime++;
                    AssetStorePreviewManager.CachedAssetStoreImage item = AssetStorePreviewManager.TextureFromUrl(assetStoreResource.staticPreviewURL, assetStoreResource.name, m_Owner.gridSize, s_Styles.resultsGridLabel, s_Styles.previewBg, false);
                    return item;
                }

                return null;
            }

            void DrawIcon(Rect position, AssetStoreAsset assetStoreResource)
            {
                // bool selected = !AssetStoreAssetSelection.Empty && AssetStoreAssetSelection.ContainsAsset (assetStoreResource.id);
                bool selected = false; // labels have selection rendering only

                m_Content.text = null;

                AssetStorePreviewManager.CachedAssetStoreImage icon = GetIconForAssetStoreAsset(assetStoreResource);

                if (icon == null)
                {
                    // Draw static preview bg color as bg for icons that do not fill the entire rect (mini icons and non-square textures)
                    // s_Styles.previewBg.Draw (position, GUIContent.none, false, false, false, false);

                    // Use builtin type icon
                    Texture2D iconImage = InternalEditorUtility.GetIconForFile(assetStoreResource.name);
                    s_Styles.resultsGrid.Draw(position, iconImage, false, false, selected, selected);
                }
                else
                {
                    // Fade in
                    m_Content.image = icon.image;
                    Color c = icon.color;
                    Color oldColor = GUI.color;
                    if (c.a != 1.0f)
                        GUI.color = c;
                    s_Styles.resultsGrid.Draw(position, m_Content, false, false, selected, selected);
                    if (c.a != 1.0f)
                    {
                        GUI.color = oldColor;
                        // The icon is not entirely faded in yet. Schedule a repaint
                        NeedsRepaint = true;
                    }

                    DrawDropShadowOverlay(position, selected, false, false);
                }
            }

            void DrawLabel(Rect position, AssetStoreAsset assetStoreResource, bool selected)
            {
                if (ListMode)
                {
                    position.width = Mathf.Max(position.width, 500); // For some reason the icon is clipped if the rect cannot contain both the text and icon so make sure its large
                    m_Content.text = assetStoreResource.displayName;

                    // For now disable drawing preview icons in list mode since it looks wrong because of the dark background on
                    // preview images.
                    m_Content.image = InternalEditorUtility.GetIconForFile(assetStoreResource.name);

                    s_Styles.resultsLabel.Draw(position, m_Content, false, false, selected, selected);
                }
                else
                {
                    // We crop the text in Preview mode
                    int pseudoInstanceID = assetStoreResource.id + 10000000; // we add a large offset to the asset store id to ensure there is no overlap with instanceIDs
                    string labeltext = m_Owner.GetCroppedLabelText(pseudoInstanceID, assetStoreResource.displayName, position.width);
                    position.height -= s_Styles.resultsGridLabel.fixedHeight;
                    // The -1 is to ensure the label has same width as the image and that it aligns with the bottom of the image
                    s_Styles.resultsGridLabel.Draw(new Rect(position.x, position.yMax + 1, position.width - 1,
                            s_Styles.resultsGridLabel.fixedHeight), labeltext,
                        false, false, selected, m_Owner.HasFocus());
                }
            }

            public override void UpdateFilter(HierarchyType hierarchyType, SearchFilter searchFilter, bool showFoldersFirst)
            {
                ItemsWantedShown = ListMode ? kDefaultRowsShownListMode : (kDefaultRowsShown * m_Grid.columns);
                Assets.Clear();
            }

            public override void UpdateHeight()
            {
                // We always show the seperator bar
                m_Height = (int)kGroupSeparatorHeight;

                if (!Visible)
                    return;

                m_Height += m_Grid.height;

                if (ItemsAvailable <= (m_Grid.rows * m_Grid.columns))
                    return; // no more items to fetch from A$ server

                m_Height += kMoreButtonOffset * 2 + (int)m_ShowMoreDims.y;
            }

            public int IndexOf(int assetID)
            {
                int idx = 0;
                foreach (AssetStoreAsset a in m_Assets)
                {
                    if (a.id == assetID)
                        return idx;
                    idx++;
                }
                return -1;
            }

            public AssetStoreAsset AssetAtIndex(int selectedIdx)
            {
                if (selectedIdx >= m_Grid.rows * m_Grid.columns)
                    return null;
                if (selectedIdx < m_Grid.rows * m_Grid.columns && selectedIdx > ItemCount)
                    return m_Assets.Last();

                int idx = 0;
                foreach (AssetStoreAsset a in m_Assets)
                {
                    if (selectedIdx == idx)
                        return a;
                    idx++;
                }

                return null;
            }

            // Handle mouse events for rect and return number of clicks
            protected int HandleMouse(Rect position)
            {
                Event evt = Event.current;

                switch (evt.type)
                {
                    case EventType.MouseDown:
                        // handle selection of the item
                        if (evt.button == 0 && position.Contains(evt.mousePosition))
                        {
                            // we've actually clicked the item
                            m_Owner.Repaint();

                            if (evt.clickCount == 2)
                            {
                                evt.Use();
                                return 2;
                            }
                            else
                            {
                                m_Owner.ScrollToPosition(ObjectListArea.AdjustRectForFraming(position));
                                evt.Use();
                                return 1;
                            }
                        }
                        break;
                    case EventType.ContextClick:
                        if (position.Contains(evt.mousePosition))
                        {
                            //evt.Use(); // We do not use the event this should be handled by client
                            return 1;
                        }
                        break;
                }
                return 0;
            }
        }
    }
}  // namespace UnityEditor
