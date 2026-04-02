// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    class RectangleOverlay : Overlay
    {
        public RectangleOverlay()
        {
            UIResources.OverlayStylesheet.ApplyTo(this);
            this.AddToTimelineClassList("rectangleOverlay");
            name = "rectangleOverlay";
        }

        public void SetRectFromLocalRect(Rect localRect)
        {
            style.translate = localRect.position;
            style.width = localRect.width;
            style.height = localRect.height;
        }

        public void SetRectFromWorldRect(Rect worldRect)
        {
            Rect localRect = parent.WorldToLocal(worldRect);
            SetRectFromLocalRect(localRect);
        }

        public void ResetRect()
        {
            SetRectFromLocalRect(Rect.zero);
        }
    }
}
