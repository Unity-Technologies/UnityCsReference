// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class ElementHighlighter
    {
        internal static readonly Color kSizePaddingSecondaryColor = new Color(194f / 255, 237f / 255, 138f / 255);
        internal static readonly Color kSizeSecondaryColor = new Color(139f / 255, 181f / 255, 192f / 255);

        private VisualElement m_PaddingHighlighter;
        private VisualElement m_ContentHighlighter;

        public bool IsEnabled { get; set; }

        public ElementHighlighter()
        {
            IsEnabled = true;
        }

        public void ClearElement()
        {
            if (m_PaddingHighlighter != null && m_PaddingHighlighter.hierarchy.parent != null)
            {
                var parent = m_PaddingHighlighter.hierarchy.parent;

                m_PaddingHighlighter.RemoveFromHierarchy();
                m_ContentHighlighter.RemoveFromHierarchy();

                parent.MarkDirtyRepaint();
            }
        }

        public void HighlightElement(VisualElement rootElement, Rect elementRect, GUIStyle style = null)
        {
            if (!IsEnabled)
                return;

            ClearElement();

            if (m_PaddingHighlighter == null)
            {
                var borderWidth = 1f;
                m_PaddingHighlighter = new VisualElement();
                m_PaddingHighlighter.style.borderLeftColor = kSizePaddingSecondaryColor;
                m_PaddingHighlighter.style.borderTopColor = kSizePaddingSecondaryColor;
                m_PaddingHighlighter.style.borderRightColor = kSizePaddingSecondaryColor;
                m_PaddingHighlighter.style.borderBottomColor = kSizePaddingSecondaryColor;
                m_PaddingHighlighter.style.borderLeftWidth = borderWidth;
                m_PaddingHighlighter.style.borderRightWidth = borderWidth;
                m_PaddingHighlighter.style.borderTopWidth = borderWidth;
                m_PaddingHighlighter.style.borderBottomWidth = borderWidth;
                m_PaddingHighlighter.pickingMode = PickingMode.Ignore;
                m_ContentHighlighter = new VisualElement();
                m_ContentHighlighter.style.borderLeftColor = kSizeSecondaryColor;
                m_ContentHighlighter.style.borderTopColor = kSizeSecondaryColor;
                m_ContentHighlighter.style.borderRightColor = kSizeSecondaryColor;
                m_ContentHighlighter.style.borderBottomColor = kSizeSecondaryColor;
                m_ContentHighlighter.style.borderLeftWidth = borderWidth;
                m_ContentHighlighter.style.borderRightWidth = borderWidth;
                m_ContentHighlighter.style.borderTopWidth = borderWidth;
                m_ContentHighlighter.style.borderBottomWidth = borderWidth;
                m_ContentHighlighter.pickingMode = PickingMode.Ignore;
            }

            SetLayout(m_PaddingHighlighter, elementRect);
            rootElement.Add(m_PaddingHighlighter);
            if (style != null)
                elementRect = style.padding.Remove(elementRect);
            SetLayout(m_ContentHighlighter,  elementRect);
            rootElement.Add(m_ContentHighlighter);
        }

        void SetLayout(VisualElement ve, Rect layout)
        {
            ve.style.position = Position.Absolute;
            ve.style.top = layout.yMin;
            ve.style.left = layout.xMin;
            ve.style.width = layout.width;
            ve.style.height = layout.height;
        }
    }
}
