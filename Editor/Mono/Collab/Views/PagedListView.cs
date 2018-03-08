// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.Collaboration
{
    internal class PagerElement : VisualElement
    {
        public PageChangeAction OnPageChange;

        private readonly Label m_PageText;
        private readonly Button m_DownButton;
        private readonly Button m_UpButton;
        private int m_CurPage;
        private int m_TotalPages;

        public PagerElement(int pageCount, int startPage = 0)
        {
            this.style.flexDirection = FlexDirection.Row;
            this.style.alignSelf = Align.Center;
            m_CurPage = startPage;
            m_TotalPages = pageCount;

            Add(m_DownButton = new Button(OnPageDownClicked) {text = "\u25c5 Newer"});
            m_DownButton.AddToClassList("PagerDown");

            m_PageText = new Label();
            m_PageText.AddToClassList("PagerLabel");
            Add(m_PageText);

            Add(m_UpButton = new Button(OnPageUpClicked) {text = "Older \u25bb"});
            m_UpButton.AddToClassList("PagerUp");

            UpdateControls();
        }

        public int PageCount
        {
            get { return m_TotalPages; }
            set
            {
                if (m_TotalPages == value)
                    return;
                m_TotalPages = value;
                if (m_CurPage > m_TotalPages)
                {
                    m_CurPage = m_TotalPages;
                }
                UpdateControls();
            }
        }

        private void OnPageDownClicked()
        {
            if (m_CurPage <= 0)
                return;

            CollabAnalytics.SendUserAction(CollabAnalytics.historyCategoryString, "NewerPage");

            m_CurPage--;

            if (OnPageChange != null)
                OnPageChange(m_CurPage);

            UpdateControls();
        }

        private void OnPageUpClicked()
        {
            if (m_CurPage >= m_TotalPages)
                return;

            CollabAnalytics.SendUserAction(CollabAnalytics.historyCategoryString, "OlderPage");

            m_CurPage++;

            if (OnPageChange != null)
                OnPageChange(m_CurPage);

            UpdateControls();
        }

        private void UpdateControls()
        {
            m_PageText.text = (m_CurPage + 1) + " / " + m_TotalPages;
            m_DownButton.SetEnabled(m_CurPage > 0);
            m_UpButton.SetEnabled(m_CurPage < m_TotalPages - 1);
        }
    }

    internal enum PagerLocation
    {
        Top,
        Bottom,
        None
    }

    internal class PagedListView : VisualElement
    {
        public const int DefaultItemsPerPage = 10;

        private readonly VisualElement m_ItemContainer;
        private readonly PagerElement m_Pager;
        private int m_PageSize = DefaultItemsPerPage;
        private IEnumerable<VisualElement> m_Items;
        private int m_TotalItems;
        private PagerLocation m_PagerLoc = PagerLocation.Bottom;

        public PagerLocation PagerLoc
        {
            get { return m_PagerLoc; }
            set
            {
                if (value == m_PagerLoc)
                    return;

                m_PagerLoc = value;
                UpdatePager();
            }
        }

        public int pageSize
        {
            set { m_PageSize = value; }
        }

        public IEnumerable<VisualElement> items
        {
            set
            {
                m_Items = value;
                LayoutItems();
            }
        }

        public int totalItems
        {
            set
            {
                if (m_TotalItems == value)
                    return;

                m_TotalItems = value;
                UpdatePager();
            }
        }

        public PageChangeAction OnPageChange
        {
            set { m_Pager.OnPageChange = value; }
        }

        public PagedListView()
        {
            m_Pager = new PagerElement(0);

            m_ItemContainer = new VisualElement()
            {
                name = "PagerItems",
            };
            Add(m_ItemContainer);
            m_Items = new List<VisualElement>();
        }

        private void UpdatePager()
        {
            if (m_Pager.parent == this)
            {
                Remove(m_Pager);
            }

            switch (m_PagerLoc)
            {
                case PagerLocation.Top:
                    Insert(0, m_Pager);
                    break;
                case PagerLocation.Bottom:
                    Add(m_Pager);
                    break;
            }

            m_Pager.PageCount = pageCount;
        }

        private void LayoutItems()
        {
            m_ItemContainer.Clear();
            foreach (var item in m_Items)
            {
                m_ItemContainer.Add(item);
            }
        }

        int pageCount
        {
            get
            {
                var pages = m_TotalItems / m_PageSize;
                if (m_TotalItems % m_PageSize > 0)
                    pages++;

                return pages;
            }
        }
    }
}
