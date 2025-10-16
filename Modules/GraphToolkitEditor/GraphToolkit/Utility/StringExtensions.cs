// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor;

namespace Unity.GraphToolkit
{
    /// <summary>
    /// Extension methods for working with Unity Style Sheet (USS) styling strings.
    /// </summary>
    /// <remarks>
    /// These methods help construct, modify, and format USS-related strings, which makes it easier to manage style names, class names, and modifiers.
    /// Use this class when generating USS style names or appending modifiers.
    /// </remarks>
    [UnityRestricted]
    internal static class StringExtensions
    {
        /// <summary>
        /// Appends a Unity Style Sheet (USS) element name to a USS name.
        /// </summary>
        /// <param name="blockName">The current USS name.</param>
        /// <param name="elementName">The element name to append.</param>
        /// <returns>The combined USS element name.</returns>
        /// <remarks>
        /// 'WithUssElement' appends a USS element name to an existing USS block name using the <c>__</c> separator. This method standardizes USS naming conventions,
        /// and ensures consistent style names for UI elements. Examples of element names include: <c>__icon</c>, <c>__title</c>, and <c>__container</c>.
        /// </remarks>
        public static string WithUssElement(this string blockName, string elementName) => blockName + "__" + elementName;

        /// <summary>
        /// Appends a Unity Style Sheet (USS) modifier to a USS name.
        /// </summary>
        /// <param name="blockName">The current USS name.</param>
        /// <param name="modifier">The modifier to append.</param>
        /// <returns>The combined USS name with the modifier.</returns>
        /// <remarks>
        /// 'WithUssModifier' appends a USS modifier to an existing USS block name using the <c>--</c> separator. This method standardizes USS naming conventions.
        /// Examples of common modifiers include: <c>--hidden</c>, <c>--collapsed</c>, and <c>--highlighted</c>.
        /// </remarks>
        public static string WithUssModifier(this string blockName, string modifier) => blockName + "--" + modifier;
    }
}
