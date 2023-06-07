// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Hierarchy
{
    /// <summary>
    /// An interface that is used to access strongly typed node data.
    /// </summary>
    /// <typeparam name="T">The property value type.</typeparam>
    public interface IHierarchyProperty<T>
    {
        /// <summary>
        /// Returns <see langword="true"/> if the native property is valid.
        /// </summary>
        public bool IsCreated { get; }

        /// <summary>
        /// Gets the property value for the given <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The property value of the hierarchy node.</returns>
        T GetValue(in HierarchyNode node);

        /// <summary>
        /// Sets the property value for a <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="value">The value to set.</param>
        void SetValue(in HierarchyNode node, T value);

        /// <summary>
        /// Removes the property value for a <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        void ClearValue(in HierarchyNode node);
    }
}
