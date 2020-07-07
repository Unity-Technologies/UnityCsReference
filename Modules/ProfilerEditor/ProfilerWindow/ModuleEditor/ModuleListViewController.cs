// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.Profiling.ModuleEditor
{
    class ModuleListViewController : ViewController
    {
        const string k_UssSelector_TitleLabel = "modules__title-label";
        const string k_UssSelector_ListView = "modules__list-view";
        const string k_UssSelector_CreateButton = "modules__create-button";

        // Data
        readonly List<ModuleData> m_Modules;

        // UI
        Label m_TitleLabel;
        ListView m_ListView;
        Button m_CreateButton;

        public ModuleListViewController(List<ModuleData> modules)
        {
            m_Modules = modules;
        }

        public event Action onCreateModule;
        public event Action<ModuleData, int> onModuleAtIndexSelected;

        public override void ConfigureView(VisualElement root)
        {
            base.ConfigureView(root);

            m_TitleLabel.text = LocalizationDatabase.GetLocalizedString("Profiler Modules");

            m_ListView.makeItem = MakeListViewItem;
            m_ListView.bindItem = BindListViewItem;
            m_ListView.selectionType = SelectionType.Single;
            m_ListView.reorderable = true;
            var dragAndDropController = m_ListView.GetDragAndDropController();
            if (dragAndDropController != null)
            {
                dragAndDropController.onItemMoved += OnListViewItemMoved;
            }
            m_ListView.onSelectionChange += OnListViewSelectionChange;
            m_ListView.itemsSource = m_Modules;

            m_CreateButton.text = LocalizationDatabase.GetLocalizedString("Add");
            m_CreateButton.clicked += AddModule;
        }

        public void SelectModuleAtIndex(int index)
        {
            m_ListView.SetSelection(index);
        }

        public void Refresh()
        {
            m_ListView.Refresh();
        }

        public void RefreshSelectedListItem()
        {
            var index = m_ListView.selectedIndex;
            foreach (var recycledItem in m_ListView.Pool)
            {
                if (recycledItem.index == index)
                {
                    m_ListView.bindItem(recycledItem.element, recycledItem.index);
                    break;
                }
            }
        }

        protected override void CollectViewElements(VisualElement root)
        {
            base.CollectViewElements(root);

            m_TitleLabel = root.Q<Label>(k_UssSelector_TitleLabel);
            m_ListView = root.Q<ListView>(k_UssSelector_ListView);
            m_CreateButton = root.Q<Button>(k_UssSelector_CreateButton);
        }

        VisualElement MakeListViewItem()
        {
            return new ModuleListViewItem();
        }

        void BindListViewItem(VisualElement element, int index)
        {
            var module = m_Modules[index];
            var moduleListViewItem = element as ModuleListViewItem;

            var titleLabel = moduleListViewItem.titleLabel;
            titleLabel.text = module.name;

            bool selectable = module.isEditable;
            moduleListViewItem.SetSelectable(selectable);
        }

        void OnListViewSelectionChange(IEnumerable<object> selectedItems)
        {
            var selectedIndex = m_ListView.selectedIndex;
            var selectedModule = m_Modules[selectedIndex];
            onModuleAtIndexSelected.Invoke(selectedModule, selectedIndex);
        }

        void OnListViewItemMoved(ItemMoveArgs<object> args)
        {
            var minimumIndex = Math.Min(args.previousIndex, args.newIndex);
            var maximumIndex = Math.Max(args.previousIndex, args.newIndex);
            for (int i = minimumIndex; i <= maximumIndex; i++)
            {
                var module = m_Modules[i];
                module.SetUpdatedEditedStateForOrderIndexChange();
            }
        }

        void AddModule()
        {
            onCreateModule.Invoke();
        }

        class ModuleListViewItem : VisualElement
        {
            const string k_UssClass = "module-list-view-item";
            const string k_UssClass_Label = "module-list-view-item__label";
            const string k_UssClass_Unselectable = "module-list-view-item__unselectable";

            public ModuleListViewItem()
            {
                AddToClassList(k_UssClass);

                var dragIndicator = new DragIndicator();
                Add(dragIndicator);

                titleLabel = new Label();
                titleLabel.AddToClassList(k_UssClass_Label);
                Add(titleLabel);
            }

            public Label titleLabel { get; }

            public void SetSelectable(bool selectable)
            {
                EnableInClassList(k_UssClass_Unselectable, !selectable);
            }
        }
    }
}
