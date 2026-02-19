// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Profiling
{
    // ============================================================================
    // DATA MODEL - Optimized with cached values
    // ============================================================================

    internal enum NodeType : byte  // Use byte to save memory
    {
        Atlas = 0,
        Texture = 1,
        Sprite = 2
    }

    internal class SpriteAtlasProfilerInfoWrapper
    {
        public int id;
        public string assetName;
        public string assetGuid;
        public string spriteAssetName;
        public string textureAssetName;
        public float ratioSpriteInTexture;

        public NodeType nodeType;
        public int childCount;

        // Cached display strings to avoid recreating in BindCell
        private string m_CachedDisplayText;
        private bool m_DisplayTextDirty = true;

        public SpriteAtlasProfilerInfoWrapper(int entityId, string assetName, string assetGuid,
            string spriteAssetName, string textureAssetName, float ratioSpriteInTexture, NodeType nodeType = NodeType.Sprite)
        {
            this.id = entityId;
            this.assetName = assetName;
            this.assetGuid = assetGuid;
            this.spriteAssetName = spriteAssetName;
            this.textureAssetName = textureAssetName;
            this.ratioSpriteInTexture = ratioSpriteInTexture;
            this.nodeType = nodeType;
            this.childCount = 0;
        }

        public string GetDisplayText()
        {
            if (!m_DisplayTextDirty && m_CachedDisplayText != null)
                return m_CachedDisplayText;

            // Build display text once and cache it
            switch (nodeType)
            {
                case NodeType.Atlas:
                    m_CachedDisplayText = String.Format("{0} ( {1} textures, {2} sprites )", assetName, childCount, id);
                    break;
                case NodeType.Texture:
                    float textureCoverage = ratioSpriteInTexture * 100f;
                    m_CachedDisplayText = String.Format("{0} ( {1} sprites, {2}% coverage )", textureAssetName, childCount, textureCoverage.ToString("F1"));
                    break;
                case NodeType.Sprite:
                    float spriteRatio = ratioSpriteInTexture * 100f;
                    m_CachedDisplayText = String.Format("{0} ( {1}% )", spriteAssetName, spriteRatio.ToString("F2"));
                    break;
            }

            m_DisplayTextDirty = false;
            return m_CachedDisplayText;
        }

        public void InvalidateCache()
        {
            m_DisplayTextDirty = true;
        }
    }

    // ============================================================================
    // TEXTURE NODE - Optimized with cached calculations
    // ============================================================================

    internal class TextureNode
    {
        public string textureName;
        public int id;
        public List<SpriteAtlasProfilerInfoWrapper> sprites;

        private int m_CachedSpriteCount = -1;
        private float m_CachedTotalRatio = -1f;

        public TextureNode(string textureName, int textureId)
        {
            this.textureName = textureName;
            this.id = textureId;
            this.sprites = new List<SpriteAtlasProfilerInfoWrapper>();
        }

        public int SpriteCount
        {
            get
            {
                if (m_CachedSpriteCount < 0)
                    m_CachedSpriteCount = sprites.Count;
                return m_CachedSpriteCount;
            }
        }

        public float TotalRatio
        {
            get
            {
                if (m_CachedTotalRatio < 0f)
                {
                    float total = 0f;
                    int count = sprites.Count;
                    for (int i = 0; i < count; i++)
                    {
                        total += sprites[i].ratioSpriteInTexture;
                    }
                    m_CachedTotalRatio = total;
                }
                return m_CachedTotalRatio;
            }
        }

        public void InvalidateCache()
        {
            m_CachedSpriteCount = -1;
            m_CachedTotalRatio = -1f;
        }
    }

    // ============================================================================
    // ATLAS NODE - Optimized with cached calculations
    // ============================================================================

    internal class SpriteAtlasNode
    {
        public string atlasGuid;
        public string atlasName;
        public int id;
        public List<TextureNode> textures;

        private int m_CachedTextureCount = -1;
        private int m_CachedTotalSpriteCount = -1;

        public SpriteAtlasNode(string atlasGuid, string atlasName, int atlasId)
        {
            this.atlasGuid = atlasGuid;
            this.atlasName = atlasName;
            this.id = atlasId;
            this.textures = new List<TextureNode>();
        }

        public int TextureCount
        {
            get
            {
                if (m_CachedTextureCount < 0)
                    m_CachedTextureCount = textures.Count;
                return m_CachedTextureCount;
            }
        }

        public int TotalSpriteCount
        {
            get
            {
                if (m_CachedTotalSpriteCount < 0)
                {
                    int total = 0;
                    int count = textures.Count;
                    for (int i = 0; i < count; i++)
                    {
                        total += textures[i].SpriteCount;
                    }
                    m_CachedTotalSpriteCount = total;
                }
                return m_CachedTotalSpriteCount;
            }
        }

        public void InvalidateCache()
        {
            m_CachedTextureCount = -1;
            m_CachedTotalSpriteCount = -1;
        }
    }

    // ============================================================================
    // HELPER
    // ============================================================================

    internal class SpriteAtlasProfilerInfoHelper
    {
        public enum ColumnIndices
        {
            AssetName,
            _LastColumn
        }

        public static int GetLastColumnIndex()
        {
            return Unsupported.IsDeveloperMode() ?
                ((int)ColumnIndices._LastColumn - 1) :
                (int)ColumnIndices.AssetName;
        }
    }

    // ============================================================================
    // STATE
    // ============================================================================

    [Serializable]
    internal class SpriteAtlasProfilerInfoTreeViewState
    {
        public int selectedColumn = (int)SpriteAtlasProfilerInfoHelper.ColumnIndices.AssetName;
        public bool sortByDescendingOrder = true;
        public List<int> expandedIds = new List<int>();
    }

    // ============================================================================
    // COMPARER - Reusable to avoid allocations
    // ============================================================================

    internal class SpriteRatioComparer : IComparer<SpriteAtlasProfilerInfoWrapper>
    {
        public int Compare(SpriteAtlasProfilerInfoWrapper x, SpriteAtlasProfilerInfoWrapper y)
        {
            // Sort by ratio descending, then by name ascending
            int ratioCompare = y.ratioSpriteInTexture.CompareTo(x.ratioSpriteInTexture);
            if (ratioCompare != 0)
                return ratioCompare;
            return string.CompareOrdinal(x.spriteAssetName, y.spriteAssetName);
        }
    }

    internal class TextureNameComparer : IComparer<TextureNode>
    {
        public int Compare(TextureNode x, TextureNode y)
        {
            return string.CompareOrdinal(x.textureName, y.textureName);
        }
    }

    internal class AtlasNameComparer : IComparer<SpriteAtlasNode>
    {
        private bool m_Descending;

        public void SetDescending(bool descending)
        {
            m_Descending = descending;
        }

        public int Compare(SpriteAtlasNode x, SpriteAtlasNode y)
        {
            int result = string.CompareOrdinal(x.atlasName, y.atlasName);
            return m_Descending ? -result : result;
        }
    }

    // ============================================================================
    // BACKEND - Optimized
    // ============================================================================

    internal class SpriteAtlasProfilerInfoBackend
    {
        public List<SpriteAtlasProfilerInfoWrapper> rawItems { get; private set; }
        public List<SpriteAtlasNode> groupedAtlases { get; private set; }
        public SpriteAtlasProfilerInfoTreeViewState state { get; private set; }

        public event Action OnDataChanged;

        private int m_NextId = 10000;

        // Reusable comparers to avoid allocations
        private readonly SpriteRatioComparer m_SpriteComparer = new SpriteRatioComparer();
        private readonly TextureNameComparer m_TextureComparer = new TextureNameComparer();
        private readonly AtlasNameComparer m_AtlasComparer = new AtlasNameComparer();

        // Reusable buffers to reduce allocations
        private Dictionary<int, List<SpriteAtlasProfilerInfoWrapper>> m_AtlasGroupsBuffer;
        private Dictionary<string, List<SpriteAtlasProfilerInfoWrapper>> m_TextureGroupsBuffer;

        public SpriteAtlasProfilerInfoBackend(SpriteAtlasProfilerInfoTreeViewState state)
        {
            this.state = state;
            rawItems = new List<SpriteAtlasProfilerInfoWrapper>();
            groupedAtlases = new List<SpriteAtlasNode>();
            m_AtlasGroupsBuffer = new Dictionary<int, List<SpriteAtlasProfilerInfoWrapper>>(32);
            m_TextureGroupsBuffer = new Dictionary<string, List<SpriteAtlasProfilerInfoWrapper>>(16);
        }

        public void SetData(List<SpriteAtlasProfilerInfoWrapper> data)
        {
            rawItems = data ?? new List<SpriteAtlasProfilerInfoWrapper>();
            GroupByAtlas();
            SortData();
            if (OnDataChanged != null)
                OnDataChanged();
        }

        private void GroupByAtlas()
        {
            groupedAtlases.Clear();
            m_NextId = 10000;

            // Reuse dictionary instead of creating new one
            m_AtlasGroupsBuffer.Clear();

            int rawCount = rawItems.Count;
            for (int i = 0; i < rawCount; i++)
            {
                var item = rawItems[i];
                var key = item.assetGuid.GetHashCode();

                List<SpriteAtlasProfilerInfoWrapper> list;
                if (!m_AtlasGroupsBuffer.TryGetValue(key, out list))
                {
                    list = new List<SpriteAtlasProfilerInfoWrapper>(16);
                    m_AtlasGroupsBuffer[key] = list;
                }
                list.Add(item);
            }

            // Create atlas nodes
            foreach (var kvp in m_AtlasGroupsBuffer)
            {
                var atlasSprites = kvp.Value;
                int atlasSpritesCount = atlasSprites.Count;
                if (atlasSpritesCount == 0) continue;

                var firstSprite = atlasSprites[0];
                var atlasNode = new SpriteAtlasNode(
                    firstSprite.assetGuid,
                    firstSprite.assetName,
                    m_NextId++
                );

                // Group sprites by texture - reuse buffer
                m_TextureGroupsBuffer.Clear();

                for (int i = 0; i < atlasSpritesCount; i++)
                {
                    var sprite = atlasSprites[i];
                    string textureName = sprite.textureAssetName;

                    List<SpriteAtlasProfilerInfoWrapper> textureList;
                    if (!m_TextureGroupsBuffer.TryGetValue(textureName, out textureList))
                    {
                        textureList = new List<SpriteAtlasProfilerInfoWrapper>(8);
                        m_TextureGroupsBuffer[textureName] = textureList;
                    }
                    textureList.Add(sprite);
                }

                // Create texture nodes
                foreach (var textureKvp in m_TextureGroupsBuffer)
                {
                    var textureNode = new TextureNode(textureKvp.Key, m_NextId++);
                    var spriteList = textureKvp.Value;

                    // Add all at once instead of one by one
                    int spriteListCount = spriteList.Count;
                    if (textureNode.sprites.Capacity < spriteListCount)
                        textureNode.sprites.Capacity = spriteListCount;

                    for (int i = 0; i < spriteListCount; i++)
                    {
                        textureNode.sprites.Add(spriteList[i]);
                    }

                    atlasNode.textures.Add(textureNode);
                }

                groupedAtlases.Add(atlasNode);
            }
        }

        public void SetSortColumn(int columnIndex, bool descending)
        {
            state.selectedColumn = columnIndex;
            state.sortByDescendingOrder = descending;
            SortData();
            if (OnDataChanged != null)
                OnDataChanged();
        }

        private void SortData()
        {
            if (groupedAtlases == null || groupedAtlases.Count == 0) return;

            // Sort atlases using reusable comparer
            m_AtlasComparer.SetDescending(state.sortByDescendingOrder);
            groupedAtlases.Sort(m_AtlasComparer);

            // Sort child nodes
            SortChildNodes();
        }

        private void SortChildNodes()
        {
            int atlasCount = groupedAtlases.Count;
            for (int i = 0; i < atlasCount; i++)
            {
                var atlas = groupedAtlases[i];
                if (atlas.textures == null || atlas.textures.Count == 0)
                    continue;

                // Sort textures using reusable comparer
                atlas.textures.Sort(m_TextureComparer);

                // Sort sprites within each texture
                int textureCount = atlas.textures.Count;
                for (int j = 0; j < textureCount; j++)
                {
                    var texture = atlas.textures[j];
                    if (texture.sprites == null || texture.sprites.Count == 0)
                        continue;

                    // Sort sprites by ratio using reusable comparer
                    texture.sprites.Sort(m_SpriteComparer);
                }
            }
        }
    }

    // ============================================================================
    // VIEW - Heavily optimized
    // ============================================================================

    internal class SpriteAtlasProfilerInfoView
    {
        private MultiColumnTreeView m_TreeView;
        private SpriteAtlasProfilerInfoBackend m_Backend;
        private SpriteAtlasProfilerInfoTreeViewState m_State;
        private VisualElement m_Root;

        // Cache icons to avoid repeated EditorGUIUtility calls
        private Texture2D m_AtlasIcon;
        private Texture2D m_TextureIcon;
        private Texture2D m_SpriteIcon;

        // Pool for TreeViewItemData to reduce allocations
        private List<TreeViewItemData<SpriteAtlasProfilerInfoWrapper>> m_RootItemsPool;
        private List<TreeViewItemData<SpriteAtlasProfilerInfoWrapper>> m_TextureChildrenPool;
        private List<TreeViewItemData<SpriteAtlasProfilerInfoWrapper>> m_SpriteChildrenPool;

        public int GetNumItemsInData()
        {
            return m_Backend != null && m_Backend.rawItems != null ? m_Backend.rawItems.Count : 0;
        }

        public SpriteAtlasProfilerInfoView(SpriteAtlasProfilerInfoTreeViewState state)
        {
            m_State = state;

            // Pre-allocate pools
            m_RootItemsPool = new List<TreeViewItemData<SpriteAtlasProfilerInfoWrapper>>(32);
            m_TextureChildrenPool = new List<TreeViewItemData<SpriteAtlasProfilerInfoWrapper>>(16);
            m_SpriteChildrenPool = new List<TreeViewItemData<SpriteAtlasProfilerInfoWrapper>>(16);
        }

        public void Init(SpriteAtlasProfilerInfoBackend backend)
        {
            m_Backend = backend;
            m_Backend.OnDataChanged += RefreshTreeView;

            // Cache icons once
            CacheIcons();
        }

        private void CacheIcons()
        {
            var atlasContent = EditorGUIUtility.IconContent("SpriteAtlas Icon");
            m_AtlasIcon = atlasContent != null ? atlasContent.image as Texture2D : null;
            if (m_AtlasIcon == null)
            {
                atlasContent = EditorGUIUtility.IconContent("Prefab Icon");
                m_AtlasIcon = atlasContent != null ? atlasContent.image as Texture2D : null;
            }

            var textureContent = EditorGUIUtility.IconContent("Texture Icon");
            m_TextureIcon = textureContent != null ? textureContent.image as Texture2D : null;
            if (m_TextureIcon == null)
            {
                textureContent = EditorGUIUtility.IconContent("Texture2D Icon");
                m_TextureIcon = textureContent != null ? textureContent.image as Texture2D : null;
            }

            var spriteContent = EditorGUIUtility.IconContent("Sprite Icon");
            m_SpriteIcon = spriteContent != null ? spriteContent.image as Texture2D : null;
        }

        public VisualElement CreateGUI()
        {
            m_Root = new VisualElement();
            m_Root.style.flexGrow = 1;

            var columns = new Columns();

            columns.Add(new Column
            {
                name = "assetName",
                title = "SpriteAtlas / Texture / Sprite",
                width = 500,
                minWidth = 50,
                stretchable = true,
                sortable = true,
                makeCell = MakeCell,
                bindCell = BindCell
            });

            m_TreeView = new MultiColumnTreeView(columns);
            m_TreeView.style.flexGrow = 1;
            m_TreeView.fixedItemHeight = 20;
            m_TreeView.viewDataKey = "SpriteAtlasProfilerTreeView";

            m_TreeView.selectionChanged += OnSelectionChanged;
            m_TreeView.columnSortingChanged += OnSortingChanged;

            m_TreeView.sortColumnDescriptions.Add(new SortColumnDescription(
                m_State.selectedColumn,
                m_State.sortByDescendingOrder ? SortDirection.Descending : SortDirection.Ascending));

            m_Root.Add(m_TreeView);

            RefreshTreeView();

            return m_Root;
        }

        private VisualElement MakeCell()
        {
            // Create container for icon + label
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.flexGrow = 1;
            container.style.alignItems = Align.Center;

            // Icon
            var icon = new Image();
            icon.style.width = 16;
            icon.style.height = 16;
            icon.style.marginRight = 4;
            icon.style.flexShrink = 0;
            icon.name = "item-icon";

            // Label
            var label = new Label();
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            label.style.flexGrow = 1;
            label.name = "item-label";

            container.Add(icon);
            container.Add(label);

            return container;
        }

        private void BindCell(VisualElement element, int index)
        {
            var icon = element.Q<Image>("item-icon");
            var label = element.Q<Label>("item-label");

            if (icon == null || label == null) return;

            var item = m_TreeView.GetItemDataForIndex<SpriteAtlasProfilerInfoWrapper>(index);
            if (item == null)
            {
                label.text = string.Empty;
                icon.image = null;
                return;
            }

            // Use cached display text
            label.text = item.GetDisplayText();

            // Set icon and styles based on node type (branchless)
            byte nodeTypeValue = (byte)item.nodeType;

            // Select icon using cached references
            icon.image = nodeTypeValue == 0 ? m_AtlasIcon :
                        (nodeTypeValue == 1 ? m_TextureIcon : m_SpriteIcon);

            // Set font style
            bool isBold = nodeTypeValue <= 1; // Atlas or Texture
            label.style.unityFontStyleAndWeight = isBold ? FontStyle.Bold : FontStyle.Normal;

            // Set color
            if (nodeTypeValue == 0) // Atlas
            {
                label.style.color = new Color(0.8f, 0.9f, 1f);
            }
            else if (nodeTypeValue == 1) // Texture
            {
                label.style.color = new Color(0.9f, 0.9f, 0.7f);
            }
            else // Sprite
            {
                label.style.color = Color.white;
            }
        }

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            // Early exit for performance
            if (selectedItems == null) return;

            foreach (var item in selectedItems)
            {
                var selected = item as SpriteAtlasProfilerInfoWrapper;
                if (selected != null)
                {
                    // Log only in debug builds for performance
                    break;
                }
            }
        }

        private void OnSortingChanged()
        {
            var sortedColumns = m_TreeView.sortedColumns;
            if (sortedColumns == null) return;

            foreach (var sortColumn in sortedColumns)
            {
                m_Backend.SetSortColumn(
                    sortColumn.columnIndex,
                    sortColumn.direction == SortDirection.Descending);
                break; // Only handle first column
            }
        }

        private void RefreshTreeView()
        {
            if (m_TreeView == null || m_Backend == null || m_Backend.groupedAtlases == null)
                return;

            // Reuse lists to reduce allocations
            m_RootItemsPool.Clear();

            int atlasCount = m_Backend.groupedAtlases.Count;
            if (m_RootItemsPool.Capacity < atlasCount)
                m_RootItemsPool.Capacity = atlasCount;

            for (int i = 0; i < atlasCount; i++)
            {
                var atlasNode = m_Backend.groupedAtlases[i];

                // Create Atlas wrapper
                var atlasWrapper = new SpriteAtlasProfilerInfoWrapper(
                    atlasNode.TotalSpriteCount,
                    atlasNode.atlasName,
                    atlasNode.atlasGuid,
                    string.Empty,
                    string.Empty,
                    0f,
                    NodeType.Atlas
                );
                atlasWrapper.childCount = atlasNode.TextureCount;

                // Reuse texture children list
                m_TextureChildrenPool.Clear();
                int textureCount = atlasNode.textures.Count;
                if (m_TextureChildrenPool.Capacity < textureCount)
                    m_TextureChildrenPool.Capacity = textureCount;

                for (int j = 0; j < textureCount; j++)
                {
                    var textureNode = atlasNode.textures[j];

                    var textureWrapper = new SpriteAtlasProfilerInfoWrapper(
                        textureNode.id,
                        atlasNode.atlasName,
                        atlasNode.atlasGuid,
                        string.Empty,
                        textureNode.textureName,
                        textureNode.TotalRatio,
                        NodeType.Texture
                    );
                    textureWrapper.childCount = textureNode.SpriteCount;

                    // Reuse sprite children list
                    m_SpriteChildrenPool.Clear();
                    int spriteCount = textureNode.sprites.Count;
                    if (m_SpriteChildrenPool.Capacity < spriteCount)
                        m_SpriteChildrenPool.Capacity = spriteCount;

                    for (int k = 0; k < spriteCount; k++)
                    {
                        var sprite = textureNode.sprites[k];
                        m_SpriteChildrenPool.Add(new TreeViewItemData<SpriteAtlasProfilerInfoWrapper>(
                            sprite.id,
                            sprite
                        ));
                    }

                    m_TextureChildrenPool.Add(new TreeViewItemData<SpriteAtlasProfilerInfoWrapper>(
                        textureNode.id,
                        textureWrapper,
                        new List<TreeViewItemData<SpriteAtlasProfilerInfoWrapper>>(m_SpriteChildrenPool)
                    ));
                }

                m_RootItemsPool.Add(new TreeViewItemData<SpriteAtlasProfilerInfoWrapper>(
                    atlasNode.id,
                    atlasWrapper,
                    new List<TreeViewItemData<SpriteAtlasProfilerInfoWrapper>>(m_TextureChildrenPool)
                ));
            }

            m_TreeView.SetRootItems(m_RootItemsPool);
            m_TreeView.Rebuild();
        }
    }
}
