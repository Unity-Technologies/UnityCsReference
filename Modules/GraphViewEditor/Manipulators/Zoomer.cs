// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    class ContentZoomer : Manipulator
    {
        public static readonly Vector3 DefaultMinScale = new Vector3(0.1f, 0.1f, 1.0f);
        public static readonly Vector3 DefaultMaxScale = new Vector3(3.0f, 3.0f, 1.0f);

        public float zoomStep { get; set; }

        public Vector3 minScale { get; set; }
        public Vector3 maxScale { get; set; }

        public bool keepPixelCacheOnZoom { get; set; }

        private bool delayRepaintScheduled { get; set; }

        public ContentZoomer()
        {
            zoomStep = 0.01f;
            minScale = DefaultMinScale;
            maxScale = DefaultMaxScale;
            keepPixelCacheOnZoom = false;
        }

        public ContentZoomer(Vector3 minScale, Vector3 maxScale)
        {
            zoomStep = 0.01f;
            this.minScale = minScale;
            this.maxScale = maxScale;
            keepPixelCacheOnZoom = false;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            var graphView = target as GraphView;
            if (graphView == null)
            {
                throw new InvalidOperationException("Manipulator can only be added to a GraphView");
            }

            target.RegisterCallback<WheelEvent>(OnWheel);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<WheelEvent>(OnWheel);
        }

        private IVisualElementScheduledItem m_OnTimerTicker;
        private void OnTimer(TimerState timerState)
        {
            var graphView = target as GraphView;
            if (graphView == null)
                return;

            if (graphView.elementPanel != null)
                graphView.elementPanel.keepPixelCacheOnWorldBoundChange = false;

            delayRepaintScheduled = false;
        }

        void OnWheel(WheelEvent evt)
        {
            var graphView = target as GraphView;
            if (graphView == null)
                return;

            Vector3 position = graphView.viewTransform.position;
            Vector3 scale = graphView.viewTransform.scale;

            // TODO: augment the data to have the position as well, so we don't have to read in data from the target.
            // 0-1 ranged center relative to size
            Vector2 zoomCenter = target.ChangeCoordinatesTo(graphView.contentViewContainer, evt.localMousePosition);
            float x = zoomCenter.x + graphView.contentViewContainer.layout.x;
            float y = zoomCenter.y + graphView.contentViewContainer.layout.y;

            position += Vector3.Scale(new Vector3(x, y, 0), scale);
            Vector3 s = Vector3.one - Vector3.one * evt.delta.y * zoomStep;
            s.z = 1;
            scale = Vector3.Scale(scale, s);

            // Limit scale
            scale.x = Mathf.Max(Mathf.Min(maxScale.x, scale.x), minScale.x);
            scale.y = Mathf.Max(Mathf.Min(maxScale.y, scale.y), minScale.y);
            scale.z = Mathf.Max(Mathf.Min(maxScale.z, scale.z), minScale.z);

            position -= Vector3.Scale(new Vector3(x, y, 0), scale);

            // Delay updating of the pixel cache so the scrolling appears smooth.
            if (graphView.elementPanel != null && keepPixelCacheOnZoom)
            {
                graphView.elementPanel.keepPixelCacheOnWorldBoundChange = true;

                if (m_OnTimerTicker == null)
                {
                    m_OnTimerTicker = graphView.schedule.Execute(OnTimer);
                }

                m_OnTimerTicker.ExecuteLater(500);

                delayRepaintScheduled = true;
            }

            graphView.UpdateViewTransform(position, scale);

            evt.StopPropagation();
        }
    }
}
