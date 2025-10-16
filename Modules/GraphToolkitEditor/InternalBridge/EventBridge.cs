// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.InternalBridge
{
    static class EventBridge
    {
        public static void SetPropagationBothWay(this EventBase eventBase)
        {
            eventBase.propagation = EventBase.EventPropagation.TricklesDown | EventBase.EventPropagation.Bubbles;
        }

        public static NavigationCancelEvent GetPooleNavigationCancelEvent(NavigationCancelEvent evt)
        {
            return NavigationCancelEvent.GetPooled(evt.deviceType, evt.modifiers);
        }

        public static NavigationSubmitEvent GetPooledNavigationSubmitEvent(NavigationSubmitEvent evt)
        {
            return NavigationSubmitEvent.GetPooled(evt.deviceType, evt.modifiers);
        }

        public static NavigationMoveEvent GetPooledNavigationMoveEvent(NavigationMoveEvent evt)
        {
            return NavigationMoveEvent.GetPooled(evt.direction, evt.deviceType, evt.modifiers);
        }
    }
}
