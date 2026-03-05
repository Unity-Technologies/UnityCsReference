// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Pool;

namespace UnityEngine.UIElements;

internal class PhysicsDocumentPicker
{
    private void Pick(Ray worldRay, float maxDistance, int layerMask, out Collider collider, out IPanelComponent panelComponent, out VisualElement pickedElement, out float distance)
    {
        var result = WorldSpaceInput.PickDocument3D(worldRay, maxDistance, layerMask);
        collider = result.collider;
        panelComponent = result.panelComponent;
        pickedElement = result.pickedElement;
        distance = result.distance;
    }

    public bool TryPickWithCapture(int pointerId, Ray worldRay, float maxDistance, int layerMask, out Collider collider, out IPanelComponent panelComponent, out VisualElement elementUnderPointer, out float distance, out bool captured)
    {
        captured = GetCapturingDocument(pointerId, out var capturingDocument);
        if (!captured)
        {
            Pick(worldRay, maxDistance, layerMask, out collider, out panelComponent, out elementUnderPointer, out distance);
            return !float.IsPositiveInfinity(distance);
        }

        if (capturingDocument != null && ((1 << capturingDocument.gameObject.layer) & layerMask) != 0)
        {
            collider = null; // This may not be the expected behaviour. We should consider returning a collider.
            panelComponent = capturingDocument;
            elementUnderPointer = WorldSpaceInput.Pick3D(panelComponent, worldRay, out distance);
            return true;
        }

        collider = null;
        panelComponent = null;
        elementUnderPointer = null;
        distance = 0;
        return false;
    }

    bool GetCapturingDocument(int pointerId, out IPanelComponent capturingComponent)
    {
        var capturingElement = RuntimePanel.s_EventDispatcher.pointerState.GetCapturingElement(pointerId);
        if (capturingElement is VisualElement capturingVE)
        {
            var capturingElementPanel = capturingVE.elementPanel;
            if (capturingElementPanel != null && !capturingElementPanel.isFlat)
            {
                capturingComponent = capturingVE.FindRootPanelComponent();
                if (capturingComponent != null) // UUM-117081: don't hang on to an invalid capture
                    return true;
            }
        }

        var capturingPanel = PointerDeviceState.GetPlayerPanelWithSoftPointerCapture(pointerId);
        if (capturingPanel != null)
        {
            if (!capturingPanel.isFlat)
            {
                capturingComponent = PointerDeviceState.GetWorldSpacePanelComponentWithSoftPointerCapture(pointerId);
                if (capturingComponent != null) // UUM-117081: don't hang on to an invalid capture
                    return true;
            }
        }

        capturingComponent = null;
        return false;
    }
}

internal class ScreenOverlayPanelPicker
{
    public bool TryPick(BaseRuntimePanel panel, int pointerId, Vector2 screenPosition, Vector2 delta,
        int? targetDisplay, out bool captured)
    {
        // Even if we have capture, we don't have a way to compute panel position from another display so we need to
        // reject the event.
        if (targetDisplay != null && targetDisplay != panel.targetDisplay)
        {
            captured = false;
            return false;
        }

        captured = GetCapturingPanel(pointerId, out var capturingPanel);
        if (captured)
        {
            if (capturingPanel == panel)
                return true;
        }
        else
        {
            if (panel.ScreenToPanel(screenPosition, delta, out var panelPosition))
            {
                var pick = panel.Pick(panelPosition, pointerId);
                if (pick != null)
                    return true;
            }
        }

        return false;
    }

    bool GetCapturingPanel(int pointerId, out BaseVisualElementPanel capturingPanel)
    {
        var capturingElement = RuntimePanel.s_EventDispatcher.pointerState.GetCapturingElement(pointerId);
        if (capturingElement is VisualElement capturingVE)
        {
            capturingPanel = capturingVE.elementPanel;
        }
        else
        {
            capturingPanel = PointerDeviceState.GetPlayerPanelWithSoftPointerCapture(pointerId);
        }

        return capturingPanel != null;
    }
}
