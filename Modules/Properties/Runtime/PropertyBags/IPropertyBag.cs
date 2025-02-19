// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.Properties
{
    /// <summary>
    /// This interface provides access to an <see cref="IProperty{TContainer}"/> of a <see cref="IPropertyBag{TContainer}"/> by index.
    /// </summary>
    /// <typeparam name="TContainer">The container type to access.</typeparam>
    public interface IIndexedProperties<TContainer>
    {
        /// <summary>
        /// Gets the property associated with the specified index.
        /// </summary>
        /// <param name="container">The container hosting the data.</param>
        /// <param name="index">The index of the property to get.</param>
        /// <param name="property">When this method returns, contains the property associated with the specified index, if the name is found; otherwise, null.</param>
        /// <returns><see langword="true"/> if the <see cref="IIndexedProperties{TContainer}"/> contains a property for the specified index; otherwise, <see langword="false"/>.</returns>
        bool TryGetProperty(ref TContainer container, int index, out IProperty<TContainer> property);
    }

    /// <summary>
    /// This interface provides access to an <see cref="IProperty{TContainer}"/> of a <see cref="IPropertyBag{TContainer}"/> by name.
    /// </summary>
    /// <typeparam name="TContainer">The container type to access.</typeparam>
    public interface INamedProperties<TContainer>
    {
        /// <summary>
        /// Gets the property associated with the specified name.
        /// </summary>
        /// <param name="container">The container hosting the data.</param>
        /// <param name="name">The name of the property to get.</param>
        /// <param name="property">When this method returns, contains the property associated with the specified name, if the name is found; otherwise, null.</param>
        /// <returns><see langword="true"/> if the <see cref="INamedProperties{TContainer}"/> contains a property with the specified name; otherwise, <see langword="false"/>.</returns>
        bool TryGetProperty(ref TContainer container, string name, out IProperty<TContainer> property);
    }

    /// <summary>
    /// This interface provides access to an <see cref="IProperty{TContainer}"/> of a <see cref="IPropertyBag{TContainer}"/> by a key.
    /// </summary>
    /// <typeparam name="TContainer">The container type to access.</typeparam>
    /// <typeparam name="TKey">The key type to access the property with.</typeparam>
    public interface IKeyedProperties<TContainer, TKey>
    {
        /// <summary>
        /// Gets the property associated with the specified name.
        /// </summary>
        /// <param name="container">The container hosting the data.</param>
        /// <param name="key">The key to lookup.</param>
        /// <param name="property">When this method returns, contains the property associated with the specified name, if the name is found; otherwise, null.</param>
        /// <returns><see langword="true"/> if the <see cref="INamedProperties{TContainer}"/> contains a property with the specified name; otherwise, <see langword="false"/>.</returns>
        bool TryGetProperty(ref TContainer container, TKey key, out IProperty<TContainer> property);
    }

    /// <summary>
    /// Base untyped interface for implementing property bags.
    /// </summary>
    public interface IPropertyBag
    {
        /// <summary>
        /// Call this method to invoke <see cref="ITypeVisitor.Visit{TContainer}"/> with the strongly typed container type.
        /// </summary>
        /// <param name="visitor">The visitor being run.</param>
        void Accept(ITypeVisitor visitor);

        /// <summary>
        /// Call this method to invoke <see cref="IPropertyBagVisitor.Visit{TContainer}"/> with the strongly typed container for the given <paramref name="container"/> object.
        /// </summary>
        /// <param name="visitor">The visitor to invoke the visit callback on.</param>
        /// <param name="container">The container being visited.</param>
        void Accept(IPropertyBagVisitor visitor, ref object container);
    }

    /// <summary>
    /// Base typed interface for implementing property bags.
    /// </summary>
    public interface IPropertyBag<TContainer> : IPropertyBag
    {
        /// <summary>
        /// Returns an enumerator that iterates through all static properties for the type.
        /// </summary>
        /// <remarks>
        /// This should return a subset properties returned by <see cref="GetProperties(ref TContainer)"/>.
        /// </remarks>
        /// <returns>A <see cref="IEnumerator{IProperty}"/> structure for all properties.</returns>
        PropertyCollection<TContainer> GetProperties();

        /// <summary>
        /// Returns an enumerator that iterates through all static and dynamic properties for the given container.
        /// </summary>
        /// <remarks>
        /// This should return all static properties returned by <see cref="GetProperties()"/> in addition to any dynamic properties.
        /// If the container is a collection type all elements will be iterated.
        /// </remarks>
        /// <param name="container">The container hosting the data.</param>
        /// <returns>A <see cref="IEnumerator{IProperty}"/> structure for all properties.</returns>
        PropertyCollection<TContainer> GetProperties(ref TContainer container);

        /// <summary>
        /// Creates and returns a new instance of <typeparamref name="TContainer"/>.
        /// </summary>
        /// <returns>A new instance of <typeparamref name="TContainer"/>.</returns>
        TContainer CreateInstance();

        /// <summary>
        /// Tries to create a new instance of <typeparamref name="TContainer"/>.
        /// </summary>
        /// <param name="instance">When this method returns, contains the created instance, if type construction succeeded; otherwise, the default value for <typeparamref name="TContainer"/>.</param>
        /// <returns><see langword="true"/> if a new instance of type <typeparamref name="TContainer"/> was created; otherwise, <see langword="false"/>.</returns>
        bool TryCreateInstance(out TContainer instance);

        /// <summary>
        /// Call this method to invoke <see cref="IPropertyBagVisitor.Visit{TContainer}"/> with the strongly typed container.
        /// </summary>
        /// <param name="visitor">The visitor being run.</param>
        /// <param name="container">The container being visited.</param>
        void Accept(IPropertyBagVisitor visitor, ref TContainer container);
    }

    /// <summary>
    /// Base untyped interface for implementing collection based property bags.
    /// </summary>
    public interface ICollectionPropertyBag<TCollection, TElement> : IPropertyBag<TCollection>, ICollectionPropertyBagAccept<TCollection>
        where TCollection : ICollection<TElement>
    {
    }

    /// <summary>
    /// Base typed interface for implementing list based property bags.
    /// </summary>
    public interface IListPropertyBag<TList, TElement> : ICollectionPropertyBag<TList, TElement>, IListPropertyBagAccept<TList>, IListPropertyAccept<TList>, IIndexedProperties<TList>
        where TList : IList<TElement>
    {
    }

    /// <summary>
    /// Base typed interface for implementing set based property bags.
    /// </summary>
    public interface ISetPropertyBag<TSet, TElement> : ICollectionPropertyBag<TSet, TElement>, ISetPropertyBagAccept<TSet>, ISetPropertyAccept<TSet>, IKeyedProperties<TSet, object>
        where TSet : ISet<TElement>
    {
    }

    /// <summary>
    /// Base typed interface for implementing dictionary based property bags.
    /// </summary>
    public interface IDictionaryPropertyBag<TDictionary, TKey, TValue> : ICollectionPropertyBag<TDictionary, KeyValuePair<TKey, TValue>>, IDictionaryPropertyBagAccept<TDictionary>, IDictionaryPropertyAccept<TDictionary>, IKeyedProperties<TDictionary, object>
        where TDictionary : IDictionary<TKey, TValue>
    {
    }
}
