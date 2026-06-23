// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    [UxmlElement]
    [UsedImplicitly]
    [MovedFrom("Unity.UI.Builder")]
    internal partial class FilterStyleField : BaseField<List<FilterFunction>>
    {
        internal const string k_FilterListViewName = "filter-list-view";

        const string k_FilterFunctionDropdownClassName = "inspector-variables-dropdown";
        static readonly string k_InspectorContainerClassName = "unity-ui-inspector__container";
        static readonly string k_AddMoreIconClassName = BaseListView.footerAddButtonName + "--with-menu";
        static readonly int k_DefaultMenuFontSize = 12;

        const string k_EmptyListText = "Click the + icon to create a new filter function.";
        const string k_EmptyListClassName = "filter-list-empty";

        const string k_FieldClassName = "unity-filter-style-field";
        const string k_UxmlPath = "UIToolkitAuthoring/Inspector/Controls/Filters/FilterStyleField.uxml";
        const string k_UssPathNoExt = "UIToolkitAuthoring/Inspector/Controls/Filters/FilterStyleField";

        private ListView m_FilterListView;
        private List<FilterFunction> m_FilterSource;
        private bool m_AllowCustomFilters = true;

        internal ListView filterListView => m_FilterListView;

        // Added to prevent the error regarding a missing converter for List<FilterFunction>.
        [UxmlAttribute("value"), HideInInspector]
        internal string valueOverride { get; set; }
        
        /// <summary>
        /// Gets or sets whether custom filters can be added.
        /// When false, the Custom filter type will not appear in the add filter dropdown.
        /// </summary>
        public bool allowCustomFilters
        {
            get => m_AllowCustomFilters;
            set => m_AllowCustomFilters = value;
        }

        public FilterStyleField()
            : this(null) { }

        public FilterStyleField(string label)
            : base(label)
        {
            AddToClassList(k_InspectorContainerClassName);
            AddToClassList(k_FieldClassName);

            var template = EditorGUIUtility.Load(k_UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);

            styleSheets.Add(EditorGUIUtility.Load(k_UssPathNoExt + ".uss") as StyleSheet);
            styleSheets.Add(
                EditorGUIUtility.Load(k_UssPathNoExt + (EditorGUIUtility.isProSkin ? "Dark" : "Light") + ".uss") as
                    StyleSheet);

            m_FilterListView = this.Q<ListView>(k_FilterListViewName);
            m_FilterListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            m_FilterListView.selectionType = SelectionType.Multiple;
            m_FilterListView.makeNoneElement =
                () => new Label(L10n.Tr(k_EmptyListText)).WithClassList(k_EmptyListClassName);
            m_FilterListView.makeItem = () =>
            {
                return new FilterFunctionListViewItem();
            };
            m_FilterListView.bindItem = (e, i) =>
            {
                var item = e as FilterFunctionListViewItem;
                var filterFunction = m_FilterSource[i];
                item.index = i;
                item.SetFilterFunction(filterFunction);
            };

            m_FilterListView.overridingAddButtonBehavior = (_, btn) =>
            {
                // Build menu dynamically to respect current allowCustomFilters setting
                var enumData = UnityEngine.EnumDataUtility.GetCachedEnumData(
                    typeof(FilterFunctionType),
                    UnityEngine.EnumDataUtility.CachedType.ExcludeObsolete,
                    NameFormatter.FormatVariableName);

                var menu = new GenericDropdownMenu();
                int count = enumData.displayNames.Length;
                for (int i = 1; i < count; ++i)
                {
                    var ffType = (FilterFunctionType)i;

                    // Skip Custom filter type if not allowed
                    if (!m_AllowCustomFilters && ffType == FilterFunctionType.Custom)
                        continue;

                    menu.AddItem(enumData.displayNames[i], false, (_) => OnFilterFunctionAdded(ffType), null);
                }

                menu.DropDown(btn.worldBound, btn, DropdownMenuSizeMode.Auto);
                menu.contentContainer.AddToClassList(k_FilterFunctionDropdownClassName);
                menu.contentContainer.style.fontSize = k_DefaultMenuFontSize;
            };

            m_FilterListView.onRemove += OnRemoveFilterFunction;
            m_FilterListView.itemIndexChanged += OnListReordered;

            // The set icons for add/remove buttons
            var addButton = m_FilterListView.Q<Button>(BaseListView.footerAddButtonName);
            addButton.AddToClassList(k_AddMoreIconClassName);
            SetValueWithoutNotify(new List<FilterFunction>());
        }

        public override void SetValueWithoutNotify(List<FilterFunction> newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_FilterSource = newValue ?? new List<FilterFunction>();
            m_FilterListView.itemsSource = m_FilterSource;
            m_FilterListView.RefreshItems();
        }

        internal void SetValueWithoutRefresh(List<FilterFunction> newValue)
        {
            base.SetValueWithoutNotify(newValue);
            // Update m_FilterSource in place to keep it in sync without reassigning itemsSource
            // which could cause UI stutters during dragging
            m_FilterSource.Clear();
            if (newValue != null)
            {
                m_FilterSource.AddRange(newValue);
            }
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

            // Build the new filter list with the added filter
            var newFilterList = new List<FilterFunction>(value ?? new List<FilterFunction>());
            newFilterList.Add(f);
            value = newFilterList;

            using (var evt = FilterListChangedEvent.GetPooled())
            {
                evt.elementTarget = this;
                evt.newFilterList = newFilterList;
                evt.refreshField = true;
                SendEvent(evt);
            }
        }

        internal void FilterFunctionTypeChanged(FilterFunctionListViewItem item)
        {
            var currentIndex = item.index;

            // Build the new filter list with the changed filter
            var newFilterList = new List<FilterFunction>(m_FilterSource ?? new List<FilterFunction>());
            if (currentIndex >= 0 && currentIndex < newFilterList.Count)
            {
                newFilterList[currentIndex] = item.filterFunction;
            }

            // Update the field's value immediately so it's not stale for subsequent changes
            value = newFilterList;

            using (var pooled = FilterListChangedEvent.GetPooled())
            {
                pooled.elementTarget = this;
                pooled.newFilterList = newFilterList;
                pooled.refreshField = true;
                SendEvent(pooled);
            }
        }

        internal void FilterFunctionValueChanged(FilterFunctionListViewItem item, int paramIndex)
        {
            var currentIndex = item.index;

            // Build the new filter list with the changed filter
            var newFilterList = new List<FilterFunction>(m_FilterSource ?? new List<FilterFunction>());
            if (currentIndex >= 0 && currentIndex < newFilterList.Count)
            {
                newFilterList[currentIndex] = item.filterFunction;
            }

            value = newFilterList;

            using (var pooled = FilterListChangedEvent.GetPooled())
            {
                pooled.elementTarget = this;
                pooled.newFilterList = newFilterList;
                pooled.refreshField = false;
                SendEvent(pooled);
            }
        }

        internal void OnRemoveFilterFunction(BaseListView listView)
        {
            var indicesToRemove = new List<int>();

            // If no items are selected, remove last item
            if (listView.selectedIndex == -1 && value.Count > 0)
            {
                indicesToRemove.Add(value.Count - 1);
            }
            else
            {
                foreach (var selectedIndex in listView.selectedIndicesList)
                {
                    if (selectedIndex >= value.Count)
                        continue;

                    indicesToRemove.Add(selectedIndex);
                }
            }

            // Build the new filter list with the removed filters
            var newFilterList = new List<FilterFunction>(value ?? new List<FilterFunction>());
            var sortedIndices = new List<int>(indicesToRemove);
            sortedIndices.Sort((a, b) => b.CompareTo(a)); // Sort in descending order
            foreach (var index in sortedIndices)
            {
                if (index >= 0 && index < newFilterList.Count)
                    newFilterList.RemoveAt(index);
            }

            value = newFilterList;

            using (var evt = FilterListChangedEvent.GetPooled())
            {
                evt.elementTarget = this;
                evt.newFilterList = newFilterList;
                evt.refreshField = true;
                SendEvent(evt);
            }
        }

        void OnListReordered(int previousIndex, int newIndex)
        {
            // Update the field's value to match m_FilterSource, which has been reordered by the ListView
            SetValueWithoutNotify(new List<FilterFunction>(m_FilterSource));

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
