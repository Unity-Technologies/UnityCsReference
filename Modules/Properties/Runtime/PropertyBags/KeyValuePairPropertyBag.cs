// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.Properties
{
    /// <summary>
    /// A <see cref="IPropertyBag{T}"/> implementation for a generic key/value pair.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    public class KeyValuePairPropertyBag<TKey, TValue> : PropertyBag<KeyValuePair<TKey, TValue>>, INamedProperties<KeyValuePair<TKey, TValue>>
    {
        static readonly DelegateProperty<KeyValuePair<TKey, TValue>, TKey> s_KeyProperty =
            new DelegateProperty<KeyValuePair<TKey, TValue>, TKey>(
                nameof(KeyValuePair<TKey, TValue>.Key),
                (ref KeyValuePair<TKey, TValue> container) => container.Key,
                null);

        static readonly DelegateProperty<KeyValuePair<TKey, TValue>, TValue> s_ValueProperty =
            new DelegateProperty<KeyValuePair<TKey, TValue>, TValue>(
                nameof(KeyValuePair<TKey, TValue>.Value),
                (ref KeyValuePair<TKey, TValue> container) => container.Value,
                null);

        /// <inheritdoc cref="IPropertyBag{T}.GetProperties()"/>
        public override PropertyCollection<KeyValuePair<TKey, TValue>> GetProperties()
        {
            return new PropertyCollection<KeyValuePair<TKey, TValue>>(GetPropertiesEnumerable());
        }

        /// <inheritdoc cref="IPropertyBag{T}.GetProperties(ref T)"/>
        public override PropertyCollection<KeyValuePair<TKey, TValue>> GetProperties(ref KeyValuePair<TKey, TValue> container)
        {
            return new PropertyCollection<KeyValuePair<TKey, TValue>>(GetPropertiesEnumerable());
        }

        static IEnumerable<IProperty<KeyValuePair<TKey, TValue>>> GetPropertiesEnumerable()
        {
            yield return s_KeyProperty;
            yield return s_ValueProperty;
        }

        /// <summary>
        /// Gets the property associated with the specified name.
        /// </summary>
        /// <param name="container">The container hosting the data.</param>
        /// <param name="name">The name of the property to get.</param>
        /// <param name="property">When this method returns, contains the property associated with the specified name, if the name is found; otherwise, null.</param>
        /// <returns><see langword="true"/> if the <see cref="INamedProperties{TContainer}"/> contains a property with the specified name; otherwise, <see langword="false"/>.</returns>
        public bool TryGetProperty(ref KeyValuePair<TKey, TValue> container, string name, out IProperty<KeyValuePair<TKey, TValue>> property)
        {
            if (name == nameof(KeyValuePair<TKey, TValue>.Key))
            {
                property = s_KeyProperty;
                return true;
            }

            if (name == nameof(KeyValuePair<TKey, TValue>.Value))
            {
                property = s_ValueProperty;
                return true;
            }

            property = default;
            return false;
        }
    }
}
