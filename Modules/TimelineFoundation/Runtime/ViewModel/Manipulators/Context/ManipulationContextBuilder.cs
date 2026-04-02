// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Timeline.Foundation.Time;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal class ManipulationContextBuilder
    {
        TimeRange m_TotalRange;
        Dictionary<Track, List<Item>> m_ManipulatedItems = new Dictionary<Track, List<Item>>();

        public ManipulationContextBuilder AddItem(Item item)
        {
            if (item.parent == null)
                throw new ArgumentException("parent track is null", nameof(item));

            if (m_ManipulatedItems.Count == 0)
                m_TotalRange = item.GetVisibleRange();
            else
                m_TotalRange = m_TotalRange.Union(item.GetVisibleRange());

            if (m_ManipulatedItems.TryGetValue(item.parent, out List<Item> items))
                items.Add(item);
            else
                m_ManipulatedItems.Add(item.parent, new List<Item> { item });

            return this;
        }

        public ManipulationContext CreateContext()
        {
            var manipulatedTracks = new List<ManipulatedTrack>();
            var allItems = new List<Item>();
            foreach(var manipulatedTrack in m_ManipulatedItems)
            {
                var items = new List<Item>(manipulatedTrack.Value);
                allItems.AddRange(items);

                items.Sort((i1, i2) => i1.start.CompareTo(i2.start));
                manipulatedTracks.Add(new ManipulatedTrack(manipulatedTrack.Key, items));
            }

            allItems.Sort((i1, i2) => i1.start.CompareTo(i2.start));

            return new ManipulationContext(
                m_TotalRange,
                new List<Track>(m_ManipulatedItems.Keys),
                allItems,
                manipulatedTracks);
        }
    }
}
