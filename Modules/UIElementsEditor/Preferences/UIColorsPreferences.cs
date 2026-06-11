// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements;

class UIColorsPreferences(IEnumerable<string> keywords = null)
    : SettingsProvider("Preferences/Colors/UI Authoring", SettingsScope.User, keywords)
{
    [SettingsProvider]
    internal static SettingsProvider CreateUIColorsPreferences()
    {
        return Unsupported.IsSourceBuild()
            ? new UIColorsPreferences()
            : null;
    }

    StyleSheet m_PreviewStyleSheet;
    SyntaxHighlightConfiguration m_UssSyntaxHighlightConfiguration;

    VisualTreeAsset m_PreviewVisualTreeAsset;
    SyntaxHighlightConfiguration m_UxmlSyntaxHighlightConfiguration;

    const string k_VisualTreeAsset = "Settings/UIColorsPreferences.uxml";

    const string k_SyntaxHighlightStyleSheetPreview = "Settings/StyleSheetSyntaxHighlightingPreview.uss";
    const string k_SyntaxHighlightVisualTreeAssetPreview = "Settings/VisualTreeAssetSyntaxHighlightingPreview.uxml";

    public override void OnActivate(string searchContext, VisualElement rootElement)
    {
        base.OnActivate(searchContext, rootElement);

        var colorPreferences = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        colorPreferences.CloneTree(rootElement);

        SetupSyntaxHighlightSection(rootElement);
        SetupUIViewportSection(rootElement);
    }

    void SetupSyntaxHighlightSection(VisualElement rootElement)
    {
        var uxmlTab = rootElement.Q<Tab>("UXML");
        uxmlTab.iconImage = Background.FromObject(EditorGUIUtility.Load("VisualTreeAsset Icon"));
        m_UxmlSyntaxHighlightConfiguration = uxmlTab.Q<SyntaxHighlightConfiguration>();
        m_UxmlSyntaxHighlightConfiguration.ColorPrefix = VisualTreeAssetExporter.ColorsPreferenceCategory;
        m_PreviewVisualTreeAsset = EditorGUIUtility.Load(k_SyntaxHighlightVisualTreeAssetPreview) as VisualTreeAsset;
        m_UxmlSyntaxHighlightConfiguration.PreviewText = GetVisualTreeAssetPreview();
        m_UxmlSyntaxHighlightConfiguration.RegisterCallback<ChangeEvent<Color>>(UpdateVisualTreeAssetPreview);

        var ussTab = rootElement.Q<Tab>("USS");
        ussTab.iconImage = Background.FromObject(EditorGUIUtility.Load("StyleSheet Icon"));
        m_UssSyntaxHighlightConfiguration = ussTab.Q<SyntaxHighlightConfiguration>();
        m_UssSyntaxHighlightConfiguration.ColorPrefix = StyleSheetExporter.ColorsPreferenceCategory;
        m_PreviewStyleSheet = EditorGUIUtility.Load(k_SyntaxHighlightStyleSheetPreview) as StyleSheet;
        m_UssSyntaxHighlightConfiguration.PreviewText = GetStyleSheetPreview();
        m_UssSyntaxHighlightConfiguration.RegisterCallback<ChangeEvent<Color>>(UpdateStyleSheetPreview);
    }

    void UpdateStyleSheetPreview(ChangeEvent<Color> evt)
    {
        m_UssSyntaxHighlightConfiguration.PreviewText = GetStyleSheetPreview();
    }

    string GetStyleSheetPreview()
    {
        var options = StyleSheetExporter.UssExportOptions.Default;
        options.useColorHighlighting = true;
        return StyleSheetExporter.Default.ToUssString(m_PreviewStyleSheet, options);
    }

    void UpdateVisualTreeAssetPreview(ChangeEvent<Color> evt)
    {
        m_UxmlSyntaxHighlightConfiguration.PreviewText = GetVisualTreeAssetPreview();
    }

    string GetVisualTreeAssetPreview()
    {
        var options = VisualTreeAssetExporter.ExportOptions.Default;
        options.useColorHighlighting = true;
        return VisualTreeAssetExporter.Default.ToUxmlString(m_PreviewVisualTreeAsset, options);
    }

    void SetupUIViewportSection(VisualElement rootElement)
    {
    }
}
