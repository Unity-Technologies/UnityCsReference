// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Provides completion to the text field used to edit the binding path property of a VisualElement or a Binding.
/// </summary>
class DataSourcePathCompleter : FieldSearchCompleter<PropertyPathInfo>
{
    private const string k_PopupStyleSheet = "UIToolkitAuthoring/Inspector/Binding/DataSourcePathCompleterPopup.uss";
    private const string k_PopupStyleSheetDark = "UIToolkitAuthoring/Inspector/Binding/DataSourcePathCompleterPopupDark.uss";
    private const string k_PopupStyleSheetLight = "UIToolkitAuthoring/Inspector/Binding/DataSourcePathCompleterPopupLight.uss";

    private List<Type> m_ToUICompatibleTypes;
    private Dictionary<Type, List<Type>> m_ToSourceCompatibleTypesMap;
    private List<PropertyPathInfo> m_CompatibleProperties = new();
    private bool m_ShowForDataSource = true;
    private List<PropertyPathInfo> m_AllProperties = new();
    private string m_Prefix = string.Empty;
    const int k_MaxPaths = 10000;
    const int k_MaxDepth = 64;
    private DataBinding m_Binding;

    private ShowOnlyCompatibleResultsToggle m_ShowOnlyCompatibleResultsToggle;

    private PropertyPathInfoDetailsView m_DetailsView;

    bool m_ShowsOnlyCompatibleResults = true;

    /// <summary>
    /// Gets and sets the value that indicates whether the completer should only list out converter groups compatible with the types of the bound properties.
    /// </summary>
    public bool ShowsOnlyCompatibleResults
    {
        get => m_ShowsOnlyCompatibleResults;
        set
        {
            if (m_ShowsOnlyCompatibleResults == value)
                return;

            m_ShowsOnlyCompatibleResults = value;
            m_ShowOnlyCompatibleResultsToggle?.toggle.SetValueWithoutNotify(value);
            UpdateResults(m_ShowForDataSource);
            Refresh();
        }
    }

    /// <summary>
    /// The visual element that owns the binding path property or that owns the binding that owns the attribute.
    /// </summary>
    public VisualElement Element { get; set; }

    /// <summary>
    /// The binding that owns the binding path attribute.
    /// </summary>
    public DataBinding Binding
    {
        get => m_Binding;
        set => m_Binding = value;
    }

    /// <summary>
    /// The binding data source.
    /// </summary>
    public object BindingDataSourceObject { get; set; }

    /// <summary>
    /// The type of the binding data source.
    /// </summary>
    public Type BindingDataSourceType { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="field">The attached text field used to edit the binding path property.</param>
    public DataSourcePathCompleter(TextField field)
    {
        AlwaysVisible = true;
        SetupCompleterField(field, true);

        MakeItem = () => new PropertyPathInfoViewItem();
        BindItem = (e, i) =>
        {
            var item = e as PropertyPathInfoViewItem;
            var res = Results[i];
            var path = res.propertyPath.ToString();

            // If the path contains indices then replace them by 'index'
            item.PropertyPath = path.Replace("[0]", "[index]");
            item.PropertyType = res.type;
        };
        GetTextFromDataCallback = (data) => data.propertyPath.ToString();
        DataSourceCallback = () =>
            Binding != null && ShowsOnlyCompatibleResults ? m_CompatibleProperties : m_AllProperties;
        MatcherCallback = Matcher;

        // Set up the detail view that shows information about the selected or hovered property
        HoveredItemChanged += propertyInfo =>
        {
            // If no item is hovered over then fallback to the selected item
            if (propertyInfo.propertyPath.IsEmpty)
                propertyInfo = SelectedData;
            ShowPropertyDetails(propertyInfo);
        };

        SelectionChanged += ShowPropertyDetails;
    }

    /// <summary>
    /// Updates the completer results
    /// </summary>
    public void UpdateResults(bool showForDataSource)
    {
        Enabled = Element != null && (BindingDataSourceObject != null || BindingDataSourceType != null);
        UpdatePropertyList(showForDataSource);
    }

    protected override VisualElement MakeDetailsContent()
    {
        m_DetailsView = new PropertyPathInfoDetailsView();
        return m_DetailsView;
    }

    protected override VisualElement MakeFooterContent()
    {
        if (m_Binding == null)
            return null;

        m_ShowOnlyCompatibleResultsToggle = new ShowOnlyCompatibleResultsToggle();
        m_ShowOnlyCompatibleResultsToggle.toggle.value = ShowsOnlyCompatibleResults;
        m_ShowOnlyCompatibleResultsToggle.toggle.RegisterValueChangedCallback((evt) =>
        {
            ShowsOnlyCompatibleResults = evt.newValue;
        });

        return m_ShowOnlyCompatibleResultsToggle;
    }

    void ShowPropertyDetails(PropertyPathInfo propertyInfo)
    {
        if (m_DetailsView == null)
            return;

        IProperty property = null;
        var source = BindingDataSourceObject;

        if (source != null)
        {
            if (source is AnyObjectField.NonUnityObjectValue nonUnityObjVal)
                source = nonUnityObjVal.data;
            if (!propertyInfo.propertyPath.IsEmpty)
                PropertyContainer.TryGetProperty(source, propertyInfo.propertyPath, out property);
        }

        m_DetailsView.SetInfo(source, propertyInfo, property);
    }

    protected override FieldSearchCompleterPopup CreatePopup()
    {
        var popup = base.CreatePopup();
        var sheet = EditorGUIUtility.Load(k_PopupStyleSheet) as StyleSheet;
        var themeSheet = EditorGUIUtility.isProSkin
            ? EditorGUIUtility.Load(k_PopupStyleSheetDark) as StyleSheet
            : EditorGUIUtility.Load(k_PopupStyleSheetLight) as StyleSheet;

        popup.styleSheets.Add(sheet);
        popup.styleSheets.Add(themeSheet);
        return popup;
    }

    void UpdatePropertyList(bool showForDataSource)
    {
        m_ShowForDataSource = showForDataSource;
        m_Prefix = string.Empty;
        RebuildPropertyList();
    }

    protected override void OnFilterTextChanged(string text) => UpdateForText(text);

    internal void UpdateForText(string text)
    {
        if (!Enabled)
            return;

        var prefix = GetPrefix(text);
        if (prefix == m_Prefix)
            return;

        m_Prefix = prefix;
        RebuildPropertyList();
    }

    static string GetPrefix(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var index = text.LastIndexOf('.');
        return index > 0 ? text.Substring(0, index) : string.Empty;
    }

    bool TryNarrowToPrefix(ref object source, ref Type type, out PropertyPath prefixPath)
    {
        prefixPath = default;

        try
        {
            var result = source != null ? DataBindingUtility.IsPathValid(source, m_Prefix) : DataBindingUtility.IsPathValid(type, m_Prefix);
            if (result.returnCode != VisitReturnCode.Ok || result.type == null)
                return false;

            var path = new PropertyPath(m_Prefix);
            object value = null;
            if (source != null)
            {
                var container = source;
                PropertyContainer.TryGetValue(ref container, path, out value);
            }

            if (typeof(IList).IsAssignableFrom(value?.GetType() ?? result.type))
                return false;

            source = value;
            type = value == null ? result.type : null;
            prefixPath = path;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    void CollectPaths()
    {
        object source = null;
        Type type = null;

        if (m_ShowForDataSource)
        {
            source = BindingDataSourceObject;
            if (source is AnyObjectField.NonUnityObjectValue nonUnityObject)
                source = nonUnityObject.data;
            if (source == null)
                return;
        }
        else
        {
            type = BindingDataSourceType;
        }

        var prefixPath = default(PropertyPath);
        if (m_Prefix.Length > 0)
            TryNarrowToPrefix(ref source, ref type, out prefixPath);

        using var pooled = ListPool<PropertyPathInfo>.Get(out var level);
        var previousCount = -1;
        for (var depth = 1; depth <= k_MaxDepth; depth++)
        {
            level.Clear();
            if (source != null)
                DataBindingUtility.GetPropertyPaths(source, depth, k_MaxPaths + 1, level);
            else
                DataBindingUtility.GetPropertyPaths(type, depth, k_MaxPaths + 1, level);

            if (level.Count > k_MaxPaths && depth > 1)
                break;

            m_AllProperties.Clear();
            m_AllProperties.AddRange(level);

            if (level.Count > k_MaxPaths)
            {
                m_AllProperties.RemoveRange(k_MaxPaths, m_AllProperties.Count - k_MaxPaths);
                break;
            }

            if (level.Count == previousCount)
                break;

            previousCount = level.Count;
        }

        if (!prefixPath.IsEmpty)
        {
            for (var i = 0; i < m_AllProperties.Count; i++)
            {
                var info = m_AllProperties[i];
                m_AllProperties[i] = new PropertyPathInfo(PropertyPath.Combine(prefixPath, info.propertyPath), info.type);
            }
        }
    }

    void RebuildPropertyList()
    {
        m_AllProperties.Clear();
        m_CompatibleProperties.Clear();
        m_ToUICompatibleTypes?.Clear();
        m_ToSourceCompatibleTypesMap?.Clear();

        if (Enabled)
        {
            CollectPaths();

            // If it is the binding path property of a binding then filter out the compatible properties
            if (Binding != null)
            {
                var property = PropertyContainer.GetProperty(Element, new PropertyPath(Binding.property));
                var compatibleType = property?.DeclaredValueType();

                m_ToUICompatibleTypes ??= new List<Type>();
                m_ToSourceCompatibleTypesMap ??= new Dictionary<Type, List<Type>>();

                var convertsToUi = Binding.bindingMode is BindingMode.TwoWay or BindingMode.ToTarget or BindingMode.ToTargetOnce;

                if (convertsToUi)
                    DataBindingUtility.GetAllConversionsFromSourceToUI(Binding, compatibleType, m_ToUICompatibleTypes);

                foreach (var prop in m_AllProperties)
                {
                    bool canConvertToUi = convertsToUi &&
                                          (compatibleType == typeof(string) ||
                                           CanConvert(prop.type, compatibleType, m_ToUICompatibleTypes));
                    bool canConvertToSrc = false;

                    if (Binding.bindingMode is BindingMode.TwoWay or BindingMode.ToSource)
                    {
                        if (!m_ToSourceCompatibleTypesMap.TryGetValue(prop.type, out var toSrcCompatibleTypes))
                        {
                            toSrcCompatibleTypes = new List<Type>();
                            DataBindingUtility.GetAllConversionsFromUIToSource(Binding, prop.type,
                                toSrcCompatibleTypes);
                            m_ToSourceCompatibleTypesMap[prop.type] = toSrcCompatibleTypes;
                        }

                        canConvertToSrc = (prop.type == typeof(string)) ||
                                          CanConvert(compatibleType, prop.type, toSrcCompatibleTypes);
                    }

                    bool isCompatible = false;

                    // If the bindingMode is BindingMode.Bidirectional then check whether we can convert in both directions (ui to source and source to ui)
                    if (Binding.bindingMode == BindingMode.TwoWay && canConvertToUi && canConvertToSrc)
                        isCompatible = true;
                    // If the bindingMode is BindingMode.ToSource then check whether we can convert ui property to source
                    else if (Binding.bindingMode == BindingMode.ToSource && canConvertToSrc)
                        isCompatible = true;
                    // If the bindingMode is BindingMode.ToTarget or BindingMode.OnceToTarget then check whether we can convert source property to ui
                    else if ((Binding.bindingMode is BindingMode.ToTarget or BindingMode.ToTargetOnce) &&
                             canConvertToUi)
                        isCompatible = true;

                    if (isCompatible)
                    {
                        m_CompatibleProperties.Add(prop);
                    }
                }
            }
        }

        m_ShowOnlyCompatibleResultsToggle?.SetCompatibleResultCount(m_CompatibleProperties.Count,
            m_AllProperties.Count);
    }

    static bool CanConvert(Type sourceType, Type destinationType, List<Type> convertibleTypes)
    {
        return destinationType.IsAssignableFrom(sourceType) || convertibleTypes.Contains(sourceType);
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
