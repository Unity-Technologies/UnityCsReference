// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderVariantAnalyser
{
    [UxmlElement]
    partial class DetailsPanel : PanelBase
    {
        [System.Serializable]
        public class Data : IPanelData
        {
            public bool isShader;
            public int idx;
            public int hierarchySelection = -1;
            public int variantSelection = -1;
            public List<FilterElement> filters = new();
            public string materialFilter = string.Empty;
            public string name = string.Empty;

            public Data(bool shader, int index)
            {
                isShader = shader;
                idx = index;
                filters.Add(FilterElement.kCommon);
            }

            public string Caption => name;

            public PanelType Panel => PanelType.Details;
        }

        Data m_Data;
        MultiColumnTreeView m_VariantsTree;
        readonly Label m_VariantsCaption;
        readonly MultiColumnListView m_VariantsList;
        readonly TwoPaneSplitView m_VerticalSplit;
        readonly Label m_MaterialsCaption;
        readonly IndexListView m_MaterialList;
        readonly KeywordFilterField m_KeywordFilter;
        readonly Toggle m_HideMatchesToggle;
        readonly ToolbarSearchField m_MaterialSearch;

        FilteredVariants m_Variants;
        IndexByVariantSelectionFilter m_MaterialsByVariant;
        ByNameIndex m_MaterialsByName;

        VisualTreeAsset UxmlAsset => (VisualTreeAsset)EditorGUIUtility.Load("ShaderVariantAnalyser/DetailsPanel.uxml");

        public DetailsPanel()
        {
            UxmlAsset.CloneTree(this);

            m_VariantsTree = this.Q<MultiColumnTreeView>("variants-tree");
            m_VariantsTree.columns["name"].makeCell = () => new Label();
            m_VariantsTree.columns["name"].bindCell = (v, i) => (v as Label).text = m_VariantsTree.GetItemDataForIndex<HierarchyNodeManaged>(i).value.value;
            m_VariantsTree.columns["variants"].makeCell = IndexListView.NumericCell;
            m_VariantsTree.columns["variants"].bindCell = (v, i) => (v as Label).text = m_VariantsTree.GetItemDataForIndex<HierarchyNodeManaged>(i).count.ToString();
            m_VariantsTree.columns["materials"].makeCell = IndexListView.NumericCell;
            m_VariantsTree.columns["materials"].bindCell = (v, i) => (v as Label).text = m_VariantsTree.GetItemDataForIndex<HierarchyNodeManaged>(i).materials.ToString();
            m_VariantsTree.selectedIndicesChanged += OnHierarchySelection;

            m_KeywordFilter = this.Q<KeywordFilterField>("keyword-filter");
            m_KeywordFilter.OnFiltersChanged += UpdateVariantFilters;

            m_HideMatchesToggle = this.Q<Toggle>("keywords-hide-matches-panel");
            m_HideMatchesToggle.RegisterValueChangedCallback(v => m_VariantsList.RefreshItems());

            m_VariantsList = this.Q<MultiColumnListView>("variant-list");
            m_VariantsList.columns["name"].makeCell = () => new KeywordList();
            m_VariantsList.columns["name"].bindCell = (v, i) => (v as KeywordList).Fill(m_Variants[i], m_Data.filters, m_Variants.CommonKeywords, !m_HideMatchesToggle.value);
            m_VariantsList.selectedIndicesChanged += OnVariantSelection;

            m_VerticalSplit = this.Q<TwoPaneSplitView>("vertical-split");
            m_VariantsCaption = this.Q<Label>("variant-caption");
            m_MaterialsCaption = this.Q<Label>("material-caption");

            m_MaterialSearch = this.Q<ToolbarSearchField>("index-filter");
            m_MaterialSearch.RegisterValueChangedCallback(evt => UpdateMaterialSearch());

            m_MaterialList = this.Q<IndexListView>("material-list");
        }

        void OnHierarchySelection(IEnumerable<int> indices)
        {
            m_Data.hierarchySelection = m_VariantsTree.GetIdForIndex(m_VariantsTree.selectedIndex);
            UpdateHierarchyFilters();
            m_VariantsList.selectedIndex = -1;
        }

        void OnVariantSelection(IEnumerable<int> indices)
        {
            if (m_VariantsList.selectedIndex >= 0)
                m_Data.variantSelection = m_Variants.ToRawIndex(m_VariantsList.selectedIndex);
            else
                m_Data.variantSelection = -1;
            UpdateMaterialFilter();
        }

        void UpdateHierarchyFilters()
        {
            RefreshVariants();
            UpdateMaterialFilter();
        }

        void UpdateVariantFilters()
        {
            m_Data.filters.Clear();
            m_Data.filters.AddRange(m_KeywordFilter.Filters);

            RefreshVariants();
        }

        void RefreshVariants()
        {
            List<FilterElement> filters = new(m_Data.filters);
            for (int id = m_Data.hierarchySelection; id >= 0; id = m_VariantsTree.viewController.GetParentId(id))
            {
                var data = m_VariantsTree.GetItemDataForId<HierarchyNodeManaged>(id);
                filters.Add(data.value);
            }

            m_Variants.UpdateVariantsFilter(filters);
            m_KeywordFilter.UpdateCommonKeywords(m_Variants.CommonKeywords);
            m_VariantsList.RefreshItems();
            m_VariantsCaption.text = $"Variants ({m_Variants.Count})";

            m_VariantsList.selectedIndex = m_Variants.FindRawIndex(m_Data.variantSelection);
        }

        void UpdateMaterialFilter()
        {
            if (!m_Data.isShader)
                return;

            int node = -1;
            int keyword = -2;

            for (int id = m_Data.hierarchySelection; id >= 0; id = m_VariantsTree.viewController.GetParentId(id))
            {
                var data = m_VariantsTree.GetItemDataForId<HierarchyNodeManaged>(id);
                if (data.value.type == FilterType.Snippet && node == -1)
                    node = data.value.hint;
                else if (data.value.type == FilterType.Keyword)
                    keyword = data.value.hint;
            }

            if (m_Data.variantSelection >= 0)
                m_MaterialsByVariant.FilterFromVariant(m_Data.variantSelection);
            else
                m_MaterialsByVariant.FilterFromHierarchy(node, keyword);

            RefreshMaterials();
        }

        void UpdateMaterialSearch()
        {
            if (!m_Data.isShader)
                return;

            m_Data.materialFilter = m_MaterialSearch.value;
            m_MaterialsByName.UpdateFilter(m_Data.materialFilter);

            // Don't update the number in the header
            m_MaterialList.Filter = m_Data.materialFilter;
            m_MaterialList.Refresh();
        }

        void RefreshMaterials()
        {
            m_MaterialsByName.Refresh();
            m_MaterialList.Refresh();
            m_MaterialsCaption.text = $"Materials ({m_MaterialsByVariant.Count})";
        }

        public Data BindData(DataReader reader, int index, bool isShader)
        {
            var data = new Data(isShader, index);
            if (BindData(reader, data))
                return data;

            return null;
        }

        public override bool BindData(DataReader reader, IPanelData data)
        {
            var casted = data as Data;
            if (casted == null)
                return false;
            return BindData(reader, casted);
        }

        public bool BindData(DataReader reader, Data data)
        {
            m_Data = data;

            IDetailsReader detailReader = null;
            if (m_Data.isShader)
                detailReader = reader.CreateShaderReader(m_Data.idx);
            else
                detailReader = reader.CreateMaterialReader(m_Data.idx);

            // Abort if we can't read the data
            if (detailReader == null)
                return false;

            m_Variants = detailReader.Variants;

            if (m_Data.isShader)
            {
                m_MaterialsByVariant = detailReader.GetReferencingMaterials(reader.Materials);
                m_MaterialsByName = new ByNameIndex(m_MaterialsByVariant, reader.Materials);

                m_MaterialList.BindData(m_MaterialsByName, false);
                m_VerticalSplit.UnCollapse();
            }
            else
            {
                m_VerticalSplit.CollapseChild(1);
            }

            m_Data.name = detailReader.Name;
            m_VariantsTree.columns["materials"].visible = m_Data.isShader;
            m_VariantsTree.SetRootItems(detailReader.GenerateTree());
            m_VariantsList.itemsSource = new ReadOnlyIList<VariantEntry>(m_Variants);

            ExpandTreeItem(m_VariantsTree.viewController.GetParentId(m_Data.hierarchySelection));
            m_VariantsTree.selectedIndex = m_VariantsTree.viewController.GetIndexForId(m_Data.hierarchySelection);
            m_VariantsList.selectedIndex = m_Variants.FindRawIndex(m_Data.variantSelection);

            m_KeywordFilter.SetAvailableFilters(m_Variants.AvailableFilters);
            m_KeywordFilter.SetFilters(m_Data.filters);

            RefreshVariants();
            if (m_Data.isShader)
            {
                RefreshMaterials();
                m_MaterialSearch.value = m_Data.materialFilter;
            }

            return true;
        }

        void ExpandTreeItem(int id)
        {
            if (id < 0)
                return;

            ExpandTreeItem(m_VariantsTree.viewController.GetParentId(id));
            m_VariantsTree.ExpandItem(id);
        }

        public override void Unbind()
        {
            m_Data = null;
            m_Variants = null;
            m_MaterialsByVariant = null;
            m_MaterialsByName = null;

            m_MaterialList.Unbind();
            m_VariantsList.itemsSource = null;
        }

        public override void SetDisplay(bool visible)
        {
            if (visible)
            {
                if (m_Data.isShader)
                    m_MaterialList.BindData(m_MaterialsByName, false);
                m_VariantsList.itemsSource = new ReadOnlyIList<VariantEntry>(m_Variants);
            }
            else
            {
                m_MaterialList.Unbind();
                m_VariantsList.itemsSource = null;
            }

            base.SetDisplay(visible);
        }
    }

    [UxmlElement]
    partial class KeywordList : VisualElement
    {
        Label GetOrCreateLabel()
        {
            if (m_ElementCount >= m_Container.childCount)
                m_Container.Add(new Label());

            var l = m_Container.hierarchy.ElementAt(m_ElementCount++) as Label;
            l.RemoveFromClassList(k_Matched);
            l.RemoveFromClassList(k_Unused);
            l.RemoveFromClassList(k_Count);
            l.RemoveFromClassList(k_ExtraKeyword);

            return l;
        }

        public void Fill(IEnumerable<FilterElement> items, ICollection<FilterElement> filters, ICollection<FilterElement> common, bool showFiltered)
        {
            m_ElementCount = 0;
            bool hasCommon = filters.Contains(FilterElement.kCommon);

            foreach (var item in items)
            {
                var highlight = filters.Contains(item);
                if (!highlight && hasCommon)
                    highlight = common.Contains(item);

                if (highlight && !showFiltered)
                    continue;

                var l = GetOrCreateLabel();

                if (highlight)
                    l.AddToClassList(k_Matched);


                l.text = item.value;
                l.tooltip = item.Tooltip;
            }

            if (m_ElementCount == 0)
            {
                var l = GetOrCreateLabel();
                l.text = "<all filtered>";
                l.tooltip = "All the keywords match the filters and matching keywords are hidden";
            }

            m_CountLabel = GetOrCreateLabel();
            m_CountLabel.AddToClassList(k_Count);

            for (int i = m_ElementCount; i < m_Container.childCount; ++i)
            {
                m_Container.hierarchy.ElementAt(i).AddToClassList(k_Unused);
            }

            Recalculate(layout);
        }

        readonly Toggle m_Toggle = new();
        readonly VisualElement m_Container = new();
        Label m_CountLabel = null;
        int m_ElementCount = 0;

        const string k_Matched = "keyword-set-container__matched";
        const string k_Expanded = "keyword-set-container__expanded";
        const string k_Collapsed = "keyword-set-container__collapsed";
        const string k_Fits = "keyword-set-container__fits";
        const string k_Unused = "keyword-set-container__unused";
        const string k_Count = "keyword-set__count";

        static VisualTreeAsset UxmlAsset => (VisualTreeAsset)EditorGUIUtility.Load("ShaderVariantAnalyser/KeywordList.uxml");

        public KeywordList()
        {
            UxmlAsset.CloneTree(this);

            m_Toggle = this.Q<Toggle>("foldout-toggle");
            m_Toggle.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                if (evt.newValue)
                {
                    AddToClassList(k_Expanded);
                    RemoveFromClassList(k_Collapsed);
                }
                else
                {
                    AddToClassList(k_Collapsed);
                    RemoveFromClassList(k_Expanded);
                }
            });

            m_Container = this.Q<VisualElement>("container");

            AddToClassList(k_Collapsed);
            RegisterCallback<GeometryChangedEvent>(evt => Recalculate(evt.newRect));
        }

        const string k_ExtraKeyword = "keyword-set-container__extra-keyword";

        static float GetRequiredWidth(Label referenceLabel, string text, float referenceHeight)
        {
            return referenceLabel.MeasureTextSize(text, 0, MeasureMode.Undefined, referenceHeight, MeasureMode.Undefined).x + referenceLabel.resolvedStyle.paddingLeft + referenceLabel.resolvedStyle.paddingRight;
        }

        void Recalculate(Rect newRect)
        {
            var numElements = m_ElementCount - 1;
            if (numElements <= 0)
                return;

            var firstChild = m_Container.hierarchy.ElementAt(0) as Label;

            var availableSize = m_Container.resolvedStyle.width - GetRequiredWidth(firstChild, "+XXXX", newRect.height); // Use a four digit placehoder to ensure enough space
            var childrenSize = GetRequiredWidth(firstChild, firstChild.text, newRect.height);

            // Skip the first element (it will never be hidden)
            int currentIdx = 1;
            for (; currentIdx < numElements; ++currentIdx)
            {
                var childLabel = m_Container.hierarchy.ElementAt(currentIdx) as Label;
                childrenSize += GetRequiredWidth(firstChild, childLabel.text, newRect.height);

                if (childrenSize > availableSize)
                    break;

                childLabel.RemoveFromClassList(k_ExtraKeyword);
            }

            if (currentIdx == numElements)
                AddToClassList(k_Fits);
            else
            {
                RemoveFromClassList(k_Fits);
                m_CountLabel.text = $"+{numElements - currentIdx}";
            }

            for (; currentIdx < numElements; ++currentIdx)
            {
                m_Container.hierarchy.ElementAt(currentIdx).AddToClassList(k_ExtraKeyword);
            }
        }
    }

    [UxmlElement]
    partial class KeywordFilterField : VisualElement
    {
        readonly VisualElement m_Container;
        readonly ButtonFilterBlock m_AddButton = new(EditorGUIUtility.IconContent("Toolbar Plus More").image);
        readonly CommonKeywordsFilterBlock m_CommonKeywordsBlock = new(0, string.Empty);
        readonly Button m_CancelButton;

        readonly HashSet<FilterElement> m_Filters = new();
        readonly List<FilterElement> m_AvailableFilters = new();

        public event System.Action OnFiltersChanged;
        public HashSet<FilterElement> Filters => m_Filters;

        static IEnumerable<string> AllNames(IEnumerable<FilterElement> keywords)
        {
            foreach (var k in keywords)
                yield return k.value;
        }

        static string GetCommonKeywordsTooltip(IEnumerable<FilterElement> keywords)
        {
            return string.Join(", ", AllNames(keywords));
        }

        public void UpdateCommonKeywords(ICollection<FilterElement> keywords)
        {
            m_CommonKeywordsBlock.UpdateContent(keywords.Count, GetCommonKeywordsTooltip(keywords));
        }

        void AddInternal(FilterElement filter)
        {
            m_Filters.Add(filter);

            KeywordFilterBlock block;
            if (filter.type == FilterType.CommonKeywords)
                block = m_CommonKeywordsBlock;
            else
            {
                block = new KeywordFilterBlock(filter);
                block.OnRemove += () => Remove(block);
            }

            m_Container.Insert(m_Container.childCount - 1, block);
        }

        public void Add(FilterElement filter)
        {
            AddInternal(filter);
            OnFiltersChanged?.Invoke();
        }

        public void Remove(KeywordFilterBlock block)
        {
            m_Filters.Remove(block.keyword);
            m_Container.Remove(block);
            OnFiltersChanged?.Invoke();
        }

        public void SetFilters(IEnumerable<FilterElement> filters)
        {
            m_Filters.Clear();
            while (m_Container.childCount > 1)
                m_Container.RemoveAt(0);

            foreach (var f in filters)
                AddInternal(f);

            OnFiltersChanged?.Invoke();
        }

        public void SetAvailableFilters(IEnumerable<FilterElement> filters)
        {
            m_AvailableFilters.Clear();
            m_AvailableFilters.AddRange(filters);
        }

        VisualTreeAsset UxmlAsset =>  (VisualTreeAsset)EditorGUIUtility.Load("ShaderVariantAnalyser/KeywordFilterField.uxml");

        public KeywordFilterField()
        {
            UxmlAsset.CloneTree(this);

            m_CommonKeywordsBlock.OnRemove += () => Remove(m_CommonKeywordsBlock);

            m_Container = this.Q<VisualElement>("container");
            m_CancelButton = this.Q<Button>("unity-cancel");
            m_CancelButton.clicked += () => SetFilters(System.Array.Empty<FilterElement>());

            m_AddButton.RegisterCallback<ClickEvent>(evt =>
            {
                var selector = new KeywordFilterSelector(m_AvailableFilters, m_Filters);
                selector.OnSelected += Add;
                selector.Show(m_AddButton.worldBound);
            });
            m_Container.Add(m_AddButton);
        }
    }

    abstract class KeywordFilterElement : VisualElement
    {
        public KeywordFilterElement()
        {
            AddToClassList("keyword-filter-block");
        }
    }

    class ButtonFilterBlock : KeywordFilterElement
    {
        readonly Image m_Image = new();

        public ButtonFilterBlock(Texture image)
        {
            AddToClassList("keyword-filter-block__button");

            var img = new Image
            {
                image = image
            };
            Add(img);
            tooltip = "Add filters";
        }
    }

    class KeywordFilterBlock : KeywordFilterElement
    {
        public FilterElement keyword { get; protected set; }

        public event Action OnRemove;

        public KeywordFilterBlock(FilterElement kw) : base()
        {
            keyword = kw;
            focusable = true;
            tooltip = kw.Tooltip;
            RegisterCallback<ClickEvent>(evt => Focus());

            Add(new Label(kw.value));

            var remove = new Button();
            remove.AddToClassList("remove-block-button");
            remove.AddToClassList("keyword-filter-block__button");
            remove.clicked += () => OnRemove();
            Add(remove);

            RegisterCallback<KeyUpEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
                    OnRemove.Invoke();
            });
        }
    }

    class CommonKeywordsFilterBlock : KeywordFilterBlock
    {
        readonly Label m_CountLabel = new();

        public CommonKeywordsFilterBlock(int count, string tooltip)
            : base(FilterElement.kCommon)
        {
            AddToClassList("keyword-filter-block__common");

            var separator = new VisualElement();
            separator.AddToClassList("keyword-filter-block__common__separator");
            Insert(childCount - 1, separator);

            m_CountLabel.text = count.ToString();
            Insert(childCount - 1, m_CountLabel);
        }

        public void UpdateContent(int count, string tooltip)
        {
            this.tooltip = tooltip;
            m_CountLabel.text = count.ToString();
        }
    }

    class KeywordFilterSelector : AdvancedDropdown
    {
        public event Action<FilterElement> OnSelected;

        readonly IReadOnlyList<FilterElement> m_Options;
        readonly AdvancedDropdownItem m_Root;

        public KeywordFilterSelector(IReadOnlyList<FilterElement> options, ICollection<FilterElement> active)
            : base(new AdvancedDropdownState())
        {
            m_Options = options;
            m_Root = new AdvancedDropdownItem("Filters");
            var kw = new AdvancedDropdownItem("Keywords");
            var api = new AdvancedDropdownItem("Graphics API");

            for (int i = 0; i < options.Count; i++)
            {
                var item = new AdvancedDropdownItem(options[i].value);
                item.enabled = !active.Contains(options[i]);
                switch (options[i].type)
                {
                    case FilterType.Api:
                        api.AddChild(item);
                        break;
                    case FilterType.Keyword:
                        kw.AddChild(item);
                        break;
                    case FilterType.CommonKeywords:
                        m_Root.AddChild(item);
                        break;
                }
                // AddChild modifies the id, so we need to set it afterwards
                item.id = i;
            }

            m_Root.AddChild(kw);
            kw.id = -1;
            m_Root.AddChild(api);
            api.id = -1;
        }

        protected override AdvancedDropdownItem BuildRoot() => m_Root;

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item.enabled && item.id >= 0)
                OnSelected?.Invoke(m_Options[item.id]);
        }
    }
}
