// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class SearchListViewItem : SearchViewItem
    {
        private bool m_CompactMode;
        private readonly Label m_Description;
        private readonly SearchViewItemButtonWithContext m_ActionDropdown;

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

            m_Label.AddToClassList(labelClassName);

            m_Description = new Label() { name = "SearchListViewItemDescription" };
            m_Description.AddToClassList(descriptionClassName);

            Add(m_Thumbnail);

            var labels = new VisualElement() { name = "SearchListViewItemContent" };
            labels.AddToClassList(labelsClassName);
            labels.Add(m_Label);
            labels.Add(m_Description);
            Add(labels);

            if (!viewModel.IsPicker())
            {
                m_ActionDropdown = new SearchViewItemButtonWithContext(
                    moreActionButtonName,
                    string.Empty,
                    moreActionsTooltip,
                    OnActionDropdownClicked,
                    baseIconButtonClassName,
                    moreActionButtonClassName);
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
            m_CompactMode = SearchListView.IsCompactMode(m_ViewModel);
            m_Thumbnail.AddToClassList(m_CompactMode ? thumbnailClassName.WithUssModifier("compact") : thumbnailClassName);

            if (m_CompactMode)
                item.options |= SearchItemOptions.Compacted;

            m_Label.style.display = m_CompactMode ? DisplayStyle.None : DisplayStyle.Flex;

            base.Bind(item);

            m_Description.text = item.GetDescription(context, stripHTML: false);
            if (!m_CompactMode)
                m_Description.tooltip = item.GetValue()?.ToString() ?? string.Empty;
            else
                m_Description.tooltip = m_Label.text;

            m_BindedItem.options &= ~SearchItemOptions.Compacted;

            if (m_ActionDropdown != null)
                m_ActionDropdown.BoundItem = item;
        }

        public override void Unbind()
        {
            m_Thumbnail.RemoveFromClassList(thumbnailClassName);
            m_Thumbnail.RemoveFromClassList(thumbnailClassName.WithUssModifier("compact"));

            m_Label.style.display = DisplayStyle.Flex;
            m_Description.text = string.Empty;
            m_Description.tooltip = string.Empty;

            if (m_ActionDropdown != null)
                m_ActionDropdown.BoundItem = null;

            base.Unbind();
        }
    }

    class SearchListView : SearchBaseCollectionView<ListView>
    {
        const float k_CompactItemHeight = 20f;

        public static readonly string ussClassName = "search-list-view";

        internal static string resultViewId = "list";
        public override string ViewId => resultViewId;

        public static SearchListView Create(ISearchView viewModel)
        {
            return new SearchListView(viewModel);
        }

        public static Texture2D FetchIcon()
        {
            return EditorGUIUtility.LoadIconRequired("ListView");
        }

        public static SearchResultViewDescriptor GetDescriptor()
        {
            return new SearchResultViewDescriptor(resultViewId, Create, FetchIcon,
                (float)DisplayMode.Compact, (float)DisplayMode.List, (float)DisplayMode.List,
                description: "List View",
                buttonClassName: "search-statusbar__list-mode-button");
        }

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

        internal static bool IsCompactMode(ISearchView viewModel)
        {
            return viewModel.currentResultViewId == SearchListView.resultViewId && SearchUtils.GetDisplayModeFromItemSize(viewModel.state.itemIconSize) == DisplayMode.Compact;
        }

        protected override float GetItemHeight()
        {
            if (IsCompactMode(m_ViewModel))
                return k_CompactItemHeight;
            return base.GetItemHeight();
        }

        private VisualElement MakeItem()
        {
            return new SearchListViewItem(m_ViewModel);
        }

        private void BindItem(VisualElement element, int index)
        {
            if (index < 0 || index >= m_ViewModel.results.Count)
                return;

            var e = (SearchListViewItem)element;
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
