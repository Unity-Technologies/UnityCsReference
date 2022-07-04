// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.Properties
{
    /// <summary>
    /// Interface used to receive a visitation callbacks for a specific type.
    /// </summary>
    public interface ITypeVisitor
    {
        /// <summary>
        /// Implement this method to accept visitation for container type.
        /// </summary>
        /// <typeparam name="TContainer">The container type.</typeparam>
        void Visit<TContainer>();
    }

    /// <summary>
    /// Interface used to receive a visitation callbacks for property bags.
    /// </summary>
    public interface IPropertyBagVisitor
    {
        /// <summary>
        /// Implement this method to accept visitation for a property bag and container.
        /// </summary>
        /// <remarks>
        /// This method is invoked by <see cref="IPropertyBag{TContainer}.Accept(IPropertyBagVisitor,ref TContainer)"/>.
        /// </remarks>
        /// <param name="properties">The properties of the container.</param>
        /// <param name="container">The container being visited.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        void Visit<TContainer>(IPropertyBag<TContainer> properties, ref TContainer container);
    }

    /// <summary>
    /// Interface for visiting property bags.
    /// </summary>
    public interface ICollectionPropertyBagVisitor
    {
        /// <summary>
        /// Implement this method to accept visitation for a collection of properties.
        /// </summary>
        /// <remarks>
        /// This method is invoked by <see cref="ICollectionPropertyBagAccept{TContainer}.Accept(ICollectionPropertyBagVisitor,ref TContainer)"/>.
        /// </remarks>
        /// <param name="properties">The properties of the container.</param>
        /// <param name="container">The container being visited.</param>
        /// <typeparam name="TCollection">The list type.</typeparam>
        /// <typeparam name="TElement">The element type.</typeparam>
        void Visit<TCollection, TElement>(ICollectionPropertyBag<TCollection, TElement> properties, ref TCollection container)
            where TCollection : ICollection<TElement>;
    }

    /// <summary>
    /// Interface for visiting property bags.
    /// </summary>
    public interface IListPropertyBagVisitor
    {
        /// <summary>
        /// Implement this method to accept visitation for a collection of properties.
        /// </summary>
        /// <remarks>
        /// This method is invoked by <see cref="IListPropertyBagAccept{TContainer}.Accept(IListPropertyBagVisitor,ref TContainer)"/>.
        /// </remarks>
        /// <param name="properties">The properties of the container.</param>
        /// <param name="container">The container being visited.</param>
        /// <typeparam name="TList">The list type.</typeparam>
        /// <typeparam name="TElement">The element type.</typeparam>
        void Visit<TList, TElement>(IListPropertyBag<TList, TElement> properties, ref TList container)
            where TList : IList<TElement>;
    }

    /// <summary>
    /// Interface for visiting property bags.
    /// </summary>
    public interface ISetPropertyBagVisitor
    {
        /// <summary>
        /// Implement this method to accept visitation for a collection of properties.
        /// </summary>
        /// <remarks>
        /// This method is invoked by <see cref="ISetPropertyBagAccept{TContainer}.Accept(ISetPropertyBagVisitor,ref TContainer)"/>.
        /// </remarks>
        /// <param name="properties">The properties of the container.</param>
        /// <param name="container">The container being visited.</param>
        /// <typeparam name="TSet">The set type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        void Visit<TSet, TValue>(ISetPropertyBag<TSet, TValue> properties, ref TSet container)
            where TSet : ISet<TValue>;
    }

    /// <summary>
    /// Interface for visiting property bags.
    /// </summary>
    public interface IDictionaryPropertyBagVisitor
    {
        /// <summary>
        /// Implement this method to accept visitation for a collection of properties.
        /// </summary>
        /// <remarks>
        /// This method is invoked by <see cref="IDictionaryPropertyBagAccept{TContainer}"/>.
        /// </remarks>
        /// <param name="properties">The properties of the container.</param>
        /// <param name="container">The container being visited.</param>
        /// <typeparam name="TDictionary">The dictionary type.</typeparam>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        void Visit<TDictionary, TKey, TValue>(IDictionaryPropertyBag<TDictionary, TKey, TValue> properties, ref TDictionary container)
            where TDictionary : IDictionary<TKey, TValue>;
    }

    /// <summary>
    /// Interface for receiving strongly typed property callbacks.
    /// </summary>
    public interface IPropertyVisitor
    {
        /// <summary>
        /// Implement this method to accept visitation for a specific property.
        /// </summary>
        /// <remarks>
        /// This method is invoked by <see cref="IPropertyAccept{TContainer}"/>
        /// </remarks>
        /// <param name="property">The property being visited.</param>
        /// <param name="container">The container being visited.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        void Visit<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container);
    }

    /// <summary>
    /// Interface for receiving strongly typed property callbacks for collections.
    /// </summary>
    public interface ICollectionPropertyVisitor
    {
        /// <summary>
        /// Implement this method to accept visitation for a specialized collection property.
        /// </summary>
        /// <remarks>
        /// This method is invoked by <see cref="ICollectionPropertyAccept{TList}"/>
        /// </remarks>
        /// <param name="property">The property being visited.</param>
        /// <param name="container">The container being visited.</param>
        /// <param name="collection">The collection value.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <typeparam name="TCollection">The collection value type.</typeparam>
        /// <typeparam name="TElement">The collection element type.</typeparam>
        void Visit<TContainer, TCollection, TElement>(Property<TContainer, TCollection> property, ref TContainer container, ref TCollection collection)
            where TCollection : ICollection<TElement>;
    }

    /// <summary>
    /// Interface for receiving strongly typed property callbacks for lists.
    /// </summary>
    public interface IListPropertyVisitor
    {
        /// <summary>
        /// Implement this method to accept visitation for a specialized list property.
        /// </summary>
        /// <remarks>
        /// This method is invoked by <see cref="IListPropertyAccept{TList}"/>
        /// </remarks>
        /// <param name="property">The property being visited.</param>
        /// <param name="container">The container being visited.</param>
        /// <param name="list">The list value.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <typeparam name="TList">The list value type.</typeparam>
        /// <typeparam name="TElement">The collection element type.</typeparam>
        void Visit<TContainer, TList, TElement>(Property<TContainer, TList> property, ref TContainer container, ref TList list)
            where TList : IList<TElement>;
    }

    /// <summary>
    /// Interface for receiving strongly typed property callbacks for sets.
    /// </summary>
    public interface ISetPropertyVisitor
    {
        /// <summary>
        /// Implement this method to accept visitation for a specialized set property.
        /// </summary>
        /// <remarks>
        /// This method is invoked by <see cref="ISetPropertyAccept{TSet}"/>
        /// </remarks>
        /// <param name="property">The property being visited.</param>
        /// <param name="container">The container being visited.</param>
        /// <param name="set">The hash set value.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <typeparam name="TSet">The set value type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        void Visit<TContainer, TSet, TValue>(Property<TContainer, TSet> property, ref TContainer container, ref TSet set)
            where TSet : ISet<TValue>;
    }

    /// <summary>
    /// Interface for receiving strongly typed property callbacks for dictionaries.
    /// </summary>
    /// <remarks>
    /// <seealso cref="IDictionaryPropertyAccept{TList}" />
    /// </remarks>
    public interface IDictionaryPropertyVisitor
    {
        /// <summary>
        /// Implement this method to accept visitation for a specialized dictionary property.
        /// </summary>
        /// <remarks>
        /// This method is invoked by <see cref="IDictionaryPropertyAccept{TDictionary}"/>
        /// </remarks>
        /// <param name="property">The property being visited.</param>
        /// <param name="container">The container being visited.</param>
        /// <param name="dictionary">The dictionary value.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <typeparam name="TDictionary">The dictionary value type.</typeparam>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        void Visit<TContainer, TDictionary, TKey, TValue>(Property<TContainer, TDictionary> property, ref TContainer container, ref TDictionary dictionary)
            where TDictionary : IDictionary<TKey, TValue>;
    }
}
