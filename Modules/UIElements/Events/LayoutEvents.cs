// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    public class GeometryChangedEvent : EventBase<GeometryChangedEvent>
    {
        public static GeometryChangedEvent GetPooled(Rect oldRect, Rect newRect)
        {
            GeometryChangedEvent e = GetPooled();
            e.oldRect = oldRect;
            e.newRect = newRect;
            return e;
        }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            oldRect = Rect.zero;
            newRect = Rect.zero;
            layoutPass = 0;
        }

        public Rect oldRect { get; private set; }
        public Rect newRect { get; private set; }

        internal int layoutPass {get; set; }

        public GeometryChangedEvent()
        {
            LocalInit();
        }
    }
}
