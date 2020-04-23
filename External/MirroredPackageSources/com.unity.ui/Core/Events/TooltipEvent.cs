using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEngine.UIElements
{
    public class TooltipEvent : EventBase<TooltipEvent>
    {
        public string tooltip { get; set; }
        public Rect rect { get; set; }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
            rect = default(Rect);
            tooltip = string.Empty;
        }

        internal static TooltipEvent GetPooled(string tooltip, Rect rect)
        {
            TooltipEvent e = GetPooled();
            e.tooltip = tooltip;
            e.rect = rect;
            return e;
        }

        public TooltipEvent()
        {
            LocalInit();
        }
    }
}
