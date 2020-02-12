// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    // Classes for copy/paste support of animation window related things

    [Serializable]
    internal sealed class AnimationWindowEventClipboard
    {
        public float time = 0;
        public string functionName = "";
        public string stringParam = "";
        public int objectParam = 0;
        public float floatParam = 0;
        public int intParam = 0;
        public SendMessageOptions messageOptions = SendMessageOptions.RequireReceiver;

        public AnimationWindowEventClipboard(AnimationEvent e)
        {
            time = e.time;
            functionName = e.functionName;
            stringParam = e.stringParameter;
            objectParam = e.objectReferenceParameter ? e.objectReferenceParameter.GetInstanceID() : 0;
            floatParam = e.floatParameter;
            intParam = e.intParameter;
            messageOptions = e.messageOptions;
        }

        public static AnimationEvent FromClipboard(AnimationWindowEventClipboard e)
        {
            return new AnimationEvent
            {
                time = e.time,
                functionName = e.functionName,
                stringParameter = e.stringParam,
                objectReferenceParameter = InternalEditorUtility.GetObjectFromInstanceID(e.objectParam),
                floatParameter = e.floatParam,
                intParameter = e.intParam,
                messageOptions = e.messageOptions
            };
        }
    }

    [Serializable]
    internal class AnimationWindowEventsClipboard
    {
        public AnimationWindowEventClipboard[] events;

        internal static bool CanPaste()
        {
            return Clipboard.HasCustomValue<AnimationWindowEventsClipboard>();
        }

        internal static void CopyEvents(IList<AnimationEvent> allEvents, bool[] selected, int explicitIndex = -1)
        {
            var copyEvents = new List<AnimationWindowEventClipboard>();
            // If a selection already exists, copy selection instead of clicked index
            if (Array.Exists(selected, s => s))
            {
                for (var i = 0; i < selected.Length; ++i)
                {
                    if (selected[i])
                        copyEvents.Add(new AnimationWindowEventClipboard(allEvents[i]));
                }
            }
            // Else, only copy the clicked animation event
            else if (explicitIndex >= 0)
            {
                copyEvents.Add(new AnimationWindowEventClipboard(allEvents[explicitIndex]));
            }
            var data = new AnimationWindowEventsClipboard {events = copyEvents.ToArray()};
            Clipboard.SetCustomValue(data);
        }

        internal static AnimationEvent[] AddPastedEvents(AnimationEvent[] events, float time, out bool[] selected)
        {
            selected = null;
            var data = Clipboard.GetCustomValue<AnimationWindowEventsClipboard>();
            if (data?.events == null || data.events.Length == 0)
                return null;

            var minTime = data.events.Min(e => e.time);

            var origEventsCount = events.Length;
            // Append new events to the end first,
            var newEvents = new List<AnimationEvent>();
            foreach (var e in data.events)
            {
                var t = e.time - minTime + time;
                var newEvent = AnimationWindowEventClipboard.FromClipboard(e);
                newEvent.time = t;
                newEvents.Add(newEvent);
            }
            events = events.Concat(newEvents).ToArray();

            // Re-sort events by time
            var order = new int[events.Length];
            for (var i = 0; i < order.Length; i++)
                order[i] = i;
            Array.Sort(events, order, new AnimationEventTimeLine.EventComparer());

            // Mark pasted ones as selected
            selected = new bool[events.Length];
            for (var i = 0; i < order.Length; ++i)
                selected[i] = order[i] >= origEventsCount;

            return events;
        }
    }
}
