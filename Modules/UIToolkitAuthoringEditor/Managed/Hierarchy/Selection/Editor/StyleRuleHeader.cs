// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
partial class StyleRuleHeader : UISelectionObjectHeader
{
    public static readonly BindingId RuleProperty = nameof(Rule);
    public static readonly BindingId RuleNameProperty = nameof(RuleName);

    public new const string UssClass = "unity-style-rule-header";
    public const string RuleNameUssClass = UssClass + "__rule-name";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/StyleRuleHeader.uxml";
    private const string k_StyleSheet = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspector.uss";
    private const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorDark.uss";
    private const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorLight.uss";

    static readonly StyleSheetNodeTypeHandler.StyleSheetEditorExporter s_Exporter = new();
    static StyleSheet s_StyleSheet;
    static StyleSheet s_ThemedStyleSheet;
    static bool s_ThemedStyleSheetIsProSkin;

    private TextField m_RuleName;
    private StyleRule m_Rule;

    public InspectorSearchField SearchField { get; }

    protected override VisualTreeAsset IdentifierDetails => EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;

    [UxmlAttribute, CreateProperty]
    public string RuleName
    {
        get => m_RuleName.value;
        set
        {
            if (string.CompareOrdinal(m_RuleName.value, value) == 0)
                return;
            m_RuleName.value = value;
            NotifyPropertyChanged(RuleNameProperty);
        }
    }

    [CreateProperty]
    public StyleRule Rule
    {
        get => m_Rule;
        set
        {
            m_Rule = value;

            m_RuleName.dataSource = Rule;

            TypeIcon = EditorGUIUtility.Load("StyleSheet Icon") as Texture2D;
            TypeName = "Rule";

            if (m_Rule == null)
            {
                RuleName = null;
                m_RuleName.value = null;
            }
            else
            {
                RuleName = s_Exporter.ToUssString(m_Rule.styleSheet, m_Rule.complexSelectors, StyleSheetNodeTypeHandler.s_ExportOptions);
            }
            NotifyPropertyChanged(RuleProperty);
        }
    }

    public StyleRuleHeader()
    {
        AddToClassList(UssClass);
        if (s_StyleSheet == null)
            s_StyleSheet = EditorGUIUtility.Load(k_StyleSheet) as StyleSheet;
        if (s_ThemedStyleSheet == null || s_ThemedStyleSheetIsProSkin != EditorGUIUtility.isProSkin)
        {
            s_ThemedStyleSheet = EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight) as StyleSheet;
            s_ThemedStyleSheetIsProSkin = EditorGUIUtility.isProSkin;
        }
        styleSheets.Add(s_StyleSheet);
        styleSheets.Add(s_ThemedStyleSheet);

        TypeIcon = EditorGUIUtility.Load("StyleSheet Icon") as Texture2D;
        TypeName = "Rule";

        m_RuleName = this.Q<TextField>(className: RuleNameUssClass);
        m_RuleName.isDelayed = true;
        m_RuleName.dataSource = this;

        m_RuleName.RegisterValueChangedCallback(OnRuleNameChanged);

        Undo.undoRedoPerformed += OnUndoRedo;
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

        SearchField = new InspectorSearchField();
        Add(SearchField);
    }

    void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    void OnUndoRedo()
    {
        // Refresh the rule name display after undo/redo
        if (m_Rule != null)
        {
            RuleName = s_Exporter.ToUssString(m_Rule.styleSheet, m_Rule.complexSelectors, StyleSheetNodeTypeHandler.s_ExportOptions);
        }
    }

    void OnRuleNameChanged(ChangeEvent<string> evt)
    {
        if (Rule == null || string.CompareOrdinal(evt.previousValue, evt.newValue) == 0)
            return;

        if (string.IsNullOrEmpty(evt.newValue))
        {
            m_RuleName.SetValueWithoutNotify(evt.previousValue);
            return;
        }

        var selectorStrings = evt.newValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var selectorString in selectorStrings)
        {
            if (StyleSheetExtensions.ValidateSelector(selectorString, out var error))
                continue;

            Debug.LogError( $"Invalid selector string '{selectorString}': {error}.");

            // Revert to previous valid value
            m_RuleName.SetValueWithoutNotify(evt.previousValue);
            return;
        }

        RenameStyleRuleCommand.Execute(CommandSources.Inspector, selectorStrings, Rule);
    }
}
