// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderVariantAnalyser
{
    [UxmlElement]
    partial class IndexPanel : PanelBase
    {
        [System.Serializable]
        public class Data : IPanelData
        {
            public bool isShader;
            public string filter = "";

            public Data(bool shaders)
            {
                isShader = shaders;
            }

            public string Caption => isShader ? "Shaders" : "Materials";

            public PanelType Panel => PanelType.Index;
        }

        Data m_Data;
        readonly IndexListView m_List;
        readonly ToolbarSearchField m_Search;

        IVisualElementScheduledItem m_PreviousSearch;
        string m_PendingSearchString;

        ByNameIndex m_FilteredReader;

        public event System.Action<bool, int> OnItemSelected;

        static VisualTreeAsset UxmlAsset => (VisualTreeAsset)EditorGUIUtility.Load("ShaderVariantAnalyser/IndexPanel.uxml");

        public IndexPanel()
        {
            UxmlAsset.CloneTree(this);

            m_Search = this.Q<ToolbarSearchField>("index-filter");
            m_List = this.Q<IndexListView>("index-list");

            m_Search.RegisterValueChangedCallback(evt => OnSearchFilterChanged(evt.newValue));
            m_List.OnItemSelected += (idx) => OnItemSelected?.Invoke(m_Data.isShader, m_FilteredReader.ToRawIndex(idx));
        }

        void OnSearchFilterChanged(string searchString)
        {
            if (m_PendingSearchString == searchString)
                return;

            m_PendingSearchString = searchString;

            if (m_PreviousSearch != null && m_PreviousSearch.isActive)
                m_PreviousSearch.Pause();

            m_PreviousSearch = schedule
                .Execute(() => PerformSearch(searchString))
                .StartingIn(100);
        }

        void PerformSearch(string searchString)
        {
            if (m_Data == null)
                return;

            m_Data.filter = searchString;
            m_FilteredReader.UpdateFilter(searchString);
            m_List.Filter = searchString;
            m_List.Refresh();
        }

        public Data BindData(DataReader reader, bool isShader)
        {
            var data = new Data(isShader);
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

            IndexReader idxReader = m_Data.isShader ? reader.Shaders : reader.Materials;
            m_FilteredReader = new ByNameIndex(idxReader, idxReader);

            m_Search.value = m_Data.filter;
            m_List.Filter = m_Data.filter;
            m_FilteredReader.UpdateFilter(m_Data.filter);
            m_List.BindData(m_FilteredReader, m_Data.isShader);

            return true;
        }

        public override void SetDisplay(bool visible)
        {
            base.SetDisplay(visible);

            m_Search.Focus();
        }

        public override void Unbind()
        {
            m_Data = null;
            m_FilteredReader = null;
            m_List.Unbind();
        }
    }

    [UxmlElement]
    partial class IndexListView : VisualElement
    {
        string m_SelectionColorBeginTag = EditorGUIUtility.isProSkin ? "<mark=#2C5D87>" : "<mark=#3A72B0>";
        const string k_SelectionColorEndTag = "</mark>";

        string HighlightText(string text)
        {
            if (string.IsNullOrEmpty(Filter))
                return text;

            var startIndex = text.IndexOf(Filter, 0, System.StringComparison.InvariantCultureIgnoreCase);
            while (startIndex != -1)
            {
                text = text.Insert(startIndex, m_SelectionColorBeginTag);
                var endIdx = startIndex + m_SelectionColorBeginTag.Length + Filter.Length;
                text = text.Insert(endIdx, k_SelectionColorEndTag);

                startIndex = text.IndexOf(Filter, endIdx + k_SelectionColorEndTag.Length, System.StringComparison.InvariantCultureIgnoreCase);
            }
            return text;
        }

        readonly MultiColumnListView m_List;

        IReadOnlyList<IndexEntryRef> m_Entries;
        public string Filter { get; set; }

        public event System.Action<int> OnItemSelected;

        public void BindData(IReadOnlyList<IndexEntryRef> entries, bool isShader)
        {
            m_Entries = entries;

            m_List.columns["variants"].visible = isShader;
            m_List.columns["materials"].visible = isShader;

            m_List.itemsSource = new ReadOnlyIList<IndexEntryRef>(entries);
        }

        public void Unbind()
        {
            m_Entries = null;
            m_List.itemsSource = null;
        }

        public void Refresh()
        {
            m_List.RefreshItems();
        }

        Texture m_ShaderIcon = AssetPreview.GetMiniTypeThumbnail(typeof(Shader));
        Texture m_ComputeIcon = AssetPreview.GetMiniTypeThumbnail(typeof(ComputeShader));
        Texture m_MaterialIcon = AssetPreview.GetMiniTypeThumbnail(typeof(Material));
        Texture m_CollectionIcon = AssetPreview.GetMiniTypeThumbnail(typeof(ShaderVariantCollection));

        Texture GetIconForType(EntryType type)
        {
            switch (type)
            {
                case EntryType.Shader:
                    return m_ShaderIcon;
                case EntryType.ComputeShader:
                    return m_ComputeIcon;
                case EntryType.Material:
                    return m_MaterialIcon;
                case EntryType.VariantCollection:
                    return m_CollectionIcon;
            }
            return null;
        }

        static VisualTreeAsset UxmlAsset => (VisualTreeAsset)EditorGUIUtility.Load("ShaderVariantAnalyser/IndexListView.uxml");

        public static Label NumericCell()
        {
            var l = new Label();
            l.AddToClassList("numeric-cell");
            return l;
        }

        public IndexListView()
        {
            UxmlAsset.CloneTree(this);

            m_List = this.Q<MultiColumnListView>("index-list-view-list");

            m_List.columns["name"].makeCell = () => new LabelWithIcon();
            m_List.columns["name"].bindCell = (v, i) =>
            {
                var l = v as LabelWithIcon;
                var item = m_Entries[i];
                l.Text = HighlightText(item.Name);
                l.Icon = GetIconForType(item.Type);
            };
            m_List.columns["variants"].makeCell = NumericCell;
            m_List.columns["variants"].bindCell = (v, i) => (v as Label).text = m_Entries[i].VariantCount.ToString();
            m_List.columns["materials"].makeCell = NumericCell;
            m_List.columns["materials"].bindCell = (v, i) => (v as Label).text = m_Entries[i].MaterialCount.ToString();

            m_List.itemsChosen += selection =>
            {
                if (m_List.selectedIndex < 0)
                    return;

                OnItemSelected?.Invoke(m_List.selectedIndex);
            };
        }
    }

    [UxmlElement]
    partial class LabelWithIcon : VisualElement
    {
        readonly Label m_Label;
        readonly Image m_Icon;

        [UxmlAttribute]
        public string Text
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }

        [UxmlAttribute]
        public Texture Icon
        {
            get => m_Icon.image;
            set => m_Icon.image = value;
        }

        public LabelWithIcon()
        {
            AddToClassList("label-with-icon");
            m_Icon = new Image();
            m_Icon.AddToClassList("label-with-icon__icon");
            Add(m_Icon);
            m_Label = new Label();
            m_Label.AddToClassList("label-with-icon__label");
            Add(m_Label);
        }
    }
}
