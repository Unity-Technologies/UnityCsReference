// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class ZIndexWindowManagerDemo : ElementSnippet<ZIndexWindowManagerDemo>
    {
        internal override void Apply(VisualElement container)
        {
            container.AddToClassList(EditorGUIUtility.isProSkin ? "dark" : "light");

            Initialize(container);
        }

        /// <sample>
        #region sample
        VisualElement[] m_Windows;
        int[] m_ZIndices;
        int m_ZCounter;

        void Initialize(VisualElement root)
        {
            var desktop = root.Q("desktop");
            int count = desktop.childCount;

            m_Windows = new VisualElement[count];
            m_ZIndices = new int[count];
            m_ZCounter = count;

            for (int i = 0; i < count; i++)
            {
                var win = desktop.ElementAt(i);
                var idx = i;
                m_Windows[i] = win;
                m_ZIndices[i] = i + 1;
                win.RegisterCallback<PointerDownEvent>(_ => FocusWindow(idx), TrickleDown.TrickleDown);
            }
        }

        void FocusWindow(int index)
        {
            m_ZCounter++;
            m_ZIndices[index] = m_ZCounter;

            for (int i = 0; i < m_Windows.Length; i++)
            {
                m_Windows[i].EnableInClassList("focused", i == index);
                m_Windows[i].style.zIndex = m_ZIndices[i];
            }
        }
        #endregion
        /// </sample>
    }
}
