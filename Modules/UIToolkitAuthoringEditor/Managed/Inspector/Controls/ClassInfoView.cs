// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
class ClassInfoView : VisualElement
{
    const string k_UssClassName = "unity-ui-inspector__classinfo-view";
    const string k_UxmlTemplatePath = "UIToolkitAuthoring/Inspector/Controls/ClassInfoView.uxml";
    const string k_EmptyText = "None";

    Label m_MatchingElements;
    Label m_StyleSheetLabel;

    /// <summary>
    /// Optional callback to retrieve how many elements in the document match the given selector string.
    /// Receives the selector string (e.g. ".my-class") and returns the count.
    /// When null, the "Used in:" row shows "None".
    /// </summary>
    public Func<string, int> GetMatchingElementCount { get; set; }

    public string MatchingElements
    {
        get => m_MatchingElements.text;
        set => m_MatchingElements.text = value;
    }

    public string SourceStyleSheet
    {
        get => m_StyleSheetLabel.text;
        set => m_StyleSheetLabel.text = value;
    }

    public ClassInfoView()
    {
        AddToClassList(k_UssClassName);

        var template = EditorGUIUtility.Load(k_UxmlTemplatePath) as VisualTreeAsset;
        template.CloneTree(this);

        m_MatchingElements = this.Q<Label>("matching-elements-label");
        m_StyleSheetLabel = this.Q<Label>("stylesheet-label");
        m_MatchingElements.style.textOverflow = TextOverflow.Ellipsis;
        m_MatchingElements.displayTooltipWhenElided = true;
        m_StyleSheetLabel.style.textOverflow = TextOverflow.Ellipsis;
        m_StyleSheetLabel.displayTooltipWhenElided = true;

        ClearUI();
    }

    void ClearUI()
    {
        MatchingElements = k_EmptyText;
        SourceStyleSheet = k_EmptyText;
    }

    public void SetInfo(ClassCompleterInfo completerInfo)
    {
        ClearUI();

        if (!completerInfo.IsValidClassInfo())
            return;

        var styleSheet = completerInfo.StyleSheet;
        var fullPath = AssetDatabase.GetAssetPath(styleSheet);
        string displayPath;

        if (string.IsNullOrEmpty(fullPath))
        {
            displayPath = styleSheet.name;
        }
        else
        {
            displayPath = fullPath == UnityEditor.Experimental.EditorResources.libraryBundlePath
                ? styleSheet.name
                : Path.GetFileName(fullPath);
        }

        SourceStyleSheet = displayPath;

        if (GetMatchingElementCount != null)
        {
            var count = GetMatchingElementCount("." + completerInfo.StyleSelectorPart.value);
            var elementWord = count == 1 ? "element" : "elements";
            MatchingElements = $"{count} other {elementWord} in this document";
        }
    }
}
