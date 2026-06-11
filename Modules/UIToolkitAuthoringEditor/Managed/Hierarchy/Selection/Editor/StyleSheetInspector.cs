// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
internal sealed partial class StyleSheetInspector : VisualElement
{
    public static readonly BindingId StyleSheetProperty = nameof(StyleSheet);

    private NewSelectorField m_NewSelectorField;
    private ListView m_ImportsListView;

    public const string UssClass = "unity-stylesheet-inspector";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/StyleSheet/StyleSheetInspector.uxml";
    private const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/StyleSheet/StyleSheetInspectorDark.uss";
    private const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/StyleSheet/StyleSheetInspectorLight.uss";
    private const string k_UIToolkitAuthoringInspectorStyleSheetDark = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorDark.uss";
    private const string k_UIToolkitAuthoringInspectorStyleSheetLight = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorLight.uss";

    private StyleSheet m_StyleSheet;

    [CreateProperty]
    public StyleSheet StyleSheet
    {
        get => m_StyleSheet;
        set
        {
            if (m_StyleSheet == value)
                return;

            m_StyleSheet = value;

            if (m_ImportsListView != null && m_StyleSheet != null)
            {
                m_ImportsListView.itemsSource = m_StyleSheet.imports;
                m_ImportsListView.RefreshItems();
            }

            NotifyPropertyChanged(StyleSheetProperty);
        }
    }

    public StyleSheetInspector()
    {
        AddToClassList(UssClass);

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        styleSheets.Add(styleSheet);

        var authoringInspectorStyleSheetPath = EditorGUIUtility.isProSkin ? k_UIToolkitAuthoringInspectorStyleSheetDark : k_UIToolkitAuthoringInspectorStyleSheetLight;
        var authoringInspectorStyleSheet = EditorGUIUtility.Load(authoringInspectorStyleSheetPath) as StyleSheet;
        styleSheets.Add(authoringInspectorStyleSheet);

        m_NewSelectorField = this.Q<NewSelectorField>("new-selector-field");
        m_NewSelectorField.RegisterCallback<NewSelectorSubmitEvent>(OnCreateNewSelector);

        m_ImportsListView = this.Q<ListView>("imports-list-view");
        ConfigureImportsListView();
    }

    void OnCreateNewSelector(NewSelectorSubmitEvent evt)
    {
        AddStyleRuleCommand.Execute(CommandSources.Inspector, StyleSheet, evt.selectorStr);
    }

    void ConfigureImportsListView()
    {
        if (m_ImportsListView == null)
            return;

        m_ImportsListView.onAdd = OnAddImport;
        m_ImportsListView.onRemove = OnRemoveImport;
        m_ImportsListView.showBorder = true;
        m_ImportsListView.selectionType = SelectionType.Multiple;
        m_ImportsListView.makeItem = () => new ObjectField { objectType = typeof(StyleSheet) };
        m_ImportsListView.bindItem = (element, i) =>
        {
            var objectField = (ObjectField)element;
            objectField.label = $"StyleSheet {i}";
            objectField.userData = i;
            objectField.value = m_StyleSheet?.GetStyleSheetImportAtIndex(i);
            objectField.RegisterValueChangedCallback(OnImportChanged);
        };
        m_ImportsListView.unbindItem = (element, i) =>
        {
            var objectField = (ObjectField)element;
            objectField.UnregisterValueChangedCallback(OnImportChanged);
        };

        Undo.undoRedoEvent += OnUndoRedo;
    }

    void OnUndoRedo(in UndoRedoInfo undo)
    {
        // This is needed as imports is a standard array. When updating the list, we must re-assign the ItemsSource property.
        m_ImportsListView.itemsSource = m_StyleSheet.imports;
        m_ImportsListView.RefreshItems();
    }

    void OnAddImport(BaseListView listView)
    {
        if (m_StyleSheet == null)
            return;

        AddStyleSheetImportCommand.Execute(CommandSources.Inspector, m_StyleSheet);

        // This is needed as imports is a standard array. When updating the list, we must re-assign the ItemsSource property.
        listView.itemsSource = m_StyleSheet.imports;
        listView.RefreshItems();
    }

    void OnRemoveImport(BaseListView listView)
    {
        if (m_StyleSheet == null || m_StyleSheet.imports.Length == 0)
            return;

        int index = listView.selectedIndex;
        if (index < 0 || index >= m_StyleSheet.imports.Length)
            index = m_StyleSheet.imports.Length - 1;

        RemoveStyleSheetImportCommand.Execute(CommandSources.Inspector, m_StyleSheet, index);

        // This is needed as imports is a standard array. When updating the list, we must re-assign the ItemsSource property.
        listView.itemsSource = m_StyleSheet.imports;
        listView.RefreshItems();
    }

    void OnImportChanged(ChangeEvent<Object> evt)
    {
        if (m_StyleSheet == null)
            return;

        var field = (ObjectField)evt.target;
        int index = (int)field.userData;

        SetStyleSheetImportCommand.Execute(CommandSources.Inspector, m_StyleSheet, index, evt.newValue as StyleSheet);
    }
}
