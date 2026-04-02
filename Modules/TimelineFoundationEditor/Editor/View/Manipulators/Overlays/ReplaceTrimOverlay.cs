// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;

namespace Unity.Timeline.Foundation.View
{
    class ReplaceTrimOverlay : TrimBehaviourOverlay
    {
        const string k_Style = "trimReplaceOverlay";
        IEnumerable<TimeRange> m_FollowingItems;

        public ReplaceTrimOverlay()
        {
            this.AddToTimelineClassList(k_Style);
        }

        protected override DiscreteTime? GetStartIndicatorTime(Item item)
        {
            DiscreteTime? startIndicator = ManipulatorUtils.GetReplaceStartTrimIndicator(item, m_FollowingItems);
            if (startIndicator.HasValue)
                cursor = EditModeCursorUtils.GetCursor(EditModeCursorUtils.CursorType.Replace);
            return startIndicator;
        }

        protected override DiscreteTime? GetEndIndicatorTime(Item item)
        {
            DiscreteTime? endIndicator = ManipulatorUtils.GetReplaceEndTrimIndicator(item, m_FollowingItems);
            if (endIndicator.HasValue)
                cursor = EditModeCursorUtils.GetCursor(EditModeCursorUtils.CursorType.Replace);
            return endIndicator;
        }

        public override void ResetIndicators(TrimBehaviour behaviour, SequenceLookup lookup)
        {
            cursor = EditModeCursorUtils.GetCursor(EditModeCursorUtils.CursorType.None);
            Item item = lookup.GetItemFromId(behaviour.GetManipulatedItems()[0].ID);
            IEnumerable<Item> clips = lookup.GetTrackFromId(item.parent.ID).Items.OnlyClips();

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_FollowingItems = behaviour.location switch
            {
                TrimBehaviour.Location.Start => clips.TakeWhile(i => i.ID != item.ID).Select(i => i.GetVisibleRange()),
                TrimBehaviour.Location.End => clips.SkipWhile(i => i.ID != item.ID).Skip(1).Select(i => i.GetVisibleRange()),
                _ => m_FollowingItems
            };
#pragma warning restore UA2001
        }
    }
}
