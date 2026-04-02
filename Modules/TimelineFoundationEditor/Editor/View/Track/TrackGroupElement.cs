// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Widgets;

namespace Unity.Timeline.Foundation.View
{
    class TrackGroupElement : TrackElement
    {
        public TrackGroupElement(TrackElementContext context) : base(context)
        {
            this.AddToTimelineClassList("groupTrack");
        }

        public override void Select()
        {
            this.AddToTimelineClassList("groupTrack--selected");
        }

        public override void Unselect()
        {
            this.RemoveFromTimelineClassList("groupTrack--selected");
        }
    }
}
