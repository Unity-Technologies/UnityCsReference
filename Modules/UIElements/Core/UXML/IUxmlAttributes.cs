// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This type allows UXML attribute value retrieval during the VisualElement instantiation. An instance will be provided to the factory method - see <see cref="UXMLFactoryAttribute"/>.
    /// </summary>
    public interface IUxmlAttributes
    {
        /// <summary>
        /// Get the value of an attribute as a string.
        /// </summary>
        /// <param name="attributeName">Attribute name.</param>
        /// <param name="value">The attribute value or null if not found.</param>
        /// <returns>True if the attribute was found, false otherwise.</returns>
        bool TryGetAttributeValue(string attributeName, out string value);
    }
}
