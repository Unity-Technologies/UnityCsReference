// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class VisualSplitter : ImmediateModeElement
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
                    FlexDirection flexDirection = visualSplitter.resolvedStyle.flexDirection;

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

                    FlexDirection flexDirection = visualSplitter.resolvedStyle.flexDirection;
                    bool isVertical = flexDirection == FlexDirection.Column || flexDirection == FlexDirection.ColumnReverse;

                    float relativeMousePosition;
                    if (isVertical)
                    {
                        float minHeight = visualElement.resolvedStyle.minHeight == StyleKeyword.Auto ? 0 : visualElement.resolvedStyle.minHeight.value;
                        float nextMinHeight = nextVisualElement.resolvedStyle.minHeight == StyleKeyword.Auto ? 0 : nextVisualElement.resolvedStyle.minHeight.value;
                        float availableHeight = visualElement.layout.height + nextVisualElement.layout.height - minHeight - nextMinHeight;
                        float maxHeight = visualElement.resolvedStyle.maxHeight.value <= 0 ? availableHeight : visualElement.resolvedStyle.maxHeight.value;

                        relativeMousePosition = (Math.Min(e.localMousePosition.y, visualElement.layout.yMin + maxHeight) - visualElement.layout.yMin - minHeight) / availableHeight;
                    }
                    else
                    {
                        float minWidth = visualElement.resolvedStyle.minWidth == StyleKeyword.Auto ? 0 : visualElement.resolvedStyle.minWidth.value;
                        float nextMinWidth = nextVisualElement.resolvedStyle.minWidth == StyleKeyword.Auto ? 0 : nextVisualElement.resolvedStyle.minWidth.value;
                        float availableWidth = visualElement.layout.width + nextVisualElement.layout.width - minWidth - nextMinWidth;
                        float maxWidth = visualElement.resolvedStyle.maxWidth.value <= 0 ? availableWidth : visualElement.resolvedStyle.maxWidth.value;

                        relativeMousePosition = (Math.Min(e.localMousePosition.x, visualElement.layout.xMin + maxWidth) - visualElement.layout.xMin - minWidth) / availableWidth;
                    }

                    relativeMousePosition = Math.Max(0.0f, Math.Min(0.999f, relativeMousePosition));

                    float totalFlex = visualElement.resolvedStyle.flexGrow + nextVisualElement.resolvedStyle.flexGrow;
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

        public static readonly string ussClassName = "unity-visual-splitter";

        public VisualSplitter()
        {
            AddToClassList(ussClassName);
            this.AddManipulator(new SplitManipulator());
        }

        public List<VisualElement> GetAffectedVisualElements()
        {
            List<VisualElement> elements = VisualElementListPool.Get();
            var count = hierarchy.childCount;
            for (int i = 0; i < count; ++i)
            {
                VisualElement element = hierarchy[i];
                if (element.resolvedStyle.position == Position.Relative)
                    elements.Add(element);
            }

            return elements;
        }

        protected override void ImmediateRepaint()
        {
            UpdateCursorRects();
        }

        void UpdateCursorRects()
        {
            var count = hierarchy.childCount;
            for (int i = 0; i < count - 1; ++i)
            {
                VisualElement visualElement = hierarchy[i];
                bool isVertical = resolvedStyle.flexDirection == FlexDirection.Column || resolvedStyle.flexDirection == FlexDirection.ColumnReverse;

                EditorGUIUtility.AddCursorRect(GetSplitterRect(visualElement), isVertical ? MouseCursor.ResizeVertical : MouseCursor.SplitResizeLeftRight);
            }
        }

        public Rect GetSplitterRect(VisualElement visualElement)
        {
            Rect rect = visualElement.layout;
            if (resolvedStyle.flexDirection == FlexDirection.Row)
            {
                rect.xMin = visualElement.layout.xMax - splitSize * 0.5f;
                rect.xMax = visualElement.layout.xMax + splitSize * 0.5f;
            }
            else if (resolvedStyle.flexDirection == FlexDirection.RowReverse)
            {
                rect.xMin = visualElement.layout.xMin - splitSize * 0.5f;
                rect.xMax = visualElement.layout.xMin + splitSize * 0.5f;
            }
            else if (resolvedStyle.flexDirection == FlexDirection.Column)
            {
                rect.yMin = visualElement.layout.yMax - splitSize * 0.5f;
                rect.yMax = visualElement.layout.yMax + splitSize * 0.5f;
            }
            else if (resolvedStyle.flexDirection == FlexDirection.ColumnReverse)
            {
                rect.yMin = visualElement.layout.yMin - splitSize * 0.5f;
                rect.yMax = visualElement.layout.yMin + splitSize * 0.5f;
            }

            return rect;
        }
    }
}
