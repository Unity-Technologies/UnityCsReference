// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;

namespace Unity.Properties
{
    /// <summary>
    /// An <see cref="IPropertyBag{T}"/> implementation for a generic collection of key/value pairs using the <see cref="IDictionary{TKey, TValue}"/> interface.
    /// </summary>
    /// <typeparam name="TDictionary">The key/value collection type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    public class KeyValueCollectionPropertyBag<TDictionary, TKey, TValue> : PropertyBag<TDictionary>, IDictionaryPropertyBag<TDictionary, TKey, TValue>
        where TDictionary : IDictionary<TKey, TValue>
    {
        class KeyValuePairProperty : Property<TDictionary, KeyValuePair<TKey, TValue>>, IDictionaryElementProperty<TKey>
        {
            public override string Name => Key.ToString();
            public override bool IsReadOnly => false;

            public override KeyValuePair<TKey, TValue> GetValue(ref TDictionary container)
            {
                return new KeyValuePair<TKey, TValue>(Key, container[Key]);
            }

            public override void SetValue(ref TDictionary container, KeyValuePair<TKey, TValue> value)
            {
                container[value.Key] = value.Value;
            }

            public TKey Key { get; internal set; }
            public object ObjectKey => Key;
        }

        /// <summary>
        /// Collection used to dynamically return the same instance pointing to a different <see cref="KeyValuePair{TKey,TValue}"/>.
        /// </summary>
        readonly struct Enumerable : IEnumerable<IProperty<TDictionary>>
        {
            class Enumerator : IEnumerator<IProperty<TDictionary>>
            {
                readonly TDictionary m_Dictionary;
                readonly KeyValuePairProperty m_Property;
                readonly TKey m_Previous;
                readonly List<TKey> m_Keys;
                int m_Position;

                public Enumerator(TDictionary dictionary, KeyValuePairProperty property)
                {
                    m_Dictionary = dictionary;
                    m_Property = property;
                    m_Previous = property.Key;
                    m_Position = -1;
                    m_Keys = UnityEngine.Pool.ListPool<TKey>.Get();
                    m_Keys.AddRange(m_Dictionary.Keys);
                }

                /// <inheritdoc/>
                public IProperty<TDictionary> Current => m_Property;

                /// <inheritdoc/>
                object IEnumerator.Current => Current;

                /// <inheritdoc/>
                public bool MoveNext()
                {
                    m_Position++;

                    if (m_Position < m_Dictionary.Count)
                    {
                        m_Property.Key = m_Keys[m_Position];
                        return true;
                    }

                    m_Property.Key = m_Previous;
                    return false;
                }

                /// <inheritdoc/>
                public void Reset()
                {
                    m_Position = -1;
                    m_Property.Key = m_Previous;
                }

                /// <inheritdoc/>
                public void Dispose()
                {
                    UnityEngine.Pool.ListPool<TKey>.Release(m_Keys);
                }
            }

            readonly TDictionary m_Dictionary;
            readonly KeyValuePairProperty m_Property;

            public Enumerable(TDictionary dictionary, KeyValuePairProperty property)
            {
                m_Dictionary = dictionary;
                m_Property = property;
            }

            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator()
                => new Enumerator(m_Dictionary, m_Property);

            /// <inheritdoc/>
            IEnumerator<IProperty<TDictionary>> IEnumerable<IProperty<TDictionary>>.GetEnumerator()
                => new Enumerator(m_Dictionary, m_Property);
        }

        /// <inheritdoc cref="IPropertyBag{T}.GetProperties()"/>
        public override PropertyCollection<TDictionary> GetProperties()
        {
            return PropertyCollection<TDictionary>.Empty;
        }

        /// <inheritdoc cref="IPropertyBag{T}.GetProperties(ref T)"/>
        public override PropertyCollection<TDictionary> GetProperties(ref TDictionary container)
        {
            return new PropertyCollection<TDictionary>(new Enumerable(container, m_KeyValuePairProperty));
        }

        /// <summary>
        /// Shared instance of a dictionary element property. We re-use the same instance to avoid allocations.
        /// </summary>
        readonly KeyValuePairProperty m_KeyValuePairProperty = new KeyValuePairProperty();

        void ICollectionPropertyBagAccept<TDictionary>.Accept(ICollectionPropertyBagVisitor visitor, ref TDictionary container)
        {
            visitor.Visit(this, ref container);
        }

        void IDictionaryPropertyBagAccept<TDictionary>.Accept(IDictionaryPropertyBagVisitor visitor, ref TDictionary container)
        {
            visitor.Visit(this, ref container);
        }

        void IDictionaryPropertyAccept<TDictionary>.Accept<TContainer>(IDictionaryPropertyVisitor visitor, Property<TContainer, TDictionary> property, ref TContainer container,
            ref TDictionary dictionary)
        {
            using (new AttributesScope(m_KeyValuePairProperty, property))
            {
                visitor.Visit<TContainer, TDictionary, TKey, TValue>(property, ref container, ref dictionary);
            }
        }

        /// <inheritdoc/>
        bool IKeyedProperties<TDictionary, object>.TryGetProperty(ref TDictionary container, object key, out IProperty<TDictionary> property)
        {
            if (container.ContainsKey((TKey)key))
            {
                property = new KeyValuePairProperty { Key = (TKey)key };
                return true;
            }

            property = default;
            return false;
        }
    }
}
