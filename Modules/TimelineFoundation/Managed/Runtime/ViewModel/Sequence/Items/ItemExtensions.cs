// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal static class ItemExtensions
    {
        public static Item Previous(this Item item, ItemTypeFlags flag = ItemTypeFlags.All)
        {
            using ItemView.Enumerator enumerator = ItemView.CreateBackwardEnumerator(item.parent.Items, item.index, flag);
            enumerator.MoveNext();
            return enumerator.Current;
        }

        public static Item Next(this Item item, ItemTypeFlags flag = ItemTypeFlags.All)
        {
            using ItemView.Enumerator enumerator = ItemView.CreateForwardEnumerator(item.parent.Items, item.index, flag);
            enumerator.MoveNext();
            return enumerator.Current;
        }

        /// <summary>
        /// Get the range of this item including left and right transitions, if any.
        /// Range is absolute.
        /// </summary>
        /// <returns>The visible range of this item.</returns>
        public static TimeRange GetVisibleRange(this Item item)
        {
            switch (item.type)
            {
                case Item.Type.Marker or Item.Type.Transition:
                    return item.range;
                default:
                    Item previousItem = item.Previous();
                    Item nextItem = item.Next();
                    DiscreteTime visibleStart = item.start;
                    DiscreteTime visibleEnd = item.end;

                    if (previousItem.isTransition)
                        visibleStart -= previousItem.duration;

                    if (nextItem.isTransition)
                        visibleEnd += nextItem.duration;

                    return new TimeRange(visibleStart, visibleEnd);
            }
        }

        public static bool IsValid(this Item item)
        {
            return item != Item.Invalid;
        }

        public static bool MatchesFlag(this ItemTypeFlags input, ItemTypeFlags flags)
        {
            return (input & flags) != 0;
        }

        public static bool MatchesFlag(this Item.Type itemType, ItemTypeFlags flags) => ToItemTypes(itemType).MatchesFlag(flags);
        public static bool MatchesFlag(this Item item, ItemTypeFlags flags) => item.type.MatchesFlag(flags);

        public static bool Contains(this IReadOnlyList<Item> items, ItemTypeFlags typeFlags)
        {
            return Only(items, typeFlags).Count > 0;
        }

        public static ItemView Only(this IReadOnlyList<Item> items, ItemTypeFlags flag)
        {
            return new ItemView(items, flag);
        }

        public static ItemView Not(this IReadOnlyList<Item> items, ItemTypeFlags flag)
        {
            ItemTypeFlags inverted = ItemTypeFlags.All ^ flag;
            return new ItemView(items, inverted);
        }

        public static ItemTypeFlags AccumulateItemTypes(this IEnumerable<Item> items)
        {
            var types = ItemTypeFlags.None;

            foreach (Item item in items)
                types |= ToItemTypes(item.type);

            return types;
        }

        public static Item PreviousClip(this Item item) => item.Previous(ItemTypeFlags.Clip);
        public static Item PreviousGap(this Item item) => item.Previous(ItemTypeFlags.Gap);
        public static Item PreviousTransition(this Item item) => item.Previous(ItemTypeFlags.Transition);
        public static Item PreviousMarker(this Item item) => item.Previous(ItemTypeFlags.Marker);

        public static Item NextClip(this Item item) => item.Next(ItemTypeFlags.Clip);
        public static Item NextGap(this Item item) => item.Next(ItemTypeFlags.Gap);
        public static Item NextTransition(this Item item) => item.Next(ItemTypeFlags.Transition);
        public static Item NextMarker(this Item item) => item.Next(ItemTypeFlags.Marker);

        public static bool ContainsClip(this IReadOnlyList<Item> items) => items.Contains(ItemTypeFlags.Clip);
        public static bool ContainsMarker(this IReadOnlyList<Item> items) => items.Contains(ItemTypeFlags.Marker);
        public static bool ContainsGap(this IReadOnlyList<Item> items) => items.Contains(ItemTypeFlags.Gap);
        public static bool ContainsTransition(this IReadOnlyList<Item> items) => items.Contains(ItemTypeFlags.Transition);
        public static bool ContainsInterval(this IReadOnlyList<Item> items) => items.Contains(ItemTypeFlags.Interval);

        public static ItemView OnlyClips(this IReadOnlyList<Item> items) => items.Only(ItemTypeFlags.Clip);
        public static ItemView OnlyMarkers(this IReadOnlyList<Item> items) => items.Only(ItemTypeFlags.Marker);
        public static ItemView OnlyGaps(this IReadOnlyList<Item> items) => items.Only(ItemTypeFlags.Gap);
        public static ItemView OnlyTransitions(this IReadOnlyList<Item> items) => items.Only(ItemTypeFlags.Transition);
        public static ItemView OnlyIntervals(this IReadOnlyList<Item> items) => items.Only(ItemTypeFlags.Interval);

        public static ItemView FilterOutClips(this IReadOnlyList<Item> items) => items.Not(ItemTypeFlags.Clip);
        public static ItemView FilterOutMarkers(this IReadOnlyList<Item> items) => items.Not(ItemTypeFlags.Marker);
        public static ItemView FilterOutGaps(this IReadOnlyList<Item> items) => items.Not(ItemTypeFlags.Gap);
        public static ItemView FilterOutTransitions(this IReadOnlyList<Item> items) => items.Not(ItemTypeFlags.Transition);
        public static ItemView FilterOutIntervals(this IReadOnlyList<Item> items) => items.Not(ItemTypeFlags.Interval);

        static ItemTypeFlags ToItemTypes(Item.Type type)
        {
            return type switch
            {
                Item.Type.Clip => ItemTypeFlags.Clip,
                Item.Type.Gap => ItemTypeFlags.Gap,
                Item.Type.Transition => ItemTypeFlags.Transition,
                Item.Type.Marker => ItemTypeFlags.Marker,
                _ => ItemTypeFlags.None
            };
        }
    }
}
