// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;

namespace Unity.Properties
{
    readonly struct IndexedCollectionPropertyBagEnumerable<TContainer>
    {
        readonly IIndexedCollectionPropertyBagEnumerator<TContainer> m_Impl;
        readonly TContainer m_Container;

        public IndexedCollectionPropertyBagEnumerable(IIndexedCollectionPropertyBagEnumerator<TContainer> impl, TContainer container)
        {
            m_Impl = impl;
            m_Container = container;
        }

        public IndexedCollectionPropertyBagEnumerator<TContainer> GetEnumerator()
            => new IndexedCollectionPropertyBagEnumerator<TContainer>(m_Impl, m_Container);
    }

    struct IndexedCollectionPropertyBagEnumerator<TContainer> : IEnumerator<IProperty<TContainer>>
    {
        readonly IIndexedCollectionPropertyBagEnumerator<TContainer> m_Impl;
        readonly IndexedCollectionSharedPropertyState m_Previous;

        TContainer m_Container;
        int m_Position;

        internal IndexedCollectionPropertyBagEnumerator(IIndexedCollectionPropertyBagEnumerator<TContainer> impl, TContainer container)
        {
            m_Impl = impl;
            m_Container = container;
            m_Previous = impl.GetSharedPropertyState();
            m_Position = -1;
        }

        /// <inheritdoc/>
        public IProperty<TContainer> Current => m_Impl.GetSharedProperty();

        /// <inheritdoc/>
        object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            m_Position++;

            if (m_Position < m_Impl.GetCount(ref m_Container))
            {
                m_Impl.SetSharedPropertyState(new IndexedCollectionSharedPropertyState { Index = m_Position, IsReadOnly = false });
                return true;
            }

            m_Impl.SetSharedPropertyState(m_Previous);
            return false;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            m_Position = -1;
            m_Impl.SetSharedPropertyState(m_Previous);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }

    interface IIndexedCollectionPropertyBagEnumerator<TContainer>
    {
        public int GetCount(ref TContainer container);
        public IProperty<TContainer> GetSharedProperty();
        public IndexedCollectionSharedPropertyState GetSharedPropertyState();
        public void SetSharedPropertyState(IndexedCollectionSharedPropertyState state);
    }

    struct IndexedCollectionSharedPropertyState
    {
        public int Index;
        public bool IsReadOnly;
    }

    /// <summary>
    /// An <see cref="IPropertyBag{T}"/> implementation for a generic collection of elements which can be accessed by index. This is based on the <see cref="IList{TElement}"/> interface.
    /// </summary>
    /// <typeparam name="TList">The collection type.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    public class IndexedCollectionPropertyBag<TList, TElement> : PropertyBag<TList>, IListPropertyBag<TList, TElement>, IConstructorWithCount<TList>, IIndexedCollectionPropertyBagEnumerator<TList>
        where TList : IList<TElement>
    {
        class ListElementProperty : Property<TList, TElement>, IListElementProperty
        {
            internal int m_Index;
            internal bool m_IsReadOnly;

            /// <inheritdoc/>
            public int Index => m_Index;

            /// <inheritdoc/>
            public override string Name => Index.ToString();

            /// <inheritdoc/>
            public override bool IsReadOnly => m_IsReadOnly;

            /// <inheritdoc/>
            public override TElement GetValue(ref TList container) => container[m_Index];

            /// <inheritdoc/>
            public override void SetValue(ref TList container, TElement value) => container[m_Index] = value;
        }

        /// <summary>
        /// Shared instance of a list element property. We re-use the same instance to avoid allocations.
        /// </summary>
        readonly ListElementProperty m_Property = new ListElementProperty();

        /// <inheritdoc cref="IPropertyBag{T}.GetProperties()"/>
        public override PropertyCollection<TList> GetProperties()
        {
            return PropertyCollection<TList>.Empty;
        }

        /// <inheritdoc cref="IPropertyBag{T}.GetProperties(ref T)"/>
        public override PropertyCollection<TList> GetProperties(ref TList container)
        {
            return new PropertyCollection<TList>(new IndexedCollectionPropertyBagEnumerable<TList>(this, container));
        }

        /// <summary>
        /// Gets the property associated with the specified index.
        /// </summary>
        /// <param name="container">The container hosting the data.</param>
        /// <param name="index">The index of the property to get.</param>
        /// <param name="property">When this method returns, contains the property associated with the specified index, if the name is found; otherwise, null.</param>
        /// <returns><see langword="true"/> if the <see cref="IIndexedProperties{TContainer}"/> contains a property for the specified index; otherwise, <see langword="false"/>.</returns>
        public bool TryGetProperty(ref TList container, int index, out IProperty<TList> property)
        {
            if ((uint) index >= (uint) container.Count)
            {
                property = null;
                return false;
            }

            property = new ListElementProperty
            {
                m_Index = index,
                m_IsReadOnly = false
            };

            return true;
        }

        void ICollectionPropertyBagAccept<TList>.Accept(ICollectionPropertyBagVisitor visitor, ref TList container)
        {
            visitor.Visit(this, ref container);
        }

        void IListPropertyBagAccept<TList>.Accept(IListPropertyBagVisitor visitor, ref TList list)
        {
            visitor.Visit(this, ref list);
        }

        void IListPropertyAccept<TList>.Accept<TContainer>(IListPropertyVisitor visitor, Property<TContainer, TList> property, ref TContainer container, ref TList list)
        {
            using (new AttributesScope(m_Property, property))
            {
                visitor.Visit<TContainer, TList, TElement>(property, ref container, ref list);
            }
        }

        TList IConstructorWithCount<TList>.InstantiateWithCount(int count)
        {
            return InstantiateWithCount(count);
        }

        /// <summary>
        /// Implement this method to provide custom type instantiation with a count value for the container type.
        /// </summary>
        /// <remarks>
        /// You MUST also override <see cref="InstantiationKind"/> to return <see langword="ConstructionType.PropertyBagOverride"/> for this method to be called.
        /// </remarks>
        protected virtual TList InstantiateWithCount(int count)
        {
            return default;
        }

        int IIndexedCollectionPropertyBagEnumerator<TList>.GetCount(ref TList container)
        {
            return container.Count;
        }

        IProperty<TList> IIndexedCollectionPropertyBagEnumerator<TList>.GetSharedProperty()
        {
            return m_Property;
        }

        IndexedCollectionSharedPropertyState IIndexedCollectionPropertyBagEnumerator<TList>.GetSharedPropertyState()
        {
            return new IndexedCollectionSharedPropertyState { Index = m_Property.m_Index, IsReadOnly = m_Property.IsReadOnly };
        }

        void IIndexedCollectionPropertyBagEnumerator<TList>.SetSharedPropertyState(IndexedCollectionSharedPropertyState state)
        {
            m_Property.m_Index = state.Index;
            m_Property.m_IsReadOnly = state.IsReadOnly;
        }
    }
}
