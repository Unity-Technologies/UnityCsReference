// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor.Search;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>e
    /// Provides completion to the text field used to edit the binding path property of a VisualElement or a Binding.
    /// </summary>
    class BuilderDataSourcePathCompleter : FieldSearchCompleter<PropertyPathInfo>
    {
        private List<Type> m_ToUICompatibleTypes;
        private Dictionary<Type, List<Type>> m_ToSrcCompatibleTypesMap;
        private List<PropertyPathInfo> m_CompatibleProperties = new ();
        private bool m_ShowForDataSource = true;
        private List<PropertyPathInfo> m_AllProperties = new ();
        private DataBinding m_Binding;

        private ShowOnlyCompatibleResultsToggle m_ShowOnlyCompatibleResultsToggle;

        private BuilderPropertyPathInfoView m_DetailsView;

        /// <summary>
        /// Gets and sets the value that indicates whether the completer should only list out converter groups compatible with the types of the bound properties.
        /// </summary>
        public bool showsOnlyCompatibleResults
        {
            get => m_ShowOnlyCompatibleResultsToggle?.toggle.value ?? false;
            set => m_ShowOnlyCompatibleResultsToggle.toggle.value = value;
        }

        /// <summary>
        /// The visual element that owns the binding path property or that owns the binding that owns the attribute.
        /// </summary>
        public VisualElement element { get; set; }

        /// <summary>
        /// The binding that owns the binding path attribute.
        /// </summary>
        public DataBinding binding
        {
            get => m_Binding;
            set => m_Binding = value;
        }

        /// <summary>
        /// The binding data source.
        /// </summary>
        public object bindingDataSource { get; set; }

        /// <summary>
        /// The type of the binding data source.
        /// </summary>
        public Type bindingDataSourceType { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="field">The attached text field used to edit the binding path property.</param>
        public BuilderDataSourcePathCompleter(TextField field)
        {
            alwaysVisible = true;
            SetupCompleterField(field, true);

            makeItem = () => new BuilderPropertyPathInfoViewItem();
            bindItem = (e, i) =>
            {
                var item = e as BuilderPropertyPathInfoViewItem;
                var res = results[i];
                var path = res.propertyPath.ToString();

                // If the path contains indices then replace them by 'index'
                item.propertyName = path.Replace("[0]", "[index]");
                item.propertyType = res.type;
            };
            getTextFromDataCallback = (data) => data.propertyPath.ToString();
            dataSourceCallback = () => binding != null && showsOnlyCompatibleResults ? m_CompatibleProperties : m_AllProperties;
            matcherCallback = Matcher;

            // Set up the detail view that shows information about the selected or hovered property
            hoveredItemChanged += propertyInfo =>
            {
                // If no item is hovered over then fallback to the selected item
                if (propertyInfo.propertyPath.IsEmpty)
                    propertyInfo = selectedData;
                ShowPropertyDetails(propertyInfo);
            };

            selectionChanged += ShowPropertyDetails;
        }

        /// <summary>
        /// Updates the completer results
        /// </summary>
        public void UpdateResults(bool showForDataSource)
        {
            enabled = element != null && (bindingDataSource != null || bindingDataSourceType != null);
            UpdatePropertyList(showForDataSource);
        }

        protected override VisualElement MakeDetailsContent()
        {
            m_DetailsView = new BuilderPropertyPathInfoView();
            m_DetailsView.style.display = DisplayStyle.None;
            return m_DetailsView;
        }

        protected override VisualElement MakeFooterContent()
        {
            if (m_Binding == null)
                return null;

            m_ShowOnlyCompatibleResultsToggle = new ShowOnlyCompatibleResultsToggle();
            m_ShowOnlyCompatibleResultsToggle.toggle.RegisterValueChangedCallback((_) =>
            {
                UpdateResults(m_ShowForDataSource);
                Refresh();
            });

            return m_ShowOnlyCompatibleResultsToggle;
        }

        void ShowPropertyDetails(PropertyPathInfo propertyInfo)
        {
            if (string.IsNullOrEmpty(propertyInfo.propertyPath.ToString()))
            {
                m_DetailsView.style.display = DisplayStyle.None;
            }
            else
            {
                IProperty property = null;
                var source = bindingDataSource;

                if (bindingDataSource != null)
                {
                    if (bindingDataSource is BuilderObjectField.NonUnityObjectValue nonuUnityObjVale)
                        source = nonuUnityObjVale.data;
                    PropertyContainer.TryGetProperty(source, propertyInfo.propertyPath, out property);
                }

                m_DetailsView.SetInfo(source, propertyInfo, property);
                m_DetailsView.style.display = DisplayStyle.Flex;
            }
        }

        protected override FieldSearchCompleterPopup CreatePopup()
        {
            var popup = base.CreatePopup();
            var sheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(
                BuilderConstants.UIBuilderPackagePath + "/Inspector/BuilderDataSourcePathCompleterPopup.uss");

            popup.styleSheets.Add(sheet);
            return popup;
        }

        void UpdatePropertyList(bool showForDataSource)
        {
            m_ShowForDataSource = showForDataSource;
            m_AllProperties.Clear();
            m_CompatibleProperties.Clear();
            m_ToUICompatibleTypes?.Clear();
            m_ToSrcCompatibleTypesMap?.Clear();

            if (enabled)
            {
                // If the binding data source is specified then ignore the binding data source type.
                if (m_ShowForDataSource)
                {
                    if (bindingDataSource != null)
                    {
                        var source = bindingDataSource;

                        if (source is BuilderObjectField.NonUnityObjectValue nonUnityObject)
                            source = nonUnityObject.data;
                        DataBindingUtility.GetPropertyPaths(source, int.MaxValue, m_AllProperties);
                    }
                }
                else
                {
                    DataBindingUtility.GetPropertyPaths(bindingDataSourceType, int.MaxValue, m_AllProperties);
                }

                // If it is the binding path property of a binding then filter out the compatible properties
                if (binding != null)
                {
                    var property = PropertyContainer.GetProperty(element, new PropertyPath(binding.property));
                    var compatibleType = property?.DeclaredValueType();

                    m_ToUICompatibleTypes ??= new List<Type>();
                    m_ToSrcCompatibleTypesMap ??= new Dictionary<Type, List<Type>>();

                    if (binding.bindingMode is BindingMode.TwoWay or BindingMode.ToTarget or BindingMode.ToTargetOnce)
                        DataBindingUtility.GetAllConversionsFromSourceToUI(binding, compatibleType, m_ToUICompatibleTypes);

                    foreach (var prop in m_AllProperties)
                    {
                        bool canConvertToUi = compatibleType == typeof(string) || IsCompatibleType(m_ToUICompatibleTypes, prop.type);
                        bool canConvertToSrc = false;

                        if (binding.bindingMode is BindingMode.TwoWay or BindingMode.ToSource)
                        {
                            if (!m_ToSrcCompatibleTypesMap.TryGetValue(prop.type, out var toSrcCompatibleTypes))
                            {
                                toSrcCompatibleTypes = new List<Type>();
                                DataBindingUtility.GetAllConversionsFromUIToSource(binding, prop.type, toSrcCompatibleTypes);
                                m_ToSrcCompatibleTypesMap[prop.type] = toSrcCompatibleTypes;
                            }

                            canConvertToSrc = (prop.type == typeof(string)) || IsCompatibleType(toSrcCompatibleTypes, compatibleType);
                        }

                        bool isCompatible = false;

                        // If the bindingMode is BindingMode.Bidirectional then check whether we can convert in both directions (ui to source and source to ui)
                        if (binding.bindingMode == BindingMode.TwoWay && canConvertToUi && canConvertToSrc)
                            isCompatible = true;
                        // If the bindingMode is BindingMode.ToSource then check whether we can convert ui property to source
                        else if (binding.bindingMode == BindingMode.ToSource && canConvertToSrc)
                            isCompatible = true;
                        // If the bindingMode is BindingMode.ToTarget or BindingMode.OnceToTarget then check whether we can convert source property to ui
                        else if ((binding.bindingMode is BindingMode.ToTarget or BindingMode.ToTargetOnce) && canConvertToUi)
                            isCompatible = true;

                        if (isCompatible)
                        {
                            m_CompatibleProperties.Add(prop);
                        }
                    }
                }
            }

            m_ShowOnlyCompatibleResultsToggle?.SetCompatibleResultCount(m_CompatibleProperties.Count, m_AllProperties.Count);
        }

        bool IsCompatibleType(List<Type> compatibleTypes, Type type)
        {
            foreach (var t in compatibleTypes)
            {
                if (t.IsAssignableFrom(type))
                {
                    return true;
                }
            }

            return false;
        }

        bool Matcher(string filter, PropertyPathInfo data)
        {
            var text = GetTextFromData(data);

            // Remove all indices before comparing
            text = DataBindingUtility.ReplaceAllIndicesInPath(text, null);
            filter = DataBindingUtility.ReplaceAllIndicesInPath(filter, null);
            return !string.IsNullOrEmpty(text) && FuzzySearch.FuzzyMatch(filter, text);
        }
    }
}
