// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderResizer : BuilderTransformer
    {
        static readonly string s_UssClassName = "unity-builder-resizer";
        public static readonly string s_CursorSetterUssClassName = s_UssClassName + "__cursor-setter";
        static readonly string s_TrackedStylesProperty = "TrackedStyles";
        static readonly int s_HighlightHandleOnInspectorChangeDelayMS = 250;

        IVisualElementScheduledItem m_UndoWidthHighlightScheduledItem;
        IVisualElementScheduledItem m_UndoHeightHighlightScheduledItem;

        Dictionary<string, VisualElement> m_HandleElements = new();

        // Used in tests
        public Dictionary<string, VisualElement> handleElements => m_HandleElements;

        [Serializable]
        public new class UxmlSerializedData : BuilderTransformer.UxmlSerializedData
        {
            public override object CreateInstance() => new BuilderResizer();
        }

        public BuilderResizer()
        {
            var builderTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Manipulators/BuilderResizer.uxml");
            builderTemplate.CloneTree(this);

            AddToClassList(s_UssClassName);

            // Add side handles
            AddHandle("top-handle", true, TrackedStyles.Top | TrackedStyles.Height, OnStartDrag, OnEndDrag, OnDragTop);
            AddHandle("left-handle", true, TrackedStyles.Left | TrackedStyles.Width, OnStartDrag, OnEndDrag, OnDragLeft);
            AddHandle("bottom-handle", false, TrackedStyles.Bottom | TrackedStyles.Height, OnStartDrag, OnEndDrag, OnDragBottom);
            AddHandle("right-handle", false, TrackedStyles.Right | TrackedStyles.Width, OnStartDrag, OnEndDrag, OnDragRight);

            // Add corner handles
            AddHandle("top-left-handle", true, TrackedStyles.Left | TrackedStyles.Top | TrackedStyles.Width | TrackedStyles.Height, OnStartDrag, OnEndDrag, OnDragTopLeft);
            AddHandle("top-right-handle", true, TrackedStyles.Right | TrackedStyles.Top | TrackedStyles.Height | TrackedStyles.Width, OnStartDrag, OnEndDrag, OnDragTopRight);
            AddHandle("bottom-left-handle", true, TrackedStyles.Left | TrackedStyles.Bottom | TrackedStyles.Width | TrackedStyles.Height, OnStartDrag, OnEndDrag, OnDragBottomLeft);
            AddHandle("bottom-right-handle", false, TrackedStyles.Right | TrackedStyles.Bottom | TrackedStyles.Width | TrackedStyles.Height, OnStartDrag, OnEndDrag, OnDragBottomRight);

            m_UndoWidthHighlightScheduledItem = this.schedule.Execute(UndoWidthHighlight);
            m_UndoWidthHighlightScheduledItem.Pause();
            m_UndoHeightHighlightScheduledItem = this.schedule.Execute(UndoHeightHighlight);
            m_UndoHeightHighlightScheduledItem.Pause();
        }

        private void AddHandle(string handleName, bool absolute, TrackedStyles trackedStyles, Action<VisualElement> startDrag, Action endDrag, Action<Vector2> dragAction)
        {
            var handle = this.Q(handleName);

            m_HandleElements.Add(handleName, handle);
            if (absolute)
                m_AbsoluteOnlyHandleElements.Add(handle);
            handle.AddManipulator(new Manipulator(startDrag, endDrag, dragAction));
            handle.SetProperty(s_TrackedStylesProperty, trackedStyles);
        }

        protected override void UpdateBoundStyles()
        {
            base.UpdateBoundStyles();

            void UpdateHandleFromBindings(VisualElement handle)
            {
                var handleName = handle.name;
                var trackedStyles = (TrackedStyles)handle.GetProperty(s_TrackedStylesProperty);
                var handleTooltip = string.Empty;
                var boundTrackedStyles = m_BoundStyles & trackedStyles;
                var bound = boundTrackedStyles != 0;

                m_HandleElements[handleName].EnableInClassList(s_DisabledHandleClassName, bound);

                if (bound)
                {
                    var asText = boundTrackedStyles.ToString().ToLower();

                    handleTooltip = string.Format(BuilderConstants.CannotResizeBecauseOfBoundPropertiesMessage, asText);
                }
                m_HandleElements[handleName].Q(className: s_CursorSetterUssClassName).tooltip = handleTooltip;
            }

            foreach (var handlePair in m_HandleElements)
            {
                UpdateHandleFromBindings(handlePair.Value);
            }
        }

        void OnDrag(
            TrackedStyles primaryStyle,
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
                if (primaryStyle == TrackedStyles.Top || primaryStyle == TrackedStyles.Left)
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

        protected override void OnEndDrag()
        {
            base.OnEndDrag();
            NotifySelection(false);
        }

        void OnDragTop(Vector2 diff, List<string> changeList)
        {
            OnDrag(
                TrackedStyles.Top,
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
                TrackedStyles.Left,
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
                TrackedStyles.Bottom,
                m_TargetRectOnStartDrag.height,
                m_TargetCorrectedBottomOnStartDrag,
                diff.y,
                changeList);

            style.height = Mathf.Round(m_ThisRectOnStartDrag.height + diff.y);
        }

        void OnDragRight(Vector2 diff, List<string> changeList)
        {
            OnDrag(
                TrackedStyles.Right,
                m_TargetRectOnStartDrag.width,
                m_TargetCorrectedRightOnStartDrag,
                diff.x,
                changeList);

            style.width = Mathf.Round(m_ThisRectOnStartDrag.width + diff.x);
        }

        void NotifySelection(bool refreshOnly = true)
        {
            var styleChangeType = refreshOnly ? BuilderStylingChangeType.RefreshOnly : BuilderStylingChangeType.Default;

            m_Selection.NotifyOfStylingChange(this, m_ScratchChangeList, styleChangeType);
            if (!refreshOnly)
                m_Selection.NotifyOfHierarchyChange(this, m_Target, BuilderHierarchyChangeType.InlineStyle | BuilderHierarchyChangeType.FullRefresh);
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
                if (IsNoneOrAuto(TrackedStyles.Left) && !IsNoneOrAuto(TrackedStyles.Right))
                    m_HandleElements["left-handle"].pseudoStates |= PseudoStates.Hover;
                else if (!IsNoneOrAuto(TrackedStyles.Left) && IsNoneOrAuto(TrackedStyles.Right))
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
                if (IsNoneOrAuto(TrackedStyles.Top) && !IsNoneOrAuto(TrackedStyles.Bottom))
                    m_HandleElements["top-handle"].pseudoStates |= PseudoStates.Hover;
                else if (!IsNoneOrAuto(TrackedStyles.Top) && IsNoneOrAuto(TrackedStyles.Bottom))
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
