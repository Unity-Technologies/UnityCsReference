// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Accessibility
{
    [UxmlElement]
    partial class SearchableLabel : VisualElement
    {
        private static readonly string s_UssClassName = "searchable-label";
        private static readonly string s_LabelTextUssClassName = s_UssClassName + "__text";
        private static readonly string s_HighlightUssClassName = s_UssClassName + "__highlight";

        private readonly Label m_Label;
        private readonly VisualElement m_Highlight;

        [UxmlAttribute]
        [CreateProperty]
        public string text
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }

        public SearchableLabel()
        {
            AddToClassList(s_UssClassName);
            m_Label = new Label();
            m_Label.AddToClassList(s_LabelTextUssClassName);
            m_Highlight = new VisualElement();
            m_Highlight.AddToClassList(s_HighlightUssClassName);
            Add(m_Label);
            Add(m_Highlight);
            ClearHighlight();
        }

        public void ClearHighlight()
        {
            m_Highlight.style.display = DisplayStyle.None;
        }

        public void HighlightText(string query)
        {
            ClearHighlight();

            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
                return;

            var indexOf = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);

            if (indexOf < 0)
                return;

            var startPos = m_Label.MeasureTextSize(text[..indexOf], 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);
            var endPos = m_Label.MeasureTextSize(text[..(indexOf + query.Length)], 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);

            m_Highlight.style.width = endPos.x - startPos.x;
            m_Highlight.style.left = startPos.x;
            m_Highlight.style.display = DisplayStyle.Flex;
        }
    }
}
