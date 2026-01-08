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
    internal enum NodeType
    {
        Atlas,
        Texture,
        Sprite
    }

    internal class SpriteAtlasProfilerInfoWrapper
    {
        public int id;
        public string assetName;
        public string assetGuid;
        public string spriteAssetName;
        public string textureAssetName;
        public float spriteTextureSizeRatio;

        public NodeType nodeType;
        public int childCount;

        public SpriteAtlasProfilerInfoWrapper(int entityId, string assetName, string assetGuid,
            string spriteAssetName, string textureAssetName, float spriteTextureSizeRatio, NodeType nodeType = NodeType.Sprite)
        {
            this.id = entityId;
            this.assetName = assetName;
            this.assetGuid = assetGuid;
            this.spriteAssetName = spriteAssetName;
            this.textureAssetName = textureAssetName;
            this.spriteTextureSizeRatio = spriteTextureSizeRatio;
            this.nodeType = nodeType;
            this.childCount = 0;
        }
    }

    internal class TextureNode
    {
        public string textureName;
        public int id;
        public List<SpriteAtlasProfilerInfoWrapper> sprites;

        public TextureNode(string textureName, int textureId)
        {
            this.textureName = textureName;
            this.id = textureId;
            this.sprites = new List<SpriteAtlasProfilerInfoWrapper>();
        }

        public int SpriteCount
        {
            get { return sprites.Count; }
        }

        // Calculate total ratio coverage
        public float TotalRatio
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < sprites.Count; i++)
                {
                    total += sprites[i].spriteTextureSizeRatio;
                }
                return total;
            }
        }
    }

    internal class SpriteAtlasNode
    {
        public string atlasGuid;
        public string atlasName;
        public int id;
        public List<TextureNode> textures;

        public SpriteAtlasNode(string atlasGuid, string atlasName, int atlasId)
        {
            this.atlasGuid = atlasGuid;
            this.atlasName = atlasName;
            this.id = atlasId;
            this.textures = new List<TextureNode>();
        }

        public int TextureCount
        {
            get { return textures.Count; }
        }

        public int TotalSpriteCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < textures.Count; i++)
                {
                    total += textures[i].SpriteCount;
                }
                return total;
            }
        }
    }

    internal class SpriteAtlasProfilerInfoHelper
    {
        public enum ColumnIndices
        {
            AssetName,
            _LastColumn
        }

        public static string GetColumnString(SpriteAtlasProfilerInfoWrapper info, ColumnIndices index)
        {
            switch (index)
            {
                case ColumnIndices.AssetName:
                    if (info.nodeType == NodeType.Atlas)
                    {
                        return string.Format("{0} ({1} textures, {2} sprites)",
                            info.assetName, info.childCount, info.id); // Reusing fields temporarily
                    }
                    else if (info.nodeType == NodeType.Texture)
                    {
                        return string.Format("{0} ({1} sprites)", info.textureAssetName, info.childCount);
                    }
                    else
                    {
                        return string.Format("{0} ({1:P1})", info.spriteAssetName, info.spriteTextureSizeRatio);
                    }
            }
            return "Unknown";
        }

        public static int GetLastColumnIndex()
        {
            return Unsupported.IsDeveloperMode() ?
                ((int)ColumnIndices._LastColumn - 1) :
                (int)ColumnIndices.AssetName;
        }
    }

    [Serializable]
    internal class SpriteAtlasProfilerInfoTreeViewState
    {
        public int selectedColumn = (int)SpriteAtlasProfilerInfoHelper.ColumnIndices.AssetName;
        public bool sortByDescendingOrder = true;
        public List<int> expandedIds = new List<int>();
    }

    internal class SpriteAtlasProfilerInfoBackend
    {
        public List<SpriteAtlasProfilerInfoWrapper> rawItems { get; private set; }
        public List<SpriteAtlasNode> groupedAtlases { get; private set; }
        public SpriteAtlasProfilerInfoTreeViewState state { get; private set; }

        public event Action OnDataChanged;

        private int m_NextId = 10000;

        public SpriteAtlasProfilerInfoBackend(SpriteAtlasProfilerInfoTreeViewState state)
        {
            this.state = state;
            rawItems = new List<SpriteAtlasProfilerInfoWrapper>();
            groupedAtlases = new List<SpriteAtlasNode>();
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

            // Group by atlas using Dictionary
            var atlasGroups = new Dictionary<string, List<SpriteAtlasProfilerInfoWrapper>>();

            for (int i = 0; i < rawItems.Count; i++)
            {
                var item = rawItems[i];
                var key = item.assetGuid + "|" + item.assetName;

                if (!atlasGroups.ContainsKey(key))
                {
                    atlasGroups[key] = new List<SpriteAtlasProfilerInfoWrapper>();
                }
                atlasGroups[key].Add(item);
            }

            // Create atlas nodes and group sprites by texture
            foreach (var kvp in atlasGroups)
            {
                var atlasSprites = kvp.Value;
                if (atlasSprites.Count == 0) continue;

                var firstSprite = atlasSprites[0];
                var atlasNode = new SpriteAtlasNode(
                    firstSprite.assetGuid,
                    firstSprite.assetName,
                    m_NextId++
                );

                // Group sprites by texture within this atlas
                var textureGroups = new Dictionary<string, List<SpriteAtlasProfilerInfoWrapper>>();

                for (int i = 0; i < atlasSprites.Count; i++)
                {
                    var sprite = atlasSprites[i];
                    var textureName = sprite.textureAssetName;

                    if (!textureGroups.ContainsKey(textureName))
                    {
                        textureGroups[textureName] = new List<SpriteAtlasProfilerInfoWrapper>();
                    }
                    textureGroups[textureName].Add(sprite);
                }

                // Create texture nodes
                foreach (var textureKvp in textureGroups)
                {
                    var textureNode = new TextureNode(textureKvp.Key, m_NextId++);

                    for (int i = 0; i < textureKvp.Value.Count; i++)
                    {
                        textureNode.sprites.Add(textureKvp.Value[i]);
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

            var columnIndex = (SpriteAtlasProfilerInfoHelper.ColumnIndices)state.selectedColumn;
            bool descending = state.sortByDescendingOrder;

            // Sort atlases by name
            switch (columnIndex)
            {
                case SpriteAtlasProfilerInfoHelper.ColumnIndices.AssetName:
                    if (descending)
                    {
                        groupedAtlases.Sort((x, y) => y.atlasName.CompareTo(x.atlasName));
                    }
                    else
                    {
                        groupedAtlases.Sort((x, y) => x.atlasName.CompareTo(y.atlasName));
                    }
                    break;
            }

            // Sort textures and sprites within each atlas
            SortChildNodes(columnIndex, descending);
        }

        private void SortChildNodes(SpriteAtlasProfilerInfoHelper.ColumnIndices columnIndex, bool descending)
        {
            for (int i = 0; i < groupedAtlases.Count; i++)
            {
                var atlas = groupedAtlases[i];
                if (atlas.textures == null || atlas.textures.Count == 0)
                    continue;

                // Sort textures within atlas by name
                atlas.textures.Sort((x, y) => x.textureName.CompareTo(y.textureName));

                // Sort sprites within each texture by ratio (descending by default to show largest first)
                for (int j = 0; j < atlas.textures.Count; j++)
                {
                    var texture = atlas.textures[j];
                    if (texture.sprites == null || texture.sprites.Count == 0)
                        continue;

                    // Sort by ratio first, then by name
                    texture.sprites.Sort((x, y) =>
                    {
                        int ratioCompare = y.spriteTextureSizeRatio.CompareTo(x.spriteTextureSizeRatio);
                        if (ratioCompare != 0)
                            return ratioCompare;
                        return x.spriteAssetName.CompareTo(y.spriteAssetName);
                    });
                }
            }
        }
    }

    // ============================================================================
    // VIEW - Updated with ratio display
    // ============================================================================

    internal class SpriteAtlasProfilerInfoView
    {
        private MultiColumnTreeView m_TreeView;
        private SpriteAtlasProfilerInfoBackend m_Backend;
        private SpriteAtlasProfilerInfoTreeViewState m_State;
        private VisualElement m_Root;
        static  Action<string> s_SelectionChangedCallback;
        internal static event Action<string> selectionChanged
        {
            add { s_SelectionChangedCallback += value; }
            remove { s_SelectionChangedCallback -= value; }
        }

        public int GetNumItemsInData()
        {
            return m_Backend != null && m_Backend.rawItems != null ? m_Backend.rawItems.Count : 0;
        }

        public SpriteAtlasProfilerInfoView(SpriteAtlasProfilerInfoTreeViewState state)
        {
            m_State = state;
        }

        public void Init(SpriteAtlasProfilerInfoBackend backend)
        {
            m_Backend = backend;
            m_Backend.OnDataChanged += RefreshTreeView;
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
                bindCell = (element, index) => BindCell(element, index, 0)
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

        private void BindCell(VisualElement element, int index, int columnIndex)
        {
            var container = element;
            var icon = container.Q<Image>("item-icon");
            var label = container.Q<Label>("item-label");

            if (icon == null || label == null) return;

            var item = m_TreeView.GetItemDataForIndex<SpriteAtlasProfilerInfoWrapper>(index);
            if (item == null)
            {
                label.text = "";
                icon.image = null;
                return;
            }

            // Reset styles
            label.style.unityFontStyleAndWeight = FontStyle.Normal;
            label.style.color = Color.white;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            switch (columnIndex)
            {
                case 0:
                    if (item.nodeType == NodeType.Atlas)
                    {
                        // Atlas node - show texture count and total sprite count
                        var isNonAtlased = item.assetName.Contains("?");
                        icon.image = GetIconForNodeType(NodeType.Atlas, isNonAtlased);
                        label.text = string.Format("{0} ({1} textures, {2} sprites)",
                            item.assetName, item.childCount, item.id); // childCount=textureCount, id=totalSpriteCount
                    }
                    else if (item.nodeType == NodeType.Texture)
                    {
                        // Texture node - show sprite count and total ratio
                        icon.image = GetIconForNodeType(NodeType.Texture, false);
                        var ratioPercent = item.spriteTextureSizeRatio * 100f;
                        label.text = string.Format("{0} ({1} sprites, {2:F1}% coverage)",
                            item.textureAssetName, item.childCount, ratioPercent);
                    }
                    else
                    {
                        // Sprite node - show ratio as percentage
                        icon.image = GetIconForNodeType(NodeType.Sprite, false);
                        var ratioPercent = item.spriteTextureSizeRatio * 100f;
                        label.text = string.Format("{0} ({1:F2}%)", item.spriteAssetName, ratioPercent);
                    }
                    break;
            }
        }

        private Texture2D GetIconForNodeType(NodeType nodeType, bool useDefault)
        {
            if (useDefault)
            {
                return EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
            }

            switch (nodeType)
            {
                case NodeType.Atlas:
                    return EditorGUIUtility.IconContent("SpriteAtlas Icon").image as Texture2D;

                case NodeType.Texture:
                    return EditorGUIUtility.IconContent("Texture Icon").image as Texture2D;

                case NodeType.Sprite:
                    return EditorGUIUtility.IconContent("Sprite Icon").image as Texture2D;

                default:
                    return null;
            }
        }

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            SpriteAtlasProfilerInfoWrapper selected = null;

            foreach (var item in selectedItems)
            {
                selected = item as SpriteAtlasProfilerInfoWrapper;
                if (selected != null)
                {
                    break;
                }
            }

            if (selected != null)
            {
                s_SelectionChangedCallback?.Invoke(selected.assetGuid);
            }
        }

        private void OnSortingChanged()
        {
            var sortedColumns = m_TreeView.sortedColumns;
            if (sortedColumns != null)
            {
                var sortedColumnsList = new List<SortColumnDescription>(sortedColumns);
                if (sortedColumnsList.Count > 0)
                {
                    var sortColumn = sortedColumnsList[0];
                    m_Backend.SetSortColumn(
                        sortColumn.columnIndex,
                        sortColumn.direction == SortDirection.Descending);
                }
            }
        }

        private void RefreshTreeView()
        {
            if (m_TreeView == null || m_Backend == null || m_Backend.groupedAtlases == null)
                return;

            var rootItems = new List<TreeViewItemData<SpriteAtlasProfilerInfoWrapper>>();

            for (int i = 0; i < m_Backend.groupedAtlases.Count; i++)
            {
                var atlasNode = m_Backend.groupedAtlases[i];

                // Create Atlas node (Level 1)
                var atlasWrapper = new SpriteAtlasProfilerInfoWrapper(
                    atlasNode.TotalSpriteCount, // Storing total sprite count in id field
                    atlasNode.atlasName,
                    atlasNode.atlasGuid,
                    "",
                    "",
                    0f,
                    NodeType.Atlas
                );
                atlasWrapper.childCount = atlasNode.TextureCount;

                // Create Texture children (Level 2)
                var textureChildren = new List<TreeViewItemData<SpriteAtlasProfilerInfoWrapper>>();

                for (int j = 0; j < atlasNode.textures.Count; j++)
                {
                    var textureNode = atlasNode.textures[j];

                    // Create texture wrapper
                    var textureWrapper = new SpriteAtlasProfilerInfoWrapper(
                        textureNode.id,
                        atlasNode.atlasName,
                        atlasNode.atlasGuid,
                        "",
                        textureNode.textureName,
                        textureNode.TotalRatio, // Total coverage ratio
                        NodeType.Texture
                    );
                    textureWrapper.childCount = textureNode.SpriteCount;

                    // Create Sprite children (Level 3)
                    var spriteChildren = new List<TreeViewItemData<SpriteAtlasProfilerInfoWrapper>>();

                    for (int k = 0; k < textureNode.sprites.Count; k++)
                    {
                        var sprite = textureNode.sprites[k];
                        spriteChildren.Add(new TreeViewItemData<SpriteAtlasProfilerInfoWrapper>(
                            sprite.id,
                            sprite
                        ));
                    }

                    // Add texture with its sprite children
                    textureChildren.Add(new TreeViewItemData<SpriteAtlasProfilerInfoWrapper>(
                        textureNode.id,
                        textureWrapper,
                        spriteChildren
                    ));
                }

                // Add atlas with its texture children
                rootItems.Add(new TreeViewItemData<SpriteAtlasProfilerInfoWrapper>(
                    atlasNode.id,
                    atlasWrapper,
                    textureChildren
                ));
            }

            m_TreeView.SetRootItems(rootItems);
            m_TreeView.Rebuild();
        }
    }
}
