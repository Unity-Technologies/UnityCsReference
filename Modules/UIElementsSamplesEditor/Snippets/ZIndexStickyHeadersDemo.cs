// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class ZIndexStickyHeadersDemo : ElementSnippet<ZIndexStickyHeadersDemo>
    {
        internal override void Apply(VisualElement container)
        {
            container.AddToClassList(EditorGUIUtility.isProSkin ? "dark" : "light");

            Initialize(container);
        }

        /// <sample>
        #region sample
        const int k_PinnedZIndex = 100;

        ScrollView m_Scroll;
        VisualElement[] m_Headers;

        void Initialize(VisualElement root)
        {
            m_Scroll = root.Q<ScrollView>("scroll");

            var headers = new List<VisualElement>();
            var content = m_Scroll.contentContainer;
            for (int i = 0; i < content.childCount; i++)
            {
                var child = content.ElementAt(i);
                if (child.ClassListContains("sticky-header"))
                    headers.Add(child);
            }
            m_Headers = headers.ToArray();

            m_Scroll.verticalScroller.valueChanged += _ => UpdateSticky();
            m_Scroll.contentContainer.RegisterCallback<GeometryChangedEvent>(_ => UpdateSticky());
        }

        void UpdateSticky()
        {
            if (m_Headers == null) return;

            float top = m_Scroll.scrollOffset.y;

            for (int i = 0; i < m_Headers.Length; i++)
            {
                var header = m_Headers[i];
                float headerY = header.layout.y;
                float headerH = header.layout.height;
                float nextY = i + 1 < m_Headers.Length ? m_Headers[i + 1].layout.y : float.MaxValue;

                bool active = top >= headerY && top < nextY;
                if (active)
                {
                    float translateY = top - headerY;
                    float maxTranslateY = nextY - headerH - headerY;
                    if (translateY > maxTranslateY) translateY = maxTranslateY;

                    header.style.translate = new Translate(0, translateY);
                    header.style.zIndex = k_PinnedZIndex;
                }
                else
                {
                    header.style.translate = new Translate(0, 0);
                    header.style.zIndex = StyleKeyword.Null;
                }
            }
        }
        #endregion
        /// </sample>
    }
}
