using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderTracker : VisualElement, IBuilderSelectionNotifier
    {
        static readonly string s_UssClassName = "unity-builder-tracker";
        protected static readonly string s_ActiveClassName = "unity-builder-tracker--active";

        protected VisualElement m_Target;
        BuilderCanvas m_Canvas;

        public BuilderCanvas canvas => m_Canvas;

        public BuilderTracker()
        {
            m_Target = null;

            AddToClassList(s_UssClassName);
        }

        public virtual void Activate(VisualElement target)
        {
            if (m_Target == target)
                return;

            if (m_Target != null)
                Deactivate();

            if (target == null)
                return;

            m_Target = target;

            AddToClassList(s_ActiveClassName);

            m_Target.RegisterCallback<GeometryChangedEvent>(OnExternalTargetResize);
            m_Target.RegisterCallback<DetachFromPanelEvent>(OnTargetDeletion);

            m_Canvas = m_Target.GetFirstAncestorOfType<BuilderCanvas>();
            m_Canvas.RegisterCallback<GeometryChangedEvent>(OnCanvasResize);

            if (float.IsNaN(m_Target.layout.width))
            {
                m_Target.RegisterCallback<GeometryChangedEvent>(OnInitialStylesResolved);
            }
            else
            {
                SetStylesFromTargetStyles();
                ResizeSelfFromTarget(m_Target.rect);
            }
        }

        public virtual void Deactivate()
        {
            if (m_Target == null)
                return;

            m_Target.UnregisterCallback<GeometryChangedEvent>(OnExternalTargetResize);
            m_Target.UnregisterCallback<DetachFromPanelEvent>(OnTargetDeletion);
            m_Canvas?.UnregisterCallback<GeometryChangedEvent>(OnCanvasResize);

            m_Target = null;
            m_Canvas = null;

            RemoveFromClassList(s_ActiveClassName);
        }

        void OnInitialStylesResolved(GeometryChangedEvent evt)
        {
            SetStylesFromTargetStyles();
            if (m_Target != null)
                m_Target.UnregisterCallback<GeometryChangedEvent>(OnInitialStylesResolved);
        }

        protected virtual void SetStylesFromTargetStyles()
        {}

        void OnExternalTargetResize(GeometryChangedEvent evt)
        {
            ResizeSelfFromTarget(m_Target.rect);
        }

        void OnCanvasResize(GeometryChangedEvent evt)
        {
            if (m_Target == null)
                return;

            ResizeSelfFromTarget(m_Target.rect);
        }

        void OnTargetDeletion(DetachFromPanelEvent evt)
        {
            Deactivate();
        }

        protected void ResizeSelfFromTarget(Rect targetRect)
        {
            var targetMarginTop = m_Target.resolvedStyle.marginTop;
            var targetMarginLeft = m_Target.resolvedStyle.marginLeft;
            var targetMarginRight = m_Target.resolvedStyle.marginRight;
            var targetMarginBottom = m_Target.resolvedStyle.marginBottom;

            targetRect.y -= targetMarginTop;
            targetRect.x -= targetMarginLeft;
            targetRect.width = targetRect.width + (targetMarginLeft + targetMarginRight);
            targetRect.height = targetRect.height + (targetMarginTop + targetMarginBottom);

            var selfRect = m_Target.ChangeCoordinatesTo(this.hierarchy.parent, targetRect);

            var top = selfRect.y;
            var left = selfRect.x;
            var width = selfRect.width;
            var height = selfRect.height;

            style.top = top - resolvedStyle.borderTopWidth;
            style.left = left - resolvedStyle.borderLeftWidth;
            style.width = width + resolvedStyle.borderLeftWidth + resolvedStyle.borderRightWidth;
            style.height = height + resolvedStyle.borderTopWidth + resolvedStyle.borderBottomWidth;
        }

        public void SelectionChanged()
        {

        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            if (m_Target == null)
                return;

            if (!changeType.HasFlag(BuilderHierarchyChangeType.InlineStyle))
                return;

            SetStylesFromTargetStyles();
            ResizeSelfFromTarget(m_Target.rect);
        }

        public virtual void StylingChanged(List<string> styles, BuilderStylingChangeType changeType)
        {
            if (m_Target == null)
                return;

            SetStylesFromTargetStyles();
            ResizeSelfFromTarget(m_Target.rect);
        }
    }
}
