// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Utility for applying a registered data type style to an icon <see cref="Image"/>.
    /// </summary>
    static class DataTypeIconHelper
    {
        /// <summary>
        /// Applies a registered data type style to an icon image and returns the new <c>m_IconIsInline</c> value.
        /// </summary>
        /// <remarks>
        /// Three cases are handled:
        /// <list type="bullet">
        /// <item><c>suppressIcon</c>: icon is hidden; tintColor is set inline.</item>
        /// <item>Custom <c>icon</c> texture: icon image and tintColor are set inline.</item>
        /// <item>Default USS icon (<c>suppressIcon=false</c>, <c>icon=null</c>): USS provides the image;
        ///   only tintColor is set inline. The image element is recreated when switching away from a
        ///   previously-inline state so that USS regains control of the icon image.</item>
        /// </list>
        /// When <paramref name="overrideIcon"/> is <c>false</c> (collection element type fallback),
        /// USS provides the collection icon image and only tintColor is applied.
        /// When <paramref name="typeStyle"/> is <c>null</c>, USS controls everything;
        /// the image element is recreated if needed to clear stale inline properties.
        /// </remarks>
        /// <param name="iconImage">The icon image. May be replaced if recreation is required.</param>
        /// <param name="typeStyle">The registered style for the data type, or null if none.</param>
        /// <param name="overrideIcon">
        ///   <c>false</c> when the style comes from the collection element type
        ///   (USS provides the collection image, only tintColor is applied).
        /// </param>
        /// <param name="wasInline">Whether any inline property was previously set on the icon.</param>
        /// <param name="createNewImage">
        ///   Factory that removes the old icon from its parent, creates a fresh <see cref="Image"/>
        ///   with the correct USS classes inserted at the same index, and returns it.
        ///   Only called when recreation is required.
        /// </param>
        /// <returns><c>true</c> if any inline property is now set on the icon; <c>false</c> otherwise.</returns>
        internal static bool ApplyIconStyle(
            ref Image iconImage,
            (Texture2D icon, Color color, bool suppressIcon)? typeStyle,
            bool overrideIcon,
            bool wasInline,
            Func<Image> createNewImage)
        {
            if (!typeStyle.HasValue)
            {
                // No registered style: let USS control everything.
                if (wasInline)
                    iconImage = createNewImage();
                return false;
            }

            if (!overrideIcon)
            {
                // Collection fallback: USS provides the icon image; we only tint it.
                // Recreate to clear any stale inline image property that would block USS.
                if (wasInline)
                    iconImage = createNewImage();
                iconImage.tintColor = typeStyle.Value.color;
                iconImage.style.display = DisplayStyle.Flex;
                return true;
            }

            if (typeStyle.Value.suppressIcon)
            {
                iconImage.tintColor = typeStyle.Value.color;
                iconImage.style.display = DisplayStyle.None;
                return true;
            }

            if (typeStyle.Value.icon != null)
            {
                iconImage.tintColor = typeStyle.Value.color;
                iconImage.style.display = DisplayStyle.Flex;
                iconImage.image = typeStyle.Value.icon;
                return true;
            }

            // Default USS icon with custom color: recreate to clear any stale inline image.
            if (wasInline)
                iconImage = createNewImage();
            iconImage.tintColor = typeStyle.Value.color;
            iconImage.style.display = DisplayStyle.Flex;
            return true;
        }
    }
}
