// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// View model for a <see cref="ItemLibraryItem"/> in the ItemLibrary Tree View.
    /// </summary>
    interface IItemView_Internal : ITreeItemView_Internal
    {
        /// <summary>
        /// The <see cref="ItemLibraryItem"/> represented by this view.
        /// </summary>
        public ItemLibraryItem Item { get; }
    }
}
