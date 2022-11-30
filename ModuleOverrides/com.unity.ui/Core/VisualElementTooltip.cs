// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;

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
        [CreateProperty]
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
                    if (string.IsNullOrEmpty(value))
                    {
                        return;
                    }

                    RegisterCallback<TooltipEvent>(SetTooltip);
                }

                var tooltipText = GetProperty(tooltipPropertyKey) as string;
                if (string.CompareOrdinal(tooltipText, value) == 0)
                    return;
                SetProperty(tooltipPropertyKey, value);
                NotifyPropertyChanged(tooltipProperty);
            }
        }
    }
}
