// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Timeline.Foundation.Common;

namespace Unity.Timeline.Foundation.Model
{
    interface ISequenceEvent
    {
        void Accept(SequenceEventVisitor visitor);
    }

    abstract class SequenceEventVisitor
    {
        public virtual void VisitAll(IEnumerable<ISequenceEvent> events)
        {
            foreach (ISequenceEvent evt in events)
            {
                evt.Accept(this);
            }
        }

        public virtual void Visit(ModelEvents.SequenceMetadataChanged evt) { }
        public virtual void Visit(ModelEvents.StackHierarchyChanged evt) { }
        public virtual void Visit(ModelEvents.TrackMetadataChanged evt) { }
        public virtual void Visit(ModelEvents.TrackContentsChanged evt) { }
        public virtual void Visit(ModelEvents.ItemContentsChanged evt) { }
    }

    static class ModelEvents
    {
        public readonly struct SequenceMetadataChanged : ISequenceEvent
        {
            public void Accept(SequenceEventVisitor visitor)
            {
                visitor.Visit(this);
            }
        }

        public readonly struct TrackContentsChanged : ISequenceEvent
        {
            public readonly ITrack track;

            public TrackContentsChanged(ITrack track)
            {
                this.track = track;
            }

            public void Accept(SequenceEventVisitor visitor)
            {
                visitor.Visit(this);
            }
        }

        public readonly struct TrackMetadataChanged : ISequenceEvent
        {
            public readonly ITrack track;

            public TrackMetadataChanged(ITrack track)
            {
                this.track = track;
            }

            public void Accept(SequenceEventVisitor visitor)
            {
                visitor.Visit(this);
            }
        }

        public readonly struct StackHierarchyChanged : ISequenceEvent
        {
            public readonly IStack stack;

            public StackHierarchyChanged(IStack stack)
            {
                this.stack = stack;
            }

            public void Accept(SequenceEventVisitor visitor)
            {
                visitor.Visit(this);
            }
        }

        public readonly struct ItemContentsChanged : ISequenceEvent
        {
            public readonly UniqueID itemID;
            public readonly IItemContent itemContent;

            public ItemContentsChanged(IItemContent itemContent, UniqueID id)
            {
                this.itemID = id;
                this.itemContent = itemContent;
            }

            public void Accept(SequenceEventVisitor visitor)
            {
                visitor.Visit(this);
            }
        }
    }
}
