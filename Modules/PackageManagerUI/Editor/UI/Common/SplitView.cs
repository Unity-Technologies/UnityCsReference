// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class SplitView : VisualElement
    {
        private static readonly string s_ClassName = "split-view";
        private static readonly string s_ContentContainerClassName = "split-view-content";
        private static readonly string s_HandleDragLineClassName = "split-view-dragline";
        private static readonly string s_HandleDragLineAnchorClassName = "split-view-dragline-anchor";

        public new class UxmlFactory : UxmlFactory<SplitView, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private UxmlIntAttributeDescription m_LeftPaneInitialSize = new UxmlIntAttributeDescription { name = "fixed-pane-initial-size", defaultValue = 100 };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var fixedPaneInitialSize = m_LeftPaneInitialSize.GetValueFromBag(bag, cc);
                ((SplitView)ve).Init(fixedPaneInitialSize);
            }
        }

        public event Action<float> onSizeChanged = delegate {};

        private VisualElement m_LeftPane;
        private VisualElement m_RightPane;
        private VisualElement m_DragLine;
        private VisualElement m_DragLineAnchor;
        private VisualElement m_Content;

        private float m_MinLeftWidth;
        private float m_MaxLeftWidth;
        private float m_MinRightWidth;
        private float m_InitialLeftWidth;

        private AnchorManipulator m_AnchorManipulator;

        public SplitView()
        {
            AddToClassList(s_ClassName);

            m_Content = new VisualElement {name = "splitview-content"};
            m_Content.AddToClassList(s_ContentContainerClassName);
            hierarchy.Add(m_Content);

            // Create drag anchor line.
            m_DragLineAnchor = new VisualElement {name = "splitview-dragline-anchor"};
            m_DragLineAnchor.AddToClassList(s_HandleDragLineAnchorClassName);
            hierarchy.Add(m_DragLineAnchor);

            // Create drag
            m_DragLine = new VisualElement {name = "splitview-dragline"};
            m_DragLine.AddToClassList(s_HandleDragLineClassName);
            m_DragLineAnchor.Add(m_DragLine);
        }

        public SplitView(float fixedPaneStartDimension) : this()
        {
            Init(fixedPaneStartDimension);
        }

        private float m_LeftWidth;
        public float leftWidth
        {
            get { return m_LeftWidth; }
            set { m_LeftWidth = value; }
        }

        private void Init(float fixedPaneInitialDimension)
        {
            m_MinLeftWidth = 0;
            m_MaxLeftWidth = float.MaxValue;
            m_MinRightWidth = 0;
            m_InitialLeftWidth = fixedPaneInitialDimension;

            style.minWidth = m_InitialLeftWidth;

            if (m_AnchorManipulator != null)
            {
                m_DragLineAnchor.RemoveManipulator(m_AnchorManipulator);
                m_AnchorManipulator = null;
            }

            if (m_Content.childCount != 2)
                RegisterCallback<GeometryChangedEvent>(OnPostDisplaySetup);
            else
                PostDisplaySetup();
        }

        private void OnPostDisplaySetup(GeometryChangedEvent evt)
        {
            if (m_Content.childCount != 2)
                return;

            PostDisplaySetup();

            UnregisterCallback<GeometryChangedEvent>(OnPostDisplaySetup);
            RegisterCallback<GeometryChangedEvent>(OnSizeChange);
        }

        private void PostDisplaySetup()
        {
            if (m_Content.childCount != 2)
                return;

            m_LeftPane = m_Content[0];
            m_MinLeftWidth = m_LeftPane.resolvedStyle.minWidth.value;
            m_MaxLeftWidth = m_LeftPane.resolvedStyle.maxWidth.value;
            if (m_MaxLeftWidth <= m_MinLeftWidth)
                m_MaxLeftWidth = float.MaxValue;

            m_InitialLeftWidth = Mathf.Max(m_InitialLeftWidth, m_MinLeftWidth);
            if (m_LeftWidth == 0)
                m_LeftWidth = m_InitialLeftWidth;

            m_LeftPane.style.flexShrink = 0;
            m_LeftPane.style.width = m_LeftWidth;

            m_RightPane = m_Content[1];
            m_MinRightWidth = m_RightPane.resolvedStyle.minWidth.value;

            m_RightPane.style.flexGrow = 1;
            m_RightPane.style.flexShrink = 0;
            m_RightPane.style.flexBasis = 0;

            m_DragLineAnchor.style.left = m_LeftWidth;

            m_AnchorManipulator = new AnchorManipulator(this);
            m_DragLineAnchor.AddManipulator(m_AnchorManipulator);

            onSizeChanged?.Invoke(m_LeftWidth);

            UnregisterCallback<GeometryChangedEvent>(OnPostDisplaySetup);
            RegisterCallback<GeometryChangedEvent>(OnSizeChange);
        }

        private void OnSizeChange(GeometryChangedEvent evt)
        {
            var maxLength = resolvedStyle.width;
            var dragLinePos = m_DragLineAnchor.resolvedStyle.left;
            if (dragLinePos > maxLength)
            {
                var delta = maxLength - dragLinePos;
                m_AnchorManipulator.ApplyDelta(delta);
            }
        }

        public override VisualElement contentContainer => m_Content;

        private class AnchorManipulator : MouseManipulator
        {
            private Vector2 m_Start;
            private bool m_Active;
            private SplitView m_SplitView;

            public AnchorManipulator(SplitView splitView)
            {
                m_SplitView = splitView;
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
                m_Active = false;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<MouseDownEvent>(OnMouseDown);
                target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            }

            public void ApplyDelta(float delta)
            {
                var oldLeftDimension = m_SplitView.m_LeftPane.resolvedStyle.width;
                var newLeftDimension = oldLeftDimension + delta;
                if (newLeftDimension < oldLeftDimension && newLeftDimension < m_SplitView.m_MinLeftWidth)
                    newLeftDimension = m_SplitView.m_MinLeftWidth;

                var maxLength = m_SplitView.resolvedStyle.width - m_SplitView.m_MinRightWidth;
                maxLength = Mathf.Min(m_SplitView.m_MaxLeftWidth, maxLength);
                if (newLeftDimension > oldLeftDimension && newLeftDimension > maxLength)
                    newLeftDimension = maxLength;

                if (newLeftDimension != oldLeftDimension)
                {
                    m_SplitView.m_LeftPane.style.width = newLeftDimension;
                    target.style.left = newLeftDimension;
                    m_SplitView.onSizeChanged?.Invoke(newLeftDimension);
                }
            }

            private void OnMouseDown(MouseDownEvent e)
            {
                if (m_Active)
                {
                    e.StopImmediatePropagation();
                    return;
                }

                if (CanStartManipulation(e))
                {
                    m_Start = e.localMousePosition;
                    m_Active = true;
                    target.CaptureMouse();
                    e.StopPropagation();
                }
            }

            private void OnMouseMove(MouseMoveEvent e)
            {
                if (!m_Active || !target.HasMouseCapture())
                    return;

                ApplyDelta((e.localMousePosition - m_Start).x);
                e.StopPropagation();
            }

            private void OnMouseUp(MouseUpEvent e)
            {
                if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(e))
                    return;

                m_Active = false;
                target.ReleaseMouse();
                e.StopPropagation();
            }
        }
    }
}
