// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View.Internals
{
    class CanvasManager : ICanvas
    {
        public CanvasTransform canvasTransform => CreateCanvasTransform();
        public TimeConverter timeConverter => new TimeConverter(m_TimeFormat, m_FrameRate, m_DisplayRangeTransform);
        public IOverlayManager overlayManager => m_ContentsOverlay;
        public Rect worldBound => m_ContentsOverlay.worldBound;
        public bool snapToFrame { get; set; }

        public Rect WorldToLocal(Rect worldRect) => m_ContentsOverlay.WorldToLocal(worldRect);
        public Rect LocalToWorld(Rect localRect) => m_ContentsOverlay.LocalToWorld(localRect);
        public Vector2 WorldToLocal(Vector2 worldPos) => m_ContentsOverlay.WorldToLocal(worldPos);
        public Vector2 LocalToWorld(Vector2 localPos) => m_ContentsOverlay.LocalToWorld(localPos);

        readonly VisualElement m_Viewport;
        readonly CanvasOverlayManager m_ContentsOverlay;

        TimeFormat m_TimeFormat;
        TimeTransform m_DisplayRangeTransform = TimeTransform.Identity;
        FrameRate m_FrameRate;

        IVisualElementScheduledItem m_RepositionScheduledItem;
        UQueryState<CanvasElement> m_AllCanvasElements;
        float m_DisplayWidth;
        TimeRange m_DisplayRange;

        public CanvasManager(VisualElement viewport, CanvasOverlayManager overlayManager)
        {
            m_ContentsOverlay = overlayManager;
            m_ContentsOverlay.canvas = this;
            m_Viewport = viewport;
            m_FrameRate = FrameRate.k_60Fps;

            m_AllCanvasElements = viewport.Query<CanvasElement>().Build();
            viewport.RegisterCallback<CanvasUpdateEvent>(OnCanvasUpdate, TrickleDown.TrickleDown);
            m_ContentsOverlay.RegisterCallback<GeometryChangedEvent>(OnContentsGeometryChanged, TrickleDown.TrickleDown);
        }

        public void SetDisplayRange(TimeRange range)
        {
            if (m_DisplayRange != range)
            {
                m_DisplayRange = range;
                RepositionAll();
                m_ContentsOverlay?.UpdateOverlays();
            }
        }

        public void SetTimeFormat(TimeFormat timeFormat)
        {
            if (m_TimeFormat != timeFormat)
            {
                m_TimeFormat = timeFormat;
                m_ContentsOverlay.UpdateOverlays();
            }
        }

        public void SetDisplayTransform(TimeTransform transform)
        {
            if (m_DisplayRangeTransform != transform)
            {
                m_DisplayRangeTransform = transform;
                m_ContentsOverlay.UpdateOverlays();
            }
        }

        public void SetFrameRate(FrameRate frameRate)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (m_FrameRate != frameRate)
            {
                m_FrameRate = frameRate;
                m_ContentsOverlay.UpdateOverlays();
            }
        }

        CanvasTransform CreateCanvasTransform()
        {
            Debug.Assert(CanUpdate(), "Canvas transform is invalid - the canvas has not been layouted.");
            return new CanvasTransform(m_DisplayRange, m_DisplayWidth);
        }

        void OnCanvasUpdate(CanvasUpdateEvent evt)
        {
            switch (evt.updateType)
            {
                case CanvasUpdateEvent.UpdateType.All:
                    RepositionAll();
                    break;
                case CanvasUpdateEvent.UpdateType.Target:
                    RepositionTarget(evt.target as CanvasElement);
                    break;
                case CanvasUpdateEvent.UpdateType.TargetAndDescendants:
                    RepositionTargetAndDescendants(evt.target as VisualElement);
                    break;
            }
        }

        void OnContentsGeometryChanged(GeometryChangedEvent evt)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (m_DisplayWidth != evt.newRect.width)
            {
                m_DisplayWidth = evt.newRect.width;

                //don't schedule RepositionAll if it is already scheduled
                if (m_RepositionScheduledItem is not { isActive: true })
                    m_RepositionScheduledItem = m_Viewport.schedule.Execute(RepositionAll);
            }
        }

        void RepositionAll()
        {
            if (!CanUpdate()) return;

            CanvasTransform tr = CreateCanvasTransform();
            m_AllCanvasElements.ForEach(i => i.PositionInCanvas(tr));
        }

        public void RepositionTargetAndDescendants(VisualElement target)
        {
            if (target == null || !CanUpdate()) return;

            CanvasTransform tr = CreateCanvasTransform();
            UQueryState<CanvasElement> query = m_AllCanvasElements.RebuildOn(target);

            foreach (CanvasElement canvasElement in query)
                canvasElement.PositionInCanvas(tr);
        }

        void RepositionTarget(CanvasElement target)
        {
            if (target == null || !CanUpdate()) return;

            CanvasTransform tr = CreateCanvasTransform();
            target.PositionInCanvas(tr);
        }

        bool CanUpdate()
        {
            //this check works correctly with NaN: comparison with NaN always returns false
            return m_DisplayWidth > 0f;
        }
    }
}
