// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal class VariableInfoView : VisualElement
{
    static readonly string s_UssClassName = "unity-ui-inspector__varinfo-view";
    static readonly string s_ValueAndPreviewUssClassName = s_UssClassName + "__value-preview-container";
    static readonly string s_PreviewThumbnailUssClassName = s_ValueAndPreviewUssClassName + "--thumbnail";
    static readonly string s_EmptyText = "None";

    const string k_UxmlTemplatePath = "UIToolkitAuthoring/Inspector/Controls/VariableInfoView.uxml";
    const string k_StyleSheet = "UIToolkitAuthoring/Inspector/Controls/VariableEditing.uss";
    const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/Controls/VariableEditingDark.uss";
    const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/Controls/VariableEditingLight.uss";

    Label m_NameLabel;
    Label m_ValueLabel;
    Label m_StyleSheetLabel;
    Label m_DescriptionLabel;
    VisualElement m_DescriptionContainer;
    VisualElement m_ValueAndPreviewContainer;
    VisualElement m_Preview;
    Image m_Thumbnail;

    public string VariableName
    {
        get => m_NameLabel.text;
        set => m_NameLabel.text = value;
    }

    public string VariableValue
    {
        get => m_ValueLabel.text;
        set
        {
            m_ValueLabel.text = value;
            m_ValueLabel.EnableInClassList(VariableEditingHandler.HiddenStyleClassName, string.IsNullOrEmpty(value));
        }
    }

    public string SourceStyleSheet
    {
        get => m_StyleSheetLabel.text;
        set => m_StyleSheetLabel.text = value;
    }

    public string Description
    {
        get => m_DescriptionLabel.text;
        set
        {
            m_DescriptionLabel.text = value;
            m_DescriptionContainer.EnableInClassList(VariableEditingHandler.HiddenStyleClassName, string.IsNullOrEmpty(value));
        }
    }

    public VariableInfoView()
    {
        AddToClassList(s_UssClassName);

        styleSheets.Add(EditorGUIUtility.Load(k_StyleSheet) as StyleSheet);
        var themeUssPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        styleSheets.Add(EditorGUIUtility.Load(themeUssPath) as StyleSheet);

        var template = EditorGUIUtility.Load(k_UxmlTemplatePath) as VisualTreeAsset;
        template.CloneTree(this);

        m_NameLabel = this.Q<Label>("name-label");
        m_ValueLabel = this.Q<Label>("value-label");
        m_StyleSheetLabel = this.Q<Label>("stylesheet-label");
        m_DescriptionLabel = this.Q<Label>("description-label");
        m_DescriptionContainer = this.Q("description-container");
        m_ValueAndPreviewContainer = this.Q("value-preview-container");
        m_Preview = this.Q("preview");
        m_Thumbnail = this.Q<Image>("thumbnail");

        m_NameLabel.displayTooltipWhenElided = true;
        m_ValueLabel.displayTooltipWhenElided = true;
        m_StyleSheetLabel.displayTooltipWhenElided = true;

        ClearUI();
    }

    void ClearUI()
    {
        VariableName = s_EmptyText;
        VariableValue = s_EmptyText;
        SourceStyleSheet = s_EmptyText;
        Description = null;
        m_Preview.AddToClassList(VariableEditingHandler.HiddenStyleClassName);
        m_Preview.style.backgroundColor = Color.clear;
        m_ValueAndPreviewContainer.RemoveFromClassList(s_PreviewThumbnailUssClassName);
        m_Thumbnail.image = null;
        m_Thumbnail.vectorImage = null;
    }

    public void SetInfo(in VariableInfo info)
    {
        ClearUI();

        if (!info.IsValid())
            return;

        if (info.Sheet)
        {
            var varStyleSheetOrigin = info.Sheet;
            var fullPath = AssetDatabase.GetAssetPath(varStyleSheetOrigin);
            string displayPath;

            if (string.IsNullOrEmpty(fullPath))
            {
                displayPath = varStyleSheetOrigin.name;
            }
            else
            {
                displayPath = fullPath == "Library/unity editor resources"
                    ? varStyleSheetOrigin.name
                    : Path.GetFileName(fullPath);
            }

            var valueText = StyleSheetExporter.GetStyleVariableValueString(
                info.Sheet,
                info.StyleVariable,
                info.StyleVariable.handles[0].valueType != StyleValueType.Function ? 0 : 2);

            VariableValue = valueText;
            SourceStyleSheet = displayPath;
        }

        VariableName = info.Name;
        Description = info.Description;

        if (info.StyleVariable.handles == null || info.StyleVariable.handles.Length == 0)
            return;

        if (info.StyleVariable.handles[0].valueType == StyleValueType.Color)
        {
            m_Preview.style.backgroundColor = info.Sheet.ReadColor(info.StyleVariable.handles[0]);
            m_Preview.RemoveFromClassList(VariableEditingHandler.HiddenStyleClassName);
        }
        else if (info.StyleVariable.handles[0].valueType == StyleValueType.Enum)
        {
            var colorName = info.Sheet.ReadAsString(info.StyleVariable.handles[0]);
            if (StyleSheetColor.TryGetColor(colorName.ToLowerInvariant(), out var color))
            {
                m_Preview.style.backgroundColor = color;
                m_Preview.RemoveFromClassList(VariableEditingHandler.HiddenStyleClassName);
            }
        }
        else if (info.StyleVariable.handles[0].valueType is StyleValueType.ResourcePath or StyleValueType.AssetReference)
        {
            var source = new ImageSource();
            var dpiScaling = 1.0f;
            if (StylePropertyReader.TryGetImageSourceFromValue(
                    new StylePropertyValue { sheet = info.Sheet, handle = info.StyleVariable.handles[0] },
                    dpiScaling, out source) == false)
            {
                source.texture = Panel.LoadResource("d_console.warnicon", typeof(Texture2D), dpiScaling) as Texture2D;
            }

            m_Thumbnail.image = source.texture;
            m_Thumbnail.vectorImage = source.vectorImage;
            m_Preview.RemoveFromClassList(VariableEditingHandler.HiddenStyleClassName);
            m_ValueAndPreviewContainer.AddToClassList(s_PreviewThumbnailUssClassName);
        }
    }
}
