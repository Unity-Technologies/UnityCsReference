// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    sealed class CanvasUpdateEvent : EventBase<CanvasUpdateEvent>
    {
        public enum UpdateType
        {
            All,
            Target,
            TargetAndDescendants,
        }

        public UpdateType updateType { get; private set; }

        public static CanvasUpdateEvent GetPooled(UpdateType updateType)
        {
            CanvasUpdateEvent pooled = EventBase<CanvasUpdateEvent>.GetPooled();
            pooled.updateType = updateType;
            pooled.tricklesDown = true;
            pooled.bubbles = false;
            return pooled;
        }

        public static void Send(VisualElement target, UpdateType type)
        {
            using CanvasUpdateEvent evt = GetPooled(type);
            evt.target = target;
            target.SendEvent(evt);
        }

        public CanvasUpdateEvent() => LocalInit();

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            updateType = UpdateType.TargetAndDescendants;
        }
    }
}
