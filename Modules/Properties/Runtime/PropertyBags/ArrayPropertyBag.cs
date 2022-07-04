// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Properties
{
    /// <summary>
    /// An <see cref="IPropertyBag{T}"/> implementation for a built in array of <typeparamref name="TElement"/>.
    /// </summary>
    /// <typeparam name="TElement">The element type.</typeparam>
    public sealed class ArrayPropertyBag<TElement> : IndexedCollectionPropertyBag<TElement[], TElement>
    {
        /// <inheritdoc/>
        protected override InstantiationKind InstantiationKind => InstantiationKind.PropertyBagOverride;

        /// <inheritdoc/>
        protected override TElement[] InstantiateWithCount(int count) => new TElement[count];

        /// <inheritdoc/>
        protected override TElement[] Instantiate() => Array.Empty<TElement>();
    }
}
