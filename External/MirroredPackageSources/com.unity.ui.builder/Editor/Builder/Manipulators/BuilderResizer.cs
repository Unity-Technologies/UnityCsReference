using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderResizer : BuilderTransformer
    {
        static readonly string s_UssClassName = "unity-builder-resizer";
        static readonly int s_HighlightHandleOnInspectorChangeDelayMS = 250;

        IVisualElementScheduledItem m_UndoWidthHighlightScheduledItem;
        IVisualElementScheduledItem m_UndoHeightHighlightScheduledItem;

        Dictionary<string, VisualElement> m_HandleElements;

        public new class UxmlFactory : UxmlFactory<BuilderResizer, UxmlTraits> {}

        public BuilderResizer()
        {
            var builderTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Manipulators/BuilderResizer.uxml");
            builderTemplate.CloneTree(this);

            AddToClassList(s_UssClassName);

            m_HandleElements = new Dictionary<string, VisualElement>();

            m_HandleElements.Add("top-handle", this.Q("top-handle"));
            m_HandleElements.Add("left-handle", this.Q("left-handle"));
            m_HandleElements.Add("bottom-handle", this.Q("bottom-handle"));
            m_HandleElements.Add("right-handle", this.Q("right-handle"));

            m_HandleElements.Add("top-left-handle", this.Q("top-left-handle"));
            m_HandleElements.Add("top-right-handle", this.Q("top-right-handle"));

            m_HandleElements.Add("bottom-left-handle", this.Q("bottom-left-handle"));
            m_HandleElements.Add("bottom-right-handle", this.Q("bottom-right-handle"));

            m_HandleElements["top-handle"].AddManipulator(new Manipulator(base.OnStartDrag, base.OnEndDrag, OnDragTop));
            m_HandleElements["left-handle"].AddManipulator(new Manipulator(base.OnStartDrag, base.OnEndDrag, OnDragLeft));
            m_HandleElements["bottom-handle"].AddManipulator(new Manipulator(base.OnStartDrag, base.OnEndDrag, OnDragBottom));
            m_HandleElements["right-handle"].AddManipulator(new Manipulator(base.OnStartDrag, base.OnEndDrag, OnDragRight));

            m_HandleElements["top-left-handle"].AddManipulator(new Manipulator(base.OnStartDrag, base.OnEndDrag, OnDragTopLeft));
            m_HandleElements["top-right-handle"].AddManipulator(new Manipulator(base.OnStartDrag, base.OnEndDrag, OnDragTopRight));

            m_HandleElements["bottom-left-handle"].AddManipulator(new Manipulator(base.OnStartDrag, base.OnEndDrag, OnDragBottomLeft));
            m_HandleElements["bottom-right-handle"].AddManipulator(new Manipulator(base.OnStartDrag, base.OnEndDrag, OnDragBottomRight));

            base.m_AbsoluteOnlyHandleElements.Add(m_HandleElements["top-handle"]);
            base.m_AbsoluteOnlyHandleElements.Add(m_HandleElements["left-handle"]);
            base.m_AbsoluteOnlyHandleElements.Add(m_HandleElements["top-left-handle"]);
            base.m_AbsoluteOnlyHandleElements.Add(m_HandleElements["top-right-handle"]);
            base.m_AbsoluteOnlyHandleElements.Add(m_HandleElements["bottom-left-handle"]);

            m_UndoWidthHighlightScheduledItem = this.schedule.Execute(UndoWidthHighlight);
            m_UndoWidthHighlightScheduledItem.Pause();
            m_UndoHeightHighlightScheduledItem = this.schedule.Execute(UndoHeightHighlight);
            m_UndoHeightHighlightScheduledItem.Pause();
        }

        void OnDrag(
            TrackedStyle primaryStyle,
            float onStartDragLength,
            float onStartDragPrimary,
            float delta,
            List<string> changeList)
        {
            var oppositeStyle = GetOppositeStyle(primaryStyle);
            var lengthStyle = GetLengthStyle(primaryStyle);

            // Make sure our delta is a whole number so we don't end up with non-whole pixel values.
            delta = Mathf.Ceil(delta / canvas.zoomScale);

            if (!IsNoneOrAuto(oppositeStyle) && !IsNoneOrAuto(primaryStyle))
            {
                SetStyleSheetValue(primaryStyle, onStartDragPrimary - delta);
                changeList.Add(GetStyleName(primaryStyle));
            }
            else if (IsNoneOrAuto(oppositeStyle) && !IsNoneOrAuto(primaryStyle))
            {
                SetStyleSheetValue(lengthStyle, onStartDragLength + delta);
                SetStyleSheetValue(primaryStyle, onStartDragPrimary - delta);
                changeList.Add(GetStyleName(primaryStyle));
                changeList.Add(GetStyleName(lengthStyle));
            }
            else if (!IsNoneOrAuto(oppositeStyle) && IsNoneOrAuto(primaryStyle))
            {
                SetStyleSheetValue(lengthStyle, onStartDragLength + delta);
                changeList.Add(GetStyleName(lengthStyle));
            }
            else
            {
                if (primaryStyle == TrackedStyle.Top || primaryStyle == TrackedStyle.Left)
                {
                    SetStyleSheetValue(lengthStyle, onStartDragLength + delta);
                    SetStyleSheetValue(primaryStyle, onStartDragPrimary - delta);
                    changeList.Add(GetStyleName(primaryStyle));
                    changeList.Add(GetStyleName(lengthStyle));
                }
                else
                {
                    SetStyleSheetValue(lengthStyle, onStartDragLength + delta);
                    changeList.Add(GetStyleName(lengthStyle));
                }
            }
        }

        void OnDragTop(Vector2 diff, List<string> changeList)
        {
            OnDrag(
                TrackedStyle.Top,
                m_TargetRectOnStartDrag.height,
                m_TargetRectOnStartDrag.y,
                -diff.y,
                changeList);

            style.height = Mathf.Round(m_ThisRectOnStartDrag.height - diff.y);
            style.top = Mathf.Round(m_ThisRectOnStartDrag.y + diff.y);
        }

        void OnDragLeft(Vector2 diff, List<string> changeList)
        {
            OnDrag(
                TrackedStyle.Left,
                m_TargetRectOnStartDrag.width,
                m_TargetRectOnStartDrag.x,
                -diff.x,
                changeList);

            style.width = Mathf.Round(m_ThisRectOnStartDrag.width - diff.x);
            style.left = Mathf.Round(m_ThisRectOnStartDrag.x + diff.x);
        }

        void OnDragBottom(Vector2 diff, List<string> changeList)
        {
            OnDrag(
                TrackedStyle.Bottom,
                m_TargetRectOnStartDrag.height,
                m_TargetCorrectedBottomOnStartDrag,
                diff.y,
                changeList);

            style.height = Mathf.Round(m_ThisRectOnStartDrag.height + diff.y);
        }

        void OnDragRight(Vector2 diff, List<string> changeList)
        {
            OnDrag(
                TrackedStyle.Right,
                m_TargetRectOnStartDrag.width,
                m_TargetCorrectedRightOnStartDrag,
                diff.x,
                changeList);

            style.width = Mathf.Round(m_ThisRectOnStartDrag.width + diff.x);
        }

        void NotifySelection()
        {
            m_Selection.NotifyOfStylingChange(this, m_ScratchChangeList);
            m_Selection.NotifyOfHierarchyChange(this, m_Target, BuilderHierarchyChangeType.InlineStyle);
        }

        void OnDragTop(Vector2 diff)
        {
            m_ScratchChangeList.Clear();
            OnDragTop(diff, m_ScratchChangeList);
            NotifySelection();
        }

        void OnDragLeft(Vector2 diff)
        {
            m_ScratchChangeList.Clear();
            OnDragLeft(diff, m_ScratchChangeList);
            NotifySelection();
        }

        void OnDragBottom(Vector2 diff)
        {
            m_ScratchChangeList.Clear();
            OnDragBottom(diff, m_ScratchChangeList);
            NotifySelection();
        }

        void OnDragRight(Vector2 diff)
        {
            m_ScratchChangeList.Clear();
            OnDragRight(diff, m_ScratchChangeList);
            NotifySelection();
        }

        void OnDragTopLeft(Vector2 diff)
        {
            m_ScratchChangeList.Clear();
            OnDragTop(diff, m_ScratchChangeList);
            OnDragLeft(diff, m_ScratchChangeList);
            NotifySelection();
        }

        void OnDragTopRight(Vector2 diff)
        {
            m_ScratchChangeList.Clear();
            OnDragTop(diff, m_ScratchChangeList);
            OnDragRight(diff, m_ScratchChangeList);
            NotifySelection();
        }

        void OnDragBottomLeft(Vector2 diff)
        {
            m_ScratchChangeList.Clear();
            OnDragBottom(diff, m_ScratchChangeList);
            OnDragLeft(diff, m_ScratchChangeList);
            NotifySelection();
        }

        void OnDragBottomRight(Vector2 diff)
        {
            m_ScratchChangeList.Clear();
            OnDragBottom(diff, m_ScratchChangeList);
            OnDragRight(diff, m_ScratchChangeList);
            NotifySelection();
        }

        void UndoWidthHighlight()
        {
            m_HandleElements["left-handle"].pseudoStates &= ~PseudoStates.Hover;
            m_HandleElements["right-handle"].pseudoStates &= ~PseudoStates.Hover;
        }

        void UndoHeightHighlight()
        {
            m_HandleElements["top-handle"].pseudoStates &= ~PseudoStates.Hover;
            m_HandleElements["bottom-handle"].pseudoStates &= ~PseudoStates.Hover;
        }

        public override void StylingChanged(List<string> styles, BuilderStylingChangeType changeType)
        {
            if (m_Target == null)
                return;

            base.StylingChanged(styles, changeType);

            if (styles == null)
                return;

            if (styles.Contains("width"))
            {
                if (IsNoneOrAuto(TrackedStyle.Left) && !IsNoneOrAuto(TrackedStyle.Right))
                    m_HandleElements["left-handle"].pseudoStates |= PseudoStates.Hover;
                else if (!IsNoneOrAuto(TrackedStyle.Left) && IsNoneOrAuto(TrackedStyle.Right))
                    m_HandleElements["right-handle"].pseudoStates |= PseudoStates.Hover;
                else
                {
                    m_HandleElements["left-handle"].pseudoStates |= PseudoStates.Hover;
                    m_HandleElements["right-handle"].pseudoStates |= PseudoStates.Hover;
                }
                m_UndoWidthHighlightScheduledItem.ExecuteLater(s_HighlightHandleOnInspectorChangeDelayMS);
            }

            if (styles.Contains("height"))
            {
                if (IsNoneOrAuto(TrackedStyle.Top) && !IsNoneOrAuto(TrackedStyle.Bottom))
                    m_HandleElements["top-handle"].pseudoStates |= PseudoStates.Hover;
                else if (!IsNoneOrAuto(TrackedStyle.Top) && IsNoneOrAuto(TrackedStyle.Bottom))
                    m_HandleElements["bottom-handle"].pseudoStates |= PseudoStates.Hover;
                else
                {
                    m_HandleElements["top-handle"].pseudoStates |= PseudoStates.Hover;
                    m_HandleElements["bottom-handle"].pseudoStates |= PseudoStates.Hover;
                }
                m_UndoHeightHighlightScheduledItem.ExecuteLater(s_HighlightHandleOnInspectorChangeDelayMS);
            }
        }
    }
}
