// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    class MoveOverlay : CanvasOverlay
    {
        const string k_LeftLine = "leftLine";
        const string k_RightLine = "rightLine";
        const string k_Style = "moveOverlay";
        const string k_LeftSnapStyle = k_Style + "--leftSnap";
        const string k_RightSnapStyle = k_Style + "--rightSnap";

        TimeTooltipOverlay m_LeftTooltip;
        TimeTooltipOverlay m_RightTooltip;
        VerticalLineOverlay m_LeftLine;
        VerticalLineOverlay m_RightLine;
        RangeOverlay m_RangeOverlay;
        UQueryState<ItemOverlay> m_ItemOverlays;
        TimeRange range => m_RangeOverlay.range;

        public MoveOverlay()
        {
            this.StretchToParentSize();
            Internals.UIResources.OverlayStylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_Style);

            m_ItemOverlays = this.Query<ItemOverlay>().Build();

            m_RangeOverlay = new RangeOverlay();
            Add(m_RangeOverlay);

            m_LeftLine = new VerticalLineOverlay() { name = k_LeftLine };
            Add(m_LeftLine);

            m_RightLine = new VerticalLineOverlay() { name = k_RightLine };
            Add(m_RightLine);

            m_LeftTooltip = new TimeTooltipOverlay();
            Add(m_LeftTooltip);

            m_RightTooltip = new TimeTooltipOverlay();
            Add(m_RightTooltip);

            Hide();
        }

        public void SetInitialRange(TimeRange initialRange)
        {
            SetRange(initialRange);
        }

        public void UpdateRange(TimeRange newRange)
        {
            DiscreteTime delta = newRange.start - range.start;
            SetRange(newRange);

            foreach (ItemOverlay clip in m_ItemOverlays)
                clip.range += delta;
        }

        public void RemoveClipOverlays()
        {
            m_ItemOverlays.ForEach(overlay => overlay.RemoveFromHierarchy());
        }

        public void SetWorldY(float worldY)
        {
            m_ItemOverlays.ForEach(overlay => overlay.SetWorldY(worldY));
        }

        public void AttachTo(Rect worldRect)
        {
            m_ItemOverlays.ForEach(overlay => overlay.SnapTo(worldRect));
        }

        public void AddItemOverlay(Item item, Rect worldRect)
        {
            var itemOverlay = new ItemOverlay
            {
                label = item.name,
                range = GetItemRange(item, worldRect)
            };

            Add(itemOverlay);
            itemOverlay.SnapTo(worldRect);
            itemOverlay.Hide();
        }

        public void SetItemOverlayState(bool show, bool isValid)
        {
            m_ItemOverlays.ForEach(overlay => SetItemOverlayState(overlay, show, isValid));
        }

        void SetRange(TimeRange newRange)
        {
            m_RangeOverlay.range = newRange;
            m_LeftLine.time = newRange.start;
            m_RightLine.time = newRange.end;
            m_LeftTooltip.time = newRange.start;
            m_RightTooltip.time = newRange.end;
        }

        public IEnumerable<TimeRange> GetShownItemOverlaysRanges()
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return m_ItemOverlays.Where(overlay => overlay.isShown && overlay.state == ItemOverlay.State.Valid).Select(overlay => overlay.range);
#pragma warning restore UA2001
        }

        static void SetItemOverlayState(ItemOverlay item, bool show, bool isValid)
        {
            if (show)
                item.Show();
            else
                item.Hide();

            item.state = isValid ? ItemOverlay.State.Valid : ItemOverlay.State.Invalid;
        }

        public void SetSnapState(bool leftSnap, bool rightSnap)
        {
            if (leftSnap)
                this.AddToTimelineClassList(k_LeftSnapStyle);
            else
                this.RemoveFromTimelineClassList(k_LeftSnapStyle);

            if (rightSnap)
                this.AddToTimelineClassList(k_RightSnapStyle);
            else
                this.RemoveFromTimelineClassList(k_RightSnapStyle);
        }

        TimeRange GetItemRange(Item item, Rect worldRect)
        {
            if (item.isClip)
                return item.GetVisibleRange();
            if (item.isMarker)
            {
                DiscreteTime visibleDuration = Canvas.canvasTransform.PixelWidthToDuration(worldRect.width);
                return new TimeRange(item.start, item.start + visibleDuration) - visibleDuration.Half();
            }
            return TimeRange.Empty;
        }
    }
}
