// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    class PanEvent : EventBase<PanEvent>
    {
        public Vector2 factor { get; private set; }

        public static PanEvent GetPooled(Vector2 factor)
        {
            PanEvent pooled = EventBase<PanEvent>.GetPooled();
            pooled.factor = factor;
            pooled.bubbles = true;
            pooled.tricklesDown = true;
            return pooled;
        }

        public static void Send(VisualElement target, Vector2 factor)
        {
            using PanEvent evt = GetPooled(factor);
            evt.target = target;
            target.SendEvent(evt);
        }

        public PanEvent() => LocalInit();

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            factor = Vector2.one;
        }
    }
}
