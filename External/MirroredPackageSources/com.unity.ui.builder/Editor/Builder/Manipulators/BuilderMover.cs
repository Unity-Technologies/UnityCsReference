using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderMover : BuilderTransformer
    {
        static readonly string s_UssClassName = "unity-builder-mover";

        Manipulator m_MoveManipulator;

        public BuilderParentTracker parentTracker { get; set; }

        public new class UxmlFactory : UxmlFactory<BuilderMover, UxmlTraits> {}

        public BuilderMover()
        {
            AddToClassList(s_UssClassName);

            m_MoveManipulator = new Manipulator(OnStartDrag, OnEndDrag, OnMove);
        }

        public override void Deactivate()
        {
            base.Deactivate();
            this.RemoveManipulator(m_MoveManipulator);
        }

        protected override void SetStylesFromTargetStyles()
        {
            base.SetStylesFromTargetStyles();

            if (m_Target == null || m_Target.resolvedStyle.position == Position.Relative)
            {
                this.style.cursor = StyleKeyword.None;
            }
            else
            {
                this.style.cursor = StyleKeyword.Null;
                this.AddManipulator(m_MoveManipulator);
            }
        }

        new void OnStartDrag(VisualElement handle)
        {
            base.OnStartDrag(handle);

            if (parentTracker == null)
                return;

            parentTracker.Activate(m_Target.parent);
        }

        new void OnEndDrag()
        {
            base.OnEndDrag();

            if (parentTracker == null)
                return;

            parentTracker.Deactivate();
        }

        void OnMove(
            TrackedStyle trackedStyle,
            bool force,
            float onStartDragPrimary,
            float delta,
            List<string> changeList)
        {
            if (IsNoneOrAuto(trackedStyle) && !force)
                return;

            // Make sure our delta is a whole number so we don't end up with non-whole pixel values.
            delta = Mathf.Ceil(delta / canvas.zoomScale);

            var styleName = GetStyleName(trackedStyle);
            SetStyleSheetValue(styleName, onStartDragPrimary + delta);
            m_ScratchChangeList.Add(styleName);
        }

        void OnMove(Vector2 diff)
        {
            m_ScratchChangeList.Clear();

            bool forceTop = IsNoneOrAuto(TrackedStyle.Top) && IsNoneOrAuto(TrackedStyle.Bottom);
            bool forceLeft = IsNoneOrAuto(TrackedStyle.Left) && IsNoneOrAuto(TrackedStyle.Right);

            OnMove(TrackedStyle.Top, forceTop, m_TargetRectOnStartDrag.y, diff.y, m_ScratchChangeList);
            OnMove(TrackedStyle.Right, false, m_TargetCorrectedRightOnStartDrag, -diff.x, m_ScratchChangeList);
            OnMove(TrackedStyle.Left, forceLeft, m_TargetRectOnStartDrag.x, diff.x, m_ScratchChangeList);
            OnMove(TrackedStyle.Bottom, false, m_TargetCorrectedBottomOnStartDrag, -diff.y, m_ScratchChangeList);

            style.left = Mathf.Round(m_ThisRectOnStartDrag.x + diff.x);
            style.top = Mathf.Round(m_ThisRectOnStartDrag.y + diff.y);

            m_Selection.NotifyOfStylingChange(this, m_ScratchChangeList);
            m_Selection.NotifyOfHierarchyChange(this, m_Target, BuilderHierarchyChangeType.InlineStyle);
        }
    }
}
