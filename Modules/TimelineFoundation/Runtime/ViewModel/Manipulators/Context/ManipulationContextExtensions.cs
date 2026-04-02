// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Timeline.Foundation.ViewModel
{
    static class ManipulationContextExtensions
    {
        public static IEnumerable<Item> FirstItemsOnAllTracks(this ManipulationContext context)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return context.manipulatedTracks.Select(i =>
#pragma warning disable UA2010 // Remove compile error for First()
            i.manipulatedItems.First()
#pragma warning restore UA2010
            );
#pragma warning restore UA2001
        }

        public static bool IsEmpty(this ManipulationContext context)
        {
            return context.allItems == null || context.allItems.Count == 0;
        }

        public static bool CanMoveToTrack(in this ManipulationContext context, IManipulationHandler handler, Track target)
        {
            if (handler == null) return true;

            foreach (Item item in context.allItems)
            {
                if (!handler.CanMoveToTrack(item.GetGenericContent(), target))
                    return false;
            }

            return true;
        }

        public static bool SupportsMoveToTrack(in this ManipulationContext context, IManipulationHandler handler)
        {
            if (handler == null) return true;

            foreach (Item item in context.allItems)
            {
                if (!handler.SupportsMoveToTrack(item.GetGenericContent()))
                    return false;
            }

            return true;
        }

        public static bool ManipulatingMarkers(in this ManipulationContext context)
        {
            return context.allItems.ContainsMarker();
        }

        public static bool ManipulatingClips(in this ManipulationContext context)
        {
            return context.allItems.ContainsClip();
        }
    }
}
