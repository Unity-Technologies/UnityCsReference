// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    // This is basically the same as the standard layout update except for 1 thing :
    // - Only call dirty repaint when the layout rect has changed instead of "layoutNode.HasNewLayout"
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal partial class UIRLayoutUpdater : BaseVisualTreeUpdater
    {
         // When changing this value, we always consider that some controls may require multiple passes to compute their layout.
         // We also consider that these controls can also be nested inside other similar controls.
         // For example, a simple scroll view may need more than 2 passes to lay out the viewport and scrollers.
         // Therefore, having a deep hierarchy of scroll views can require a fair amount of layout passes.
        public const int kMaxValidateLayoutCount = 10;

        private static readonly string s_Description = "UIElements.UpdateLayout";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

        static readonly ProfilerMarker k_ComputeLayoutMarker = new("LayoutUpdater.ComputeLayout");
        static readonly ProfilerMarker k_UpdateSubTreeMarker = new("LayoutUpdater.UpdateSubTree");
        static readonly ProfilerMarker k_DispatchChangeEventsMarker = new("LayoutUpdater.DispatchChangeEvents");


        private bool m_CurrentlyRecordingLayout = false;

        internal static StopRecordingInterface s_StopRecording = null;

        public bool recordLayout
        {
            get
            {
                return m_CurrentlyRecordingLayout;
            }

            set
            {
                m_CurrentlyRecordingLayout = value;
                if (value)
                {
                    recordedLayoutItemList.Clear();
                    s_FrameIndex = 0;
                    s_OldMainLoopCount = 0;
                    s_PassIndex = 0;
                }
            }
        }

        static public void IncrementMainLoopCount()
        {
            s_MainLoopCount++;
        }

        public int recordLayoutCount
        {
            get
            {
                return recordedLayoutItemList.Count;
            }
        }

        public List<LayoutDebuggerItem> GetListOfRecord()
        {
            return recordedLayoutItemList;
        }

        List<LayoutDebuggerItem> recordedLayoutItemList = new List<LayoutDebuggerItem>();

        static int s_FrameIndex = 0;
        static int s_OldMainLoopCount = 0;
        static int s_MainLoopCount = 0;
        static int s_PassIndex = 0;


        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & (VersionChangeType.Layout | VersionChangeType.Hierarchy)) == 0)
                return;

            var layoutNode = ve.layoutNode;
            if (layoutNode != null && layoutNode.IsMeasureDefined)
            {
                layoutNode.MarkDirty();
            }
        }

        List<(Rect, Rect, VisualElement)> changeEventsList = new();

        public override void Update()
        {
            // update flex once
            int validateLayoutCount = 0;

            LayoutDebuggerItem record;
            int layoutLoop = 0;

            List<VisualElement> currentDirtyVE = null;

            if (visualTree.layoutNode.IsDirty)
            {
                if (recordLayout)
                {
                    if (s_OldMainLoopCount == s_MainLoopCount)
                    {
                        s_PassIndex++;
                    }
                    else
                    {
                        s_OldMainLoopCount = s_MainLoopCount;
                        s_FrameIndex++;
                        s_PassIndex = 0;
                    }

                    currentDirtyVE = new List<VisualElement>();
                    LayoutDebuggerVisualElement.TrackDirtyElement(visualTree, currentDirtyVE);
                    LayoutDebuggerVisualElement ve = new LayoutDebuggerVisualElement();
                    record = new LayoutDebuggerItem(s_FrameIndex, s_PassIndex, layoutLoop, ve);
                    LayoutDebuggerVisualElement.CopyLayout(visualTree, ve, currentDirtyVE);
                    layoutLoop++;
                    recordedLayoutItemList.Add(record);
                }

                while (visualTree.layoutNode.IsDirty)
                {
                    changeEventsList.Clear();

                    // Doing multiple layout passes requires to update the styles or else the
                    // elements may not be initialized properly and the resulting layout will be invalid.
                    if (validateLayoutCount > 0)
                        panel.ApplyStyles();

                    if (recordLayout)
                    {
                        currentDirtyVE.Clear();
                        LayoutDebuggerVisualElement.TrackDirtyElement(visualTree, currentDirtyVE);
                    }

                    panel.duringLayoutPhase = true;
                    k_ComputeLayoutMarker.Begin();
                    visualTree.layoutNode.CalculateLayout();
                    k_ComputeLayoutMarker.End();
                    panel.duringLayoutPhase = false;

                    k_UpdateSubTreeMarker.Begin();
                    UpdateSubTree(visualTree, changeEventsList);
                    k_UpdateSubTreeMarker.End();

                    if (recordLayout)
                    {
                        LayoutDebuggerVisualElement ve = new LayoutDebuggerVisualElement();
                        record = new LayoutDebuggerItem(s_FrameIndex, s_PassIndex, layoutLoop, ve);

                        LayoutDebuggerVisualElement.CopyLayout(visualTree, ve, currentDirtyVE);
                        layoutLoop++;
                        recordedLayoutItemList.Add(record);
                    }
                    k_DispatchChangeEventsMarker.Begin();
                    DispatchChangeEvents(changeEventsList, validateLayoutCount);
                    k_DispatchChangeEventsMarker.End();

                    if (validateLayoutCount++ >= kMaxValidateLayoutCount)
                    {
                        Debug.LogError("Layout update is struggling to process current layout (consider simplifying to avoid recursive layout): " + visualTree);
                        if (s_StopRecording != null)
                        {
                            s_StopRecording.StopRecording();
                        }
                        break;
                    }
                }
            }

            // This call happens here for two reasons
            // 1. Visibility style of the focused element may have changed (regardless of the layout having updated)
            // 2. Display style of the focused element may have changed, but it's only manually propagated to children
            //    as part of this updater.
            // Note: this is a O(1) call
            visualTree.focusController.ReevaluateFocus();
        }

        /// <summary>
        /// This method update areAncestorsAndSelfDisplayed and disableRendering for the provided element and for all disabled element under.
        /// It also trigger changeEvents if some element were disabled
        /// This method does not update the elements under a displayed element since they are updated in UpdateSubTree
        /// </summary>
        /// <param name="ve"></param>
        /// <param name="changeEvents"> is a list to accumulate the event to dispatch later</param>
        /// <param name="inheritedDisplayed"> false when any of the ancestors has display:none, otherwise true</param>
        /// <returns> has his state changed</returns>
        static private bool UpdateHierarchyDisplayed(VisualElement ve, List<(Rect, Rect, VisualElement)> changeEvents, bool inheritedDisplayed = true)
        {
            var isDisplayed = inheritedDisplayed & ve.resolvedStyle.display != DisplayStyle.None;

            // If our parent is disabled, there is no need to disable the rendering on the child,
            // but if the child is disabled, there is no need to re-enable it.
            // If we are displayed we definitely don't want to be disabled!
            if (inheritedDisplayed && !isDisplayed)
                ve.disableRendering = true;
            else if (isDisplayed)
                ve.disableRendering = false;

            // If the status changed, we need to update the display status of the children.
            // If the children has already the correct state, we don't need to continue
            // (This check will stop the depth first branch from being updated)
            if (ve.areAncestorsAndSelfDisplayed == isDisplayed)
                return false;

            ve.areAncestorsAndSelfDisplayed = isDisplayed;

            // UpdateSubTree will propagate the Display:flex status but will not recurse in the case of a display:none
            // We need recurse here to update/propagate the Display:none status and generate changeEvents when elements were hidden this frame
            if (!isDisplayed)
            {
                if (inheritedDisplayed)//for the element having a displayed parent and that just became in display:none
                {
                    // Make sure bounding box are re-computed (using layout).
                    ve.IncrementVersion(VersionChangeType.Size);
                }

                if (ve.HasSelfEventInterests(GeometryChangedEvent.EventCategory))
                    changeEvents.Add((ve.lastLayout, Rect.zero, ve));

                var childCount = ve.hierarchy.childCount;
                for (int i = 0; i < childCount; ++i)
                {
                    UpdateHierarchyDisplayed(ve.hierarchy[i], changeEvents, isDisplayed);
                }
            }

            return true;
        }

        private void UpdateSubTree(VisualElement ve, List<(Rect, Rect, VisualElement)> changeEvents)
        {
            // Because the UpdateSubTree recursion always starts form the root, and does not process subtrees that are not displayed, inheritedDisplay is always true.
            bool isDisplayedJustChanged = UpdateHierarchyDisplayed(ve, changeEvents, true);

            // Events for disabled element and their descendant have been added by UpdateHierarchyDisplayed
            // We don't trigger any IncrementVersion because we want to keep the UI buffered.
            // the transform and size version change will occur when the element become displayed again.
            if (!ve.areAncestorsAndSelfDisplayed)
                return;

            Rect layoutRect = new Rect(ve.layoutNode.LayoutX, ve.layoutNode.LayoutY, ve.layoutNode.LayoutWidth, ve.layoutNode.LayoutHeight);

            // we encode right/bottom into width/height
            Rect rawPadding = new Rect(ve.layoutNode.LayoutPaddingLeft, ve.layoutNode.LayoutPaddingLeft, ve.layoutNode.LayoutPaddingRight, ve.layoutNode.LayoutPaddingBottom);

            // This is not the "real" padding rect: it may differ in size and position because the borders are ignored.
            // Alone, it cannot be used to identify padding size changes, because the bottom and right values depend on the
            // layout rect width/height. A change in layout rect width/height with a corresponding variation in
            // right/bottom values may yield the same pseudoPaddingRect. Fortunately, the layout rect size and padding rect
            // size change trigger the same code path which explains why we haven't seen any issue with this so far.
            Rect layoutPseudoPaddingRect = new Rect(
               rawPadding.x,
                rawPadding.y,
                layoutRect.width - (rawPadding.x + rawPadding.width),
                layoutRect.height - (rawPadding.y + rawPadding.height));
            Rect lastLayoutRect = ve.lastLayout;
            Rect lastPseudoPaddingRect = ve.lastPseudoPadding;

            VersionChangeType changeType = 0;
            // Changing the layout/padding size should trigger the following version changes:
            // - Size:    to update the clipping rect, when required
            // - Repaint: to update the geometry inside the new rect
            bool layoutSizeChanged = lastLayoutRect.size != layoutRect.size;
            bool pseudoPaddingSizeChanged = lastPseudoPaddingRect.size != layoutPseudoPaddingRect.size;
            if (layoutSizeChanged || pseudoPaddingSizeChanged)
                changeType |= VersionChangeType.Size | VersionChangeType.Repaint;

            // Changing the layout/padding position should trigger the following version change:
            // - Transform: to draw the element and content at the right position
            bool layoutPositionChanged = layoutRect.position != lastLayoutRect.position;
            bool paddingPositionChanged = layoutPseudoPaddingRect.position != lastPseudoPaddingRect.position;
            if (layoutPositionChanged || paddingPositionChanged || isDisplayedJustChanged)
                changeType |= VersionChangeType.Transform;

            if (isDisplayedJustChanged)
                changeType |= VersionChangeType.Size;

            // If the scale or rotate value of the style are not default, a change in the size will affect the resulting
            // translation part of the local transform. Only when the transformOrigin is at (0, 0) we do not need to
            // update the transform.
            if ((changeType & (VersionChangeType.Size | VersionChangeType.Transform)) == VersionChangeType.Size)
            {
                if (!ve.hasDefaultRotationAndScale)
                {
                    // if the pivot is not at the top left, update the transform
                    if (!Mathf.Approximately(ve.resolvedStyle.transformOrigin.x, 0.0f) ||
                        !Mathf.Approximately(ve.resolvedStyle.transformOrigin.y, 0.0f))
                    {
                        changeType |= VersionChangeType.Transform;
                    }
                }
            }

            if (changeType != 0)
                ve.IncrementVersion(changeType);

            ve.lastLayout = layoutRect;
            ve.lastPseudoPadding = layoutPseudoPaddingRect;

            // ignore clean sub trees
            bool hasNewLayout = ve.layoutNode.HasNewLayout;
            if (hasNewLayout)
            {
                var childCount = ve.hierarchy.childCount;
                for (int i = 0; i < childCount; ++i)
                {
                    var child = ve.hierarchy[i];

                    if (child.layoutNode.HasNewLayout)
                        UpdateSubTree(child, changeEvents);
                }
            }

            // Only send GeometryChanged events when the layout changes
            // (padding changes don't affect the element's outer geometry).
            if ((layoutSizeChanged || layoutPositionChanged || isDisplayedJustChanged) && ve.HasSelfEventInterests(GeometryChangedEvent.EventCategory))
            {
                changeEvents.Add((isDisplayedJustChanged ? Rect.zero : lastLayoutRect, layoutRect, ve));
            }

            if (hasNewLayout)
            {
                ve.layoutNode.MarkLayoutSeen();
            }
        }

        private void DispatchChangeEvents(List<(Rect, Rect, VisualElement)> changeEvents, int currentLayoutPass)
        {
            foreach ((var oldRect, var newRect, var ve) in changeEvents)
            {
                using (var evt = GeometryChangedEvent.GetPooled(oldRect, newRect))
                {
                    evt.layoutPass = currentLayoutPass;
                    evt.elementTarget = ve;
                    EventDispatchUtilities.HandleEventAtTargetAndDefaultPhase(evt, panel, ve);
                }
            }
        }
    }
}
