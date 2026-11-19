// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View.Internals
{
    class SnapEngineDebugOverlay : CanvasOverlay
    {
        public SnapEngine snapEngine { get; set; }
        public float attractionWidth { get; set; }

        CanvasTransform m_CanvasTransform;

        public SnapEngineDebugOverlay()
        {
            generateVisualContent += GenerateVisualContent;
            this.StretchToParentSize();

            //auto-update in case the snap engine has changed
            schedule.Execute(TimerUpdateEvent).Every(250);
        }

        void TimerUpdateEvent(TimerState timerState)
        {
            MarkDirtyRepaint();
        }

        void GenerateVisualContent(MeshGenerationContext obj)
        {
            if (snapEngine == null)
                return;

            Rect rect = layout;
            var color = new Color(1.0f, 0.0f, 0.0f, 0.5f);

            foreach (DiscreteTime edge in snapEngine.edges)
            {
                float xPos = m_CanvasTransform.TimeToPixel(edge);
                obj.DrawRect(new Rect(xPos - attractionWidth, rect.y, 2.0f * attractionWidth, rect.height), color);
            }
        }

        protected override void Update(ICanvas canvas)
        {
            m_CanvasTransform = canvas.canvasTransform;
            MarkDirtyRepaint();
        }
    }
}
