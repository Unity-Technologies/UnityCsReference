// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Entities.Serialization
{
    /// <summary>
    /// The <see cref="SerializedReferences"/> class can be used to gather, serialize and deserialize object references within a stream.
    /// </summary>
    /// <remarks>
    /// An instance of this class can be shared between top level calls to manage references between multiple serialization calls.
    /// </remarks>
    class SerializedReferences
    {
        readonly Dictionary<int, object> m_DeserializationIndex = new Dictionary<int, object>();
        readonly Dictionary<object, int> m_SerializationIndex = new Dictionary<object, int>();
        readonly HashSet<object> m_References = new HashSet<object>();
        readonly HashSet<object> m_Serialized = new HashSet<object>();

        /// <summary>
        /// Creates a new serialized reference to the specified object. This is an internal method.
        /// </summary>
        /// <param name="value">The object to reference.</param>
        /// <returns>The serialized index of the specified object.</returns>
        internal int AddSerializedReference(object value)
        {
            if (!m_SerializationIndex.TryGetValue(value, out var index))
            {
                index = m_SerializationIndex.Count;
                m_SerializationIndex.Add(value, index);
            }

            return index;
        }

        /// <summary>
        /// Gets the serialized reference for the specified object. This is an internal method.
        /// </summary>
        /// <param name="value">The object to get a reference to.</param>
        /// <param name="id">When this method returns, contains the id if successful, otherwise default.</param>
        /// <returns>The serialized index of the specified object.</returns>
        internal bool TryGetSerializedReference(object value, out int id)
        {
            return m_SerializationIndex.TryGetValue(value, out id);
        }

        /// <summary>
        /// Adds a reference to a deserialized object. This is an internal method.
        /// </summary>
        /// <param name="id">The id of the object.</param>
        /// <param name="value">The deserialized value.</param>
        /// <typeparam name="T">The value type.</typeparam>
        internal void AddDeserializedReference<T>(int id, T value)
        {
            if (!TypeTraits<T>.IsValueType)
            {
                m_DeserializationIndex[id] = value;
            }
        }
        
        /// <summary>
        /// Adds a reference to a deserialized object. This is an internal method.
        /// </summary>
        /// <param name="value">The deserialized value.</param>
        /// <typeparam name="T">The value type.</typeparam>
        internal void AddDeserializedReference<T>(T value)
        {
            if (!TypeTraits<T>.IsValueType)
            {
                m_DeserializationIndex[m_DeserializationIndex.Count] = value;
            }
        }

        /// <summary>
        /// Gets the deserialized value for the specified id. This is an internal method.
        /// </summary>
        /// <param name="id">The id of the deserialized value.</param>
        /// <returns>The deserialized object for the specified id.</returns>
        internal object GetDeserializedReference(int id)
        {
            m_DeserializationIndex.TryGetValue(id, out var value);
            return value;
        }
        
        /// <summary>
        /// Flags the specified object as being gathered during the serialized reference pre-pass. This is an internal method.
        /// </summary>
        /// <param name="value">The object being visited.</param>
        /// <returns><see langword="true"/> if this is the first time encountering this object; otherwise, <see langword="false"/>.</returns>
        internal bool SetVisited(object value)
            => m_References.Add(value);
        
        /// <summary>
        /// Flags the specified object as being serialized during the main serialization pass. This is an internal method.
        /// </summary>
        /// <param name="value">The object being serialized.</param>
        /// <returns><see langword="true"/> if this is the first time encountering this object; otherwise, <see langword="false"/>.</returns>
        internal bool SetSerialized(object value)
            => m_Serialized.Add(value);


        /// <summary>
        /// Clears this object for re-use. This is an internal method.
        /// </summary>
        internal void Clear()
        {
            m_References.Clear();
            m_Serialized.Clear();
            m_SerializationIndex.Clear();
            m_DeserializationIndex.Clear();
        }
    }
}
