using System;

namespace UnityEngine.UIElements
{
    public partial class VisualElement
    {
        public string tooltip
        {
            get
            {
                string userArg;
                TryGetUserArgs<TooltipEvent, string>(OnTooltip, TrickleDown.NoTrickleDown, out userArg);
                return userArg ?? string.Empty;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    UnregisterCallback<TooltipEvent, string>(OnTooltip);
                }
                else
                {
                    RegisterCallback<TooltipEvent, string>(OnTooltip, value);
                }
            }
        }

        static void OnTooltip(TooltipEvent e, string tooltip)
        {
            VisualElement element = e.currentTarget as VisualElement;
            if (element != null)
                e.rect = element.worldBound;
            e.tooltip = tooltip;
            e.StopImmediatePropagation();
        }
    }
}
