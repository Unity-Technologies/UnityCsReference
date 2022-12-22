// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using Unity.Properties.Internal;

namespace Unity.Properties
{
    /// <summary>
    /// The <see cref="PropertyBag"/> class provides access to registered property bag instances.
    /// </summary>
    public static partial class PropertyBag
    {
        /// <summary>
        /// Gets an interface to the <see cref="PropertyBag{TContainer}"/> for the given type.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="IPropertyBag"/> can be used to get the strongly typed generic using the <see cref="IPropertyBagAccept"/> interface method.
        /// </remarks>
        /// <param name="type">The container type to resolve the property bag for.</param>
        /// <returns>The resolved property bag.</returns>
        public static IPropertyBag GetPropertyBag(Type type)
        {
            return PropertyBagStore.GetPropertyBag(type);
        }

        /// <summary>
        /// Gets the strongly typed <see cref="PropertyBag{TContainer}"/> for the given <typeparamref name="TContainer"/>.
        /// </summary>
        /// <typeparam name="TContainer">The container type to resolve the property bag for.</typeparam>
        /// <returns>The resolved property bag, strongly typed.</returns>
        public static IPropertyBag<TContainer> GetPropertyBag<TContainer>()
        {
            return PropertyBagStore.GetPropertyBag<TContainer>();
        }

        /// <summary>
        /// Gets a property bag for the concrete type of the given value.
        /// </summary>
        /// <param name="value">The value type to retrieve a property bag for.</param>
        /// <param name="propertyBag">When this method returns, contains the property bag associated with the specified value, if the bag is found; otherwise, null.</param>
        /// <typeparam name="TValue"></typeparam>
        /// <returns><see langword="true"/> if the property bag was found for the specified value; otherwise, <see langword="false"/>.</returns>
        public static bool TryGetPropertyBagForValue<TValue>(ref TValue value, out IPropertyBag propertyBag)
        {
            return PropertyBagStore.TryGetPropertyBagForValue(ref value, out propertyBag);
        }

        /// <summary>
        /// Returns true if a property bag exists for the given type.
        /// </summary>
        /// <typeparam name="TContainer">The container type to check a property bag for.</typeparam>
        /// <returns><see langword="true"/> if there is a property bag for the given type; otherwise, <see langword="false"/>.</returns>
        public static bool Exists<TContainer>()
        {
            return PropertyBagStore.Exists<TContainer>();
        }

        /// <summary>
        /// Returns true if a property bag exists for the given type.
        /// </summary>
        /// <param name="type">The type to check for a property bag</param>
        /// <returns><see langword="true"/> if there is a property bag for the given type; otherwise, <see langword="false"/>.</returns>
        public static bool Exists(Type type)
        {
            return PropertyBagStore.Exists(type);
        }

        /// <summary>
        /// Returns all the <see cref="System.Type"/> that have a registered property bag.
        /// </summary>
        /// <returns>A list of types with a registered property bag.</returns>
        public static IEnumerable<Type> GetAllTypesWithAPropertyBag()
        {
            return PropertyBagStore.AllTypes;
        }

        /// <summary>
        /// Allows for <see cref="IPropertyBag"/> to be registered on a background thread while ensuring that the jobs will be completed before
        /// the next call to <see cref="GetPropertyBag"/>.
        /// </summary>
        /// <param name="handle">The job handle to wait on.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddJobToWaitQueue(JobHandle handle)
        {
            PropertyBagStore.AddJobToWaitQueue(handle);
        }
    }

    /// <summary>
    /// Base class for implementing a property bag for a specified container type. This is an abstract class.
    /// </summary>
    /// <remarks>
    /// This is used as the base class internally and should NOT be extended.
    ///
    /// When implementing custom property bags use:
    /// * <seealso cref="ContainerPropertyBag{TContainer}"/>.
    /// * <seealso cref="IndexedCollectionPropertyBag{TContainer,TValue}"/>.
    /// </remarks>
    /// <typeparam name="TContainer">The container type.</typeparam>
    public abstract class PropertyBag<TContainer> : IPropertyBag<TContainer>, IPropertyBagRegister, IConstructor<TContainer>
    {
        static PropertyBag()
        {
            if (!TypeTraits.IsContainer(typeof(TContainer)))
            {
                throw new InvalidOperationException($"Failed to create a property bag for Type=[{typeof(TContainer)}]. The type is not a valid container type.");
            }
        }

        /// <inheritdoc/>
        void IPropertyBagRegister.Register()
        {
            PropertyBagStore.AddPropertyBag(this);
        }

        /// <summary>
        /// Accepts visitation from a specified <see cref="ITypeVisitor"/>.
        /// </summary>
        /// <param name="visitor">The visitor handling visitation.</param>
        /// <exception cref="ArgumentNullException">The visitor is null.</exception>
        public void Accept(ITypeVisitor visitor)
        {
            if (null == visitor)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.Visit<TContainer>();
        }

        /// <summary>
        /// Accepts visitation from a specified <see cref="IPropertyBagVisitor"/> using an object as the container.
        /// </summary>
        /// <param name="visitor">The visitor handling the visitation.</param>
        /// <param name="container">The container being visited.</param>
        /// <exception cref="ArgumentNullException">The container is null.</exception>
        /// <exception cref="InvalidCastException">The container type does not match the property bag type.</exception>
        void IPropertyBag.Accept(IPropertyBagVisitor visitor, ref object container)
        {
            if (null == container)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (!(container is TContainer typedContainer))
            {
                throw new ArgumentException($"The given ContainerType=[{container.GetType()}] does not match the PropertyBagType=[{typeof(TContainer)}]");
            }

            PropertyBag.AcceptWithSpecializedVisitor(this, visitor, ref typedContainer);

            container = typedContainer;
        }

        /// <summary>
        /// Accepts visitation from a specified <see cref="IPropertyBagVisitor"/> using a strongly typed container.
        /// </summary>
        /// <param name="visitor">The visitor handling the visitation.</param>
        /// <param name="container">The container being visited.</param>
        void IPropertyBag<TContainer>.Accept(IPropertyBagVisitor visitor, ref TContainer container)
        {
            visitor.Visit(this, ref container);
        }

        /// <inheritdoc/>
        PropertyCollection<TContainer> IPropertyBag<TContainer>.GetProperties()
        {
            return GetProperties();
        }

        /// <inheritdoc/>
        PropertyCollection<TContainer> IPropertyBag<TContainer>.GetProperties(ref TContainer container)
        {
            return GetProperties(ref container);
        }

        /// <inheritdoc/>
        InstantiationKind IConstructor.InstantiationKind => InstantiationKind;

        /// <inheritdoc/>
        TContainer IConstructor<TContainer>.Instantiate()
        {
            return Instantiate();
        }

        /// <summary>
        /// Implement this method to return a <see cref="PropertyCollection{TContainer}"/> that can enumerate through all properties for the <typeparamref name="TContainer"/>.
        /// </summary>
        /// <returns>A <see cref="PropertyCollection{TContainer}"/> structure which can enumerate each property.</returns>
        public abstract PropertyCollection<TContainer> GetProperties();

        /// <summary>
        /// Implement this method to return a <see cref="PropertyCollection{TContainer}"/> that can enumerate through all properties for the <typeparamref name="TContainer"/>.
        /// </summary>
        /// <param name="container">The container hosting the data.</param>
        /// <returns>A <see cref="PropertyCollection{TContainer}"/> structure which can enumerate each property.</returns>
        public abstract PropertyCollection<TContainer> GetProperties(ref TContainer container);

        /// <summary>
        /// Implement this property and return true to provide custom type instantiation for the container type.
        /// </summary>
        protected virtual InstantiationKind InstantiationKind { get; } = InstantiationKind.Activator;

        /// <summary>
        /// Implement this method to provide custom type instantiation for the container type.
        /// </summary>
        /// <remarks>
        /// You MUST also override <see cref="InstantiationKind"/> to return <see langword="ConstructionType.PropertyBagOverride"/> for this method to be called.
        /// </remarks>
        protected virtual TContainer Instantiate()
        {
            return default;
        }

        /// <summary>
        /// Creates and returns a new instance of <see cref="TContainer"/>.
        /// </summary>
        /// <returns>A new instance of <see cref="TContainer"/>.</returns>
        public TContainer CreateInstance() => TypeUtility.Instantiate<TContainer>();

        /// <summary>
        /// Tries to create a new instance of <see cref="TContainer"/>.
        /// </summary>
        /// <param name="instance">When this method returns, contains the created instance, if type instantiation succeeded; otherwise, the default value for <typeparamref name="TContainer"/>.</param>
        /// <returns><see langword="true"/> if a new instance of type <see cref="TContainer"/> was created; otherwise, <see langword="false"/>.</returns>
        public bool TryCreateInstance(out TContainer instance) => TypeUtility.TryInstantiate(out instance);
    }
}
