// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor;

/// <summary>
/// Base class for partitioning bounding boxes.
/// </summary>
/// <typeparam name="TElementKey">The type of a unique key identifying an element whose bounding box is partitioned.</typeparam>
abstract class BaseBoundingBoxSpacePartitioning<TElementKey>
    where TElementKey : IEquatable<TElementKey>
{
    /// <summary>
    /// Struct representing an partitioned element.
    /// </summary>
    public readonly struct BoundingBoxElement
    {
        /// <summary>
        /// The unique key identifying the partitioned element.
        /// </summary>
        public readonly TElementKey Key;

        /// <summary>
        /// The bounding box of the partitioned element.
        /// </summary>
        public readonly Rect BoundingBox;

        /// <summary>
        /// Creates a <see cref="BoundingBoxElement"/>.
        /// </summary>
        /// <param name="key">The unique key identifying the partitioned element.</param>
        /// <param name="boundingBox">The bounding box of the partitioned element.</param>
        public BoundingBoxElement(TElementKey key, Rect boundingBox)
        {
            Key = key;
            BoundingBox = boundingBox;
        }
    }

    /// <summary>
    /// Hashset of keys of the partitioned elements.
    /// </summary>
    protected HashSet<TElementKey> m_PartitionedElements = new();

    /// <summary>
    /// Returns true if the space partitioning is empty.
    /// </summary>
    public virtual bool Empty => m_PartitionedElements.Count == 0;

    /// <summary>
    /// Returns the number of partitioned elements.
    /// </summary>
    public virtual int Count => m_PartitionedElements.Count;

    /// <summary>
    /// Clears the space partitioning.
    /// </summary>
    public virtual void Clear()
    {
        m_PartitionedElements.Clear();
    }

    /// <summary>
    /// Adds or updates elements in the space partitioning.
    /// </summary>
    /// <param name="elements">A collection of <see cref="BoundingBoxElement"/> to add or update in the space partitioning.</param>
    public virtual void AddOrUpdateElements(IReadOnlyCollection<BoundingBoxElement> elements)
    {
        AddPartitionedElements(elements);
    }

    /// <summary>
    /// Removes elements from the space partitioning.
    /// </summary>
    /// <param name="elements">A collection of keys to remove from the space partitioning.</param>
    public virtual void RemoveElements(IReadOnlyCollection<TElementKey> elements)
    {
        RemovePartitionedElements(elements);
    }

    /// <summary>
    /// Gets all elements that are within a specified region. If <paramref name="allowOverlap"/> is true,
    /// any element that has its bounding box overlapping with the region is returned, otherwise only the ones
    /// where the bounding box is completely inside the region are returned.
    /// </summary>
    /// <param name="region">The region where to look for elements.</param>
    /// <param name="allowOverlap">True for allowing overlapping bounding box, false to look for bounding boxes that are completely inside the region.</param>
    /// <returns>A collection of keys of the elements found in the region.</returns>
    public abstract IReadOnlyCollection<TElementKey> GetElementsInRegion(Rect region, bool allowOverlap);

    /// <summary>
    /// Gets all elements that are within a specified region. If <paramref name="allowOverlap"/> is true,
    /// any element that has its bounding box overlapping with the region is returned, otherwise only the ones
    /// where the bounding box is completely inside the region are returned.
    /// </summary>
    /// <param name="region">The region where to look for elements.</param>
    /// <param name="allowOverlap">True for allowing overlapping bounding box, false to look for bounding boxes that are completely inside the region.</param>
    /// <param name="outCollection">The collection in which to put elements found in the region.</param>
    public abstract void GetElementsInRegion(Rect region, bool allowOverlap, ICollection<TElementKey> outCollection);

    /// <summary>
    /// Gets all elements that are at a specific position. An element is considered at a position if its bounding box contains
    /// the position.
    /// </summary>
    /// <param name="position">The position where to search for elements.</param>
    /// <returns>A collection of keys of the elements found at the position.</returns>
    public abstract IReadOnlyCollection<TElementKey> GetElementsAtPosition(Vector2 position);

    /// <summary>
    /// Gets all elements that are at a specific position. An element is considered at a position if its bounding box contains
    /// the position.
    /// </summary>
    /// <param name="position">The position where to search for elements.</param>
    /// <param name="outCollection">The collection in which to put elements found at the position.</param>
    public abstract void GetElementsAtPosition(Vector2 position, ICollection<TElementKey> outCollection);

    /// <summary>
    /// Returns true if an element is partitioned, false otherwise.
    /// </summary>
    /// <param name="element">The key identifying the element.</param>
    /// <returns>True if the element is partitioned, false otherwise</returns>
    public bool IsElementPartitioned(TElementKey element)
    {
        return m_PartitionedElements.Contains(element);
    }

    /// <summary>
    /// Adds a collection of element keys to the set of partitioned elements.
    /// </summary>
    /// <param name="elements">The collection of keys.</param>
    protected void AddPartitionedElements(IEnumerable<TElementKey> elements)
    {
        m_PartitionedElements.UnionWith(elements);
    }

    /// <summary>
    /// Adds a collection of <see cref="BoundingBoxElement"/> to the set of partitioned elements.
    /// </summary>
    /// <param name="elements">The collection of <see cref="BoundingBoxElement"/>.</param>
    protected void AddPartitionedElements(IEnumerable<BoundingBoxElement> elements)
    {
        foreach (var boundingBoxElement in elements)
            m_PartitionedElements.Add(boundingBoxElement.Key);
    }

    /// <summary>
    /// Removes a collection of keys from the set of partitioned elements.
    /// </summary>
    /// <param name="elements">The collection of keys.</param>
    protected void RemovePartitionedElements(IEnumerable<TElementKey> elements)
    {
        m_PartitionedElements.ExceptWith(elements);
    }

    /// <summary>
    /// Removes a collection of keys from the set of partitioned elements.
    /// </summary>
    /// <param name="elements">The collection of keys.</param>
    protected void RemovePartitionedElements(IEnumerable<BoundingBoxElement> elements)
    {
        foreach (var boundingBoxElement in elements)
            m_PartitionedElements.Remove(boundingBoxElement.Key);
    }

    /// <summary>
    /// Converts a collection of <see cref="BoundingBoxElement"/> to a hashset of <typeparamref name="TElementKey"/>.
    /// </summary>
    /// <param name="elements">The collection of <see cref="BoundingBoxElement"/>.</param>
    /// <returns>A hashset of keys.</returns>
    protected static HashSet<TElementKey> ToElementKeysHashSet(IReadOnlyCollection<BoundingBoxElement> elements)
    {
        var elementKeys = new HashSet<TElementKey>();
        foreach (var element in elements)
            elementKeys.Add(element.Key);
        return elementKeys;
    }

    /// <summary>
    /// Converts a collection of <see cref="BoundingBoxElement"/> to a hashset of <typeparamref name="TElementKey"/>.
    /// </summary>
    /// <param name="elements">The collection of <see cref="BoundingBoxElement"/>.</param>
    /// <returns>A hashset of keys.</returns>
    protected static HashSet<TElementKey> ToElementKeysHashSet(ReadOnlySpan<BoundingBoxElement> elements)
    {
        var elementKeys = new HashSet<TElementKey>();
        foreach (var element in elements)
            elementKeys.Add(element.Key);
        return elementKeys;
    }

    /// <summary>
    /// Converts a collection of <typeparamref name="TElementKey"/> to a hashset of <typeparamref name="TElementKey"/>.
    /// </summary>
    /// <param name="elementKeys">A collection of <typeparamref name="TElementKey"/>.</param>
    /// <returns>A hashset of keys.</returns>
    protected static HashSet<TElementKey> ToElementKeysHashSet(IReadOnlyCollection<TElementKey> elementKeys)
    {
        if (elementKeys is HashSet<TElementKey> hashSet)
            return hashSet;
        hashSet = new HashSet<TElementKey>();
        foreach (var key in elementKeys)
            hashSet.Add(key);
        return hashSet;
    }
}
