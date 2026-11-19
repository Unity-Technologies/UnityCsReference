// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    class HandleOverlay : CanvasOverlay
    {
        const string k_Name = "handleOverlay";
        const string k_FullHandleStyle = "full";
        const string k_LeftHandleStyle = "left";
        const string k_RightHandleStyle = "right";

        enum HandleStyle
        {
            None = 0,
            Full = 1,
            HalfLeft = 2,
            HalfRight = 3
        }

        static HandleStyle ToHandleStyle(string handleStyle)
        {
            return handleStyle switch
            {
                k_FullHandleStyle => HandleStyle.Full,
                k_LeftHandleStyle => HandleStyle.HalfLeft,
                k_RightHandleStyle => HandleStyle.HalfRight,
                _ => HandleStyle.None
            };
        }

        static readonly CustomStyleProperty<string> k_HandleStyleProperty = new("--handle-style");
        static readonly CustomStyleProperty<float> k_ArrowHeightProperty = new("--arrow-height");
        static readonly CustomStyleProperty<Color> k_HandleColorProperty = new("--handle-color");

        float m_ArrowHeight;
        HandleStyle m_HandleStyle;
        Color m_HandleColor;

        public HandleOverlay(PickingMode pickingMode = PickingMode.Ignore) : base(pickingMode)
        {
            UIResources.OverlayStylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_Name);
            name = k_Name;

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            generateVisualContent += GenerateVisualContent;
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            customStyle.TryGetValue(k_HandleColorProperty, out m_HandleColor);
            customStyle.TryGetValue(k_ArrowHeightProperty, out m_ArrowHeight);
            customStyle.TryGetValue(k_HandleStyleProperty, out string handleStyleName);

            m_HandleStyle = ToHandleStyle(handleStyleName);
            MarkDirtyRepaint();
        }

        void GenerateVisualContent(MeshGenerationContext context)
        {
            var linePos = new Vector2(layout.center.x, layout.y);
            switch (m_HandleStyle)
            {
                case HandleStyle.Full:
                    context.DrawRectangularArrow(linePos, Vector2.down, layout.width, layout.height, m_ArrowHeight, m_HandleColor);
                    break;
                case HandleStyle.HalfLeft:
                    context.DrawHalfRectangularArrow(linePos, Vector2.down, layout.width * 0.5f, layout.height, m_ArrowHeight, false, m_HandleColor);
                    break;
                case HandleStyle.HalfRight:
                    context.DrawHalfRectangularArrow(linePos, Vector2.down, layout.width * 0.5f, layout.height, m_ArrowHeight, true, m_HandleColor);
                    break;
            }
        }
    }
}
