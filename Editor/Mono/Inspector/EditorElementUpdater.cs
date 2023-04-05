// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Diagnostics;
using UnityEditorInternal;
using UnityEngine.UIElements;

namespace UnityEditor
{
    /// <summary>
    /// The <see cref="EditorElementUpdater"/> handles calling <see cref="IEditorElement.CreateInspectorElement"/> in a time-sliced manner.
    /// </summary>
    class EditorElementUpdater
    {
        readonly PropertyEditor m_PropertyEditor;
        readonly List<IEditorElement> m_EditorElements = new List<IEditorElement>();
        readonly Stopwatch m_UpdateTickTimer = new Stopwatch();

        Panel Panel => m_PropertyEditor.rootVisualElement.panel as Panel;

        int m_Index;

        public int Position => m_Index;
        public int Count => m_EditorElements.Count;

        public EditorElementUpdater(PropertyEditor propertyEditor)
        {
            m_PropertyEditor = propertyEditor;
            m_Index = 0;
        }

        /// <summary>
        /// Adds the specified <see cref="IEditorElement"/> to the updater.
        /// </summary>
        /// <param name="element">The editor element to add.</param>
        public void Add(IEditorElement element)
        {
            m_EditorElements.Add(element);
        }

        /// <summary>
        /// Removes the specified <see cref="IEditorElement"/> from the updater.
        /// </summary>
        /// <param name="element">The editor element to remove.</param>
        public void Remove(IEditorElement element)
        {
            var index = m_EditorElements.IndexOf(element);

            if (index == -1)
                return;

            if (m_Index > index)
                m_Index--;
        }

        /// <summary>
        /// Clears the internal state and resets the enumerator.
        /// </summary>
        public void Clear()
        {
            m_EditorElements.Clear();
            m_Index = 0;
        }

        /// <summary>
        /// Invokes <see cref="IEditorElement.CreateInspectorElement"/> until the first <paramref name="count"/> elements are created.
        /// </summary>
        /// <param name="count">The number of elements to create.</param>
        public void CreateMinimumInspectorElementsWithoutLayout(int count)
        {
            for (; m_Index < count && m_Index < m_EditorElements.Count; m_Index++)
                m_EditorElements[m_Index].CreateInspectorElement();
        }

        /// <summary>
        /// Invokes <see cref="IEditorElement.CreateInspectorElement"/> for the specified number of elements.
        /// </summary>
        /// <param name="count">The number of elements to create.</param>
        public void CreateInspectorElementsWithoutLayout(int count)
        {
            for (var i = 0; m_Index < m_EditorElements.Count && i < count; i++)
                m_EditorElements[m_Index++].CreateInspectorElement();
        }

        /// <summary>
        /// Invokes <see cref="IEditorElement.CreateInspectorElement"/> followed by a layout until the given <paramref name="viewport"/> is filled.
        /// </summary>
        /// <param name="viewport">The viewport to build elements for.</param>
        /// <param name="contentContainer"></param>
        public void CreateInspectorElementsForViewport(ScrollView viewport, VisualElement contentContainer)
        {
            if (m_Index >= m_EditorElements.Count)
                return;

            var scroll = viewport.verticalScroller.value;

            while (m_Index < m_EditorElements.Count)
            {
                var element = m_EditorElements[m_Index++];

                element.CreateInspectorElement();

                // If this element contributes to the layout, re-compute it immediately to determine how much of the viewport is occupied.
                if (null != element.editor && InternalEditorUtility.GetIsInspectorExpanded(element.editor.target))
                {
                    Panel?.UpdateWithoutRepaint();

                    if (contentContainer.ElementAt(m_Index - 1).layout.yMax - scroll > viewport.layout.height)
                        break;
                }
            }

            viewport.verticalScroller.value = scroll;
        }

        /// <summary>
        /// Invokes <see cref="IEditorElement.CreateInspectorElement"/> followed by a layout until the target <see cref="targetMilliseconds"/> has been reached.
        /// </summary>
        /// <remarks>
        /// The time value includes creation of the GUI, styling and layout. This does NOT include render time.
        /// </remarks>
        /// <param name="targetMilliseconds">The target number of milliseconds to spend on the update.</param>
        public void CreateInspectorElementsForMilliseconds(long targetMilliseconds)
        {
            if (m_Index >= m_EditorElements.Count)
                return;

            m_UpdateTickTimer.Restart();

            while (m_UpdateTickTimer.ElapsedMilliseconds < targetMilliseconds)
            {
                var element = m_EditorElements[m_Index++];

                element.CreateInspectorElement();

                // If this was the last element. We can early out and let the standard update loop tick the layout and repaint.
                if (m_Index >= m_EditorElements.Count)
                    break;

                // If this element contributes to the layout, re-compute it immediately to determine how much of the viewport is occupied.
                if (null != element.editor && InternalEditorUtility.GetIsInspectorExpanded(element.editor.target))
                    Panel?.UpdateWithoutRepaint();
            }
        }
    }
}
