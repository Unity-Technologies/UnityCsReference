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
            root.Add(overlay.GetSimpleHeader());
            root.Add(overlay.CreatePanelContent());

            RegisterCallback<MouseEnterEvent>(evt => m_CursorIsOverPopup = true);
            RegisterCallback<MouseLeaveEvent>(evt => m_CursorIsOverPopup = false);

            RegisterCallback<GeometryChangedEvent>(evt =>
            {

                var proposed = overlay.collapsedButtonRect;
                proposed.size = evt.newRect.size;

                if (overlay.layout == Layout.HorizontalToolbar)
                    this.EnableInClassList(k_FromHorizontal, true);
                else if (overlay.layout == Layout.VerticalToolbar)
                    this.EnableInClassList(k_FromVertical, true);

                if (!overlay.isInToolbar)
                    this.EnableInClassList(k_OutsideToolbar, true);

                var overlayWorldBound = overlay.rootVisualElement.worldBound;
                var placement = OverlayCanvas.ClampRectToBounds(overlay.canvas.windowRoot.worldBound, proposed);

                if (!Mathf.Approximately(proposed.position.x, placement.position.x))
                    this.EnableInClassList(k_Clamped, true);

                HandleGeometryChangedEvent(overlay.canvas, placement, overlayWorldBound);
            });
        }

        public OverlayPopup(OverlayCanvas canvas, Overlay overlay)
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
            root.Add(overlay.GetSimpleHeader());
            root.Add(overlay.CreatePanelContent());

            var mousePosition = PointerDeviceState.GetPointerPosition(PointerId.mousePointerId, ContextType.Editor);
            mousePosition = canvas.rootVisualElement.WorldToLocal(mousePosition);

            RegisterCallback<MouseEnterEvent>(evt => m_CursorIsOverPopup = true);
            RegisterCallback<MouseLeaveEvent>(evt => m_CursorIsOverPopup = false);

            RegisterCallback<GeometryChangedEvent>(evt =>
            {
                //Use mouse position to set the popup to the right coordinates
                var proposed = new Rect(mousePosition + new Vector2(0f,evt.newRect.height/2f), evt.newRect.size);
                var overlayWorldBound = new Rect(mousePosition, Vector2.zero);

                if (float.IsNaN(overlayWorldBound.width))
                    overlayWorldBound.width = 0f;
                if (float.IsNaN(overlayWorldBound.height))
                    overlayWorldBound.height = 0f;

                var placement = OverlayCanvas.ClampRectToBounds(canvas.windowRoot.worldBound, proposed);
                if (!Mathf.Approximately(proposed.position.x, placement.position.x))
                    this.EnableInClassList(k_Clamped, true);

                HandleGeometryChangedEvent(canvas, placement, overlayWorldBound);
            });
        }

        void HandleGeometryChangedEvent(OverlayCanvas canvas, Rect placement, Rect overlayWorldBound)
        {
                var canvasWorld = canvas.rootVisualElement.worldBound;

                var rightPlacement = overlayWorldBound.x + overlayWorldBound.width;
                var rightSideSpace = canvasWorld.xMax - rightPlacement;

                var xAdjusted = placement.position.x;
                var maxWidth = placement.width;
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

                        maxWidth = canvasWorld.xMax - xAdjusted;
                    }
                }

                var yAdjusted = placement.position.y;
                var bottomSpace = canvasWorld.yMax - yAdjusted;

                var maxHeight = placement.height;
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
                        if (bottomSpace <= upSpace)
                            yAdjusted = upPlacement - placement.height;

                        maxHeight = canvasWorld.yMax - yAdjusted;
                    }
                }

                placement.position = new Vector2(xAdjusted, yAdjusted);

                style.maxHeight = maxHeight;
                style.maxWidth = maxWidth;

                transform.position = placement.position - canvasWorld.position;
        }
    }
}
