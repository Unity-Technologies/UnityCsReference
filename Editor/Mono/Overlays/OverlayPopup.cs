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

        public Overlay overlay { get; private set; }

        OverlayPopup(Overlay overlay)
        {
            name = "overlay-popup";
            this.overlay = overlay;
            Overlay.treeAsset.CloneTree(this);

            this.Q(Overlay.k_CollapsedContent)?.RemoveFromHierarchy();
            this.Q(null, Overlay.k_Header)?.RemoveFromHierarchy();

            focusable = true;
            pickingMode = PickingMode.Position;
            AddToClassList(Overlay.ussClassName);
            style.position = Position.Absolute;

            var root = this.Q("overlay-content");
            root.renderHints = RenderHints.ClipWithScissors;
            root.Add(overlay.GetSimpleHeader());
            root.Add(overlay.CreatePanelContent());

            RegisterCallback<MouseEnterEvent>(evt => m_CursorIsOverPopup = true);
            RegisterCallback<MouseLeaveEvent>(evt => m_CursorIsOverPopup = false);
        }

        public static OverlayPopup CreateUnderOverlay(Overlay overlay)
        {
            var popup = new OverlayPopup(overlay);

            popup.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var proposed = overlay.collapsedButtonRect;
                proposed.size = evt.newRect.size;

                if (overlay.layout == Layout.HorizontalToolbar)
                    popup.EnableInClassList(k_FromHorizontal, true);
                else if (overlay.layout == Layout.VerticalToolbar)
                    popup.EnableInClassList(k_FromVertical, true);

                if (!overlay.isInToolbar)
                    popup.EnableInClassList(k_OutsideToolbar, true);

                var overlayWorldBound = overlay.rootVisualElement.worldBound;
                var placement = OverlayCanvas.ClampRectToBounds(overlay.canvas.windowRoot.worldBound, proposed);
                popup.HandleGeometryChangedEvent(overlay.canvas, placement, overlayWorldBound);
            });
            return popup;
        }

        public static OverlayPopup CreateAtPosition(OverlayCanvas canvas, Overlay overlay, Vector2 position)
        {
            var popup = new OverlayPopup(overlay);

            popup.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                //Use mouse position to set the popup to the right coordinates
                var proposed = new Rect(position, evt.newRect.size);
                var overlayWorldBound = new Rect(position, Vector2.zero);

                var placement = OverlayCanvas.ClampRectToBounds(canvas.windowRoot.worldBound, proposed);
                if (!Mathf.Approximately(proposed.position.x, placement.position.x))
                    popup.EnableInClassList(k_Clamped, true);

                popup.HandleGeometryChangedEvent(canvas, placement, overlayWorldBound);
            });
            return popup;
        }

        public static OverlayPopup CreateAtCanvasCenter(OverlayCanvas canvas, Overlay overlay)
        {
            var popup = new OverlayPopup(overlay);

            popup.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var size = evt.newRect.size;
                var parentRect = canvas.rootVisualElement.rect;
                var middle = parentRect.size / 2f;
                var position = middle - size / 2f;

                var placement = OverlayCanvas.ClampRectToBounds(canvas.windowRoot.worldBound, new Rect(position, size));
                popup.Place(placement);
            });
            return popup;
        }

        void HandleGeometryChangedEvent(OverlayCanvas canvas, Rect placement, Rect overlayWorldBound)
        {
            var canvasWorld = canvas.rootVisualElement.worldBound;

            var rightPlacement = overlayWorldBound.x + overlayWorldBound.width;
            var rightSideSpace = canvasWorld.xMax - rightPlacement;

            var xAdjusted = placement.position.x;
            if (rightSideSpace >= placement.width)
            {
                xAdjusted = rightPlacement;
            }
            else
            {
                var leftSideSpace = placement.x - canvas.rootVisualElement.worldBound.x;
                if (leftSideSpace >= placement.width)
                {
                    xAdjusted = overlayWorldBound.x - placement.width;
                }
                else // If neither side has enough space, show the popup on the widest one
                {
                    if (rightSideSpace > leftSideSpace)
                        xAdjusted = overlayWorldBound.x + overlayWorldBound.width;
                    else
                        xAdjusted = overlayWorldBound.x - placement.width;

                    placement.width = canvasWorld.xMax - xAdjusted;
                }
            }

            var yAdjusted = placement.position.y;
            var bottomSpace = canvasWorld.yMax - yAdjusted;

            if (bottomSpace < placement.height)
            {
                var upPlacement = overlayWorldBound.y + overlayWorldBound.height;
                var upSpace = upPlacement - canvasWorld.y;
                if (upSpace >= placement.height)
                {
                    yAdjusted = upPlacement - placement.height;
                }
                else // If neither side has enough space, show the popup on the widest one
                {
                    // Try to show the popup as clamped if possible
                    EnableInClassList(k_Clamped, true);
                    if (bottomSpace <= upSpace)
                    {
                        var oldY = yAdjusted;
                        yAdjusted = canvasWorld.yMin;
                        placement.height = oldY - yAdjusted;
                    }
                    else
                    {
                        placement.height = canvasWorld.yMax - yAdjusted;
                    }
                }
            }

            placement.position = new Vector2(xAdjusted, yAdjusted) - canvasWorld.position;

            Place(placement);
        }

        void Place(Rect placement)
        {
            style.maxHeight = placement.height;
            style.maxWidth = placement.width;
            transform.position = placement.position;
        }
    }
}
