// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements;

internal static class PhysicsDocumentPicker
{
    // pickedElement is a visualElement but is not identified as such for code stripping reasons
    private static void Pick(Ray worldRay, float maxDistance, int layerMask, out Collider collider, out IPanelComponent panelComponent, out IEventHandler pickedElement, out float distance)
    {
        var result = WorldSpaceInput.PickDocument3D(worldRay, maxDistance, layerMask);
        collider = result.collider;
        panelComponent = result.panelComponent;
        pickedElement = result.pickedElement;
        distance = result.distance;
    }

    // pickedElement is a visualElement but is not identified as such for code stripping reasons
    public static bool TryPickWithCapture(int pointerId, Ray worldRay, float maxDistance, int layerMask, out Collider collider, out IPanelComponent panelComponent, out IEventHandler elementUnderPointer, out float distance, out bool captured)
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

    static bool GetCapturingDocument(int pointerId, out IPanelComponent capturingComponent)
    {
        IRuntimePanel.uIElementsRuntimeUtility.GetCapturingElement(pointerId, out var panel, out var capturingElement);
        if (capturingElement is not null)
        {
            var capturingElementPanel = panel;
            if (capturingElementPanel != null && !capturingElementPanel.isFlat)
            {
                capturingComponent = ((VisualElement)capturingElement).FindRootPanelComponent();
                if (capturingComponent != null) // UUM-117081: don't hang on to an invalid capture
                    return true;
            }
        }

        var capturingPanel = IRuntimePanel.pointerDeviceState.GetPlayerPanelWithSoftPointerCapture(pointerId);
        if (capturingPanel != null)
        {
            if (!capturingPanel.isFlat)
            {
                capturingComponent = IRuntimePanel.pointerDeviceState.GetWorldSpacePanelComponentWithSoftPointerCapture(pointerId);
                if (capturingComponent != null) // UUM-117081: don't hang on to an invalid capture
                    return true;
            }
        }

        capturingComponent = null;
        return false;
    }
}

internal static class ScreenOverlayPanelPicker
{
    public static bool TryPick(IRuntimePanel panel, int pointerId, Vector2 screenPosition, Vector2 delta,
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

    static bool GetCapturingPanel(int pointerId, out IRuntimePanel capturingPanel)
    {
        IRuntimePanel.uIElementsRuntimeUtility.GetCapturingElement(pointerId, out var panel, out var capturingElement);
        if (capturingElement != null )
        {
            capturingPanel = panel;
        }
        else
        {
            capturingPanel = IRuntimePanel.pointerDeviceState.GetPlayerPanelWithSoftPointerCapture(pointerId);
        }

        return capturingPanel != null;
    }
}
