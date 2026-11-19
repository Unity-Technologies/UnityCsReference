// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.Properties.Internal
{
    /// <summary>
    /// Interface for attaching attributes to an object. This is an internal interface.
    /// </summary>
    interface IAttributes
    {
        /// <summary>
        /// Gets access the the internal <see cref="Attribute"/> storage.
        /// </summary>
        List<Attribute> Attributes { get; set; }

        /// <summary>
        /// Adds an attribute to this object.
        /// </summary>
        /// <param name="attribute">The attribute to add.</param>
        void AddAttribute(Attribute attribute);

        /// <summary>
        /// Adds a set of attributes to this object.
        /// </summary>
        /// <param name="attributes"></param>
        void AddAttributes(IEnumerable<Attribute> attributes);

        /// <summary>
        /// Sets the attributes for the duration of the scope.
        /// </summary>
        /// <param name="attributes"></param>
        AttributesScope CreateAttributesScope(IAttributes attributes);
    }
}
