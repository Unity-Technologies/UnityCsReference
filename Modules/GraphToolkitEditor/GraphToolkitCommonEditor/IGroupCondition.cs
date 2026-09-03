// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for a condition that groups other conditions.
    /// </summary>
    /// <remarks>
    /// A group condition combines its nested conditions with the logical operation specified by
    /// <see cref="Operation"/>. Nested conditions can themselves be group conditions, forming a
    /// tree that expresses arbitrary boolean logic. To traverse the tree, enumerate
    /// <see cref="Get"/> and check whether each element is itself an <see cref="IGroupCondition"/>.
    /// This interface is implemented by Unity and is not intended to be implemented by user code.
    /// </remarks>
    public interface IGroupCondition : ICondition
    {
        /// <summary>
        /// The logical operation applied to the nested conditions.
        /// </summary>
        GroupConditionOperation Operation { get; }

        /// <summary>
        /// Retrieves the conditions nested in this group.
        /// </summary>
        /// <returns>The nested conditions, in the order they appear in the group.</returns>
        IEnumerable<ICondition> Get();

        /// <summary>
        /// The number of conditions nested in this group.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Retrieves the condition at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the condition to retrieve.</param>
        /// <returns>The condition at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is less than 0 or greater than or equal to <see cref="Count"/>.
        /// </exception>
        /// <remarks>
        /// Throws <see cref="System.ArgumentOutOfRangeException"/> when the index is less than 0
        /// or greater thanor equal to <see cref="Count"/>
        /// </remarks>
        ICondition Get(int index);

        /// <summary>
        /// Inserts a condition into this group at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert the condition. Pass <see cref="Count"/>
        /// to append the condition at the end of the group.</param>
        /// <param name="condition">The condition to insert.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is less than 0 or greater than <see cref="Count"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <paramref name="condition"/> is this group or a group that contains this group, or when it is, or
        /// contains, a variable condition whose variable belongs to another graph.
        /// </exception>
        /// <remarks>
        /// If the condition already belongs to another group, it is first removed from that group.
        /// Throws <see cref="System.ArgumentOutOfRangeException"/> when the index is less than 0 or greater than <see cref="Count"/>.
        /// Throws <see cref="System.ArgumentException"/> when the condition is this group or a group that contains this group, or when
        /// the condition, or one of its nested conditions, uses a variable from another graph.
        /// </remarks>
        void Insert(int index, ICondition condition);

        /// <summary>
        /// Adds an existing condition to this group.
        /// </summary>
        /// <param name="condition">The condition to add.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <paramref name="condition"/> is this group or a group that contains this group, or when it is, or
        /// contains, a variable condition whose variable belongs to another graph.
        /// </exception>
        /// <remarks>
        /// If the condition already belongs to another group, it is first removed from that group. The
        /// condition is appended at the end of this group.
        /// Throws <see cref="System.ArgumentException"/> when the condition is this group or a group that contains this group, or when
        /// the condition, or one of its nested conditions, uses a variable from another graph.
        /// </remarks>
        void Add(ICondition condition);

        /// <summary>
        /// Removes a condition from this group.
        /// </summary>
        /// <param name="condition">The condition to remove. Must be a direct child of this group.</param>
        void Remove(ICondition condition);

        /// <summary>
        /// Removes all conditions from this group.
        /// </summary>
        void Clear();
    }
}
