// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Timeline.Foundation.Model;

namespace Unity.Timeline.Foundation.ViewModel
{
    abstract class SequenceEventStream : IDisposable
    {
        List<ISequenceEvent> m_Events = new List<ISequenceEvent>();

        public IEnumerable<ISequenceEvent> sequenceEvents => m_Events;

        public virtual void Add<T>(T evt) where T : ISequenceEvent
        {
            m_Events.Add(evt);
        }

        public virtual bool HasChanges() => m_Events.Count > 0;

        public virtual void VisitAll(SequenceEventVisitor visitor)
        {
            visitor.VisitAll(m_Events);
        }

        public virtual void Clear()
        {
            m_Events.Clear();
        }

        public virtual void Dispose() { }
    }
}
