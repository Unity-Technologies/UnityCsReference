// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace Unity.UI.Builder
{
    [UxmlElement]
    [UsedImplicitly]
    class FilterStyleField : BaseField<List<FilterFunction>>
    {
        [Serializable]
        public new class UxmlSerializedData : BindableElement.UxmlSerializedData
        {
            public override object CreateInstance() => new FilterStyleField();
        }

        internal const string k_FilterListViewName = "filter-list-view";

        const string k_FilterFunctionDropdownClassName = "inspector-variables-dropdown";
        static readonly string k_AddMoreIconClassName = BaseListView.footerAddButtonName + "--with-menu";

        const string k_EmptyListText = "Click the + icon to create a new filter function.";
        const string k_EmptyListClassName = "filter-list-empty";

        const string k_FieldClassName = "unity-filter-style-field";
        const string k_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/FilterStyleField.uxml";
        const string k_UssPathNoExt = BuilderConstants.UtilitiesPath + "/StyleField/FilterStyleField";

        private ListView m_FilterListView;
        private List<FilterFunction> m_FilterSource;

        internal ListView filterListView => m_FilterListView;

        public FilterStyleField() : this(null) { }

        public FilterStyleField(string label) : base(label)
        {
            AddToClassList(BuilderConstants.InspectorContainerClassName);
            AddToClassList(k_FieldClassName);
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPathNoExt + (EditorGUIUtility.isProSkin ? "Dark" : "Light") + ".uss"));
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPathNoExt + ".uss"));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);

            template.CloneTree(this);

            m_FilterListView = this.Q<ListView>(k_FilterListViewName);
            m_FilterListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            m_FilterListView.selectionType = SelectionType.Multiple;
            m_FilterListView.makeNoneElement = () => new Label(L10n.Tr(k_EmptyListText)) { classList = { k_EmptyListClassName } };
            m_FilterListView.makeItem = () => {
                return new FilterFunctionListViewItem();
            };
            m_FilterListView.bindItem = (e, i) =>
            {
                var item = e as FilterFunctionListViewItem;
                var filterFunction = m_FilterSource[i];
                item.index = i;
                item.SetFilterFunction(filterFunction);
            };

            var enumData = UnityEngine.EnumDataUtility.GetCachedEnumData(
                typeof(FilterFunctionType),
                UnityEngine.EnumDataUtility.CachedType.ExcludeObsolete,
                NameFormatter.FormatVariableName);

            var menu = new GenericDropdownMenu();
            for (int i = 1; i < (int)FilterFunctionType.Count; ++i)
            {
                var ffType = (FilterFunctionType)i;
                menu.AddItem(enumData.displayNames[i], false, (_) => OnFilterFunctionAdded(ffType), null);
            }

            m_FilterListView.overridingAddButtonBehavior = (_, btn) =>
            {
                menu.DropDown(btn.worldBound, btn, DropdownMenuSizeMode.Auto);
                menu.contentContainer.AddToClassList(k_FilterFunctionDropdownClassName);
            };

            m_FilterListView.onRemove += OnRemoveFilterFunction;
            m_FilterListView.itemIndexChanged += OnListReordered;

            // The set icons for add/remove buttons
            var addButton = m_FilterListView.Q<Button>(BaseListView.footerAddButtonName);
            addButton.AddToClassList(k_AddMoreIconClassName);
        }

        public override void SetValueWithoutNotify(List<FilterFunction> newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_FilterSource = newValue ?? new List<FilterFunction>();
            m_FilterListView.itemsSource = m_FilterSource;
            m_FilterListView.RefreshItems();
        }

        internal void OnFilterFunctionAdded(FilterFunctionType ffType)
        {
            var f = new FilterFunction(ffType);

            var def = f.GetDefinition();
            if (def != null)
            {
                for (int i = 0; i < def.parameters.Length; ++i)
                    f.AddParameter(def.parameters[i].defaultValue);
            }

            using (var evt = FilterFunctionAddedEvent.GetPooled())
            {
                evt.elementTarget = this;
                evt.filterFunction = f;
                SendEvent(evt);
            }
        }

        internal void FilterFunctionTypeChanged(FilterFunctionListViewItem item)
        {
            var currentIndex = item.index;
            if (currentIndex >= 0 && currentIndex < m_FilterSource.Count)
            {
                m_FilterSource[currentIndex] = item.filterFunction;
            }

            using (var pooled = FilterFunctionChangedEvent.GetPooled())
            {
                pooled.elementTarget = this;
                pooled.item = item;
                pooled.filterFunction = item.filterFunction;
                pooled.index = currentIndex;
                SendEvent(pooled);
            }
        }

        internal void FilterFunctionValueChanged(FilterFunctionListViewItem item, int paramIndex)
        {
            var currentIndex = item.index;
            if (currentIndex >= 0 && currentIndex < m_FilterSource.Count)
            {
                m_FilterSource[currentIndex] = item.filterFunction;
            }
            using (var pooled = FilterFunctionValueChangedEvent.GetPooled())
            {
                pooled.elementTarget = this;
                pooled.item = item;
                pooled.filterFunction = item.filterFunction;
                pooled.index = currentIndex;
                pooled.paramIndex = paramIndex;
                SendEvent(pooled);
            }
        }

        internal void OnRemoveFilterFunction(BaseListView listView)
        {
            var indicesToRemove = new List<int>();

            // If no items are selected, remove last item
            if (listView.selectedIndex == -1 && m_FilterSource.Count > 0)
            {
                indicesToRemove.Add(m_FilterSource.Count - 1);
            }
            else
            {
                foreach (var selectedIndex in listView.selectedIndices)
                {
                    if (selectedIndex >= m_FilterSource.Count)
                        continue;

                    indicesToRemove.Add(selectedIndex);
                }
            }

            using (var evt = FilterFunctionRemovedEvent.GetPooled())
            {
                evt.elementTarget = this;
                evt.indices = indicesToRemove;
                SendEvent(evt);
            }
        }

        void OnListReordered(int previousIndex, int newIndex)
        {
            using (var evt = FilterFunctionReorderedEvent.GetPooled())
            {
                evt.elementTarget = this;
                evt.fromIndex = previousIndex;
                evt.toIndex = newIndex;
                SendEvent(evt);
            }
        }
    }
}
