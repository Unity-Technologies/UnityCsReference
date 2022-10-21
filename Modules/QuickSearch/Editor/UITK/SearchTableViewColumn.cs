// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Internal;

namespace UnityEditor.Search
{
    class SearchTableViewColumn : Column
    {
        public static readonly string ussClassName = MultiColumnCollectionHeader.ussClassName + "__column";
        public static readonly string defaultContentUssClassName = ussClassName + "__default-content";
        public static readonly string iconElementName = "unity-multi-column-header-column-icon";
        public static readonly string titleElementName = "unity-multi-column-header-column-title";
        public static readonly string titleUssClassName = ussClassName + "__title";
        public static readonly string contentUssClassName = ussClassName + "__content";
        public static readonly string hasTitleUssClassName = contentUssClassName + "--has-title";

        public readonly ISearchView searchView;
        public readonly SearchColumn searchColumn;
        private readonly ITableView tableView;

        private Label m_Title;
        private MultiColumnHeaderColumnIcon m_Icon;

        public SearchTableViewColumn(SearchColumn searchColumn, ISearchView searchView, ITableView tableView)
        {
            this.searchView = searchView;
            this.searchColumn = searchColumn;
            this.tableView = tableView;

            icon = Background.FromObject(searchColumn.content.image);
            name = searchColumn.path;
            title = searchColumn.content.text;
            width = searchColumn.width;
            sortable = searchColumn.options.HasAny(SearchColumnFlags.CanSort);
            optional = searchColumn.options.HasAny(SearchColumnFlags.CanHide);
            makeCell = MakeSearchColumnCell;
            bindCell = BindSearchColumnCell;
            unbindCell = UnbindSearchColumnCell;
            destroyCell = DestroyCell;
            makeHeader = MakeHeader;
            bindHeader = BindHeader;
            destroyHeader = DestroyHeader;
        }

        private VisualElement MakeHeader()
        {
            resized += OnColumnResized;

            var defContent = new VisualElement() { pickingMode = PickingMode.Ignore };
            defContent.AddToClassList(defaultContentUssClassName);

            m_Icon = new MultiColumnHeaderColumnIcon() { name = iconElementName, pickingMode = PickingMode.Ignore };
            m_Title = new Label() { name = titleElementName, pickingMode = PickingMode.Ignore };
            m_Title.AddToClassList(titleUssClassName);
            m_Title.style.flexGrow = 0f;

            defContent.Add(m_Icon);
            defContent.Add(m_Title);
            return defContent;
        }

        private void BindHeader(VisualElement ve)
        {
            m_Title.text = title;
            m_Title.tooltip = searchColumn.content.tooltip;
            ve.EnableInClassList(hasTitleUssClassName, !string.IsNullOrEmpty(title));

            if (m_Icon != null)
            {
                if (icon.texture != null)
                {
                    m_Icon.isImageInline = true;
                    m_Icon.image = icon.texture;
                }
                else
                {
                    if (m_Icon.isImageInline)
                        m_Icon.image = null;
                }
                m_Icon.UpdateClassList();
            }

            var options = searchColumn.options;
            if (options.HasAny(SearchColumnFlags.TextAlignmentCenter))
            {
                ve.style.justifyContent = Justify.Center;
                ve.style.unityTextAlign = TextAnchor.MiddleCenter;
                ve.parent.parent.style.flexDirection = FlexDirection.RowReverse;
            }
            else if (options.HasAny(SearchColumnFlags.TextAlignmentRight))
            {
                ve.style.justifyContent = Justify.FlexEnd;
                ve.style.unityTextAlign = TextAnchor.MiddleRight;
                ve.parent.parent.style.flexDirection = FlexDirection.Row;
            }
            else
            {
                ve.style.justifyContent = Justify.FlexStart;
                ve.style.unityTextAlign = TextAnchor.MiddleLeft;
                ve.parent.parent.style.flexDirection = FlexDirection.RowReverse;
            }
        }

        private void DestroyHeader(VisualElement obj)
        {
            resized -= OnColumnResized;
        }

        private void OnColumnResized(Column c)
        {
            searchColumn.width = c.width.value;
        }

        VisualElement MakeSearchColumnCell()
        {
            return new SearchTableViewCell(searchColumn, searchView, tableView);
        }

        void BindSearchColumnCell(VisualElement ve, int index)
        {
            if (index < 0 || index >= searchView.results.Count)
                return;

            var item = searchView.results[index];
            try
            { 
                ((SearchTableViewCell)ve).Bind(item);
            }
            catch (SearchColumnBindException ex)
            {
                Debug.LogException(ex);
            }
        }

        private void UnbindSearchColumnCell(VisualElement ve, int index)
        {
            ((SearchTableViewCell)ve).Unbind();
        }

        private void DestroyCell(VisualElement ve)
        {
        }
    }    
}
