// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderDragger
    {
        protected enum DestinationPane
        {
            Hierarchy,
            Viewport,
            Stylesheet
        };

        static readonly string s_DraggerPreviewClassName = "unity-builder-dragger-preview";
        static readonly string s_DraggedPreviewClassName = "unity-builder-dragger-preview--dragged";

        static readonly string s_TreeItemHoverHoverClassName = "unity-builder-explorer__item--dragger-hover";
        public static readonly string s_TreeItemHoverWithDragBetweenElementsSupportClassName = "unity-builder-explorer__between-element-item--dragger-hover";
        static readonly string s_TreeViewItemName = "unity-tree-view__item";
        static readonly int s_DistanceToActivation = 5;

        // It's possible to have multiple BuilderDraggers on the same element. This ensures
        // a kind of capture without using the capture system and just between BuilderDraggers.
        internal static BuilderDragger s_CurrentlyActiveBuilderDragger = null;

        Vector2 m_Start;
        bool m_Active;
        bool m_WeStartedTheDrag;

        BuilderPaneWindow m_PaneWindow;
        VisualElement m_Root;
        VisualElement m_Canvas;
        BuilderSelection m_Selection;

        VisualElement m_DraggedElement;
        VisualElement m_LastHoverElement;
        int m_LastHoverElementChildIndex;
        VisualElement m_LastRowHoverElement;

        BuilderParentTracker m_ParentTracker;
        BuilderPlacementIndicator m_PlacementIndicator;

        public VisualElement builderHierarchyRoot { get; set; }
        public VisualElement builderStylesheetRoot { get; set; }

        public bool active => m_Active;

        /// <summary>
        /// Indicates whether this dragger prevents other draggers from getting active at the same time.
        /// </summary>
        protected bool exclusive { get; set; } = true;

        protected BuilderPaneWindow paneWindow { get { return m_PaneWindow; } }
        protected BuilderSelection selection { get { return m_Selection; } }
        protected VisualElement documentRootElement => m_Canvas;

        protected BuilderViewport viewport { get; private set; }
        protected BuilderPlacementIndicator placementIndicator => m_PlacementIndicator;

        List<ManipulatorActivationFilter> activators { get; set; }
        ManipulatorActivationFilter m_CurrentActivator;

        public event Action onEndDrag;

        public BuilderDragger(
            BuilderPaneWindow paneWindow,
            VisualElement root, BuilderSelection selection,
            BuilderViewport viewport = null, BuilderParentTracker parentTracker = null)
        {
            m_PaneWindow = paneWindow;
            m_Root = root;
            this.viewport = viewport;
            m_Canvas = viewport?.documentRootElement;
            m_Selection = selection;
            m_ParentTracker = parentTracker;
            m_PlacementIndicator = viewport?.placementIndicator;
            if (m_PlacementIndicator != null)
                m_PlacementIndicator.documentRootElement = m_Canvas;

            activators = new List<ManipulatorActivationFilter>();
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });

            m_Active = false;
            m_WeStartedTheDrag = false;

            m_DraggedElement = CreateDraggedElement();
            m_DraggedElement.AddToClassList(s_DraggerPreviewClassName);
            m_Root.Add(m_DraggedElement);
        }

        internal void Reset()
        {
            m_Active = false;
            m_WeStartedTheDrag = false;
            m_Start = default;
            m_LastHoverElement = default;
            m_LastRowHoverElement = default;
            m_LastHoverElementChildIndex = default;
            s_CurrentlyActiveBuilderDragger = default;
        }

        protected virtual VisualElement CreateDraggedElement()
        {
            return new VisualElement();
        }

        protected virtual void FillDragElement(VisualElement pill)
        {
        }

        protected virtual bool StartDrag(VisualElement target, Vector2 mousePosition, VisualElement pill)
        {
            return true;
        }

        protected virtual void PerformDrag(VisualElement target, VisualElement pickedElement, int index = -1)
        {
        }

        protected virtual void PerformAction(VisualElement destination, DestinationPane pane, Vector2 localMousePosition, int index = -1)
        {
        }

        protected virtual void FailAction(VisualElement target)
        {
        }

        protected virtual void EndDrag()
        {
        }

        protected virtual bool IsPickedElementValid(VisualElement element)
        {
            return true;
        }

        protected virtual bool SupportsDragBetweenElements(VisualElement element)
        {
            return false;
        }

        protected virtual bool SupportsDragInEmptySpace(VisualElement element)
        {
            return true;
        }

        protected virtual bool SupportsPlacementIndicator()
        {
            return true;
        }

        protected virtual VisualElement GetDefaultTargetElement()
        {
            return m_Canvas.Query().Where(e => e.GetVisualTreeAsset() == m_PaneWindow.document.visualTreeAsset).First();
        }

        private StyleLength m_targetInitialMinWidth;
        private StyleLength m_targetInitialMinHeight;

        protected void FixElementSizeAndPosition(VisualElement target)
        {
            m_targetInitialMinWidth = target.style.minWidth;
            m_targetInitialMinHeight = target.style.minHeight;

            target.style.minWidth = target.resolvedStyle.width;
            target.style.minHeight = target.resolvedStyle.height;
        }

        protected void UnfixElementSizeAndPosition(VisualElement target)
        {
            target.style.minWidth = m_targetInitialMinWidth;
            target.style.minHeight = m_targetInitialMinHeight;

            selection.ForceVisualAssetUpdateWithoutSave(target, BuilderHierarchyChangeType.InlineStyle);
        }

        public void RegisterCallbacksOnTarget(VisualElement target)
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<KeyUpEvent>(OnEsc);

            target.RegisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
        }

        void UnregisterCallbacksFromTarget(DetachFromPanelEvent evt)
        {
            var target = evt.elementTarget;

            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<KeyUpEvent>(OnEsc);

            target.UnregisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
        }

        bool StartDrag(VisualElement target, Vector2 mousePosition)
        {
            var startSuccess = StartDrag(target, mousePosition, m_DraggedElement);
            if (!startSuccess)
                return startSuccess;

            if (s_CurrentlyActiveBuilderDragger == this)
            {
                FillDragElement(m_DraggedElement);
                m_DraggedElement.BringToFront();
                m_DraggedElement.AddToClassList(s_DraggedPreviewClassName);
            }

            // So we don't have a flashing element at the top left corner
            // at the very start of the drag.
            PerformDragInner(target, mousePosition);

            return startSuccess;
        }

        bool TryToPickInCanvas(Vector2 mousePosition)
        {
            if (viewport == null)
                return false;

            var localMouse = m_Canvas.WorldToLocal(mousePosition);
            if (!m_Canvas.ContainsPoint(localMouse))
            {
                m_ParentTracker?.Deactivate();
                m_PlacementIndicator?.Deactivate();
                return false;
            }

            var pickedElement = Panel.PickAllWithoutValidatingLayout(m_Canvas, mousePosition);

            // Don't allow selection of elements inside template instances or outside current active document.
            pickedElement = pickedElement.GetClosestElementPartOfCurrentDocument();
            if (pickedElement != null && !pickedElement.IsPartOfActiveVisualTreeAsset(m_PaneWindow.document))
                pickedElement = null;

            // Get Closest valid element.
            pickedElement = pickedElement.GetClosestElementThatIsValid(IsPickedElementValid);

            if (pickedElement == null)
            {
                m_ParentTracker?.Deactivate();
                m_PlacementIndicator?.Deactivate();
                m_LastHoverElement = null;
                return false;
            }

            // The placement indicator might decide to change the parent.
            if (SupportsPlacementIndicator() && m_PlacementIndicator != null)
            {
                m_PlacementIndicator.Activate(pickedElement, mousePosition);
                pickedElement = m_PlacementIndicator.parentElement;
            }

            m_ParentTracker.Activate(pickedElement);

            m_LastHoverElement = pickedElement;
            if (SupportsPlacementIndicator() && m_PlacementIndicator != null)
                m_LastHoverElementChildIndex = m_PlacementIndicator.indexWithinParent;

            return true;
        }

        bool IsElementTheScrollView(VisualElement pickedElement)
        {
            if (!SupportsDragInEmptySpace(pickedElement))
                return false;

            if (pickedElement == null)
                return false;

            if (pickedElement is ScrollView)
                return true;

            if (pickedElement.ClassListContains(ScrollView.viewportUssClassName))
                return true;

            return false;
        }

        static bool CanPickInExplorerRoot(Vector2 mousePosition, VisualElement explorerRoot)
        {
            return explorerRoot != null && explorerRoot.ContainsPoint(explorerRoot.WorldToLocal(mousePosition));
        }

        bool TryToPickInExplorer(Vector2 mousePosition, VisualElement explorerRoot)
        {
            if (!CanPickInExplorerRoot(mousePosition, explorerRoot))
                return false;

            // Pick element under mouse.
            var pickedElement = Panel.PickAllWithoutValidatingLayout(explorerRoot, mousePosition);

            // Pick the first valid element by walking up the tree.
            VisualElement pickedDocumentElement = null;
            VisualElement explorerItemReorderZone = null;
            while (true)
            {
                if (pickedElement == null)
                    break;

                if (IsElementTheScrollView(pickedElement))
                    break;

                if (pickedElement.ClassListContains(BuilderConstants.ExplorerItemReorderZoneClassName))
                    explorerItemReorderZone = pickedElement;

                pickedDocumentElement = pickedElement.GetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName) as VisualElement;
                if (pickedDocumentElement != null)
                    break;

                pickedElement = pickedElement.parent;
            }

            // Check if reordering on top of current pickedElement is supported.
            var supportsDragBetweenElements = false;
            if (explorerItemReorderZone != null && SupportsDragBetweenElements(pickedDocumentElement))
            {
                pickedElement = explorerItemReorderZone;
                supportsDragBetweenElements = true;
            }

            // Don't allow selection of elements inside template instances.
            VisualElement linkedCanvasPickedElement = null;
            if (pickedElement != null && pickedElement.ClassListContains(BuilderConstants.ExplorerItemReorderZoneClassName))
            {
                linkedCanvasPickedElement = GetLinkedElementFromReorderZone(pickedElement);
            }
            else if (pickedElement != null)
            {
                linkedCanvasPickedElement = pickedElement.GetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName) as VisualElement;
            }

            // Validate element with implementation.
            var hoverElementIsValid = pickedElement != null && (IsElementTheScrollView(pickedElement) || IsPickedElementValid(linkedCanvasPickedElement));
            if (!hoverElementIsValid && !supportsDragBetweenElements)
                pickedElement = null;

            m_LastHoverElement = pickedElement;
            if (pickedElement == null)
            {
                m_LastRowHoverElement = null;
                return false;
            }

            // The hover style class may not be applied to the hover element itself. We need
            // to find the correct parent.
            m_LastRowHoverElement = m_LastHoverElement;
            if (!IsElementTheScrollView(pickedElement))
            {
                while (m_LastRowHoverElement != null && m_LastRowHoverElement.name != s_TreeViewItemName)
                    m_LastRowHoverElement = m_LastRowHoverElement.parent;
            }

            if (hoverElementIsValid && m_LastRowHoverElement != null)
                m_LastRowHoverElement.AddToClassList(s_TreeItemHoverHoverClassName);

            if (supportsDragBetweenElements)
                explorerItemReorderZone.AddToClassList(s_TreeItemHoverWithDragBetweenElementsSupportClassName);

            return true;
        }

        void PerformDragInner(VisualElement target, Vector2 mousePosition)
        {
            // Move dragged element.
            m_DraggedElement.style.left = mousePosition.x;
            m_DraggedElement.style.top = mousePosition.y;

            m_LastRowHoverElement?.RemoveFromClassList(s_TreeItemHoverHoverClassName);
            m_LastRowHoverElement?.Query(className: BuilderConstants.ExplorerItemReorderZoneClassName).ForEach(e => e.RemoveFromClassList(s_TreeItemHoverWithDragBetweenElementsSupportClassName));

            // Note: It's important for the Hierarchy/Stylesheet panes to be checked first because
            // this check does not account for which element is on top of which other element
            // so if the Viewport is panned such that the Canvas is behind the Hierarchy,
            // the TryToPickIn..() call will return true for the Canvas.
            var isCanvasBlocked = CanPickInExplorerRoot(mousePosition, builderHierarchyRoot) || CanPickInExplorerRoot(mousePosition, builderStylesheetRoot);
            if (isCanvasBlocked)
            {
                var validHover = TryToPickInExplorer(mousePosition, builderHierarchyRoot) || TryToPickInExplorer(mousePosition, builderStylesheetRoot);
                if (validHover)
                {
                    VisualElement pickedElement;
                    int index;
                    GetPickedElementFromHoverElement(out pickedElement, out index);

                    if (pickedElement == null)
                        return;

                    // Mirror final drag destination in the viewport using the placement indicator.
                    m_PlacementIndicator?.Activate(pickedElement, index);

                    m_Active = true;
                    PerformDrag(target, pickedElement, index);
                    return;
                }
            }
            else
            {
                var validHover = TryToPickInCanvas(mousePosition);
                if (validHover)
                {
                    m_Active = true;
                    PerformDrag(target, m_LastHoverElement, m_LastHoverElementChildIndex);
                    return;
                }
            }

            m_PlacementIndicator?.Deactivate();
            PerformDrag(target, null);
        }

        void EndDragInner()
        {
            EndDrag();
            onEndDrag?.Invoke();

            m_LastRowHoverElement?.RemoveFromClassList(s_TreeItemHoverHoverClassName);
            m_LastRowHoverElement?.Query(className: BuilderConstants.ExplorerItemReorderZoneClassName).ForEach(e => e.RemoveFromClassList(s_TreeItemHoverWithDragBetweenElementsSupportClassName));
            m_DraggedElement.RemoveFromClassList(s_DraggedPreviewClassName);
            m_ParentTracker?.Deactivate();
            m_PlacementIndicator?.Deactivate();
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (s_CurrentlyActiveBuilderDragger != null && s_CurrentlyActiveBuilderDragger != this && s_CurrentlyActiveBuilderDragger.exclusive)
                return;

            var target = evt.currentTarget as VisualElement;

            if (m_WeStartedTheDrag && target.HasMouseCapture())
            {
                evt.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(evt))
                return;

            if (target.HasMouseCapture())
                return;

            s_CurrentlyActiveBuilderDragger ??= this;

            m_Start = evt.mousePosition;
            m_WeStartedTheDrag = true;
            if (s_CurrentlyActiveBuilderDragger == this)
                target.CaptureMouse();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            var target = evt.currentTarget as VisualElement;

            if (!target.HasMouseCapture())
                return;

            if (!m_Active && m_WeStartedTheDrag)
            {
                if (Mathf.Abs(m_Start.x - evt.mousePosition.x) > s_DistanceToActivation ||
                    Mathf.Abs(m_Start.y - evt.mousePosition.y) > s_DistanceToActivation)
                {
                    var startSuccess = StartDrag(target, evt.mousePosition);

                    if (startSuccess)
                    {
                        evt.StopPropagation();
                        m_Active = true;
                    }
                    else
                    {
                        if (s_CurrentlyActiveBuilderDragger == this)
                        {
                            target.ReleaseMouse();
                            s_CurrentlyActiveBuilderDragger = null;
                        }
                    }
                }

                return;
            }

            if (m_Active)
            {
                PerformDragInner(target, evt.mousePosition);
            }

            evt.StopPropagation();
        }

        VisualElement GetLinkedElementFromReorderZone(VisualElement hoverZone)
        {
            var reorderZone = hoverZone;
            var explorerItem = reorderZone.userData as BuilderExplorerItem;
            var sibling = explorerItem.GetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName) as VisualElement;
            return sibling;
        }

        void SelectItemOnSingleClick(MouseUpEvent evt)
        {
            // TODO: ListView right now does not allow selecting a single
            // item that is already part of multi-selection, and having
            // only that item selected. Clicking on any already-selected
            // item in ListView does nothing. This needs to be fixed in trunk.
            //
            // In the meantime, we use this leaked mouse click hack, which
            // also accounts for another bug in ListView, to catch these
            // unhandled selection events and do the single-item selection
            // ourselves.
            //
            // See: https://unity3d.atlassian.net/browse/UIT-1011

            if (m_Selection.selectionCount <= 1)
                return;

            if (evt.modifiers.HasFlag(EventModifiers.Control)
                || evt.modifiers.HasFlag(EventModifiers.Shift)
                || evt.modifiers.HasFlag(EventModifiers.Command))
                return;

            var element = evt.elementTarget;
            var ancestor = element is BuilderExplorerItem ? element as BuilderExplorerItem : element?.GetFirstAncestorOfType<BuilderExplorerItem>();
            if (ancestor == null)
                return;

            var documentElement = ancestor.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName) as VisualElement;
            if (documentElement == null)
                return;

            m_Selection.Select(null, documentElement);
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
            {
                evt.StopPropagation();
                return;
            }

            var target = evt.currentTarget as VisualElement;

            if (!CanStopManipulation(evt))
                return;

            target.ReleaseMouse();
            m_WeStartedTheDrag = false;

            if (s_CurrentlyActiveBuilderDragger == this)
                s_CurrentlyActiveBuilderDragger = null;

            if (!m_Active)
            {
                SelectItemOnSingleClick(evt);
                return;
            }

            var currentMouse = evt.mousePosition;
            if (m_LastHoverElement != null)
            {
                var localCanvasMouse = viewport != null ? m_Canvas.WorldToLocal(currentMouse) : Vector2.zero;
                var localHierarchyMouse = builderHierarchyRoot?.WorldToLocal(currentMouse) ?? Vector2.zero;
                var localStylesheetMouse = builderStylesheetRoot?.WorldToLocal(currentMouse) ?? Vector2.zero;

                if (builderHierarchyRoot != null && builderHierarchyRoot.ContainsPoint(localHierarchyMouse))
                {
                    VisualElement newParent;
                    int index;
                    GetPickedElementFromHoverElement(out newParent, out index);

                    if (newParent != null)
                        PerformAction(newParent, DestinationPane.Hierarchy, localHierarchyMouse, index);
                }
                else if (builderStylesheetRoot != null && builderStylesheetRoot.ContainsPoint(localStylesheetMouse))
                {
                    VisualElement newParent;
                    int index;
                    GetPickedElementFromHoverElement(out newParent, out index);

                    if (newParent != null)
                        PerformAction(newParent, DestinationPane.Stylesheet, localStylesheetMouse, index);
                }
                else if (viewport != null && m_Canvas.ContainsPoint(localCanvasMouse))
                {
                    PerformAction(m_LastHoverElement, DestinationPane.Viewport, localCanvasMouse, m_LastHoverElementChildIndex);
                }
            }

            m_Active = false;

            evt.StopPropagation();

            EndDragInner();
        }

        void GetPickedElementFromHoverElement(out VisualElement pickedElement, out int index)
        {
            index = -1;
            if (IsElementTheScrollView(m_LastRowHoverElement))
            {
                pickedElement = GetDefaultTargetElement();
            }
            else if (m_LastHoverElement.ClassListContains(BuilderConstants.ExplorerItemReorderZoneClassName))
            {
                var reorderZone = m_LastHoverElement;
                var sibling = GetLinkedElementFromReorderZone(reorderZone);

                pickedElement = sibling.parent;

                var siblingIndex = pickedElement.IndexOf(sibling);
                index = pickedElement.childCount;

                if (reorderZone.ClassListContains(BuilderConstants.ExplorerItemReorderZoneAboveClassName))
                {
                    index = siblingIndex;
                }
                else if (reorderZone.ClassListContains(BuilderConstants.ExplorerItemReorderZoneBelowClassName))
                {
                    index = siblingIndex + 1;
                }
            }
            else
            {
                pickedElement = m_LastHoverElement.GetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName) as VisualElement;
            }
        }

        void OnEsc(KeyUpEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape)
                return;

            var target = evt.currentTarget as VisualElement;

            if (!m_Active)
                return;

            if (s_CurrentlyActiveBuilderDragger == this)
                s_CurrentlyActiveBuilderDragger = null;

            m_Active = false;

            if (!target.HasMouseCapture())
                return;

            target.ReleaseMouse();
            evt.StopPropagation();
            EndDragInner();

            FailAction(target);
        }

        bool CanStartManipulation(IMouseEvent evt)
        {
            foreach (var activator in activators)
            {
                if (activator.Matches(evt))
                {
                    m_CurrentActivator = activator;
                    return true;
                }
            }

            return false;
        }

        bool CanStopManipulation(IMouseEvent evt)
        {
            if (evt == null)
            {
                return false;
            }

            return ((MouseButton)evt.button == m_CurrentActivator.button);
        }
    }
}
