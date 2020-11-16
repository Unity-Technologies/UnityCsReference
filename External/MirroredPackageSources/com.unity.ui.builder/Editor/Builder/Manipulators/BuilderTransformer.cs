using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderTransformer : BuilderManipulator
    {
        static readonly string s_UssClassName = "unity-builder-transformer";
        static readonly string s_ActiveHandleClassName = "unity-builder-transformer--active";

        protected List<string> m_ScratchChangeList;

        VisualElement m_DragHoverCoverLayer;

        protected float m_TargetCorrectedBottomOnStartDrag;
        protected float m_TargetCorrectedRightOnStartDrag;
        protected Rect m_TargetRectOnStartDrag;
        protected Rect m_ThisRectOnStartDrag;

        public BuilderTransformer()
        {
            var builderTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Manipulators/BuilderTransformer.uxml");
            builderTemplate.CloneTree(this);

            AddToClassList(s_UssClassName);

            m_ScratchChangeList = new List<string>();

            m_DragHoverCoverLayer = this.Q("drag-hover-cover-layer");
        }

        protected void OnStartDrag(VisualElement handle)
        {
            m_TargetRectOnStartDrag = m_Target.layout;
            m_ThisRectOnStartDrag = this.layout;

            // Adjust for margins.
            var targetMarginTop = m_Target.resolvedStyle.marginTop;
            var targetMarginLeft = m_Target.resolvedStyle.marginLeft;
            var targetMarginBottom = m_Target.resolvedStyle.marginBottom;
            var targetMarginRight = m_Target.resolvedStyle.marginRight;
            m_TargetRectOnStartDrag.y -= targetMarginTop;
            m_TargetRectOnStartDrag.x -= targetMarginLeft;

            // Adjust for parent borders.
            var parentBorderTop = m_Target.parent.resolvedStyle.borderTopWidth;
            var parentBorderLeft = m_Target.parent.resolvedStyle.borderLeftWidth;
            var parentBorderBottom = m_Target.parent.resolvedStyle.borderBottomWidth;
            var parentBorderRight = m_Target.parent.resolvedStyle.borderRightWidth;
            m_TargetRectOnStartDrag.y -= parentBorderTop;
            m_TargetRectOnStartDrag.x -= parentBorderLeft;

            var parentRect = m_Target.parent.layout;
            m_TargetCorrectedBottomOnStartDrag =
                parentRect.height - m_TargetRectOnStartDrag.yMax - targetMarginTop - targetMarginBottom - parentBorderTop - parentBorderBottom;
            m_TargetCorrectedRightOnStartDrag =
                parentRect.width - m_TargetRectOnStartDrag.xMax - targetMarginLeft - targetMarginRight - parentBorderLeft - parentBorderRight;

            // This is a bit of a hack since the base class constructor always runs before
            // the child class' constructor, therefore, our hover overlay will be first
            // in the parent, not last. We need to make it last the first time.
            if (m_DragHoverCoverLayer.parent.IndexOf(m_DragHoverCoverLayer) != m_DragHoverCoverLayer.parent.childCount - 1)
                m_DragHoverCoverLayer.BringToFront();
            m_DragHoverCoverLayer.style.display = DisplayStyle.Flex;
            m_DragHoverCoverLayer.style.cursor = handle.computedStyle.cursor;
        }

        protected void OnEndDrag()
        {
            m_DragHoverCoverLayer.style.display = DisplayStyle.None;
            m_DragHoverCoverLayer.RemoveFromClassList(s_ActiveClassName);
        }

        protected class Manipulator : MouseManipulator
        {
            Vector2 m_Start;
            protected bool m_Active;

            Action<VisualElement> m_StartDrag;
            Action m_EndDrag;
            Action<Vector2> m_DragAction;

            public Manipulator(Action<VisualElement> startDrag, Action endDrag, Action<Vector2> dragAction)
            {
                m_StartDrag = startDrag;
                m_EndDrag = endDrag;
                m_DragAction = dragAction;
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

            protected void OnMouseDown(MouseDownEvent e)
            {
                if (m_Active)
                {
                    e.StopImmediatePropagation();
                    return;
                }

                if (CanStartManipulation(e))
                {
                    // Ignore double-click to allow text editing of the selected element
                    if (e.clickCount == 2)
                        return;

                    m_StartDrag(target);
                    m_Start = e.mousePosition;

                    m_Active = true;
                    target.CaptureMouse();
                    e.StopPropagation();

                    target.AddToClassList(s_ActiveHandleClassName);
                }
            }

            protected void OnMouseMove(MouseMoveEvent e)
            {
                if (!m_Active || !target.HasMouseCapture())
                    return;

                Vector2 diff = e.mousePosition - m_Start;

                m_DragAction(diff);

                e.StopPropagation();
            }

            protected void OnMouseUp(MouseUpEvent e)
            {
                if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(e))
                    return;

                m_Active = false;
                target.ReleaseMouse();
                e.StopPropagation();
                m_EndDrag();

                target.RemoveFromClassList(s_ActiveHandleClassName);
            }
        }

    }
}
