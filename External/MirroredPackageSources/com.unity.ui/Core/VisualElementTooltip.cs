using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Base class for objects that are part of the UIElements visual tree.
    /// </summary>
    /// <remarks>
    /// VisualElement contains several features that are common to all controls in UIElements, such as layout, styling and event handling.
    /// Several other classes derive from it to implement custom rendering and define behaviour for controls.
    /// </remarks>
    public partial class VisualElement
    {
        /// <summary>
        /// Text to display inside an information box after the user hovers the element for a small amount of time.
        /// </summary>
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
