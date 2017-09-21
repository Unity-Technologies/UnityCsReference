// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public class PostLayoutEvent : EventBase<PostLayoutEvent>, IPropagatableEvent
    {
        public static PostLayoutEvent GetPooled(bool hasNewLayout)
        {
            PostLayoutEvent e = GetPooled();
            e.hasNewLayout = hasNewLayout;
            return e;
        }

        protected override void Init()
        {
            base.Init();
            hasNewLayout = false;
        }

        public bool hasNewLayout { get; private set; }

        public PostLayoutEvent()
        {
            Init();
        }
    }
}
