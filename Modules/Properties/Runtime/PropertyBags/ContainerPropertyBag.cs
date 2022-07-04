// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.Properties
{
    /// <summary>
    /// Base class for implementing a static property bag for a specified container type. This is an abstract class.
    /// </summary>
    /// <remarks>
    /// A <see cref="ContainerPropertyBag{TContainer}"/> is used to describe and traverse the properties for a specified <typeparamref name="TContainer"/> type.
    ///
    /// In order for properties to operate on a type, a <see cref="ContainerPropertyBag{TContainer}"/> must exist and be pre-registered for that type.
    ///
    /// _NOTE_ In editor use cases property bags can be generated dynamically through reflection. (see Unity.Properties.Reflection)
    /// </remarks>
    /// <typeparam name="TContainer">The container type.</typeparam>
    public abstract class ContainerPropertyBag<TContainer> : PropertyBag<TContainer>, INamedProperties<TContainer>
    {
        static ContainerPropertyBag()
        {
            if (!TypeTraits.IsContainer(typeof(TContainer)))
            {
                throw new InvalidOperationException($"Failed to create a property bag for Type=[{typeof(TContainer)}]. The type is not a valid container type.");
            }
        }

        readonly List<IProperty<TContainer>> m_PropertiesList = new List<IProperty<TContainer>>();
        readonly Dictionary<string, IProperty<TContainer>> m_PropertiesHash = new Dictionary<string, IProperty<TContainer>>();

        /// <summary>
        /// Adds a <see cref="Property{TContainer,TValue}"/> to the property bag.
        /// </summary>
        /// <param name="property">The <see cref="Property{TContainer,TValue}"/> to add.</param>
        /// <typeparam name="TValue">The value type for the given property.</typeparam>
        protected void AddProperty<TValue>(Property<TContainer, TValue> property)
        {
            m_PropertiesList.Add(property);
            m_PropertiesHash.Add(property.Name, property);
        }

        /// <inheritdoc/>
        public override PropertyCollection<TContainer> GetProperties()
            => new PropertyCollection<TContainer>(m_PropertiesList);

        /// <inheritdoc/>
        public override PropertyCollection<TContainer> GetProperties(ref TContainer container)
            => new PropertyCollection<TContainer>(m_PropertiesList);

        /// <summary>
        /// Gets the property associated with the specified name.
        /// </summary>
        /// <param name="container">The container hosting the data.</param>
        /// <param name="name">The name of the property to get.</param>
        /// <param name="property">When this method returns, contains the property associated with the specified name, if the name is found; otherwise, null.</param>
        /// <returns><see langword="true"/> if the <see cref="INamedProperties{TContainer}"/> contains a property with the specified name; otherwise, <see langword="false"/>.</returns>
        public bool TryGetProperty(ref TContainer container, string name, out IProperty<TContainer> property)
            => m_PropertiesHash.TryGetValue(name, out property);
    }
}
