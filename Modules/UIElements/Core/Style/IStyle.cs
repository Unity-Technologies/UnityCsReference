// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    public partial interface IStyle
    {
        /// <summary>
        /// Background image scaling in the element's box.
        /// </summary>
        /// <remarks>
        /// This property is deprecated. Use [[BackgroundPropertyHelper]] to set the background properties.
        /// For more information, Refer to [[wiki:UIB-styling-ui-backgrounds#set-the-scale-mode-for-a-background-image|Set the scale mode for a background image]].
        /// </remarks>
        [Obsolete("unityBackgroundScaleMode is deprecated. Use background-* properties instead.")]
        StyleEnum<ScaleMode> unityBackgroundScaleMode { get; set; }
    }
}
