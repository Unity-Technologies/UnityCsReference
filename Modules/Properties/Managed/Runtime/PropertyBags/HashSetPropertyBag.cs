// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.Properties
{
    /// <summary>
    /// An <see cref="IPropertyBag{T}"/> implementation for a <see cref="HashSet{TElement}"/> type.
    /// </summary>
    /// <typeparam name="TElement">The element type.</typeparam>
    public class HashSetPropertyBag<TElement> : SetPropertyBagBase<HashSet<TElement>, TElement>
    {
        /// <inheritdoc/>
        protected override InstantiationKind InstantiationKind => InstantiationKind.PropertyBagOverride;

        /// <inheritdoc/>
        protected override HashSet<TElement> Instantiate() => new HashSet<TElement>();
    }
}
