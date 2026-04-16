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

        /// <summary>
        /// Clears inline style properties of the element.
        /// </summary>
        /// <param name="clearSourceAssetStyles">Indicates if the inline style properties coming from the source asset must also be cleared for the element. </param>
        /// <remarks>
        ///
        /// After clearing, the style properties of the element revert to the values defined in stylesheets or default values.
        ///
        /// By default, this method clears all inline style properties, including those coming from the source asset from which the element was created.
        /// To preserve these properties, set the <paramref name="clearSourceAssetStyles"/> to false.
        /// </remarks>
        /// <example>
        /// The following example compares this method and resetting style properties individually.
        /// <code source="../../../Modules/UIElements/Tests/UIElementsExamples/Assets/ui-toolkit-manual-code-examples/doc-examples/VisualElementClearInlineStylesWindow.cs"/>
        /// </example>
        void Clear(bool clearSourceAssetStyles = true);
    }
}
