// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Animations.AnimationWindow.TimelineFoundation
{
    class VerticalLineOverlay : CanvasOverlay
    {
        const string k_Name = "verticalLineOverlay";

        static readonly CustomStyleProperty<float> k_SegmentHeightProperty = new CustomStyleProperty<float>("--segment-height");
        static readonly CustomStyleProperty<Color> k_LineColorProperty = new CustomStyleProperty<Color>("--line-color");
        static readonly CustomStyleProperty<float> k_LineWidthProperty = new CustomStyleProperty<float>("--line-width");

        DiscreteTime m_Time;
        float m_WorldPixel;

        public DiscreteTime time
        {
            get => m_Time;
            set
            {
                m_Time = value;
                ForceUpdate();
            }
        }

        public VerticalLineOverlay()
        {
            this.StretchToParentSize();
            this.AddToTimelineClassList(k_Name);

            AddStyleSheetPath("StyleSheets/TimelineFoundation/Overlays.uss");

            if (EditorGUIUtility.isProSkin)
            {
                AddStyleSheetPath("StyleSheets/TimelineFoundation/OverlaysDark.uss");
            }
            else
            {
                AddStyleSheetPath("StyleSheets/TimelineFoundation/OverlaysLight.uss");
            }

            //UIResources.OverlayStylesheet.ApplyTo(this);
            name = k_Name;

            generateVisualContent += GenerateVisualContent;
        }

        protected override void Update(ICanvas canvas)
        {
            m_WorldPixel = canvas.TimeToWorldPixel(m_Time);
            MarkDirtyRepaint();
        }

        void GenerateVisualContent(MeshGenerationContext context)
        {
            Rect bounds = layout;
            var linePos = new Vector2(WorldToLocalX(m_WorldPixel), bounds.y);

            customStyle.TryGetValue(k_SegmentHeightProperty, out float segmentHeight);
            customStyle.TryGetValue(k_LineWidthProperty, out float lineWidth);
            customStyle.TryGetValue(k_LineColorProperty, out Color color);

            if (segmentHeight > 0)
                context.DrawVerticalDottedLine(linePos, bounds.height, lineWidth, segmentHeight, color);
            else
                context.DrawVerticalLine(linePos, bounds.height, lineWidth, color);
        }
    }
}
