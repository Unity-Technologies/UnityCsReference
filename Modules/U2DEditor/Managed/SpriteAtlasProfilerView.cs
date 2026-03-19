// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Properties;

namespace UnityEditor.U2D.Profiling
{
    internal class SpriteNode
    {
        public int id;
        public string assetName;
        public string assetGuid;
        public string spriteAssetName;
        public string textureAssetName;
        public float ratioSpriteInTexture;

        public SpriteNode(int entityId, string assetName, string assetGuid,
            string spriteAssetName, string textureAssetName, float ratioSpriteInTexture)
        {
            this.id = entityId;
            this.assetName = assetName;
            this.assetGuid = assetGuid;
            this.spriteAssetName = spriteAssetName;
            this.textureAssetName = textureAssetName;
            this.ratioSpriteInTexture = ratioSpriteInTexture;
        }
    }

    internal class TextureNode
    {
        public string textureName;
        public int id;
        public List<SpriteNode> sprites;

        private int m_CachedSpriteCount = -1;

        public TextureNode(string textureName, int textureId)
        {
            this.textureName = textureName;
            this.id = textureId;
            this.sprites = new List<SpriteNode>();
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
    }

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

    [Serializable]
    internal class SpriteAtlasProfilerInfoTreeViewState
    {
        public int selectedColumn = (int)SpriteAtlasProfilerInfoHelper.ColumnIndices.AssetName;
        public bool sortByDescendingOrder = true;
        public List<int> expandedIds = new List<int>();
    }

    internal class SpriteAtlasProfilerInfoBackend
    {
        public List<SpriteNode> rawItems { get; private set; }
        public List<SpriteAtlasNode> groupedAtlases { get; private set; }
        public SpriteAtlasProfilerInfoTreeViewState state { get; private set; }

        public event Action OnDataChanged;

        private int m_NextId = 10000;

        // Reusable buffers to reduce allocations
        private Dictionary<int, List<SpriteNode>> m_AtlasGroupsBuffer;
        private Dictionary<string, List<SpriteNode>> m_TextureGroupsBuffer;

        public SpriteAtlasProfilerInfoBackend(SpriteAtlasProfilerInfoTreeViewState state)
        {
            this.state = state;
            rawItems = new List<SpriteNode>();
            groupedAtlases = new List<SpriteAtlasNode>();
            m_AtlasGroupsBuffer = new Dictionary<int, List<SpriteNode>>(32);
            m_TextureGroupsBuffer = new Dictionary<string, List<SpriteNode>>(16);
        }

        public void SetData(List<SpriteNode> data)
        {
            rawItems = data ?? new List<SpriteNode>();
            GroupByAtlas();
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

                List<SpriteNode> list;
                if (!m_AtlasGroupsBuffer.TryGetValue(key, out list))
                {
                    list = new List<SpriteNode>(16);
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

                    List<SpriteNode> textureList;
                    if (!m_TextureGroupsBuffer.TryGetValue(textureName, out textureList))
                    {
                        textureList = new List<SpriteNode>(8);
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
    }

    internal partial class SpriteAtlasProfilerInfoView
    {

        const string k_Uxml = "U2DEditor/SpriteAtlasProfiler/SpriteAtlasProfilerView/SpriteAtlasProfilerView.uxml";
        private SpriteAtlasProfilerInfoBackend m_Backend;
        private SpriteAtlasProfilerInfoTreeViewState m_State;
        private VisualElement m_Root;
        Label m_SelectFrameHintElement;
        MultiColumnTreeView m_Table;
        List<TreeViewItemData<SpriteAtlasProfilerCellData>> m_Data = new();

        public SpriteAtlasProfilerInfoView(SpriteAtlasProfilerInfoTreeViewState state)
        {
            m_State = state;
        }

        public VisualElement CreateGUI()
        {
            m_Root = (EditorGUIUtility.Load(k_Uxml) as VisualTreeAsset).Instantiate();
            m_SelectFrameHintElement = m_Root.Q<Label>("SelectFrameHint");
            m_SelectFrameHintElement.style.display = DisplayStyle.None;
            m_Table = m_Root.Q<MultiColumnTreeView>("Table");
            if(EditorGUIUtility.isProSkin)
                m_Table.AddToClassList("dark");
            else
                m_Table.AddToClassList("light");

            SetupTable();
            return m_Root;
        }

        public void Init(SpriteAtlasProfilerInfoBackend backend)
        {
            if(m_Backend != null)
            {
                m_Backend.OnDataChanged -= RefreshTreeView;
            }
            m_Backend = backend;
            if(m_Backend != null)
                m_Backend.OnDataChanged += RefreshTreeView;
            RefreshTreeView();
        }


        private void RefreshTreeView()
        {
            if (m_Backend == null || m_Backend.groupedAtlases == null)
            {
                ShowTable(false, "Start profiling or select a profiler frame to see usage data.");
                return;
            }

            UpdateTableView(false, false);
        }

        void SetupTable()
        {
            m_Table.sortingMode = ColumnSortingMode.Custom;
            m_Table.columnSortingChanged += OnColumnSortingChanged;
            for(int i = 0; i < m_Table.columns.Count; ++i)
            {
                var column = m_Table.columns[i];

                if (column.name == "Name")
                {
                    column.bindCell = (element, i) => // item is template
                    {
                        var label = element.Q<Label>();
                        var itemData = m_Table.GetItemDataForIndex<SpriteAtlasProfilerCellData>(i);
                        BindLabelToDataSource(label, column.bindingPath, itemData);
                        SetNameColumnCellIcon(element, itemData);
                    };
                }
                else
                {
                    column.bindCell = (element, i) => // item is template
                    {
                        var itemData = m_Table.GetItemDataForIndex<SpriteAtlasProfilerCellData>(i);
                        var label = element.Q<Label>();
                        BindLabelToDataSource(label, column.bindingPath, itemData);
                    };
                }

                column.unbindCell = (element, _) => // item is template
                {
                    var label = element.Q<Label>();
                    label.SetBinding("text", null);
                };

                column.makeCell = () =>
                {
                    return new CellLabelWithIcon();
                };
                column.comparison = (a,b) =>
                {
                    var aData = m_Table.GetItemDataForIndex<SpriteAtlasProfilerCellData>(a);
                    var bData = m_Table.GetItemDataForIndex<SpriteAtlasProfilerCellData>(b);
                    return SpriteAtlasProfilerCellData.Compare(aData, bData, column.bindingPath);
                };
            }
        }

        void BindLabelToDataSource(Label label, string path, SpriteAtlasProfilerCellData cellData)
        {
            label.SetBinding("text", new DataBinding
            {
                dataSourcePath = new PropertyPath(path),
                bindingMode = BindingMode.ToTarget,
                dataSource = cellData
            });
        }

        void SetNameColumnCellIcon(VisualElement ele, SpriteAtlasProfilerCellData data)
        {
            var icon = ele.Q("Icon");
            icon.RemoveFromClassList("texture-icon");
            icon.RemoveFromClassList("sprite-icon");
            icon.RemoveFromClassList("spriteatlas-icon");
            if (data.icon?.Length > 0)
            {
                icon.AddToClassList(data.icon);
                icon.style.display = DisplayStyle.Flex;
            }
            else
            {
                icon.style.display = DisplayStyle.None;
            }

        }

        void OnColumnSortingChanged()
        {
            UpdateTableView(true, true);
        }

        void CollectExpandedID(List<int> expandedIds, IEnumerable<TreeViewItemData<SpriteAtlasProfilerCellData>> data)
        {
            foreach(var d in data)
            {
                if(m_Table.IsExpanded(d.id))
                    expandedIds.Add(d.id);
                if(d.hasChildren)
                    CollectExpandedID(expandedIds, d.children);
            }
        }
        void UpdateTableView(bool keepExpand, bool sortUpdate = false)
        {
            BuildAltasInfoDataTree(sortUpdate);
            m_Table.Clear();
            List<int> expandedIds = new();
            if (keepExpand)
            {
                CollectExpandedID(expandedIds, m_Data);
            }
            m_Table.SetRootItems(m_Data);
            m_Table.Rebuild();
            foreach(var expand in expandedIds)
                m_Table.ExpandItem(expand);
            ShowTable(m_Data.Count > 0, "No profiling data to show.");
        }

        void ShowTable(bool show, string noShowReason)
        {
            m_Table.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            m_SelectFrameHintElement.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
            if (!string.IsNullOrEmpty(noShowReason) && !show)
                m_SelectFrameHintElement.text = noShowReason;
        }

        void BuildAltasInfoDataTree(bool sortUpdate)
        {
            m_Data.Clear();

            if(!sortUpdate || m_Backend != null)
            {
                for (int i = 0; i < m_Backend.groupedAtlases.Count; ++i)
                {
                    var atlasData = m_Backend.groupedAtlases[i];
                    var atlasCellData = new SpriteAtlasProfilerCellData(atlasData) { icon = "spriteatlas-icon" };
                    if (atlasData.atlasGuid == "?")
                    {
                        atlasCellData.icon = "sprite-icon";
                        List<TreeViewItemData<SpriteAtlasProfilerCellData>> textureChildren = new();
                        foreach (var texture in atlasData.textures)
                        {
                            foreach(var sprite in texture.sprites)
                            {
                                textureChildren.Add(new TreeViewItemData<SpriteAtlasProfilerCellData>(sprite.id, new SpriteAtlasProfilerCellData(sprite)
                                {
                                    icon = "sprite-icon"
                                }));
                            }
                            if(m_Table.sortedColumns != null)
                            {
                                textureChildren.Sort(SortData);
                            }
                        }

                        m_Data.Add(new TreeViewItemData<SpriteAtlasProfilerCellData>(atlasData.id, atlasCellData,
                            textureChildren));
                    }
                    else
                    {
                        m_Data.Add(new TreeViewItemData<SpriteAtlasProfilerCellData>(atlasData.id, atlasCellData,
                            BuildSpriteInfoDataTree(atlasData)));
                    }
                }
            }
            if(m_Table.sortedColumns != null)
            {
                m_Data.Sort(SortData);
            }
        }

        int SortData(TreeViewItemData<SpriteAtlasProfilerCellData> a, TreeViewItemData<SpriteAtlasProfilerCellData> b)
        {
            using (var enumerator = m_Table.sortedColumns.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    int result = SpriteAtlasProfilerCellData.Compare(a.data, b.data, enumerator.Current.column.bindingPath);
                    if (result != 0)
                        return result * (enumerator.Current.direction == SortDirection.Ascending ? 1 : -1);
                }
            }

            return SpriteAtlasProfilerCellData.Compare(a.data, b.data, null);
        }

        List<TreeViewItemData<SpriteAtlasProfilerCellData>> BuildSpriteInfoDataTree(SpriteAtlasNode atlasInfo)
        {
            List<TreeViewItemData<SpriteAtlasProfilerCellData>> data = new List<TreeViewItemData<SpriteAtlasProfilerCellData>>();
            foreach(var texture in atlasInfo.textures)
            {
                var textureCellData = new SpriteAtlasProfilerCellData(texture)
                {
                    icon = "texture-icon"
                };
                List<TreeViewItemData<SpriteAtlasProfilerCellData>> textureChildren = new();
                foreach(var sprite in texture.sprites)
                {
                    textureChildren.Add(new TreeViewItemData<SpriteAtlasProfilerCellData>(sprite.id, new SpriteAtlasProfilerCellData(sprite)
                    {
                        icon = "sprite-icon"
                    }));
                }
                if(m_Table.sortedColumns != null)
                {
                    textureChildren.Sort(SortData);
                }
                data.Add(new TreeViewItemData<SpriteAtlasProfilerCellData>(texture.id, textureCellData,
                    textureChildren));
            }

            if(m_Table.sortedColumns != null)
            {
                data.Sort(SortData);
            }

            return data;
        }
    }
}
