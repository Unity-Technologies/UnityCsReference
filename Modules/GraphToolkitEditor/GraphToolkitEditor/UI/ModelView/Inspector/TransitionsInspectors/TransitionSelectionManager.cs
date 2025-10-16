// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for elements that can be selected through the <see cref="TransitionSelectionManager{T}"/>.
    /// </summary>
    internal interface ISelectableElement
    {
        bool IsSelected { get; set; }
    }

    /// <summary>
    /// Manager for selecting elements in the transitions and condition editors.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class TransitionSelectionManager<T> where T : class, ISelectableElement
    {
        List<T> m_SelectedElements = new();
        List<T> m_ContinuousSelection;
        T m_LastSelectedForContinuousSelection;

        public IReadOnlyList<T> SelectedElements => m_SelectedElements;

        public T LastSelectedForContinuousSelection => m_LastSelectedForContinuousSelection;

        public T CurrentSelectionHead
        {
            get
            {
                if (m_ContinuousSelection != null && m_ContinuousSelection.Count > 0)
                    return m_ContinuousSelection[^1];

                if (m_SelectedElements.Count > 0)
                    return m_SelectedElements[^1];

                return null;
            }
        }

        public void ClearSelection()
        {
            foreach (var element in m_SelectedElements)
            {
                element.IsSelected = false;
            }
            m_SelectedElements.Clear();
        }

        public void Select(T element, bool byKey = false)
        {
            if (m_SelectedElements.Count == 1 && m_SelectedElements[0] == element)
                return;

            ClearSelection();
            element.IsSelected = true;
            m_SelectedElements.Add(element);
            m_LastSelectedForContinuousSelection = element;
            m_ContinuousSelection = null;
        }

        public void SetContinuousSelection(List<T> elements)
        {
            if (m_ContinuousSelection != null)
            {
                foreach (var element in m_ContinuousSelection)
                {
                    if (!elements.Contains(element))
                    {
                        element.IsSelected = false;
                        m_SelectedElements.Remove(element);
                    }
                }
            }
            foreach (var element in elements)
            {
                element.IsSelected = true;
                if (!m_SelectedElements.Contains(element))
                    m_SelectedElements.Add(element);
            }

            m_ContinuousSelection = elements;
        }

        public void Remove(T element)
        {
            element.IsSelected = false;
            m_SelectedElements.Remove(element);
            m_ContinuousSelection = null;
            if (ReferenceEquals(m_LastSelectedForContinuousSelection, element))
                m_LastSelectedForContinuousSelection = m_SelectedElements.Count > 0 ? m_SelectedElements[^1] : null;
        }

        public void Toggle(T element)
        {
            if (element.IsSelected)
            {
                element.IsSelected = false;
                m_SelectedElements.Remove(element);
                if (ReferenceEquals(m_LastSelectedForContinuousSelection, element))
                    m_LastSelectedForContinuousSelection = m_SelectedElements.Count > 0 ? m_SelectedElements[^1] : null;
            }
            else
            {
                element.IsSelected = true;
                m_SelectedElements.Add(element);
                m_LastSelectedForContinuousSelection = element;
            }
            m_ContinuousSelection = null;
        }
    }
}
