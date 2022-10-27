// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// Base class to filter databases.
    /// </summary>
    abstract class ItemLibraryFilter
    {
        /// <summary>
        /// Checks if an item matches the filter.
        /// </summary>
        /// <param name="item">item the check</param>
        /// <returns>true if the item matches the filter, false otherwise.</returns>
        public abstract bool Match(ItemLibraryItem item);
    }
}
