// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    class PlayRangeMarker : DraggablePlayHeadOverlay
    {
        const string k_Name = "playRangeMarker";
        static readonly StylesheetResource k_Stylesheet = UIResources.StylesheetFactory.Get<PlayRangeOverlay>();

        public Action<DiscreteTime> timeDragged;

        public PlayRangeMarker(ICanvas canvas)
            : base(canvas)
        {
            k_Stylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_Name);
        }

        protected override void SetTime(DiscreteTime time)
        {
            timeDragged?.Invoke(time);
        }
    }

    //TODO - ATL-1543 - needs to Implement SequenceElement
    class PlayRangeOverlay : CanvasOverlay
    {
        const string k_Name = "playRangeOverlay";
        const string k_NameRectangle = "playRangeRectangle";
        const string k_NameLine = "playRangeLine";
        const string k_NameMarkerStart = "markerStart";
        const string k_NameMarkerEnd = "markerEnd";

        static readonly StylesheetResource k_Stylesheet = UIResources.StylesheetFactory.Get<PlayRangeOverlay>();

        PlayRangeMarker m_PlayRangeMarkerStart;
        PlayRangeMarker m_PlayRangeMarkerEnd;
        RangeOverlay m_Rectangle;
        RangeOverlay m_Line;

        TimeRange m_PlayRange = TimeRange.Empty;

        public TimeRange playRange
        {
            get => m_PlayRange;
            set
            {
                m_PlayRange = value;
                ForceUpdate();
            }
        }

        public PlayRangeOverlay(ICanvas canvas)
        {
            this.StretchToParentSize();
            k_Stylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_Name);
            name = k_Name;

            m_PlayRangeMarkerStart = new PlayRangeMarker(canvas) { name = k_NameMarkerStart };
            Add(m_PlayRangeMarkerStart);

            m_PlayRangeMarkerEnd = new PlayRangeMarker(canvas) { name = k_NameMarkerEnd };
            Add(m_PlayRangeMarkerEnd);

            m_Rectangle = new RangeOverlay() { name = k_NameRectangle };
            Add(m_Rectangle);

            m_Line = new RangeOverlay() { name = k_NameLine };
            Add(m_Line);

            m_PlayRangeMarkerStart.timeDragged += time => SetPlayRange(time, playRange.end);
            m_PlayRangeMarkerEnd.timeDragged += time => SetPlayRange(playRange.start, time);

            Hide();
        }

        protected override void Update(ICanvas canvas)
        {
            m_PlayRangeMarkerStart.time = m_PlayRange.start;
            m_PlayRangeMarkerEnd.time = m_PlayRange.end;
            m_Line.range = m_PlayRange;
            m_Rectangle.range = m_PlayRange;
            MarkDirtyRepaint();
        }

        protected virtual void SetPlayRange(DiscreteTime start, DiscreteTime end)
        {
            if (start > end)
                start = end;
            playRange = new TimeRange(start, end);
        }
    }
}
