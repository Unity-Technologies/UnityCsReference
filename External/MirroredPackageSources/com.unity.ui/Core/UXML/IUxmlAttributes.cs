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
