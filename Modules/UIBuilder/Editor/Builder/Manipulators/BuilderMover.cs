// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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

        [Serializable]
        public new class UxmlSerializedData : BuilderTransformer.UxmlSerializedData
        {
            public override object CreateInstance() => new BuilderMover();
        }

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

            parentTracker?.Activate(m_Target.parent);
        }

        new void OnEndDrag()
        {
            base.OnEndDrag();

            parentTracker?.Deactivate();

            m_Selection.NotifyOfStylingChange(this, m_ScratchChangeList);
            m_Selection.NotifyOfHierarchyChange(this, m_Target, BuilderHierarchyChangeType.InlineStyle | BuilderHierarchyChangeType.FullRefresh);
        }

        void OnMove(
            TrackedStyles trackedStyle,
            bool force,
            float onStartDragPrimary,
            float delta,
            List<string> changeList)
        {
            if (IsNoneOrAuto(trackedStyle) && !force)
                return;

            // Make sure our delta is a whole number so we don't end up with non-whole pixel values.
            delta = Mathf.Ceil(delta / canvas.zoomScale);

            SetStyleSheetValue(trackedStyle, onStartDragPrimary + delta);

            var styleName = GetStyleName(trackedStyle);
            m_ScratchChangeList.Add(styleName);
        }

        void OnMove(Vector2 diff)
        {
            m_ScratchChangeList.Clear();

            bool forceTop = IsNoneOrAuto(TrackedStyles.Top) && IsNoneOrAuto(TrackedStyles.Bottom);
            bool forceLeft = IsNoneOrAuto(TrackedStyles.Left) && IsNoneOrAuto(TrackedStyles.Right);

            // Do not move vertically if either top or bottom is bound
            if (!AreStylesBound(TrackedStyles.Top | TrackedStyles.Bottom))
            {
                OnMove(TrackedStyles.Top, forceTop, m_TargetRectOnStartDrag.y, diff.y, m_ScratchChangeList);
                OnMove(TrackedStyles.Bottom, false, m_TargetCorrectedBottomOnStartDrag, -diff.y, m_ScratchChangeList);
                style.top = Mathf.Round(m_ThisRectOnStartDrag.y + diff.y);
            }
            // Do not move horizontally if either left or right is bound
            if (!AreStylesBound(TrackedStyles.Left | TrackedStyles.Right))
            {
                OnMove(TrackedStyles.Right, false, m_TargetCorrectedRightOnStartDrag, -diff.x, m_ScratchChangeList);
                OnMove(TrackedStyles.Left, forceLeft, m_TargetRectOnStartDrag.x, diff.x, m_ScratchChangeList);
                style.left = Mathf.Round(m_ThisRectOnStartDrag.x + diff.x);
            }

            if (m_ScratchChangeList.Count > 0)
            {
                m_Selection.NotifyOfStylingChange(this, m_ScratchChangeList, BuilderStylingChangeType.RefreshOnly);
                m_Selection.NotifyOfHierarchyChange(this, m_Target, BuilderHierarchyChangeType.InlineStyle);
            }
        }
    }
}
