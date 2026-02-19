// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Animations.AnimationWindow.TimelineFoundation
{
    class DisplayRangeChangeEvent : EventBase<DisplayRangeChangeEvent>
    {
        public TimeRange previousValue { get; private set; }
        public TimeRange newValue { get; private set; }

        public static DisplayRangeChangeEvent GetPooled(TimeRange previousValue, TimeRange newValue)
        {
            DisplayRangeChangeEvent pooled = EventBase<DisplayRangeChangeEvent>.GetPooled();
            pooled.previousValue = previousValue;
            pooled.newValue = newValue;
            pooled.bubbles = true;
            pooled.tricklesDown = true;
            return pooled;
        }

        public static void Send(VisualElement target, TimeRange previousValue, TimeRange newValue)
        {
            using DisplayRangeChangeEvent evt = GetPooled(previousValue, newValue);
            evt.target = target;
            target.SendEvent(evt);
        }
    }
}
