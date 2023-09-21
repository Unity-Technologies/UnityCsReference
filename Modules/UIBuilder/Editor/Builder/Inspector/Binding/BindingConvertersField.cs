// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Represents a view used to edit the list of local converters to source or to target of a Binding instance
    /// </summary>
    class BindingConvertersField : BaseField<string>
    {
        /// <summary>
        /// Provides completion to the field used to edit the local converters of a Binding instance.
        /// </summary>
        public class Completer : FieldSearchCompleter<ConverterGroup>
        {
            private static readonly ConverterGroup s_CurrentTextGroup = new ConverterGroup(BuilderConstants.CompleterCurrentEntryItemId, null, null);
            private BindingConvertersField m_Field;
            private List<ConverterGroup> m_MatchingConverterGroups = new ();

            private ShowOnlyCompatibleResultsToggle m_ShowOnlyCompatibleResultsToggle;
            private BindingConverterGroupDetailsView m_DetailsView;
            private bool m_ShowsOnlyCompatible = true;

            /// <summary>
            /// Returns and sets the value that indicates whether the completer should only list out converter groups compatible with the types of the bound properties.
            /// </summary>
            public bool showsOnlyCompatibleResults
            {
                get => m_ShowsOnlyCompatible;
                set
                {
                    if (m_ShowsOnlyCompatible == value)
                        return;

                    m_ShowsOnlyCompatible = value;
                    if (m_ShowOnlyCompatibleResultsToggle != null)
                    {
                        m_ShowOnlyCompatibleResultsToggle.toggle.value = value;
                        UpdateResults();
                    }
                }
            }

            public Completer(BindingConvertersField field) : base(field.m_SearchField)
            {
                m_Field = field;
                SetupCompleterField(m_Field.searchField, true);
                alwaysVisible = true;
                dataSourceCallback = () => m_MatchingConverterGroups;
                makeItem = () => new BindingConverterGroupViewItem();
                bindItem = (e, i) =>
                {
                    var item = e as BindingConverterGroupViewItem;
                    var group = results[i];

                    item.getTextFromDataCallback = field.GetViewItemDisplayTextFromConverterGroup;
                    item.EnableInClassList(BindingConverterGroupViewItem.s_CheckUssClassName, m_Field.m_ValueAsConverterGroupList.Contains(group));
                    item.SetGroup(group, m_Field.GetGroupCompatibility(group));
                    item.tooltip = string.Empty; // Do not display tooltip.
                };

                matcherCallback = Matcher;
                getTextFromDataCallback = field.GetCompletionTextFromConverterGroup;

                // Set up the detail view that shows information about the selected or hovered property
                hoveredItemChanged += group =>
                {
                    // If no item is hovered over then fallback to the selected item
                    if (string.IsNullOrEmpty(group?.id))
                        group = selectedData;
                    ShowGroupDetails(group);
                };

                selectionChanged += ShowGroupDetails;
            }

            protected override VisualElement MakeDetailsContent()
            {
                m_DetailsView = new BindingConverterGroupDetailsView();
                m_DetailsView.style.display = DisplayStyle.None;
                return m_DetailsView;
            }

            protected override VisualElement MakeFooterContent()
            {
                m_ShowOnlyCompatibleResultsToggle = new ShowOnlyCompatibleResultsToggle();
                m_ShowOnlyCompatibleResultsToggle.toggle.value = showsOnlyCompatibleResults;
                m_ShowOnlyCompatibleResultsToggle.toggle.RegisterValueChangedCallback((evt) =>
                {
                    showsOnlyCompatibleResults = evt.newValue;
                    Refresh();
                });

                UpdateResults();

                return m_ShowOnlyCompatibleResultsToggle;
            }

            protected override string GetResultCountText(int count)
            {
                return base.GetResultCountText(string.IsNullOrEmpty(textField.text) ? count : count - 1);
            }

            void ShowGroupDetails(ConverterGroup group)
            {
                if (string.IsNullOrEmpty(group?.id))
                {
                    m_DetailsView.style.display = DisplayStyle.None;
                }
                else
                {
                    m_DetailsView.SetGroup(group, m_Field.GetGroupCompatibility(group));
                    m_DetailsView.style.display = DisplayStyle.Flex;
                }
            }

            protected override bool MatchFilter(string filter, in ConverterGroup data)
            {
                if (data.id == BuilderConstants.CompleterCurrentEntryItemId)
                    return !string.IsNullOrEmpty(filter);
                return base.MatchFilter(filter, data);
            }

            protected override FieldSearchCompleterPopup CreatePopup()
            {
                var popup = base.CreatePopup();
                var sheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UssPath_BindingWindow);

                popup.styleSheets.Add(sheet);
                return popup;
            }

            static bool Matcher(string filter, ConverterGroup group)
            {
                return !string.IsNullOrEmpty(group.id) && FuzzySearch.FuzzyMatch(filter, group.id)
                    || !string.IsNullOrEmpty(group.displayName) && FuzzySearch.FuzzyMatch(filter, group.displayName);
            }

            /// <summary>
            /// Updates the completer results
            /// </summary>
            public void UpdateResults()
            {
                if (m_ShowOnlyCompatibleResultsToggle == null)
                    return;

                m_MatchingConverterGroups.Clear();

                var groups = showsOnlyCompatibleResults ? m_Field.m_CompatibleGroups : m_Field.m_AllGroups;

                m_MatchingConverterGroups.AddRange(groups);
                m_MatchingConverterGroups.Add(s_CurrentTextGroup);
                m_ShowOnlyCompatibleResultsToggle.SetCompatibleResultCount(m_Field.m_CompatibleGroups.Count, m_Field.m_AllGroups.Count);
            }
        }

        private static readonly string s_UssClassName = "binding-converters-field";
        private static readonly string s_ListViewUssClassName = s_UssClassName + "__list-view";
        private static readonly string s_SearchFieldContainerName = "converter-search-field-container";
        private static readonly string s_SearchFieldName = "converter-search-field";

        private TextField m_SearchField;
        private ListView m_ListView;

        private Completer m_Completer;
        private List<ConverterGroup> m_ValueAsConverterGroupList = new();
        private bool m_UpdateGroupsBlocked;

        private List<ConverterGroup> m_CompatibleGroups = new();
        private List<ConverterGroup> m_AllGroups = new ();

        public Completer completer => m_Completer;

        public TextField searchField => m_SearchField;

        public ListView listView => m_ListView;

        /// <summary>
        /// Constructs a field
        /// </summary>
        public BindingConvertersField() : base(string.Empty)
        {
            AddToClassList(s_UssClassName);

            visualInput = new VisualElement();

            m_SearchField = new TextField() {name = s_SearchFieldName};
            m_SearchField.isDelayed = true;
            m_SearchField.placeholderText = BuilderConstants.BindingWindowLocalConverterPlaceholderText;
            m_ListView = new ListView
            {
                classList = {s_ListViewUssClassName},
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                itemsSource = m_ValueAsConverterGroupList,
                makeItem = () => new BindingConverterGroupViewItem(),
                bindItem = (ve, i) =>
                {
                    var item = ve as BindingConverterGroupViewItem;
                    var group = m_ValueAsConverterGroupList[i];

                    // If the path contains indices then replace them by 'index'
                    item.getTextFromDataCallback = GetViewItemDisplayTextFromConverterGroup;
                    item.SetGroup(group, GetGroupCompatibility(group));
                    item.onDeleteButtonClicked = () => { RemoveConverterGroupAt(i); };
                },
                unbindItem = (e, i) =>
                {
                    var item = e as BindingConverterGroupViewItem;

                    item.onDeleteButtonClicked = null;
                }
            };

            m_ListView.itemIndexChanged += (_, _) => UpdateValueFromGroups();

            var searchFieldContainer = new VisualElement() { name = s_SearchFieldContainerName };

            searchFieldContainer.Add(m_SearchField);

            visualInput.Add(m_ListView);
            visualInput.Add(searchFieldContainer);

            m_Completer = new Completer(this);
            m_Completer.itemChosen += (i) =>
            {
                var chosenGroup = m_Completer.results[i];
                var id = chosenGroup.id;

                if (chosenGroup.id == BuilderConstants.CompleterCurrentEntryItemId)
                    id = m_SearchField.text;

                AddConverterGroups(id);
            };

            // Ensure this is done after the completer registers its callbacks.
            m_SearchField.RegisterValueChangedCallback((evt) => AddConverterGroups(evt.newValue));
        }

        string GetViewItemDisplayTextFromConverterGroup(ConverterGroup group)
        {
            if (group.id == BuilderConstants.CompleterCurrentEntryItemId)
            {
                return string.Format(BuilderConstants.BindingWindowConverterCompleter_UseCurrentEntryMessage, m_SearchField.text);
            }
            return !string.IsNullOrEmpty(group.displayName) ? $"{group.displayName} ({group.id})" : group.id;
        }

        string GetCompletionTextFromConverterGroup(ConverterGroup group)
        {
            if (group.id == BuilderConstants.CompleterCurrentEntryItemId)
            {
                return m_SearchField.text;
            }
            return group.id;
        }

        /// <summary>
        /// Sets the current data source context.
        /// </summary>
        /// <param name="element">The selected VisualElement</param>
        /// <param name="binding">The edited binding</param>
        /// <param name="bindingDataSource">The current data source</param>
        /// <param name="bindingDataSourceType">The current data source type</param>
        public void SetDataSourceContext(VisualElement element, string dataSourcePath, string property, object bindingDataSource, Type bindingDataSourceType, bool isConverterToSource)
        {
            m_AllGroups.Clear();
            m_CompatibleGroups.Clear();

            var uiProperty = PropertyContainer.GetProperty(element, new PropertyPath(property));
            var uiPropertyType = uiProperty?.DeclaredValueType();
            var propertyPath = new PropertyPath(dataSourcePath);

            Type sourcePropertyType = null; // If the binding data source is specified then ignore the binding data source type.

            if (bindingDataSource != null)
            {
                var source = bindingDataSource;

                if (source is BuilderObjectField.NonUnityObjectValue nonUnityObject)
                    source = nonUnityObject.data;

                if (propertyPath.IsEmpty)
                {
                    sourcePropertyType = bindingDataSource.GetType();
                }
                else if (PropertyContainer.TryGetProperty(source, propertyPath, out var sourceProperty))
                {
                    sourcePropertyType = sourceProperty.DeclaredValueType();
                }
            }
            else if (bindingDataSourceType != null)
            {
                if (propertyPath.IsEmpty)
                    sourcePropertyType = bindingDataSourceType;
                else
                {
                    using (ListPool<PropertyPathInfo>.Get(out var properties))
                    {
                        DataBindingUtility.GetPropertyPaths(bindingDataSourceType, int.MaxValue, properties);

                        var sourceProperty = new PropertyPathInfo();
                        foreach (var p in properties)
                        {
                            if (p.propertyPath == propertyPath)
                            {
                                sourceProperty = p;
                                break;
                            }
                        }

                        sourcePropertyType = sourceProperty.type;
                    }
                }
            }

            using var _ = ListPool<string>.Get(out var compatibleGroupIds);

            if (uiPropertyType != null && sourcePropertyType != null)
            {
                if (isConverterToSource)
                    DataBindingUtility.GetMatchingConverterGroups(uiPropertyType, sourcePropertyType, compatibleGroupIds);
                else
                    DataBindingUtility.GetMatchingConverterGroups(sourcePropertyType, uiPropertyType, compatibleGroupIds);
            }
            else if (uiPropertyType != null)
            {
                if (isConverterToSource)
                    DataBindingUtility.GetMatchingConverterGroupsFromType(uiPropertyType, compatibleGroupIds);
                else
                    DataBindingUtility.GetMatchingConverterGroupsToType(uiPropertyType, compatibleGroupIds);
            }

            ConverterGroups.GetAllConverterGroups(m_AllGroups);

            foreach (var group in m_AllGroups)
            {
                if (compatibleGroupIds.Contains(group.id))
                    m_CompatibleGroups.Add(group);
            }

            m_Completer.UpdateResults();
            UpdateGroupsFromValue();
        }

        ConverterGroup FindGroup(string groupId)
        {
            foreach (var g in m_AllGroups)
            {
                if (g.id == groupId)
                    return g;
            }

            return null;
        }

        public BindingCompatibilityStatus GetGroupCompatibility(ConverterGroup group)
        {
            var status = BindingCompatibilityStatus.Unknown;

            if (m_CompatibleGroups.Contains(group))
            {
                status = BindingCompatibilityStatus.Compatible;
            }
            else if (m_AllGroups.Contains(group))
            {
                status = BindingCompatibilityStatus.Incompatible;
            }

            return status;
        }

        private void UpdateValueFromGroups()
        {
            var list = ListPool<string>.Get();
            try
            {
                m_UpdateGroupsBlocked = true;
                foreach (var group in m_ValueAsConverterGroupList)
                {
                    list.Add(group.id);
                }
                value = string.Join(", ", list);
            }
            finally
            {
                m_UpdateGroupsBlocked = false;
                ListPool<string>.Release(list);
            }
        }

        private void UpdateGroupsFromValue()
        {
            if (m_UpdateGroupsBlocked)
                return;

            m_ValueAsConverterGroupList.Clear();
            AddConverterGroupsFromCommaSeparatedList(value);
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
            UpdateGroupsFromValue();
        }

        private void AddConverterGroups(string groupIds)
        {
            if (AddConverterGroupsFromCommaSeparatedList(groupIds) == false)
                return;
            UpdateValueFromGroups();
            m_SearchField.value = string.Empty;
            m_SearchField.cursorIndex = 0;
        }

        private bool AddConverterGroupsFromCommaSeparatedList(string groupIds)
        {
            if (string.IsNullOrEmpty(groupIds))
                return false;

            var groupIdArray = groupIds.Split(",");
            var groupIdsToAdd = new List<string>();

            foreach (var groupId in groupIdArray)
            {
                groupIdsToAdd.Add(groupId.Trim());
            }

            var added = false;
            foreach (var groupIdToAdd in groupIdsToAdd)
            {
                if (string.IsNullOrEmpty(groupIdToAdd))
                    continue;

                var found = false;
                foreach (var existingGroup in m_ValueAsConverterGroupList)
                {
                    if (existingGroup.id == groupIdToAdd)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                    continue;

                m_ValueAsConverterGroupList.Add(FindGroup(groupIdToAdd) ?? new ConverterGroup(groupIdToAdd));
                added = true;
            }
            m_ListView.Rebuild();
            return added;
        }

        private void RemoveConverterGroupAt(int index)
        {
            m_ValueAsConverterGroupList.RemoveAt(index);
            m_ListView.RefreshItems();
            UpdateValueFromGroups();
        }

        internal bool ContainsUnknownCompatibilityGroup()
        {
            foreach (var converterGroup in m_ValueAsConverterGroupList)
            {
                if (GetGroupCompatibility(converterGroup) == BindingCompatibilityStatus.Unknown)
                    return true;
            }
            return false;
        }
    }
}
