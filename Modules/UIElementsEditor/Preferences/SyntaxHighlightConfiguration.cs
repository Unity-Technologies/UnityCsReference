// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements;

[UxmlElement]
partial class SyntaxHighlightConfiguration : VisualElement
{
    const string k_VisualTreeAsset = "Settings/SyntaxHighlightConfiguration.uxml";
    const string k_DarkStyleSheet = "Settings/SyntaxHighlightConfigurationDark.uss";
    const string k_LightStyleSheet = "Settings/SyntaxHighlightConfigurationLight.uss";

    public const string UssClass = "syntax-highlight-configuration";
    public const string ColorSectionUssClass = UssClass + "__colors-section";
    public const string PreviewContainerUssClass = UssClass + "__preview-container";
    public const string ResetToDefaultUssClass = UssClass + "__reset-to-default";

    UIPrefColorSection m_Colors;

    TextField m_PreviewField;

    public string ColorPrefix
    {
        get => m_Colors.ColorPrefix;
        set => m_Colors.ColorPrefix = value;
    }

    public string PreviewText
    {
        get => m_PreviewField.text;
        set => m_PreviewField.text = value;
    }

    public SyntaxHighlightConfiguration()
    {
        AddToClassList(UssClass);
        var syntaxHighlightSection = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        syntaxHighlightSection.CloneTree(this);
        m_Colors = this.Q<UIPrefColorSection>(className: ColorSectionUssClass);

        styleSheets.Add(EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? k_DarkStyleSheet : k_LightStyleSheet) as StyleSheet);

        m_PreviewField = this.Q<TextField>(className: PreviewContainerUssClass);
        m_PreviewField.textInputBase.textElement.enableRichText = true;
    }
}
