// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A list of disposable items.
    /// </summary>
    /// <typeparam name="T">The items type.</typeparam>
    class DisposableList<T> : List<T>, IDisposable where T : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableList{T}"/> class.
        /// </summary>
        public DisposableList() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableList{T}"/> class.
        /// </summary>
        /// <param name="capacity">The initial capacity of the list.</param>
        public DisposableList(int capacity)
            : base(capacity) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableList{T}"/> class.
        /// </summary>
        /// <param name="collection">A collection used to initialize this.</param>
        public DisposableList(IEnumerable<T> collection)
            : base(collection) { }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var item in this)
            {
                item.Dispose();
            }
        }
    }
}
