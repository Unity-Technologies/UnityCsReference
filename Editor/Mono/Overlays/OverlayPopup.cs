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

        public OverlayPopup(Overlay overlay)
        {
            name = "overlay-popup";
            Overlay.treeAsset.CloneTree(this);

            var background = this.Q(Overlay.k_Background);
            var backgroundColor = background.style.backgroundColor.value;
            backgroundColor.a = .95f;
            background.style.backgroundColor = backgroundColor;

            this.Q(Overlay.k_CollapsedContent)?.RemoveFromHierarchy();
            this.Q(null, Overlay.k_Header)?.RemoveFromHierarchy();

            focusable = true;
            pickingMode = PickingMode.Position;
            AddToClassList(Overlay.ussClassName);
            style.position = Position.Absolute;

            var root = this.Q("overlay-content");
            root.renderHints = RenderHints.ClipWithScissors;
            root.Add(overlay.CreatePanelContentSafe());

            RegisterCallback<MouseEnterEvent>(evt => m_CursorIsOverPopup = true);
            RegisterCallback<MouseLeaveEvent>(evt => m_CursorIsOverPopup = false);

            RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var proposed = overlay.m_CollapsedContent.worldBound;
                proposed.size = evt.newRect.size;
                var placement = OverlayCanvas.ClampRectToBounds(overlay.canvas.windowRoot.worldBound, proposed);
                var canvasWorld = overlay.canvas.rootVisualElement.worldBound;

                style.left = placement.x - canvasWorld.x;
                style.top = placement.y - canvasWorld.y;
            });
        }
    }
}
