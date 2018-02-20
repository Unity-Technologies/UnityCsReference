// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public class PostLayoutEvent : EventBase<PostLayoutEvent>, IPropagatableEvent
    {
        public static PostLayoutEvent GetPooled(bool hasNewLayout, Rect oldRect, Rect newRect)
        {
            PostLayoutEvent e = GetPooled();
            e.hasNewLayout = hasNewLayout;
            e.oldRect = oldRect;
            e.newRect = newRect;
            return e;
        }

        protected override void Init()
        {
            base.Init();
            hasNewLayout = false;
        }

        public bool hasNewLayout { get; private set; }
        public Rect oldRect { get; private set; }
        public Rect newRect { get; private set; }

        public PostLayoutEvent()
        {
            Init();
        }
    }
}
