// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Experimental.UIElements.Debugger;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor
{
    internal class ElementHighlighter
    {
        private VisualElement m_PaddingHighlighter;
        private VisualElement m_ContentHighlighter;

        public bool IsEnabled { get; set; }

        public ElementHighlighter()
        {
            IsEnabled = true;
        }

        public void ClearElement()
        {
            if (m_PaddingHighlighter != null && m_PaddingHighlighter.shadow.parent != null)
            {
                var parent = m_PaddingHighlighter.shadow.parent;

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
                m_PaddingHighlighter.style.borderColor = UIElementsDebugger.Styles.kSizePaddingSecondaryColor;
                m_PaddingHighlighter.style.borderLeftWidth = borderWidth;
                m_PaddingHighlighter.style.borderRightWidth = borderWidth;
                m_PaddingHighlighter.style.borderTopWidth = borderWidth;
                m_PaddingHighlighter.style.borderBottomWidth = borderWidth;
                m_PaddingHighlighter.pickingMode = PickingMode.Ignore;
                m_ContentHighlighter = new VisualElement();
                m_ContentHighlighter.style.borderColor = UIElementsDebugger.Styles.kSizeSecondaryColor;
                m_ContentHighlighter.style.borderLeftWidth = borderWidth;
                m_ContentHighlighter.style.borderRightWidth = borderWidth;
                m_ContentHighlighter.style.borderTopWidth = borderWidth;
                m_ContentHighlighter.style.borderBottomWidth = borderWidth;
                m_ContentHighlighter.pickingMode = PickingMode.Ignore;
            }

            m_PaddingHighlighter.layout = elementRect;
            rootElement.Add(m_PaddingHighlighter);
            if (style != null)
                elementRect = style.padding.Remove(elementRect);
            m_ContentHighlighter.layout = elementRect;
            rootElement.Add(m_ContentHighlighter);
        }
    }
}
