// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Jobs;

namespace Unity.Properties.Internal
{
    interface IPropertyBagRegister
    {
        /// <summary>
        /// Statically registers the property bag through the <see cref="PropertyBagStore"/>.
        /// </summary>
        void Register();
    }

    /// <summary>
    /// Static class used to store all property bags. This is an internal class.
    /// </summary>
    /// <remarks>
    /// This storage is used to resolve <see cref="IPropertyBag{TContainer}"/> types by internal properties algorithms.
    /// </remarks>
    static class PropertyBagStore
    {
        internal struct TypedStore<TContainer>
        {
            public static IPropertyBag<TContainer> PropertyBag;
        }

        static readonly ConcurrentDictionary<Type, IPropertyBag> s_PropertyBags = new ConcurrentDictionary<Type, IPropertyBag>();
        static readonly List<Type> s_RegisteredTypes = new List<Type>();

        /// <summary>
        /// Instance of the dynamic property bag provider. This is used to allow an external assembly to generate property bags for us.
        /// </summary>
        static ReflectedPropertyBagProvider s_PropertyBagProvider = null;

        // The reflected property bag provider is created inside of a job to lessen the impact on domain reload.
        // If it is queried before the job finishes, we go through the slower path.
        static ReflectedPropertyBagProvider ReflectedPropertyBagProvider => s_PropertyBagProvider ??= new ReflectedPropertyBagProvider();

        internal static List<Type> AllTypes => s_RegisteredTypes;

        internal static void CreatePropertyBagProvider()
        {
            s_PropertyBagProvider = new ReflectedPropertyBagProvider();
        }

        /// <summary>
        /// Adds a <see cref="ContainerPropertyBag{TContainer}"/> to the store.
        /// </summary>
        /// <param name="propertyBag">The <see cref="ContainerPropertyBag{TContainer}"/> to add.</param>
        /// <typeparam name="TContainer">The container type this <see cref="ContainerPropertyBag{TContainer}"/> describes.</typeparam>
        internal static void AddPropertyBag<TContainer>(IPropertyBag<TContainer> propertyBag)
        {
            if (!TypeTraits<TContainer>.IsContainer)
            {
                throw new Exception($"PropertyBagStore Type=[{typeof(TContainer)}] is not a valid container type. Type can not be primitive, enum or string.");
            }

            if (TypeTraits<TContainer>.IsAbstractOrInterface)
            {
                throw new Exception($"PropertyBagStore Type=[{typeof(TContainer)}] is not a valid container type. Type can not be abstract or interface.");
            }

            if (null != TypedStore<TContainer>.PropertyBag)
            {
                var currentPropertyBag = TypedStore<TContainer>.PropertyBag;
                // When registering a property bag, prefer to use the one coming from its own assembly.
                if (currentPropertyBag.GetType().Assembly == typeof(TContainer).Assembly)
                    return;

                if (propertyBag.GetType().GetCustomAttributes<System.Runtime.CompilerServices.CompilerGeneratedAttribute>().Any())
                {
                    // If there is already a property bag registered, only register the provided one
                    // if it is contained in the same assembly as its target type.
                    if (propertyBag.GetType().Assembly != typeof(TContainer).Assembly)
                        return;
                }
            }

            TypedStore<TContainer>.PropertyBag = propertyBag;
            if (!s_PropertyBags.ContainsKey(typeof(TContainer)))
            {
                s_RegisteredTypes.Add(typeof(TContainer));
            }

            s_PropertyBags[typeof(TContainer)] = propertyBag;
        }

        /// <summary>
        /// Gets the strongly typed <see cref="ContainerPropertyBag{TContainer}"/> for the given <typeparamref name="TContainer"/>.
        /// </summary>
        /// <typeparam name="TContainer">The container type to resolve the property bag for.</typeparam>
        /// <returns>The resolved property bag, strongly typed.</returns>
        internal static IPropertyBag<TContainer> GetPropertyBag<TContainer>()
        {
            if (null != TypedStore<TContainer>.PropertyBag)
            {
                return TypedStore<TContainer>.PropertyBag;
            }

            var untyped = GetPropertyBag(typeof(TContainer));

            if (null == untyped)
            {
                return null;
            }

            if (!(untyped is IPropertyBag<TContainer> typed))
            {
                throw new InvalidOperationException($"PropertyBag type container type mismatch.");
            }

            return typed;
        }

        /// <summary>
        /// Gets an interface to the <see cref="ContainerPropertyBag{TContainer}"/> for the given type.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="IPropertyBag"/> can be used to get the strongly typed generic using the <see cref="IPropertyBag.Accept(ITypeVisitor)"/> method.
        /// </remarks>
        /// <param name="type">The container type to resolve the property bag for.</param>
        /// <returns>The resolved property bag.</returns>
        internal static IPropertyBag GetPropertyBag(Type type)
        {
            if (s_PropertyBags.TryGetValue(type, out var propertyBag))
            {
                return propertyBag;
            }

            if (!TypeTraits.IsContainer(type))
            {
                return null;
            }

            if (type.IsArray && type.GetArrayRank() != 1)
            {
                return null;
            }

            if (type.IsInterface || type.IsAbstract)
            {
                return null;
            }

            if (type == typeof(object))
            {
                return null;
            }

            propertyBag = ReflectedPropertyBagProvider.CreatePropertyBag(type);
            if (null == propertyBag)
            {
                s_PropertyBags.TryAdd(type, null);
            }
            else
            {
                (propertyBag as IPropertyBagRegister)?.Register();
                return propertyBag;
            }

            return null;
        }

        /// <summary>
        /// Returns true if the given type has a static property bag registered.
        /// </summary>
        /// <typeparam name="TContainer"></typeparam>
        /// <returns><see langword="true"/> if the property bag exists; otherwise, <see langword="false"/>.</returns>
        internal static bool Exists<TContainer>()
        {
            return null != TypedStore<TContainer>.PropertyBag;
        }

        internal static bool Exists(Type type)
        {
            return s_PropertyBags.ContainsKey(type);
        }

        /// <summary>
        /// Returns true if the given <paramref name="value"/> type is backed by a property bag.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <typeparam name="TContainer">The container type to check.</typeparam>
        /// <returns><see langword="true"/> if the property bag exists; otherwise, <see langword="false"/>.</returns>
        internal static bool Exists<TContainer>(ref TContainer value)
        {
            if (!TypeTraits<TContainer>.CanBeNull)
            {
                return GetPropertyBag<TContainer>() != null;
            }

            // We can't reliably determine if there is a property bag.
            if (EqualityComparer<TContainer>.Default.Equals(value, default(TContainer)))
            {
                return false;
            }

            return GetPropertyBag(value.GetType()) != null;
        }

        /// <summary>
        /// Gets a property bag for the concrete type of the given value.
        /// </summary>
        /// <param name="value">The value type to retrieve a property bag for.</param>
        /// <param name="propertyBag">When this method returns, contains the property bag associated with the specified value, if the bag is found; otherwise, null.</param>
        /// <typeparam name="TValue"></typeparam>
        /// <returns><see langword="true"/> if the property bag was found for the specified value; otherwise, <see langword="false"/>.</returns>
        internal static bool TryGetPropertyBagForValue<TValue>(ref TValue value, out IPropertyBag propertyBag)
        {
            // early out for primitive types that don't have associated containers
            // note: GetPropertyBag checks for TypeTraits.IsContainerType(type) already
            if (!TypeTraits<TValue>.IsContainer)
            {
                propertyBag = null;
                return false;
            }

            // We can not recurse on a null value.
            if (TypeTraits<TValue>.CanBeNull)
            {
                if (EqualityComparer<TValue>.Default.Equals(value, default))
                {
                    propertyBag = GetPropertyBag<TValue>();
                    return null != propertyBag;
                }
            }

            if (TypeTraits<TValue>.IsValueType)
            {
                propertyBag = GetPropertyBag<TValue>();
                return null != propertyBag;
            }

            propertyBag = GetPropertyBag(value.GetType());
            return null != propertyBag;
        }
    }
}
