// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine.UIElements;

namespace UnityEditor.Profiling.ModuleEditor
{
    class ModuleDetailsViewController : ViewController
    {
        const string k_UssSelector_CurrentModuleDetailsTitleTextField = "current-module-details__title-text-field";
        const string k_UssSelector_CurrentModuleDetailsChartCountersTitleLabel = "current-module-details__chart-counters__title-label";
        const string k_UssSelector_CurrentModuleDetailsChartCountersDescriptionLabel = "current-module-details__chart-counters__description-label";
        const string k_UssSelector_CurrentModuleDetailsChartCountersCountLabel = "current-module-details__chart-counters__count-label";
        const string k_UssSelector_CurrentModuleDetailsChartCountersRemoveSelectedToolbarButton = "current-module-details__chart-counters__remove-selected-toolbar-button";
        const string k_UssSelector_CurrentModuleDetailsChartCountersSelectAllToolbarButton = "current-module-details__chart-counters__select-all-toolbar-button";
        const string k_UssSelector_CurrentModuleDetailsChartCountersDeselectAllToolbarButton = "current-module-details__chart-counters__deselect-all-toolbar-button";
        const string k_UssSelector_CurrentModuleDetailsChartCountersListView = "current-module-details__chart-counters__list-view";
        const string k_UssSelector_CurrentModuleDetailsDeleteButton = "current-module-details__delete-button";
        const string k_UssSelector_AllCountersTitleLabel = "all-counters__title-label";
        const string k_UssSelector_AllCountersDescriptionLabel = "all-counters__description-label";
        const string k_UssSelector_AllCountersTreeView = "all-counters__tree-view";
        const string k_UssSelector_AllCountersAddSelectedToolbarButton = "all-counters__add-selected-toolbar-button";
        const string k_UssSelector_ModuleDetailsConfirmButton = "module-details__confirm-button";
        const string k_UssSelector_ModuleDetailsNoModuleSelectedLabel = "module-details__no-module-selected-label";

        const string k_UssSelector_DragAndDropTargetHover = "drag-and-drop__drop-target--hover";

        const string k_AllCountersTreeViewDataKey = "all-counters__tree-view__data-key";
        const int k_MaximumTitleLength = 40;

        // Data
        ModuleData m_Module;
        List<ITreeViewItem> m_TreeDataItems;

        // UI
        // Any members of type Button below that are suffixed 'ToolbarButton' are defined as ToolbarButtons in the UXML and are therefore ToolbarButtons at runtime. They are declared as Buttons here because we cannot use ToolbarButton (UnityEditor.UIElements) from the Editor assembly without moving this code to its own assembly/module.
        TextField m_TitleTextField;
        Label m_ChartCountersTitleLabel;
        Label m_ChartCountersDescriptionLabel;
        Label m_ChartCountersCountLabel;
        Button m_ChartCountersRemoveSelectedToolbarButton;
        Button m_ChartCountersSelectAllToolbarButton;
        Button m_ChartCountersDeselectAllToolbarButton;
        ListView m_ChartCountersListView;
        Button m_DeleteModuleButton;
        Label m_AllCountersTitleLabel;
        Label m_AllCountersDescriptionLabel;
        TreeView m_AllCountersTreeView;
        Button m_AllCountersAddSelectedButton;
        Button m_ConfirmButton;
        Label m_NoModuleSelectedLabel;

        bool m_isConnectedToEditor;

        public event Action<ModuleData> onDeleteModule;
        public event Action onConfirmChanges;
        public event Action onModuleNameChanged;

        public ModuleDetailsViewController(bool isConnectedToEditor)
        {
            m_isConnectedToEditor = isConnectedToEditor;
        }

        public override void ConfigureView(VisualElement root)
        {
            base.ConfigureView(root);

            m_TitleTextField.RegisterValueChangedCallback(OnTitleChanged);

            m_ChartCountersTitleLabel.text = LocalizationDatabase.GetLocalizedString("Counters");
            m_ChartCountersDescriptionLabel.text = LocalizationDatabase.GetLocalizedString("Add counters to be displayed by the module.");
            m_ChartCountersRemoveSelectedToolbarButton.text = LocalizationDatabase.GetLocalizedString("Remove Selected");
            m_ChartCountersRemoveSelectedToolbarButton.clicked += RemoveSelectedCountersFromModule;
            m_ChartCountersSelectAllToolbarButton.text = LocalizationDatabase.GetLocalizedString("Select All");
            m_ChartCountersSelectAllToolbarButton.clicked += SelectAllChartCounters;
            m_ChartCountersDeselectAllToolbarButton.text = LocalizationDatabase.GetLocalizedString("Deselect All");
            m_ChartCountersDeselectAllToolbarButton.clicked += DeselectAllChartCounters;
            m_ChartCountersListView.makeItem = MakeListViewItem;
            m_ChartCountersListView.bindItem = BindListViewItem;
            m_ChartCountersListView.selectionType = SelectionType.Multiple;
            m_ChartCountersListView.reorderable = true;
            var dragAndDropController = m_ChartCountersListView.GetDragAndDropController();
            if (dragAndDropController != null)
            {
                dragAndDropController.onItemMoved += OnListViewItemMoved;
            }
            m_DeleteModuleButton.text = LocalizationDatabase.GetLocalizedString("Delete Module");
            m_DeleteModuleButton.clicked += DeleteModule;

            m_AllCountersTitleLabel.text = LocalizationDatabase.GetLocalizedString("Available Counters");
            m_AllCountersDescriptionLabel.text = LocalizationDatabase.GetLocalizedString("Select counters in the list below to add them to the selected module's counters. This list includes all built-in Unity counters, as well as any User-defined counters present upon load in the Profiler's data stream.");
            m_AllCountersTreeView.makeItem = MakeTreeViewItem;
            m_AllCountersTreeView.bindItem = BindTreeViewItem;
            m_AllCountersTreeView.viewDataKey = k_AllCountersTreeViewDataKey;
            m_AllCountersTreeView.selectionType = SelectionType.Multiple;
            m_AllCountersTreeView.onSelectionChange += OnTreeViewSelectionChanged;
            m_AllCountersTreeView.onItemsChosen += OnTreeViewSelectionChosen;
            m_AllCountersAddSelectedButton.text = LocalizationDatabase.GetLocalizedString("Add Selected");
            m_AllCountersAddSelectedButton.clicked += AddSelectedTreeViewCountersToModule;

            m_ConfirmButton.text = LocalizationDatabase.GetLocalizedString("Save Changes");
            m_ConfirmButton.clicked += ConfirmChanges;

            m_NoModuleSelectedLabel.text = LocalizationDatabase.GetLocalizedString("Select a custom module from the list or add a new custom module.");

            LoadAllCounters();
        }

        public void SetModule(ModuleData module)
        {
            m_Module = module;
            m_ChartCountersListView.ClearSelection();

            if (m_Module.isEditable)
            {
                m_TitleTextField.SetValueWithoutNotify(m_Module.localizedName);
                UpdateChartCountersCountLabel();
                m_ChartCountersListView.itemsSource = m_Module.chartCounters;
                m_ChartCountersListView.Refresh();
                m_AllCountersTreeView.Refresh();
                m_NoModuleSelectedLabel.visible = false;
            }
            else
            {
                m_NoModuleSelectedLabel.visible = true;
            }
        }

        public void SetNoModuleSelected()
        {
            m_NoModuleSelectedLabel.visible = true;
        }

        protected override void CollectViewElements(VisualElement root)
        {
            base.CollectViewElements(root);
            m_TitleTextField = root.Q<TextField>(k_UssSelector_CurrentModuleDetailsTitleTextField);
            m_ChartCountersTitleLabel = root.Q<Label>(k_UssSelector_CurrentModuleDetailsChartCountersTitleLabel);
            m_ChartCountersDescriptionLabel = root.Q<Label>(k_UssSelector_CurrentModuleDetailsChartCountersDescriptionLabel);
            m_ChartCountersCountLabel = root.Q<Label>(k_UssSelector_CurrentModuleDetailsChartCountersCountLabel);
            m_ChartCountersRemoveSelectedToolbarButton = root.Q<Button>(k_UssSelector_CurrentModuleDetailsChartCountersRemoveSelectedToolbarButton);
            m_ChartCountersSelectAllToolbarButton = root.Q<Button>(k_UssSelector_CurrentModuleDetailsChartCountersSelectAllToolbarButton);
            m_ChartCountersDeselectAllToolbarButton = root.Q<Button>(k_UssSelector_CurrentModuleDetailsChartCountersDeselectAllToolbarButton);
            m_ChartCountersListView = root.Q<ListView>(k_UssSelector_CurrentModuleDetailsChartCountersListView);
            m_DeleteModuleButton = root.Q<Button>(k_UssSelector_CurrentModuleDetailsDeleteButton);
            m_AllCountersTitleLabel = root.Q<Label>(k_UssSelector_AllCountersTitleLabel);
            m_AllCountersDescriptionLabel = root.Q<Label>(k_UssSelector_AllCountersDescriptionLabel);
            m_AllCountersTreeView = root.Q<TreeView>(k_UssSelector_AllCountersTreeView);
            m_AllCountersAddSelectedButton = root.Q<Button>(k_UssSelector_AllCountersAddSelectedToolbarButton);
            m_ConfirmButton = root.Q<Button>(k_UssSelector_ModuleDetailsConfirmButton);
            m_NoModuleSelectedLabel = root.Q<Label>(k_UssSelector_ModuleDetailsNoModuleSelectedLabel);
        }

        void LoadAllCounters()
        {
            using (ProfilerMarkers.k_LoadAllCounters.Auto())
            {
                ProfilerMarkers.k_FetchCounters.Begin();
                var counterCollector = new CounterCollector();
                SortedDictionary<string, List<string>> unityCounters;
                SortedDictionary<string, List<string>> userCounters;
                if (m_isConnectedToEditor)
                    counterCollector.LoadEditorCounters(out unityCounters, out userCounters);
                else
                    counterCollector.LoadCounters(out unityCounters, out userCounters);
                ProfilerMarkers.k_FetchCounters.End();

                // Format counter data for display in tree view.
                ProfilerMarkers.k_FormatCountersForDisplay.Begin();
                m_TreeDataItems = new List<ITreeViewItem>();
                TreeViewItemData.ResetNextId();
                AddCounterGroupToTreeDataItems(unityCounters, "Unity", m_TreeDataItems);
                AddCounterGroupToTreeDataItems(userCounters, "User", m_TreeDataItems);
                ProfilerMarkers.k_FormatCountersForDisplay.End();

                // Update tree view UI.
                ProfilerMarkers.k_RebuildCountersUI.Begin();
                m_AllCountersTreeView.rootItems = m_TreeDataItems;
                m_AllCountersTreeView.Refresh();
                ProfilerMarkers.k_RebuildCountersUI.End();
            }
        }

        void AddCounterGroupToTreeDataItems(SortedDictionary<string, List<string>> counterDictionary, string groupName, List<ITreeViewItem> treeDataItems)
        {
            if (counterDictionary.Count == 0)
            {
                return;
            }

            var group = new GroupTreeViewItemData(groupName);
            foreach (var categoryName in counterDictionary.Keys)
            {
                var category = new CategoryTreeViewItemData(categoryName);

                var counters = new List<ITreeViewItem>();
                foreach (var counter in counterDictionary[categoryName])
                {
                    counters.Add(new CounterTreeViewItemData(counter));
                }

                category.AddChildren(counters);
                group.AddChild(category);
            }

            treeDataItems.Add(group);
        }

        VisualElement MakeListViewItem()
        {
            var listViewItem = new CounterListViewItem();
            return listViewItem;
        }

        void BindListViewItem(VisualElement element, int index)
        {
            var counter = m_Module.chartCounters[index];
            var counterListViewItem = element as CounterListViewItem;
            var titleLabel = counterListViewItem.titleLabel;
            titleLabel.text = counter.m_Name;
        }

        VisualElement MakeTreeViewItem()
        {
            var treeViewItem = new CounterTreeViewItem();
            return treeViewItem;
        }

        void BindTreeViewItem(VisualElement element, ITreeViewItem item)
        {
            var itemData = item as TreeViewItemData;
            var treeViewItem = element as CounterTreeViewItem;
            var titleLabel = treeViewItem.titleLabel;
            titleLabel.text = itemData.data;

            bool moduleHasCounter = false;
            if ((m_Module != null) && (itemData is CounterTreeViewItemData counterItemData))
            {
                var category = counterItemData.category;
                var counter = counterItemData.data;
                moduleHasCounter = m_Module.ContainsChartCounter(counter, category);
            }

            treeViewItem.SetSelectable(!moduleHasCounter);
        }

        void OnTitleChanged(ChangeEvent<string> evt)
        {
            var newValue = evt.newValue;
            if (newValue.Length > k_MaximumTitleLength)
            {
                m_TitleTextField.SetValueWithoutNotify(evt.previousValue);
                return;
            }

            m_Module.SetName(evt.newValue);
            onModuleNameChanged?.Invoke();
        }

        void OnTreeViewSelectionChanged(IEnumerable<ITreeViewItem> selectedItems)
        {
            var selectedCounterItems = new List<int>();
            foreach (var selectedItem in selectedItems)
            {
                // Only counters have no children.
                if (!selectedItem.hasChildren)
                {
                    selectedCounterItems.Add(selectedItem.id);
                }
                else
                {
                    var id = selectedItem.id;
                    if (m_AllCountersTreeView.IsExpanded(id))
                    {
                        m_AllCountersTreeView.CollapseItem(id);
                    }
                    else
                    {
                        m_AllCountersTreeView.ExpandItem(id);
                    }
                }
            }
            m_AllCountersTreeView.SetSelectionWithoutNotify(selectedCounterItems);
        }

        void OnTreeViewSelectionChosen(IEnumerable<ITreeViewItem> selectedItems)
        {
            AddSelectedTreeViewCountersToModule();
        }

        void AddSelectedTreeViewCountersToModule()
        {
            var selectedItems = m_AllCountersTreeView.selectedItems;
            foreach (var selectedItem in selectedItems)
            {
                var counterTreeViewItemData = selectedItem as CounterTreeViewItemData;
                if (counterTreeViewItemData != null)
                {
                    var counter = new ProfilerCounterData()
                    {
                        m_Category = counterTreeViewItemData.category,
                        m_Name = counterTreeViewItemData.data,
                    };
                    AddCounterToModuleWithoutUIRefresh(counter);
                }
            }

            m_AllCountersTreeView.ClearSelection();
            m_ChartCountersListView.Refresh();
            m_AllCountersTreeView.Refresh();
            UpdateChartCountersCountLabel();
        }

        void AddCounterToModuleWithoutUIRefresh(ProfilerCounterData counter)
        {
            if (m_Module.hasMaximumChartCounters)
            {
                return;
            }

            if (m_Module.ContainsChartCounter(counter))
            {
                return;
            }

            m_Module.AddChartCounter(counter);
        }

        void RemoveSelectedCountersFromModule()
        {
            var selectedIndices = m_ChartCountersListView.selectedIndices.ToList();
            selectedIndices.Sort((a, b) => b.CompareTo(a)); // Ensure indices are in reverse order as we are deleting.
            for (int i = 0; i < selectedIndices.Count; i++)
            {
                var selectedIndex = selectedIndices[i];
                m_Module.RemoveChartCounterAtIndex(selectedIndex);
            }

            m_ChartCountersListView.ClearSelection();
            m_ChartCountersListView.Refresh();
            m_AllCountersTreeView.Refresh();
            UpdateChartCountersCountLabel();
        }

        void ConfirmChanges()
        {
            onConfirmChanges?.Invoke();
        }

        void DeleteModule()
        {
            var title = LocalizationDatabase.GetLocalizedString("Delete Module");
            var localizedMessageFormat = LocalizationDatabase.GetLocalizedString("Are you sure you want to delete the module '{0}'?");
            var message = string.Format(localizedMessageFormat, m_Module.localizedName);
            var delete = LocalizationDatabase.GetLocalizedString("Delete");
            var cancel = LocalizationDatabase.GetLocalizedString("Cancel");
            if (EditorUtility.DisplayDialog(title, message, delete, cancel))
            {
                onDeleteModule.Invoke(m_Module);
            }
        }

        void SelectAllChartCounters()
        {
            m_ChartCountersListView.SelectAll();
        }

        void DeselectAllChartCounters()
        {
            m_ChartCountersListView.ClearSelection();
        }

        void UpdateChartCountersCountLabel()
        {
            m_ChartCountersCountLabel.text = $"{m_Module.chartCounters.Count}/{ModuleData.k_MaximumChartCountersCount}";
        }

        void OnListViewItemMoved(ItemMoveArgs<object> args)
        {
            m_Module.SetUpdatedEditedStateForOrderIndexChange();
        }

        static class ProfilerMarkers
        {
            public static readonly ProfilerMarker k_LoadAllCounters = new ProfilerMarker("ModuleEditor.LoadAllCounters");
            public static readonly ProfilerMarker k_FetchCounters = new ProfilerMarker("ModuleEditor.FetchCounters");
            public static readonly ProfilerMarker k_FormatCountersForDisplay = new ProfilerMarker("ModuleEditor.FormatCountersForDisplay");
            public static readonly ProfilerMarker k_RebuildCountersUI = new ProfilerMarker("ModuleEditor.RebuildCountersUI");
        }

        class TreeViewItemData : TreeViewItem<string>
        {
            static int m_NextId = 0;

            public TreeViewItemData(string data, List<TreeViewItem<string>> children = null)
                : base(m_NextId++, data, children) {}

            public static void ResetNextId()
            {
                m_NextId = 0;
            }
        }

        class GroupTreeViewItemData : TreeViewItemData
        {
            public GroupTreeViewItemData(string group) : base(group) {}
        }

        class CategoryTreeViewItemData : TreeViewItemData
        {
            public CategoryTreeViewItemData(string category) : base(category) {}
        }

        class CounterTreeViewItemData : TreeViewItemData
        {
            public CounterTreeViewItemData(string counter) : base(counter) {}

            public string category
            {
                get
                {
                    var categoryTreeViewItemData = parent as CategoryTreeViewItemData;
                    return categoryTreeViewItemData.data;
                }
            }
        }

        abstract class CounterItem : VisualElement
        {
            const string k_UssClass_Label = "counter-list-view-item__label";

            public CounterItem()
            {
                titleLabel = new Label();
                titleLabel.AddToClassList(k_UssClass_Label);
                Add(titleLabel);
            }

            public Label titleLabel { get; }
        }

        class CounterListViewItem : CounterItem
        {
            const string k_UssClass = "counter-list-view-item";

            public CounterListViewItem()
            {
                AddToClassList(k_UssClass);

                var dragIndicator = new DragIndicator();
                Add(dragIndicator);
                dragIndicator.SendToBack();
            }
        }

        class CounterTreeViewItem : CounterItem
        {
            const string k_UssClass = "counter-tree-view-item";
            const string k_UssClass_Unselectable = "counter-tree-view-item__unselectable";

            public CounterTreeViewItem()
            {
                AddToClassList(k_UssClass);
            }

            public void SetSelectable(bool selectable)
            {
                EnableInClassList(k_UssClass_Unselectable, !selectable);
            }
        }
    }
}
