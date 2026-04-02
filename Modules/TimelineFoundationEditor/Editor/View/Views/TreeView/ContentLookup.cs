// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.View
{
    readonly struct ContentLookup
    {
        readonly IReadOnlyDictionary<UniqueID, ITrackHeaderElement> m_TrackHeaderLookup;
        readonly IReadOnlyDictionary<UniqueID, ITrackContentElement> m_TrackContentLookup;
        readonly IReadOnlyDictionary<UniqueID, ItemElement> m_ItemElementLookup;

        internal ContentLookup(IReadOnlyDictionary<UniqueID, ITrackHeaderElement> trackHeaderLookup,
            IReadOnlyDictionary<UniqueID, ITrackContentElement> trackContentLookup,
            IReadOnlyDictionary<UniqueID, ItemElement> itemLookup)
        {
            m_TrackHeaderLookup = trackHeaderLookup;
            m_TrackContentLookup = trackContentLookup;
            m_ItemElementLookup = itemLookup;
        }

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public IEnumerable<TrackHeaderElement> GetTrackHeaderElements() => m_TrackHeaderLookup.Values.Cast<TrackHeaderElement>();
        public IEnumerable<TrackElement> GetTrackElements() => m_TrackContentLookup.Values.Cast<TrackElement>();
#pragma warning restore UA2001
        public IEnumerable<ItemElement> GetItemElements() => m_ItemElementLookup.Values;

        public TrackHeaderElement GetTrackHeaderElement(UniqueID trackId) => m_TrackHeaderLookup.GetValue(trackId) as TrackHeaderElement;
        public TrackElement GetTrackElement(UniqueID trackId) => m_TrackContentLookup.GetValue(trackId) as TrackElement;
        public ItemElement GetItemElement(UniqueID itemId) => m_ItemElementLookup.GetValue(itemId);

        public TrackHeaderElement GetTrackHeaderElement(Track track) => GetTrackHeaderElement(track.ID);
        public TrackElement GetTrackElement(Track track) => GetTrackElement(track.ID);
        public ItemElement GetItemElement(Item item) => GetItemElement(item.ID);
    }
}
