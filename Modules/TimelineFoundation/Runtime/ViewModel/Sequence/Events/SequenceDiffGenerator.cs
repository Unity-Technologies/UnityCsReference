// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_DIFF_GENERATOR

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Model;

namespace Unity.Timeline.Foundation.ViewModel.Internals
{
    class SequenceDiffGenerator : SequenceEventVisitor
    {
        SequenceDiff.Builder m_DiffBuilder;
        SequenceSnapshot m_PreviousSnapshot;

        public SequenceDiff UpdateSnapshot(SequenceSnapshot previousSnapshot, IEnumerable<ISequenceEvent> events)
        {
            Init(previousSnapshot);
            VisitAll(events);
            return Finish();
        }

        public override void Visit(ModelEvents.SequenceMetadataChanged evt)
        {
            base.Visit(evt);
            m_PreviousSnapshot.snapshot.UpdateMetadata_Internal();
        }

        public override void Visit(ModelEvents.TrackContentsChanged evt)
        {
            DiffTrackContent(evt.track);
        }

        public override void Visit(ModelEvents.TrackMetadataChanged evt)
        {
            DiffTrackMetadata(evt.track);
        }

        public override void Visit(ModelEvents.StackHierarchyChanged evt)
        {
            DiffStackChildren(evt.stack);
        }

        public override void Visit(ModelEvents.ItemContentsChanged evt)
        {
            DiffItemContent(evt.itemID);
        }

        void Init(SequenceSnapshot sequenceSnapshot)
        {
            m_PreviousSnapshot = sequenceSnapshot;
            m_DiffBuilder = new SequenceDiff.Builder(sequenceSnapshot.snapshot);
        }

        SequenceDiff Finish()
        {
            m_PreviousSnapshot = null;
            SequenceDiff diff = m_DiffBuilder.Finish();
            m_DiffBuilder = new SequenceDiff.Builder();
            return diff;
        }

        void DiffTrackContent(ITrack trackModel)
        {
            Track trackSnapshot = m_PreviousSnapshot.GetOrCreateSnapshot_Internal(trackModel);

            CutList trackContents = trackModel.GetCutList();
            trackSnapshot.SetCutList_Internal(trackContents);
            MarkerList markerContents = trackModel.GetMarkers();
            trackSnapshot.SetMarkerList_Internal(markerContents);

            IReadOnlyList<Item> items = SequenceSnapshot.CreateTrackItems_Internal(trackSnapshot, trackContents, markerContents);
            trackSnapshot.SetItems_Internal(items);

            m_DiffBuilder.PushTrackContentChange(trackSnapshot);
            m_PreviousSnapshot.snapshot.UpdateMetadata_Internal();
        }

        void DiffItemContent(UniqueID itemID)
        {
            m_DiffBuilder.PushItemContentChanged(itemID);
        }

        void DiffTrackMetadata(ITrack trackModel)
        {
            Track trackSnapshot = m_PreviousSnapshot.GetOrCreateSnapshot_Internal(trackModel);

            trackSnapshot.SetName_Internal(trackModel.name);
            trackSnapshot.SetMetadata_Internal(trackModel.metadata);

            m_DiffBuilder.PushTrackMetadataChange(trackSnapshot);
            m_PreviousSnapshot.snapshot.UpdateMetadata_Internal();
        }

        void DiffStackChildren(IStack stackModel)
        {
            Stack stackSnapshot = m_PreviousSnapshot.GetSnapshotFor(stackModel);

            var childrenCount = stackSnapshot.children.Count;
            List<ITrack> oldChildList = new List<ITrack>(childrenCount);
            List<ITrack> newChildList = new List<ITrack>(stackModel.GetChildTracks());

            var updatedChildSnapshots = new Track[newChildList.Count];

            for (int i = 0; i < childrenCount; i++)
            {
                var track = stackSnapshot.children[i].model;
                int newIndex = newChildList.IndexOf(track);
                if (newIndex == -1) // removed
                {
                    Track snapshot = m_PreviousSnapshot.GetSnapshotFor(track);
                    Log($"Track Removed {snapshot}");
                    m_DiffBuilder.PushTrackRemoved(snapshot);
                    m_PreviousSnapshot.RemoveSnapshot_Internal(track);
                    continue;
                }

                oldChildList.Add(track);
            }

            for (var newIndex = 0; newIndex < newChildList.Count; newIndex++)
            {
                int oldIndex = oldChildList.IndexOf(newChildList[newIndex]);
                if (oldIndex == -1) //added
                {
                    Track newSnapshot = m_PreviousSnapshot.GetOrCreateSnapshot_Internal(newChildList[newIndex]);
                    Log($"Track Added {newSnapshot}");
                    m_DiffBuilder.PushTrackAdded(newSnapshot);
                    updatedChildSnapshots[newIndex] = newSnapshot;
                }
                else if (oldIndex != newIndex) //moved
                {
                    Track snapshot = m_PreviousSnapshot.GetSnapshotFor(newChildList[newIndex]);
                    Log($"Track Moved {snapshot}");
                    m_DiffBuilder.PushTrackReordered(snapshot);
                    updatedChildSnapshots[newIndex] = snapshot;
                }
                else // no change
                {
                    Track snapshot = m_PreviousSnapshot.GetSnapshotFor(newChildList[newIndex]);
                    updatedChildSnapshots[newIndex] = snapshot;
                }
            }

            ValidateChildren(updatedChildSnapshots);
            stackSnapshot.SetChildren_Internal(new List<Track>(updatedChildSnapshots));
            m_PreviousSnapshot.snapshot.UpdateMetadata_Internal();
        }

        [Conditional("DEBUG_DIFF_GENERATOR")]
        static void ValidateChildren(IReadOnlyList<Track> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                    UnityEngine.Debug.Log($"Child {i} is null.");
            }
        }

        [Conditional("DEBUG_DIFF_GENERATOR")]
        static void Log(string log)
        {
            UnityEngine.Debug.Log($"{nameof(SequenceDiffGenerator)}: {log}");
        }
    }
}
