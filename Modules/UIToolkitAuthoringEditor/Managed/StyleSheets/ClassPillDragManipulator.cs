// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Hierarchy;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

class ClassPillDragManipulator : MouseManipulator
{
    readonly string ClassName;
    readonly ClassPill Pill;
    readonly StyleSheetNodeTypeHandler Handler;

    const float k_DragThreshold = 5f;
    Vector2 m_MouseDownPosition;

    public ClassPillDragManipulator(string selectorString, ClassPill pill, StyleSheetNodeTypeHandler handler)
    {
        ClassName = selectorString.Substring(1);
        Pill = pill;
        Handler = handler;
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
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

    void OnMouseDown(MouseDownEvent evt)
    {
        if (!CanStartManipulation(evt))
            return;

        // Don't stop propagation for double-clicks so rename can trigger
        if (evt.clickCount >= 2)
            return;

        m_MouseDownPosition = evt.mousePosition;
        target.CaptureMouse();
        evt.StopPropagation();
    }

    void OnMouseMove(MouseMoveEvent evt)
    {
        if (!target.HasMouseCapture())
            return;

        var delta = (m_MouseDownPosition - evt.mousePosition).magnitude;
        if (delta < k_DragThreshold)
            return;

        target.ReleaseMouse();

        DragAndDrop.PrepareStartDrag();
        DragAndDrop.SetGenericData(StyleSheetNodeTypeHandler.DraggedSelectorKey, ClassName);

        // Adds support for reordering between style sheets when dragging the rule
        var hierarchyItem = target.GetFirstAncestorOfType<HierarchyViewItem>();
        var isValidNode = hierarchyItem?.Node != null && hierarchyItem.Node != HierarchyNode.Null;
        if (isValidNode)
        {
            if (Handler.Mappings.TryGetValue(hierarchyItem.Node, out var styleNode) && styleNode.Rule != null)
            {
                var draggedRules = new List<StyleRule> { styleNode.Rule };
                DragAndDrop.SetGenericData(StyleSheetEditingNodeTypeHandler.DraggedRulesKey, draggedRules);
            }
        }

        // The DragPreviewWindow is a bit finicky in Windows OS ATM. Once we fix it, we can re-enable this line.
        // Handler.StartDragPreview(Pill);

        DragAndDrop.StartDrag(Pill.text);

        // Update the selection when a selector pill is being dragged. The MouseUp/PointerUp will not always be called
        // since we support dragging out of the window.
        if (isValidNode)
        {
            Handler.Window?.HierarchyView?.SetSelection(new[] { hierarchyItem.Node });
            Handler.Window?.GlobalSelectionHandler?.SyncGlobalSelectionFromViewModel();
        }

        evt.StopPropagation();
    }

    void OnMouseUp(MouseUpEvent evt)
    {
        if (target.HasMouseCapture())
            target.ReleaseMouse();

        // When clicking on the pill, we want to update the selection. Mostly hit when clicking into the class pill.
        var hierarchyItem = target.GetFirstAncestorOfType<HierarchyViewItem>();
        if (hierarchyItem?.Node == null || hierarchyItem.Node == HierarchyNode.Null)
            return;

        Handler.Window?.HierarchyView?.SetSelection(new[] { hierarchyItem.Node });
        Handler.Window?.GlobalSelectionHandler?.SyncGlobalSelectionFromViewModel();
    }
}
