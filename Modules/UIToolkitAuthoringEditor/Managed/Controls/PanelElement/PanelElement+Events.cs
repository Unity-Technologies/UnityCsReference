// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed partial class PanelElement
{
    bool m_ForwardHierarchicalEvents;

    public bool ForwardHierarchicalEvents
    {
        get => m_ForwardHierarchicalEvents;
        set => m_ForwardHierarchicalEvents = value;
    }

    protected override void HandleEventTrickleDown(EventBase evt)
    {
        try
        {
            if (SubPanel == null)
                return;

            if (ForwardHierarchicalEvents)
                ForwardEventTrickleDown(evt);
        }
        finally
        {
            base.HandleEventTrickleDown(evt);
        }
    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        try
        {
            if (SubPanel == null)
                return;

            if (ForwardHierarchicalEvents)
                ForwardEventBubbleUp(evt);
        }
        finally
        {
            base.HandleEventBubbleUp(evt);
        }
    }

    internal void ForwardEventTrickleDown(EventBase evt)
    {
        if (SubPanel == null)
            return;

        var convertedPosition = ConvertPosition(evt);
        switch (evt)
        {
            case FocusInEvent focusInEvent:
                if (subRootVisualElement.focusController.focusedElement != null)
                    return;
                // or clicking into the nested panel will force focus the first element of the nested panel.
                var element = default(VisualElement);
                switch (focusInEvent.direction)
                {
                    case 1:
                        element = GetPreviousFocusableInTree(subRootVisualElement, subRootVisualElement);
                        break;
                    case 2:
                        element = GetNextFocusableInTree(subRootVisualElement, subRootVisualElement);
                        break;
                }

                element?.Focus();
                break;
            case FocusOutEvent:
                subRootVisualElement.focusController.BlurLastFocusedElement();
                break;
            case PointerDownEvent pointerDownEvent:
                DispatchNestedPointerEvent(pointerDownEvent, convertedPosition, EventType.MouseDown, PointerDownEvent.GetPooled);
                break;
            case PointerUpEvent pointerUpEvent:
                DispatchNestedPointerEvent(pointerUpEvent, convertedPosition, EventType.MouseUp, PointerUpEvent.GetPooled);
                break;
            case PointerMoveEvent pointerMoveEvent:
                DispatchNestedPointerEvent(pointerMoveEvent, convertedPosition, EventType.MouseMove, PointerMoveEvent.GetPooled);
                break;
            case KeyDownEvent keyDownEvent:
                DispatchNestedKeyEvent(keyDownEvent, KeyDownEvent.GetPooled);
                break;
            case KeyUpEvent keyUpEvent:
                DispatchNestedKeyEvent(keyUpEvent, KeyUpEvent.GetPooled);
                break;
        }
    }

    internal void ForwardEventBubbleUp(EventBase evt)
    {
        switch(evt)
        {
            case GeometryChangedEvent geometryChangedEvent:
                OnGeometryChanged(geometryChangedEvent);
                break;
            case AttachToPanelEvent:
                //CreatePanel();
                break;
            case DetachFromPanelEvent:
                //DestroyPanel();
                break;
            case NavigationMoveEvent navigationMoveEvent:
                if (ForwardHierarchicalEvents)
                    DispatchNestedNavigationEvent(navigationMoveEvent, NavigationMoveEvent.GetPooled);
                else
                {
                    switch (navigationMoveEvent.direction)
                    {
                        case NavigationMoveEvent.Direction.None:
                            break;
                        case NavigationMoveEvent.Direction.Left:
                            break;
                        case NavigationMoveEvent.Direction.Up:
                            break;
                        case NavigationMoveEvent.Direction.Right:
                            break;
                        case NavigationMoveEvent.Direction.Down:
                            break;
                        case NavigationMoveEvent.Direction.Next:
                            focusController?.SwitchFocus(GetNextFocusableInTree(this, panel.visualTree), true);
                            break;
                        case NavigationMoveEvent.Direction.Previous:
                            focusController?.SwitchFocus(GetPreviousFocusableInTree(this, panel.visualTree), true);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                break;
        }
    }

    void OnGeometryChanged(GeometryChangedEvent evt)
    {
        ResizeRenderTexture(evt.newRect.size);
    }

    internal Vector3 ConvertPosition(EventBase evt)
    {
        if (evt is not IPointerEvent pEvt)
            return Vector3.zero;

        return LocalToPanelPosition(pEvt.localPosition);
    }

    internal Vector2 LocalToPanelPosition(Vector2 localPosition)
    {
        var scaledPos = localPosition * SubPanel.pixelsPerPoint;
        return SubPanel is RuntimePanel runtimePanel
            ? (Vector2)runtimePanel.ScreenToPanel(scaledPos)
            : scaledPos;
    }

    internal void PickAll(Vector2 localPosition, List<VisualElement> results)
    {
        if (SubPanel == null)
            return;

        SubPanel.PickAll(LocalToPanelPosition(localPosition), results);
    }

    void DispatchNestedPointerEvent<T>(
        T original,
        Vector2 convertedPosition,
        EventType type,
        Func<EventType, Vector3, Vector2, int, int, EventModifiers, int, T> getPooled)
        where T : PointerEventBase<T>, new()
    {
        using var nestedEvt = getPooled(
            type,
            convertedPosition,
            original.deltaPosition,
            original.button,
            original.clickCount,
            original.modifiers,
            ((IPointerEventInternal)original).displayIndex);
        nestedEvt.propagation = original.propagation;
        SubPanel.SendEvent(nestedEvt);

        if (nestedEvt.isPropagationStopped || nestedEvt.isImmediatePropagationStopped)
            original.StopImmediatePropagation();
    }

    void DispatchNestedKeyEvent<T>(
        T original,
        Func<char, KeyCode, EventModifiers, T> getPooled)
        where T : KeyboardEventBase<T>, new()
    {
        using var nestedEvt = getPooled(
            original.character,
            original.keyCode,
            original.modifiers);
        nestedEvt.propagation = original.propagation;
        SubPanel.SendEvent(nestedEvt);

        if (nestedEvt.isPropagationStopped || nestedEvt.isImmediatePropagationStopped)
        {
            original.StopImmediatePropagation();
        }
    }

    void DispatchNestedNavigationEvent(
        NavigationMoveEvent original,
        Func<Vector2, EventModifiers, NavigationMoveEvent> getPooled)
    {
        var element = default(VisualElement);
        switch (original.direction)
        {
            case NavigationMoveEvent.Direction.None:
                break;
            case NavigationMoveEvent.Direction.Left:
                break;
            case NavigationMoveEvent.Direction.Up:
                break;
            case NavigationMoveEvent.Direction.Right:
                break;
            case NavigationMoveEvent.Direction.Down:
                break;
            case NavigationMoveEvent.Direction.Next:
                element = GetNextFocusableInTree(subRootVisualElement, subRootVisualElement);
                break;
            case NavigationMoveEvent.Direction.Previous:
                element = GetPreviousFocusableInTree(subRootVisualElement, subRootVisualElement);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }


        using var nestedEvt = getPooled(
            original.move,
            original.modifiers);
        nestedEvt.propagation = original.propagation;
        SubPanel.SendEvent(nestedEvt);

        if (subRootVisualElement.focusController.GetLeafFocusedElement() != element)
        {
            if (nestedEvt.isPropagationStopped || nestedEvt.isImmediatePropagationStopped)
                original.StopImmediatePropagation();
        }
        else
        {
            subRootVisualElement.focusController.focusedElement?.Blur();

            switch (original.direction)
            {
                case NavigationMoveEvent.Direction.None:
                    break;
                case NavigationMoveEvent.Direction.Left:
                    break;
                case NavigationMoveEvent.Direction.Up:
                    break;
                case NavigationMoveEvent.Direction.Right:
                    break;
                case NavigationMoveEvent.Direction.Down:
                    break;
                case NavigationMoveEvent.Direction.Next:
                    this.focusController.SwitchFocus(GetNextFocusableInTree(this, panel.visualTree), true);
                    break;
                case NavigationMoveEvent.Direction.Previous:
                    this.focusController.SwitchFocus(GetPreviousFocusableInTree(this, panel.visualTree), true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }
    }

    static VisualElement GetNextFocusableInTree(VisualElement currentFocusable, VisualElement root)
    {
        if (currentFocusable == null)
        {
            return null;
        }

        VisualElement ve = GetNextElementDepthFirst(currentFocusable, root);
        while (!ve.canGrabFocus || ve.tabIndex < 0 || ve.excludeFromFocusRing)
        {
            ve = GetNextElementDepthFirst(ve, root);

            if (ve == currentFocusable)
            {
                // We went through the whole tree and did not find anything.
                return currentFocusable;
            }
        }

        return ve;
    }

    static VisualElement GetPreviousFocusableInTree(VisualElement currentFocusable, VisualElement root)
    {
        if (currentFocusable == null)
        {
            return null;
        }

        var ve = GetPreviousElementDepthFirst(currentFocusable, root);
        while (!ve.canGrabFocus || ve.tabIndex < 0 || ve.excludeFromFocusRing)
        {
            ve = GetPreviousElementDepthFirst(ve, root);

            if (ve == currentFocusable)
            {
                // We went through the whole tree and did not find anything.
                return currentFocusable;
            }
        }

        return ve;
    }

    static VisualElement GetNextElementDepthFirst(VisualElement ve, VisualElement root)
    {
        ve = ve.GetNextElementDepthFirst();

        if (!root.Contains(ve))
        {
            // continue at the beginning
            ve = root;
        }

        return ve;
    }

    static VisualElement GetPreviousElementDepthFirst(VisualElement ve, VisualElement root)
    {
        ve = ve.GetPreviousElementDepthFirst();

        if (!root.Contains(ve))
        {
            // continue at the end
            ve = root;
            while (ve.childCount > 0)
            {
                ve = ve.hierarchy.ElementAt(ve.childCount - 1);
            }
        }

        return ve;
    }
}
