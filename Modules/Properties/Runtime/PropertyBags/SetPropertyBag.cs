// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.Properties
{
    /// <summary>
    /// A <see cref="IPropertyBag{T}"/> implementation for a generic set of elements using the <see cref="ISet{TElement}"/> interface.
    /// </summary>
    /// <typeparam name="TSet">The collection type.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    public class SetPropertyBagBase<TSet, TElement> : PropertyBag<TSet>, ISetPropertyBag<TSet, TElement>
        where TSet : ISet<TElement>
    {
        class SetElementProperty : Property<TSet, TElement>, ISetElementProperty<TElement>
        {
            internal TElement m_Value;

            public override string Name => m_Value.ToString();
            public override bool IsReadOnly => true;

            public override TElement GetValue(ref TSet container) => m_Value;
            public override void SetValue(ref TSet container, TElement value) => throw new InvalidOperationException("Property is ReadOnly.");

            public TElement Key => m_Value;
            public object ObjectKey => m_Value;
        }

        /// <summary>
        /// Shared instance of a set element property. We re-use the same instance to avoid allocations.
        /// </summary>
        readonly SetElementProperty m_Property = new SetElementProperty();

        public override PropertyCollection<TSet> GetProperties()
        {
            return PropertyCollection<TSet>.Empty;
        }

        public override PropertyCollection<TSet> GetProperties(ref TSet container)
        {
            return new PropertyCollection<TSet>(GetPropertiesEnumerable(container));
        }

        IEnumerable<IProperty<TSet>> GetPropertiesEnumerable(TSet container)
        {
            foreach (var element in container)
            {
                m_Property.m_Value = element;
                yield return m_Property;
            }
        }

        void ICollectionPropertyBagAccept<TSet>.Accept(ICollectionPropertyBagVisitor visitor, ref TSet container)
        {
            visitor.Visit(this, ref container);
        }

        void ISetPropertyBagAccept<TSet>.Accept(ISetPropertyBagVisitor visitor, ref TSet container)
        {
            visitor.Visit(this, ref container);
        }

        void ISetPropertyAccept<TSet>.Accept<TContainer>(ISetPropertyVisitor visitor, Property<TContainer, TSet> property, ref TContainer container, ref TSet dictionary)
        {
            using (new AttributesScope(m_Property, property))
            {
                visitor.Visit<TContainer, TSet, TElement>(property, ref container, ref dictionary);
            }
        }

        /// <summary>
        /// Gets the property associated with the specified name.
        /// </summary>
        /// <param name="container">The container hosting the data.</param>
        /// <param name="key">The key to lookup.</param>
        /// <param name="property">When this method returns, contains the property associated with the specified name, if the name is found; otherwise, null.</param>
        /// <returns><see langword="true"/> if the <see cref="INamedProperties{TContainer}"/> contains a property with the specified name; otherwise, <see langword="false"/>.</returns>
        public bool TryGetProperty(ref TSet container, object key, out IProperty<TSet> property)
        {
            if (container.Contains((TElement) key))
            {
                property = new SetElementProperty {m_Value = (TElement) key};
                return true;
            }

            property = default;
            return false;
        }
    }
}
