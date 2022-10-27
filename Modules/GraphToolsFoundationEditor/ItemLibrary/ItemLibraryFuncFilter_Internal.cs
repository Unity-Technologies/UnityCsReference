// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// Basic filter made of collections of functors
    /// </summary>
    class ItemLibraryFuncFilter_Internal : ItemLibraryFilter
    {
        /// <summary>
        /// Empty filter, will not filter anything.
        /// </summary>
        public static ItemLibraryFuncFilter_Internal Empty => new ItemLibraryFuncFilter_Internal();

        protected List<Func<ItemLibraryItem, bool>> m_FilterFunctions = new List<Func<ItemLibraryItem, bool>>();

        /// <summary>
        /// Instantiates a filter with filtering functions.
        /// </summary>
        /// <param name="functions">Filtering functions that say whether to keep an item or not</param>
        public ItemLibraryFuncFilter_Internal(params Func<ItemLibraryItem, bool>[] functions)
        {
            m_FilterFunctions.AddRange(functions);
        }

        /// <summary>
        /// Add a filter functor to a filter in place.
        /// </summary>
        /// <param name="func">filter functor to add</param>
        /// <returns>The filter with the new functor added</returns>
        public ItemLibraryFuncFilter_Internal WithFilter(Func<ItemLibraryItem, bool> func)
        {
            m_FilterFunctions.Add(func);
            return this;
        }

        /// <inheritdoc />
        public override bool Match(ItemLibraryItem item)
        {
            return m_FilterFunctions.All(f => f(item));
        }
    }
}
