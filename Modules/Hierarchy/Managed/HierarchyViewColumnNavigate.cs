// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.HierarchyV2;

namespace Unity.Hierarchy
{
    [VisibleToOtherModules]
    sealed partial class HierarchyViewColumnNavigate : Column
    {
        internal const string k_HierarchyNavigateColumnName = "HierarchyViewColumn Navigate";
        static readonly UniqueStyleString k_HierarchyItemRightArrowButton = new("hierarchy-item__right-arrow-button");
        static readonly UniqueStyleString k_HierarchyColumnContextMenuClassName = new("hierarchy-column__context-menu");
        static readonly UniqueStyleString k_HierarchyColumnContextMenuIconClassName = new("hierarchy-column__context-menu-icon");

        static readonly Length k_ColumnWidth = new(18f, LengthUnit.Pixel);

        readonly HierarchyView m_View;
        readonly UnityEngine.Pool.ObjectPool<Button> m_ButtonPool;
        VisualElement m_KebabMenuButton;

        // Delegate that editor module can populate to provide editor-style menu
        [AutoStaticsCleanupOnCodeReload]
        public static Action<VisualElement, MultiColumnLayoutConfiguration> ShowColumnMenuCallback { get; set; }

        public HierarchyViewColumnNavigate(HierarchyView view)
        {
            m_View = view;
            m_ButtonPool = new UnityEngine.Pool.ObjectPool<Button>(
                CreateButton,
                actionOnRelease: button =>
                {
                    button.userData = null;
                    button.tooltip = null;
                    button.style.display = DisplayStyle.None;
                },
                defaultCapacity: 0,
                maxSize: 512);
            title = "";
            name = k_HierarchyNavigateColumnName;
            ApplyDefaultColumnProperties();

            makeCell = MakeCell;
            destroyCell = DestroyCell;
            makeHeader = MakeHeader;
        }

        internal void ApplyDefaultColumnProperties()
        {
            width = k_ColumnWidth;
            minWidth = k_ColumnWidth;
            maxWidth = k_ColumnWidth;

            visible = true;
            optional = false;
            resizable = false;
            stretchable = false;
            sortable = false;
        }

        VisualElement MakeHeader()
        {
            m_KebabMenuButton = new VisualElement { tooltip = "Column visibility" };
            m_KebabMenuButton.AddToClassList(k_HierarchyColumnContextMenuClassName);
            m_KebabMenuButton.RegisterCallback<PointerUpEvent>(evt => ShowColumnVisibilityMenu());

            var kebabMenuIcon = new VisualElement();
            kebabMenuIcon.AddToClassList(k_HierarchyColumnContextMenuIconClassName);
            kebabMenuIcon.pickingMode = PickingMode.Ignore;
            m_KebabMenuButton.Add(kebabMenuIcon);

            return m_KebabMenuButton;
        }

        void ShowColumnVisibilityMenu()
        {
            var collectionView = m_View.Q<CollectionView>();
            if (collectionView?.layoutConfiguration is not MultiColumnLayoutConfiguration layoutConfig)
                return;

            var columns = layoutConfig.columns;
            if (columns == null)
                return;

            // If editor module has registered a callback, use it (provides editor-style menu)
            if (ShowColumnMenuCallback != null)
            {
                ShowColumnMenuCallback(m_KebabMenuButton, layoutConfig);
                return;
            }

            // Fallback to GenericDropdownMenu
            ShowFallbackMenu(layoutConfig);
        }

        void ShowFallbackMenu(MultiColumnLayoutConfiguration layoutConfig)
        {
            var menu = new GenericDropdownMenu();
            var columns = layoutConfig.columns;

            // Add "Resize To Fit" option
            var canResizeToFit = true;
            foreach (var col in columns)
            {
                if (!col.visible)
                    continue;

                if (columns.stretchMode == Columns.StretchMode.GrowAndFill && col.stretchable)
                {
                    canResizeToFit = false;
                    break;
                }
            }

            if (canResizeToFit)
            {
                menu.AddItem("Resize To Fit", false, () =>
                {
                    layoutConfig.header?.ResizeToFit();
                });
            }
            else
            {
                menu.AddDisabledItem("Resize To Fit", false);
            }

            menu.AddSeparator("");

            // Add column visibility toggles
            foreach (var column in columns)
            {
                var title = column.title ?? column.name;
                if (string.IsNullOrEmpty(title))
                    continue;

                var isPrimaryColumn = !string.IsNullOrEmpty(column.name) &&
                                      !string.IsNullOrEmpty(columns.primaryColumnName) &&
                                      columns.primaryColumnName == column.name;
                var isDisabled = isPrimaryColumn || !column.optional;

                if (isDisabled)
                {
                    menu.AddDisabledItem(title, column.visible);
                }
                else
                {
                    var col = column;
                    menu.AddItem(title, column.visible, () => { col.visible = !col.visible; });
                }
            }

            menu.DropDown(m_KebabMenuButton.worldBound, m_KebabMenuButton, DropdownMenuSizeMode.Auto);
        }

        Button CreateButton()
        {
            var button = new Button();
            button.AddToClassList(k_HierarchyItemRightArrowButton);
            button.RemoveFromClassList(Button.ussClassNameUnique);
            button.style.display = DisplayStyle.None;
            return button;
        }

        VisualElement MakeCell()
        {
            var button = m_ButtonPool.Get();
            return button;
        }

        void DestroyCell(VisualElement element)
        {
            if (element is Button button)
            {
                m_ButtonPool.Release(button);
            }
        }
    }
}
