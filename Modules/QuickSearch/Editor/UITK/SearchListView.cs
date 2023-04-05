// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class SearchListViewItem : SearchViewItem
    {
        private readonly bool m_CompactMode;
        private readonly Label m_Description;
        private readonly Button m_ActionDropdown;

        public static readonly string moreActionsTooltip = L10n.Tr("Open actions menu");

        public static readonly string ussClassName = "search-list-view-item";
        public static readonly string thumbnailClassName = ussClassName.WithUssElement("thumbnail");
        public static readonly string labelClassName = ussClassName.WithUssElement("label");
        public static readonly string descriptionClassName = ussClassName.WithUssElement("description");
        public static readonly string labelsClassName = ussClassName.WithUssElement("labels");
        public static readonly string moreActionButtonClassName = ussClassName.WithUssElement("more-action-button");

        public SearchListViewItem(ISearchView viewModel)
            : base(string.Empty, viewModel, ussClassName)
        {
            style.flexDirection = FlexDirection.Row;

            m_CompactMode = viewModel.state.itemSize <= 1f;

            if (m_CompactMode)
                m_Thumbnail.AddToClassList(thumbnailClassName.WithUssModifier("compact"));
            else
                m_Thumbnail.AddToClassList(thumbnailClassName);

            m_Label.AddToClassList(labelClassName);

            m_Description = new Label() { name = "SearchListViewItemDescription" };
            m_Description.AddToClassList(descriptionClassName);

            Add(m_Thumbnail);

            var labels = new VisualElement() { name = "SearchListViewItemContent" };
            labels.AddToClassList(labelsClassName);

            if (!m_CompactMode)
                labels.Add(m_Label);
            labels.Add(m_Description);
            Add(labels);

            if (!viewModel.IsPicker())
            {
                m_ActionDropdown = CreateButton("SearchItemActionsDropdown", moreActionsTooltip, OnActionDropdownClicked, baseIconButtonClassName, moreActionButtonClassName);
                Add(m_ActionDropdown);
            }

            Add(m_FavoriteButton);
        }

        public override bool ShouldFetchPreview()
        {
            return !m_CompactMode && base.ShouldFetchPreview();
        }

        public override void Bind(in SearchItem item)
        {
            if (m_CompactMode)
                item.options |= SearchItemOptions.Compacted;

            base.Bind(item);

            m_Description.text = item.GetDescription(context, stripHTML: false);
            if (!m_CompactMode)
                m_Description.tooltip = item.GetValue()?.ToString() ?? string.Empty;
            else
                m_Description.tooltip = m_Label.text;

            m_BindedItem.options &= ~SearchItemOptions.Compacted;
        }
    }

    class SearchListView : SearchBaseCollectionView<ListView>
    {
        const float k_CompactItemHeight = 20f;

        public static readonly string ussClassName = "search-list-view";

        public SearchListView(ISearchView viewModel)
            : base("SearchListView", viewModel, ussClassName)
        {
            m_ListView = new ListView((IList)viewModel.results, GetItemHeight(), MakeItem, BindItem)
            {
                unbindItem = UnbindItem,
                destroyItem = DestroyItem,
                reorderable = false,
                selectionType = viewModel.multiselect ? SelectionType.Multiple : SelectionType.Single,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All
            };

            Add(m_ListView);
        }

        protected override float GetItemHeight()
        {
            if (viewState.itemSize <= (float)DisplayMode.Compact)
                return k_CompactItemHeight;
            return base.GetItemHeight();
        }

        private VisualElement MakeItem()
        {
            return new SearchListViewItem(m_ViewModel);
        }

        private void BindItem(VisualElement element, int index)
        {
            var e = (SearchListViewItem)element;
            if (index >= 0 && index < m_ViewModel.results.Count)
                e.Bind(m_ViewModel.results[index]);
        }

        private void UnbindItem(VisualElement element, int index)
        {
            var e = (SearchListViewItem)element;
            e.Unbind();
        }

        private void DestroyItem(VisualElement element)
        {
            var e = (SearchListViewItem)element;
            e.Destroy();
        }
    }
}
