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
        //PropertyName to store in property bag.
        internal static readonly PropertyName tooltipPropertyKey = new PropertyName("--unity-tooltip");

        /// <summary>
        /// Text to display inside an information box after the user hovers the element for a small amount of time.
        /// </summary>
        public string tooltip
        {
            get
            {
                string tooltipText = GetProperty(tooltipPropertyKey) as string;

                return tooltipText ?? String.Empty;
            }
            set
            {
                if (!HasProperty(tooltipPropertyKey))
                {
                    RegisterCallback<TooltipEvent>(evt => OnTooltip(evt));
                }

                SetProperty(tooltipPropertyKey, value);
            }
        }

        static void OnTooltip(TooltipEvent e)
        {
            VisualElement element = e.currentTarget as VisualElement;
            if (element != null && !string.IsNullOrEmpty(element.tooltip))
            {
                e.rect = element.worldBound;
                e.tooltip = element.tooltip;
                e.StopImmediatePropagation();
            }
        }
    }
}
