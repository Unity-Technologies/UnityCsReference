// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Properties
{
    /// <summary>
    /// Base interface for working with a collection element property.
    /// </summary>
    public interface ICollectionElementProperty
    {

    }

    /// <summary>
    /// Interface over a property representing a list element.
    /// </summary>
    public interface IListElementProperty : ICollectionElementProperty
    {
        /// <summary>
        /// The index of this property in the list.
        /// </summary>
        int Index { get; }
    }

    /// <summary>
    /// Interface over a property representing a set element.
    /// </summary>
    public interface ISetElementProperty : ICollectionElementProperty
    {
        /// <summary>
        /// The key of this property in the set.
        /// </summary>
        object ObjectKey { get; }
    }

    /// <summary>
    /// Interface over a property representing a set element.
    /// </summary>
    public interface ISetElementProperty<out TKey> : ISetElementProperty
    {
        /// <summary>
        /// The key of this property in the set.
        /// </summary>
        TKey Key { get; }
    }

    /// <summary>
    /// Interface over a property representing a untyped dictionary element.
    /// </summary>
    public interface IDictionaryElementProperty : ICollectionElementProperty
    {
        /// <summary>
        /// The key of this property in the dictionary.
        /// </summary>
        object ObjectKey { get; }
    }

    /// <summary>
    /// Interface over a property representing a typed dictionary element.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    public interface IDictionaryElementProperty<out TKey> : IDictionaryElementProperty
    {
        /// <summary>
        /// The key of this property in the dictionary.
        /// </summary>
        TKey Key { get; }
    }
}
