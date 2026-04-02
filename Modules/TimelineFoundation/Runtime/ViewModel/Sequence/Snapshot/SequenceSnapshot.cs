// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Model;
using UnityEngine;

namespace Unity.Timeline.Foundation.ViewModel.Internals
{
    class SequenceSnapshot
    {
        struct ItemComparer : IComparer<Item>
        {
            public static IComparer<Item> Instance = new ItemComparer();

            public int Compare(Item x, Item y)
            {
                int startComparison = x.range.start.CompareTo(y.range.start);
                return startComparison == 0 ? x.duration.CompareTo(y.duration) : startComparison;
            }
        }

        Dictionary<UniqueID, Stack> m_StackInfos = new Dictionary<UniqueID, Stack>();
        SequenceDiffGenerator m_DiffGenerator = new SequenceDiffGenerator();

        public Sequence snapshot { get; private set; }

        public SequenceSnapshot(ISequence sequence)
        {
            Init(sequence);
        }

        public SequenceDiff IncrementalUpdate(IEnumerable<ISequenceEvent> events)
        {
            SequenceDiff diff = m_DiffGenerator.UpdateSnapshot(this, events);
            return diff;
        }

        public Track GetSnapshotFor(ITrack model)
        {
            return GetSnapshotFor((IStack)model) as Track;
        }

        public Stack GetSnapshotFor(IStack model)
        {
            return m_StackInfos.TryGetValue(model.ID, out Stack stack) ? stack : null;
        }

        internal Track GetOrCreateSnapshot_Internal(ITrack track)
        {
            Track trackSnapshot = GetSnapshotFor(track);
            return trackSnapshot ?? CreateSnapshot_Internal(track);
        }

        void Init(ISequence model)
        {
            if (model == null)
            {
                snapshot = Sequence.Invalid;
                return;
            }

            snapshot = new Sequence(model);
            m_StackInfos.Add(model.ID, snapshot);

            var tracks = new List<Track>();
            foreach (ITrack track in model.GetChildTracks())
            {
                Track newTrack = CreateSnapshot_Internal(track);
                tracks.Add(newTrack);
            }

            snapshot.SetChildren_Internal(tracks);
            snapshot.UpdateMetadata_Internal();
        }

        internal Track CreateSnapshot_Internal(ITrack model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            Stack parentSnapshot = GetSnapshotFor(model.parent);
            var trackSnapshot = new Track(model, parentSnapshot);
            m_StackInfos.Add(model.ID, trackSnapshot);
            trackSnapshot.SetChildren_Internal(CreateTrackChildren(model));

            CutList trackCutList = model.GetCutList();
            trackSnapshot.SetCutList_Internal(trackCutList);
            MarkerList trackMarkerList = model.GetMarkers();
            trackSnapshot.SetMarkerList_Internal(trackMarkerList);

            IReadOnlyList<Item> items = CreateTrackItems_Internal(trackSnapshot, trackCutList, trackMarkerList);
            trackSnapshot.SetItems_Internal(items);

            return trackSnapshot;
        }

        List<Track> CreateTrackChildren(IStack model)
        {
            var children = new List<Track>();
            foreach (ITrack child in model.GetChildTracks())
            {
                Track childSnapshot = GetOrCreateSnapshot_Internal(child);
                children.Add(childSnapshot);
            }

            return children;
        }

        internal static IReadOnlyList<Item> CreateTrackItems_Internal(Track track, CutList cutList, MarkerList markers)
        {
            int itemsCount = (cutList?.Count ?? 0) + (markers?.Count ?? 0);
            var items = new List<Item>(itemsCount);

            if (cutList != null)
            {
                foreach (CutList.Item clItem in cutList)
                    BuildItem(clItem, items, track);
            }

            if (markers != null)
            {
                foreach (Marker marker in markers)
                    BuildItem(marker, items, track);
            }

            SortAndWriteItemIndexes(items);
            return items;
        }

        static void BuildItem(CutList.Item clItem, List<Item> items, Track track)
        {
            switch (clItem.type)
            {
                case CutList.ItemType.Clip:
                    Item clip = ItemFactory.CreateClip(clItem.handle, track, -1, clItem.noOverlapRange, clItem.contentRange, clItem.content);
                    items.Add(clip);
                    break;
                case CutList.ItemType.Gap:
                    Item gap = ItemFactory.CreateGap(clItem.handle, track, -1, clItem.noOverlapRange, clItem.content);
                    items.Add(gap);
                    break;
            }

            CutList.Transition transition = clItem.GetRightTransition();
            if (transition.isValid)
            {
                Item tr = ItemFactory.CreateTransition(transition.id, track, -1, transition.range, transition.content);
                items.Add(tr);
            }
        }

        static void BuildItem(Marker marker, List<Item> items, Track track)
        {
            Item item = ItemFactory.CreateMarker(marker.id, track, -1, marker.time, marker.content);
            items.Add(item);
        }

        static void SortAndWriteItemIndexes(List<Item> items)
        {
            items.StableSort(ItemComparer.Instance);
            for (var i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                items[i] = item.ChangeIndex_Internal(i);
            }
        }

        internal void RemoveSnapshot_Internal(IStack stack)
        {
            UniqueID stackID = stack.ID;
            if (m_StackInfos.TryGetValue(stackID, out Stack stackSnapshot))
            {
                foreach (Track child in stackSnapshot.children)
                    RemoveSnapshot_Internal(child.model);
                m_StackInfos.Remove(stackID);
            }
        }
    }
}
