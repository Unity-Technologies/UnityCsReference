// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Specifies the sorting method.
    /// </summary>
    /// <remarks>
    /// Use <see cref="SortMethod"/> to define how elements are sorted, either by their creation order or their display order.
    /// </remarks>
    public enum SortMethod
    {
        /// <summary>
        /// The sorting is done based on the creation order.
        /// </summary>
        Creation,

        /// <summary>
        /// The sorting is done based on the display order.
        /// </summary>
        Display
    }
}
