// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor;

static class TextAreaFieldHelper
{
    internal static readonly string textAreaFieldUssClassName = "gtk-text-area-field";
    static readonly int k_SingleLineHeight = 21;
    static readonly int k_LineHeight = 15;

    internal static TextField CreateTextAreaField(TextAreaAttribute textAreaAttribute, bool isDelayed)
    {
        if (textAreaAttribute == null)
            return null;

        // First line
        var initialHeight = k_SingleLineHeight;

        var minHeight = initialHeight + (textAreaAttribute!.minLines - 1) * k_LineHeight;
        var maxHeight = initialHeight + (textAreaAttribute!.maxLines - 1) * k_LineHeight;

        if (maxHeight < minHeight)
            maxHeight = minHeight;

        var field = new TextField
        {
            isDelayed = isDelayed,
            multiline = true,
            style =
            {
                minHeight = minHeight,
                maxHeight = maxHeight,
            },
            verticalScrollerVisibility = ScrollerVisibility.Auto
        };
        field.AddToClassList(textAreaFieldUssClassName);

        // Prevent zooming the graph when the scroller is visible.
        field.RegisterCallback<WheelEvent>(evt =>
        {
            var scrollView = field.Q<ScrollView>();
            if (scrollView != null && scrollView.verticalScroller.resolvedStyle.display == DisplayStyle.Flex)
                evt.StopImmediatePropagation();
        });

        return field;
    }

    internal static void UpdateTextAreaHeight(TextAreaAttribute textAreaAttribute, TextField textAreaField, string text)
    {
        if (textAreaAttribute == null || textAreaField == null)
            return;

        var textInput = textAreaField.Q(TextField.textInputUssName);
        var textElement = textInput?.Q<TextElement>();
        if (textElement == null)
            return;

        float fullTextHeight;
        try
        {
            fullTextHeight = EditorStyles.textArea.CalcHeight(new GUIContent(text), textElement.contentRect.width);
        }
        catch
        {
            // In case of any issue calculating the height, fallback to single line height.
            fullTextHeight = k_SingleLineHeight;
        }

        int lines = Mathf.RoundToInt(fullTextHeight / k_LineHeight);

        lines = Mathf.Clamp(lines, textAreaAttribute.minLines, textAreaAttribute.maxLines);

        textAreaField.style.height = k_SingleLineHeight // First line
                                     + (lines - 1) * k_LineHeight; // Remaining lines
    }
}
