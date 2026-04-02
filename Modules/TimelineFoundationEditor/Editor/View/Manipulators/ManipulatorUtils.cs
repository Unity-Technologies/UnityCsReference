// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.View.Internals;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    static class ManipulatorUtils
    {
        public static void AddEdges(this SnapEngine snapEngine, ViewContext viewContext, MoveBehaviour behaviour)
        {
            BuildSnapEngine(snapEngine, viewContext, behaviour.GetManipulatedItems());
        }

        public static void AddEdges(this SnapEngine snapEngine, ViewContext viewContext, TrimBehaviour behaviour)
        {
            BuildSnapEngine(snapEngine, viewContext, behaviour.GetManipulatedItems());
        }

        public static void ShowSnapEngineDebug(CanvasOverlay moveOverlay, SnapEngine snapEngine, float attractionWidth)
        {
            RemoveSnapEngineDebug(moveOverlay);

            var snapEngineDebugOverlay = new SnapEngineDebugOverlay { snapEngine = snapEngine, attractionWidth = attractionWidth };

            moveOverlay.Add(snapEngineDebugOverlay);
            snapEngineDebugOverlay.Show();
            snapEngineDebugOverlay.ForceUpdate();
        }

        public static void RemoveSnapEngineDebug(CanvasOverlay moveOverlay)
        {
            var snapEngineDebug = moveOverlay.Q<SnapEngineDebugOverlay>();
            if (snapEngineDebug != null)
                moveOverlay.Remove(snapEngineDebug);
        }

        public static void BuildSnapEngine(SnapEngine snapEngine, ViewContext viewContext, IReadOnlyCollection<Item> filterOut)
        {
            snapEngine.AddEdge(DiscreteTime.Zero)
                .AddEdge(viewContext.time)
                .AddItemEdges(viewContext.visibleItems, filterOut);
        }

        public static DiscreteTime? GetRippleStartMoveIndicator(Track track, IReadOnlyList<Item> manipulatedItems, SequenceLookup sequenceLookup)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
#pragma warning disable UA2011 // Pre-existing usage of FirstOrDefault.
            var manipulatedItem = manipulatedItems.OnlyClips().Where(i => i.parent.ID == track.ID).OrderBy(i => i.GetVisibleRange().start).FirstOrDefault();
#pragma warning restore UA2011
#pragma warning restore UA2001
            if (!manipulatedItem.IsValid())
                return null;
            var item = sequenceLookup.GetItemFromId(manipulatedItem.ID);
            if (!item.IsValid())
                return null;
            return item.GetVisibleRange().start;
        }

        public static IEnumerable<DiscreteTime> GetMixStartMoveIndicators(Track track, IReadOnlyList<Item> manipulatedItems, SequenceLookup sequenceLookup)
        {
            var times = new List<DiscreteTime>();
            foreach (var item in manipulatedItems)
            {
                if (!item.IsValid() || item.parent.ID != track.ID)
                    continue;

                if (item.Previous().isTransition)
                    times.Add(item.GetVisibleRange().start);
            }
            return times;
        }

        public static IEnumerable<DiscreteTime> GetMixEndMoveIndicators(Track track, IReadOnlyList<Item> manipulatedItems, SequenceLookup sequenceLookup)
        {
            var times = new List<DiscreteTime>();
            foreach (var item in manipulatedItems)
            {
                if (!item.IsValid() || item.parent.ID != track.ID)
                    continue;

                if (item.Next().isTransition)
                    times.Add(item.GetVisibleRange().end);
            }
            return times;
        }

        public static DiscreteTime? GetMixStartTrimIndicator(Item item)
        {
            if (!item.IsValid() || !item.Previous().isTransition)
                return null;
            return item.Previous().GetVisibleRange().end;
        }

        public static DiscreteTime? GetMixEndTrimIndicator(Item item)
        {
            if (!item.IsValid() || !item.Next().isTransition)
                return null;
            return item.Next().GetVisibleRange().start;
        }

        public static DiscreteTime? GetRippleStartTrimIndicator(Item item)
        {
            if (!item.IsValid())
                return null;
            return item.GetVisibleRange().start;
        }

        public static DiscreteTime? GetRippleEndTrimIndicator(Item item)
        {
            if (!item.IsValid())
                return null;
            return item.GetVisibleRange().end;
        }

        public static DiscreteTime? GetReplaceStartTrimIndicator(Item item, IEnumerable<TimeRange> previousItemsRanges)
        {
#pragma warning disable UA2006 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (!item.IsValid() || !previousItemsRanges.Any(r => r.Overlaps(item.GetVisibleRange())))
#pragma warning restore UA2006
                return null;
            return item.GetVisibleRange().start;
        }

        public static DiscreteTime? GetReplaceEndTrimIndicator(Item item, IEnumerable<TimeRange> nextItemsRanges)
        {
#pragma warning disable UA2006 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (!item.IsValid() || !nextItemsRanges.Any(r => r.Overlaps(item.GetVisibleRange())))
#pragma warning restore UA2006
                return null;
            return item.GetVisibleRange().end;
        }

        public static T PickElemenOfType<T>(this PointerManipulator manipulator, Vector2 position) where T : class
        {
            return manipulator.target.panel.Pick(position)?.GetFirstOfType<T>();
        }
    }
}
