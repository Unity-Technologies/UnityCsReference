// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.StyleSheets;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.Experimental.UIElements
{
    internal class VisualSplitter : VisualElement
    {
        const int kDefaultSplitSize = 10;
        public int splitSize = kDefaultSplitSize;

        public new class UxmlFactory : UxmlFactory<VisualSplitter, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits {}

        private class SplitManipulator : MouseManipulator
        {
            private int m_ActiveVisualElementIndex = -1;
            private int m_NextVisualElementIndex = -1;

            private List<VisualElement> m_AffectedElements;

            bool m_Active;

            public SplitManipulator()
            {
                activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                target.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
                target.RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
                target.UnregisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
            }

            protected void OnMouseDown(MouseDownEvent e)
            {
                if (CanStartManipulation(e))
                {
                    VisualSplitter visualSplitter = target as VisualSplitter;
                    FlexDirection flexDirection = visualSplitter.style.flexDirection;

                    if (m_AffectedElements != null)
                    {
                        VisualElementListPool.Release(m_AffectedElements);
                    }
                    m_AffectedElements = visualSplitter.GetAffectedVisualElements();

                    for (int i = 0; i < m_AffectedElements.Count - 1; ++i)
                    {
                        VisualElement visualElement = m_AffectedElements[i];

                        Rect splitterRect = visualSplitter.GetSplitterRect(visualElement);

                        if (splitterRect.Contains(e.localMousePosition))
                        {
                            bool isReverse = flexDirection == FlexDirection.RowReverse || flexDirection == FlexDirection.ColumnReverse;

                            if (isReverse)
                            {
                                m_ActiveVisualElementIndex = i + 1;
                                m_NextVisualElementIndex = i;
                            }
                            else
                            {
                                m_ActiveVisualElementIndex = i;
                                m_NextVisualElementIndex = i + 1;
                            }

                            m_Active = true;
                            target.CaptureMouse();
                            e.StopPropagation();
                        }
                    }
                }
            }

            protected void OnMouseMove(MouseMoveEvent e)
            {
                if (m_Active)
                {
                    // These calculations should only work if flex-basis is auto.
                    // However, Yoga implementation of flex-basis 0 is broken and behaves much like
                    // flex-basis auto, so it currently works with flex-basis 0 too.

                    VisualSplitter visualSplitter = target as VisualSplitter;
                    VisualElement visualElement = m_AffectedElements[m_ActiveVisualElementIndex];
                    VisualElement nextVisualElement = m_AffectedElements[m_NextVisualElementIndex];

                    FlexDirection flexDirection = visualSplitter.style.flexDirection;
                    bool isVertical = flexDirection == FlexDirection.Column || flexDirection == FlexDirection.ColumnReverse;

                    float relativeMousePosition;
                    if (isVertical)
                    {
                        relativeMousePosition = (e.localMousePosition.y - visualElement.layout.yMin - visualElement.style.minHeight) /
                            (visualElement.layout.height + nextVisualElement.layout.height -
                                visualElement.style.minHeight - nextVisualElement.style.minHeight);
                    }
                    else
                    {
                        relativeMousePosition = (e.localMousePosition.x - visualElement.layout.xMin - visualElement.style.minWidth) /
                            (visualElement.layout.width + nextVisualElement.layout.width -
                                visualElement.style.minWidth - nextVisualElement.style.minWidth);
                    }

                    relativeMousePosition = Math.Max(0.0f, Math.Min(1.0f, relativeMousePosition));

                    float totalFlex = visualElement.style.flexGrow + nextVisualElement.style.flexGrow;
                    visualElement.style.flexGrow = relativeMousePosition * totalFlex;
                    nextVisualElement.style.flexGrow = (1.0f - relativeMousePosition) * totalFlex;

                    e.StopPropagation();
                }
            }

            protected void OnMouseUp(MouseUpEvent e)
            {
                if (m_Active && CanStopManipulation(e))
                {
                    m_Active = false;
                    target.ReleaseMouse();
                    e.StopPropagation();

                    m_ActiveVisualElementIndex = -1;
                    m_NextVisualElementIndex = -1;
                }
            }
        }

        public VisualSplitter()
        {
            this.AddManipulator(new SplitManipulator());
        }

        public List<VisualElement> GetAffectedVisualElements()
        {
            List<VisualElement> elements = VisualElementListPool.Get();
            for (int i = 0; i < shadow.childCount; ++i)
            {
                VisualElement element = shadow[i];
                if (element.style.positionType == PositionType.Relative)
                    elements.Add(element);
            }

            return elements;
        }

        protected override void DoRepaint(IStylePainter painter)
        {
            for (int i = 0; i < shadow.childCount - 1; ++i)
            {
                VisualElement visualElement = shadow[i];
                bool isVertical = style.flexDirection == FlexDirection.Column || style.flexDirection == FlexDirection.ColumnReverse;

                EditorGUIUtility.AddCursorRect(GetSplitterRect(visualElement), isVertical ? MouseCursor.ResizeVertical : MouseCursor.SplitResizeLeftRight);
            }
        }

        public Rect GetSplitterRect(VisualElement visualElement)
        {
            Rect rect = visualElement.layout;
            if (style.flexDirection == FlexDirection.Row)
            {
                rect.xMin = visualElement.layout.xMax - splitSize * 0.5f;
                rect.xMax = visualElement.layout.xMax + splitSize * 0.5f;
            }
            else if (style.flexDirection == FlexDirection.RowReverse)
            {
                rect.xMin = visualElement.layout.xMin - splitSize * 0.5f;
                rect.xMax = visualElement.layout.xMin + splitSize * 0.5f;
            }
            else if (style.flexDirection == FlexDirection.Column)
            {
                rect.yMin = visualElement.layout.yMax - splitSize * 0.5f;
                rect.yMax = visualElement.layout.yMax + splitSize * 0.5f;
            }
            else if (style.flexDirection == FlexDirection.ColumnReverse)
            {
                rect.yMin = visualElement.layout.yMin - splitSize * 0.5f;
                rect.yMax = visualElement.layout.yMin + splitSize * 0.5f;
            }

            return rect;
        }
    }
}
