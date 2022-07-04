// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Properties.Internal;

namespace Unity.Properties
{
    public static partial class PropertyBag
    {
        /// <summary>
        /// Registers a strongly typed <see cref="PropertyBag{TContainer}"/> for a type.
        /// </summary>
        /// <param name="propertyBag">The <see cref="PropertyBag{TContainer}"/> to register.</param>
        /// <typeparam name="TContainer">The container type this property bag describes.</typeparam>
        public static void Register<TContainer>(PropertyBag<TContainer> propertyBag)
        {
            PropertyBagStore.AddPropertyBag(propertyBag);
        }

        /// <summary>
        /// Creates and registers a <see cref="IPropertyBag{T}"/> for a built in array type.
        /// </summary>
        /// <remarks>
        /// To generate AOT paths for visitors use <seealso cref="RegisterArray{TContainer, TElement}"/> instead.
        /// </remarks>
        /// <typeparam name="TElement">The element type to register.</typeparam>
        public static void RegisterArray<TElement>()
        {
            if (PropertyBagStore.TypedStore<IPropertyBag<TElement[]>>.PropertyBag == null)
            {
                PropertyBagStore.AddPropertyBag(new ArrayPropertyBag<TElement>());
            }
        }

        /// <summary>
        /// Creates and registers a <see cref="IPropertyBag{T}"/> for a built in array type.
        /// </summary>
        /// <remarks>
        /// The container is required to provide AOT code paths for <see cref="PropertyVisitor"/>.
        /// </remarks>
        /// <typeparam name="TContainer">The container type to register.</typeparam>
        /// <typeparam name="TElement">The element type to register.</typeparam>
        public static void RegisterArray<TContainer, TElement>()
        {
            RegisterArray<TElement>();
        }

        /// <summary>
        /// Creates and registers a <see cref="IPropertyBag{T}"/> for a <see cref="List{TElement}"/> type.
        /// </summary>
        /// <remarks>
        /// To generate AOT paths for visitors use <seealso cref="RegisterList{TContainer, TElement}"/> instead.
        /// </remarks>
        /// <typeparam name="TElement">The element type to register.</typeparam>
        public static void RegisterList<TElement>()
        {
            if (PropertyBagStore.TypedStore<IPropertyBag<TElement[]>>.PropertyBag == null)
            {
                PropertyBagStore.AddPropertyBag(new ListPropertyBag<TElement>());
            }
        }

        /// <summary>
        /// Creates and registers a <see cref="IPropertyBag{T}"/> for a <see cref="List{TElement}"/> type.
        /// </summary>
        /// <remarks>
        /// The container is required to provide AOT code paths for <see cref="PropertyVisitor"/>.
        /// </remarks>
        /// <typeparam name="TContainer">The container type to register.</typeparam>
        /// <typeparam name="TElement">The element type to register.</typeparam>
        public static void RegisterList<TContainer, TElement>()
        {
            RegisterList<TElement>();
        }

        /// <summary>
        /// Creates and registers a <see cref="IPropertyBag{T}"/> for a <see cref="HashSet{TElement}"/> type.
        /// </summary>
        /// <remarks>
        /// To generate AOT paths for visitors use <seealso cref="RegisterHashSet{TContainer, TElement}"/> instead.
        /// </remarks>
        /// <typeparam name="TElement">The element type to register.</typeparam>
        public static void RegisterHashSet<TElement>()
        {
            if (PropertyBagStore.TypedStore<IPropertyBag<HashSet<TElement>>>.PropertyBag == null)
            {
                PropertyBagStore.AddPropertyBag(new HashSetPropertyBag<TElement>());
            }
        }

        /// <summary>
        /// Creates and registers a <see cref="IPropertyBag{T}"/> for a <see cref="HashSet{TElement}"/> type.
        /// </summary>
        /// <remarks>
        /// The container is required to provide AOT code paths for <see cref="PropertyVisitor"/>.
        /// </remarks>
        /// <typeparam name="TContainer">The container type to register.</typeparam>
        /// <typeparam name="TElement">The element type to register.</typeparam>
        public static void RegisterHashSet<TContainer, TElement>()
        {
            RegisterHashSet<TElement>();
        }

        /// <summary>
        /// Creates and registers a <see cref="IPropertyBag{T}"/> for a <see cref="Dictionary{TKey, TValue}"/> type.
        /// </summary>
        /// <remarks>
        /// To generate AOT paths for visitors use <seealso cref="RegisterDictionary{TContainer, TKey, TValue}"/> instead.
        /// </remarks>
        /// <typeparam name="TKey">The key type to register.</typeparam>
        /// <typeparam name="TValue">The value type to register.</typeparam>
        public static void RegisterDictionary<TKey, TValue>()
        {
            if (PropertyBagStore.TypedStore<IPropertyBag<Dictionary<TKey, TValue>>>.PropertyBag == null)
            {
                PropertyBagStore.AddPropertyBag(new DictionaryPropertyBag<TKey, TValue>());
            }
        }

        /// <summary>
        /// Creates and registers a <see cref="IPropertyBag{T}"/> for a <see cref="Dictionary{TKey, TValue}"/> type.
        /// </summary>
        /// <remarks>
        /// The container is required to provide AOT code paths for <see cref="PropertyVisitor"/>.
        /// </remarks>
        /// <typeparam name="TContainer">The container type to register.</typeparam>
        /// <typeparam name="TKey">The key type to register.</typeparam>
        /// <typeparam name="TValue">The value type to register.</typeparam>
        public static void RegisterDictionary<TContainer, TKey, TValue>()
        {
            RegisterDictionary<TKey, TValue>();
        }

        /// <summary>
        /// Creates and registers a <see cref="IPropertyBag{T}"/> for the specified <see cref="IList{T}"/> type.
        /// </summary>
        /// <remarks>
        /// To generate AOT paths for visitors use <seealso cref="RegisterIList{TContainer, TList, TElement}"/> instead.
        /// </remarks>
        /// <typeparam name="TList">The generic list type to register.</typeparam>
        /// <typeparam name="TElement">The element type to register.</typeparam>
        public static void RegisterIList<TList, TElement>()
            where TList : IList<TElement>
        {
            if (PropertyBagStore.TypedStore<IPropertyBag<TList>>.PropertyBag == null)
            {
                PropertyBagStore.AddPropertyBag(new IndexedCollectionPropertyBag<TList, TElement>());
            }
        }

        /// <summary>
        /// Creates and registers a <see cref="IPropertyBag{T}"/> for the specified <see cref="IList{T}"/> type.
        /// </summary>
        /// <remarks>
        /// The container is required to provide AOT code paths for <see cref="PropertyVisitor"/>.
        /// </remarks>
        /// <typeparam name="TContainer">The container type to register.</typeparam>
        /// <typeparam name="TList">The generic list type to register.</typeparam>
        /// <typeparam name="TElement">The element type to register.</typeparam>
        public static void RegisterIList<TContainer, TList, TElement>()
            where TList : IList<TElement>
        {
            RegisterIList<TList, TElement>();
        }

        /// <summary>
        /// Creates and registers a <see cref="IPropertyBag{T}"/> for the specified <see cref="ISet{T}"/> type.
        /// </summary>
        /// <remarks>
        /// To generate AOT paths for visitors use <seealso cref="RegisterISet{TContainer, TList, TElement}"/> instead.
        /// </remarks>
        /// <typeparam name="TSet">The generic set type to register.</typeparam>
        /// <typeparam name="TElement">The element type to register.</typeparam>
        public static void RegisterISet<TSet, TElement>()
            where TSet : ISet<TElement>
        {
            if (PropertyBagStore.TypedStore<IPropertyBag<TSet>>.PropertyBag == null)
            {
                PropertyBagStore.AddPropertyBag(new SetPropertyBagBase<TSet, TElement>());
            }
        }

        /// <summary>
        /// Creates and registers a <see cref="IPropertyBag{T}"/> for the specified <see cref="ISet{T}"/> type.
        /// </summary>
        /// <remarks>
        /// The container is required to provide AOT code paths for <see cref="PropertyVisitor"/>.
        /// </remarks>
        /// <typeparam name="TContainer">The container type to register.</typeparam>
        /// <typeparam name="TSet">The generic set type to register.</typeparam>
        /// <typeparam name="TElement">The element type to register.</typeparam>
        public static void RegisterISet<TContainer, TSet, TElement>()
            where TSet : ISet<TElement>
        {
            RegisterISet<TSet, TElement>();
        }

        /// <summary>
        /// Creates and registers a <see cref="IPropertyBag{T}"/> for the specified <see cref="IDictionary{K, V}"/> type.
        /// </summary>
        /// <remarks>
        /// To generate AOT paths for visitors use <seealso cref="RegisterIDictionary{TContainer, TDictionary, TKey, TValue}"/> instead.
        /// </remarks>
        /// <typeparam name="TDictionary">The generic dictionary type to register.</typeparam>
        /// <typeparam name="TKey">The key type to register.</typeparam>
        /// <typeparam name="TValue">The value type to register.</typeparam>
        public static void RegisterIDictionary<TDictionary, TKey, TValue>()
            where TDictionary : IDictionary<TKey, TValue>
        {
            if (PropertyBagStore.TypedStore<IPropertyBag<TDictionary>>.PropertyBag == null)
            {
                PropertyBagStore.AddPropertyBag(new KeyValueCollectionPropertyBag<TDictionary, TKey, TValue>());
                PropertyBagStore.AddPropertyBag(new KeyValuePairPropertyBag<TKey, TValue>());
            }
        }

        /// <summary>
        /// Creates and registers a <see cref="IPropertyBag{T}"/> for the specified <see cref="IDictionary{K, V}"/> type.
        /// </summary>
        /// <remarks>
        /// The container is required to provide AOT code paths for <see cref="PropertyVisitor"/>.
        /// </remarks>
        /// <typeparam name="TContainer">The container type to register.</typeparam>
        /// <typeparam name="TDictionary">The generic dictionary type to register.</typeparam>
        /// <typeparam name="TKey">The key type to register.</typeparam>
        /// <typeparam name="TValue">The value type to register.</typeparam>
        public static void RegisterIDictionary<TContainer, TDictionary, TKey, TValue>()
            where TDictionary : IDictionary<TKey, TValue>
        {
            RegisterIDictionary<TDictionary, TKey, TValue>();
        }
    }
}
