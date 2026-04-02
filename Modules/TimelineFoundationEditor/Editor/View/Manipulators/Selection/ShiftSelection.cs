// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View.Internals
{
    static class ShiftSelection
    {
        public static IEnumerable<UniqueID> GetSelectableElements(ISelectableElement pickedElement, SelectionData selectionData,
            SequenceData sequenceData)
        {
            if (pickedElement is ItemElement itemElement)
            {
                return GetSelectableItems(itemElement, selectionData, sequenceData);
            }

            if (pickedElement is ITrackElement trackElement)
            {
                IEnumerable<UniqueID> tracks = GetSelectableTracks(sequenceData.GetTrackFromId(trackElement.ID),
                    selectionData, sequenceData);
                if (tracks != null)
                    return tracks;
            }

            if (pickedElement is VisualElement element)
            {
                ItemElement item = element.GetFirstAncestorOfType<ItemElement>();
                if (item == null)
                    return Array.Empty<UniqueID>();
                return GetSelectableItems(item, selectionData, sequenceData);
            }

            return Array.Empty<UniqueID>();
        }

        static IEnumerable<UniqueID> GetSelectableItems(ItemElement itemElement, SelectionData selectionData, SequenceData sequenceData)
        {
            IEnumerable<UniqueID> items = GetSelectableItemsSameTrack(itemElement.item,
                selectionData, sequenceData);
            if (items != null)
                return items;
            items = GetSelectableItemsMultipleTracks(itemElement.item, selectionData, sequenceData);
            if (items != null)
                return items;
            return Array.Empty<UniqueID>();
        }

        static IEnumerable<UniqueID> GetSelectableItemsMultipleTracks(Item toSelect, SelectionData selectionData,
            SequenceData sequenceData)
        {
            IEnumerable<Item> selectedItems = SequenceQuery.GetAllSelectedItems(selectionData, sequenceData);
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
#pragma warning disable UA2012 // Pre-existing usage of LastOrDefault.
            Track lastTrackSelected = selectedItems.Select(i => i.parent).OrderBy(t => t.index).LastOrDefault();
#pragma warning restore UA2012
#pragma warning restore UA2001
            if (lastTrackSelected == null)
                return null;

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            IEnumerable<Track> tracks = SequenceQuery.TracksBetween(sequenceData.sequence, lastTrackSelected,
                toSelect.parent, exclude: SequenceQuery.TrackInCollapsedGroup).Concat(new[] { lastTrackSelected, toSelect.parent });
#pragma warning disable UA2011 // Pre-existing usage of LastOrDefault.
            Item lastItemSelected = selectedItems.Intersect(tracks.SelectMany(t => t.Items))
                .OrderBy(i => i.start)
                .FirstOrDefault();
#pragma warning restore UA2011
#pragma warning restore UA2001
            if (lastItemSelected == Item.Invalid)
                return null;

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return SequenceQuery.ItemsBetween(tracks, toSelect, lastItemSelected)
                .Select(i => i.ID);
#pragma warning restore UA2001
        }

        static IEnumerable<UniqueID> GetSelectableTracks(Track toSelect, SelectionData selectionData, SequenceData sequenceData)
        {
#pragma warning disable UA2012 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            Track lastTrackSelected = SequenceQuery.GetSelectedTracks(selectionData, sequenceData).LastOrDefault();
#pragma warning restore UA2012
            if (lastTrackSelected == null)
                return null;
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return SequenceQuery.TracksBetween(sequenceData.sequence, lastTrackSelected, toSelect,
                exclude: SequenceQuery.TrackInCollapsedGroup).Select(t => t.ID);
#pragma warning restore UA2001
        }

        static IEnumerable<UniqueID> GetSelectableItemsSameTrack(Item toSelect, SelectionData selectionData,
            SequenceData sequenceData)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
#pragma warning disable UA2012 // Pre-existing usage of LastOrDefault.
            Item lastItemSelected = SequenceQuery.GetItemSelectionOnTrack(selectionData, sequenceData, toSelect.parent)
                .OrderBy(item => item.index).LastOrDefault();
#pragma warning restore UA2012
#pragma warning restore UA2001
            if (lastItemSelected == Item.Invalid)
                return null;
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return SequenceQuery.ItemsInRange(toSelect.parent, GetRangeBetweenItems(lastItemSelected, toSelect))
                .Select(i => i.ID);
#pragma warning restore UA2001
        }

        static TimeRange GetRangeBetweenItems(Item item1, Item item2) => new TimeRange(DiscreteTimeTimeExtensions.Min(item1.start, item2.start),
            DiscreteTimeTimeExtensions.Max(item1.start, item2.start));
    }
}
