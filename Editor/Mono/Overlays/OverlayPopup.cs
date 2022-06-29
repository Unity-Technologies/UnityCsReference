// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    class OverlayPopup : VisualElement
    {
        // FocusOutEvent.originalMousePosition is not valid, so we keep track of where the mouse is when clicking.
        bool m_CursorIsOverPopup;
        public bool containsCursor => m_CursorIsOverPopup;
        const string k_OutsideToolbar = "overlay-popup--outside-toolbar";
        const string k_FromHorizontal = "overlay-popup--from-horizontal";
        const string k_FromVertical = "overlay-popup--from-vertical";
        const string k_Clamped = "overlay-popup--clamped";

        public OverlayPopup(Overlay overlay)
        {
            name = "overlay-popup";
            Overlay.treeAsset.CloneTree(this);

            this.Q(Overlay.k_CollapsedContent)?.RemoveFromHierarchy();
            this.Q(null, Overlay.k_Header)?.RemoveFromHierarchy();

            focusable = true;
            pickingMode = PickingMode.Position;
            AddToClassList(Overlay.ussClassName);
            style.position = Position.Absolute;

            var root = this.Q("overlay-content");
            root.renderHints = RenderHints.ClipWithScissors;
            root.Add(overlay.CreatePanelContent());

            RegisterCallback<MouseEnterEvent>(evt => m_CursorIsOverPopup = true);
            RegisterCallback<MouseLeaveEvent>(evt => m_CursorIsOverPopup = false);

            RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var proposed = overlay.collapsedButtonRect;
                proposed.size = evt.newRect.size;
                var placement = OverlayCanvas.ClampRectToBounds(overlay.canvas.windowRoot.worldBound, proposed);

                if (!Mathf.Approximately(proposed.position.x, placement.position.x))
                    this.EnableInClassList(k_Clamped, true);

                var canvasWorld = overlay.canvas.rootVisualElement.worldBound;

                if (overlay.layout == Layout.HorizontalToolbar)
                    this.EnableInClassList(k_FromHorizontal, true);
                else if (overlay.layout == Layout.VerticalToolbar)
                    this.EnableInClassList(k_FromVertical, true);

                if (!overlay.isInToolbar)
                {
                    this.EnableInClassList(k_OutsideToolbar, true);
                    var overlayWorldBound = overlay.rootVisualElement.worldBound;

                    var rightPlacement = overlayWorldBound.x + overlayWorldBound.width;
                    var rightSideSpace = canvasWorld.xMax - rightPlacement;

                    var xAdjusted = placement.position.x;
                    if (rightSideSpace >= placement.width)
                        xAdjusted = rightPlacement;
                    else
                    {
                        var leftSideSpace = placement.x - overlay.canvas.rootVisualElement.worldBound.x;
                        if (leftSideSpace >= placement.width)
                            xAdjusted = overlayWorldBound.x - placement.width;
                        else // If neither side has enough space, show the popup on the widest one
                        {
                            if (rightSideSpace > leftSideSpace)
                                xAdjusted = overlayWorldBound.x + overlayWorldBound.width;
                            else
                                xAdjusted = overlayWorldBound.x - placement.width;
                        }
                    }
                    placement.position = new Vector2(xAdjusted, placement.position.y);
                }

                style.maxWidth = canvasWorld.xMax - placement.position.x;
                style.maxHeight = canvasWorld.yMax - placement.position.y;
                transform.position = placement.position - canvasWorld.position;
            });
        }
    }
}
