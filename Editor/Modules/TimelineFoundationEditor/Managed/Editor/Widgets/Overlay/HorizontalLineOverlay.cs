// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    class HorizontalLineOverlay : Overlay
    {
        const string k_Name = "horizontalLineOverlay";

        static readonly CustomStyleProperty<float> s_SegmentWidthProperty = new CustomStyleProperty<float>("--segment-width");
        static readonly CustomStyleProperty<Color> s_LineColorProperty = new CustomStyleProperty<Color>("--line-color");

        public HorizontalLineOverlay()
        {
            this.StretchToParentSize();
            UIResources.OverlayStylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_Name);
            name = k_Name;

            generateVisualContent += GenerateVisualContent;
        }

        public void SetPositionFromWorld(Vector2 worldPosition)
        {
            Vector2 localPos = parent.WorldToLocal(worldPosition);
            SetLocalPosition(localPos);
        }

        public void SetLocalPosition(Vector2 localPosition)
        {
            style.translate = localPosition;
        }

        void GenerateVisualContent(MeshGenerationContext obj)
        {
            Rect bounds = layout;

            customStyle.TryGetValue(s_SegmentWidthProperty, out float segmentWidth);
            customStyle.TryGetValue(s_LineColorProperty, out Color color);

            if (segmentWidth > 0)
                obj.DrawHorizontalDottedLine(bounds.position, bounds.width, bounds.height, segmentWidth, color);
            else
                obj.DrawHorizontalLine(bounds.position, bounds.width, bounds.height, color);
        }
    }
}
