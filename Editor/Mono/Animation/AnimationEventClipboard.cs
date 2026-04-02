// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    // Classes for copy/paste support of animation window related things
    class AnimationEventComparer : IComparer<AnimationEvent>
    {
        public int Compare(AnimationEvent x, AnimationEvent y)
        {
            float timeX = x.time;
            float timeY = y.time;
            if (timeX != timeY)
                return ((int)Mathf.Sign(timeX - timeY));

            int valueX = x.GetHashCode();
            int valueY = y.GetHashCode();
            return valueX - valueY;
        }
    }

    [Serializable]
    sealed class AnimationEventClipboard
    {
        public float time = 0;
        public string functionName = "";
        public string stringParam = "";
        public EntityId objectParam = EntityId.None;
        public float floatParam = 0;
        public int intParam = 0;
        public SendMessageOptions messageOptions = SendMessageOptions.RequireReceiver;

        public AnimationEventClipboard(AnimationEvent e)
        {
            time = e.time;
            functionName = e.functionName;
            stringParam = e.stringParameter;
            objectParam = e.objectReferenceParameter ? e.objectReferenceParameter.GetEntityId() : EntityId.None;
            floatParam = e.floatParameter;
            intParam = e.intParameter;
            messageOptions = e.messageOptions;
        }

        public static AnimationEvent FromClipboard(AnimationEventClipboard e)
        {
            return new AnimationEvent
            {
                time = e.time,
                functionName = e.functionName,
                stringParameter = e.stringParam,
                objectReferenceParameter = InternalEditorUtility.GetObjectFromEntityId(e.objectParam),
                floatParameter = e.floatParam,
                intParameter = e.intParam,
                messageOptions = e.messageOptions
            };
        }
    }

    [Serializable]
    class AnimationEventsClipboard
    {
        public AnimationEventClipboard[] events;

        internal static bool CanPaste()
        {
            return Clipboard.HasCustomValue<AnimationEventsClipboard>();
        }

        internal static void CopyEvents(IList<AnimationEvent> allEvents, bool[] selected, int explicitIndex = -1)
        {
            var copyEvents = new List<AnimationEventClipboard>();
            // If a selection already exists, copy selection instead of clicked index
            if (Array.Exists(selected, s => s))
            {
                for (var i = 0; i < selected.Length; ++i)
                {
                    if (selected[i])
                        copyEvents.Add(new AnimationEventClipboard(allEvents[i]));
                }
            }
            // Else, only copy the clicked animation event
            else if (explicitIndex >= 0)
            {
                copyEvents.Add(new AnimationEventClipboard(allEvents[explicitIndex]));
            }
            var data = new AnimationEventsClipboard {events = copyEvents.ToArray()};

            Clipboard.SetCustomValue(data);
        }

        internal static AnimationEvent[] AddPastedEvents(AnimationEvent[] events, float time, out bool[] selected)
        {
            selected = null;
            var data = Clipboard.GetCustomValue<AnimationEventsClipboard>();
            if (data?.events == null || data.events.Length == 0)
                return null;

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var minTime = data.events.Min(e => e.time);
#pragma warning restore UA2001

            var origEventsCount = events.Length;
            // Append new events to the end first,
            var newEvents = new List<AnimationEvent>();
            foreach (var e in data.events)
            {
                var t = e.time - minTime + time;
                var newEvent = AnimationEventClipboard.FromClipboard(e);
                newEvent.time = t;
                newEvents.Add(newEvent);
            }
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            events = events.Concat(newEvents).ToArray();
#pragma warning restore UA2001

            // Re-sort events by time
            var order = new int[events.Length];
            for (var i = 0; i < order.Length; i++)
                order[i] = i;
            Array.Sort(events, order, new AnimationEventComparer());

            // Mark pasted ones as selected
            selected = new bool[events.Length];
            for (var i = 0; i < order.Length; ++i)
                selected[i] = order[i] >= origEventsCount;

            return events;
        }
    }
}
