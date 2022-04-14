// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ListView = UnityEngine.UIElements.ListView;

namespace UnityEditor.SceneTemplate
{
    class DependencyListView : VisualElement
    {
        enum DependencySortMode
        {
            Name,
            Type
        }

        struct DependencyComparer : IComparer<SerializedProperty>
        {
            public bool ascending;
            public DependencySortMode mode;

            DependencyComparer(DependencySortMode mode, bool ascending)
            {
                this.ascending = ascending;
                this.mode = mode;
            }

            public int Compare(SerializedProperty x, SerializedProperty y)
            {
                var dir = ascending ? 1 : -1;
                switch (mode)
                {
                    case DependencySortMode.Type:
                    {
                        var xType = FetchType(x);
                        var yType = FetchType(y);
                        return dir * xType.CompareTo(yType);
                    }
                    case DependencySortMode.Name:
                    {
                        var xName = FetchName(x);
                        var yName = FetchName(y);
                        return dir * xName.CompareTo(yName);
                    }
                }

                return 0;
            }

            public static DependencyComparer Default = new DependencyComparer(DependencySortMode.Type, true);
        }

        class DependencyRowItem : VisualElement
        {
            public Image icon { get; }
            public Label label { get; }
            public Label typeLabel { get; }
            public Toggle cloneToggle { get; }

            public DependencyRowItem()
            {
                style.flexDirection = FlexDirection.Row;
                name = k_DependencyRowElementName;

                icon = new Image() { scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
                icon.AddToClassList("scene-template-asset-inspector-dependency-row-icon");
                Add(icon);

                label = new Label();
                label.AddToClassList("scene-template-asset-inspector-dependency-row-label");
                Add(label);

                typeLabel = new Label();
                typeLabel.AddToClassList("scene-template-asset-inspector-dependency-row-type-label");
                Add(typeLabel);

                cloneToggle = new Toggle();
                cloneToggle.AddToClassList("scene-template-asset-inspector-dependency-row-clone-toggle");
                Add(cloneToggle);
            }
        }

        class SortableHeaderElement : VisualElement
        {
            const string k_AscendingArrow = "\u2191";
            const string k_DescendingArrow = "\u2193";

            Label m_Arrow;

            public SortableHeaderElement(string label, params string[] additionalClasses)
            {
                AddToClassList("dependency-list-view-header-sortable-item");
                foreach (var additionalClass in additionalClasses)
                {
                    AddToClassList(additionalClass);
                }

                var labelElement = new Label(label);
                labelElement.AddToClassList("dependency-list-view-header-item-label-common");
                labelElement.AddToClassList("dependency-list-view-header-sortable-item-label");
                Add(labelElement);

                m_Arrow = new Label();
                Add(m_Arrow);
            }

            public void UpdateSortingDirection(DependencySortMode mode, DependencyComparer currentComparer)
            {
                if (mode != currentComparer.mode)
                {
                    m_Arrow.text = string.Empty;
                    return;
                }

                m_Arrow.text = currentComparer.ascending ? k_AscendingArrow : k_DescendingArrow;
            }
        }

        const string k_ListInternalView = "dependency-list-internal-view";
        const string k_ListView = "dependency-list-view";
        const string k_HeaderItem = "dependency-list-view-header";
        const string k_SearchFieldItem = "dependency-list-view-search-field";
        const string k_DependencyRowElementName = "scene-template-asset-inspector-dependency-row";
        static readonly string k_BaseDependenciesLabel = L10n.Tr("Dependencies");
        static readonly string k_BaseTypeLabel = L10n.Tr("Type");

        public ListView listView { get; }
        public VisualElement header { get; }

        bool m_SearchFieldReady;
        internal bool viewReady => m_SearchFieldReady;

        Toggle m_CloneHeaderToggle;

        SerializedObject m_SerializedObject;

        readonly List<SerializedProperty> m_OriginalItems;
        List<SerializedProperty> m_FilteredItems;
        QueryEngine<SerializedProperty> m_QueryEngine;
        DependencyComparer m_DependencyComparer = DependencyComparer.Default;

        int m_ItemSize;
        string m_CurrentSearchString = string.Empty;

        public DependencyListView(List<SerializedProperty> itemsSource, int itemHeight, SerializedObject serializedObject)
        {
            SetupQueryEngine();

            m_SerializedObject = serializedObject;
            m_OriginalItems = itemsSource;
            m_FilteredItems = new List<SerializedProperty>();
            m_ItemSize = itemHeight;

            var listViewContainer = new VisualElement();
            listViewContainer.AddToClassList(k_ListView);
            listViewContainer.style.flexGrow = 1;

            listView = new ListView(m_FilteredItems, itemHeight, MakeItem, BindItem);
            listView.name = k_ListInternalView;
            listView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            listView.style.flexGrow = 1;
            listView.RegisterCallback<KeyUpEvent>(OnKeyUpEvent);
            listView.selectionType = SelectionType.Multiple;
            listView.itemsChosen += OnDoubleClick;
            listView.style.maxHeight = Mathf.Max(m_OriginalItems.Count * m_ItemSize + 100, listView.style.maxHeight.value.value);

            var searchField = new ToolbarSearchField();
            searchField.name = "dependency-listview-toolbar-searchfield";
            searchField.RegisterValueChangedCallback(evt =>
            {
                m_CurrentSearchString = evt.newValue;
                FilterItems(evt.newValue);
            });
            var textField = searchField.Q<TextField>();
            if (textField != null)
            {
                textField.maxLength = 1024;
                m_SearchFieldReady = true;
            }

            searchField.AddToClassList(k_SearchFieldItem);
            Add(searchField);

            header = MakeHeader();
            UpdateHeader();
            listViewContainer.Add(header);

            listViewContainer.Add(listView);
            Add(listViewContainer);

            FilterItems(searchField.value);
        }

        VisualElement MakeHeader()
        {
            var changeAllRowElement = new VisualElement();
            changeAllRowElement.AddToClassList(k_HeaderItem);
            changeAllRowElement.style.flexDirection = FlexDirection.Row;

            var dependenciesLabelField = new SortableHeaderElement(k_BaseDependenciesLabel, "scene-template-asset-inspector-dependency-header");
            dependenciesLabelField.RegisterCallback<ClickEvent>(evt =>
            {
                UpdateSortingMode(DependencySortMode.Name);
            });
            dependenciesLabelField.tooltip = L10n.Tr("Scene dependencies");
            changeAllRowElement.Add(dependenciesLabelField);

            var typeLabelField = new SortableHeaderElement(k_BaseTypeLabel, "scene-template-asset-inspector-dependency-header-type-column");
            typeLabelField.tooltip = L10n.Tr("Dependency type");
            typeLabelField.RegisterCallback<ClickEvent>(evt =>
            {
                UpdateSortingMode(DependencySortMode.Type);
            });
            changeAllRowElement.Add(typeLabelField);

            var cloneLabel = new Label(L10n.Tr("Clone"));
            cloneLabel.tooltip = L10n.Tr("Is the dependency cloned on scene template instantiation or is it referenced?");
            cloneLabel.AddToClassList("dependency-list-view-header-item-label-common");
            changeAllRowElement.Add(cloneLabel);
            m_CloneHeaderToggle = new Toggle();
            UpdateGlobalCloneToggle();
            m_CloneHeaderToggle.RegisterValueChangedCallback(evt =>
            {
                var listContent = listView.Q<VisualElement>("unity-content-container");
                foreach (var row in listContent.Children())
                {
                    var mode = evt.newValue ? TemplateInstantiationMode.Clone : TemplateInstantiationMode.Reference;
                    SetDependencyInstantiationMode(row, mode);
                }
                m_SerializedObject.ApplyModifiedProperties();
            });
            m_CloneHeaderToggle.AddToClassList("scene-template-asset-inspector-dependency-header-clone-column");
            changeAllRowElement.Add(m_CloneHeaderToggle);

            return changeAllRowElement;
        }

        static VisualElement MakeItem()
        {
            var row = new DependencyRowItem();
            return row;
        }

        void BindItem(VisualElement el, int modelIndex)
        {
            var property = m_FilteredItems[modelIndex];
            var rowItem = (DependencyRowItem)el;
            var depProperty = property.FindPropertyRelative(SceneTemplateUtils.DependencyPropertyName);
            var refValue = depProperty.objectReferenceValue;
            var type = refValue.GetType();
            var content = EditorGUIUtility.ObjectContent(refValue, type);
            rowItem.icon.image = content.image;
            rowItem.label.text = content.text;
            rowItem.label.tooltip = AssetDatabase.GetAssetPath(refValue);
            rowItem.typeLabel.text = type.Name;
            rowItem.typeLabel.tooltip = type.FullName;
            rowItem.userData = property;

            var instantiationModeProperty = property.FindPropertyRelative(SceneTemplateUtils.InstantiationModePropertyName);
            rowItem.cloneToggle.value = IsCloning(instantiationModeProperty);
            rowItem.cloneToggle.SetEnabled(SceneTemplateProjectSettings.Get().GetDependencyInfo(depProperty.objectReferenceValue).supportsModification);
            rowItem.cloneToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == IsCloning(instantiationModeProperty))
                    return;
                var newInstantiationType = (evt.newValue ? TemplateInstantiationMode.Clone : TemplateInstantiationMode.Reference);
                instantiationModeProperty.enumValueIndex = (int)newInstantiationType;

                // Sync Selection if the dependency is part of it:
                if (listView.selectedIndices.Contains(modelIndex))
                    SyncListSelectionToValue(newInstantiationType);
                m_SerializedObject.ApplyModifiedProperties();

                UpdateGlobalCloneToggle();
            });
        }

        bool AreAllFilteredDependenciesCloned()
        {
            return m_FilteredItems.Where(sp =>
            {
                var depProperty = sp.FindPropertyRelative(SceneTemplateUtils.DependencyPropertyName);
                return SceneTemplateProjectSettings.Get().GetDependencyInfo(depProperty.objectReferenceValue).supportsModification;
            }).All(sp =>
                {
                    var instantiationModeProperty = sp.FindPropertyRelative(SceneTemplateUtils.InstantiationModePropertyName);
                    return IsCloning(instantiationModeProperty);
                });
        }

        static bool IsCloning(SerializedProperty prop)
        {
            var instantiationMode = (TemplateInstantiationMode)prop.enumValueIndex;
            return instantiationMode == TemplateInstantiationMode.Clone;
        }

        void OnKeyUpEvent(KeyUpEvent e)
        {
            if (e.keyCode == KeyCode.Space)
            {
                if (listView.selectedIndex == -1)
                    return;

                // If there is any value that is not set to Clone, set everything to Clone. Otherwise,
                // set everything to Reference.
                var selectedItems = GetSelectedDependencies();
                var allClone = selectedItems.Select(item => item.FindPropertyRelative(SceneTemplateUtils.InstantiationModePropertyName))
                    .All(instantiationModeProperty => instantiationModeProperty.enumValueIndex == (int)TemplateInstantiationMode.Clone);

                var newEnumValue = allClone ? TemplateInstantiationMode.Reference : TemplateInstantiationMode.Clone;
                SyncListSelectionToValue(newEnumValue);

                e.StopPropagation();
            }
        }

        void SyncListSelectionToValue(TemplateInstantiationMode mode)
        {
            if (listView.selectedIndex != -1)
            {
                var listContent = listView.Q<VisualElement>("unity-content-container");
                foreach (var row in listContent.Children())
                {
                    if (row.ClassListContains("unity-list-view__item--selected"))
                    {
                        SetDependencyInstantiationMode(row, mode);
                    }
                }
            }
            m_SerializedObject.ApplyModifiedProperties();
        }

        static void SetDependencyInstantiationMode(VisualElement row, TemplateInstantiationMode mode)
        {
            var prop = (SerializedProperty)row.userData;
            var rowItem = (DependencyRowItem)row;
            var toggle = rowItem.cloneToggle;
            if (!toggle.enabledSelf)
                return;

            toggle.SetValueWithoutNotify(mode == TemplateInstantiationMode.Clone);

            var depProp = prop.FindPropertyRelative(SceneTemplateUtils.InstantiationModePropertyName);
            if (depProp.enumValueIndex != (int)mode)
            {
                depProp.enumValueIndex = (int)mode;
            }
        }

        IEnumerable<SerializedProperty> GetSelectedDependencies()
        {
            var selectedItems = new List<SerializedProperty>();
            if (listView.selectedIndex != -1)
            {
                var listContent = listView.Q<VisualElement>("unity-content-container");
                foreach (var row in listContent.Children())
                {
                    var prop = (SerializedProperty)row.userData;
                    if (row.ClassListContains("unity-list-view__item--selected"))
                    {
                        selectedItems.Add(prop);
                    }
                }
            }
            return selectedItems;
        }

        static void OnDoubleClick(IEnumerable<object> objs)
        {
            var obj = objs.FirstOrDefault();
            var property = obj as SerializedProperty;
            if (property == null)
                return;

            var depProperty = property.FindPropertyRelative(SceneTemplateUtils.DependencyPropertyName);
            if (depProperty.objectReferenceValue != null)
            {
                EditorGUIUtility.PingObject(depProperty.objectReferenceValue);
            }
        }

        void FilterItems(string searchText)
        {
            m_FilteredItems.Clear();

            var query = m_QueryEngine.ParseQuery(searchText);
            query.returnPayloadIfEmpty = true;
            foreach (var item in query.Apply(m_OriginalItems))
            {
                m_FilteredItems.Add(item);
            }

            UpdateListSorting(false);
            listView?.Rebuild();
            UpdateListViewSize();
            UpdateGlobalCloneToggle();
        }

        public void Refresh()
        {
            FilterItems(m_CurrentSearchString);
        }

        public void UpdateListViewSize()
        {
            listView.style.height = (m_FilteredItems.Count) * m_ItemSize;
        }

        void SetupQueryEngine()
        {
            m_QueryEngine = new QueryEngine<SerializedProperty>();
            m_QueryEngine.SetSearchDataCallback(GetSearchData, s => s.ToLowerInvariant(), StringComparison.Ordinal);

            m_QueryEngine.AddFilter("t", FetchType, StringComparison.Ordinal, new[] {":"});
            m_QueryEngine.AddTypeParser(s => new ParseResult<string>(true, s.ToLowerInvariant()));
        }

        static IEnumerable<string> GetSearchData(SerializedProperty arg)
        {
            if (arg == null)
                yield break;
            yield return FetchName(arg);
        }

        void UpdateGlobalCloneToggle()
        {
            m_CloneHeaderToggle.SetValueWithoutNotify(AreAllFilteredDependenciesCloned());
        }

        static string FetchName(SerializedProperty property)
        {
            var depProperty = property.FindPropertyRelative(SceneTemplateUtils.DependencyPropertyName);
            var refValue = depProperty.objectReferenceValue;
            return EditorGUIUtility.GetObjectNameWithInfo(refValue).ToLowerInvariant();
        }

        static string FetchType(SerializedProperty property)
        {
            var depProperty = property.FindPropertyRelative(SceneTemplateUtils.DependencyPropertyName);
            return depProperty.objectReferenceValue.GetType().Name.ToLowerInvariant();
        }

        void UpdateSortingMode(DependencySortMode newMode)
        {
            if (m_DependencyComparer.mode != newMode)
                m_DependencyComparer.mode = newMode;
            else
                m_DependencyComparer.ascending = !m_DependencyComparer.ascending;
            UpdateHeader();
            UpdateListSorting();
        }

        void UpdateListSorting(bool refreshListView = true)
        {
            m_FilteredItems.Sort(m_DependencyComparer);
            if (refreshListView)
                listView?.Rebuild();
        }

        void UpdateHeader()
        {
            var dependencyElement = (SortableHeaderElement)header.ElementAt(0);
            var typeElement = (SortableHeaderElement)header.ElementAt(1);

            dependencyElement.UpdateSortingDirection(DependencySortMode.Name, m_DependencyComparer);
            typeElement.UpdateSortingDirection(DependencySortMode.Type, m_DependencyComparer);
        }
    }
}
