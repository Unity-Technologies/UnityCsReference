// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Model;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel.Internals;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal class SequenceSourceComponent : Component<SequenceData>
    {
        public ISequence sequence { get; }

        readonly SequenceSnapshot m_Snapshot;
        readonly SequenceEventStream m_EventStream;
        SequenceLookup m_SequenceLookup;

        public SequenceSourceComponent(ISequence sequence, SequenceEventStream eventStream = null)
        {
            this.sequence = sequence;
            m_EventStream = eventStream;

            if (sequence != null)
            {
                m_Snapshot = new SequenceSnapshot(sequence);
                m_SequenceLookup = SequenceLookup.CreateFrom(m_Snapshot.snapshot);
            }
        }

        protected override SequenceData GenerateReadOnlyData()
        {
            if (sequence == null)
                return new SequenceData(null, m_SequenceLookup);

            if (m_EventStream == null)
                return new SequenceData(m_Snapshot.snapshot, m_SequenceLookup);

            IEnumerable<ISequenceEvent> sequenceEvents = m_EventStream.sequenceEvents;
            SequenceDiff diff = m_Snapshot.IncrementalUpdate(sequenceEvents);
            m_SequenceLookup = SequenceLookup.CreateFrom(m_SequenceLookup, diff);

            return new SequenceData(m_Snapshot.snapshot, diff, m_SequenceLookup);
        }

        protected override void CheckForExternalChanges()
        {
            if (m_EventStream != null && m_EventStream.HasChanges())
                MarkAsDirty();
        }

        public void SetFrameRate(FrameRate frameRate)
        {
            sequence.SetFrameRate(frameRate);
        }

        public Item GetItemFromId(UniqueID id) => sequence == null ? Item.Invalid : m_SequenceLookup.GetItemFromId(id);
        public Track GetTrackFromId(UniqueID id) => sequence == null ? null : m_SequenceLookup.GetTrackFromId(id);
    }
}
