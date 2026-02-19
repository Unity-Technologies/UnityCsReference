// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

internal sealed class StyleSheetInspector : VisualElement
{
    [Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        /// <summary>
        /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>. You should not need to call it.
        /// </summary>
        [Conditional("UNITY_EDITOR"), RegisterUxmlCache]
        public new static void Register()
        {
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
        }

        public override object CreateInstance()
        {
            return new StyleSheetInspector();
        }
    }

    public static readonly BindingId StyleSheetProperty = nameof(StyleSheet);

    private NewSelectorField m_NewSelectorField;
    private ListView m_ImportsListView;
    private readonly StyleSheetHeader m_Header;

    public const string UssClass = "unity-stylesheet-inspector";
    public const string HeaderUssClass = "unity-stylesheet-inspector__header";

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

            m_Header.StyleSheet = StyleSheet;

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

        m_Header = this.Q<StyleSheetHeader>(className:HeaderUssClass);

        m_ImportsListView = this.Q<ListView>("imports-list-view");
        ConfigureImportsListView();
    }

    void OnCreateNewSelector(NewSelectorSubmitEvent evt)
    {
        // TODO: use command when UI-4401 lands
    }

    void ConfigureImportsListView()
    {
        if (m_ImportsListView == null)
            return;

        m_ImportsListView.onAdd = OnAddImport;
        m_ImportsListView.onRemove = OnRemoveImport;
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
    }

    void OnAddImport(BaseListView listView)
    {
        if (m_StyleSheet == null)
            return;

        m_StyleSheet.AddImportAtIndex(-1, new StyleSheet.ImportStruct());
        listView.itemsSource = m_StyleSheet.imports;
        listView.RefreshItems();
    }

    void OnRemoveImport(BaseListView listView)
    {
        if (m_StyleSheet == null)
            return;

        int index = listView.selectedIndex;
        if (index < 0 || index >= m_StyleSheet.imports.Length)
            index = m_StyleSheet.imports.Length - 1;

        m_StyleSheet.RemoveImport(index);
        listView.itemsSource = m_StyleSheet.imports;
        listView.RefreshItems();
    }

    void OnImportChanged(ChangeEvent<Object> evt)
    {
        if (m_StyleSheet == null)
            return;

        var field = (ObjectField)evt.target;
        int index = (int)field.userData;

        if (evt.newValue is StyleSheet sheet)
            m_StyleSheet.SetStyleSheetImportAtIndex(index, sheet);
    }
}
