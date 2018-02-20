// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public class GeometryChangedEvent : EventBase<GeometryChangedEvent>, IPropagatableEvent
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
            oldRect = Rect.zero;
            newRect = Rect.zero;
        }

        public Rect oldRect { get; private set; }
        public Rect newRect { get; private set; }

        public GeometryChangedEvent()
        {
            Init();
        }
    }
}
