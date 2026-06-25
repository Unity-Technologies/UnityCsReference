// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Provides completion to the field for USS class names.
/// Consumers must set <see cref="FieldSearchCompleter{TData}.DataSourceCallback"/> before use to provide the
/// list of <see cref="ClassCompleterInfo"/> items.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
class ClassCompleter : FieldSearchCompleter<ClassCompleterInfo>
{
    const string k_PopupStyleSheet = "UIToolkitAuthoring/Inspector/Controls/Completers/ClassCompleterPopup.uss";

    public static readonly string ItemUssClassName = "unity-field-search-completer-popup__item";
    public static readonly string ItemNameLabelName = "nameLabel";
    public static readonly string ItemNameLabelUssClassName = "unity-field-search-completer-popup__item__name-label";
    public static readonly string ClassLabelUssClassName = "class-label";
    public static readonly string CreateClassLabelUssClassName = "create-class-label";
    public static readonly string StyleSheetHeaderUssClassName = "stylesheet-header";

    ClassInfoView m_DetailsView;

    /// <summary>
    /// Optional callback to count how many elements in the current document match a given selector string
    /// (e.g. ".my-class"). Forwarded to <see cref="ClassInfoView"/> for display in the details panel.
    /// </summary>
    public Func<string, int> GetMatchingElementCount { get; set; }

    public ClassCompleter(TextField textField)
    {
        AlwaysVisible = true;

        MakeItem = () =>
        {
            var item = new VisualElement();
            item.AddToClassList(ItemUssClassName);

            var nameLabel = new Label();
            nameLabel.AddToClassList(ItemNameLabelUssClassName);
            nameLabel.name = ItemNameLabelName;
            nameLabel.style.textOverflow = TextOverflow.Ellipsis;
            item.Add(nameLabel);

            return item;
        };

        BindItem = (e, i) =>
        {
            var res = Results[i];
            var label = e.Q<Label>(ItemNameLabelName);

            if (res.IsValidStyleSheetInfo())
            {
                label.text = res.StyleSheet.name;
                e.RemoveFromClassList(ClassLabelUssClassName);
                e.RemoveFromClassList(CreateClassLabelUssClassName);
                e.AddToClassList(StyleSheetHeaderUssClassName);
                e.pickingMode = PickingMode.Ignore;
                label.pickingMode = PickingMode.Ignore;
            }
            else
            {
                if (res.IsValidClassInfo())
                {
                    label.text = "." + res.StyleSelectorPart.value;
                    e.AddToClassList(ClassLabelUssClassName);
                    e.RemoveFromClassList(CreateClassLabelUssClassName);
                }
                else
                {
                    var currentText = AttachedTextField?.text ?? string.Empty;
                    label.text = string.IsNullOrEmpty(currentText)
                        ? "Type to create new class"
                        : $"Create class \"{currentText}\"?";
                    e.AddToClassList(CreateClassLabelUssClassName);
                    e.RemoveFromClassList(ClassLabelUssClassName);
                }

                e.RemoveFromClassList(StyleSheetHeaderUssClassName);
                e.pickingMode = PickingMode.Position;
                label.pickingMode = PickingMode.Position;
            }
        };

        HoveredItemChanged += UpdateDetailsView;
        SelectionChanged += UpdateDetailsView;

        SetupCompleterField(textField, useRealWindow: false);
    }

    void UpdateDetailsView(ClassCompleterInfo data)
    {
        if (m_DetailsView == null)
            return;

        if (!data.IsValidClassInfo())
            return;

        m_DetailsView.SetInfo(data);
    }

    protected override VisualElement MakeDetailsContent()
    {
        m_DetailsView = new ClassInfoView { GetMatchingElementCount = GetMatchingElementCount };
        return m_DetailsView;
    }

    protected override string GetResultCountText(int count)
    {
        var classCount = 0;
        if (Results != null)
        {
            foreach (var r in Results)
            {
                if (r.IsValidClassInfo())
                    classCount++;
            }
        }
        return $"{classCount} found";
    }

    protected override bool MatchFilter(string filter, in ClassCompleterInfo data)
    {
        // Always include the "create new" entry and stylesheet headers
        if (data.IsCreateNewClassField() || data.IsValidStyleSheetInfo())
            return true;

        return string.IsNullOrEmpty(filter) || GetClassName(data).Contains(filter);
    }

    protected override string GetTextFromData(ClassCompleterInfo data)
    {
        return GetClassName(data);
    }

    string GetClassName(ClassCompleterInfo data)
    {
        if (data.IsValidStyleSheetInfo())
            return data.StyleSheet != null ? data.StyleSheet.name : string.Empty;

        // For the "create new class" entry, preserve whatever the user has typed
        if (data.IsCreateNewClassField())
            return AttachedTextField?.text ?? string.Empty;

        return "." + data.StyleSelectorPart.value;
    }

    protected override FieldSearchCompleterPopup CreatePopup()
    {
        var popup = base.CreatePopup();
        popup.styleSheets.Add(EditorGUIUtility.Load(k_PopupStyleSheet) as StyleSheet);
        return popup;
    }
}
