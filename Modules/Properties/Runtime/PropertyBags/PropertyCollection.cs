// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.Properties
{
    /// <summary>
    /// The <see cref="PropertyCollection{TContainer}"/> struct provides enumerable access to all <see cref="IProperty{TContainer}"/> for a given <see cref="PropertyBag{TContainer}"/>.
    /// </summary>
    /// <typeparam name="TContainer">The container type which this collection exposes properties for.</typeparam>
    public readonly struct PropertyCollection<TContainer> : IEnumerable<IProperty<TContainer>>
    {
        enum EnumeratorType
        {
            Empty,
            Enumerable,
            List,
            IndexedCollectionPropertyBag
        }

        /// <summary>
        /// An enumerator struct to enumerate all properties for the given <typeparamref name="TContainer"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<IProperty<TContainer>>
        {
            readonly EnumeratorType m_Type;

            IEnumerator<IProperty<TContainer>> m_Enumerator;
            List<IProperty<TContainer>>.Enumerator m_Properties;
            IndexedCollectionPropertyBagEnumerator<TContainer> m_IndexedCollectionPropertyBag;

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>The element in the collection at the current position of the enumerator.</returns>
            public IProperty<TContainer> Current { get; private set; }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>The element in the collection at the current position of the enumerator.</returns>
            object IEnumerator.Current => Current;

            /// <summary>
            /// Passthrough enumerator.
            /// </summary>
            /// <param name="enumerator"></param>
            internal Enumerator(IEnumerator<IProperty<TContainer>> enumerator)
            {
                m_Type = EnumeratorType.Enumerable;
                m_Enumerator = enumerator;
                m_Properties = default;
                m_IndexedCollectionPropertyBag = default;
                Current = default;
            }

            internal Enumerator(List<IProperty<TContainer>>.Enumerator properties)
            {
                m_Type = EnumeratorType.List;
                m_Enumerator = default;
                m_Properties = properties;
                m_IndexedCollectionPropertyBag = default;
                Current = default;
            }

            internal Enumerator(IndexedCollectionPropertyBagEnumerator<TContainer> enumerator)
            {
                m_Type = EnumeratorType.IndexedCollectionPropertyBag;
                m_Enumerator = default;
                m_Properties = default;
                m_IndexedCollectionPropertyBag = enumerator;
                Current = default;
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
            public bool MoveNext()
            {
                bool result;

                switch (m_Type)
                {
                    case EnumeratorType.Empty:
                        return false;
                    case EnumeratorType.Enumerable:
                        result = m_Enumerator.MoveNext();
                        Current = m_Enumerator.Current;
                        break;
                    case EnumeratorType.List:
                        result = m_Properties.MoveNext();
                        Current = m_Properties.Current;
                        break;
                    case EnumeratorType.IndexedCollectionPropertyBag:
                        result = m_IndexedCollectionPropertyBag.MoveNext();
                        Current = m_IndexedCollectionPropertyBag.Current;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return result;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            public void Reset()
            {
                switch (m_Type)
                {
                    case EnumeratorType.Empty:
                        break;
                    case EnumeratorType.Enumerable:
                        m_Enumerator.Reset();
                        break;
                    case EnumeratorType.List:
                        ((IEnumerator) m_Properties).Reset();
                        break;
                    case EnumeratorType.IndexedCollectionPropertyBag:
                        m_IndexedCollectionPropertyBag.Reset();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            /// <summary>
            /// Disposes of the underlying enumerator.
            /// </summary>
            public void Dispose()
            {
                switch (m_Type)
                {
                    case EnumeratorType.Empty:
                        break;
                    case EnumeratorType.Enumerable:
                        m_Enumerator.Dispose();
                        break;
                    case EnumeratorType.List:
                        // If we try to invoke the dispose call here we incur a boxing cost.
                        // Fortunately List<T>.Enumerator has no dispose implementation.
                        break;
                    case EnumeratorType.IndexedCollectionPropertyBag:
                        m_IndexedCollectionPropertyBag.Dispose();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        readonly EnumeratorType m_Type;
        readonly IEnumerable<IProperty<TContainer>> m_Enumerable;
        readonly List<IProperty<TContainer>> m_Properties;
        readonly IndexedCollectionPropertyBagEnumerable<TContainer> m_IndexedCollectionPropertyBag;

        /// <summary>
        /// Returns an empty collection of properties.
        /// </summary>
        public static PropertyCollection<TContainer> Empty { get; } = new PropertyCollection<TContainer>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyCollection{TContainer}"/> struct which wraps the given enumerable.
        /// </summary>
        /// <param name="enumerable">An <see cref="IEnumerable"/> of properties to wrap.</param>
        public PropertyCollection(IEnumerable<IProperty<TContainer>> enumerable)
        {
            m_Type = EnumeratorType.Enumerable;
            m_Enumerable = enumerable;
            m_Properties = null;
            m_IndexedCollectionPropertyBag = default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyCollection{TContainer}"/> struct which wraps the given properties list.
        /// </summary>
        /// <param name="properties">A list of properties to wrap.</param>
        public PropertyCollection(List<IProperty<TContainer>> properties)
        {
            m_Type = EnumeratorType.List;
            m_Enumerable = null;
            m_Properties = properties;
            m_IndexedCollectionPropertyBag = default;
        }

        internal PropertyCollection(IndexedCollectionPropertyBagEnumerable<TContainer> enumerable)
        {
            m_Type = EnumeratorType.IndexedCollectionPropertyBag;
            m_Enumerable = null;
            m_Properties = null;
            m_IndexedCollectionPropertyBag = enumerable;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public Enumerator GetEnumerator()
        {
            switch (m_Type)
            {
                case EnumeratorType.Empty:
                    return default;
                case EnumeratorType.Enumerable:
                    return new Enumerator(m_Enumerable.GetEnumerator());
                case EnumeratorType.List:
                    return new Enumerator(m_Properties.GetEnumerator());
                case EnumeratorType.IndexedCollectionPropertyBag:
                    return new Enumerator(m_IndexedCollectionPropertyBag.GetEnumerator());
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc/>
        IEnumerator<IProperty<TContainer>> IEnumerable<IProperty<TContainer>>.GetEnumerator()
            => GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
