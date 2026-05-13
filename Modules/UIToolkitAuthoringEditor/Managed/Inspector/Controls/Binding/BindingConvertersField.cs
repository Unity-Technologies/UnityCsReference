// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Represents a view used to edit the list of local converters to source or to target of a Binding instance
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class BindingConvertersField : BaseField<string>
    {
        /// <summary>
        /// The context of the converters field
        /// </summary>
        public record struct Context(
            VisualElement EditedElement,
            string BindingProperty,
            object DataSourceObject,
            Type DataSourceType,
            string DataSourcePath,
            bool ConvertersToSource
        );

        /// <summary>
        /// Provides completion to the field used to edit the local converters of a Binding instance.
        /// </summary>
        public class ConvertersCompleter : FieldSearchCompleter<ConverterGroup>
        {
            public static readonly string CurrentEntryItemId = "__unity-ui-completer-current-entry-item";
            public static readonly string IncompatibleMessage = L10n.Tr("Not currently compatible with type");
            public static readonly string CompatibleMessage = L10n.Tr("Compatible with type");
            public static readonly string UnknownCompatibilityMessage = L10n.Tr("Unknown");
            public static readonly string SelectEditedText = L10n.Tr("Select to use a custom converter ID");
            public static readonly string UseCurrentEntryMessage = L10n.Tr("Use \"{0}\" as converter ID");

            private static readonly ConverterGroup s_CurrentTextGroup = new(CurrentEntryItemId);
            private BindingConvertersField m_Field;
            private List<ConverterGroup> m_MatchingConverterGroups = new ();

            private ShowOnlyCompatibleResultsToggle m_ShowOnlyCompatibleResultsToggle;
            private BindingConverterGroupDetailsView m_DetailsView;
            private bool m_ShowsOnlyCompatible = true;

            /// <summary>
            /// Returns and sets the value that indicates whether the completer should only list out converter groups compatible with the types of the bound properties.
            /// </summary>
            public bool ShowsOnlyCompatibleResults
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

            public ConvertersCompleter(BindingConvertersField field)
            {
                m_Field = field;
                SetupCompleterField(m_Field.SearchField, true);
                AlwaysVisible = true;
                DataSourceCallback = () => m_MatchingConverterGroups;
                MakeItem = () => new BindingConverterGroupViewItem();
                BindItem = (e, i) =>
                {
                    var item = e as BindingConverterGroupViewItem;
                    var group = Results[i];

                    item.getTextFromDataCallback = field.GetViewItemDisplayTextFromConverterGroup;
                    item.EnableInClassList(BindingConverterGroupViewItem.s_CheckUssClassName, m_Field.m_ValueAsConverterGroupList.Contains(group));
                    item.SetGroup(group, m_Field.GetGroupCompatibility(group));
                    item.tooltip = string.Empty; // Do not display tooltip.
                };

                MatcherCallback = Matcher;
                GetTextFromDataCallback = field.GetCompletionTextFromConverterGroup;

                // Set up the detail view that shows information about the selected or hovered property
                HoveredItemChanged += group =>
                {
                    // If no item is hovered over then fallback to the selected item
                    if (string.IsNullOrEmpty(group?.id))
                        group = SelectedData;
                    ShowGroupDetails(group);
                };

                SelectionChanged += ShowGroupDetails;
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
                m_ShowOnlyCompatibleResultsToggle.toggle.value = ShowsOnlyCompatibleResults;
                m_ShowOnlyCompatibleResultsToggle.toggle.RegisterValueChangedCallback((evt) =>
                {
                    ShowsOnlyCompatibleResults = evt.newValue;
                    Refresh();
                });

                UpdateResults();

                return m_ShowOnlyCompatibleResultsToggle;
            }

            protected override string GetResultCountText(int count)
            {
                return base.GetResultCountText(string.IsNullOrEmpty(AttachedTextField.text) ? count : count - 1);
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
                if (data.id == CurrentEntryItemId)
                    return !string.IsNullOrEmpty(filter);
                return base.MatchFilter(filter, data);
            }

            protected override FieldSearchCompleterPopup CreatePopup()
            {
                var popup = base.CreatePopup();

                AddConvertersFieldStyleSheets(popup);
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

                var groups = ShowsOnlyCompatibleResults ? m_Field.m_CompatibleGroups : m_Field.m_AllGroups;

                m_MatchingConverterGroups.AddRange(groups);
                m_MatchingConverterGroups.Add(s_CurrentTextGroup);
                m_ShowOnlyCompatibleResultsToggle.SetCompatibleResultCount(m_Field.m_CompatibleGroups.Count, m_Field.m_AllGroups.Count);
            }
        }

        private static readonly string s_UssClassName = "binding-converters-field";
        private static readonly string s_ListViewUssClassName = s_UssClassName + "__list-view";
        private static readonly string s_SearchFieldContainerName = "converter-search-field-container";
        private static readonly string s_SearchFieldName = "converter-search-field";
        public static readonly string BindingWindowLocalConverterPlaceholderText = L10n.Tr("Enter a converter ID");

        private const string k_StyleSheet = "UIToolkitAuthoring/Inspector/Binding/BindingConvertersField.uss";
        private const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/Binding/BindingConvertersFieldDark.uss";
        private const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/Binding/BindingConvertersFieldLight.uss";

        private TextField m_SearchField;
        private ListView m_ListView;

        ConvertersCompleter m_Completer;
        BindingConvertersFieldController m_Controller;
        private List<ConverterGroup> m_ValueAsConverterGroupList = new();
        private bool m_UpdateGroupsBlocked;

        private List<ConverterGroup> m_CompatibleGroups = new();
        private List<ConverterGroup> m_AllGroups = new ();

        public ConvertersCompleter Completer => m_Completer;

        public TextField SearchField => m_SearchField;

        public ListView ListView => m_ListView;

        /// <summary>
        /// The Visual Element being editing that is used to determine compability of the converters
        /// </summary>
        public VisualElement EditedElement
        {
            get => m_Controller.EditedElement;
            set => m_Controller.EditedElement = value;
        }

        /// <summary>
        /// Constructs a field
        /// </summary>
        public BindingConvertersField() : base(string.Empty)
        {
            AddToClassList(s_UssClassName);

            visualInput = new VisualElement();

            m_SearchField = new TextField() {name = s_SearchFieldName};
            m_SearchField.isDelayed = true;
            m_SearchField.placeholderText = BindingWindowLocalConverterPlaceholderText;
            m_ListView = new ListView
            {
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
            }.WithClassList(s_ListViewUssClassName);

            m_ListView.itemIndexChanged += (_, _) => UpdateValueFromGroups();

            var searchFieldContainer = new VisualElement() { name = s_SearchFieldContainerName };

            searchFieldContainer.Add(m_SearchField);

            visualInput.Add(m_ListView);
            visualInput.Add(searchFieldContainer);

            m_Completer = new ConvertersCompleter(this);
            m_Completer.ItemChosen += (i) =>
            {
                var chosenGroup = m_Completer.Results[i];
                var id = chosenGroup.id;

                if (chosenGroup.id == ConvertersCompleter.CurrentEntryItemId)
                    id = m_SearchField.text;

                AddConverterGroups(id);
            };

            // Ensure this is done after the completer registers its callbacks.
            m_SearchField.RegisterValueChangedCallback((evt) => AddConverterGroups(evt.newValue));

            AddConvertersFieldStyleSheets(this);

            m_Controller = new BindingConvertersFieldController(this);
        }

        static void AddConvertersFieldStyleSheets(VisualElement element)
        {
            // Load assets.
            var mainUSS = EditorGUIUtility.Load(k_StyleSheet) as StyleSheet;
            var themeUSSPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
            var themeUSS = EditorGUIUtility.Load(themeUSSPath) as StyleSheet;

            element.styleSheets.Add(mainUSS);
            element.styleSheets.Add(themeUSS);
        }

        string GetViewItemDisplayTextFromConverterGroup(ConverterGroup group)
        {
            if (group.id == ConvertersCompleter.CurrentEntryItemId)
            {
                return string.Format(ConvertersCompleter.UseCurrentEntryMessage, m_SearchField.text);
            }
            return !string.IsNullOrEmpty(group.displayName) ? $"{group.displayName} ({group.id})" : group.id;
        }

        string GetCompletionTextFromConverterGroup(ConverterGroup group)
        {
            if (group.id == ConvertersCompleter.CurrentEntryItemId)
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
        public void SetBindingDataSourceContext(Context context)
        {
            m_AllGroups.Clear();
            m_CompatibleGroups.Clear();

            if (context.EditedElement == null || string.IsNullOrEmpty(context.BindingProperty))
                return;

            var uiProperty = PropertyContainer.GetProperty(context.EditedElement, new PropertyPath(context.BindingProperty));
            var uiPropertyType = uiProperty?.DeclaredValueType();
            var propertyPath = new PropertyPath(context.DataSourcePath);

            Type sourcePropertyType = null; // If the binding data source is specified then ignore the binding data source type.

            if (context.DataSourceObject != null)
            {
                var source = context.DataSourceObject;

                if (source is AnyObjectField.NonUnityObjectValue nonUnityObject)
                    source = nonUnityObject.data;

                if (propertyPath.IsEmpty)
                {
                    sourcePropertyType = context.DataSourceObject.GetType();
                }
                else if (PropertyContainer.TryGetProperty(source, propertyPath, out var sourceProperty))
                {
                    sourcePropertyType = sourceProperty.DeclaredValueType();
                }
            }
            else if (context.DataSourceType != null)
            {
                if (propertyPath.IsEmpty)
                    sourcePropertyType = context.DataSourceType;
                else
                {
                    using (ListPool<PropertyPathInfo>.Get(out var properties))
                    {
                        DataBindingUtility.GetPropertyPaths(context.DataSourceType, int.MaxValue, properties);

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
                if (context.ConvertersToSource)
                    DataBindingUtility.GetMatchingConverterGroups(uiPropertyType, sourcePropertyType, compatibleGroupIds);
                else
                    DataBindingUtility.GetMatchingConverterGroups(sourcePropertyType, uiPropertyType, compatibleGroupIds);
            }
            else if (uiPropertyType != null)
            {
                if (context.ConvertersToSource)
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

        public bool ContainsUnknownCompatibilityGroup()
        {
            foreach (var converterGroup in m_ValueAsConverterGroupList)
            {
                if (GetGroupCompatibility(converterGroup) == BindingCompatibilityStatus.Unknown)
                    return true;
            }
            return false;
        }
    }


    /// <summary>
    /// Controller used to update the context of a BindingConvertersField, which updates its value and completion.
    /// </summary>
    class BindingConvertersFieldController
    {
        static readonly string k_BindingProperty = nameof(DataBinding.property);
        static readonly string k_BindingMode = nameof(DataBinding.bindingMode);
        static readonly string k_DataSource = nameof(DataBinding.dataSourceUnityObject);
        static readonly string k_DataSourceType = nameof(DataBinding.dataSourceType);
        static readonly string k_DataSourcePathString = nameof(DataBinding.dataSourcePathString);
        static readonly string k_UiToSourceConvertersString = nameof(DataBinding.uiToSourceConvertersString);
        public static readonly string BindingWindowLocalConverterNotApplicableMessage = L10n.Tr("It is not applicable for the specified binding mode");

        bool m_Configured;

        private VisualElement m_EditedElement;
        PropertyField m_LastParentPropertyField;

        public BindingConvertersField Field { get; }

        public VisualElement EditedElement
        {
            get => m_EditedElement;
            set
            {
                if (m_EditedElement == value)
                    return;

                UnconfigureField();
                m_EditedElement = value;
                ConfigureField();
            }
        }

        private SerializedProperty GetBoundSerializedProperty()
        {
            return m_LastParentPropertyField?.serializedProperty;
        }

        private SerializedProperty GetDataBindingProperty()
        {
            var boundProperty = GetBoundSerializedProperty();

            if (boundProperty != null && boundProperty.isValid)
            {
                var parent = boundProperty.Copy();

                if (parent != null)
                {
                    if (parent.Parent())
                    {
                        return parent;
                    }
                }
            }
            return null;
        }

        private bool ConvertersToSource
        {
            get
            {
                var boundProperty = GetBoundSerializedProperty();

                if (boundProperty == null)
                    return false;
                return boundProperty.name == k_UiToSourceConvertersString;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="field">The field controlled by the controller</param>
        public BindingConvertersFieldController(BindingConvertersField field)
        {
            Field = field;
            Field.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            Field.RegisterCallback<DetachFromPanelEvent>(DetachAttachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            m_LastParentPropertyField = Field.GetFirstAncestorOfType<PropertyField>();

            if (m_LastParentPropertyField != null)
            {
                ConfigureField();
            }
        }

        void DetachAttachFromPanel(DetachFromPanelEvent e)
        {
            UnconfigureField();
            m_LastParentPropertyField = null;
        }

        void ConfigureField()
        {
            if (m_Configured || m_EditedElement == null)
                return;

            var dataBindingProperty = GetDataBindingProperty();

            if (dataBindingProperty == null)
            {
                return;
            }

            var bindingModeProperty = dataBindingProperty.FindPropertyRelative(k_BindingMode);
            var dataSourceProperty = dataBindingProperty.FindPropertyRelative(k_DataSource);
            var dataSourcePathStringProperty = dataBindingProperty.FindPropertyRelative(k_DataSourcePathString);
            var dataSourceTypeProperty = dataBindingProperty.FindPropertyRelative(k_DataSourceType);

            Field.TrackPropertyValue(bindingModeProperty, OnTrackedBindingMode);
            Field.TrackPropertyValue(dataSourceProperty, OnTrackedDataSource);
            Field.TrackPropertyValue(dataSourceTypeProperty, OnTrackedDataSource);
            Field.TrackPropertyValue(dataSourcePathStringProperty, OnTrackedDataSource);
            m_Configured = true;

            UpdateConvertersContext();
            UpdateField();
        }

        void UnconfigureField()
        {
            if (!m_Configured)
                return;

            m_Configured = false;

            var dataBindingProperty = GetDataBindingProperty();

            if (dataBindingProperty == null)
            {
                return;
            }

            var bindingModeProperty = dataBindingProperty.FindPropertyRelative(k_BindingMode);
            var dataSourceProperty = dataBindingProperty.FindPropertyRelative(k_DataSource);
            var dataSourcePathStringProperty = dataBindingProperty.FindPropertyRelative(k_DataSourcePathString);
            var dataSourceTypeProperty = dataBindingProperty.FindPropertyRelative(k_DataSourceType);

            Field.UntrackPropertyValue(bindingModeProperty, OnTrackedBindingMode);
            Field.UntrackPropertyValue(dataSourceProperty, OnTrackedDataSource);
            Field.UntrackPropertyValue(dataSourceTypeProperty, OnTrackedDataSource);
            Field.UntrackPropertyValue(dataSourcePathStringProperty, OnTrackedDataSource);
        }

        private void OnTrackedBindingMode(object obj, SerializedProperty property)
        {
            UpdateField();
        }

        private void OnTrackedDataSource(object obj, SerializedProperty property)
        {
            UpdateConvertersContext();
        }

        /// <summary>
        /// Updates the context
        /// </summary>
        private void UpdateConvertersContext()
        {
            if (!m_Configured)
                return;

            var dataBindingProperty = GetDataBindingProperty();

            if (dataBindingProperty == null)
                return;

            var bindingProperty = dataBindingProperty.FindPropertyRelative(k_BindingProperty)?.stringValue;
            var dataSourceObject = GetDataSourceObjectValue(dataBindingProperty) ??
                                   GetInheritedDataSourceObject(dataBindingProperty);
            var dataSourceType = GetDataSourceTypeValue(dataBindingProperty) ?? GetInheritedDataSourceType(dataBindingProperty);
            var dataSourcePath = GetDataSourcePathValue(dataBindingProperty);

            var context = new BindingConvertersField.Context(EditedElement,
                bindingProperty,
                dataSourceObject,
                dataSourceType,
                dataSourcePath,
                ConvertersToSource
                );

            Field.SetBindingDataSourceContext(context);
        }

        private void UpdateField()
        {
            var dataBindingProperty = GetDataBindingProperty();
            var bindingModeProperty = dataBindingProperty.FindPropertyRelative(k_BindingMode);
            var bindingMode = (BindingMode)bindingModeProperty.enumValueIndex;

            if (ConvertersToSource)
            {
                Field.SetEnabled(bindingMode is BindingMode.TwoWay or BindingMode.ToSource);
                Field.tooltip = !Field.enabledSelf ? BindingWindowLocalConverterNotApplicableMessage : "";
            }
            else
            {
                Field.SetEnabled(bindingMode != BindingMode.ToSource);
                Field.tooltip = !Field.enabledSelf ? BindingWindowLocalConverterNotApplicableMessage : "";
            }
        }

        /// <summary>
        /// Gets the value of data source object field.
        /// </summary>
        public Object GetDataSourceObjectValue(SerializedProperty dataBindingProperty)
        {
            var dataSourceProperty = dataBindingProperty.FindPropertyRelative(k_DataSource);
            return dataSourceProperty?.objectReferenceValue;
        }

        /// <summary>
        /// Gets the value of the data source type field.
        /// </summary>
        public Type GetDataSourceTypeValue(SerializedProperty dataBindingProperty)
        {
            var dataSourceTypeProperty = dataBindingProperty.FindPropertyRelative(k_DataSourceType);
            var fullTypeName = dataSourceTypeProperty?.stringValue;

            return !string.IsNullOrEmpty(fullTypeName) ? Type.GetType(fullTypeName) : null;
        }

        /// <summary>
        /// Gets The value of the data source path field.
        /// </summary>
        public string GetDataSourcePathValue(SerializedProperty dataBindingProperty)
        {
            var dataSourcePathProperty = dataBindingProperty.FindPropertyRelative(k_DataSourcePathString);
            return dataSourcePathProperty?.stringValue;
        }

        /// <summary>
        /// Gets data source inherited from the selected VisualElement.
        /// </summary>
        public object GetInheritedDataSourceObject(SerializedProperty dataBindingProperty)
        {
            if (m_EditedElement == null)
                return null;

            DataBindingUtility.TryGetRelativeDataSourceFromHierarchy(m_EditedElement, out var source);
            return source;
        }

        /// <summary>
        /// Gets data source type inherited from the selected VisualElement.
        /// </summary>
        public Type GetInheritedDataSourceType(SerializedProperty dataBindingProperty)
        {
            if (m_EditedElement == null)
                return null;

            DataBindingUtility.TryGetRelativeDataSourceTypeFromHierarchy(m_EditedElement, out var sourceType);
            return sourceType;
        }
    }
}
