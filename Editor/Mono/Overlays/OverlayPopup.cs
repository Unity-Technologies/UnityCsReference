// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    class OverlayPopup : VisualElement
    {
        const int k_Margin = 4;

        internal enum Anchor
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Center
        }

        // FocusOutEvent.originalMousePosition is not valid, so we keep track of where the mouse is when clicking.
        bool m_CursorIsOverPopup;
        public bool containsCursor => m_CursorIsOverPopup;
        const string k_OutsideToolbar = "overlay-popup--outside-toolbar";
        const string k_FromHorizontal = "overlay-popup--from-horizontal";
        const string k_FromVertical = "overlay-popup--from-vertical";
        const string k_Clamped = "overlay-popup--clamped";

        public Overlay overlay { get; private set; }

        bool m_ShouldRecalculateAnchors;
        bool m_UseMargins;
        bool m_Horizontal;
        Rect m_TargetRect;
        Vector2 m_Margin = Vector2.zero;
        Anchor m_PopupAnchor = Anchor.Center;
        Anchor m_TargetAnchor = Anchor.Center;

        internal static void GetAnchors(Rect targetElement, Rect container, bool horizontal, Vector2 size, out Anchor popupAnchor, out Anchor targetAnchor, out Vector2 margin)
        {
            float DistanceToEdge(float targetPos, float edgePos)
            {
                return Mathf.Abs(edgePos - targetPos);
            }

            float distAbove = DistanceToEdge(targetElement.yMin, container.yMin);
            float distBelow = DistanceToEdge(targetElement.yMax, container.yMax);
            float distLeft = DistanceToEdge(targetElement.xMin, container.xMin);
            float distRight = DistanceToEdge(targetElement.xMax, container.xMax);

            bool showUnder = distBelow >= size.y || distBelow > distAbove;
            bool showRight = distRight >= size.x || distRight > distLeft;

            if (horizontal)
            {
                if (showUnder)
                {
                    popupAnchor = showRight ? Anchor.TopLeft : Anchor.TopRight;
                    targetAnchor = showRight ? Anchor.BottomLeft : Anchor.BottomRight;
                }
                else
                {
                    popupAnchor = showRight ? Anchor.BottomLeft : Anchor.BottomRight;
                    targetAnchor = showRight ? Anchor.TopLeft : Anchor.TopRight;
                }

                margin = new Vector2(0, showUnder ? k_Margin : -k_Margin);
            }
            else
            {
                if (showUnder)
                {
                    popupAnchor = showRight ? Anchor.TopLeft : Anchor.TopRight;
                    targetAnchor = showRight ? Anchor.TopRight : Anchor.TopLeft;
                }
                else
                {
                    popupAnchor = showRight ? Anchor.BottomLeft : Anchor.BottomRight;
                    targetAnchor = showRight ? Anchor.BottomRight : Anchor.BottomLeft;
                }

                margin = new Vector2(showRight ? k_Margin : -k_Margin, 0);
            }
        }

        internal static Rect PlaceNextToTarget(Vector2 size, Rect targetRect, Rect containerRect, Anchor popupAnchor, Anchor targetAnchor, Vector2 margin)
        {
            Vector2 targetPos = targetRect.center;
            switch (targetAnchor)
            {
                case Anchor.TopLeft: targetPos = new Vector2(targetRect.xMin, targetRect.yMin); break;
                case Anchor.TopRight: targetPos = new Vector2(targetRect.xMax, targetRect.yMin); break;
                case Anchor.BottomLeft: targetPos = new Vector2(targetRect.xMin, targetRect.yMax); break;
                case Anchor.BottomRight: targetPos = new Vector2(targetRect.xMax, targetRect.yMax); break;
            }

            var popupRect = new Rect(targetPos, size);
            popupRect.position += margin;

            switch (popupAnchor)
            {
                case Anchor.TopRight: popupRect.position -= new Vector2(size.x, 0); break;
                case Anchor.BottomLeft: popupRect.position -= new Vector2(0, size.y); break;
                case Anchor.BottomRight: popupRect.position -= new Vector2(size.x, size.y); break;
                case Anchor.Center: popupRect.position -= new Vector2(size.x * .5f, size.y * .5f); break;
            }

            return OverlayUtilities.ClampRectToRect(popupRect, containerRect);
        }

        void UpdatePosition(Vector2 size)
        {
            var targetRect = m_TargetRect;
            var containerRect = parent.rect;

            EnableInClassList(k_Clamped, size.x < containerRect.size.x || size.y < containerRect.size.y);

            style.translate = PlaceNextToTarget(size, targetRect, containerRect, m_PopupAnchor, m_TargetAnchor, m_UseMargins ? m_Margin : Vector2.zero).position;
        }

        OverlayPopup(Overlay overlay, Rect targetRect, bool horizontal = true, bool includeMargins = false)
        {
            name = "overlay-popup";
            this.overlay = overlay;
            m_TargetRect = targetRect;
            m_Horizontal = horizontal;
            m_UseMargins = includeMargins;
            Overlay.treeAsset.CloneTree(this);

            this.Q(Overlay.k_CollapsedContent)?.RemoveFromHierarchy();
            this.Q(null, Overlay.k_Header)?.RemoveFromHierarchy();

            focusable = true;
            pickingMode = PickingMode.Position;
            AddToClassList(Overlay.ussClassName);
            style.position = Position.Absolute;

            Refresh();

            RegisterCallback<MouseEnterEvent>(evt => m_CursorIsOverPopup = true);
            RegisterCallback<MouseLeaveEvent>(evt => m_CursorIsOverPopup = false);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // Only update when size changes
            if (evt.oldRect.size == evt.newRect.size)
                return;

            if (m_ShouldRecalculateAnchors)
            {
                GetAnchors(m_TargetRect, parent.rect, m_Horizontal, evt.newRect.size, out m_PopupAnchor, out m_TargetAnchor, out m_Margin);
                m_ShouldRecalculateAnchors = false;
            }

            UpdatePosition(evt.newRect.size);
        }

        public void Refresh()
        {
            var root = this.Q("overlay-content");

            root.Clear();

            root.renderHints = RenderHints.ClipWithScissors;
            style.maxHeight = StyleKeyword.Null;
            style.maxWidth = StyleKeyword.Null;

            root.Add(overlay.GetSimpleHeader());
            root.Add(overlay.CreatePanelContent());

            root.Focus();
        }

        public static OverlayPopup CreateUnderOverlay(Overlay overlay)
        {
            var popup = new OverlayPopup(overlay, overlay.canvas.rootVisualElement.WorldToLocal(overlay.collapsedButtonRect), !overlay.isInToolbar || overlay.activeLayout != Layout.VerticalToolbar, true);
            popup.m_ShouldRecalculateAnchors = true;

            if (overlay.layout == Layout.HorizontalToolbar)
                popup.EnableInClassList(k_FromHorizontal, true);
            else if (overlay.layout == Layout.VerticalToolbar)
                popup.EnableInClassList(k_FromVertical, true);

            if (!overlay.isInToolbar)
                popup.EnableInClassList(k_OutsideToolbar, true);

            return popup;
        }

        public static OverlayPopup CreateAtPosition(OverlayCanvas canvas, Overlay overlay, Vector2 position)
        {
            return new OverlayPopup(overlay, new Rect(canvas.rootVisualElement.WorldToLocal(position), Vector2.zero))
            {
                m_ShouldRecalculateAnchors = true,
            };
        }

        public static OverlayPopup CreateAtCanvasCenter(OverlayCanvas canvas, Overlay overlay)
        {
            return new OverlayPopup(overlay, new Rect(canvas.rootVisualElement.rect.size / 2, Vector2.zero));
        }
    }
}
