// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Controls how many items can be selected at once.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="BaseVerticalCollectionView"/> and its inheritors including <see cref="ListView"/>,
    /// <see cref="TreeView"/>, <see cref="MultiColumnListView"/> and <see cref="MultiColumnTreeView"/>.
    /// Set the view's @@selectionType@@ property as one of these values to control how many items can be selected at once.
    /// </remarks>
    public enum SelectionType
    {
        /// <summary>
        /// Selections are disabled.
        /// </summary>
        None,
        /// <summary>
        /// Only one item is selectable.
        /// </summary>
        Single,
        /// <summary>
        /// Multiple items are selectable at once.
        /// </summary>
        Multiple
    }
}
