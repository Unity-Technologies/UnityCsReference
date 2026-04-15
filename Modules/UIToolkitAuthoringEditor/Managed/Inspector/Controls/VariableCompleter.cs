// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal class VariableCompleter : FieldSearchCompleter<VariableInfo>
{
    public static readonly string ItemUssClassName = "unity-field-search-completer-popup__item";
    public static readonly string ItemNameLabelName = "nameLabel";
    public static readonly string ItemNameLabelUssClassName = "unity-field-search-completer-popup__item__name-label";
    public static readonly string ItemEditorOnlyLabelName = "editorOnlyLabel";
    public static readonly string ItemEditorOnlyLabelUssClassName = "unity-field-search-completer-popup__item__editor-only-label";
    public static readonly string TagPillClassName = "unity-tag-pill";
    public static readonly string EditorOnlyTag = "Editor Only";

    VariableEditingHandler m_Handler;
    VariableInfoView m_DetailsView;

    public VariableCompleter(VariableEditingHandler handler)
    {
        m_Handler = handler;
        GetFilterFromTextCallback = text => text?.TrimStart('-');
        DataSourceCallback = () =>
        {
            return StyleVariableUtility.GetAllAvailableVariables(
                handler.context.CurrentVisualElement,
                StyleValueTypeResolver.GetCompatibleTypes(handler.styleName),
                handler.context.EditorExtensionMode);
        };
        MakeItem = () =>
        {
            var item = new VisualElement().WithClassList(ItemUssClassName);

            var nameLabel = new Label();
            nameLabel.AddToClassList(ItemNameLabelUssClassName);
            nameLabel.name = ItemNameLabelName;
            nameLabel.style.textOverflow = TextOverflow.Ellipsis;

            var editorOnlyLabel = new Label(EditorOnlyTag);
            editorOnlyLabel.AddToClassList(ItemEditorOnlyLabelUssClassName);
            editorOnlyLabel.AddToClassList(TagPillClassName);
            editorOnlyLabel.name = ItemEditorOnlyLabelName;

            item.Add(nameLabel);
            item.Add(editorOnlyLabel);
            return item;
        };
        BindItem = (e, i) =>
        {
            var res = Results[i];
            e.Q<Label>(ItemNameLabelName).text = res.Name;
        };

        HoveredItemChanged += UpdateDetailView;
        SelectionChanged += UpdateDetailView;
        ItemChosen += (i) =>
        {
            if (!m_Handler.isVariableFieldVisible)
            {
                var varName = Results[i].Name;
                m_Handler.SetVariable(varName);
            }
        };

        MatcherCallback = Matcher;
        GetTextFromDataCallback = GetVarName;
        SetupCompleterField(handler.variableField?.TextField, false);
    }

    void UpdateDetailView(VariableInfo data)
    {
        if (m_DetailsView == null || !data.IsValid()) return;

        m_DetailsView.SetInfo(data);
    }

    protected override VisualElement MakeDetailsContent()
    {
        m_DetailsView = new VariableInfoView();
        return m_DetailsView;
    }

    static string GetVarName(VariableInfo data)
    {
        return data.Name;
    }

    bool Matcher(string filter, VariableInfo data)
    {
        var text = data.Name;
        return !string.IsNullOrEmpty(text) && text.Contains(filter);
    }

    protected override bool IsValidText(string text)
    {
        if (m_Handler.variableField != null && m_Handler.variableField.TextField == AttachedTextField)
        {
            return true;
        }

        return text != null && text.StartsWith(VariableNameUtilities.UssVariablePrefix);
    }
}
