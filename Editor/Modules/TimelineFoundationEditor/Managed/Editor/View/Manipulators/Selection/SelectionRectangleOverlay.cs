// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View.Internals
{
    class SelectionRectangleOverlay<TElement, TSelection>
        where TElement : VisualElement
        where TSelection : IUniqueElement
    {
        Vector2 m_MouseStart;
        Vector2 m_MouseDelta;
        public RectangleOverlay overlay { get; }
        public event Action onBeginRectangle;
        PointerManipulator m_Manipulator;

        public SelectionRectangleOverlay(PointerManipulator manipulator)
        {
            overlay = new RectangleOverlay();
            overlay.Hide();
            m_Manipulator = manipulator;
        }

        public void OnStartDrag(Vector2 mouseWorldPosition)
        {
            m_MouseStart = mouseWorldPosition;
            m_MouseDelta = Vector2.zero;
            onBeginRectangle?.Invoke();
        }

        public void UpdateSize(Vector2 mouseDelta)
        {
            if (!overlay.isShown)
                overlay.Show();

            m_MouseDelta += mouseDelta;

            overlay.SetRectFromWorldRect(new Rect(m_MouseStart.x + Math.Min(0, m_MouseDelta.x),
                m_MouseStart.y + Math.Min(0, m_MouseDelta.y),
                Math.Abs(m_MouseDelta.x),
                Math.Abs(m_MouseDelta.y)));
        }

        public void OnEndDrag()
        {
            overlay.ResetRect();
            overlay.Hide();
        }

        public IEnumerable<TElement> GetOverlappedElements()
        {
            return m_Manipulator.target.Query<TElement>().Where(v => v is TSelection
                && overlay.worldBound.Overlaps(v.worldBound)).Build();
        }


        public void ApplyToRectangleSelectableContent(VisualElement target, Action<TElement> action)
        {
            var elements = GetOverlappedElements();
            foreach (var element in elements)
            {
                action.Invoke(element);
            }
        }
    }
}
