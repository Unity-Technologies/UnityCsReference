// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    public abstract partial class Overlay
    {
        public event Action<bool> floatingChanged;
        public event Action<Vector3> floatingPositionChanged;

        bool m_Floating;
        Vector2 m_FloatingSnapOffset;

        internal DockPosition dockPosition => container.topOverlays.Contains(this) ? DockPosition.Top : DockPosition.Bottom;
        internal SnapCorner floatingSnapCorner { get; private set; } = SnapCorner.TopLeft;

        internal Vector2 floatingSnapOffset
        {
            get => m_FloatingSnapOffset;
            private set
            {
                if (m_FloatingSnapOffset == value)
                    return;

                m_FloatingSnapOffset = value;
                UpdateAbsolutePosition();
                floatingPositionChanged?.Invoke(floatingPosition);
            }
        }

        //overlay floating position in window
        public Vector2 floatingPosition
        {
            get => SnapToFloatingPosition(floatingSnapCorner, floatingSnapOffset);
            set
            {
                var position = canvas.ClampToOverlayWindow(new Rect(value, rootVisualElement.rect.size)).position;
                FloatingToSnapPosition(position, out var snapCorner, out var snapOffset);
                floatingSnapCorner = snapCorner;
                floatingSnapOffset = snapOffset;
            }
        }

        public bool floating
        {
            get => m_Floating;
            internal set
            {
                if (m_Floating == value) return;
                m_Floating = value;
                OnFloatingChanged(value);
            }
        }

        void OnFloatingChanged(bool floating)
        {
            UpdateDropZones();
            UpdateStyling();

            if (floating)
                UpdateAbsolutePosition();

            container?.UpdateIsVisibleInContainer(this);
            UpdateLayoutBasedOnContainer();
            floatingChanged?.Invoke(floating);
        }

        public void Undock()
        {
            if (floating)
                return;

            canvas.floatingContainer.Add(rootVisualElement);
            floating = true;
        }

        Vector2 SnapToFloatingPosition(SnapCorner corner, Vector2 snapPosition)
        {
            switch (corner)
            {
                case SnapCorner.TopLeft:
                    return snapPosition;
                case SnapCorner.TopRight:
                    return new Vector2(canvas.floatingContainer.localBound.width - rootVisualElement.localBound.width + snapPosition.x, snapPosition.y);
                case SnapCorner.BottomLeft:
                    return new Vector2(snapPosition.x, canvas.floatingContainer.localBound.height - rootVisualElement.localBound.height + snapPosition.y);
                case SnapCorner.BottomRight:
                    return canvas.floatingContainer.localBound.size - rootVisualElement.localBound.size + snapPosition;
                default:
                    return Vector2.zero;
            }
        }

        void FloatingToSnapPosition(Vector2 floatingPosition, out SnapCorner snapCorner, out Vector2 snapOffset)
        {
            Rect containerRect = canvas.floatingContainer.localBound;
            var aTopLeft = containerRect.position;
            var aTopRight = containerRect.position + new Vector2(containerRect.width, 0);
            var aBottomLeft = containerRect.position + new Vector2(0, containerRect.height);
            var aBottomRight = containerRect.max;

            Rect overlayRect = new Rect(floatingPosition, rootVisualElement.localBound.size);
            var bTopLeft = overlayRect.position;
            var bTopRight = overlayRect.position + new Vector2(overlayRect.width, 0);
            var bBottomLeft = overlayRect.position + new Vector2(0, overlayRect.height);
            var bBottomRight = overlayRect.max;

            var topLeft = bTopLeft - aTopLeft;
            var topRight = bTopRight - aTopRight;
            var bottomLeft = bBottomLeft - aBottomLeft;
            var bottomRight = bBottomRight - aBottomRight;

            snapOffset = topLeft;
            snapCorner = SnapCorner.TopLeft;

            if (topRight.sqrMagnitude < snapOffset.sqrMagnitude)
            {
                snapOffset = topRight;
                snapCorner = SnapCorner.TopRight;
            }

            if (bottomLeft.sqrMagnitude < snapOffset.sqrMagnitude)
            {
                snapOffset = bottomLeft;
                snapCorner = SnapCorner.BottomLeft;
            }

            if (bottomRight.sqrMagnitude < snapOffset.sqrMagnitude)
            {
                snapOffset = bottomRight;
                snapCorner = SnapCorner.BottomRight;
            }
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.newRect.size != evt.oldRect.size)
                floatingPosition = floatingPosition; //Force a clamp of the container
        }

        internal void UpdateAbsolutePosition()
        {
            if (rootVisualElement.resolvedStyle.position == Position.Absolute)
            {
                var position = floatingPosition;
                rootVisualElement.style.left = position.x;
                rootVisualElement.style.top = position.y;
            }
        }
    }
}
