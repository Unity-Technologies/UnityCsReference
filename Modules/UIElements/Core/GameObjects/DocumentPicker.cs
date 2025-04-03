// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Pool;

namespace UnityEngine.UIElements;

internal class PhysicsDocumentPicker
{
    public void Pick(Ray worldRay, float maxDistance, int layerMask, out UIDocument document, out VisualElement pickedElement, out float distance)
    {
        var result = WorldSpaceInput.PickDocument3D(worldRay, maxDistance, layerMask);
        document = result.document;
        pickedElement = result.pickedElement;
        distance = result.distance;
    }

    public bool TryPickWithCapture(int pointerId, Ray worldRay, float maxDistance, int layerMask, out UIDocument document, out VisualElement elementUnderPointer, out float distance, out bool captured)
    {
        captured = GetCapturingDocument(pointerId, out var capturingDocument);
        if (captured)
        {
            if (capturingDocument != null && ((1 << capturingDocument.gameObject.layer) & layerMask) != 0)
            {
                document = capturingDocument;
                elementUnderPointer = WorldSpaceInput.Pick3D(document, worldRay, out distance);
                return true;
            }
        }
        else
        {
            Pick(worldRay, maxDistance, layerMask, out document, out elementUnderPointer, out distance);
            return !float.IsPositiveInfinity(distance);
        }

        document = null;
        elementUnderPointer = null;
        distance = 0;
        return false;
    }

    bool GetCapturingDocument(int pointerId, out UIDocument capturingDocument)
    {
        var capturingElement = RuntimePanel.s_EventDispatcher.pointerState.GetCapturingElement(pointerId);
        if (capturingElement is VisualElement capturingVE)
        {
            var capturingElementPanel = capturingVE.elementPanel;
            if (capturingElementPanel != null && !capturingElementPanel.isFlat)
            {
                capturingDocument = UIDocument.FindParentDocument(capturingVE);
                return true;
            }
        }

        var capturingPanel = PointerDeviceState.GetPlayerPanelWithSoftPointerCapture(pointerId);
        if (capturingPanel != null)
        {
            if (!capturingPanel.isFlat)
            {
                capturingDocument = PointerDeviceState.GetWorldSpaceDocumentWithSoftPointerCapture(pointerId);
                return true;
            }
        }

        capturingDocument = null;
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
