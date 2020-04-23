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
