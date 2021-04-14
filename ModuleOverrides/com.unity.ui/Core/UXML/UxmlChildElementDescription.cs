// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Describe an allowed child element for an element.
    /// </summary>
    public class UxmlChildElementDescription
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UxmlChildElementDescription(Type t)
        {
            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            elementName = t.Name;
            elementNamespace = t.Namespace;
        }

        /// <summary>
        /// The name of the allowed child element.
        /// </summary>
        public string elementName { get; protected set; }
        /// <summary>
        /// The namespace name of the allowed child element.
        /// </summary>
        public string elementNamespace { get; protected set; }
    }
}
