using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Event sent to find the first VisualElement that displays a tooltip.
    /// </summary>
    /// <remarks>
    /// This event can be used instead of <see cref="VisualElement.tooltip"/> to compute tooltips only when they are about to be displayed.
    /// </remarks>
    public class TooltipEvent : EventBase<TooltipEvent>
    {
        /// <summary>
        /// Text to display inside the tooltip box.
        /// </summary>
        public string tooltip { get; set; }
        /// <summary>
        /// Rectangle of the hovered VisualElement in the panel coordinate system.
        /// </summary>
        public Rect rect { get; set; }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
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
