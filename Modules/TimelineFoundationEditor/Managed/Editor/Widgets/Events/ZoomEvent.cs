// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    class ZoomEvent : EventBase<ZoomEvent>
    {
        public float centerRatio { get; private set; }
        public float scale { get; private set; }

        public static ZoomEvent GetPooled(float centerRatio, float scale)
        {
            ZoomEvent pooled = EventBase<ZoomEvent>.GetPooled();
            pooled.centerRatio = centerRatio;
            pooled.scale = scale;
            pooled.bubbles = true;
            pooled.tricklesDown = true;
            return pooled;
        }

        public static void Send(VisualElement target, float centerRatio, float scale)
        {
            using ZoomEvent evt = GetPooled(centerRatio, scale);
            evt.target = target;
            target.SendEvent(evt);
        }

        public ZoomEvent() => LocalInit();

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            centerRatio = 0f;
            scale = 1f;
        }
    }
}
