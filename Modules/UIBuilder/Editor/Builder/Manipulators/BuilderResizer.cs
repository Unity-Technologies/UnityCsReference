// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Resizes the target element in the UI Builder.
    /// When resizing we do 3 steps:
    ///  1. Clamp the drag value so that the height/width does not go below 0. A negative value can cause the element to go to an unexpected size.
    ///  2. Apply the clamped value to the target element.
    ///  3. Check the geometry of the target element matches the requested value. If not, we need to adjust the drag value to match the geometry. This handles elements with min/max sizes.
    /// </summary>
    class BuilderResizer : BuilderTransformer
    {
        const string k_TopHandleName = "top-handle";
        const string k_LeftHandleName = "left-handle";
        const string k_BottomHandleName = "bottom-handle";
        const string k_RightHandleName = "right-handle";
        const string k_TopLeftHandleName = "top-left-handle";
        const string k_TopRightHandleName = "top-right-handle";
        const string k_BottomLeftHandleName = "bottom-left-handle";
        const string k_BottomRightHandleName = "bottom-right-handle";

        const int k_LayoutCountLimit = UIRLayoutUpdater.kMaxValidateLayoutCount / 2;

        static readonly string s_UssClassName = "unity-builder-resizer";
        public static readonly string s_CursorSetterUssClassName = s_UssClassName + "__cursor-setter";
        static readonly string s_TrackedStylesProperty = "TrackedStyles";
        static readonly int s_HighlightHandleOnInspectorChangeDelayMS = 250;

        IVisualElementScheduledItem m_UndoWidthHighlightScheduledItem;
        IVisualElementScheduledItem m_UndoHeightHighlightScheduledItem;

        Dictionary<string, VisualElement> m_HandleElements = new();

        // Current callback to check the geometry matches the requested value.
        EventCallback<GeometryChangedEvent> m_TargetGeometryChangedCallback;

        // We need to keep track of the diff so we can calculate the expected values without rounding errors caused by the zoom scale.
        // Currently only used for the bottom and right handles.
        Vector2 m_LastDragDiff;

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
            AddHandle(k_TopHandleName, true, TrackedStyles.Top | TrackedStyles.Height, OnStartResizeDrag, OnEndResizeDrag, OnDragTop);
            AddHandle(k_LeftHandleName, true, TrackedStyles.Left | TrackedStyles.Width, OnStartResizeDrag, OnEndResizeDrag, OnDragLeft);
            AddHandle(k_BottomHandleName, false, TrackedStyles.Bottom | TrackedStyles.Height, OnStartResizeDrag, OnEndResizeDrag, OnDragBottom);
            AddHandle(k_RightHandleName, false, TrackedStyles.Right | TrackedStyles.Width, OnStartResizeDrag, OnEndResizeDrag, OnDragRight);

            // Add corner handles
            AddHandle(k_TopLeftHandleName, true, TrackedStyles.Left | TrackedStyles.Top | TrackedStyles.Width | TrackedStyles.Height, OnStartResizeDrag, OnEndResizeDrag, OnDragTopLeft);
            AddHandle(k_TopRightHandleName, true, TrackedStyles.Right | TrackedStyles.Top | TrackedStyles.Height | TrackedStyles.Width, OnStartResizeDrag, OnEndResizeDrag, OnDragTopRight);
            AddHandle(k_BottomLeftHandleName, true, TrackedStyles.Left | TrackedStyles.Bottom | TrackedStyles.Width | TrackedStyles.Height, OnStartResizeDrag, OnEndResizeDrag, OnDragBottomLeft);
            AddHandle(k_BottomRightHandleName, false, TrackedStyles.Right | TrackedStyles.Bottom | TrackedStyles.Width | TrackedStyles.Height, OnStartResizeDrag, OnEndResizeDrag, OnDragBottomRight);

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

        void OnStartResizeDrag(VisualElement element)
        {
            OnStartDrag(element);

            // We can not use geometry changed events when the element is using flex-grow. (UUM-72096)
            if (m_Target.resolvedStyle.flexGrow != 0 && m_Target.resolvedStyle.position == Position.Relative)
                return;

            // We need to monitor for geometry changed events so we can compare the requested against the actual to see if a min/max size is being enforced.
            switch (element.name)
            {
                case k_TopHandleName: m_TargetGeometryChangedCallback = OnTargetGeometryChangedDragTop; break;
                case k_LeftHandleName: m_TargetGeometryChangedCallback = OnTargetGeometryChangedDragLeft; break;
                case k_BottomHandleName: m_TargetGeometryChangedCallback = OnTargetGeometryChangedDragBottom; break;
                case k_RightHandleName: m_TargetGeometryChangedCallback = OnTargetGeometryChangedDragRight; break;
                case k_TopLeftHandleName: m_TargetGeometryChangedCallback = OnTargetGeometryChangedDragTopLeft; break;
                case k_TopRightHandleName: m_TargetGeometryChangedCallback = OnTargetGeometryChangedDragTopRight; break;
                case k_BottomLeftHandleName: m_TargetGeometryChangedCallback = OnTargetGeometryChangedDragBottomLeft; break;
                case k_BottomRightHandleName: m_TargetGeometryChangedCallback = OnTargetGeometryChangedDragBottomRight; break;
                default: return;
            }

            RegisterCallback(m_TargetGeometryChangedCallback);
        }

        void OnEndResizeDrag()
        {
            OnEndDrag();

            if (m_TargetGeometryChangedCallback != null)
            {
                UnregisterCallback(m_TargetGeometryChangedCallback);
                m_TargetGeometryChangedCallback = null;
            }
        }

        protected override void UpdateBoundStyles()
        {
            base.UpdateBoundStyles();

            if (m_Target != null && m_Target.parent != null)
            {
                if (m_Target.resolvedStyle.position == Position.Relative &&
                    !Mathf.Approximately(m_Target.resolvedStyle.flexGrow, 0))
                {
                    if (m_Target.parent.resolvedStyle.flexDirection == FlexDirection.Column)
                        m_BoundStyles |= TrackedStyles.Height;
                    else
                        m_BoundStyles |= TrackedStyles.Width;
                }
            }

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

        protected override void SetStylesFromTargetStyles()
        {
            base.SetStylesFromTargetStyles();
            UpdateBoundStyles();
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
            // Prevent setting a height less than 0
            var newHeight = m_TargetRectOnStartDrag.height - diff.y / canvas.zoomScale;
            if (newHeight < 0)
            {
                diff.y = m_TargetRectOnStartDrag.height * canvas.zoomScale;
            }

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
            // Prevent setting a width less than 0
            var newWidth = m_TargetRectOnStartDrag.width - diff.x / canvas.zoomScale;
            if (newWidth < 0)
            {
                diff.x = m_TargetRectOnStartDrag.width * canvas.zoomScale;
            }

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
            // Prevent setting a height less than 0
            var newHeight = m_TargetRectOnStartDrag.height + diff.y / canvas.zoomScale;
            if (newHeight < 0)
            {
                diff.y = -m_TargetRectOnStartDrag.height * canvas.zoomScale;
            }

            m_LastDragDiff = diff;

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
            // Prevent setting a width less than 0
            var newWidth = m_TargetRectOnStartDrag.width + diff.x / canvas.zoomScale;
            if (newWidth < 0)
            {
                diff.x = -m_TargetRectOnStartDrag.width * canvas.zoomScale;
            }

            m_LastDragDiff = diff;

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

        void OnTargetGeometryChangedDragTop(GeometryChangedEvent evt)
        {
            // Avoid doing too many updates, such as when the element is changing in response to our resize.
            if (evt.layoutPass > k_LayoutCountLimit)
                return;

            if (ShouldConstrainDragTopToTargetHeight(out var diffY))
            {
                m_ScratchChangeList.Clear();
                OnDragTop(new Vector2(0, diffY), m_ScratchChangeList);
            }
        }

        bool ShouldConstrainDragTopToTargetHeight(out float diffY)
        {
            if (!Mathf.Approximately(m_TargetRectOnStartDrag.yMax, m_Target.layout.yMax - m_Target.resolvedStyle.marginTop - m_Target.parent.resolvedStyle.borderTopWidth))
            {
                diffY = GenerateClampedHeightDiff();
                return true;
            }

            diffY = 0;
            return false;
        }

        float GenerateClampedHeightDiff()
        {
            // Generate the diff that will give us the same width, it needs to be scaled by the zoom scale.
            var scaledHeight = (m_Target.resolvedStyle.height + m_Target.resolvedStyle.marginBottom + m_Target.resolvedStyle.marginTop) * canvas.zoomScale;
            return m_ThisRectOnStartDrag.height - scaledHeight;
        }

        void OnTargetGeometryChangedDragLeft(GeometryChangedEvent evt)
        {
            // Avoid doing too many updates, such as when the element is changing in response to our resize.
            if (evt.layoutPass > k_LayoutCountLimit)
                return;

            if (ShouldConstrainDragLeftToTargetWidth(out var diffX))
            {
                m_ScratchChangeList.Clear();
                OnDragLeft(new Vector2(diffX, 0), m_ScratchChangeList);
            }
        }

        bool ShouldConstrainDragLeftToTargetWidth(out float diffX)
        {
            var parentBorderLeft = m_Target.parent.resolvedStyle.borderLeftWidth;
            var targetMarginLeft = m_Target.resolvedStyle.marginLeft;

            // If the right has moved then a min/max width constraint has been hit and we need to clamp to the target width.
            if (!Mathf.Approximately(m_TargetRectOnStartDrag.xMax, m_Target.layout.xMax - parentBorderLeft - targetMarginLeft))
            {
                diffX = GenerateClampedWidthDiff();
                return true;
            }

            diffX = 0;
            return false;
        }

        float GenerateClampedWidthDiff()
        {
            // Generate the diff that will give us the same width, it needs to be scaled by the zoom scale.
            var scaledWidth = (m_Target.resolvedStyle.width + m_Target.resolvedStyle.marginRight + m_Target.resolvedStyle.marginLeft) * canvas.zoomScale;
            return m_ThisRectOnStartDrag.width - scaledWidth;
        }

        void OnTargetGeometryChangedDragBottom(GeometryChangedEvent evt)
        {
            // Avoid doing too many updates, such as when the element is changing in response to our resize.
            if (evt.layoutPass > k_LayoutCountLimit)
                return;

            if (ShouldConstrainDragBottomToTargetHeight(out var diffY))
            {
                m_ScratchChangeList.Clear();
                OnDragBottom(new Vector2(0, diffY), m_ScratchChangeList);
            }
        }

        bool ShouldConstrainDragBottomToTargetHeight(out float diffY)
        {
            var expectedHeight = Mathf.Ceil(m_TargetRectOnStartDrag.height + m_LastDragDiff.y / canvas.zoomScale);
            if (!Mathf.Approximately(m_Target.resolvedStyle.height, expectedHeight))
            {
                diffY = -GenerateClampedHeightDiff();
                return true;
            }

            diffY = 0;
            return false;
        }

        void OnTargetGeometryChangedDragRight(GeometryChangedEvent evt)
        {
            // Avoid doing too many updates, such as when the element is changing in response to our resize.
            if (evt.layoutPass > k_LayoutCountLimit)
                return;

            if (ShouldConstrainDragRightToTargetWidth(out var diffX))
            {
                m_ScratchChangeList.Clear();
                OnDragRight(new Vector2(diffX, 0), m_ScratchChangeList);
            }
        }

        bool ShouldConstrainDragRightToTargetWidth(out float diffX)
        {
            var expectedWidth = Mathf.Ceil(m_TargetRectOnStartDrag.width + m_LastDragDiff.x / canvas.zoomScale);
            if (!Mathf.Approximately(m_Target.resolvedStyle.width, expectedWidth))
            {
                diffX = -GenerateClampedWidthDiff();
                return true;
            }

            diffX = 0;
            return false;
        }

        void OnTargetGeometryChangedDragTopLeft(GeometryChangedEvent evt)
        {
            // Avoid doing too many updates, such as when the element is changing in response to our resize.
            if (evt.layoutPass > k_LayoutCountLimit)
                return;

            var changedWidth = ShouldConstrainDragLeftToTargetWidth(out var diffX);
            var changedHeight = ShouldConstrainDragTopToTargetHeight(out var diffY);

            if (changedHeight || changedWidth)
            {
                m_ScratchChangeList.Clear();
                var diff = new Vector2(diffX, diffY);

                if (changedWidth)
                    OnDragLeft(diff, m_ScratchChangeList);
                if (changedHeight)
                    OnDragTop(diff, m_ScratchChangeList);
            }
        }

        void OnTargetGeometryChangedDragTopRight(GeometryChangedEvent evt)
        {
            // Avoid doing too many updates, such as when the element is changing in response to our resize.
            if (evt.layoutPass > k_LayoutCountLimit)
                return;

            var changedWidth = ShouldConstrainDragRightToTargetWidth(out var diffX);
            var changedHeight = ShouldConstrainDragTopToTargetHeight(out var diffY);

            if (changedHeight || changedWidth)
            {
                m_ScratchChangeList.Clear();
                var diff = new Vector2(diffX, diffY);

                if (changedWidth)
                    OnDragRight(diff, m_ScratchChangeList);
                if (changedHeight)
                    OnDragTop(diff, m_ScratchChangeList);
            }
        }

        void OnTargetGeometryChangedDragBottomLeft(GeometryChangedEvent evt)
        {
            // Avoid doing too many updates, such as when the element is changing in response to our resize.
            if (evt.layoutPass > k_LayoutCountLimit)
                return;

            var changedWidth = ShouldConstrainDragLeftToTargetWidth(out var diffX);
            var changedHeight = ShouldConstrainDragBottomToTargetHeight(out var diffY);

            if (changedHeight || changedWidth)
            {
                m_ScratchChangeList.Clear();
                var diff = new Vector2(diffX, diffY);

                if (changedWidth)
                    OnDragLeft(diff, m_ScratchChangeList);
                if (changedHeight)
                    OnDragBottom(diff, m_ScratchChangeList);
            }
        }

        void OnTargetGeometryChangedDragBottomRight(GeometryChangedEvent evt)
        {
            // Avoid doing too many updates, such as when the element is changing in response to our resize.
            if (evt.layoutPass > k_LayoutCountLimit)
                return;

            var changedWidth = ShouldConstrainDragRightToTargetWidth(out var diffX);
            var changedHeight = ShouldConstrainDragBottomToTargetHeight(out var diffY);

            if (changedHeight || changedWidth)
            {
                m_ScratchChangeList.Clear();
                var diff = new Vector2(diffX, diffY);

                if (changedWidth)
                    OnDragRight(diff, m_ScratchChangeList);
                if (changedHeight)
                    OnDragBottom(diff, m_ScratchChangeList);
            }
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
